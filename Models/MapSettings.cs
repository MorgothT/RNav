using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Core;
using Mapper_v1.Projections;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Mapper_v1.Models;

public partial class MapSettings : ObservableObject
{

    [ObservableProperty]
    private List<string> projectionList;
    [ObservableProperty]
    private string currentProjection;
    [ObservableProperty]
    private PointSettings pointSettings = new();
    [ObservableProperty]
    private double fontSize;
    [ObservableProperty]
    private DegreeFormat degreeFormat;
    [ObservableProperty]
    private ushort trailDuration;
    [ObservableProperty]
    private string logDirectory;
    
    // Data persistance
    [ObservableProperty]
    private ObservableCollection<ChartItem> chartItems;
    [ObservableProperty]
    private ObservableCollection<Target> targetList;
    [ObservableProperty]
    private List<TimedPoint> lastTrail;
    [ObservableProperty]
    private int selectedTargetId;
    
    // App Settings in other screens
    [ObservableProperty]
    private float targetRadius = 5;
    [ObservableProperty]
    private bool mapOverlay;
    [ObservableProperty]
    private bool showTargets = true;

    private string defaulName = "Settings.ini";
    private JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public double[] GetFontSizes
    {
        get
        {
            return new double[] { 12, 16, 20, 24, 30, 40 };
        }
    }

    //public MapSettings() => GetMapSettings();
    public MapSettings()
    {

    }
    public MapSettings GetMapSettings(string path = null)
    {
        if (path == null)
        {
            path = defaulName;
        }
        try
        {
            if (!File.Exists(path))
            {
                MapSettings settings = new MapSettings().InitializeDefaults();
                settings.SaveMapSettings();
                return settings;
            }
            var json = File.ReadAllText(path);
            MapSettings current = JsonSerializer.Deserialize<MapSettings>(json,jsonSerializerOptions);
            return current;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings from {path} - {ex.Message}");
            MapSettings defaultSettings = InitializeDefaults();
            defaultSettings.SaveMapSettings();
            return defaultSettings;
        }
    }

