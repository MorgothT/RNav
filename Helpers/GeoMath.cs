using Mapper_v1.Models;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}
