namespace Mapper_v1.Models
{
    public interface IDeviceSettings
    {
        ConnectionType CommType { get; set; }

        //int[] AvailableBaudRates { get; }
        //string[] AvailableComPorts { get; }
        int BaudRate { get; set; }
        string ComPort { get; set; }

        string IPAddress { get; set; }
        int Port { get; set; }
    }
}