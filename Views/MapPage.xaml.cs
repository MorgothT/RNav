using GeoConverter;
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

    private static Converter fromWGS84;
    //private static Converter toWGS84;
    //Projection ProjectProjection = new();
    private static ColorConvertor colorConvertor = new();
    private readonly MapViewModel vm;
    private string ProjectCRS;
    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        vm = viewModel;

        SubscribeToNmea();
        InitMapControl();
        InitUIControls();
        ConnectToGps();
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
        MapControl.Map.Widgets.Add(new ScaleBarWidget(MapControl.Map)
        {
            MaxWidth = 100,
            ScaleBarMode = ScaleBarMode.Both,
            MarginX = 0,
            MarginY = 10,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Right,
            SecondaryUnitConverter = NauticalUnitConverter.Instance
        });
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
        //switch (epsg)
        //{
        //    case 6991:
        //        //fromWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Projections.ITM);
        //        //toWGS84 = new(Converter.Projections.ITM, Converter.Ellipsoids.WGS_84);

        //        ProjectCRS = "EPSG:6991";
        //        break;
        //    case 32636:
        //        //fromWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Projections.UTM_36N);
        //        //toWGS84 = new(Converter.Projections.UTM_36N, Converter.Ellipsoids.WGS_84);
        //        ProjectCRS = "EPSG:32636";
        //        break;
        //    default:
        //        //fromWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Ellipsoids.WGS_84);
        //        //toWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Ellipsoids.WGS_84);
        //        ProjectCRS = "EPSG:4326";
        //        break;
        //}
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
                Line = { Color = colorConvertor.WMColorToMapsui(mapSettings.BoatShape.Outline) }
            }
        };
        MyMeasurementLayer = new WritableLayer() { Name = "Measurement", Opacity = 0.7f };
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
        // Adding the features
        if (mapSettings.MapOverlay == true)
        {
            var osm = OpenStreetMap.CreateTileLayer("RNav_OSM");
            osm.Name = "MapOvelay";
            MapControl.Map.Layers.Add(osm);
        }
        LoadSavedTargets();
        AddCharts();
        LoadLastTrail();
        // Adding the layers to the map

        MapControl.Map.Layers.Add(MyTargets);
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
            default:
                break;
        }
        vm.UpdateDataView(VesselData, ProjectCRS, SelectedTarget);
    }
    private void UpdateTrail()
    {
        if (mapSettings.TrailDuration == 0) return;
        //TimedPoint point = new TimedPoint(VesselData.GetGGA, fromWGS84);
        MPoint gpsPoint = new MPoint(VesselData.GetGGA.Latitude.Degrees,VesselData.GetGGA.Longitude.Degrees);
        ProjectionDefaults.Projection.Project("EPSG:4326", ProjectCRS, gpsPoint);
        TimedPoint point = new TimedPoint(gpsPoint.X,gpsPoint.Y,DateTime.Today.AddSeconds(VesselData.GetGGA.UTC.TotalSeconds));
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
        MPoint gpsPoint = new MPoint(VesselData.GetGGA.Latitude.Degrees, VesselData.GetGGA.Longitude.Degrees);
        ProjectionDefaults.Projection.Project("EPSG:4326", ProjectCRS, gpsPoint);
        TimedPoint point = new TimedPoint(gpsPoint.X, gpsPoint.Y, DateTime.Today.AddSeconds(VesselData.GetGGA.UTC.TotalSeconds));
        File.AppendAllText(logPath, $"{point.ToLocalTime()}\n");
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
        double heading = (VesselData.GetHDT.HeadingTrue + mapSettings.HeadingOffset) % 360;
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
        double lon = VesselData.GetGGA.Longitude.Degrees;
        double lat = VesselData.GetGGA.Latitude.Degrees;
        MPoint point = new MPoint(lon, lat);
        ProjectionDefaults.Projection.Project("EPSG:4326", MapControl.Map.CRS, point);
        //var p = fromWGS84.Convert(new(lon, lat, 0));
        //Point point = new MPoint(p.X, p.Y);
        MyBoatLayer.UpdateMyLocation(point);
        MyBoatLayer.DataHasChanged();
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
        var layer = new DxfLayer(chart.Path)
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            Style = GetShapefileStyles(chart),   // need to change
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
    private StyleCollection GetShapefileStyles(ChartItem chart)
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
        MyMeasurementLayer.Add(feature);
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
                    //StartMeasurement(e.MapInfo.ScreenPosition);
                    StartMeasurement(e.MapInfo.WorldPosition);  // world pos is Map CRS
                    break;
                case false:
                    StopMeasurement();
                    break;
            }
        }
        if (vm.TargetMode)
        {
            int id = mapSettings.TargetList.Count == 0 ? 0 : mapSettings.TargetList.Max(x => x.Id) + 1;
            MPoint tr = e.MapInfo.WorldPosition;
            ProjectionDefaults.Projection.Project(MapControl.Map.CRS, ProjectCRS, tr);
            MPoint wgs = tr;
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
            double bearing = GeoMath.CalcBearing(measureStart, worldPosition);
            double distance = measureStart.Distance(worldPosition);
            Distance.Text = $"{distance:F2}";
            Bearing.Text = $"{bearing:F2}";
            DrawLine(worldPosition);
            MapControl.Refresh();
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
    #endregion
}

