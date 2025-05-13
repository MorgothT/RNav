using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Core;
using Mapper_v1.Core.Contracts;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Models.DataStruct;
using Mapper_v1.Core.Models.Devices;
using Mapper_v1.Helpers;
using Mapper_v1.Models;
using Mapper_v1.Core.Projections;
using Mapsui;
using Mapper_v1.Views;
using System.Windows;
using System.Net.Sockets;
using System.Collections.ObjectModel;

namespace Mapper_v1.ViewModels;

public partial class MapViewModel : ObservableObject
{
    public WktProjections Projection = new WktProjections();

    [ObservableProperty]
    private MapSettings mapSettings = new();
    [ObservableProperty]
    private MobileSettings mobileSettings = new();
    [ObservableProperty]
    private Dictionary<string, object> data = new();
    [ObservableProperty]
    private bool measurementMode = false;
    [ObservableProperty]
    private bool targetMode = false;
    [ObservableProperty]
    private MapMode currentMapMode;
    [ObservableProperty]
    private ObservableCollection<Mobile> mobiles;

    //TODO: implanemt getting from settings
    //private static PortConfig debugPortConfig = new()
    //{
    //    Type = ConnectionType.TCP,
    //    IPAddress = "127.0.0.1",
    //    Port = 5763
    //};
    //private static PortConfig debugPortConfig2 = new()
    //{
    //    Type = ConnectionType.TCP,
    //    IPAddress = "127.0.0.1",
    //    Port = 5764
    //};
    //private static NmeaDeviceConfig debugDeviceConfig = new()
    //{
    //    PortConfig = debugPortConfig,
    //    SentencesToUse = SentencesToUse.Default,
    //    DataTypes = DataTypes.Position | DataTypes.Speed | DataTypes.Heading,
    //    DeviceName = "debug NMEA",
    //};
    //private static NmeaDeviceConfig debugDeviceConfig2 = new()
    //{
    //    PortConfig = debugPortConfig2,
    //    SentencesToUse = SentencesToUse.Default,
    //    DataTypes = DataTypes.Position | DataTypes.Speed | DataTypes.Heading,
    //    DeviceName = "debug NMEA",
    //};
    //private Mobile debugMobile = new()
    //{
    //    Name = "Debug Mobile",
    //    IsPrimery = true,
    //};
    //private Mobile debugMobile2 = new()
    //{
    //    Name = "another Debug Mobile",
    //    //IsPrimery = true,
    //};

    public MapViewModel()
    {
        InitMobiles();
        InitDataView();
    }

    private void InitMobiles()
    {
        try
        {
            // TODO: Load Mobiles from settings
            // Mobiles.Add(debugMobile);
            // Mobiles.Add(debugMobile2);

            Mobiles = MobileSettings.Mobiles;
            // there must be at least 1 mobile
            if (Mobiles.Count < 1) throw new Exception("There must be at least 1 mobile !");
            // there must be only 1 primery
            if (Mobiles.Count(m => m.IsPrimery) > 1)
            {
                // remove primery from all 
                foreach (var mobile in Mobiles)
                {
                    mobile.IsPrimery = false;
                }
            }
            if (Mobiles.Count(m => m.IsPrimery) == 0)
            {
                // make the 1st primery
                Mobiles.First().IsPrimery = true;
            }

            // INIT Mobiles devices
            foreach (var mobile in Mobiles)
            {
                mobile.InitDevices();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
            // throw;
        }
    }
    

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
    public void EditDataView()
    {

    }
    public void UpdateDataView(DataDisplay vessel,string CRS, Target CurrentTarget)
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
        Data["Time (UTC)"] = vessel.LastFixTime.ToString(@"HH\:mm\:ss\.fff");//GetGGA.UTC.ToString(@"hh\:mm\:ss");
        Data["No. of Sats"] = //vessel.GetGGA.SatelliteCount;
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
}
