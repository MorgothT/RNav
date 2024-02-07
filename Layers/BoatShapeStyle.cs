using Mapper_v1.Models;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace Mapper_v1.Layers
{
    public class BoatStyle : VectorStyle, IStyle
    {
        public int SymbolRotation { get; set; }
        public float OriginSize { get; set; } = 0.1f;
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



}
