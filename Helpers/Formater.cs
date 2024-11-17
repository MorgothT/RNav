using Mapper_v1.Models;

namespace Mapper_v1.Helpers;

public static class Formater
{
    public static string FormatLatLong(this double ll, DegreeFormat degreeFormat)
    {
        int d, m = 0;
        double mm, ss = 0;
        switch (degreeFormat)
        {
            case DegreeFormat.Deg:
                return ll.ToString("F8");
            case DegreeFormat.Min:
                d = (int)ll;
                mm = (ll - d) * 60;
                return $"{d}\"{mm.ToString("F6")}";
            case DegreeFormat.Sec:
                d = (int)ll;
                mm = (ll - d) * 60;
                m = (int)mm;
                ss = (mm - m) * 60;
                return $"{d}\"{m}'{ss.ToString("F4")}";
            default:
                return ll.ToString();
        }
    }
}
