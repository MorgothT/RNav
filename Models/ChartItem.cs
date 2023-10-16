using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mapper_v1.Models;

public partial class ChartItem : ObservableObject
{
    [ObservableProperty]
    public bool enabled;
    [ObservableProperty]
    public double opacity;
    [ObservableProperty]
    public Color lineColor;
    [ObservableProperty]
    public Color outlineColor;
    [ObservableProperty]
    private Color fillColor;
    [ObservableProperty]
    private int lineWidth;

    public string Name { get; private set; }
    public string Path { get; private set; }
    
    public ChartType ChartType {get;}

    public ChartItem(string path, Color? linecolor = null, Color? outlinecolor = null, Color? fillcolor = null, int width = 1)
    {
        Enabled = true;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Path = path;
        Opacity = 1;
        LineColor = linecolor ?? Colors.Black;
        OutlineColor = outlinecolor ?? Colors.Black;
        FillColor = fillcolor ?? Colors.Orange;
        LineWidth = width;
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