    private MapSettings InitializeDefaults()
    {
        MapSettings defaultSettings = this;
        defaultSettings.ProjectionList = ProjectProjections.GetProjections();
        defaultSettings.CurrentProjection = defaultSettings?.ProjectionList?.FirstOrDefault();
        defaultSettings.ChartItems = [];
        defaultSettings.FontSize = 12;
        defaultSettings.DegreeFormat = DegreeFormat.Deg;
        defaultSettings.TargetList = [];
        defaultSettings.TargetRadius = 10;
        defaultSettings.TrailDuration = 0;
        defaultSettings.LastTrail = [];
        defaultSettings.LogDirectory = @"C:\RNav\Logs";
        defaultSettings.MapOverlay = false;
        defaultSettings.SelectedTargetId = -1;
        defaultSettings.PointSettings = new PointSettings()
        {
            Color = Colors.Red,
            IsAbsoluteUnits = false,
            Shape = PointShape.CircleCross,
            Size = 10
        };
        return defaultSettings;
    }
    public bool SaveMapSettings(string path = null)
    {
        if(path == null)
        {
            path = defaulName;
        }
        try
        {
            var json = JsonSerializer.Serialize<MapSettings>(this, jsonSerializerOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }

    public void InitializeMapSettings()
    {
        ProjectionList = ProjectProjections.GetProjections();
        CurrentProjection = ProjectionList.First();
        ChartItems = new ObservableCollection<ChartItem>();
        //BoatShape = new BoatShape();
        FontSize = 12;
        DegreeFormat = DegreeFormat.Deg;
        TargetList = new ObservableCollection<Target>();
        TargetRadius = 10;
        TrailDuration = 0;
        LogDirectory = @"C:\RNav\Logs";
        MapOverlay = false;
        SelectedTargetId = -1;
        SaveMapSettings();
    }
    //public MapSettings GetMapSettings()
    //{
    //    try
    //    {
    //        ProjectionList = ProjectProjections.GetProjections();
    //        CurrentProjection = Properties.Map.Default.Projection ?? ProjectionList.First();
    //        if (ProjectionList.Contains(CurrentProjection) == false)
    //            CurrentProjection = ProjectionList[0];
    //        ChartItems = JsonConvert.DeserializeObject<ObservableCollection<ChartItem>>(Properties.Map.Default.Layers) ?? [];
    //        //BoatShape = JsonConvert.DeserializeObject<BoatShape>(Properties.Map.Default.BoatShape)?? new();
    //        FontSize = Properties.Map.Default.FontSize;
    //        DegreeFormat = JsonConvert.DeserializeObject<DegreeFormat>(Properties.Map.Default.DegreeFormat);
    //        TargetList = JsonConvert.DeserializeObject<ObservableCollection<Target>>(Properties.Map.Default.TargetList) ?? [];
    //        if (TargetList is null) TargetList = new ObservableCollection<Target>();
    //        TargetRadius = Properties.Map.Default.TargetRadius;
    //        //HeadingOffset = Properties.Map.Default.HeadingOffset;
    //        //DepthOffset = Properties.Map.Default.DepthOffset;
    //        //PositionOffset = Properties.Map.Default.PositionOffset;
    //        TrailDuration = Properties.Map.Default.TrailDuration;
    //        LogDirectory = Properties.Map.Default.LogDirectory;
    //        MapOverlay = Properties.Map.Default.MapOverlay;
    //        ShowTargets = Properties.Map.Default.ShowTargets;
    //        SelectedTargetId = Properties.Map.Default.LastTargetId;
    //        PointSettings = JsonConvert.DeserializeObject<PointSettings>(Properties.Map.Default.PointSettings) ?? new PointSettings();
    //        if (ProjectionList is null) InitializeMapSettings();
    //    }
    //    catch (Exception)
    //    {
    //        InitializeMapSettings();
    //    }
    //    return this;
    //}
    //public void SaveMapSettings()
    //{
    //    Properties.Map.Default.Projection = CurrentProjection;
    //    Properties.Map.Default.Layers = JsonConvert.SerializeObject(ChartItems);
    //    //Properties.Map.Default.BoatShape = JsonConvert.SerializeObject(BoatShape);
    //    Properties.Map.Default.FontSize = FontSize;
    //    Properties.Map.Default.DegreeFormat = JsonConvert.SerializeObject(DegreeFormat);
    //    Properties.Map.Default.TargetList = JsonConvert.SerializeObject(TargetList);
    //    Properties.Map.Default.TargetRadius = TargetRadius;
    //    //Properties.Map.Default.HeadingOffset = HeadingOffset;
    //    //Properties.Map.Default.DepthOffset = DepthOffset;
    //    //Properties.Map.Default.PositionOffset = PositionOffset;
    //    Properties.Map.Default.TrailDuration = TrailDuration;
    //    Properties.Map.Default.LogDirectory = LogDirectory;
    //    Properties.Map.Default.MapOverlay = MapOverlay;
    //    Properties.Map.Default.ShowTargets = ShowTargets;
    //    Properties.Map.Default.LastTargetId = SelectedTargetId;
    //    Properties.Map.Default.PointSettings = JsonConvert.SerializeObject(PointSettings);
    //    Properties.Map.Default.Save();
    //}
    //public List<TimedPoint> GetTrail()
    //{
    //    return JsonConvert.DeserializeObject<List<TimedPoint>>(Properties.Map.Default.LastTrail);
    //}
    //public void SaveTrail(List<TimedPoint> myTrail)
    //{
    //    Properties.Map.Default.LastTrail = JsonConvert.SerializeObject(myTrail);
    //}
    public void ExportCharts()
    {
        string charts = JsonSerializer.Serialize(ChartItems);
        var sfd = new SaveFileDialog()
        {
            AddExtension = true,
            AddToRecent = true,
            Filter = "Chart collection (*.chc)| *.chc",
            InitialDirectory = @"C:\RNav",
            OverwritePrompt = true
        };
        if (sfd.ShowDialog() == true)
        {
            File.WriteAllText(sfd.FileName, charts);
        }
    }
    public void ImportCharts()
    {
        var ofd = new OpenFileDialog()
        {
            Filter = "Chart collection (*.chc)| *.chc",
            InitialDirectory = @"C:\RNav",
            CheckFileExists = true,
            Multiselect = false,
        };
        if (ofd.ShowDialog() == true)
        {
            //TODO: add option to append imported charts to current list
            string charts = File.ReadAllText(ofd.FileName);
            try
            {
                ChartItems = JsonSerializer.Deserialize<ObservableCollection<ChartItem>>(charts);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading charts collection.\n{ex.Message}");
            }
        }
    }
}
