//using Mapper_v1.Models;
//using Mapsui;
//using Mapsui.Projections;
//using ProjNet.CoordinateSystems;
//using ProjNet.CoordinateSystems.Transformations;
//using System.Diagnostics.CodeAnalysis;
//using System.IO;

//namespace Mapper_v1.Projections;

//public class WktProjections : Mapsui.Projections.IProjection
//{
//    private readonly IDictionary<string, Func<double, double, (double, double)>> _toLonLat = new Dictionary<string, Func<double, double, (double, double)>>();
//    private readonly IDictionary<string, Func<double, double, (double, double)>> _fromLonLat = new Dictionary<string, Func<double, double, (double, double)>>();

//    private readonly IDictionary<string, ICoordinateTransformation> _ctToDict = new Dictionary<string, ICoordinateTransformation>();
//    private readonly IDictionary<string, ICoordinateTransformation> _ctFromDict = new Dictionary<string, ICoordinateTransformation>();

//    private string ConfigPath = ".\\Projections.cfg";

//    public WktProjections()
//    {
//        if (File.Exists(ConfigPath) == false) 
//            CreateDefaultProjectionsConfig(ConfigPath);
//        ProjectionCfg config = new ProjectionCfg(ConfigPath);
//        foreach (var wkt in config.CoordinateSystems)
//        {
//            _ctFromDict.Add(wkt.Name,GetFromLatLong(wkt.WKT));
//            _ctToDict.Add(wkt.Name, GetToLatLong(wkt.WKT));

//            string epsg = $"{wkt.Authority}:{wkt.AuthorityCode}";
//            _fromLonLat.Add(epsg,GenerateFromLatLong(wkt.Name));
//            _toLonLat.Add(epsg, GenerateToLatLong(wkt.Name));

//            //ProjectionNames.Add($"{wkt.Name} - {wkt.Authority}:{wkt.AuthorityCode}");
//        }
//        _toLonLat["EPSG:4326"] = (x, y) => (x, y);
//        _fromLonLat["EPSG:4326"] = (x, y) => (x, y);

//        _toLonLat["EPSG:3857"] = SphericalMercator.ToLonLat;
//        _fromLonLat["EPSG:3857"] = SphericalMercator.FromLonLat;

//    }

//    private void CreateDefaultProjectionsConfig(string configPath)
//    {
//        List<string> projections =
//        [
//            //"GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]",
//            //"PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0 +lon_0=0 +x_0=0 +y_0=0 +k=1 +units=m +nadgrids=@null +wktext +no_defs\"],AUTHORITY[\"EPSG\",\"3857\"]]",
//            "PROJCS[\"WGS 84 / UTM zone 36N\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",33],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"32636\"]]",
//            "PROJCS[\"Israeli Grid 05/12\",GEOGCS[\"IG05/12 Intermediate CRS\",DATUM[\"IG05_12_Intermediate_Datum\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[24.0024, 17.1032, 17.8444, -0.33009, -1.85269, 1.66969, -5.4248],AUTHORITY[\"EPSG\",\"1144\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"6990\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",31.7343936111111],PARAMETER[\"central_meridian\",35.2045169444444],PARAMETER[\"scale_factor\",1.0000067],PARAMETER[\"false_easting\",219529.584],PARAMETER[\"false_northing\",626907.39],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"6991\"]]"
//            ];
//        File.WriteAllLines(configPath, projections);
//    }

//    ICoordinateTransformation GetFromLatLong(string Wkt)
//    {
//        var csFac = new CoordinateSystemFactory(); //as ICoordinateSystemFactory;
//        var ctFac = new CoordinateTransformationFactory();// as ICoordinateTransformationFactory;
//        var csSource = csFac.CreateFromWkt(Wkt);
//        var csTarget = GeographicCoordinateSystem.WGS84;
//        var trans = ctFac.CreateFromCoordinateSystems(csTarget, csSource);
//        return trans;
//    }
//    ICoordinateTransformation GetToLatLong(string Wkt)
//    {
//        CoordinateSystemFactory csFac = new CoordinateSystemFactory();
//        CoordinateTransformationFactory ctFac = new CoordinateTransformationFactory();
//        CoordinateSystem csSource = csFac.CreateFromWkt(Wkt);
//        CoordinateSystem csTarget = GeographicCoordinateSystem.WGS84;
//        var trans = ctFac.CreateFromCoordinateSystems(csSource, csTarget);
//        return trans;
//    }
    
