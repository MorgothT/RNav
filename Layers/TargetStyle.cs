using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace Mapper_v1.Layers;

public class TargetStyle : IStyle
{
    public double MinVisible { get; set; } = 0;
    public double MaxVisible { get; set; } = double.MaxValue;
    public bool Enabled { get; set; } = true;
    public float Opacity { get; set; } = 0.5f;
    //public float Scale { get; set; }
    public double Radius { get; set; } = 5;
    public SKColor Color { get; set; }
}

public class TargetRenderer : ISkiaStyleRenderer
{
    public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache, long iteration)
    {
        MPoint worldPoint;
        //if (feature is GeometryFeature geometryFeature)
        //{
        //    worldPoint = new MPoint(geometryFeature.Geometry.InteriorPoint.X, geometryFeature.Geometry.InteriorPoint.Y);
        //}
        if (feature is PointFeature pointFeature)
        {
            worldPoint = new MPoint(pointFeature.Point.X, pointFeature.Point.Y);
        }
        else
        {
            return false;
        }

        var screenPoint = viewport.WorldToScreen(worldPoint);
        TargetStyle targetStyle = (TargetStyle)style;

        using var colored = new SKPaint { Color = targetStyle.Color, IsAntialias = true, Style = SKPaintStyle.Stroke };
        using var filled = new SKPaint { Color = targetStyle.Color.WithAlpha((byte)(255 * targetStyle.Opacity)), IsAntialias = true, Style = SKPaintStyle.Fill };
        if ((bool)feature["IsSelected"] == true)
        {
            colored.Color = SKColors.DarkRed;
            filled.Color = SKColors.DarkRed.WithAlpha((byte)(255 * targetStyle.Opacity));
        }

        canvas.Translate((float)screenPoint.X, (float)screenPoint.Y);
        canvas.Scale(-(float)(1.0 / viewport.Resolution));
        canvas.DrawCircle(0, 0, (float)targetStyle.Radius, colored);
        canvas.DrawCircle(0, 0, (float)targetStyle.Radius, filled);
        canvas.DrawPoint(0, 0, colored);
        return true;
    }
}


