using Mapsui;
using Mapsui.Projections;
using System.Diagnostics.CodeAnalysis;
using GeoConverter;

#nullable enable
namespace Mapper_v1.Projections
{
    public class IsraelProjections : IProjection
    {
        private readonly IDictionary<string, Func<double, double, (double, double)>> _toLonLat = new Dictionary<string, Func<double, double, (double, double)>>();

        private readonly IDictionary<string, Func<double, double, (double, double)>> _fromLonLat = new Dictionary<string, Func<double, double, (double, double)>>();
        //private static Converter converter;
        public IsraelProjections() 
        {
            _toLonLat["EPSG:4326"] = (double x, double y) => (x, y);
            _fromLonLat["EPSG:4326"] = (double x, double y) => (x, y);
            _toLonLat["EPSG:3857"] = SphericalMercator.ToLonLat;
            _fromLonLat["EPSG:3857"] = SphericalMercator.FromLonLat;
            _toLonLat["EPSG:6991"] = fromItm;
            _fromLonLat["EPSG:6991"] = toItm;
            _toLonLat["EPSG:32636"] = fromUtm;
            _fromLonLat["EPSG:32636"] = toUtm;
        }
        public bool IsProjectionSupported([NotNullWhen(true)] string? fromCRS, [NotNullWhen(true)] string? toCRS)
        {
            if (fromCRS == null || toCRS == null)
            {
                return false;
            }

            if (_toLonLat.ContainsKey(fromCRS))
            {
                return _fromLonLat.ContainsKey(toCRS);
            }

            return false;
        }
        private static (double lon, double lat) fromUtm(double x, double y)
        {
            var converter = new Converter(Converter.Projections.UTM_36N, Converter.Ellipsoids.WGS_84);
            var p = converter.Convert(new Converter.Point3d(x, y, 0));
            return (lon: p.X, lat: p.Y);
        }
        private (double x, double y) toUtm(double lon, double lat)
        {
            var converter = new Converter(Converter.Ellipsoids.WGS_84, GeoConverter.Converter.Projections.UTM_36N);
            var p = converter.Convert(new Converter.Point3d(lon, lat, 0));
            return (p.X, p.Y);
        }
        private static (double lon, double lat) fromItm(double x, double y)
        {
            var converter = new Converter(Converter.Projections.ITM,Converter.Ellipsoids.WGS_84);
            var p = converter.Convert(new Converter.Point3d(x, y, 0));
            return (lon:p.X, lat:p.Y); 
        }
        private static (double x, double y) toItm(double lon, double lat)
        {
            var converter = new Converter(Converter.Ellipsoids.WGS_84, GeoConverter.Converter.Projections.ITM);
            var p = converter.Convert(new Converter.Point3d(lon, lat, 0));
            return (p.X, p.Y);
        }
        private static (double X, double Y) Project(double x, double y, Func<double, double, (double, double)> projectFunc)
        {
            return projectFunc(x, y);
        }
        private static void Project(IFeature feature, Func<double, double, (double, double)> transformFunc)
        {
            Func<double, double, (double, double)> transformFunc2 = transformFunc;
            feature.CoordinateVisitor(delegate (double x, double y, CoordinateSetter setter)
            {
                var (x2, y2) = transformFunc2(x, y);
                setter(x2, y2);
            });
        }
        private static void Project(IEnumerable<IFeature> features, Func<double, double, (double, double)> transformFunc)
        {
            Func<double, double, (double, double)> transformFunc2 = transformFunc;
            foreach (IFeature feature in features)
            {
                feature.CoordinateVisitor(delegate (double x, double y, CoordinateSetter setter)
                {
                    var (x2, y2) = transformFunc2(x, y);
                    setter(x2, y2);
                });
            }
        }
        private static void Project(MPoint point, Func<double, double, (double, double)> projectFunc)
        {
            (point.X, point.Y) = projectFunc(point.X, point.Y);
        }
        public (double X, double Y) Project(string fromCRS, string toCRS, double x, double y)
        {
            var (x2, y2) = Project(x, y, _toLonLat[fromCRS]);
            return Project(x2, y2, _fromLonLat[toCRS]);
        }
        public void Project(string fromCRS, string toCRS, MPoint point)
        {
            if (!IsProjectionSupported(fromCRS, toCRS))
            {
                throw new NotSupportedException("Projection is not supported. From CRS: " + fromCRS + ". To CRS " + toCRS);
            }

            Project(point, _toLonLat[fromCRS]);
            Project(point, _fromLonLat[toCRS]);
        }
        public void Project(string fromCRS, string toCRS, MRect rect)
        {
            if (!IsProjectionSupported(fromCRS, toCRS))
            {
                throw new NotSupportedException("Projection is not supported. From CRS: " + fromCRS + ". To CRS " + toCRS);
            }

            Project(rect.Min, _toLonLat[fromCRS]);
            Project(rect.Min, _fromLonLat[toCRS]);
            Project(rect.Max, _toLonLat[fromCRS]);
            Project(rect.Max, _fromLonLat[toCRS]);
        }
        public void Project(string fromCRS, string toCRS, IFeature feature)
        {
            if (!IsProjectionSupported(fromCRS, toCRS))
            {
                throw new NotSupportedException("Projection is not supported. From CRS: " + fromCRS + ". To CRS " + toCRS);
            }

            Project(feature, _toLonLat[fromCRS]);
            Project(feature, _fromLonLat[toCRS]);
        }
        public void Project(string fromCRS, string toCRS, IEnumerable<IFeature> features)
        {
            Project(features, _toLonLat[fromCRS]);
            Project(features, _fromLonLat[toCRS]);
        }
    }
}
