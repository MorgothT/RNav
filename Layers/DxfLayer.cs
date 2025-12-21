using Mapper_v1.Converters;
using Mapper_v1.Helpers;
using Mapper_v1.Models;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Styles;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.IO;
using System.Windows;
using IFeature = Mapsui.IFeature;

namespace Mapper_v1.Layers
{
    public class DxfLayer : MemoryLayer, ILayer, IDisposable
    {
        private List<IFeature> _features;
        private string _path;
        private VectorStyle vectorStyle;
        private LabelStyle labelStyle;
        private PointStyle pointStyle;
        private static readonly ColorConvertor colorConvertor = new();
        public string CRS { get; set; }

        public DxfLayer(ChartItem chart)
        {
            //_map = map ?? throw new ArgumentNullException("Map shouldn't be null");
            _path = Path.Exists(chart.Path) ? chart.Path : throw new ArgumentException("Path does not exist");
            _features = new List<IFeature>();
            Style = null;// GetVectorStyle(chart);
            vectorStyle = GetVectorStyle(chart);
            labelStyle = GetLabelStyle(chart);
            pointStyle = GetPointStyle();
            //_styles = (StyleCollection)Style;

            // dxf checks
            DxfVersion version = DxfDocument.CheckDxfFileVersion(_path);
            if (version == DxfVersion.Unknown)
            {

            }
            if (version < DxfVersion.AutoCad2000)
            {
                MessageBox.Show("Minimum DXF version is AutoCad2000 !");
            }
            try
            {
                DxfDocument dxfDocument = DxfDocument.Load(_path);
                AddFeatures(dxfDocument);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {_path}{Environment.NewLine}{ex.Message}");
            }
            
        }

        private PointStyle GetPointStyle()
        {
            PointSettings settings = new MapSettings().GetMapSettings().PointSettings;
            PointStyle pointStyle = new PointStyle()
            {
                Color = colorConvertor.WMColorToMapsui(settings.Color),
                Shape = settings.Shape,
                Size = settings.Size,
                IsAbsoluteUnits = settings.IsAbsoluteUnits,
            };
            return pointStyle;
        }

