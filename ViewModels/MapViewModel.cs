using BruTile.Wms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlzEx.Standard;
using Mapper_v1.Core;
using Mapper_v1.Core.Contracts;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Models.DataStruct;
using Mapper_v1.Helpers;
using Mapper_v1.Models;
using Mapper_v1.Projections;
using Mapsui;
using NMEADevice;
using System.Dynamic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Windows;

namespace Mapper_v1.ViewModels;

public partial class MapViewModel : ObservableObject
{
    public WktProjections Projection = new WktProjections();

    [ObservableProperty]
    private MapSettings mapSettings = new();
    [ObservableProperty]
    private Dictionary<string, object> data = new();
    [ObservableProperty]
    private bool measurementMode = false;
    [ObservableProperty]
    private bool targetMode = false;
    [ObservableProperty]
    private MapMode currentMapMode;

    private static PortConfig debugPortConfig = new PortConfig()
    {
        IPAddress = "127.0.0.1",
        Port = 5763,
        Type = ConnectionType.TCP
    }; //TODO: implanemt getting from settings
    [ObservableProperty]
    private List<IPortConfig> portConfigs = [debugPortConfig];
    
    [ObservableProperty]
    private Dictionary<string, object> ports = [];
    private static NmeaDeviceConfig debugDeviceConfig = new NmeaDeviceConfig()
    {
        PortName = debugPortConfig.PortName,
        SentencesToUse = SentencesToUse.Default,
        DataTypes = DataTypes.Position | DataTypes.Speed | DataTypes.Heading,
        DeviceName = "debug NMEA",
    };
    [ObservableProperty]
    private List<IDeviceConfig> deviceConfigs = [debugDeviceConfig];
    [ObservableProperty]
    private Dictionary<object,string> devices = [];

    public MapViewModel()
    {
        InitPorts();
        InitDevices();
        InitDataView();
    }

    private void InitDevices()
    {
        foreach (var deviceConfig in DeviceConfigs)
        {
            Type type = deviceConfig.DeviceType;
            var port = Ports.Where(x => x.Key == deviceConfig.PortName).First().Value;
            var portConfig = PortConfigs.Where(x => x.PortName == deviceConfig.PortName).First();

            var device = Activator.CreateInstance(type, [portConfig, port, deviceConfig]);
            //Type type = device.GetType();]
            ((BaseDevice)device).DataUpdated += NewData;
            Devices.Add(device, deviceConfig.DeviceName);
        }

    }

    private void NewData(object data,Type type)
    {
        //double lon, lat;
        //lon = vessel.Longitude;     //vessel.GetGGA.Longitude.Degrees;
        //lat = vessel.Latitude;     //vessel.GetGGA.Latitude.Degrees;
        //MPoint p = new MPoint(lon, lat);
        //Projection.Project("EPSG:4326", CRS, p);


        Data["X"] = ((NmeaDeviceData)data).Position.Longitude.ToString("F2");
        //Data["Y"] = p.Y.ToString("F2");
        //Data["Latitude"] = lat.FormatLatLong(MapSettings.DegreeFormat);
        //Data["Longitude"] = lon.FormatLatLong(MapSettings.DegreeFormat);
        //Data["Heading"] = vessel.Heading.ToString("F2");    // Added offset
        //Data["Time (UTC)"] = vessel.LastFixTime.ToString(@"hh\:mm\:ss\.fff");//GetGGA.UTC.ToString(@"hh\:mm\:ss");
        //Data["No. of Sats"] = vessel.GetGGA.SatelliteCount;
        //Data["Quality"] = vessel.GetGGA.FixQuality;
        //Data["Speed (Kn)"] = vessel.SpeedInKnots.ToString("F2");
        //Data["Speed Fw (Kn)"] = vessel.SpeedFw.ToString("F2");
        //Data["Speed Sb (Kn)"] = vessel.SpeedSb.ToString("F2");
        //Data["Depth"] = vessel.Depth.ToString("F2");
    }

