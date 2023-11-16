using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Mapper_v1.Converters;
using Mapper_v1.Models;
using Mapper_v1.ViewModels;
using Mapper_v1.Properties;
using GeoConverter;
using InvernessPark.Utilities.NMEA;
using Newtonsoft.Json;
using NetTopologySuite.IO;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Extensions.Provider;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Samples.CustomWidget;
using Mapsui.Styles;
using Mapsui.Widgets.ScaleBar;
using Mapsui.UI.Wpf;
using Mapsui.Nts;
using SkiaSharp;
using System.Data;
using Mapsui.UI.Wpf.Extensions;

namespace Mapper_v1.Views;

public partial class MapPage : Page
{
    public BoatShapeLayer MyBoatLayer;
    public VesselData VesselData = new();

    private MapSettings mapSettings = new();
    private CommSettings commSettings = new();

    private ObservableCollection<NmeaDevice> devices = new();

    //private bool measureMode = false;
    private MPoint measureStart;
    private WritableLayer MyMeasurementLayer;
    //private bool targetMode = false;
    private GenericCollectionLayer<List<IFeature>> MyTargets = new();

    private static Converter fromWGS84;
    private static Converter toWGS84;
    private static ColorConvertor colorConvertor = new();
    private readonly MapViewModel vm;

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
            LoadLayers();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private Target AddTarget(MPoint worldPosition)
    {
        int id;
        if (mapSettings.TargetList is null)
        {
            mapSettings.TargetList = new();
        }
        id = mapSettings.TargetList.Count == 0 ? 0 : mapSettings.TargetList.Max(x => x.Id) + 1;
        Converter.Point3d latlon = Converter.ToDeg(new Converter.Point3d(worldPosition.X, worldPosition.Y, 0));
        Target target = new()
        {
            Id = id,
            Name = $"Target no.{id}",
            X = worldPosition.X,
            Y = worldPosition.Y,
            Lat = latlon.Y,
            Lon = latlon.X
        };
        mapSettings.TargetList.Add(target);
        mapSettings.SaveMapSettings();
        return target;
    }