        private static VectorStyle GetVectorStyle(ChartItem chart)
        {
            return new VectorStyle
            {
                Outline = new Pen   // Outline of Areas and Points
                {
                    Width = chart.LineWidth,
                    Color = colorConvertor.WMColorToMapsui(chart.OutlineColor)//new Color(255, 0, 0, 0)
                },
                Fill = new Brush { Color = colorConvertor.WMColorToMapsui(chart.FillColor) },   // Fill of Areas and Points
                Line = new Pen  //Polyline Style
                {
                    Width = chart.LineWidth,
                    Color = colorConvertor.WMColorToMapsui(chart.LineColor)
                }
            };
        }
        private static LabelStyle GetLabelStyle(ChartItem chart)
        {
            return new LabelStyle
            {
                ForeColor = colorConvertor.WMColorToMapsui(chart.LabelColor),
                BackColor = new Brush(colorConvertor.WMColorToMapsui(chart.BackroundColor)),
                Font = new Font { FontFamily = "GenericSerif", Size = (double)chart.LabelFontSize },
                HorizontalAlignment = (LabelStyle.HorizontalAlignmentEnum)chart.HorizontalAlignment, //LabelStyle.HorizontalAlignmentEnum.Center,
                VerticalAlignment = (LabelStyle.VerticalAlignmentEnum)chart.VerticalAlignment, //LabelStyle.VerticalAlignmentEnum.Center,
                Offset = new Offset { X = 0, Y = 0 },
                Halo = new Pen { Color = colorConvertor.WMColorToMapsui(chart.HaloColor), Width = 1 },
                CollisionDetection = true,
                LabelColumn = chart.LabelAttributeName
            };
        }
        //private static StyleCollection GetDxfStyles(ChartItem chart)
        //{
        //    //ColorConvertor colorConvertor = new ColorConvertor();
        //    StyleCollection styles = new StyleCollection();
        //    styles.Styles.Add(new VectorStyle
        //    {
        //        Outline = new Pen   // Outline of Areas and Points
        //        {
        //            Width = chart.LineWidth,
        //            Color = colorConvertor.WMColorToMapsui(chart.OutlineColor)//new Color(255, 0, 0, 0)
        //        },
        //        Fill = new Brush { Color = colorConvertor.WMColorToMapsui(chart.FillColor) },   // Fill of Areas and Points
        //        Line = new Pen  //Polyline Style
        //        {
        //            Width = chart.LineWidth,
        //            Color = colorConvertor.WMColorToMapsui(chart.LineColor)
        //        }
        //    });
        //    styles.Styles.Add(new LabelStyle
        //    {
        //        ForeColor = colorConvertor.WMColorToMapsui(chart.LabelColor),
        //        BackColor = new Brush(colorConvertor.WMColorToMapsui(chart.BackroundColor)),
        //        Font = new Font { FontFamily = "GenericSerif", Size = (double)chart.LabelFontSize },
        //        HorizontalAlignment = (LabelStyle.HorizontalAlignmentEnum)chart.HorizontalAlignment, //LabelStyle.HorizontalAlignmentEnum.Center,
        //        VerticalAlignment = (LabelStyle.VerticalAlignmentEnum)chart.VerticalAlignment, //LabelStyle.VerticalAlignmentEnum.Center,
        //        Offset = new Offset { X = 0, Y = 0 },
        //        Halo = new Pen { Color = colorConvertor.WMColorToMapsui(chart.HaloColor), Width = 1 },
        //        CollisionDetection = true,
        //        LabelColumn = chart.LabelAttributeName
        //    });
        //    return styles;
        //}
        private void AddFeatures(DxfDocument dxfDocument)
        {
            var ent = dxfDocument.Entities;
            foreach (var feature in ent.All)
            {
                switch (feature.Type)
                {
                    case EntityType.Arc:
                        _features.Add(CreateFeatureFromArc(feature));
                        break;
                    case EntityType.Circle:
                        _features.Add(CreateFeatureFromCircle(feature));
                        break;
                    case EntityType.Line:
                        _features.Add(CreateFeatureFromLine(feature));
                        break;
                    case EntityType.Point:
                        _features.Add(CreateFeatureFromPoint(feature));
                        break;
                    case EntityType.Polyline2D:
                        _features.Add(CreateFeatureFromPoly2D(feature));
                        break;
                    case EntityType.Polyline3D:
                        _features.Add(CreateFeatureFromPoly3D(feature));
                        break;
                    case EntityType.Text:
                        _features.Add(CreateFeatureFromText(feature));
                        break;
                    case EntityType.MText:
                        _features.Add(CreateFeatureFromMText(feature));
                        break;
                    case EntityType.Dimension:
                        break;
                    case EntityType.Ellipse:
                        break;
                    case EntityType.Face3D:
                        break;
                    case EntityType.Hatch:
                        break;
                    case EntityType.Image:
                        break;
                    case EntityType.Insert:
                        break;
                    case EntityType.Leader:
                        break;
                    case EntityType.Mesh:
                        break;
                    case EntityType.MLine:
                        break;
                    case EntityType.PolyfaceMesh:
                        break;
                    case EntityType.PolygonMesh:
                        break;
                    case EntityType.Ray:
                        break;
                    case EntityType.Shape:
                        break;
                    case EntityType.Solid:
                        break;
                    case EntityType.Spline:
                        break;
                    case EntityType.Tolerance:
                        break;
                    case EntityType.Trace:
                        break;
                    case EntityType.Underlay:
                        break;
                    case EntityType.Viewport:
                        break;
                    case EntityType.Wipeout:
                        break;
                    case EntityType.XLine:
                        break;
                    default:
                        break;
                }
            }
            Features = _features;
        }

        private IFeature CreateFeatureFromMText(EntityObject feature)
        {
            var text = feature as MText;
            var geofac = NtsGeometryServices.Instance.CreateGeometryFactory();
            var circfeature = geofac.CreatePoint(ToCoord(text.Position)).Buffer(0.001);
            var textFeature = new GeometryFeature
            {
                Geometry = circfeature,
            };
            //TODO: MTEXT - deal with other formating ?
            //text.Value = text.Value.Replace(@"\P", Environment.NewLine.ToString()); // uses PlainText later
            Color color = labelStyle.ForeColor;
            if (text.Color.IsByLayer == false)
            {
                color = new Color(text.Color.R, text.Color.G, text.Color.B,(text.Transparency.ToAlpha()));
            }
            // ignores embeded formating !
            textFeature.Styles.Add(new LabelStyle()
            {
                Text = text.PlainText(),
                BackColor = labelStyle.BackColor,
                CollisionDetection = true,
                Font = labelStyle.Font,
                ForeColor = color,
                Halo = labelStyle.Halo,
                HorizontalAlignment = labelStyle.HorizontalAlignment,
                VerticalAlignment = labelStyle.VerticalAlignment,
                //Offset = new Offset(1, -1),
                //MaxVisible = 100
            });
            return textFeature;
        }

