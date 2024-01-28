using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Layers;
using Mapper_v1.Projections;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace Mapper_v1.Models;

public partial class MapSettings : ObservableObject
{

    [ObservableProperty]
    private string currentProjection;
    [ObservableProperty]
    private List<string> projectionList;
    [ObservableProperty]
    private ObservableCollection<ChartItem> chartItems;
    [ObservableProperty]
    private BoatShape boatShape;
    [ObservableProperty]
    private double fontSize;
    [ObservableProperty]
    private DegreeFormat degreeFormat;
    [ObservableProperty]
    private ObservableCollection<Target> targetList;
    [ObservableProperty]
    private float targetRadius = 5;
    [ObservableProperty]
    private double headingOffset = 0;
    [ObservableProperty]
    private ushort trailDuration;
    [ObservableProperty]
    private string logDirectory;
    [ObservableProperty]
    private List<TimedPoint> lastTrail;
    [ObservableProperty]
    private bool mapOverlay;

    public double[] GetFontSizes
    {
        get
        {
            return new double[] { 12, 16, 20, 24, 30, 40 };
        }
    }

    public MapSettings() => GetMapSettings();

    public void InitializeMapSettings()
    {
        ProjectionList = ProjectProjections.GetProjections();
        CurrentProjection = ProjectionList.First();
        ChartItems = new ObservableCollection<ChartItem>();
        BoatShape = new BoatShape();
        FontSize = 12;
        DegreeFormat = DegreeFormat.Deg;
        TargetList = new ObservableCollection<Target>();
        TargetRadius = 10;
        TrailDuration = 0;
        LogDirectory = @"C:\RNav\Logs";
        MapOverlay = false;
        SaveMapSettings();
    }
    public MapSettings GetMapSettings()
    {
        try
        {
            CurrentProjection = Properties.Map.Default.Projection;
            ProjectionList = ProjectProjections.GetProjections();
            ChartItems = JsonConvert.DeserializeObject<ObservableCollection<ChartItem>>(Properties.Map.Default.Layers);
            BoatShape = JsonConvert.DeserializeObject<BoatShape>(Properties.Map.Default.BoatShape);
            FontSize = Properties.Map.Default.FontSize;
            DegreeFormat = JsonConvert.DeserializeObject<DegreeFormat>(Properties.Map.Default.DegreeFormat);
            TargetList = JsonConvert.DeserializeObject<ObservableCollection<Target>>(Properties.Map.Default.TargetList);
            if (TargetList is null) TargetList = new ObservableCollection<Target>();
            TargetRadius = Properties.Map.Default.TargetRadius;
            HeadingOffset = Properties.Map.Default.HeadingOffset;
            TrailDuration = Properties.Map.Default.TrailDuration;
            LogDirectory = Properties.Map.Default.LogDirectory;
            MapOverlay = Properties.Map.Default.MapOverlay;
            if (ProjectionList is null) InitializeMapSettings();
        }
        catch (Exception)
        {
            InitializeMapSettings();
        }
        return this;
    }
    public void SaveMapSettings()
    {
        Properties.Map.Default.Projection = CurrentProjection;
        Properties.Map.Default.Layers = JsonConvert.SerializeObject(ChartItems);
        Properties.Map.Default.BoatShape = JsonConvert.SerializeObject(BoatShape);
        Properties.Map.Default.FontSize = FontSize;
        Properties.Map.Default.DegreeFormat = JsonConvert.SerializeObject(DegreeFormat);
        Properties.Map.Default.TargetList = JsonConvert.SerializeObject(TargetList);
        Properties.Map.Default.TargetRadius = TargetRadius;
        Properties.Map.Default.HeadingOffset = HeadingOffset;
        Properties.Map.Default.TrailDuration = TrailDuration;
        Properties.Map.Default.LogDirectory = LogDirectory;
        Properties.Map.Default.MapOverlay = MapOverlay;

        Properties.Map.Default.Save();
    }
    public List<TimedPoint> GetTrail()
    {
        return JsonConvert.DeserializeObject<List<TimedPoint>>(Properties.Map.Default.LastTrail);
    }
    public void SaveTrail(List<TimedPoint> myTrail)
    {
        Properties.Map.Default.LastTrail = JsonConvert.SerializeObject(myTrail);
    }
}
