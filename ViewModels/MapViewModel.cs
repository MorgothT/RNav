using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvernessPark.Utilities.NMEA.Sentences;
using Mapper_v1.Models;
using Mapper_v1.Views;
using Mapsui.Tiling;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Mapper_v1.ViewModels;

public partial class MapViewModel : ObservableObject
{

    [ObservableProperty]
    private Dictionary<string,object> data = new();

    public MapViewModel()
    {
        InitDataView();
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
    }

    public void UpdateDataView(VesselData vessel, GeoConverter.Converter converter)
    {
        double lon, lat;
        lon = vessel.GetGGA.Longitude.Degrees;
        lat = vessel.GetGGA.Latitude.Degrees;
        var p = converter.Convert(new(lon, lat, 0));
        Data["X"] = p.X.ToString("F2");
        Data["Y"] = p.Y.ToString("F2");
        Data["Latitude"] = lat.ToString("F8");
        Data["Longitude"] = lon.ToString("F8");
        Data["Heading"] = vessel.GetHDT.HeadingTrue;
        Data["Time (UTC)"] = vessel.GetGGA.UTC.ToString(@"hh\:mm\:ss");
        Data["No. of Sats"] = vessel.GetGGA.SatelliteCount;
        Data["Quality"] = vessel.GetGGA.FixQuality;
        Data["Speed (Kn)"] = vessel.GetVTG.GroundSpeedKnots.ToString("F2");
        Data = new Dictionary<string, object>(Data);
    }
}
