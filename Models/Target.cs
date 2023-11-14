using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Styles;
using System.Windows;

namespace Mapper_v1.Models;

public partial class Target : ObservableObject
{

    [ObservableProperty]
    private int id;
    [ObservableProperty]
	private string name = "";
	[ObservableProperty]
    private double x;
    [ObservableProperty]
    private double y;
    [ObservableProperty]
    private double lat;
    [ObservableProperty]
    private double lon;

    /// <summary>
    /// Target point (unaware of EPSG !)
    /// </summary>
    public Target()
    {
    }

    public override string ToString()
    {
        return $"{Id},{Name},{X},{Y},{Lat},{Lon}";    
    }

    public static CalloutStyle CreateTargetCalloutStyle(string content, float radius)
    {
        return new CalloutStyle
        {
            Title = content,
            TitleFont = { FontFamily = null, Size = 12, Italic = false, Bold = true },
            TitleFontColor = Color.Gray,
            MaxWidth = 150,
            RectRadius = 10,
            ShadowWidth = 4,
            Enabled = false,
            SymbolOffset = new Offset(0, radius)
        };
    }

    public static PointFeature CreateTargetFeature(Target target,float radius)
    {
        var feature = new PointFeature(target.X, target.Y);
        feature[nameof(Id)] = target.Id;
        feature[nameof(Name)] = target.Name;
        feature[nameof(X)] = target.X.ToString("F2");
        feature[nameof(Y)] = target.Y.ToString("F2"); 
        feature[nameof(Lat)] = target.Lat.ToString("F8"); //TODO: Format with project settings
        feature[nameof(Lon)] = target.Lon.ToString("F8");
        feature.Styles.Add(CreateTargetCalloutStyle(feature.ToStringOfKeyValuePairs(), radius));
        return feature;
    }
}