    private void InitPorts()
    {
        foreach (var portConfig in PortConfigs)
        {
            try
            {
                object port;
                switch (portConfig.Type)
                {
                    case ConnectionType.Serial:
                        port = new SerialPort(portConfig.ComPort, portConfig.BaudRate);
                        break;
                    case ConnectionType.UDP:
                        port = new UdpClient(portConfig.Port);
                        break;
                    case ConnectionType.TCP:
                        port = new TcpClient(portConfig.IPAddress, portConfig.Port);
                        break;
                    default:
                        throw new ArgumentException();
                }
                Ports.Add(portConfig.PortName, port);
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }
    #region Commands

    [RelayCommand]
    private void Measure()
    {
        if (MeasurementMode)
        {
            CurrentMapMode = MapMode.Measure;
            TargetMode = false;
        }
        else if (!TargetMode) { CurrentMapMode = MapMode.Navigate; }
    }

    [RelayCommand]
    private void Target()
    {
        if (TargetMode)
        {
            CurrentMapMode = MapMode.Target;
            MeasurementMode = false;
        }
        else if (!MeasurementMode) { CurrentMapMode = MapMode.Navigate; }
    }

    #endregion

    private void InitDataView()
    {
        Data.Add("Time (UTC)", 0);
        Data.Add("Latitude", 0);
        Data.Add("Longitude", 0);
        Data.Add("X", 0);
        Data.Add("Y", 0);
        Data.Add("No. of Sats", 0);
        Data.Add("Quality", 0);
        Data.Add("Heading", 0);
        Data.Add("Speed (Kn)", 0);
        Data.Add("Speed Fw (Kn)", double.NaN);
        Data.Add("Speed Sb (Kn)", double.NaN);
        Data.Add("Depth", 0);

        Data.Add("Target Name", "N/A");
        Data.Add("Bearing", "-");
        Data.Add("Distance", "-");
    }

    public void UpdateDataView(VesselData vessel,string CRS, Target CurrentTarget)
    {
        double lon, lat;
        lon = vessel.Longitude;     //vessel.GetGGA.Longitude.Degrees;
        lat = vessel.Latitude;     //vessel.GetGGA.Latitude.Degrees;
        MPoint p = new MPoint(lon,lat);
        Projection.Project("EPSG:4326",CRS,p);

        Data["X"] = p.X.ToString("F2");
        Data["Y"] = p.Y.ToString("F2");
        Data["Latitude"] = lat.FormatLatLong(MapSettings.DegreeFormat);
        Data["Longitude"] = lon.FormatLatLong(MapSettings.DegreeFormat);
        Data["Heading"] = vessel.Heading.ToString("F2");    // Added offset
        Data["Time (UTC)"] = vessel.LastFixTime.ToString(@"hh\:mm\:ss\.fff");//GetGGA.UTC.ToString(@"hh\:mm\:ss");
        Data["No. of Sats"] = vessel.GetGGA.SatelliteCount;
        Data["Quality"] = vessel.GetGGA.FixQuality;
        Data["Speed (Kn)"] = vessel.SpeedInKnots.ToString("F2");
        Data["Speed Fw (Kn)"] = vessel.SpeedFw.ToString("F2");
        Data["Speed Sb (Kn)"] = vessel.SpeedSb.ToString("F2");
        Data["Depth"] = vessel.Depth.ToString("F2");

        if (CurrentTarget is null)
        {
            Data["Target Name"] = "N/A";
            Data["Bearing"] = "-";
            Data["Distance"] = "-";
        }
        else
        {
            Data["Target Name"] = CurrentTarget.Name;
            //MPoint vesselPoint = new MPoint(p.X, p.Y);
            //MPoint targetPoint = CurrentTarget.ToMPoint();
            Data["Bearing"] = p.CalcBearing(CurrentTarget).ToString("F1");
            Data["Distance"] = p.Distance(CurrentTarget).ToString("F1");
        }
        Data = new Dictionary<string, object>(Data);
    }

    //public void UpdateDataView(VesselData vessel, GeoConverter.Converter converter, Target CurrentTarget)
    //{
    //    double lon, lat;
    //    lon = vessel.GetGGA.Longitude.Degrees;
    //    lat = vessel.GetGGA.Latitude.Degrees;
    //    var p = converter.Convert(new(lon, lat, 0));
    //    Data["X"] = p.X.ToString("F2");
    //    Data["Y"] = p.Y.ToString("F2");
    //    Data["Latitude"] = Formater.FormatLatLong(lat, MapSettings.DegreeFormat);
    //    Data["Longitude"] = Formater.FormatLatLong(lon, MapSettings.DegreeFormat);
    //    Data["Heading"] = ((vessel.GetHDT.HeadingTrue + MapSettings.HeadingOffset) % 360).ToString("F2");    // Added offset
    //    Data["Time (UTC)"] = vessel.GetGGA.UTC.ToString(@"hh\:mm\:ss");
    //    Data["No. of Sats"] = vessel.GetGGA.SatelliteCount;
    //    Data["Quality"] = vessel.GetGGA.FixQuality;
    //    Data["Speed (Kn)"] = vessel.GetVTG.GroundSpeedKnots.ToString("F2");
    //    if (CurrentTarget is null)
    //    {
    //        Data["Target Name"] = "N/A";
    //        Data["Bearing"] = "-";
    //        Data["Distance"] = "-";
    //    }
    //    else
    //    {
    //        Data["Target Name"] = CurrentTarget.Name;
    //        MPoint vesselPoint = new MPoint(p.X, p.Y);
    //        MPoint targetPoint = new MPoint(CurrentTarget.X, CurrentTarget.Y);
    //        Data["Bearing"] = GeoMath.CalcBearing(vesselPoint, targetPoint).ToString("F1");
    //        Data["Distance"] = vesselPoint.Distance(targetPoint).ToString("F1");

    //    }
    //    Data = new Dictionary<string, object>(Data);
    //}

}
