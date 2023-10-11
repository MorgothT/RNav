using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapper_v1.Converters;
using Mapper_v1.Models;
using Mapper_v1.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Extensions.Provider;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Providers;
using Mapsui.Samples.CustomWidget;
using Mapsui.Styles;
using Mapsui.Widgets.ScaleBar;
using System.IO.Ports;
using System.Net.Sockets;
using InvernessPark.Utilities.NMEA;
using Mapper_v1.Properties;
using GeoConverter;
using System.Diagnostics;
using InvernessPark.Utilities.NMEA.Sentences;
using System.Windows.Threading;
using Mapsui.UI.Wpf;
using Newtonsoft.Json;
using System.Net;

namespace Mapper_v1.Views;

public partial class MapPage : Page
{
    public BoatShapeLayer MyBoatLayer;
    private NmeaReceiver nmeaReceiver;

    private ColorConvertor colorConvertor = new();
    private MapSettings mapSettings = new();
    private CommSettings commSettings = new();
    private SerialPort serialPort;
    private UdpClient udpClient;
    private TcpClient tcpClient;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    
    //private Random random = new Random(0);
    private Converter geoConverter;
    //private BoatShape boatShape;

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //DefaultNmeaHandler nmeaHandler = new DefaultNmeaHandler();
        NmeaHandler nmeaHandler = new NmeaHandler();
        nmeaHandler.LogNmeaMessage += NmeaMessageReceived;
        nmeaReceiver = new(nmeaHandler);


        // ... Attach handler for NMEA messages that fail NMEA checksum verification
        nmeaReceiver.NmeaMessageFailedChecksum += (bytes, index, count, expected, actual) => {
            Trace.TraceError($"Failed Checksum: {Encoding.ASCII.GetString(bytes.Skip(index).Take(count).ToArray()).Trim()} expected {expected} but got {actual}");
        };
        // ... Attach handler for NMEA messages that contain invalid syntax
        nmeaReceiver.NmeaMessageDropped += (bytes, index, count, reason) => {
            Trace.WriteLine($"Bad Syntax: {Encoding.ASCII.GetString(bytes.Skip(index).Take(count).ToArray())} reason: {reason}");
        };
        // ... Attach handler for NMEA messages that are ignored (unsupported)
        nmeaReceiver.NmeaMessageIgnored += (bytes, index, count) => {
            Trace.WriteLine($"Ignored: {Encoding.ASCII.GetString(bytes.Skip(index).Take(count).ToArray())}");
        };

