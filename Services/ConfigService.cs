using Mapper_v1.Contracts.Services;
using Mapper_v1.Models;
using System.IO;
using System.Text.Json;

namespace Mapper_v1.Services;

public class ConfigService : IConfigService
{
    private readonly string _folderPath;
    private readonly JsonSerializerOptions options = new JsonSerializerOptions()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    public MapSettings MapConfig { get; private set; }
    public MobileSettings MobileConfig { get; private set; }

    public ConfigService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RNav");
        Directory.CreateDirectory(folder);
        _folderPath = folder;

        Load();
    }

    private void Load()
    {
        // Load all configs one by one
        string mobilePath = Path.Combine(_folderPath, "Mobiles.ini");
        LoadMobileConfig(mobilePath);
        string mapPath = Path.Combine(_folderPath, "Config.ini");
        LoadMapConfig(mapPath);
    }
    public void LoadMobileConfig(string path)
    {
        if (File.Exists(path))
        {
            //var temp = new MobileSettings();
            //temp.LoadMobilesFromFile(path);
            //MobileConfig = temp;
            var json = File.ReadAllText(path);
            MobileConfig = JsonSerializer.Deserialize<MobileSettings>(json,options) ?? new MobileSettings();
        }
        else
        {
            MobileConfig = new MobileSettings();
        }
    }
    public void LoadMapConfig(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            MapConfig = JsonSerializer.Deserialize<MapSettings>(json,options) ?? new MapSettings();
        }
        else
        {
            MapConfig = new MapSettings();
        }
    }
    public void Save()
    {
        SaveMobiles(Path.Combine(_folderPath, "Mobiles.ini"));
        SaveConfig(Path.Combine(_folderPath, "Config.ini"));
    }
    public void SaveMobiles(string path)
    {
        var mobiles = JsonSerializer.Serialize(MobileConfig, options);
        File.WriteAllText(path, mobiles);
    }
    public void SaveConfig(string path)
    {
        var config = JsonSerializer.Serialize(MapConfig, options);
        File.WriteAllText(path, config);
    }
}
