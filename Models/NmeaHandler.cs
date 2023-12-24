using InvernessPark.Utilities.NMEA;
using InvernessPark.Utilities.NMEA.Sentences;

namespace Mapper_v1.Models;

public class NmeaHandler : INmeaHandler
{
    public double Lat, Lon, Heading, SOG;
    public GGA GetGGA = new();
    public VTG GetVTG = new();
    public HDT GetHDT = new();
    public delegate void OnLogNmeaMessageHandler(INmeaMessage msg);
    public OnLogNmeaMessageHandler LogNmeaMessage;
    public void HandleGGA(INmeaMessage msg)
    {
        OnLogNmeaMessage(msg);
    }

    public void HandleGSA(INmeaMessage msg)
    {
        OnLogNmeaMessage(msg);
    }

    public void HandleGST(INmeaMessage msg)
    {
        OnLogNmeaMessage(msg);
    }

    public void HandleGSV(INmeaMessage msg)
    {
        OnLogNmeaMessage(msg);
    }

    public void HandleHDT(INmeaMessage msg)
    {
        OnLogNmeaMessage(msg);
    }

    public void HandleRMC(INmeaMessage msg)
    {
        OnLogNmeaMessage(msg);
    }

    public void HandleVTG(INmeaMessage msg)
    {
        OnLogNmeaMessage(msg);
    }
    private void OnLogNmeaMessage(INmeaMessage msg)
    {
        switch (msg.GetType().Name)
        {
            //case "GGA":
            //    GetGGA = (GGA)msg;
            //    break;
            //case "VTG":
            //    GetVTG = (VTG)msg;
            //    break;
            //case "HDT":
            //    GetHDT = (HDT)msg;
            //    break;
            default:
                LogNmeaMessage?.Invoke(msg);
                break;
        }

    }
}
