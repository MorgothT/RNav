using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public double[] GetFontSizes
    {
        get
        {
            return new double[] {  12 ,  16 ,  20 ,  24 ,  30 ,  40  };
        }
    }
    [ObservableProperty]
    private DegreeFormat degreeFormat;

    [ObservableProperty]
    private ObservableCollection<Target> targetList;

    [ObservableProperty]
    private float targetRadius;


    public MapSettings() => GetMapSettings();
    
    public void InitializeMapSettings()
    {
        ProjectionList = new()
        {
            {"Israeli_Grid_05-12:6991" },
            {"WGS_1984_UTM_Zone_36N:32636" }
        };
        CurrentProjection = ProjectionList.First();
        ChartItems = new ObservableCollection<ChartItem>();
        BoatShape = new BoatShape();
        FontSize = 12;
        DegreeFormat = DegreeFormat.Deg;
        TargetList = new ObservableCollection<Target>();
        TargetRadius = 10;
        SaveMapSettings();
    }
    public MapSettings GetMapSettings()
    {
        //InitializeMapSettings();
        try
        {
            CurrentProjection = Properties.Map.Default.Projection;
            ProjectionList = JsonConvert.DeserializeObject<List<string>>(Properties.Map.Default.ProjectionList);
            ChartItems = JsonConvert.DeserializeObject<ObservableCollection<ChartItem>>(Properties.Map.Default.Layers);
            BoatShape = JsonConvert.DeserializeObject<BoatShape>(Properties.Map.Default.BoatShape);
            FontSize = Properties.Map.Default.FontSize;
            DegreeFormat = JsonConvert.DeserializeObject<DegreeFormat>(Properties.Map.Default.DegreeFormat);
            TargetList = JsonConvert.DeserializeObject<ObservableCollection<Target>>(Properties.Map.Default.TargetList);
            if (TargetList is null) TargetList = new ObservableCollection<Target>();
            try
            {
                TargetRadius = float.Parse(Properties.Map.Default.TargetRadius);
            }
            catch (Exception)
            {
                TargetRadius = 5f;
            }
            
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
        Properties.Map.Default.ProjectionList = JsonConvert.SerializeObject(ProjectionList);
        Properties.Map.Default.Layers = JsonConvert.SerializeObject(ChartItems);
        Properties.Map.Default.BoatShape = JsonConvert.SerializeObject(BoatShape);
        Properties.Map.Default.FontSize = FontSize;
        Properties.Map.Default.DegreeFormat = JsonConvert.SerializeObject(DegreeFormat);
        Properties.Map.Default.TargetList = JsonConvert.SerializeObject(TargetList);
        Properties.Map.Default.TargetRadius = TargetRadius.ToString();
        Properties.Map.Default.Save();
    }
}
