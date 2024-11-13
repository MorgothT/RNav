using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.IO;
using System.Windows.Media;

namespace Mapper_v1.Models;

public partial class BoatShape : ObservableObject
{
    [ObservableProperty]
    private string path;
    [ObservableProperty]
    private Color fillColor = Colors.Orange;
    [ObservableProperty]
    private Color outlineColor = Colors.Black;
    
    public List<SKPoint> VesselOutline { get; private set; } = new();
    public List<VesselAnchor> VesselAnchors { get; private set; } = new();
    public List<VesselArc> VesselObjects { get; private set; } = new();

    public SKPaint SKFillColor { get => new() { Color = FillColor.ToSKColor() }; }
    public SKPaint SKOutlineColor { get => new() { Color = OutlineColor.ToSKColor() }; }
    public float Opacity { get; set; } = 0.7f;

    /// <summary>
    /// For Init only
    /// </summary>
    public BoatShape()
    {
        VesselOutline = new() { new SKPoint(0, 0) };
        VesselAnchors = [];
        VesselObjects = [];
    }
    public BoatShape(string path, Color fill, Color outline)
    {
        Path = path;
        ReadBoatShapeFile();
        ChangeColors(fill, outline);
    }
    private void ReadBoatShapeFile()
    {
        VesselOutline = [];
        VesselObjects = [];
        VesselAnchors = [];
        if (File.Exists(Path))
        {
            try
            {
                List<SKPoint> points = new();
                foreach (var line in File.ReadAllLines(Path))
                {
                    if (!line.Contains(' ')) continue;
                    if (line.StartsWith("ANC")) 
                    { 
                        AddAnchor(line);
                        continue;
                    }
                    if (line.StartsWith("ARC")) 
                    {
                        AddArc(line);
                        continue;
                    }
                    else
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        points.Add(new SKPoint(float.Parse(parts[0]), float.Parse(parts[1])));
                    }
                }
                if (points.Last() != points.First()) points.Add(points.First());
                VesselOutline = new(points);
            }
            catch (Exception)
            {

            }
        }
        else
        {
            VesselOutline = CreateMarker();
        }
    }

    private void AddArc(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 6) return;
        VesselArc arc = new(double.Parse(parts[1]), double.Parse(parts[2]), double.Parse(parts[3]), double.Parse(parts[4]), double.Parse(parts[5]));
        VesselObjects.Add(arc);
    }

    private void AddAnchor(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4) return;
        VesselAnchor anchor = new(double.Parse(parts[1]), double.Parse(parts[2]), parts[3].Trim('"'));
        VesselAnchors.Add(anchor);
    }

    public List<SKPoint> CreateMarker()
    {
        //Default shape "ECDIS"
        return new List<SKPoint>()
        {
            new SKPoint(-1, 0),
            new SKPoint(1, 0),
            new SKPoint(0, 0),
            new SKPoint(0, -1),
            new SKPoint(0, 2),
            new SKPoint(0, 0.5f),
            new SKPoint(0.5f,0),
            new SKPoint(0, -0.5f),
            new SKPoint(-0.5f,0),
            new SKPoint(0, 0.5f),
            new SKPoint(0, 0)
        };
    }
    public void Refresh()
    {
        ReadBoatShapeFile();
    }
    public void ChangeFill(System.Windows.Media.Color color)
    {
        FillColor = color;
    }
    public void ChangeOutline(System.Windows.Media.Color color)
    {
        OutlineColor = color;
    }
    public void ChangeColors(System.Windows.Media.Color fill, System.Windows.Media.Color outline)
    {
        ChangeFill(fill);
        ChangeOutline(outline);
    }
    public override string ToString()
    {
        return this.Path;
    }
}

// move to proper place
public class VesselArc(double p1X = 0, double p1Y = 0, double p2X = 0, double p2Y = 0, double radius = 0)
{
    public double p1X = p1X;
    public double p1Y = p1Y;
    public double p2X = p2X;
    public double p2Y = p2Y;
    public double radius = radius;
}
public class VesselAnchor(double x = 0, double y = 0, string name = "" )
{
    public double X = x;
    public double Y = y;
    public string Name = name;
}