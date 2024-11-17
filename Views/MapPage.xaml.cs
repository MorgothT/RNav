using InvernessPark.Utilities.NMEA;
using Mapper_v1.Converters;
using Mapper_v1.Helpers;
using Mapper_v1.Layers;
using Mapper_v1.Models;
using Mapper_v1.Projections;
using Mapper_v1.Properties;
using Mapper_v1.Providers;
using Mapper_v1.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Extensions.Provider;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Samples.CustomWidget;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Wpf;
using Mapsui.UI.Wpf.Extensions;
using Mapsui.Widgets.ScaleBar;
using netDxf;
using netDxf.Header;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Mapper_v1.Views;

public partial class MapPage : Page
{
    public BoatShapeLayer MyBoatLayer;
    public MemoryLayer BoatTrailLayer;
    public VesselData VesselData = new();

    private MapSettings mapSettings = new();
    private CommSettings commSettings = new();

    private ObservableCollection<NmeaDevice> devices = new();
    private MPoint measureStart;
    private WritableLayer MyMeasurementLayer;
    private GenericCollectionLayer<List<IFeature>> MyTargets = new();
    private int SelectedTargetId = -1;
    private Target SelectedTarget;
    private List<TimedPoint> MyTrail = new();
    
    private DateTime startLineTime;
    public bool RecordEnabled;
   
    private static ColorConvertor colorConvertor = new();
    private readonly MapViewModel vm;
    private string ProjectCRS;
    
    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        vm = viewModel;

        InitVessel();

