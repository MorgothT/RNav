using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Helpers;
using Mapper_v1.Models;
using Mapper_v1.Projections;
using Mapsui;

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
        //var p = converter.Convert(new(lon, lat, 0));
        Data["X"] = p.X.ToString("F2");
        Data["Y"] = p.Y.ToString("F2");
        Data["Latitude"] = Formater.FormatLatLong(lat, MapSettings.DegreeFormat);
        Data["Longitude"] = Formater.FormatLatLong(lon, MapSettings.DegreeFormat);
        Data["Heading"] = vessel.Heading.ToString("F2");    // Added offset
        //Data["Heading"] = ((vessel.GetHDT.HeadingTrue + MapSettings.HeadingOffset) % 360).ToString("F2");    // Added offset
        Data["Time (UTC)"] = vessel.GetGGA.UTC.ToString(@"hh\:mm\:ss");
        Data["No. of Sats"] = vessel.GetGGA.SatelliteCount;
        Data["Quality"] = vessel.GetGGA.FixQuality;
        Data["Speed (Kn)"] = vessel.SpeedInKnots.ToString("F2");
        Data["Speed Fw (Kn)"] = vessel.SpeedFw.ToString("F2");
        Data["Speed Sb (Kn)"] = vessel.SpeedSb.ToString("F2");
        Data["Depth"] = vessel.Depth.ToString("F2");
        //Data["Depth"] = GeoMath.FeetToMeters(vessel.GetDBT.DepthInFeet).ToString("F2");
        if (CurrentTarget is null)
        {
            Data["Target Name"] = "N/A";
            Data["Bearing"] = "-";
            Data["Distance"] = "-";
        }
        else
        {
            Data["Target Name"] = CurrentTarget.Name;
            MPoint vesselPoint = new MPoint(p.X, p.Y);
            MPoint targetPoint = new MPoint(CurrentTarget.X, CurrentTarget.Y);
            Data["Bearing"] = GeoMath.CalcBearing(vesselPoint, targetPoint).ToString("F1");
            Data["Distance"] = vesselPoint.Distance(targetPoint).ToString("F1");

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
