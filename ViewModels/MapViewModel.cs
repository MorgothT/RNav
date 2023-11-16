using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Helpers;
using Mapper_v1.Models;

namespace Mapper_v1.ViewModels;

public partial class MapViewModel : ObservableObject
{

    [ObservableProperty]
    private MapSettings mapSettings = new();
    [ObservableProperty]
    private Dictionary<string,object> data = new();
    [ObservableProperty]
    private bool measurementMode = false;
    [ObservableProperty]
    private bool targetMode = false;
    [ObservableProperty]
    private MapMode currentMapMode;

    public MapViewModel()
    {
        InitDataView();
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
    }
    public void UpdateDataView(VesselData vessel, GeoConverter.Converter converter)
    {
        //TODO: add heading offset (to all relevent palces)
        double lon, lat;
        lon = vessel.GetGGA.Longitude.Degrees;
        lat = vessel.GetGGA.Latitude.Degrees;
        var p = converter.Convert(new(lon, lat, 0));
        Data["X"] = p.X.ToString("F2");
        Data["Y"] = p.Y.ToString("F2");
        Data["Latitude"] = Formater.FormatLatLong(lat,MapSettings.DegreeFormat);
        Data["Longitude"] = Formater.FormatLatLong(lon,MapSettings.DegreeFormat);
        Data["Heading"] = vessel.GetHDT.HeadingTrue;
        Data["Time (UTC)"] = vessel.GetGGA.UTC.ToString(@"hh\:mm\:ss");
        Data["No. of Sats"] = vessel.GetGGA.SatelliteCount;
        Data["Quality"] = vessel.GetGGA.FixQuality;
        Data["Speed (Kn)"] = vessel.GetVTG.GroundSpeedKnots.ToString("F2");
        Data = new Dictionary<string, object>(Data);
    }
}
