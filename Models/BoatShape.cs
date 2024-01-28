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
    //public float Rotation { get; set; } = 0;
    public List<SKPoint> SKPoints { get; private set; } = new();
    [ObservableProperty]
    private Color fill = Colors.Orange;
    [ObservableProperty]
    private Color outline = Colors.Black;
    public SKPaint SKFill { get => new SKPaint { Color = Fill.ToSKColor() }; }
    public SKPaint SKOutline { get => new SKPaint { Color = Outline.ToSKColor() }; }
    public float Opacity { get; set; } = 0.7f;

    /// <summary>
    /// For Init only
    /// </summary>
    public BoatShape()
    {
        SKPoints = new() { new SKPoint(0, 0) };
    }
    public BoatShape(string path, Color fill, Color outline)
    {
        Path = path;
        ReadBoatShapeFile();
        ChangeColors(fill, outline);
    }
    private void ReadBoatShapeFile()
    {
        if (File.Exists(Path))
        {
            try
            {
                List<SKPoint> points = new();
                foreach (var line in File.ReadAllLines(Path))
                {
                    if (!line.Contains(' ')) continue;
                    //line.TrimStart().TrimEnd();
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    points.Add(new SKPoint(float.Parse(parts[0]), float.Parse(parts[1])));
                }
                SKPoints = new(points);
            }
            catch (Exception)
            {

            }
        }
        else
        {
            SKPoints = CreateMarker();
        }
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
        Fill = color;
    }
    public void ChangeOutline(System.Windows.Media.Color color)
    {
        Outline = color;
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

