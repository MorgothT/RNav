using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Projections;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Mapper_v1.Models;

public partial class ChartItem : ObservableObject
{
    [ObservableProperty]
    private bool enabled;
    [ObservableProperty]
    private double opacity;
    [ObservableProperty]
    private Color lineColor;
    [ObservableProperty]
    private Color outlineColor;
    [ObservableProperty]
    private Color fillColor;
    [ObservableProperty]
    private int lineWidth;

    [ObservableProperty]
    private int labelFontSize;
    [ObservableProperty]
    private Color haloColor;
    [ObservableProperty]
    private Color labelColor;
    [ObservableProperty]
    private Color backroundColor;
    [ObservableProperty]
    private string labelAttributeName;
    [ObservableProperty]
    private VerticalAlignmentEnum verticalAlignment;
    [ObservableProperty]
    private HorizontalAlignmentEnum horizontalAlignment;


    [ObservableProperty]
    private string projection;
    public static List<string> Projections 
    { 
        get
        {
            return ProjectProjections.GetProjections();
        } 
    }
    public string Name { get; private set; }
    public string Path { get; private set; }

    public ChartType ChartType { get; }

    public ChartItem(string path, Color? linecolor = null, Color? outlinecolor = null, Color? fillcolor = null, int width = 1)
    {
        Enabled = true;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Path = path;
        //Projections = ProjectProjections.GetProjections();
        //Projection = ProjectProjections.ITM;
        Opacity = 1;
        LineColor = linecolor ?? Colors.Black;
        OutlineColor = outlinecolor ?? Colors.Black;
        FillColor = fillcolor ?? Colors.Transparent;
        LineWidth = width;
        //Label settings defaults
        LabelColor = Colors.Transparent;
        HaloColor = Colors.Transparent;
        BackroundColor = Colors.Transparent;
        LabelFontSize = 12;
        LabelAttributeName = "NAME";
        HorizontalAlignment = HorizontalAlignmentEnum.Center;
        VerticalAlignment = VerticalAlignmentEnum.Center;

        switch (System.IO.Path.GetExtension(path).ToLowerInvariant())
        {
            case ".shp":
                ChartType = ChartType.Shapefile;
                break;
            case ".tif":
                ChartType = ChartType.Geotiff;
                break;
            default:
                ChartType = ChartType.Unsupported;
                throw new FormatException();
        }
    }
}

public enum ChartType
{
    Shapefile,
    Geotiff,
    Unsupported = -1
}

public enum HorizontalAlignmentEnum
{
    Left,
    Center,
    Right
}

public enum VerticalAlignmentEnum
{
    Top,
    Center,
    Bottom
}