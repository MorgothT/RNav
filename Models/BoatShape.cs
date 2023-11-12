using BruTile.Wms;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.Extensions;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using NetTopologySuite.Geometries.Prepared;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.Media.Audio;

namespace Mapper_v1.Models;

public partial class BoatShape : ObservableObject
{

    [ObservableProperty]
    private string path;
    //public float Rotation { get; set; } = 0;
    public List<SKPoint> SKPoints { get; private set; } = new();

    [ObservableProperty]
    private System.Windows.Media.Color fill = Colors.Orange;
    [ObservableProperty]
    private System.Windows.Media.Color outline = Colors.Black;
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
    public BoatShape(string path, System.Windows.Media.Color fill, System.Windows.Media.Color outline)
    {
        ReadBoatShapeFile();
        ChangeColors(fill, outline);
    }
    //public BoatShape(string path, Mapsui.Styles.Color fill, Mapsui.Styles.Color outline)
    //{
    //    ReadBoatShapeFile();
    //}
    //public BoatShape(string path, SKPaint fill, SKPaint outline)
    //{
    //    ReadBoatShapeFile();
    //}

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
                    var parts = line.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                    points.Add(new SKPoint(float.Parse(parts[0]), float.Parse(parts[1])));
                }
                SKPoints = new(points);
            }
            catch (System.Exception)
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

public class BoatRenderer : ISkiaStyleRenderer
{
    public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache, long iteration)
    {
        if (feature is not PointFeature pointFeature) return false; // for now
        var worldPoint = pointFeature.Point;
        var screenPoint = viewport.WorldToScreen(worldPoint);

        canvas.Translate((float)screenPoint.X, (float)screenPoint.Y);
        canvas.Scale(-(float)(1.0 / viewport.Resolution));

        BoatStyle boatStyle = (style as BoatStyle);
        canvas.RotateDegrees(boatStyle.SymbolRotation);
        BoatShape boatShape = (layer as BoatShapeLayer).BoatShape;
        
        SKPath path = new SKPath();
        path.AddPoly(boatShape.SKPoints.ToArray());
        canvas.DrawPath(path, boatShape.SKFill);
        canvas.DrawCircle(0, 0, boatStyle.OriginSize, new SKPaint() { Color = SKColors.Red });
        canvas.DrawPoints(SKPointMode.Polygon, boatShape.SKPoints.ToArray(), boatShape.SKOutline);
        //canvas.DrawPoints(SKPointMode.Polygon, boatShape.SKPoints.ToArray(), new SKPaint() { Color = SKColors.Orange});
        return true;
    }
}

public class BoatStyle : VectorStyle,IStyle
{
    public int SymbolRotation { get; set; }
    public float OriginSize { get; set; } = 0.1f;
}