        SubscribeToNmea();
        InitMapControl();
        InitUIControls();
        ConnectToGps();
        //MapControl.Focus();
}

    private void InitVessel()
    {
        VesselData.HeadingOffset = mapSettings.HeadingOffset;
        VesselData.DepthOffset = mapSettings.DepthOffset;
        VesselData.PositionOffset = new(mapSettings.PositionOffset.X, mapSettings.PositionOffset.Y);
    }

    private void InitMapControl()
    {
        LoadMapControlState();
        MapControl.MouseMove += MapControlOnMouseMove;
        MapControl.Info += MapControl_Click;
        MapControl.MouseRightButtonDown += MapControl_MouseRightButtonDown;

        MapControl.Map.Navigator.RotationLock = false;
        MapControl.UnSnapRotationDegrees = 30;
        MapControl.ReSnapRotationDegrees = 5;
        MapControl.Renderer.WidgetRenders[typeof(CustomWidget)] = new CustomWidgetSkiaRenderer();
        //Scale Bar
        MapControl.Map.Widgets.Add(new ScaleBarWidget(MapControl.Map)
        {
            MaxWidth = 100,
            ScaleBarMode = ScaleBarMode.Both,
            MarginX = 0,
            MarginY = 25,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Right,
            SecondaryUnitConverter = NauticalUnitConverter.Instance
        });
        MapControl.Map.Navigator.OverrideZoomBounds = new MMinMax(1000000, 0.0149); // zoom to about 1m (depens on Lattitude)
        
        try
        {
            SetCRS();
            InitMapLayers();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void InitUIControls()
    {
        ColorSelector.SelectedColor = colorConvertor.WMColorFromMapsui(MapControl.Map.BackColor);
        Rotation.Foreground = colorConvertor.InvertBrushColor(MapControl.Map.BackColor);
        RotationSlider.Value = MapControl.Map.Navigator.Viewport.Rotation / 3.6;
        Rotation.Text = MapControl.Map.Navigator.Viewport.Rotation.ToString("F0");
    }
    private void SetCRS()
    {
        MapControl.Map.CRS = "EPSG:3857";
        ProjectionDefaults.Projection = new WktProjections(); //new IsraelProjections();
        int epsg = int.Parse(mapSettings.CurrentProjection.Split(':')[1]);
        ProjectCRS = $"EPSG:{epsg}";
    }
    private void AddCharts()
    {
        foreach (var chart in mapSettings.ChartItems)
        {
            if (ProjectProjections.GetProjections().Contains(chart.Projection) == false)
                chart.Projection = ProjectProjections.GetProjections()[0];
            if (chart.Enabled == false) continue;   //Skiping inactive charts
            if (File.Exists(chart.Path))
            {
                switch (chart.ChartType)
                {
                    case ChartType.Shapefile:
                        MapControl.Map.Layers.Add(CreateShpLayer(chart));
                        break;
                    case ChartType.Geotiff:
                        MapControl.Map.Layers.Add(CreateTiffLayer(chart));
                        break;
                    case ChartType.Dxf:
                        if (DxfDocument.CheckDxfFileVersion(chart.Path) < DxfVersion.AutoCad2000)
                        {
                            MessageBox.Show($"Error: {chart.Path}\nMinimum DXF version is AutoCad2000 !");
                            break;
                        }
                        else
                        {
                            MapControl.Map.Layers.Add(CreateDxfLayer(chart));
                            break;
                        }
                    case ChartType.Ecw:
                        MapControl.Map.Layers.Add(CreateEcwLayer(chart));
                        break;
                    default:
                        break;
                }
            }
        }
    }

    #region Data Presistence
    private void LoadSavedTargets()
    {
        SelectedTargetId = mapSettings.SelectedTargetId;
        foreach (Target target in mapSettings.TargetList)
        {
            var feature = Target.CreateTargetFeature(target, mapSettings.TargetRadius, mapSettings.DegreeFormat);
            if ((int)feature["Id"] == SelectedTargetId)
            {
                feature["IsSelected"] = true;
                SelectedTarget = target;
            }
            ProjectionDefaults.Projection.Project(ProjectCRS, MapControl.Map.CRS, feature);
            MyTargets?.Features.Add(feature);
        }
    }
    private void InitMapLayers()
    {
        // Special layers
        MyBoatLayer = new BoatShapeLayer(MapControl.Map, mapSettings.BoatShape)
        {
            Name = "Location",
            Enabled = true,
            IsCentered = ToggleTracking.IsChecked.Value,
        };
        BoatTrailLayer = new MemoryLayer()
        {
            //Features = new[] { CreateTrailFeature() },
            Name = "BoatTrail",
            Style = new VectorStyle
            {
                Fill = null,
                Outline = null,
                Line = { Color = colorConvertor.WMColorToMapsui(mapSettings.BoatShape.OutlineColor) }
            }
        };
        MyMeasurementLayer = new WritableLayer()
        {
            Name = "Measurement",
            Opacity = 0.7f
        };
        MyTargets = new GenericCollectionLayer<List<IFeature>>
        {
            Name = "Targets",
            IsMapInfoLayer = true,
            Style = new TargetStyle()
            {
                Color = SKColors.LimeGreen,
                Opacity = 0.1f,
                Radius = mapSettings.TargetRadius
            }
        };

        // Layer renderers
        if (MapControl.Renderer is Mapsui.Rendering.Skia.MapRenderer && !MapControl.Renderer.StyleRenderers.ContainsKey(typeof(BoatStyle)))
        {
            MapControl.Renderer.StyleRenderers.Add(typeof(BoatStyle), new BoatRenderer());
        }
        if (MapControl.Renderer is Mapsui.Rendering.Skia.MapRenderer && !MapControl.Renderer.StyleRenderers.ContainsKey(typeof(TargetStyle)))
        {
            MapControl.Renderer.StyleRenderers.Add(typeof(TargetStyle), new TargetRenderer());
        }

        // Adding the layers to the map
        if (mapSettings.MapOverlay == true)
        {
            var osm = OpenStreetMap.CreateTileLayer("RNav_OSM");
            osm.Name = "MapOvelay";
            MapControl.Map.Layers.Add(osm);
        }
        AddCharts();
        LoadLastTrail();
        if (mapSettings.ShowTargets == true) 
        {
            LoadSavedTargets();
            MapControl.Map.Layers.Add(MyTargets);
        }
        MapControl.Map.Layers.Add(MyMeasurementLayer);
        MapControl.Map.Layers.Add(BoatTrailLayer);
        MapControl.Map.Layers.Add(MyBoatLayer);
    }
    private void LoadLastTrail()
    {
        try
        {
            MyTrail = mapSettings.GetTrail();
        }
        catch (Exception)
        {

        }
        if (MyTrail is null)
        {
            MyTrail = new();
        }
    }
    private void SaveMapControlState()
    {
        Properties.MapControl.Default.BackColor = colorConvertor.Mapsui2String(MapControl.Map.BackColor);
        Properties.MapControl.Default.Rotation = MapControl.Map.Navigator.Viewport.Rotation;
        Properties.MapControl.Default.BtnTrackingState = (bool)ToggleTracking.IsChecked;
        Properties.MapControl.Default.BtnHeadingState = (bool)ToggleNoseUp.IsChecked;
        Properties.MapControl.Default.DataDisplayState = DataDisplay.IsExpanded;
        Properties.MapControl.Default.Save();
    }
    private void LoadMapControlState()
    {
        if (MapControl.Map is not null)
        {

            MapControl.Map.BackColor = colorConvertor.String2Mapsui(Properties.MapControl.Default.BackColor);
            MapControl.Map.Navigator.RotateTo(Properties.MapControl.Default.Rotation);
            ToggleTracking.IsChecked = Properties.MapControl.Default.BtnTrackingState;
            ToggleNoseUp.IsChecked = Properties.MapControl.Default.BtnHeadingState;
            DataDisplay.IsExpanded = Properties.MapControl.Default.DataDisplayState;
        }
    }
    private void SaveViewport()
    {
        Properties.MapControl.Default.Viewport = JsonConvert.SerializeObject(MapControl.Map.Navigator.Viewport);
        Properties.MapControl.Default.Save();
    }
    private void LoadViewport()
    {
        try
        {
            MapControl.Map.Navigator.SetViewport(JsonConvert.DeserializeObject<Viewport>(Properties.MapControl.Default.Viewport));
            MapControl.Map.Navigator.RotateTo(Properties.MapControl.Default.Rotation);
        }
        catch (Exception)
        {
        }
    }
    #endregion

    #region GNSS_Methods
    private void SubscribeToNmea()
    {
        NmeaHandler nmeaHandler = new NmeaHandler();
        nmeaHandler.LogNmeaMessage += NmeaMessageReceived;
        foreach (DeviceSettings settings in commSettings.Devices)
        {
            var dev = new NmeaDevice(nmeaHandler, settings);
            dev.NmeaReceiver.NmeaMessageFailedChecksum += (bytes, index, count, expected, actual) =>
                {
                    Trace.TraceError($"Failed Checksum: {Encoding.ASCII.GetString(bytes.Skip(index).Take(count).ToArray()).Trim()} expected {expected} but got {actual}");
                };
            dev.NmeaReceiver.NmeaMessageDropped += (bytes, index, count, reason) =>
                {
                    Trace.WriteLine($"Bad Syntax: {Encoding.ASCII.GetString(bytes.Skip(index).Take(count).ToArray())} reason: {reason}");
                };
            dev.NmeaReceiver.NmeaMessageIgnored += (bytes, index, count) =>
                {
                    Trace.WriteLine($"Ignored: {Encoding.ASCII.GetString(bytes.Skip(index).Take(count).ToArray())}");
                };
            devices.Add(dev);
        }
    }
    private void NmeaMessageReceived(INmeaMessage msg)
    {
        VesselData.Update(msg);
        if (RecordEnabled) 
            Dispatcher.BeginInvoke(new Action(() => LogNMEA(msg)));
        switch (msg.GetType().Name)
        {
            case "GGA":
                Dispatcher.BeginInvoke(UpdateLocation);
                Dispatcher.BeginInvoke(DispatcherPriority.Background, LogLocation);
                Dispatcher.BeginInvoke(DispatcherPriority.Background, UpdateTrail);
                break;
            case "HDT":
                Dispatcher.BeginInvoke(UpdateDirection);
                break;
            case "DBT":
            case "DPT":
                Dispatcher.BeginInvoke(UpdateDepth);
                break;
            case "ZDA":
                break;
            default:
                break;
        }
        vm.UpdateDataView(VesselData, ProjectCRS, SelectedTarget);
    }
    private void UpdateTrail()
    {
        if (mapSettings.TrailDuration == 0) return;
        //TimedPoint point = new TimedPoint(VesselData.GetGGA, fromWGS84);
        //MPoint gpsPoint = new MPoint(VesselData.GetGGA.Latitude.Degrees,VesselData.GetGGA.Longitude.Degrees);
        MPoint gpsPoint = new MPoint(VesselData.Longitude, VesselData.Latitude);
        ProjectionDefaults.Projection.Project("EPSG:4326", ProjectCRS, gpsPoint);
        TimedPoint point = new TimedPoint(gpsPoint.X,gpsPoint.Y,VesselData.LastFixTime);
        MyTrail.Add(point);
        if (MyTrail.Count < 2)
        {
            return;
        }
        MyTrail.RemoveAll(x => point.Time.Subtract(x.Time) > TimeSpan.FromMinutes(mapSettings.TrailDuration));
        mapSettings.SaveTrail(MyTrail);

        try
        {
            BoatTrailLayer.Features = new[] { CreateTrailFeature() };
            if (BoatTrailLayer.Features != null && BoatTrailLayer.Features.First() != null)
            {
                ProjectionDefaults.Projection.Project(ProjectCRS, MapControl.Map.CRS, BoatTrailLayer.Features);
            }
        }
        catch (Exception)
        {

        }
    }
    private void ClearTrail()
    {
        try
        {
            if (MyTrail.Count < 2) return;
            MyTrail.RemoveRange(0, MyTrail.Count - 1);
            mapSettings.SaveTrail(MyTrail);
            //BoatTrailLayer.Features = new GeometryFeature[] { null };
            //BoatTrailLayer.DataHasChanged();
        }
        catch
        {
        }
    }

    /// <summary>
    /// Logging vessel trail to file using WGS84 -> Project projection
    /// </summary>
    private void LogLocation()
    {
        string logPath = @$"{mapSettings.LogDirectory}\{DateTime.Today.ToString("yyyyMMdd")}.log";
        if (!Directory.Exists(mapSettings.LogDirectory))
        {
            Directory.CreateDirectory(mapSettings.LogDirectory);
        }
        //TimedPoint point = new TimedPoint(VesselData.GetGGA, fromWGS84);
        MPoint gpsPoint = new MPoint(VesselData.Longitude,VesselData.Latitude);
        ProjectionDefaults.Projection.Project("EPSG:4326", ProjectCRS, gpsPoint);
        TimedPoint point = new TimedPoint(gpsPoint.X, gpsPoint.Y, VesselData.LastFixTime);
        File.AppendAllText(logPath, $"{point.ToLocalTime()}\n");
    }
    //TODO: add Log for Hypack ?
    private void LogNMEA(INmeaMessage msg)
    {
        if (startLineTime == DateTime.MinValue)
            startLineTime = DateTime.UtcNow;
        string logPath = @$"{mapSettings.LogDirectory}\{startLineTime.ToString("yyyyMMdd-HHmmss")}.raw";
        if (!Directory.Exists(mapSettings.LogDirectory))
        {
            Directory.CreateDirectory(mapSettings.LogDirectory);
        }
        File.AppendAllText(logPath, $"{DateTime.UtcNow:HH:mm:ss},{msg.Payload}{Environment.NewLine}");
    }

    private void ConnectToGps()
    {
        foreach (NmeaDevice dev in devices)
        {
            try
            {
                dev.Connect();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    private void UpdateDirection()
    {
        //double heading = (VesselData.GetHDT.HeadingTrue + mapSettings.HeadingOffset) % 360;
        double heading = VesselData.Heading;
        double mapRotation = Properties.MapControl.Default.Rotation;
        if (ToggleNoseUp.IsChecked.Value)
        {
            // when bow up
            MyBoatLayer.UpdateMyDirection(heading, heading);
            MapControl.Map.Navigator.RotateTo(360 - heading);
        }
        else
        {
            //MyBoatLayer.UpdateMyDirection(heading, 0);
            MyBoatLayer.UpdateMyDirection((heading + mapRotation) % 360, 0);
        }
    }
    private void UpdateLocation()   // WGS84 (GNSS) -> Map CRS
    {
        //double lon = VesselData.GetGGA.Longitude.Degrees;
        //double lat = VesselData.GetGGA.Latitude.Degrees;
        //MPoint point = new MPoint(lon, lat);
        MPoint point = new MPoint(VesselData.Longitude, VesselData.Latitude);
        ProjectionDefaults.Projection.Project("EPSG:4326", MapControl.Map.CRS, point);
        MyBoatLayer.UpdateMyLocation(point);
        MyBoatLayer.DataHasChanged();
    }
    private void UpdateDepth()
    { 
        //TODO: Add Depth Alarm
    }

    #endregion

    #region LayerCreators
    private ILayer CreateTiffLayer(ChartItem chart)
    {
        var colorTrans = new List<Color>
                        {
                            colorConvertor.WMColorToMapsui(chart.FillColor)
                        };
        IProvider tiffsource = new GeoTiffProvider(chart.Path, colorTrans)
        {
            CRS = $"EPSG:{chart.Projection.Split(':')[1]}"
        };
        var datasource = new ProjectingProvider(tiffsource)
        {
            CRS = MapControl.Map.CRS
        };
        var layer = new Layer
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            DataSource = datasource //new GeoTiffProvider(chart.Path, colorTrans)
        };

        return layer;
    }
    private ILayer CreateEcwLayer(ChartItem chart)
    {
        var colorTrans = new List<Color>
        {
            colorConvertor.WMColorToMapsui(chart.FillColor)
        };
        EcwProvider ecwsource = new EcwProvider(chart.Path, colorTrans, chart.MaxResulotion)
        {
            CRS = $"EPSG:{chart.Projection.Split(':')[1]}"
        };
        var datasource = new ProjectingProvider(ecwsource)
        {
            CRS = MapControl.Map.CRS
        };
        var layer = new Layer
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            Style = new RasterStyle(),
            DataSource = datasource
        };
        return new RasterizingLayer(layer);
        //return layer;
    }
    private ILayer CreateShpLayer(ChartItem chart)
    {
        IProvider shpsource = new ShapeFile(chart.Path, true, readPrjFile: true, null);
        shpsource.CRS = $"EPSG:{chart.Projection.Split(':')[1]}";
        var datasource = new ProjectingProvider(shpsource)
        {
            CRS = MapControl.Map.CRS
        };
        var layer = new Layer
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            DataSource = datasource,    //shpsource,
            Style = GetShapefileStyles(chart)
        };
        return layer;
    }
    private ILayer CreateDxfLayer(ChartItem chart)
    {
        var layer = new DxfLayer(chart)
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            //Style = GetDxfStyles(chart),   //TODO: Change styles so labels don't have circles !
        };
        
        ProjectionDefaults.Projection.Project($"EPSG:{chart.Projection.Split(':')[1]}", MapControl.Map.CRS, layer.Features);
        return layer;
    }

    #endregion

    #region Helpers
    private void ZoomActive()
    {
        //MyBoatLayer.MyLocation.X.ToString();
        MRect ext = null;
        foreach (var layer in MapControl.Map.Layers)
        {
            if (layer.Enabled && layer.Name != "MapOvelay")
            {
                if (ext is null)
                {
                    ext = layer.Extent;
                }
                else
                {
                    ext = ext.Join(layer.Extent);
                }
            }
        }
        if (ext is not null)
        {
            MapControl.Map.Navigator.ZoomToBox(ext);
            MapControl.Map.Navigator.OverridePanBounds = ext;
        }
    }
    private static StyleCollection GetShapefileStyles(ChartItem chart)
    {
        var styles = new StyleCollection();
        styles.Styles.Add(new VectorStyle
        {
            Outline = new Pen   // Outline of Areas and Points
            {
                Width = chart.LineWidth,
                Color = colorConvertor.WMColorToMapsui(chart.OutlineColor)//new Color(255, 0, 0, 0)
            },
            Fill = new Brush { Color = colorConvertor.WMColorToMapsui(chart.FillColor) },   // Fill of Areas and Points
            Line = new Pen  //Polyline Style
            {
                Width = chart.LineWidth,
                Color = colorConvertor.WMColorToMapsui(chart.LineColor)
            }
        }); ;
        styles.Styles.Add(new LabelStyle
        {
            ForeColor = colorConvertor.WMColorToMapsui(chart.LabelColor),
            BackColor = new Brush(colorConvertor.WMColorToMapsui(chart.BackroundColor)),
            Font = new Font { FontFamily = "GenericSerif", Size = (double)chart.LabelFontSize },
            HorizontalAlignment = (LabelStyle.HorizontalAlignmentEnum)chart.HorizontalAlignment, //LabelStyle.HorizontalAlignmentEnum.Center,
            VerticalAlignment = (LabelStyle.VerticalAlignmentEnum)chart.VerticalAlignment, //LabelStyle.VerticalAlignmentEnum.Center,
            Offset = new Offset { X = 0, Y = 0 },
            Halo = new Pen { Color = colorConvertor.WMColorToMapsui(chart.HaloColor), Width = 1 },
            CollisionDetection = true,
            LabelColumn = chart.LabelAttributeName
        });
        return styles;
    }
    private void DrawLine(MPoint to)
    {
        MyMeasurementLayer.Clear();
        var line = new WKTReader().Read($"LINESTRING ({measureStart.X} {measureStart.Y},{to.X} {to.Y})");
        var feature = new GeometryFeature(line);
        ProjectionDefaults.Projection.Project(ProjectCRS, MapControl.Map.CRS, feature);
        ProjectionDefaults.Projection.Project(ProjectCRS, MapControl.Map.CRS, to);
        var label = new GeometryFeature(new GeometryFactory().CreatePoint(to.ToCoordinate()).Buffer(0.001));
        label.Styles.Add(new LabelStyle
        {
            BackColor = null,
            BorderThickness = 0,
            Text = $"{Distance.Text}m\n{Bearing.Text}°",
            Font = new Font { Bold = true, Size = 12 },
            VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom,
            Halo = new Pen { Color = Color.Wheat, Width = 1 },
        });
        MyMeasurementLayer.Add(feature);
        MyMeasurementLayer.Add(label);
    }
    private void TurnCalloutOff(CalloutStyle currentCallout = null)
    {
        foreach (var feature in MyTargets?.Features)
        {
            var callout = feature.Styles.Where(s => s is CalloutStyle).Cast<CalloutStyle>().FirstOrDefault();
            if (callout == currentCallout) continue;
            callout.Enabled = false;
        }
    }
    private GeometryFeature CreateTrailFeature()
    {
        if (MyTrail.Count < 2) { return null; }
        var trail = new LineString(MyTrail.Select(x => new MPoint(x.X, x.Y).ToCoordinate()).ToArray());
        return new GeometryFeature(trail);
    }
    private void StartMeasurement(MPoint point) // Map CRS -> Project Crs
    {
        ToggleTracking.IsChecked = false;
        MyBoatLayer.IsCentered = false;
        //MapControl.Map.Navigator.PanLock = true;
        //measureStart = MapControl.Map.Navigator.Viewport.ScreenToWorld(point);
        ProjectionDefaults.Projection.Project(MapControl.Map.CRS, ProjectCRS, point);
        measureStart = point;
    }
    private void StopMeasurement()
    {
        MyMeasurementLayer.Clear();
        //MapControl.Map.Navigator.PanLock = false;
        measureStart = null;
        MapControl.Refresh();
    }
    private void RemoveTargetFeature(IFeature feature)
    {
        if (feature is null) { return; }
        var pointFeature = (PointFeature)feature;
        if (SelectedTargetId == (int)pointFeature["Id"])  // Nullify the SelectedTarget
        {
            SelectedTargetId = -1;
            SelectedTarget = null;
            mapSettings.SaveMapSettings();
        }
        foreach (Target target in mapSettings.TargetList)
        {
            if (target.Id == (int)pointFeature["Id"])
            {
                mapSettings.TargetList.Remove(target);
                mapSettings.SaveMapSettings();
                return;
            }
        }
    }
    #endregion

    #region Events
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadViewport();
        Focusable = true;
        Focus();
    }
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveMapControlState();
            SaveViewport();
            foreach (var device in devices)
            {
                device.Dispose();
            }
            MapControl.Dispose();
            Trace.WriteLine("Disposed of MapControl");
        }
        catch (Exception)
        {
            MessageBox.Show("Failed to close map properly.");
        }
    }
    private void ZoomExtent_Click(object sender, RoutedEventArgs e)
    {
        ZoomActive();
    }
    private void MapControl_Click(object sender, MapInfoEventArgs e)
    {
        if (e.MapInfo?.WorldPosition is null)
        {
            return;
        }
        if (vm.MeasurementMode)
        {
            switch (measureStart is null)
            {
                case true:
                    StartMeasurement(e.MapInfo.WorldPosition);  // world pos is Map CRS
                    MyMeasurementLayer.Enabled = true;
                    break;
                case false:
                    StopMeasurement();
                    MyMeasurementLayer.Enabled = false;
                    break;
            }
        }
        if (vm.TargetMode)
        {
            int id = mapSettings.TargetList.Count == 0 ? 0 : mapSettings.TargetList.Max(x => x.Id) + 1;
            MPoint tr = new(e.MapInfo.WorldPosition);
            ProjectionDefaults.Projection.Project(MapControl.Map.CRS, ProjectCRS, tr);
            MPoint wgs = new(tr);
            ProjectionDefaults.Projection.Project(ProjectCRS, "EPSG:4326", wgs);
            Target target = Target.CreateTarget(tr, id, wgs);
            mapSettings.TargetList.Add(target);
            mapSettings.SaveMapSettings();
            var feature = Target.CreateTargetFeature(target, mapSettings.TargetRadius, mapSettings.DegreeFormat);
            ProjectionDefaults.Projection.Project(ProjectCRS, MapControl.Map.CRS, feature);
            MyTargets?.Features.Add(feature);
            e.MapInfo.Layer?.DataHasChanged();
            MapControl.Refresh();
        }
        if (!vm.MeasurementMode && !vm.TargetMode)
        {
            var calloutStyle = e.MapInfo?.Feature?.Styles.Where(s => s is CalloutStyle).Cast<CalloutStyle>().FirstOrDefault();
            TurnCalloutOff(calloutStyle);
            if (calloutStyle is not null)
            {
                calloutStyle.Enabled = !calloutStyle.Enabled;
                e.MapInfo?.Layer?.DataHasChanged(); // To trigger a refresh of graphics.
            }
            MapControl.Refresh();
        }
    }
    private void MapControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (vm.TargetMode)
        {
            if (MyTargets?.Features is null) return;
            var screenPosition = e.GetPosition(MapControl).ToMapsui();
            MapInfo info = MapControl.GetMapInfo(screenPosition);
            MyTargets.Features.Remove(info.Feature);
            RemoveTargetFeature(info.Feature);
            MapControl.Refresh();
        }
        if (vm.MeasurementMode) //do nothing in Ruler mode
        {
        }
        else    //Target Selection
        {
            if (MyTargets?.Features is null) return;
            var screenPosition = e.GetPosition(MapControl).ToMapsui();
            MapInfo info = MapControl.GetMapInfo(screenPosition);
            if (info.Feature is null) return;
            SelectedTargetId = (int)info.Feature["Id"];

            foreach (var targetFeature in MyTargets.Features)
            {
                if ((int)targetFeature["Id"] == SelectedTargetId)
                {
                    targetFeature["IsSelected"] = true;
                    SelectedTarget = Target.CreateTargetFromTargetFeature(targetFeature);
                }
                else
                {
                    targetFeature["IsSelected"] = false;
                }
            }
            mapSettings.SelectedTargetId = SelectedTargetId;
            mapSettings.SaveMapSettings();
            MapControl.Refresh();
        }
    }
    private void MapControlOnMouseMove(object sender, MouseEventArgs e)
    {
        var screenPosition = e.GetPosition(MapControl);
        var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);    // Map CRS
        ProjectionDefaults.Projection.Project(MapControl.Map.CRS, ProjectCRS, worldPosition);                        // Project CRS
        MousePosX.Text = $"{worldPosition.X:F2}";
        MousePosY.Text = $"{worldPosition.Y:F2}";
        if (vm.MeasurementMode && measureStart is not null)
        {
            double bearing = measureStart.CalcBearing(worldPosition);
            double distance = measureStart.Distance(worldPosition);
            {
                //MyMeasurementLayer.
            }
            Distance.Text = $"{distance:F2}";
            Bearing.Text = $"{bearing:F2}";
            DrawLine(worldPosition);
            MapControl.Refresh();
        }
    }

    private void RecordBtn_Click(object sender, RoutedEventArgs e)
    {
        RecordEnabled = RecordLine.IsChecked.Value;
        if (RecordEnabled)
        {
            startLineTime = DateTime.UtcNow;
        }
    }
    #endregion

    #region MapControls
    private void ToggleTracking_Click(object sender, RoutedEventArgs e)
    {
        MyBoatLayer.IsCentered = ToggleTracking.IsChecked.Value;
        SaveMapControlState();
    }
    private void ToggleNoseUp_Unchecked(object sender, RoutedEventArgs e)
    {
        MapControl.Map.Navigator.RotateTo(Properties.MapControl.Default.Rotation);
        SaveMapControlState();
    }
    private void SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
    {
        if (e.NewValue.HasValue)
        {
            MapControl.Map.BackColor = colorConvertor.WMColorToMapsui(e.NewValue.Value);
            Rotation.Foreground = colorConvertor.InvertBrushColor(MapControl.Map.BackColor);
            SaveMapControlState();
        }
    }
    private void RotationSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var percent = RotationSlider.Value / (RotationSlider.Maximum - RotationSlider.Minimum);
        MapControl.Map.Navigator.RotateTo(percent * 360);
        Rotation.Text = MapControl.Map.Navigator.Viewport.Rotation.ToString("F0");
        SaveMapControlState();
    }
    private void Button_ClearTrail(object sender, RoutedEventArgs e)
    {
        ClearTrail();
    }
    private void Grid_KeyDown(object sender, KeyEventArgs e)
    {
        //TODO: add controls and try to stop from losing focus
        Trace.WriteLine(e.Key);
        var vp = MapControl.Map.Navigator.Viewport;
        if (e.Key == Key.OemPlus || e.Key == Key.Add)
        {
            MapControl.Map.Navigator.ZoomIn();
        }
        if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
        {
            MapControl.Map.Navigator.ZoomOut();
        }
        if (e.Key == Key.Left || e.Key == Key.NumPad4)
        {
            MapControl.Map.Navigator.CenterOn(vp.CenterX - vp.Resolution*64, vp.CenterY);
        }
        if (e.Key == Key.Right || e.Key == Key.NumPad6)
        {
            MapControl.Map.Navigator.CenterOn(vp.CenterX + vp.Resolution * 64, vp.CenterY);
        }
        if (e.Key == Key.Up || e.Key == Key.NumPad8)
        {
            MapControl.Map.Navigator.CenterOn(vp.CenterX, vp.CenterY + vp.Resolution * 64);
        }
        if (e.Key == Key.Down || e.Key == Key.NumPad2)
        {
            MapControl.Map.Navigator.CenterOn(vp.CenterX, vp.CenterY - vp.Resolution * 64);
        }
        else
        {
        }
        e.Handled = true;
    }

    #endregion
}

