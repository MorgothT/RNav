using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Mapper_v1.Models;

public partial class CommSettings : ObservableObject
{
    [ObservableProperty]
    public ObservableCollection<DeviceSettings> devices;
    //public DeviceSettings SelectedDevice { get; set; }

    //public ConnectionType CommType { get; set; }
    //public bool AutoConnect { get; set; }
    //// Serial
    //public string ComPort { get; set; }
    //public int BaudRate { get; set; }
    //public string[] AvailableComPorts => SerialPort.GetPortNames();
    //public int[] AvailableBaudRates => new[] { 4800, 9600, 19200, 38400, 57600, 115200, 230400 };
    //// UDP / TCP
    //public string IPAddress { get; set; }
    //public int Port { get; set; }
    //public IPEndPoint IPEndPoint => IPEndPoint.Parse($"{IPAddress}:{Port}");

    #region Constractor
    public CommSettings() => GetSettings();
    #endregion

    public CommSettings GetSettings()
    {
        if (string.IsNullOrEmpty(Properties.Comm.Default.Devices))
        {
            Devices = new();
            //{
            //    new DeviceSettings()
            //};
            SaveSettings();
        }
        else
        {
            Devices = JsonConvert.DeserializeObject<ObservableCollection<DeviceSettings>>(Properties.Comm.Default.Devices);
        }

        //AutoConnect = Properties.Comm.Default.AutoConnect;
        //ComPort = Properties.Comm.Default.ComPort;
        //BaudRate = Properties.Comm.Default.BaudRate;
        //IPAddress = Properties.Comm.Default.IPAddress;
        //Port = Properties.Comm.Default.Port;
        //CommType = Enum.Parse<ConnectionType>(Properties.Comm.Default.ConnectionType);
        return this;
    }
    public void SaveSettings()
    {
        //Properties.Comm.Default.AutoConnect = AutoConnect;

        //Properties.Comm.Default.ComPort = ComPort;
        //Properties.Comm.Default.BaudRate = BaudRate;
        //Properties.Comm.Default.IPAddress = IPAddress;
        //Properties.Comm.Default.Port = Port;
        //Properties.Comm.Default.ConnectionType = CommType.ToString();
        Properties.Comm.Default.Devices = JsonConvert.SerializeObject(Devices);
        Properties.Comm.Default.Save();
    }

}
public struct UdpState
{
    public UdpClient u;
    public IPEndPoint e;
}


