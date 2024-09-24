using CommunityToolkit.Mvvm.ComponentModel;
using GeoConverter;
using Mapper_v1.Helpers;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Styles;
using static GeoConverter.Converter;

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

    public override string ToString()   //for export
    {
        return $"{Id},{Name},{X},{Y},{Lat},{Lon}";
    }
    public static Target Parse(string line)
    {
        try
        {
            string[] fields = line.Split(',');
            Target target = new()
            {
                Id = int.Parse(fields[0]),
                Name = fields[1],
                X = double.Parse(fields[2]),
                Y = double.Parse(fields[3]),
                Lat = double.Parse(fields[4]),
                Lon = double.Parse(fields[5])
            };
            return target;
        }
        catch (Exception)
        {
            return null;
        }

    }
    public static Target CreateTarget(MPoint point, int id, MPoint wgsPoint)
    {
        Target target = new()
        {
            Id = id,
            Name = $"Target no.{id}",
            X = point.X,
            Y = point.Y,
            Lat = wgsPoint.Y,
            Lon = wgsPoint.X
        };
        return target;
    }
    public static Target CreateTarget(MPoint point, int id, Converter converter)
    {
        Point3d latlon = converter.Convert(new Point3d(point.X, point.Y, 0));
        Target target = new()
        {
            Id = id,
            Name = $"Target no.{id}",
            X = point.X,
            Y = point.Y,
            Lat = latlon.Y,
            Lon = latlon.X
        };
        return target;
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
    /// <summary>
    /// Target is in Project CRS
    /// </summary>
    /// <param name="target"></param>
    /// <param name="radius"></param>
    /// <param name="degreeFormat"></param>
    /// <returns></returns>
    public static PointFeature CreateTargetFeature(Target target, float radius, DegreeFormat degreeFormat)
    {
        var feature = new PointFeature(target.X, target.Y);
        feature[nameof(Id)] = target.Id;
        feature[nameof(Name)] = target.Name;
        feature[nameof(X)] = target.X.ToString("F2");
        feature[nameof(Y)] = target.Y.ToString("F2");
        feature[nameof(Lat)] = Formater.FormatLatLong(target.Lat, degreeFormat);
        feature[nameof(Lon)] = Formater.FormatLatLong(target.Lon, degreeFormat);
        feature.Styles.Add(CreateTargetCalloutStyle(feature.ToStringOfKeyValuePairs(), radius));
        feature["IsSelected"] = false;
        return feature;
    }
    public static Target CreateTargetFromTargetFeature(IFeature feature)
    {
        var target = new Target();
        if (feature is not null)
        {
            target.Id = (int)feature["Id"];
            target.Name = (string)feature["Name"];
            target.X = double.Parse(feature["X"].ToString());
            target.Y = double.Parse(feature["Y"].ToString());
        }
        return target;
    }
}
