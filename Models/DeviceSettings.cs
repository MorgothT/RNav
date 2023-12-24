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
    public string[] AvailableComPorts => SerialPort.GetPortNames();
    public int[] AvailableBaudRates => new[] { 4800, 9600, 19200, 38400, 57600, 115200, 230400 };

    // UDP/TCP
    [ObservableProperty]
    private string iPAddress;
    [ObservableProperty]
    private int port;

}