//    public Func<double, double, (double,double)> GenerateFromLatLong(string crskey)
//    {
//        return (x, y) => { double[] result = _ctFromDict[crskey].MathTransform.Transform([x,y]); return new (result[0], result[1]); };
//    }
//    public Func<double, double, (double, double)> GenerateToLatLong(string crskey)
//    {
//        return (x, y) => { double[] result = _ctToDict[crskey].MathTransform.Transform([x, y]); return new (result[0], result[1]); };
//    }

//    public bool IsProjectionSupported([NotNullWhen(true)] string? fromCRS, [NotNullWhen(true)] string? toCRS)
//    {
//        if (fromCRS == null || toCRS == null)
//        {
//            return false;
//        }

//        if (_toLonLat.ContainsKey(fromCRS))
//        {
//            return _fromLonLat.ContainsKey(toCRS);
//        }

//        return false;
//    }
//    private static (double X, double Y) Project(double x, double y, Func<double, double, (double, double)> projectFunc)
//    {
//        return projectFunc(x, y);
//    }
//    private static void Project(IFeature feature, Func<double, double, (double, double)> transformFunc)
//    {
//        Func<double, double, (double, double)> transformFunc2 = transformFunc;
//        feature.CoordinateVisitor(delegate (double x, double y, CoordinateSetter setter)
//        {
//            var (x2, y2) = transformFunc2(x, y);
//            setter(x2, y2);
//        });
//    }
//    private static void Project(IEnumerable<IFeature> features, Func<double, double, (double, double)> transformFunc)
//    {
//        Func<double, double, (double, double)> transformFunc2 = transformFunc;
//        foreach (IFeature feature in features)
//        {
//            feature.CoordinateVisitor(delegate (double x, double y, CoordinateSetter setter)
//            {
//                var (x2, y2) = transformFunc2(x, y);
//                setter(x2, y2);
//            });
//        }
//    }
//    private static void Project(MPoint point, Func<double, double, (double, double)> projectFunc)
//    {
//        (point.X, point.Y) = projectFunc(point.X, point.Y);
//    }
//    public (double X, double Y) Project(string fromCRS, string toCRS, double x, double y)
//    {
//        var (x2, y2) = Project(x, y, _toLonLat[fromCRS]);
//        return Project(x2, y2, _fromLonLat[toCRS]);
//    }
//    public void Project(string fromCRS, string toCRS, MPoint point)
//    {
//        if (!IsProjectionSupported(fromCRS, toCRS))
//        {
//            throw new NotSupportedException("Projection is not supported. From CRS: " + fromCRS + ". To CRS " + toCRS);
//        }

//        Project(point, _toLonLat[fromCRS]);
//        Project(point, _fromLonLat[toCRS]);
//    }
//    public void Project(string fromCRS, string toCRS, MRect rect)
//    {
//        if (!IsProjectionSupported(fromCRS, toCRS))
//        {
//            throw new NotSupportedException("Projection is not supported. From CRS: " + fromCRS + ". To CRS " + toCRS);
//        }

//        Project(rect.Min, _toLonLat[fromCRS]);
//        Project(rect.Min, _fromLonLat[toCRS]);
//        Project(rect.Max, _toLonLat[fromCRS]);
//        Project(rect.Max, _fromLonLat[toCRS]);
//    }
//    public void Project(string fromCRS, string toCRS, IFeature feature)
//    {
//        if (!IsProjectionSupported(fromCRS, toCRS))
//        {
//            throw new NotSupportedException("Projection is not supported. From CRS: " + fromCRS + ". To CRS " + toCRS);
//        }

//        Project(feature, _toLonLat[fromCRS]);
//        Project(feature, _fromLonLat[toCRS]);
//    }
//    public void Project(string fromCRS, string toCRS, IEnumerable<IFeature> features)
//    {
//        Project(features, _toLonLat[fromCRS]);
//        Project(features, _fromLonLat[toCRS]);
//    }
//}
