using Mapper_v1.Models;
using Mapsui;
using Mapsui.Animations;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Utilities;
#nullable enable

namespace Mapper_v1.Layers;

public class BoatShapeLayer : BaseLayer, IModifyFeatureLayer, IDisposable
{
    private readonly Map _map;
    private PointFeature _feature;
    private BoatStyle _locStyle;  // style for the location indicator
    private CalloutStyle _coStyle;  // style for the callout

    private MPoint? _animationMyLocationStart;
    private MPoint? _animationMyLocationEnd;

    private readonly ConcurrentHashSet<AnimationEntry<Map>> _animations = new();
    private readonly List<IFeature> _features;
    private AnimationEntry<Map>? _animationMyDirection;
    private AnimationEntry<Map>? _animationMyLocation;

    private BoatShape _boatShape;
    /// <summary>
    /// The Vessel's Shape to be displayed on the map
    /// </summary>
    public BoatShape BoatShape
    {
        get => _boatShape;
        set
        {
            if (_boatShape != value)
            {
                _boatShape = value;
            }
        }
    }

    private bool _isCentered = true;
    /// <summary>
    /// MyLocation is always in the center of the map
    /// </summary>
    public bool IsCentered
    {
        get => _isCentered;
        set
        {
            if (_isCentered != value)
            {
                _isCentered = value;
            }
        }
    }

    private MPoint _myLocation = new(0, 0);
    /// <summary>
    /// Position of location, that is displayed
    /// </summary>
    /// <value>Position of location</value>
    public MPoint MyLocation => _myLocation;
    /// <summary>
    /// Movement direction of device at location
    /// </summary>
    /// <value>Direction at location</value>
    public double Direction { get; private set; } = 0.0;
    /// <summary>
    /// Viewing direction of device (in degrees wrt. north direction)
    /// </summary>
    /// <value>Direction at location</value>

    /// <summary>
    /// Scale of symbol
    /// </summary>
    /// <value>Scale of symbol</value>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// The text that is displayed in the MyLocation callout
    /// (can contain line breaks).
    /// </summary>
    public string CalloutText
    {
        get => _coStyle.Title ?? "";
        set
        {
            _coStyle.Title = value;
            _map.Refresh();
        }
    }

    /// <summary>
    /// Show or hide a callout with further infos next to the MyLocation symbol.
    /// </summary>
    public bool ShowCallout
    {
        get => _coStyle.Enabled;
        set
        {
            _coStyle.Enabled = value;
            _map.Refresh();
        }
    }

