using Mapper_v1.Models;
using Mapsui;
using Mapsui.Projections;
using static GeoConverter.Converter;

namespace Mapper_v1.Helpers;

public class GeoMath
{
    public static double CalcBearing(MPoint p1, MPoint p2)
    {
        var rad = Math.Atan2((p1.Y - p2.Y), (p1.X - p2.X));
        var deg = rad * 180 / Math.PI;
        return (270 - deg) % 360;
    }
    public static double CalcBearing(Point3d p1, Target t2)
    {
        return CalcBearing(new MPoint(p1.X, p1.Y), new MPoint(t2.X, t2.Y));
    }
    public static double FeetToMeters(double infeet)
    {
        return infeet * 0.3048;
    }
    public static MPoint AddOffsetToWgsPoint(MPoint point, double x, double y)
    {
        ProjectionDefaults.Projection.Project("EPSG:4326", "EPSG:3857", point);
        MPoint offsetPoint = point.Offset(x, y);
        ProjectionDefaults.Projection.Project("EPSG:3857", "EPSG:4326", offsetPoint);
        return offsetPoint;
    }
}
