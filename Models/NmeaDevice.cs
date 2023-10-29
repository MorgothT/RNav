using InvernessPark.Utilities.NMEA;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WinRT;

namespace Mapper_v1.Models;

public class NmeaDevice
{
    public NmeaReceiver NmeaReceiver { get; }
    public NmeaHandler NmeaHandler { get; private set; }
    public DeviceSettings DeviceSettings { get; private set; }
    public object Port { get; private set; }

    private CancellationTokenSource TokenSource = new();

    public NmeaDevice(NmeaHandler handler, DeviceSettings settings)
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

    private void ConnectSerial()
    {
        try
        {
            Port = new SerialPort(DeviceSettings.ComPort, DeviceSettings.BaudRate);
            (Port as SerialPort).Open();
            (Port as SerialPort).DataReceived += DataReceived_Serial;

            TokenSource.Token.Register(() => (Port as SerialPort).Close());   // Registering Cancellation token for Closing port by connection type
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show($"Couldn't open {(Port as SerialPort).PortName}.{Environment.NewLine}Please check Settings page.");
        }
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
        }, TokenSource.Token);
    }

    private void ConnectUDP()
    {
        try
        {
            //IPAddress.TryParse(DeviceSettings.IPAddress, out IPAddress address);
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, DeviceSettings.Port);
            //IPEndPoint endPoint = new IPEndPoint(address, DeviceSettings.Port);
            Port = new UdpClient(DeviceSettings.Port);
            
            Task.Run(ReceiveUDP,TokenSource.Token);
            TokenSource.Token.Register(() => (Port as UdpClient).Close());
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
        IPAddress.TryParse(DeviceSettings.IPAddress, out IPAddress address);
        while (true)
        {
            try
            {
                UdpReceiveResult result = await (Port as UdpClient).ReceiveAsync(TokenSource.Token);
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
        IPAddress.TryParse(DeviceSettings.IPAddress, out IPAddress address);
        IPEndPoint endPoint = new IPEndPoint(address, DeviceSettings.Port);
        Port = new TcpClient();
        try
        {
            await (Port as TcpClient).ConnectAsync(endPoint, TokenSource.Token);
            _ = Task.Run(ReceiveTCP, TokenSource.Token);
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
                    await client.GetStream().ReadAsync(buffer, TokenSource.Token);
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

    public void Dispose()
    {
        TokenSource.Cancel();
    }
}