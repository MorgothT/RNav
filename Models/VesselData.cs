using CommunityToolkit.Mvvm.ComponentModel;
using InvernessPark.Utilities.NMEA;
using InvernessPark.Utilities.NMEA.Sentences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Mapper_v1.Models;

public partial class VesselData : ObservableObject
{

	[ObservableProperty]
	private GGA getGGA = new();
    [ObservableProperty]
    private GSA getGSA = new();
    [ObservableProperty]
    private GST getGST = new();
    [ObservableProperty]
    private GSV getGSV = new();
    [ObservableProperty]
    private HDT getHDT = new();
    [ObservableProperty]
    private RMC getRMC = new();
    [ObservableProperty]
    private VTG getVTG = new();

    [ObservableProperty]
    private Dictionary<string,object> dataViewItems = new();

    public void GenrateDefaultDataList()
    {
        DataViewItems.Add("Latitude", GetGGA.Latitude.Degrees);
        DataViewItems.Add("Longitude", GetGGA.Longitude.Degrees);
        DataViewItems.Add("Heading", GetHDT.HeadingTrue);
        DataViewItems.Add("Time (UTC)", GetGGA.UTC);
        DataViewItems.Add("No. of Sats", GetGGA.SatelliteCount);
        DataViewItems.Add("Quality", GetGGA.FixQuality);
    }

    //public string[] GetDataLabels => GetDataLablesFromNmea();

    //private string[] GetDataLablesFromNmea()
    //{
    //    List<string> strings = new()
    //    {
    //        string.Empty,
    //        "X",
    //        "Y",
    //        "Heading",
    //        "Speed",
    //        "Time",
    //        "Dist to Tgt",
    //        "Dist to Cur",
    //        "Cur X",
    //        "Cur Y"
    //    };


    //    return strings.ToArray();
    //}
    private void UpdateView()
    {
        
        DataViewItems["Latitude"] = GetGGA.Latitude.Degrees;
        DataViewItems["Longitude"] = GetGGA.Longitude.Degrees;
        DataViewItems["Heading"] = GetHDT.HeadingTrue;
        DataViewItems["Time (UTC)"] = GetGGA.UTC;
        DataViewItems["No. of Sats"] = GetGGA.SatelliteCount;
        DataViewItems["Quality"] = GetGGA.FixQuality;

    }
    public void Update(INmeaMessage msg)
    {
        switch (msg.GetType().Name)
        {
            case "GGA":
                GetGGA = (GGA)msg;
                break;
            case "GSA":
                GetGSA = (GSA)msg;
                break;
            case "GST":
                GetGST = (GST)msg;
                break;
            case "GSV":
                GetGSV = (GSV)msg;
                break;
            case "HDT":
                GetHDT = (HDT)msg;
                break;
            case "RMC":
                GetRMC = (RMC)msg;
                break;
            case "VTG":
                GetVTG = (VTG)msg;
                break;
            default:
                break;
        }
        UpdateView();
    }
}

