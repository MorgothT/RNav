using Mapper_v1.Helpers;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.Extensions;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;
using System.Net.Http;

namespace Mapper_v1.Layers;

public class PointStyle : IStyle
{
    public double MinVisible { get; set; }
    public double MaxVisible { get; set ; } = double.MaxValue;
    public bool Enabled { get; set; } = true;
    public float Opacity { get; set; } = 1.0f;
    public Color Color { get; set; } = Color.Red;
    public PointShape Shape { get; set; } = PointShape.Dot;
    public double Size { get; set; } = 5;
}

public class PointRenderer : ISkiaStyleRenderer
{
    public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache, long iteration)
    {
        if (feature is not GeometryFeature point) return false;
        if (style is not PointStyle pointStyle) return false;
        SKPoint p = viewport.WorldToScreen(point.Geometry.Coordinate.ToMPoint()).ToSKPoint();
        SKColor color = pointStyle.Color.ToSKColor();
        SKPaint paint = new SKPaint()
        {
            Color = color,
            IsStroke = true
        };
        switch (pointStyle.Shape)
        {
            case PointShape.Dot:
                canvas.DrawPoint(p, color);
                return true;
            case PointShape.Circle:
                canvas.DrawCircle(p, (float)pointStyle.Size, paint);
                return true;
            case PointShape.Square:
                canvas.DrawRect(GetRectangle(pointStyle.Size, p), paint);
                return true;
            case PointShape.Cross:
                canvas.DrawPoints(SKPointMode.Polygon,GetXPoints(pointStyle.Size,p),paint);
                return true;
            case PointShape.SquareCross:
                canvas.DrawRect(GetRectangle(pointStyle.Size, p), paint);
                canvas.DrawPoints(SKPointMode.Lines, GetXPoints(pointStyle.Size * 1.5, p), paint);
                return true;
            case PointShape.CircleCross:
                canvas.DrawCircle(p, (float)pointStyle.Size, paint);
                canvas.DrawPoints(SKPointMode.Lines, GetXPoints(pointStyle.Size * 1.5, p), paint);
                return true;
            case PointShape.Plus:
                canvas.DrawPoints(SKPointMode.Lines,GetPlusPoints(pointStyle.Size,p),paint);
                return true;
            case PointShape.SquarePlus:
                canvas.DrawRect(GetRectangle(pointStyle.Size, p), paint);
                canvas.DrawPoints(SKPointMode.Lines, GetPlusPoints(pointStyle.Size * 1.5, p), paint);
                return true;
            case PointShape.CirclePlus:
                canvas.DrawCircle(p, (float)pointStyle.Size, paint);
                canvas.DrawPoints(SKPointMode.Lines, GetPlusPoints(pointStyle.Size * 1.5, p), paint);
                return true;
            default:
                return false;
        }
    }
    private SKPoint[] GetPlusPoints(double size, SKPoint p)
    {
        float s = (float)size;
        var points = new SKPoint[]
        {
        new SKPoint(p.X-s, p.Y),
        new SKPoint(p.X+s ,p.Y),
        new SKPoint(p.X, p.Y-s),
        new SKPoint(p.X, p.Y+s)
        };
        return points;
    }
    private SKPoint[] GetXPoints(double size, SKPoint p)
    {
        float s = (float)size;
        var points = new SKPoint[] 
        { 
        new SKPoint(p.X-s, p.Y-s),
        new SKPoint(p.X+s, p.Y+s),
        new SKPoint(p.X+s, p.Y-s),
        new SKPoint(p.X-s, p.Y+s)
        };
        return points;
    }

    private SKRect GetRectangle(double size, SKPoint p)
    {
        float s = (float)size;
        SKRect rect = new SKRect(-s, s, s, -s);
        rect.Offset(p);
        return rect;
    }
}

public enum PointShape
{
    Dot,
    Circle,
    Square,
    Cross,
    SquareCross,
    CircleCross,
    Plus,
    SquarePlus,
    CirclePlus,
}