    private void RemoveTarget(IFeature feature)
    {
        if (feature is null ) { return; }
        PointFeature pointFeature = (PointFeature)feature;
        foreach (Target target in mapSettings.TargetList)
        {
            if (target.X == pointFeature.Point.X && target.Y == pointFeature.Point.Y)
            {
                mapSettings.TargetList.Remove(target);
                mapSettings.SaveMapSettings();
                return;
            }
        }
    }
    private void AddCharts()
    {
        foreach (var chart in mapSettings.ChartItems)
        {
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
                    default:
                        break;
                }
            }
        }
    }
    private void AddSavedTargets()
    {
        foreach (Target target in mapSettings.TargetList)
        {
            var feature = Target.CreateTargetFeature(target,mapSettings.TargetRadius,mapSettings.DegreeFormat);
            MyTargets?.Features.Add(feature);
        }
    }
    private void StartMeasurement(MPoint point)
    {
        ToggleTracking.IsChecked = false;
        MyBoatLayer.IsCentered = false;
        //MapControl.Map.Navigator.PanLock = true;
        measureStart = MapControl.Map.Navigator.Viewport.ScreenToWorld(point);
    }
    private void StopMeasurement()
    {
        MyMeasurementLayer.Clear();
        //MapControl.Map.Navigator.PanLock = false;
        measureStart = null;
        MapControl.Refresh();
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
        int epsg = int.Parse(mapSettings.CurrentProjection.Split(':')[1]);
        switch (epsg)
        {
            case 6991:
                fromWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Projections.ITM);
                toWGS84 = new(Converter.Projections.ITM, Converter.Ellipsoids.WGS_84);
                break;
            case 32636:
                fromWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Projections.UTM_36N);
                toWGS84 = new(Converter.Projections.UTM_36N, Converter.Ellipsoids.WGS_84);
                break;
            default:
                fromWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Ellipsoids.WGS_84);
                toWGS84 = new(Converter.Ellipsoids.WGS_84, Converter.Ellipsoids.WGS_84);
                break;
        }
    }
    private void LoadLayers()
    {
        // Special layers
        MyBoatLayer = new BoatShapeLayer(MapControl.Map, mapSettings.BoatShape)
        {
            Name = "Location",
            Enabled = true,
            IsCentered = ToggleTracking.IsChecked.Value,
        };
        MyMeasurementLayer = new WritableLayer() { Name = "Measurement", Opacity = 0.7f };
        MyTargets = new GenericCollectionLayer<List<IFeature>>
        {
            IsMapInfoLayer = true,
            Style = new TargetStyle()
            {
                Color = SKColors.DarkRed,
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
        AddSavedTargets();
        AddCharts();
        // Adding the layers to the map
        MapControl.Map.Layers.Add(MyTargets);
        MapControl.Map.Layers.Add(MyMeasurementLayer);
        MapControl.Map.Layers.Add(MyBoatLayer);
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
                break;
            case "HDT":
                Dispatcher.BeginInvoke(UpdateDirection);
                break;
            default:
                break;
        }
        vm.UpdateDataView(VesselData, fromWGS84);
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
        double heading = VesselData.GetHDT.HeadingTrue;
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
    private void UpdateLocation()
    {
        double lon = VesselData.GetGGA.Longitude.Degrees;
        double lat = VesselData.GetGGA.Latitude.Degrees;
        var p = fromWGS84.Convert(new(lon, lat, 0));
        MPoint point = new MPoint(p.X, p.Y);
        MyBoatLayer.UpdateMyLocation(point);
        MyBoatLayer.DataHasChanged();
    } 
    #endregion

    #region Helpers
    private void ZoomActive()
    {
        MyBoatLayer.MyLocation.X.ToString();
        MRect ext = null;
        foreach (var layer in MapControl.Map.Layers)
        {
            if (layer.Enabled)
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
            MapControl.Map.Navigator.ZoomToBox(ext);

    }
    private ILayer CreateTiffLayer(ChartItem chart)
    {
        var colorTrans = new List<Color>
                        {
                            colorConvertor.WMColorToMapsui(chart.FillColor)
                        };
        return new Layer
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            DataSource = new GeoTiffProvider(chart.Path, colorTrans)
        };
    }
    private ILayer CreateShpLayer(ChartItem chart)
    {
        IProvider shpsource = new ShapeFile(chart.Path, true, readPrjFile: true, projectionCrs: null);
        var layer = new Layer
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            DataSource = shpsource,
            Style = GetShapefileStyles(chart)
        };
        return layer;
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
    private double CalcBearing(MPoint p1, MPoint p2)
    {
        var rad = Math.Atan2((p1.Y - p2.Y), (p1.X - p2.X));
        var deg = rad * 180 / Math.PI;
        return (270 - deg) % 360;
    }
    private void DrawLine(MPoint to)
    {
        MyMeasurementLayer.Clear();
        var line = new WKTReader().Read($"LINESTRING ({measureStart.X} {measureStart.Y},{to.X} {to.Y})");
        var feature = new GeometryFeature(line);
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
                    StartMeasurement(e.MapInfo.ScreenPosition);
                    break;
                case false:
                    StopMeasurement();
                    break;
            }
        }
        if (vm.TargetMode)
        {
            int id = mapSettings.TargetList.Count == 0 ? 0 : mapSettings.TargetList.Max(x => x.Id) + 1;
            Target target = Target.CreateTarget(e.MapInfo.WorldPosition,id,toWGS84);
            mapSettings.TargetList.Add(target);
            mapSettings.SaveMapSettings();
            var feature = Target.CreateTargetFeature(target, mapSettings.TargetRadius,mapSettings.DegreeFormat);
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
            RemoveTarget(info.Feature);
            MapControl.Refresh();
        }
    }
    private void MapControlOnMouseMove(object sender, MouseEventArgs e)
    {
        var screenPosition = e.GetPosition(MapControl);
        var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);
        MousePosX.Text = $"{worldPosition.X:F2}";
        MousePosY.Text = $"{worldPosition.Y:F2}";
        if (vm.MeasurementMode && measureStart is not null)
        {
            double bearing = CalcBearing(measureStart, worldPosition);
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
    //private void ToggleMeasure_Click(object sender, RoutedEventArgs e)
    //{
    //    //measureMode = (bool)ToggleMeasure.IsChecked;
    //    // disable other modes
    //    if (vm.MeasurementMode)
    //    {
    //        vm.TargetMode = false;
    //    }       
        
    //}
    //private void ToggleTarget_Click(object sender, RoutedEventArgs e)
    //{
    //    targetMode = (bool)ToggleTarget.IsChecked;
    //    // disable other modes
    //    if (targetMode)
    //    {
    //        measureMode = false;
    //    }
    //}
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
    #endregion

    
}

