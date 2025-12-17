using Mapper_v1.Models;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;
using System.Diagnostics;

namespace Mapper_v1.Layers
{
    public class BoatStyle : VectorStyle, IStyle
    {
        public int SymbolRotation { get; set; }
        public float OriginSize { get; set; } = 0.1f;   //Size of Vessel Origin
    }

    public class BoatRenderer : ISkiaStyleRenderer
    {
        public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache, long iteration)
        {
            if (feature is not PointFeature pointFeature) return false; // for now
            var worldPoint = pointFeature.Point;
            var screenPoint = viewport.WorldToScreen(worldPoint);
            BoatStyle boatStyle = (style as BoatStyle);
            BoatShape boatShape = (layer as BoatShapeLayer).BoatShape;

            canvas.Translate((float)screenPoint.X, (float)screenPoint.Y);
            //float scale = 1f / (float)((7.0 / 8.0) * viewport.Resolution); // 8/7 = 1.142857.. (with Sperical mercator, 1m at Equator is 7/8m at Lat=30°)  !!!
            var latRad = SphericalMercator.ToLonLat(worldPoint).Y*Math.PI/180;
            float scale = (float)(1 / (viewport.Resolution * Math.Cos(latRad)));
            //Trace.WriteLine($"1: {canvas.TotalMatrix.ScaleX},{canvas.TotalMatrix.ScaleY},{canvas.TotalMatrix.SkewX},{canvas.TotalMatrix.SkewY}");
            canvas.RotateDegrees(boatStyle.SymbolRotation);
            //Trace.WriteLine($"2: {canvas.TotalMatrix.ScaleX},{canvas.TotalMatrix.ScaleY},{canvas.TotalMatrix.SkewX},{canvas.TotalMatrix.SkewY}");
            // make sure the boat is seen at all zoom levels with absolute size

            canvas.Scale(scale, -scale);
            //Trace.WriteLine($"3: {canvas.TotalMatrix.ScaleX},{canvas.TotalMatrix.ScaleY},{canvas.TotalMatrix.SkewX},{canvas.TotalMatrix.SkewY}");
            if (scale >= 0.01f)
            {
                SKPath path = new SKPath();
                path.AddPoly(boatShape.VesselOutline.ToArray());
                //Draw a fill of the vessel
                canvas.DrawPath(path, boatShape.SKFillColor);
                // Draw the origin point
                canvas.DrawCircle(0, 0, boatStyle.OriginSize, new SKPaint() { Color = SKColors.Red });
                // Draw the outline of the vessel
                canvas.DrawPoints(SKPointMode.Polygon, boatShape.VesselOutline.ToArray(), boatShape.SKOutlineColor);
                // Draw objects
                foreach (VesselArc arc in boatShape.VesselObjects)
                {
                    DrawArcBetween2Points(canvas, arc, boatShape.SKOutlineColor);
                }
                //Draw Anchors
                foreach (VesselAnchor anchor in boatShape.VesselAnchors)
                {
                    DrawAnchor(canvas, anchor);
                }
            }
            else // draw a simple dot when zoomed out too far
            {
                canvas.DrawCircle(0, 0, 1/scale, new SKPaint() { Color = SKColors.Red, IsStroke = false});
            }
            return true;
        }

        private void DrawAnchor(SKCanvas canvas, VesselAnchor anchor)
        {
            var paint = new SKPaint() { Color= SKColors.Red , IsStroke=true};
            var path = new SKPath();
            
            path.AddCircle(0f, .0f, .3f);
            path.AddPoly(
                [
                new(0f,-.3f),
                new(0f,-.8f),
                //new(-.5f,-.8f),
                new(-.6f,-.5f),
                new(0f,-.8f),
                new(.6f,-.5f),
                //new(-.6f,-.6f),
                //new(-.5f,-.55f),
                //new(-.4f,-.6f),
                //new(-.5f,-.6f),
                //new(-.5f,-.8f),
                //new(.5f,-.8f),
                //new(.5f,-.6f),
                //new(.6f,-.6f),
                //new(.5f,-.55f),
                //new(.4f,-.6f),
                //new(.5f,-.6f),
                ],false);
            //path.AddArc(new SKRect(-.5f, 0f, .5f, -.5f), 0.1f, 0.2f);

            path.Offset(new SKPoint((float)anchor.X, (float)anchor.Y));
            //DrawArcBetween2Points(canvas, new(-5, -5, 5, -5, 5),paint);
            canvas.DrawPath(path,paint);
            //canvas.DrawText(anchor.Name, (float)anchor.X, (float)(anchor.Y - 1), new() { Size = 1, }, paint); //not readable !
        }

        private static void DrawArcBetween2Points(SKCanvas canvas,VesselArc arc, SKPaint color)
        {
            MPoint a = new(arc.p1X, arc.p1Y), b = new(arc.p2X, arc.p2Y);
            double radius = arc.radius;
            if (radius < 0)
            {
                var temp = b;
                b = a;
                a = temp;
                radius = -radius;
            }
            double dx = b.X - a.X, dy = b.Y - a.Y;
            var theta = Math.Atan2(dy, dx);
            var l = Math.Sqrt(dx * dx + dy * dy);
            if (2*radius >= l)
            {
                var phi = Math.Asin(l / (2 * radius));
                var h = radius * Math.Cos(phi);
                MPoint center = new((a.X + dx / 2 - h * (dy / l)), (a.Y + dy / 2 + h * (dx / l)));
                const double deg = 180 / Math.PI;
                var rect = new SKRect((float)(center.X - radius), (float)(center.Y - radius), (float)(center.X + radius), (float)(center.Y + radius));
                canvas.DrawArc(rect, (float)((theta - phi) * deg - 90), (float)(2 * phi * deg), false , new SKPaint { Color = color.Color, IsStroke = true});
            }
        }
    }
}
