using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Core.Contracts;
using Mapper_v1.Core.Contracts.Services;
using Mapper_v1.Core.Models.Controllers;
using System.Collections.ObjectModel;
using System.Text;

namespace Mapper_v1.ViewModels;

public partial class TerminalViewModel : ObservableObject
{
    private readonly IDeviceConfig _deviceConfig;
    private readonly IConnectionController _deviceController;
    private ControllerFactory ControllerFactory = new();
    private readonly Queue<string> _buffer = new();
    private const int MaxBufferLines = 300;

    [ObservableProperty]
    private ObservableCollection<string> terminalText = new();
    [ObservableProperty]
    private string connectionStatus = string.Empty;
    [ObservableProperty]
    private string terminalOutput = string.Empty;

    public bool IsConnected => _deviceController.IsConnected;


    public TerminalViewModel(IDeviceConfig deviceConfig) 
    {
        _deviceConfig = deviceConfig;
        if (deviceConfig is null)
        {
            ConnectionStatus = "Device Config is null";
            //IsConnected = false;
        }
        _deviceController = ControllerFactory.CreateController(_deviceConfig.PortConfig);
        _deviceController.StatusChanged += OnStatusChanged;
        _deviceController.DataReceived += OnDataReceived;
        _deviceController.ConnectAsync();
    }


    private void OnDataReceived(object sender, byte[] e)
    {
        // Append received data as string to the terminal text collection
        var textblock = Encoding.UTF8.GetString(e).Split(Environment.NewLine);
        foreach (string line in textblock.Where(s => string.IsNullOrEmpty(s) == false))
        {
            var text = $"[{DateTime.Now:HH:mm:ss}] {line}";
            //App.Current.Dispatcher.Invoke(() =>
            //    {
            //        TerminalText.Add(text);
            //        if (TerminalText.Count > 1000)
            //        {
            //            TerminalText.RemoveAt(0);
            //        }
            //    });
            _buffer.Enqueue(text);
            while (_buffer.Count > MaxBufferLines)
            {
                _buffer.Dequeue();
            }
            var sb = new StringBuilder();
            foreach (var item in _buffer)
            {
                sb.AppendLine(item);
            }
            TerminalOutput = sb.ToString();
        }
    }

    public async Task DisconnectAsync()
    {
        _deviceController.StatusChanged -= OnStatusChanged;
        _deviceController.DataReceived -= OnDataReceived;
        await _deviceController.DisconnectAsync();
    }

    private void OnStatusChanged(object sender, string e)
    {
        ConnectionStatus = e;
    }
}