    /// <summary>
    /// This event is triggered whenever the MyLocation symbol or label is clicked.
    /// </summary>
    public event EventHandler<MapInfoEventArgs>? Clicked;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Mapsui.Layers.MyLocationLayer"/> class
    /// with a starting location.
    /// </summary>
    /// <param name="map">MapView, to which this layer belongs</param>
    /// <param name="location">Location, where to start</param>
    public BoatShapeLayer(Map map, MPoint location, BoatShape boatShape) : this(map, boatShape)
    {
        _myLocation = location;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Mapsui.Layers.MyLocationLayer"/> class.
    /// </summary>
    /// <param name="map">Map, to which this layer belongs</param>
    public BoatShapeLayer(Map map, BoatShape boatShape)
    {
        _map = map ?? throw new ArgumentNullException("Map shouldn't be null");
        _map.Info += HandleClicked;
        _boatShape = boatShape;
        _boatShape.Refresh();

        Enabled = true;
        IsMapInfoLayer = true;

        _feature = new PointFeature(_myLocation)
        {
            ["Label"] = "MyLocation",
        };

        _locStyle = new BoatStyle
        {
            Enabled = true,
            SymbolRotation = 0,
            Fill = new Brush() { Color = Color.Orange },
            Outline = new Pen() { Color = Color.Black }
        };

        _coStyle = new CalloutStyle
        {
            Enabled = false,
            Type = CalloutType.Single,
            Title = "",
            TitleFontColor = Mapsui.Styles.Color.Black,
            ArrowAlignment = ArrowAlignment.Top,
            ArrowPosition = 0,
            SymbolOffset = new Offset(0, -SymbolStyle.DefaultHeight * 0.4f),
            MaxWidth = 300,
            RotateWithMap = true,
            SymbolOffsetRotatesWithMap = true,
            Color = Mapsui.Styles.Color.White,
            StrokeWidth = 0,
            ShadowWidth = 0
        };

        _feature.Styles.Clear();
        _feature.Styles.Add(_locStyle);
        //_feature.Styles.Add(_coStyle);

        _features = new List<IFeature> { _feature };
        Style = null;
    }

    /// <summary>
    /// Updates own location
    /// </summary>
    /// <param name="newLocation">New location</param>
    public void UpdateMyLocation(MPoint newLocation, bool animated = false)
    {
        if (!MyLocation.Equals(newLocation))
        {
            // We have a location update, so abort last animation
            if (_animationMyLocation != null)
            {
                Animation.Stop(_map, _animationMyLocation, callFinal: true);
                _animations.TryRemove(_animationMyLocation);
                _animationMyLocation = null;
            }

            if (animated)
            {
                // Save values for new animation
                _animationMyLocationStart = MyLocation;
                _animationMyLocationEnd = newLocation;
                var deltaX = _animationMyLocationEnd.X - _animationMyLocationStart.X;
                var deltaY = _animationMyLocationEnd.Y - _animationMyLocationStart.Y;

                if (_map.Navigator.Viewport.ToExtent() is not null)
                {
                    var fetchInfo = new FetchInfo(_map.Navigator.Viewport.ToSection(), _map?.CRS, ChangeType.Discrete);
                    _animationMyLocation = new AnimationEntry<Map>(
                        MyLocation,
                        newLocation,
                        animationStart: 0,
                        animationEnd: 1,
                        tick: (map, entry, v) =>
                        {
                            var modified = InternalUpdateMyLocation(new MPoint(_animationMyLocationStart.X + deltaX * v, _animationMyLocationStart.Y + deltaY * v));
                            return new AnimationResult<Map>(map, true);
                        },
                        final: (map, entry) =>
                        {
                            map.RefreshData(fetchInfo);
                            if (!MyLocation.Equals(_animationMyLocationEnd))
                            {
                                InternalUpdateMyLocation(_animationMyLocationEnd);
                            }

                            return new AnimationResult<Map>(map, false);
                        });

                    Animation.Start(_animationMyLocation, 1000);
                    _animations.Add(_animationMyLocation);

                    // Update viewport
                    if (_isCentered)
                    {
                        _map?.Navigator.CenterOn(_animationMyLocationEnd, 1000, Easing.Linear);
                    }
                }
            }
            else
            {
                var modified = InternalUpdateMyLocation(newLocation);

                // Update viewport
                if (_isCentered)
                {
                    _map?.Navigator.CenterOn(_myLocation);
                }
            }
        }
    }

    /// <summary>
    /// Updates my movement direction
    /// </summary>
    /// <param name="newDirection">New direction</param>
    /// <param name="newViewportRotation">New viewport rotation</param>
    public void UpdateMyDirection(double newDirection, double newViewportRotation, bool animated = false)
    {
        var newRotation = (int)(newDirection - newViewportRotation);
        var oldRotation = (int)_locStyle.SymbolRotation;
        var diffRotation = newDirection - oldRotation;

        if (newRotation != oldRotation)
        {
            Direction = newDirection;

            // We have a direction update, so abort last animation
            if (_animationMyDirection != null)
            {
                Animation.Stop(_map, _animationMyDirection, callFinal: true);
                _animations.TryRemove(_animationMyDirection);
                _animationMyDirection = null;
            }

            if (newRotation < 90 && oldRotation > 270)
            {
                newRotation += 360;
            }
            else if (newRotation > 270 && oldRotation < 90)
            {
                oldRotation += 360;
            }

            var endRotation = newRotation % 360;

            if (animated)
            {
                _animationMyDirection = new AnimationEntry<Map>(
                    oldRotation,
                    newRotation,
                    animationStart: 0,
                    animationEnd: 1,
                    tick: (map, entry, v) =>
                    {
                        var symbolRotation = (oldRotation + (int)(v * diffRotation)) % 360;
                        if ((int)symbolRotation != (int)_locStyle.SymbolRotation)
                        {
                            _locStyle.SymbolRotation = symbolRotation;
                            map.Refresh();
                        }

                        return new AnimationResult<Map>(map, true);
                    },
                    final: (map, v) =>
                    {
                        if ((int)_locStyle.SymbolRotation != (int)endRotation)
                        {
                            _locStyle.SymbolRotation = endRotation;
                            map.Refresh();
                        }

                        return new AnimationResult<Map>(map, false);
                    });

                Animation.Start(_animationMyDirection, 1000);
                _animations.Add(_animationMyDirection);
            }
            else
            {
                _locStyle.SymbolRotation = endRotation;
                _map.Refresh();
            }
        }
    }

    public override bool UpdateAnimations()
    {
        if (_animations.Count > 0)
        {
            var animation = Animation.UpdateAnimations(_map, _animations);
            return animation.IsRunning;
        }

        return base.UpdateAnimations();
    }

    public override IEnumerable<IFeature> GetFeatures(MRect box, double resolution)
    {
        return _features;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _feature.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool InternalUpdateMyLocation(MPoint newLocation)
    {
        var modified = false;

        if (!_myLocation.Equals(newLocation))
        {
            _myLocation = newLocation;
            _feature.Point.X = _myLocation.X;
            _feature.Point.Y = _myLocation.Y;
            modified = true;
        }

        return modified;
    }

    private void HandleClicked(object? sender, MapInfoEventArgs e)
    {
        if (e.MapInfo?.Feature != null && e.MapInfo.Feature.Equals(_feature))
        {
            Clicked?.Invoke(this, e);
        }
    }
}


