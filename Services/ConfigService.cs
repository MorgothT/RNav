using Mapper_v1.Contracts.Services;
using Mapper_v1.Core;
using Mapper_v1.Core.Contracts.Services;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Services;
using Mapper_v1.Models;
using Mapper_v1.Projections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Mapper_v1.Services;

public class ConfigService : IConfigService
{
    private readonly string _settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RNav");
    private readonly string _mapSettingsFilename = "Settings.ini";
    private readonly string _mobileFilename = "Mobiles.ini";
    private readonly IFileService _fileService;
    public MapSettings MapConfig { get; set; } = new();
    public MobileSettings MobileConfig { get; set; } = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    public ConfigService(IFileService fileService)
    {
        _fileService = fileService;
    }
    public async Task InitializeAsync()
    {
        // 1. Load Map Settings Async
        string mapSettingsPath = Path.Combine(_settingsFolder, _mapSettingsFilename);
        if (File.Exists(mapSettingsPath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(mapSettingsPath);
                MapConfig = JsonSerializer.Deserialize<MapSettings>(json, _jsonOptions) ?? CreateDefaultMapSettings();
            }
            catch
            {
                MapConfig = CreateDefaultMapSettings();
            }
        }
        else
        {
            MapConfig = CreateDefaultMapSettings();
            await SaveMapSettingsAsync();
        }
        MapConfig.ProjectionList = ProjectProjections.GetProjections();

        // 2. Load Mobile Settings Async
        string mobileSettingsPath = Path.Combine(_settingsFolder, _mobileFilename);
        if (File.Exists(mobileSettingsPath))
        {
            try
            {
                var mobiles = await Task.Run(() => _fileService.Read<ObservableCollection<Mobile>>(_settingsFolder, _mobileFilename));
                //string json = await File.ReadAllTextAsync(mobileSettingsPath);
                //var mobiles = JsonSerializer.Deserialize<ObservableCollection<Mobile>>(json, _jsonOptions);
                MobileConfig = new MobileSettings { Mobiles = mobiles ?? [] };
            }
            catch
            {
                MobileConfig = CreateDefaultMobileSettings();
                await SaveMobileSettingsAsync();
            }
        }
        else
        {
            MobileConfig = CreateDefaultMobileSettings();
            await SaveMobileSettingsAsync();
        }
    }

    public async Task SaveMapSettingsAsync()
    {
        try
        {
            Directory.CreateDirectory(_settingsFolder);
            string fullPath = Path.Combine(_settingsFolder, _mapSettingsFilename);
            string json = JsonSerializer.Serialize(MapConfig, _jsonOptions);
            await File.WriteAllTextAsync(fullPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save map settings: {ex.Message}");
        }
    }
    public async Task SaveMobileSettingsAsync()
    {
        try
        {
            ValidatePrimaryMobile();

            // Offload the synchronous FileService save to a background thread.
            // This ensures instant page navigation even during a disk-write!
            await Task.Run(() => _fileService.Save(_settingsFolder, _mobileFilename, MobileConfig.Mobiles));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save mobile settings: {ex.Message}");
        }
    }
    public async Task SaveMobilesToFileAsync(string path)
    {
        string filename = Path.GetFileName(path);
        string folder = Path.GetDirectoryName(path);
        try
        {
            ValidatePrimaryMobile();

            // Offload the synchronous FileService save to a background thread.
            // This ensures instant page navigation even during a disk-write!
            await Task.Run(() => _fileService.Save(folder, filename, MobileConfig.Mobiles));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save mobile settings: {ex.Message}");
        }
    }
    //public async Task SaveMobileSettingsAsync()
    //{
    //    try
    //    {
    //        ValidatePrimaryMobile();
    //        Directory.CreateDirectory(_settingsFolder);
    //        string fullPath = Path.Combine(_settingsFolder, _mobileFilename);

    //        string json = JsonSerializer.Serialize(MobileConfig.Mobiles, _jsonOptions);
    //        await File.WriteAllTextAsync(fullPath, json);
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Diagnostics.Debug.WriteLine($"Failed to save mobile settings: {ex.Message}");
    //    }
    //}
    public async Task LoadMapSettingsFromFileAsync(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                string json = await File.ReadAllTextAsync(path);
                MapConfig = JsonSerializer.Deserialize<MapSettings>(json, _jsonOptions) ?? CreateDefaultMapSettings();
                MapConfig.ProjectionList = ProjectProjections.GetProjections();
            }
            catch
            {
                //MapConfig = CreateDefaultMapSettings();
                MessageBox.Show("Parsing settings falied, please contact support.", "Settings Import Failed",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
        }
        else
        {
            MessageBox.Show("File not found, please contact support.", "Settings Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
    }
    public async Task LoadMobilesFromFileAsync(string path)
    {
        string filename = Path.GetFileName(path);
        string folder = Path.GetDirectoryName(path);
        try
        {
            var mobiles = await Task.Run(() => _fileService.Read<ObservableCollection<Mobile>>(folder, filename));
            //string json = await File.ReadAllTextAsync(mobileSettingsPath);
            //var mobiles = JsonSerializer.Deserialize<ObservableCollection<Mobile>>(json, _jsonOptions);
            MobileConfig = new MobileSettings { Mobiles = mobiles ?? [] };
        }
        catch
        {
            return;
        }
    }
    
    private void ValidatePrimaryMobile()
    {
        if (MobileConfig?.Mobiles == null) return;
        bool hasPrimary = false;
        foreach (var mobile in MobileConfig.Mobiles)
        {
            if (mobile.IsPrimery)
            {
                if (!hasPrimary)
                {
                    hasPrimary = true;
                }
                else
                {
                    mobile.IsPrimery = false;
                }
            }
        }
    }

    private MapSettings CreateDefaultMapSettings()
    {
        var defaults = new MapSettings
        {
            FontSize = 12,
            DegreeFormat = DegreeFormat.Deg,
            TrailDuration = 0,
            LogDirectory = @"C:\RNav\Logs",
            MapOverlay = false,
            ShowTargets = true,
            SelectedTargetId = -1,
            ChartItems = [],
            TargetList = [],
            LastTrail = [],
            PointSettings = new() { Color = System.Windows.Media.Colors.Red, Size = 10 },
            TargetSettings = new() { TargetRadius = 10, TargetColor = System.Windows.Media.Colors.Green, TargetOpacity = 0.25f }
        };
        return defaults;
    }

    private MobileSettings CreateDefaultMobileSettings()
    {
        var defaultMobile = new Mobile
        {
            Name = "boaty",
            IsPrimery = true,
            Devices = [],
            DeviceConfigs = []
        };
        return new MobileSettings { Mobiles = [defaultMobile] };
    }
}