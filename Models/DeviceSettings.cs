using CommunityToolkit.Mvvm.ComponentModel;
using System.IO.Ports;

namespace Mapper_v1.Models;

public partial class DeviceSettings : ObservableObject, IDeviceSettings
{
    [ObservableProperty]
    private ConnectionType commType;
    [ObservableProperty]
    private bool autoConnect;

    // Serial
    [ObservableProperty]
    private string comPort;
    [ObservableProperty]
    private int baudRate;
    public static string[] AvailableComPorts
    {
        get
        {
            try
            {
                return SerialPort.GetPortNames().Order().ToArray();
            }
            catch (Exception ex)
            {
                return [ex.Message];
            }
        }
    }

    public static int[] AvailableBaudRates => new[] { 4800, 9600, 19200, 38400, 57600, 115200, 230400 };

    // UDP/TCP
    [ObservableProperty]
    private string iPAddress;
    [ObservableProperty]
    private int port;

}