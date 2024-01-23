using BruTile.Wmts;
using HarfBuzzSharp;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.QuadEdge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IFeature = Mapsui.IFeature;

namespace Mapper_v1.Layers
{
    public class DxfLayer : MemoryLayer, ILayer, IDisposable
    {
        private List<IFeature> _features;
        private string _path;

        public string CRS { get; set; }

        public DxfLayer(string path)
        {
            //_map = map ?? throw new ArgumentNullException("Map shouldn't be null");
            _path = Path.Exists(path) ? path : throw new ArgumentException("Path does not exist");
            _features = new List<IFeature>();
            // dxf checks
            DxfVersion version = DxfDocument.CheckDxfFileVersion(_path);
            if (version == DxfVersion.Unknown)
            {

            }
            if (version < DxfVersion.AutoCad2000 ) 
            {
                
            }
            try
            {
                DxfDocument dxfDocument = DxfDocument.Load(_path);
                AddFeatures(dxfDocument);
            }
            catch (Exception)
            {
                MessageBox.Show("Minimum DXF version is AutoCad2000 !");
            }
            
        }

        private void AddFeatures(DxfDocument dxfDocument)
        {
            var ent = dxfDocument.Entities;
            foreach (var feature in ent.All)
            {
                switch (feature.Type)
                {
                    case netDxf.Entities.EntityType.Arc:
                        _features.Add(CreateFeatureFromArc(feature));
                        break;
                    case netDxf.Entities.EntityType.Circle:
                        _features.Add(CreateFeatureFromCircle(feature));
                        break;
                    case netDxf.Entities.EntityType.Dimension:
                        break;
                    case netDxf.Entities.EntityType.Ellipse:
                        break;
                    case netDxf.Entities.EntityType.Face3D:
                        break;
                    case netDxf.Entities.EntityType.Hatch:
                        break;
                    case netDxf.Entities.EntityType.Image:
                        break;
                    case netDxf.Entities.EntityType.Insert:
                        break;
                    case netDxf.Entities.EntityType.Leader:
                        break;
                    case netDxf.Entities.EntityType.Line:
                        _features.Add(CreateFeatureFromLine(feature));
                        break;
                    case netDxf.Entities.EntityType.Mesh:
                        break;
                    case netDxf.Entities.EntityType.MLine:
                        break;
                    case netDxf.Entities.EntityType.MText:
                        break;
                    case netDxf.Entities.EntityType.Point:
                        _features.Add(CreateFeatureFromPoint(feature));
                        break;
                    case netDxf.Entities.EntityType.PolyfaceMesh:
                        break;
                    case netDxf.Entities.EntityType.PolygonMesh:
                        break;
                    case netDxf.Entities.EntityType.Polyline2D:
                        _features.Add(CreateFeatureFromPoly2D(feature));
                        break;
                    case netDxf.Entities.EntityType.Polyline3D:
                        _features.Add(CreateFeatureFromPoly3D(feature));
                        break;
                    case netDxf.Entities.EntityType.Ray:
                        break;
                    case netDxf.Entities.EntityType.Shape:
                        break;
                    case netDxf.Entities.EntityType.Solid:
                        break;
                    case netDxf.Entities.EntityType.Spline:
                        break;
                    case netDxf.Entities.EntityType.Text:
                        _features.Add(CreateFeatureFromText(feature));
                        break;
                    case netDxf.Entities.EntityType.Tolerance:
                        break;
                    case netDxf.Entities.EntityType.Trace:
                        break;
                    case netDxf.Entities.EntityType.Underlay:
                        break;
                    case netDxf.Entities.EntityType.Viewport:
                        break;
                    case netDxf.Entities.EntityType.Wipeout:
                        break;
                    case netDxf.Entities.EntityType.XLine:
                        break;
                    default:
                        break;
                }
            }
            Features = _features;
        }

        private IFeature CreateFeatureFromText(EntityObject feature)
        {
            var text = feature as Text;
            return null;
        }

        private IFeature CreateFeatureFromCircle(EntityObject feature)
        {
            var circ = feature as Circle;
            var geofac = NtsGeometryServices.Instance.CreateGeometryFactory();
            var circfeature = geofac.CreatePoint(ToCoord(circ.Center)).Buffer(circ.Radius);
            return new GeometryFeature(circfeature);
        }

        private IFeature CreateFeatureFromArc(EntityObject feature)
        {
            var arc = feature as Arc;
            Polyline2D poly = arc.ToPolyline2D(45);
            Coordinate[] points = (from Polyline2DVertex vertex in poly.Vertexes
                                   select ToCoord(vertex.Position)).ToArray();
            return new GeometryFeature(new LineString(points));
        }

        private IFeature CreateFeatureFromPoint(EntityObject feature)
        {
            netDxf.Entities.Point point = (netDxf.Entities.Point)feature;
            return new PointFeature(ToMPoint(point.Position));
        }

        private IFeature CreateFeatureFromPoly3D(EntityObject feature)
        {
            Polyline2D poly = (feature as Polyline3D).ToPolyline2D(0);
            Coordinate[] points = (from Polyline2DVertex vertex in poly.Vertexes
                                   select ToCoord(vertex.Position)).ToArray();
            return new GeometryFeature(new LineString(points));
        }

        private IFeature CreateFeatureFromPoly2D(EntityObject feature)
        {
            Polyline2D poly = feature as Polyline2D;
            Coordinate[] points = (from Polyline2DVertex vertex in poly.Vertexes
                                       select ToCoord(vertex.Position)).ToArray();
            return new GeometryFeature(new LineString(points));
        }

        private IFeature CreateFeatureFromLine(EntityObject feature)
        {
            Line line = feature as Line;
            return new GeometryFeature(new LineString([ToCoord(line.StartPoint), ToCoord(line.EndPoint)]));
        }

        /// <summary>
        /// Vector3 to MPoint
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private MPoint ToMPoint(Vector3 position)
        {
            return new MPoint(position.X, position.Y);
        }
        /// <summary>
        /// Vector2 to Coordinate
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Coordinate ToCoord(Vector2 position)
        {
            return new Coordinate(position.X, position.Y);
        }
        /// <summary>
        /// Vector3 to Coordinate
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Coordinate ToCoord(Vector3 position)
        {
            return new Coordinate(position.X, position.Y);
        }

        public override IEnumerable<IFeature> GetFeatures(MRect box, double resolution)
        {
            return _features;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _features = null;
            }

            base.Dispose(disposing);
        }
    }
}
