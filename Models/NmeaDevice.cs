using InvernessPark.Utilities.NMEA;
using Mapper_v1.Core;
using Mapper_v1.Core.Models;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace Mapper_v1.Models;

public class NmeaDevice
{
    public NmeaReceiver NmeaReceiver { get; }
    public NmeaHandler NmeaHandler { get; private set; }
    public PortConfig DeviceSettings { get; private set; }
    public object Port { get; private set; }

    private CancellationTokenSource tokenSource = new();

    public NmeaDevice(NmeaHandler handler, PortConfig settings)
    {
        NmeaHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        DeviceSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        NmeaReceiver = new(NmeaHandler);
    }

    public void Connect()
    {
        if (DeviceSettings is not null)
        {
            switch (DeviceSettings.CommType)
            {
                case ConnectionType.Serial:
                    ConnectSerial();
                    break;
                case ConnectionType.UDP:
                    ConnectUDP();
                    break;
                case ConnectionType.TCP:
                    ConnectTCP();
                    break;
                default:
                    break;
            }
        }
    }
    public void Dispose()
    {
        tokenSource.Cancel();
    }

    private void ConnectSerial()
    {
        try
        {
            Port = new SerialPort(DeviceSettings.ComPort, DeviceSettings.BaudRate);
            (Port as SerialPort).Open();
            (Port as SerialPort).DataReceived += DataReceived_Serial;

            tokenSource.Token.Register(() => (Port as SerialPort).Close());   // Registering Cancellation token for Closing port by connection type
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show($"Couldn't open {(Port as SerialPort).PortName}.{Environment.NewLine}Please check Settings page.");
        }
        catch { }
    }
    private void DataReceived_Serial(object sender, SerialDataReceivedEventArgs e)
    {
        Task.Run(() =>
        {
            var port = (SerialPort)sender;
            int bytesToRead = port.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            try
            {
                port.Read(buffer, 0, bytesToRead);
            }
            catch (OperationCanceledException)
            {
            }
            NmeaReceiver.Receive(buffer);
        }, tokenSource.Token);
    }
    private void ConnectUDP()
    {
        try
        {
            //IPAddress.TryParse(DeviceSettings.IPAddress, out IPAddress address);
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, DeviceSettings.Port);
            //IPEndPoint endPoint = new IPEndPoint(address, DeviceSettings.Port);
            Port = new UdpClient(DeviceSettings.Port);

            Task.Run(ReceiveUDP, tokenSource.Token);
            tokenSource.Token.Register(() => (Port as UdpClient).Close());
        }
        catch (OperationCanceledException)
        {
        }
        catch (SocketException se)
        {
            MessageBox.Show($"Couldn't open UDP connection {DeviceSettings.IPAddress}:{DeviceSettings.Port}. Error Code: {se.ErrorCode}");
            //throw;
        }
        catch (Exception)
        {
            //throw;
        }

    }
    private async Task ReceiveUDP()
    {
        IPAddress.TryParse((string)DeviceSettings.IPAddress, out IPAddress address);
        while (true)
        {
            try
            {
                UdpReceiveResult result = await (Port as UdpClient).ReceiveAsync(tokenSource.Token);
                if (result.RemoteEndPoint.Address.Equals(address))  // UDP listens to the port, this filters the address. not sure if neccesery.
                {
                    NmeaReceiver.Receive(result.Buffer);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
    private async void ConnectTCP()
    {
        try
        {
            IPAddress.TryParse((string)DeviceSettings.IPAddress, out IPAddress address);
            IPEndPoint endPoint = new IPEndPoint(address, DeviceSettings.Port);
            Port = new TcpClient();
            await (Port as TcpClient).ConnectAsync(endPoint, tokenSource.Token);
            _ = Task.Run(ReceiveTCP, tokenSource.Token);
        }
        catch (SocketException se)
        {
            MessageBox.Show($"Couldn't open TCP connection {DeviceSettings.IPAddress}:{DeviceSettings.Port}. Error Code: {se.ErrorCode}");
            //throw;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
    private async Task ReceiveTCP()
    {
        using (TcpClient client = (Port as TcpClient))
        {
            byte[] buffer = new byte[client.ReceiveBufferSize];
            while (client.Connected)
            {
                try
                {
                    await client.GetStream().ReadAsync(buffer, tokenSource.Token);
                    NmeaReceiver.Receive(buffer);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}