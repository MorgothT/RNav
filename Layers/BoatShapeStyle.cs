using Mapsui.Layers;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Rendering;
using Mapsui.Styles;
using Mapsui;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapsui.Extensions;
using Mapper_v1.Models;

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
