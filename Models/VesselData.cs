using CommunityToolkit.Mvvm.ComponentModel;
using InvernessPark.Utilities.NMEA;
using InvernessPark.Utilities.NMEA.Sentences;

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
    private List<TimedPoint> trail = new();
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
    }
}