        InitMapControl();
        InitUIControls();
        ZoomActive();
    }

    private void NmeaMessageReceived(INmeaMessage msg)
    {
        switch (msg.GetType().Name)
        {
            case "GGA":
                Dispatcher.BeginInvoke(UpdateLocation,msg as GGA);
                break;
            case "VTG":
                
                break;
            case "HDT":
                Dispatcher.BeginInvoke(UpdateDirection, msg as HDT);
                break;
            default: 
                break;
        }
    }

    private void InitUIControls()
    {
        ColorSelector.SelectedColor = colorConvertor.WMColorFromMapsui(MapControl.Map.BackColor);
        Rotation.Foreground = colorConvertor.InvertBrushColor(MapControl.Map.BackColor);
        RotationSlider.Value = MapControl.Map.Navigator.Viewport.Rotation / 3.6;
        Rotation.Text = MapControl.Map.Navigator.Viewport.Rotation.ToString("F0");
    }

    private void InitMapControl()
    {
        LoadMapControlState();

        MapControl.MouseMove += MapControlOnMouseMove;
        MapControl.FeatureInfo += MapControlFeatureInfo;

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
            ConnectToGps();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void ConnectToGps()
    {
        //TODO: Implament connections !
        switch (commSettings.CommType)
        {
            case ConnectionType.Serial:
                ConnectToSerial();
                break;
            case ConnectionType.UDP:
                ConnectToUdp();
                break;
            case ConnectionType.TCP:
                ConnectToTcp();
                break;
            default:
                break;
        }
    }
    private void ConnectToUdp()
    {
        IPEndPoint e = new IPEndPoint(IPAddress.Any, commSettings.Port);
        udpClient = new UdpClient(e);
        UdpState s = new UdpState();
        s.e = e;
        s.u = udpClient;
        try
        {
            udpClient.BeginReceive(new AsyncCallback(ReceiveUdp), s); 
        }
        catch (Exception)
        {
            MessageBox.Show("Could not open the Udp port.\nPlease check the settings tab.");
        }
    }
    private void ReceiveUdp(IAsyncResult ar)
    {
        try
        {
            UdpClient u = ((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = ((UdpState)(ar.AsyncState)).e;
            byte[] received = udpClient.EndReceive(ar, ref e);
            nmeaReceiver.Receive(received);
            udpClient.BeginReceive(new AsyncCallback(ReceiveUdp), ((UdpState)(ar.AsyncState)));
        }
        catch (ObjectDisposedException)
        {
            //this will terminate BeginReceive
        }
        catch (FormatException ex)
        {   //Broken NMEA sentences
            MessageBox.Show($"{ex.Message}\n{ex.InnerException}\n{ex.StackTrace}");
            udpClient.BeginReceive(new AsyncCallback(ReceiveUdp), ((UdpState)(ar.AsyncState)));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n{ex.InnerException}\n{ex.StackTrace}");
        }
    }

    private async void ConnectToTcp()
    {
        CancellationToken ct = cancellationTokenSource.Token;
        tcpClient = new();
        try
        {
            await tcpClient.ConnectAsync(commSettings.IPEndPoint, ct);
            while (!ct.IsCancellationRequested)
            {
                await ReceiveTcpAsync(ct);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task ReceiveTcpAsync(CancellationToken ct)
    {
        NetworkStream tcpStream = tcpClient.GetStream();
        //Memory<byte> buffer = new Memory<byte> { };
        byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
        await tcpStream.ReadAsync(buffer,0, tcpClient.ReceiveBufferSize, ct);
        nmeaReceiver.Receive(buffer.ToArray());
    }

    private void ConnectToSerial()
    {
        serialPort = new SerialPort(commSettings.ComPort, commSettings.BaudRate);
        try
        {
            serialPort.Open();
            serialPort.DataReceived += SerialPort_DataReceived;
        }
        catch (Exception)
        {
            MessageBox.Show("Could not open the serial port.\nPlease check the settings tab.");
        }
    }
    
    private void UpdateDirection(HDT hdt)
    {
        // Simulating heading values
        //double heading = random.NextDouble() * 360;
        double heading = hdt.HeadingTrue;
        if (ToggleNoseUp.IsChecked.Value)
        {
            // when bow up
            MyBoatLayer.UpdateMyDirection(heading,heading);
            MapControl.Map.Navigator.RotateTo(360-heading);
        }
        else
        {
            MyBoatLayer.UpdateMyDirection(heading, 0);
        }
    }
    private void UpdateLocation(GGA gga)
    {
        double lon = gga.Longitude.Degrees;
        double lat = gga.Latitude.Degrees;
        var p = geoConverter.Convert(new(lon, lat, 0));
        MPoint point = new MPoint(p.X, p.Y);
        MyBoatLayer.UpdateMyLocation(point);
        MyBoatLayer.DataHasChanged();

        PosX.Text = MyBoatLayer.MyLocation.X.ToString("F2");
        PosY.Text = MyBoatLayer.MyLocation.Y.ToString("F2");
        //TODO: Add logic to keep vessel on screen (nose up and in center)
    }
    private void SetCRS()
    {
        //DotSpatialProjection.Init();
        //int epsg = int.Parse(mapSettings.CurrentProjection.Split(':')[1]);
        //var toProjectProjection = DotSpatialProjection.GetTransformation(4326, epsg);

        //MapControl.Map.CRS = $"EPSG:{mapSettings.CurrentProjection.Split(':')[1]}";
        int epsg = int.Parse(mapSettings.CurrentProjection.Split(':')[1]);
        switch (epsg)
        {
            case 6991:
                geoConverter = new(Converter.Ellipsoids.WGS_84, Converter.Projections.ITM);
                break;
            case 32636:
                geoConverter = new(Converter.Ellipsoids.WGS_84, Converter.Projections.UTM_36N);
                break;
            default:
                geoConverter = new(Converter.Ellipsoids.WGS_84, Converter.Ellipsoids.WGS_84);
                break;
        }
    }
    private void LoadLayers()
    {
        MyBoatLayer = new BoatShapeLayer(MapControl.Map,mapSettings.BoatShape)
        {
            Name = "Location",
            Enabled = true,
            IsCentered = ToggleTracking.IsChecked.Value,
        };
        if (MapControl.Renderer is Mapsui.Rendering.Skia.MapRenderer && !MapControl.Renderer.StyleRenderers.ContainsKey(typeof(BoatStyle)))
        {
            MapControl.Renderer.StyleRenderers.Add(typeof(BoatStyle), new BoatRenderer());
        }
 
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
        MapControl.Map.Layers.Add(MyBoatLayer);
    }
    private ILayer CreateTiffLayer(ChartItem chart)
    {
        var colorTrans = new List<Color>
                        {
                            colorConvertor.WMColorToMapsui(chart.Color)
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
        
        return new Layer
        {
            Opacity = chart.Opacity,
            Enabled = chart.Enabled,
            Name = chart.Name,
            DataSource = shpsource,
            Style = new VectorStyle
            {
                Line = new Pen
                {
                    Width = chart.LineWidth,
                    Color = colorConvertor.WMColorToMapsui(chart.Color)
                }
            }
        };
    }
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
    private void SaveMapControlState()
    {
        Properties.MapControl.Default.BackColor = colorConvertor.Mapsui2String(MapControl.Map.BackColor);
        Properties.MapControl.Default.Rotation = MapControl.Map.Navigator.Viewport.Rotation;
        Properties.MapControl.Default.BtnTrackingState = (bool)ToggleTracking.IsChecked;
        Properties.MapControl.Default.BtnHeadingState = (bool)ToggleNoseUp.IsChecked;
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
        }
        catch (Exception)
        {
        }
    }

    #region Events
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadViewport();
    }
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveViewport();
            cancellationTokenSource.Cancel();
            if (serialPort is not null && serialPort.IsOpen)
            {
                serialPort.DataReceived -= SerialPort_DataReceived;
                serialPort.Close();
            }
            if (udpClient is not null)
            { 
                udpClient.Close();
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Couldn't close the serial port.\nPlease check settings page.");
        }
    }
    private void ZoomExtent_Click(object sender, RoutedEventArgs e)
    {
        ZoomActive();
    }
    private void MapControlOnInfo(object? sender, MapInfoEventArgs args)
    {
        if (args.MapInfo?.Feature != null)
        {
            FeatureInfoBorder.Visibility = Visibility.Visible;
            FeatureInfo.Text = $"Click Info:{Environment.NewLine}{args.MapInfo.Feature.ToDisplayText()}";
        }
        else
        {
            FeatureInfoBorder.Visibility = Visibility.Hidden;
        }
    }
    private static void MapControlFeatureInfo(object? sender, FeatureInfoEventArgs e)
    {
        MessageBox.Show(e.FeatureInfo?.ToDisplayText());
    }
    private void MapControlOnMouseMove(object sender, MouseEventArgs e)
    {
        var screenPosition = e.GetPosition(MapControl);
        var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);
        MousePosX.Text = $"{worldPosition.X:F2}";
        MousePosY.Text = $"{worldPosition.Y:F2}";
    }
    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        int bytesToRead = serialPort.BytesToRead;
        byte[] buffer = new byte[bytesToRead];
        serialPort.Read(buffer, 0, bytesToRead);

        nmeaReceiver.Receive(buffer);
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
    #endregion
}
