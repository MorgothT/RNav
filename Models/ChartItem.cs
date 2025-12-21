using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Projections;
using System.Text.Json.Serialization;
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
    private double maxResulotion;


    [ObservableProperty]
    private string projection;
    
    public static List<string> Projections
    {
        get
        {
            return ProjectProjections.GetProjections();
        }
    }
    //public string Name { get; private set; }
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
    
    [JsonInclude]
    public string Path { get; private set; }
    [JsonInclude]
    public ChartType ChartType { get; private set; }

    public ChartItem() { }
    public ChartItem(string path,
                     Color? lineColor = null,
                     Color? outlineColor = null,
                     Color? fillColor = null,
                     int lineWidth = 1)
    {
        Enabled = true;
        Path = path;
        //Name = System.IO.Path.GetFileNameWithoutExtension(Path);
        Opacity = 1;
        LineColor = lineColor ?? Colors.Black;
        OutlineColor = outlineColor ?? Colors.Black;
        FillColor = fillColor ?? Colors.Transparent;
        LineWidth = lineWidth;
        //Label settings defaults
        LabelColor = Colors.Transparent;
        HaloColor = Colors.Transparent;
        BackroundColor = Colors.Transparent;
        LabelFontSize = 12;
        LabelAttributeName = "NAME";
        HorizontalAlignment = HorizontalAlignmentEnum.Center;
        VerticalAlignment = VerticalAlignmentEnum.Center;
        MaxResulotion = 0.2;
        ChartType = GetChartType(path);
        if (ChartType == ChartType.Unsupported) throw new FormatException();
    }

    private ChartType GetChartType(string path)
    {
        return System.IO.Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".shp" => ChartType.Shapefile,
            ".tif" => ChartType.Geotiff,
            ".ecw" => ChartType.Ecw,
            ".dxf" => ChartType.Dxf,
            _ => ChartType.Unsupported,
        };
    }
    //    {
    //        switch (System.IO.Path.GetExtension(path).ToLowerInvariant()) return{
    //            ".shp" => ChartType.Shapefile;
    //        }

    //            //{
    //        //    case ".shp":
    //        //        return = ChartType.Shapefile;
    //        //        break;
    //        //    case ".tif":
    //        //        ChartType = ChartType.Geotiff;
    //        //        break;
    //        //    case ".ecw":
    //        //        ChartType = ChartType.Ecw;
    //        //        break;
    //        //    case ".dxf":
    //        //        ChartType = ChartType.Dxf;
    //        //        break;
    //        //    default:
    //        //        ChartType = ChartType.Unsupported;
    //        //        throw new FormatException();
    //        //}
    //    }
}

public enum ChartType
{
    Shapefile,
    Geotiff,
    Dxf,
    Ecw,
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