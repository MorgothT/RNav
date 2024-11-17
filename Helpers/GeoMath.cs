using Mapper_v1.Models;
using Mapsui;
using Mapsui.Projections;
using System.Windows;
using static GeoConverter.Converter;

namespace Mapper_v1.Helpers;

public static class GeoMath
{
    public static double CalcBearing(this MPoint p1, MPoint p2)
    {
        var rad = Math.Atan2((p1.Y - p2.Y), (p1.X - p2.X));
        var deg = rad * 180 / Math.PI;
        return (270 - deg) % 360;
    }
    public static double CalcBearing(this Point3d p1, Target t2)
    {
        return CalcBearing(new MPoint(p1.X, p1.Y), new MPoint(t2.X, t2.Y));
    }
    public static double CalcBearing(this MPoint p1, Target t2)
    {
        return p1.CalcBearing(t2.ToMPoint());
    }
    public static double Distance(this MPoint p1, Target t2)
    {
        return p1.Distance(t2.ToMPoint());
    }
    public static double FeetToMeters(this double infeet)
    {
        return infeet * 0.3048;
    }
    public static float FeetToMeters(this float infeet)
    {
        return infeet * 0.3048f;
    }
    public static MPoint AddOffsetToWgsPoint(this MPoint point, double x, double y)
    {
        ProjectionDefaults.Projection.Project("EPSG:4326", "EPSG:3857", point);
        MPoint offsetPoint = point.Offset(x, y);
        ProjectionDefaults.Projection.Project("EPSG:3857", "EPSG:4326", offsetPoint);
        return offsetPoint;
    }
    public static MPoint AddOffsetToWgsPoint( this MPoint point, Point offset)
    {
        return point.AddOffsetToWgsPoint(offset.X, offset.Y);
    }
    public static MPoint AddOffsetToWgsPoint(this MPoint point, MPoint offset)
    {
        return point.AddOffsetToWgsPoint(offset.X, offset.Y);
    }
    public static MPoint ToMPoint(this Target target)
    {
        return new MPoint(target.X, target.Y);
    }
    //public static void ToWgs(this MPoint point, string fromCRS)
    //{
    //    ProjectionDefaults.Projection.Project(fromCRS,"EPSG:4326",point);
    //}
    public static MPoint ToWgs(this MPoint point, string fromCRS)
    {
        MPoint p = new(point);
        ProjectionDefaults.Projection.Project(fromCRS, "EPSG:4326", p);
        return p;
    }
    
    
}