        private IFeature CreateFeatureFromText(EntityObject feature)
        {
            var text = feature as Text;
            var geofac = NtsGeometryServices.Instance.CreateGeometryFactory();
            var circfeature = geofac.CreatePoint(ToCoord(text.Position)).Buffer(0.001);
            var textFeature = new GeometryFeature
            {
                Geometry = circfeature,
            };
            Color color = labelStyle.ForeColor;
            if (text.Color.IsByLayer == false)
            {
                color = new Color(text.Color.R, text.Color.G, text.Color.B, (text.Transparency.ToAlpha()));
            }
            textFeature.Styles.Clear();
            textFeature.Styles.Add(new LabelStyle()
            {
                Text = text.Value,
                BackColor = labelStyle.BackColor,
                CollisionDetection = true,
                Font = labelStyle.Font,
                ForeColor = color,
                Halo = labelStyle.Halo,
                HorizontalAlignment = labelStyle.HorizontalAlignment,
                VerticalAlignment = labelStyle.VerticalAlignment,
                //Offset = new Offset(1, -1),
                //MaxVisible = 100
            });
            return textFeature;
        }
        private IFeature CreateFeatureFromCircle(EntityObject feature)
        {
            var circ = feature as Circle;
            var geofac = NtsGeometryServices.Instance.CreateGeometryFactory();
            var circfeature = geofac.CreatePoint(ToCoord(circ.Center)).Buffer(circ.Radius);
            var gf = new GeometryFeature(circfeature);
            gf.Styles.Add(vectorStyle);
            return gf;
        }
        private IFeature CreateFeatureFromArc(EntityObject feature)
        {
            var arc = feature as Arc;
            Polyline2D poly = arc.ToPolyline2D(45);
            Coordinate[] points = (from Polyline2DVertex vertex in poly.Vertexes
                                   select ToCoord(vertex.Position)).ToArray();
            var gf = new GeometryFeature(new LineString(points));
            gf.Styles.Add(vectorStyle);
            return gf;
        }

        private IFeature CreateFeatureFromPoint(EntityObject feature)
        {
            netDxf.Entities.Point point = (netDxf.Entities.Point)feature;
            var pointFeature = new NetTopologySuite.Geometries.Point(ToCoord(point.Position)).ToFeature();
            //var geofac = NtsGeometryServices.Instance.CreateGeometryFactory();
            //var pointFeature = geofac.CreatePoint(ToCoord(point.Position)).ToFeature();
            

            //PointStyle style = pointStyle;
            pointFeature.Styles = [pointStyle];
            return pointFeature;
        }

        private IFeature CreateFeatureFromPoly3D(EntityObject feature)
        {
            Polyline2D poly = (feature as Polyline3D).ToPolyline2D(0);
            Coordinate[] points = (from Polyline2DVertex vertex in poly.Vertexes
                                   select ToCoord(vertex.Position)).ToArray();
            var gf = new GeometryFeature(new LineString(points));
            gf.Styles.Add(vectorStyle);
            return gf;
        }

        private IFeature CreateFeatureFromPoly2D(EntityObject feature)
        {
            Polyline2D poly = feature as Polyline2D;
            Coordinate[] points = (from Polyline2DVertex vertex in poly.Vertexes
                                   select ToCoord(vertex.Position)).ToArray();
            var gf = new GeometryFeature(new LineString(points));
            gf.Styles.Add(vectorStyle);
            return gf;
        }

        private IFeature CreateFeatureFromLine(EntityObject feature)
        {
            Line line = feature as Line;
            var gf = new GeometryFeature(new LineString([ToCoord(line.StartPoint), ToCoord(line.EndPoint)]));
            gf.Styles.Add(vectorStyle);
            return gf;
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
