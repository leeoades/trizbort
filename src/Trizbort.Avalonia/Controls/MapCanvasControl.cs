using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using CoreLineSegment = Trizbort.Domain.Misc.LineSegment;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaVector = Avalonia.Vector;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Controls;

public enum CanvasDragMode
{
    None,
    Pan,
    Marquee,
    MoveElement,
    MoveResizeHandle,
    MovePort,
    MoveWaypoint
}

public class MapCanvasControl : Control
{
    public static readonly StyledProperty<ICommandService?> CommandServiceProperty =
        AvaloniaProperty.Register<MapCanvasControl, ICommandService?>(nameof(CommandService));

    public ICommandService? CommandService
    {
        get => GetValue(CommandServiceProperty);
        set => SetValue(CommandServiceProperty, value);
    }

    private CoreVector _origin = CoreVector.Zero;
    private float _zoomFactor = 1.0f;
    private readonly CanvasTransform _transform = new();
    private ICommandService? _hookedService;
    private Project? _hookedProject;
    private readonly List<Element> _visibleElements = new();
    private readonly SpatialLineIndex _spatialLines = new();
    private readonly HashSet<Element> _selectedSet = new();
    private int _spatialVersion = 1;
    private int _builtSpatialVersion = 0;

    internal int SpatialVersion => _spatialVersion;
    internal int InvalidationCount { get; private set; }

    // Interaction State Machine
    private CanvasDragMode _dragMode = CanvasDragMode.None;
    private Point _panStartClient;
    private CoreVector _dragOffsetCanvas;
    private CoreVector _dragMarqueeStart;
    private CoreVector _dragMarqueeCurrent;
    private CoreVector _dragResizeHandleLastPosition;
    private ResizeHandle? _dragHandle;
    private ResizeHandle? _hoverHandle;
    private Port? _hoverPort;
    private Element? _hoverElement;
    private CurveWaypoint? _hoverWaypoint;
    private CurveWaypoint _dragWaypoint;
    private CoreVector _dragWaypointOffset;
    private MoveablePort? _dragMovePort;
    private Port? _pendingNewConnectionPort;
    private Point _pendingNewConnectionStartClient;
    private CoreVector _pendingNewConnectionStartWorld;
    private bool _isSpaceKeyDown;
    private bool _isSecondaryButtonRelease;
    private MouseButton? _gestureButton;
    private bool _panMoved;
    private Point _panLastClient;
    private CoreVector _lastRightClickWorldPos;
    private Connection? _dragNewConnection;
    private Port? _dragMovePortOriginalPort;
    private CoreVector _dragMovePortOriginalPosition;
    private readonly List<Port> _activePorts = new();
    private readonly List<ResizeHandle> _activeHandles = new();

    internal CanvasDragMode DragMode => _dragMode;
    internal Element? HoverElement => _hoverElement;
    internal Port? HoverPort => _hoverPort;
    internal ResizeHandle? HoverHandle => _hoverHandle;
    internal CurveWaypoint? HoverWaypoint => _hoverWaypoint;

    // Brushes and Pens for Adorners
    private static readonly IBrush _handleBrush = Brushes.White;
    private static readonly IBrush _hoverHandleBrush = new SolidColorBrush(AvaloniaColor.FromRgb(255, 215, 0));
    private static readonly IPen _handlePen = new Pen(Brushes.Black, 1.0);

    private static readonly IBrush _normalPortBrush = new SolidColorBrush(AvaloniaColor.FromArgb(220, 224, 255, 255));
    private static readonly IPen _normalPortPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(220, 0, 0, 128)), 1.0);
    private static readonly IBrush _hoverPortBrush = new SolidColorBrush(AvaloniaColor.FromArgb(240, 255, 140, 0));
    private static readonly IPen _hoverPortPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(240, 210, 105, 30)), 1.2);

    private static readonly IBrush _setWaypointBrush = new SolidColorBrush(AvaloniaColor.FromArgb(240, 255, 250, 205));
    private static readonly IPen _setWaypointPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(240, 178, 34, 34)), 1.2);
    private static readonly IBrush _ghostWaypointBrush = new SolidColorBrush(AvaloniaColor.FromArgb(140, 224, 255, 255));
    private static readonly IPen _ghostWaypointPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(140, 70, 130, 180)), 1.0);
    private static readonly IBrush _hoverWaypointBrush = new SolidColorBrush(AvaloniaColor.FromArgb(255, 255, 140, 0));
    private static readonly IPen _hoverWaypointPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(255, 210, 105, 30)), 1.5);

    private static readonly IBrush _marqueeFillBrush = new SolidColorBrush(AvaloniaColor.FromArgb(40, 0, 120, 215));
    private static readonly IPen _marqueeBorderPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(200, 0, 120, 215)), 1.0, DashStyle.Dash);

    private static readonly CompassPoint[] _handlePoints =
    [
        CompassPoint.NorthWest, CompassPoint.North, CompassPoint.NorthEast,
        CompassPoint.East, CompassPoint.SouthEast, CompassPoint.South,
        CompassPoint.SouthWest, CompassPoint.West
    ];

    private static readonly CurveWaypoint[] _allWaypoints =
    [
        CurveWaypoint.Quarter, CurveWaypoint.ThreeQuarter, CurveWaypoint.Middle
    ];

    public CoreVector Origin
    {
        get => CommandService?.Origin ?? _origin;
        set
        {
            if (CommandService != null)
            {
                CommandService.SetView(value, CommandService.ZoomFactor);
                return;
            }

            if (_origin != value)
            {
                _origin = value;
                _transform.Origin = value;
                InvalidateVisual();
            }
        }
    }

    public float ZoomFactor
    {
        get => CommandService?.ZoomFactor ?? _zoomFactor;
        set
        {
            var clamped = Math.Clamp(value, 0.1f, 10.0f);
            if (CommandService != null)
            {
                CommandService.SetView(CommandService.Origin, clamped);
                return;
            }

            if (Math.Abs(_zoomFactor - clamped) > 0.0001f)
            {
                _zoomFactor = clamped;
                _transform.ZoomFactor = clamped;
                InvalidateVisual();
            }
        }
    }

    public event EventHandler? ViewChanged;

    public MapCanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public new void InvalidateVisual()
    {
        InvalidationCount++;
        base.InvalidateVisual();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Project.ProjectChanged += OnProjectChanged;
        Settings.Changed += OnSettingsChanged;
        HookProject(Project.Current);
        TryHookCommandService();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Project.ProjectChanged -= OnProjectChanged;
        Settings.Changed -= OnSettingsChanged;
        UnhookProject(_hookedProject);
        TryUnhookCommandService();
    }

    private void HookProject(Project? project)
    {
        if (project == null || ReferenceEquals(_hookedProject, project))
        {
            return;
        }

        UnhookProject(_hookedProject);
        _hookedProject = project;
        project.Elements.Added += OnElementAdded;
        project.Elements.Removed += OnElementRemoved;

        foreach (var element in project.Elements)
        {
            element.Changed += OnElementChanged;
        }
    }

    private void UnhookProject(Project? project)
    {
        if (project == null) return;

        project.Elements.Added -= OnElementAdded;
        project.Elements.Removed -= OnElementRemoved;

        foreach (var element in project.Elements)
        {
            element.Changed -= OnElementChanged;
        }

        if (ReferenceEquals(_hookedProject, project))
        {
            _hookedProject = null;
        }
    }

    private void OnElementAdded(object? sender, ItemEventArgs<Element> e)
    {
        if (e.Item != null)
        {
            e.Item.Changed += OnElementChanged;
        }
        _spatialVersion++;
        if (_dragMode == CanvasDragMode.None)
        {
            RecreatePorts();
            RecreateHandles();
        }
        InvalidateVisual();
    }

    private void OnElementRemoved(object? sender, ItemEventArgs<Element> e)
    {
        if (e.Item != null)
        {
            e.Item.Changed -= OnElementChanged;
        }
        _spatialVersion++;
        if (ReferenceEquals(_hoverElement, e.Item))
        {
            _hoverElement = null;
            ToolTip.SetTip(this, null);
        }
        if (_dragMode == CanvasDragMode.None)
        {
            RecreatePorts();
            RecreateHandles();
        }
        InvalidateVisual();
    }

    private void OnElementChanged(object? sender, EventArgs e)
    {
        _spatialVersion++;
        if (_dragMode == CanvasDragMode.None)
        {
            RecreatePorts();
            RecreateHandles();
        }
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CommandServiceProperty)
        {
            TryUnhookCommandService();
            TryHookCommandService();
        }
        else if (change.Property == BoundsProperty)
        {
            var bounds = Bounds;
            _transform.ViewportWidth = bounds.Width;
            _transform.ViewportHeight = bounds.Height;
            CommandService?.SetViewportSize(bounds.Width, bounds.Height);
            InvalidateVisual();
        }
    }

    private void TryHookCommandService()
    {
        if (!this.IsAttachedToVisualTree() || CommandService == null || ReferenceEquals(_hookedService, CommandService))
        {
            return;
        }

        _hookedService = CommandService;
        _hookedService.CanvasInvalidated += OnCommandServiceCanvasInvalidated;
        _hookedService.ZoomChanged += OnCommandServiceZoomChanged;
        _hookedService.SelectionChanged += OnCommandServiceSelectionChanged;
        _hookedService.ProjectChanged += OnCommandServiceProjectChanged;

        SyncViewFromService(_hookedService);
    }

    private void TryUnhookCommandService()
    {
        if (_hookedService == null)
        {
            return;
        }

        _hookedService.CanvasInvalidated -= OnCommandServiceCanvasInvalidated;
        _hookedService.ZoomChanged -= OnCommandServiceZoomChanged;
        _hookedService.SelectionChanged -= OnCommandServiceSelectionChanged;
        _hookedService.ProjectChanged -= OnCommandServiceProjectChanged;
        _hookedService = null;
    }

    private void OnCommandServiceCanvasInvalidated(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void OnCommandServiceZoomChanged(object? sender, float zoom)
    {
        if (Math.Abs(_zoomFactor - zoom) > 0.0001f)
        {
            _zoomFactor = zoom;
            _transform.ZoomFactor = zoom;
            InvalidateVisual();
        }
    }

    private void OnCommandServiceSelectionChanged(object? sender, EventArgs e)
    {
        _selectedSet.Clear();
        if (Project.SelectedElementsProvider != null)
        {
            foreach (var elem in Project.SelectedElementsProvider())
            {
                _selectedSet.Add(elem);
            }
        }
        if (_dragMode != CanvasDragMode.MoveResizeHandle && _dragMode != CanvasDragMode.MovePort)
        {
            RecreatePorts();
            RecreateHandles();
            UpdateCursor();
        }
        InvalidateVisual();
    }

    private void OnCommandServiceProjectChanged(object? sender, EventArgs e)
    {
        _spatialVersion++;
        RecreatePorts();
        RecreateHandles();
        InvalidateVisual();
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs e)
    {
        if (e.OldProject != null)
        {
            UnhookProject(e.OldProject);
        }
        if (e.NewProject != null)
        {
            HookProject(e.NewProject);
        }
        _spatialVersion++;
        RecreatePorts();
        RecreateHandles();
        InvalidateVisual();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _cachedGridGeometry = null;
        if (CommandService != null)
        {
            SyncViewFromService(CommandService);
        }
        InvalidateVisual();
    }

    private void SyncViewFromService(ICommandService service)
    {
        _origin = service.Origin;
        _zoomFactor = service.ZoomFactor;
        _transform.Origin = _origin;
        _transform.ZoomFactor = _zoomFactor;

        _selectedSet.Clear();
        if (Project.SelectedElementsProvider != null)
        {
            foreach (var elem in Project.SelectedElementsProvider())
            {
                _selectedSet.Add(elem);
            }
        }
        RecreatePorts();
        RecreateHandles();
    }

    #region Coordinate Transformations

    public Point CanvasToClient(CoreVector world)
    {
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        return _transform.CanvasToClient(world).ToAvaloniaPoint();
    }

    public CoreVector ClientToCanvas(Point screen)
    {
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        return _transform.ClientToCanvas(screen.ToVector());
    }

    public void ZoomAt(Point cursorScreen, float newZoom)
    {
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        _transform.ZoomAt(cursorScreen.ToVector(), newZoom);

        if (CommandService != null)
        {
            CommandService.SetView(_transform.Origin, _transform.ZoomFactor);
        }
        else
        {
            Origin = _transform.Origin;
            ZoomFactor = _transform.ZoomFactor;
        }
    }

    public void FitToMap()
    {
        if (CommandService != null)
        {
            CommandService.FitToMap();
        }
        else
        {
            _transform.FitToMap(Project.Current.Elements);
            _origin = _transform.Origin;
            _zoomFactor = _transform.ZoomFactor;
            InvalidateVisual();
        }
    }

    #endregion

    #region State Machine & Hit Testing

    private void RecreateHandles()
    {
        _activeHandles.Clear();
        if (_selectedSet.Count == 1)
        {
            var single = _selectedSet.First();
            if (single is ISizeable sizeable)
            {
                foreach (var cp in _handlePoints)
                {
                    _activeHandles.Add(new ResizeHandle(cp, sizeable));
                }
            }
        }
        if (_hoverHandle != null && !_activeHandles.Contains(_hoverHandle))
        {
            _hoverHandle = null;
        }
    }

    private void RecreatePorts()
    {
        _activePorts.Clear();

        if (_dragMode == CanvasDragMode.MovePort)
        {
            if (_hoverElement is Room or MapLabel)
            {
                _activePorts.AddRange(_hoverElement.PortList);
            }
        }
        else
        {
            if ((_hoverElement is Room || _hoverElement is MapLabel) && !_selectedSet.Contains(_hoverElement))
            {
                if (_selectedSet.Count == 0)
                {
                    _activePorts.AddRange(_hoverElement.PortList);
                }
            }

            if (_selectedSet.Count == 1 && _selectedSet.First() is Connection conn)
            {
                _activePorts.AddRange(conn.PortList.OfType<MoveablePort>());
            }
        }

        if (_hoverPort != null && !_activePorts.Contains(_hoverPort))
        {
            _hoverPort = null;
        }
    }

    private IEnumerable<CurveWaypoint> GetVisibleWaypoints(Connection connection)
    {
        foreach (var wp in _allWaypoints)
        {
            if (connection.GetCurveWaypoint(wp).HasValue || connection.CanAddCurveWaypoint(wp))
            {
                yield return wp;
            }
        }
    }

    private CoreRect GetWaypointBounds(Connection connection, CurveWaypoint waypoint)
    {
        var isSet = connection.GetCurveWaypoint(waypoint).HasValue;
        var size = Settings.HandleSize * (isSet ? 2.0f : 1.5f);
        var pos = connection.GetCurveWaypointHandlePosition(waypoint);
        return new CoreRect(pos.X - size / 2, pos.Y - size / 2, size, size);
    }

    private CurveWaypoint? HitTestWaypoint(CoreVector canvasPos)
    {
        if (_selectedSet.Count == 1 && _selectedSet.First() is Connection conn)
        {
            foreach (var wp in GetVisibleWaypoints(conn))
            {
                var bounds = GetWaypointBounds(conn, wp);
                bounds.Inflate(Settings.HandleSize / 2f);
                if (bounds.Contains(canvasPos))
                {
                    return wp;
                }
            }
        }
        return null;
    }

    private ResizeHandle? HitTestHandle(CoreVector canvasPos)
    {
        for (var i = _activeHandles.Count - 1; i >= 0; i--)
        {
            var handle = _activeHandles[i];
            if (handle.HitTest(canvasPos))
            {
                return handle;
            }
        }
        return null;
    }

    private Port? HitTestPort(CoreVector canvasPos)
    {
        Port? closest = null;
        var closestDistance = float.MaxValue;
        var snapDistance = Settings.SnapToElementSize;

        foreach (var port in _activePorts)
        {
            if (_dragMode == CanvasDragMode.MovePort && port == _dragMovePort)
                continue;

            var distance = port.Distance(canvasPos);
            var curSnap = snapDistance;
            var bounds = port.Owner.UnionBoundsWith(CoreRect.Empty, true);
            if (bounds.Contains(canvasPos))
            {
                curSnap = _dragMode == CanvasDragMode.MovePort ? float.MaxValue : 0;
            }

            if (distance <= curSnap && distance < closestDistance)
            {
                closest = port;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private Element? HitTestElement(CoreVector canvasPos, bool includeMargins = false)
    {
        Element? closest = null;
        var closestDistance = float.MaxValue;
        var elements = Project.Current.Elements;

        for (var i = elements.Count - 1; i >= 0; i--)
        {
            var elem = elements[i];
            if (_dragMode == CanvasDragMode.MovePort && _dragMovePort?.Owner == elem)
                continue;

            var distance = elem.Distance(canvasPos, includeMargins);
            if (distance <= Settings.SnapToElementSize && distance < closestDistance)
            {
                closest = elem;
                closestDistance = distance;
            }
        }

        return closest;
    }

    internal StandardCursorType CursorType { get; private set; } = StandardCursorType.Arrow;

    private void UpdateCursor()
    {
        CursorType = DetermineCursorType();
        Cursor = new Cursor(CursorType);
    }

    private StandardCursorType DetermineCursorType()
    {
        if (_dragMode == CanvasDragMode.Pan || _isSpaceKeyDown)
        {
            return StandardCursorType.Hand;
        }
        if (_dragMode == CanvasDragMode.MoveWaypoint || (_dragMode == CanvasDragMode.None && _hoverWaypoint.HasValue))
        {
            return StandardCursorType.SizeAll;
        }
        if (_dragMode == CanvasDragMode.MovePort || (_hoverPort != null && _hoverPort is not MoveablePort))
        {
            return StandardCursorType.Cross;
        }
        if (_hoverPort is MoveablePort)
        {
            return StandardCursorType.Hand;
        }
        if (_hoverHandle != null)
        {
            return _hoverHandle.CompassPoint switch
            {
                CompassPoint.NorthWest or CompassPoint.SouthEast => StandardCursorType.TopLeftCorner,
                CompassPoint.NorthEast or CompassPoint.SouthWest => StandardCursorType.TopRightCorner,
                CompassPoint.North or CompassPoint.South => StandardCursorType.SizeNorthSouth,
                CompassPoint.East or CompassPoint.West => StandardCursorType.SizeWestEast,
                _ => StandardCursorType.Arrow
            };
        }
        if (_hoverElement != null && _selectedSet.Contains(_hoverElement) && _hoverElement is IMoveable)
        {
            return StandardCursorType.SizeAll;
        }
        return StandardCursorType.Arrow;
    }

    private void UpdateToolTip()
    {
        ToolTip.SetTip(this, BuildToolTipText(_hoverElement));
    }

    internal static string? BuildToolTipText(Element? element)
    {
        if (element == null || !ApplicationSettingsController.AppSettings.ShowTooltips || !element.HasTooltip())
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var part in new[] { element.GetToolTipHeader(), element.GetToolTipText(), element.GetToolTipFooter() })
        {
            if (!string.IsNullOrWhiteSpace(part)) parts.Add(part);
        }
        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }

    #endregion

    #region Pointer & Keyboard Handlers

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        if (IsGestureActive)
        {
            // A drag (or a not-yet-started connection drag) owns the pointer; ignore other buttons.
            e.Handled = true;
            return;
        }

        var point = e.GetCurrentPoint(this);
        var clientPos = point.Position;
        var worldPos = ClientToCanvas(clientPos);

        var isMiddle = point.Properties.IsMiddleButtonPressed;
        var isRight = point.Properties.IsRightButtonPressed;
        var isLeft = point.Properties.IsLeftButtonPressed;
        var isShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        var isCtrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        _gestureButton = isMiddle ? MouseButton.Middle : isRight ? MouseButton.Right : MouseButton.Left;

        if (isRight)
        {
            _panMoved = false;
            _lastRightClickWorldPos = worldPos;
        }

        if (isMiddle || isRight || (isLeft && _isSpaceKeyDown))
        {
            _dragMode = CanvasDragMode.Pan;
            _panStartClient = clientPos;
            _panLastClient = clientPos;
            e.Pointer.Capture(this);
            UpdateCursor();
            e.Handled = true;
            return;
        }

        if (isLeft)
        {
            if (e.ClickCount == 2)
            {
                var hitDoubleElem = HitTestElement(worldPos);
                if (hitDoubleElem != null)
                {
                    CommandService?.Select([hitDoubleElem], clearExisting: true);
                    _ = ShowPropertiesDialogAsync(hitDoubleElem);
                    e.Handled = true;
                    return;
                }
                else if (ApplicationSettingsController.AppSettings.DoubleClickToAddRoom)
                {
                    CommandService?.AddRoom(worldPos);
                    e.Handled = true;
                    return;
                }
            }

            var hitHandle = HitTestHandle(worldPos);
            if (hitHandle != null)
            {
                _dragMode = CanvasDragMode.MoveResizeHandle;
                _dragHandle = hitHandle;
                _hoverHandle = hitHandle;
                _dragResizeHandleLastPosition = worldPos;
                e.Pointer.Capture(this);
                UpdateCursor();
                e.Handled = true;
                return;
            }

            var hitPort = HitTestPort(worldPos);
            if (hitPort != null)
            {
                if (hitPort is MoveablePort mp)
                {
                    _dragMode = CanvasDragMode.MovePort;
                    _dragMovePort = mp;
                    _dragMovePortOriginalPort = mp.DockedAt;
                    _dragMovePortOriginalPosition = mp.Position;
                    _dragOffsetCanvas = Settings.Snap(worldPos - mp.Position);
                    e.Pointer.Capture(this);
                    UpdateCursor();
                    e.Handled = true;
                    return;
                }
                else
                {
                    _pendingNewConnectionPort = hitPort;
                    _pendingNewConnectionStartClient = clientPos;
                    _pendingNewConnectionStartWorld = worldPos;
                    _dragMovePortOriginalPort = null;
                    _dragMovePortOriginalPosition = CoreVector.Zero;
                    e.Pointer.Capture(this);
                    UpdateCursor();
                    e.Handled = true;
                    return;
                }
            }

            var hitWp = HitTestWaypoint(worldPos);
            if (hitWp.HasValue && _selectedSet.Count == 1 && _selectedSet.First() is Connection conn)
            {
                _dragMode = CanvasDragMode.MoveWaypoint;
                _dragWaypoint = hitWp.Value;
                var handlePos = conn.GetCurveWaypointHandlePosition(_dragWaypoint);
                _dragWaypointOffset = handlePos - worldPos;
                e.Pointer.Capture(this);
                UpdateCursor();
                e.Handled = true;
                return;
            }

            var hitElem = HitTestElement(worldPos);
            if (hitElem != null)
            {
                var alreadySelected = _selectedSet.Contains(hitElem);
                if (isShift || isCtrl)
                {
                    CommandService?.Select(hitElem, toggle: true);
                }
                else if (!alreadySelected)
                {
                    CommandService?.Select([hitElem], clearExisting: true);
                }

                if (_selectedSet.Contains(hitElem))
                {
                    _dragMode = CanvasDragMode.MoveElement;
                    _dragOffsetCanvas = Settings.Snap(worldPos);
                    e.Pointer.Capture(this);
                }
                UpdateCursor();
                e.Handled = true;
                return;
            }

            if (!isShift && !isCtrl)
            {
                CommandService?.ClearSelection();
            }
            _dragMode = CanvasDragMode.Marquee;
            _dragMarqueeStart = worldPos;
            _dragMarqueeCurrent = worldPos;
            e.Pointer.Capture(this);
            UpdateCursor();
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var clientPos = e.GetPosition(this);
        var worldPos = ClientToCanvas(clientPos);

        if (_pendingNewConnectionPort != null)
        {
            var dx = clientPos.X - _pendingNewConnectionStartClient.X;
            var dy = clientPos.Y - _pendingNewConnectionStartClient.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist >= Settings.DragDistanceToInitiateNewConnection)
            {
                var port = _pendingNewConnectionPort;
                _pendingNewConnectionPort = null;
                var newConn = new Connection(Project.Current, new Vertex(port), new Vertex(port))
                {
                    Style = ConnectionStyle.Solid,
                    Flow = ConnectionFlow.TwoWay
                };
                Project.Current.Elements.Add(newConn);
                _dragNewConnection = newConn;
                _dragMode = CanvasDragMode.MovePort;
                _dragMovePort = (MoveablePort)newConn.PortList[1];
                _dragOffsetCanvas = Settings.Snap(_pendingNewConnectionStartWorld - newConn.VertexList[0].Position);
                _hoverPort = null;
                CommandService?.Select(newConn);
            }
        }

        switch (_dragMode)
        {
            case CanvasDragMode.Pan:
                if (!_panMoved)
                {
                    var deltaFromStart = clientPos - _panStartClient;
                    if (Math.Abs(deltaFromStart.X) > 2 || Math.Abs(deltaFromStart.Y) > 2)
                    {
                        _panMoved = true;
                    }
                }

                var deltaScreen = clientPos - _panLastClient;
                _panLastClient = clientPos;
                Origin = new CoreVector((float)(Origin.X - deltaScreen.X / ZoomFactor), (float)(Origin.Y - deltaScreen.Y / ZoomFactor));
                e.Handled = true;
                break;

            case CanvasDragMode.MoveElement:
                var snappedWorld = Settings.Snap(worldPos);
                var moveDelta = snappedWorld - _dragOffsetCanvas;
                if (moveDelta != CoreVector.Zero)
                {
                    MapEditing.Move(Project.Current.Elements, _selectedSet.ToList(), moveDelta);
                    _dragOffsetCanvas += moveDelta;
                    Project.Current.IsDirty = true;
                    _spatialVersion++;
                    InvalidateVisual();
                }
                e.Handled = true;
                break;

            case CanvasDragMode.MoveResizeHandle:
                if (_dragHandle != null)
                {
                    _dragResizeHandleLastPosition = MapEditing.Resize(_dragHandle, _dragResizeHandleLastPosition, worldPos);
                    Project.Current.IsDirty = true;
                    _spatialVersion++;
                    InvalidateVisual();
                }
                e.Handled = true;
                break;

            case CanvasDragMode.MovePort:
                if (_dragMovePort != null)
                {
                    var targetElem = HitTestElement(worldPos, true);
                    if (!ReferenceEquals(targetElem, _hoverElement))
                    {
                        _hoverElement = targetElem;
                        RecreatePorts();
                    }

                    _hoverPort = HitTestPort(worldPos);
                    if (_hoverPort != null && _hoverPort != _dragMovePort)
                    {
                        if (_hoverPort is not MoveablePort)
                        {
                            _dragMovePort.DockAt(_hoverPort);
                        }
                        else
                        {
                            _dragMovePort.DockAt(null);
                            _dragMovePort.SetPosition(Settings.Snap(worldPos) - _dragOffsetCanvas);
                        }
                    }
                    else
                    {
                        _dragMovePort.DockAt(null);
                        _dragMovePort.SetPosition(Settings.Snap(worldPos) - _dragOffsetCanvas);
                    }
                    Project.Current.IsDirty = true;
                    _spatialVersion++;
                    InvalidateVisual();
                }
                e.Handled = true;
                break;

            case CanvasDragMode.MoveWaypoint:
                if (_selectedSet.Count == 1 && _selectedSet.First() is Connection conn)
                {
                    conn.SetCurveWaypoint(_dragWaypoint, Settings.Snap(worldPos + _dragWaypointOffset));
                    Project.Current.IsDirty = true;
                    _spatialVersion++;
                    InvalidateVisual();
                }
                e.Handled = true;
                break;

            case CanvasDragMode.Marquee:
                _dragMarqueeCurrent = worldPos;
                InvalidateVisual();
                e.Handled = true;
                break;

            case CanvasDragMode.None:
                if (_pendingNewConnectionPort == null)
                {
                    var prevHoverElem = _hoverElement;
                    var prevHoverPort = _hoverPort;
                    var prevHoverHandle = _hoverHandle;
                    var prevHoverWp = _hoverWaypoint;

                    var newHoverElem = HitTestElement(worldPos);
                    if (!ReferenceEquals(newHoverElem, _hoverElement))
                    {
                        _hoverElement = newHoverElem;
                        RecreatePorts();
                        RecreateHandles();
                        UpdateToolTip();
                    }

                    _hoverWaypoint = HitTestWaypoint(worldPos);
                    _hoverHandle = HitTestHandle(worldPos);
                    _hoverPort = HitTestPort(worldPos);

                    UpdateCursor();

                    if (!ReferenceEquals(prevHoverElem, _hoverElement) ||
                        !ReferenceEquals(prevHoverPort, _hoverPort) ||
                        !ReferenceEquals(prevHoverHandle, _hoverHandle) ||
                        prevHoverWp != _hoverWaypoint)
                    {
                        InvalidateVisual();
                    }
                }
                break;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var releasedButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind.GetMouseButton();
        if (IsGestureActive && _gestureButton.HasValue && releasedButton != _gestureButton.Value)
        {
            // Releasing a secondary button must not end the gesture owned by another button.
            // Avalonia MouseDevice drops pointer capture in its finally block after this event.
            // Mark secondary release and schedule re-capture so the primary gesture continues unbroken.
            _isSecondaryButtonRelease = true;
            var pointer = e.Pointer;
            Dispatcher.UIThread.Post(() =>
            {
                if (IsGestureActive && VisualRoot != null)
                {
                    pointer.Capture(this);
                }
            });
            return;
        }

        var wasPan = _dragMode == CanvasDragMode.Pan;
        var wasRight = releasedButton == MouseButton.Right;
        var panMoved = _panMoved;
        var rightWorld = _lastRightClickWorldPos;

        EndDrag(e.KeyModifiers, cancelled: false);
        e.Pointer.Capture(null);

        if (wasPan && wasRight && !panMoved)
        {
            ShowContextMenuAt(rightWorld);
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_isSecondaryButtonRelease)
        {
            _isSecondaryButtonRelease = false;
            return;
        }

        EndDrag(KeyModifiers.None, cancelled: true);
    }

    internal bool IsGestureActive => _dragMode != CanvasDragMode.None || _pendingNewConnectionPort != null;

    private static bool IsDegenerateConnection(Connection connection)
    {
        if (connection.VertexList.Count != 2) return false;
        var v0 = connection.VertexList[0];
        var v1 = connection.VertexList[1];

        // Same port on both ends is always degenerate
        if (v0.Port != null && v0.Port == v1.Port) return true;

        // If both ends are docked to different ports, it is a valid connection between ports (even if positions touch)
        if (v0.Port != null && v1.Port != null && v0.Port != v1.Port) return false;

        // If one or both ends are undocked and the distance is negligible, it is degenerate
        return v0.Position.Distance(v1.Position) < Numeric.Small;
    }

    /// <summary>
    /// Single completion/cancel path for every drag gesture (pointer release, capture loss, Escape).
    /// Never leaves an invalid zero-length or same-port connection behind, and restores original
    /// connectivity on cancelled existing connection edits.
    /// </summary>
    private void EndDrag(KeyModifiers modifiers, bool cancelled)
    {
        var mode = _dragMode;
        var movePort = _dragMovePort;
        var newConnection = _dragNewConnection;
        var origPort = _dragMovePortOriginalPort;
        var origPos = _dragMovePortOriginalPosition;

        // Reset state first so re-entrant events (e.g. capture lost) see an idle canvas.
        _dragMode = CanvasDragMode.None;
        _dragHandle = null;
        _dragMovePort = null;
        _dragNewConnection = null;
        _dragMovePortOriginalPort = null;
        _dragMovePortOriginalPosition = CoreVector.Zero;
        _pendingNewConnectionPort = null;
        _gestureButton = null;
        _isSecondaryButtonRelease = false;

        if (mode == CanvasDragMode.MovePort && movePort?.Owner is Connection connection)
        {
            var isCreatedByDrag = ReferenceEquals(connection, newConnection);
            if (cancelled)
            {
                if (isCreatedByDrag)
                {
                    Project.Current.Elements.Remove(connection);
                    CommandService?.ClearSelection();
                    _hoverElement = null;
                    _hoverPort = null;
                }
                else
                {
                    // Restore existing connection endpoint to its original docked port or position
                    if (origPort != null)
                    {
                        movePort.DockAt(origPort);
                    }
                    else
                    {
                        movePort.DockAt(null);
                        movePort.SetPosition(origPos);
                    }
                    CommandService?.Select(connection);
                }
            }
            else
            {
                var discard = IsDegenerateConnection(connection);
                if (discard)
                {
                    if (isCreatedByDrag)
                    {
                        Project.Current.Elements.Remove(connection);
                        CommandService?.ClearSelection();
                        _hoverElement = null;
                        _hoverPort = null;
                    }
                    else
                    {
                        // Restore existing connection endpoint to its original docked port or position
                        if (origPort != null)
                        {
                            movePort.DockAt(origPort);
                        }
                        else
                        {
                            movePort.DockAt(null);
                            movePort.SetPosition(origPos);
                        }
                        CommandService?.Select(connection);
                    }
                }
                else
                {
                    CommandService?.Select(connection);
                }
            }
            Project.Current.IsDirty = true;
        }
        else if (mode == CanvasDragMode.Marquee && !cancelled)
        {
            var minX = Math.Min(_dragMarqueeStart.X, _dragMarqueeCurrent.X);
            var maxX = Math.Max(_dragMarqueeStart.X, _dragMarqueeCurrent.X);
            var minY = Math.Min(_dragMarqueeStart.Y, _dragMarqueeCurrent.Y);
            var maxY = Math.Max(_dragMarqueeStart.Y, _dragMarqueeCurrent.Y);
            var rect = new CoreRect(minX, minY, Math.Max(0.01f, maxX - minX), Math.Max(0.01f, maxY - minY));

            var hitElements = Project.Current.Elements.Where(elem => elem.Intersects(rect)).ToList();
            var isShiftOrCtrl = (modifiers & (KeyModifiers.Shift | KeyModifiers.Control)) != 0;

            if (!isShiftOrCtrl)
            {
                CommandService?.Select(hitElements, clearExisting: true);
            }
            else
            {
                foreach (var elem in hitElements)
                {
                    if (_selectedSet.Contains(elem))
                    {
                        CommandService?.Deselect(elem);
                    }
                    else
                    {
                        CommandService?.Select(elem, addToSelection: true);
                    }
                }
            }
        }

        if (mode != CanvasDragMode.None)
        {
            _spatialVersion++;
        }
        RecreatePorts();
        RecreateHandles();
        UpdateCursor();
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var cursorScreen = e.GetPosition(this);
        var zoomDelta = e.Delta.Y > 0 ? 1.25f : 0.8f;
        var newZoom = ZoomFactor * zoomDelta;

        ZoomAt(cursorScreen, newZoom);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Space)
        {
            _isSpaceKeyDown = true;
            UpdateCursor();
            e.Handled = true;
            return;
        }

        var isCtrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        var isShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        var isAlt = (e.KeyModifiers & KeyModifiers.Alt) != 0;

        switch (e.Key)
        {
            case Key.Delete or Key.Back:
                if (!IsGestureActive) CommandService?.DeleteSelected();
                e.Handled = true;
                break;

            case Key.A when isCtrl:
                CommandService?.SelectAll();
                e.Handled = true;
                break;

            case Key.Escape:
                if (IsGestureActive)
                {
                    EndDrag(KeyModifiers.None, cancelled: true);
                }
                else
                {
                    CommandService?.ClearSelection();
                }
                e.Handled = true;
                break;

            case Key.OemPlus or Key.Add:
                CommandService?.ZoomIn();
                e.Handled = true;
                break;

            case Key.OemMinus or Key.Subtract:
                CommandService?.ZoomOut();
                e.Handled = true;
                break;

            case Key.D0 or Key.NumPad0 when isCtrl:
                CommandService?.ResetZoom();
                e.Handled = true;
                break;

            case Key.C when isCtrl:
                _ = CopySelectedToClipboardAsync();
                e.Handled = true;
                break;

            case Key.X when isCtrl:
                _ = CutSelectedToClipboardAsync();
                e.Handled = true;
                break;

            case Key.V when isCtrl:
                _ = PasteFromClipboardAsync();
                e.Handled = true;
                break;

            case Key.Left or Key.Right or Key.Up or Key.Down:
                if (_selectedSet.Count > 0)
                {
                    HandleArrowKey(e.Key, isCtrl, isShift, isAlt);
                    e.Handled = true;
                }
                break;
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        if (_isSpaceKeyDown)
        {
            _isSpaceKeyDown = false;
            UpdateCursor();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == Key.Space)
        {
            _isSpaceKeyDown = false;
            UpdateCursor();
            e.Handled = true;
        }
    }

    private void HandleArrowKey(Key key, bool isCtrl, bool isShift, bool isAlt)
    {
        if (isAlt && isCtrl)
        {
            var delta = Settings.SnapToGrid ? Settings.GridSize : 2.0f;
            foreach (var sizeable in _selectedSet.OfType<ISizeable>())
            {
                switch (key)
                {
                    case Key.Left:
                        if (sizeable.Width - delta >= Settings.GridSize)
                            sizeable.Size = new CoreVector(sizeable.Width - delta, sizeable.Height);
                        break;
                    case Key.Right:
                        sizeable.Size = new CoreVector(sizeable.Width + delta, sizeable.Height);
                        break;
                    case Key.Up:
                        if (sizeable.Height - delta >= Settings.GridSize)
                            sizeable.Size = new CoreVector(sizeable.Width, sizeable.Height - delta);
                        break;
                    case Key.Down:
                        sizeable.Size = new CoreVector(sizeable.Width, sizeable.Height + delta);
                        break;
                }
            }
            Project.Current.IsDirty = true;
            _spatialVersion++;
            InvalidateVisual();
        }
        else
        {
            var step = isShift ? Settings.GridSize * 2 : (Settings.SnapToGrid ? Settings.GridSize : 2.0f);
            var moveDelta = key switch
            {
                Key.Left => new CoreVector(-step, 0),
                Key.Right => new CoreVector(step, 0),
                Key.Up => new CoreVector(0, -step),
                Key.Down => new CoreVector(0, step),
                _ => CoreVector.Zero
            };

            if (moveDelta != CoreVector.Zero && _selectedSet.Count > 0)
            {
                CommandService?.NudgeSelected(moveDelta);
            }
        }
    }

    public async Task CopySelectedToClipboardAsync()
    {
        try
        {
            var text = CommandService?.CopySelected();
            if (!string.IsNullOrEmpty(text))
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Copy failed: {ex}");
        }
    }

    public async Task CutSelectedToClipboardAsync()
    {
        try
        {
            var text = CommandService?.CutSelected();
            if (!string.IsNullOrEmpty(text))
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cut failed: {ex}");
        }
    }

    public async Task PasteFromClipboardAsync()
    {
        try
        {
            string? text = null;
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                text = await topLevel.Clipboard.GetTextAsync();
            }
            CommandService?.Paste(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Paste failed: {ex}");
        }
    }

    private void ShowContextMenuAt(CoreVector worldPos)
    {
        var hitElem = HitTestElement(worldPos);
        if (hitElem != null && !_selectedSet.Contains(hitElem))
        {
            CommandService?.Select([hitElem], clearExisting: true);
        }

        var menu = new ContextMenu();
        var items = new List<Control>();

        var selectedElements = Project.Current.GetSelectedElements();
        var activeElement = selectedElements.Count == 1 ? selectedElements[0] : (hitElem ?? Project.Current.ActiveSelectedElement);

        if (activeElement != null)
        {
            var propertiesItem = new MenuItem { Header = "_Properties..." };
            propertiesItem.Click += (_, _) => _ = ShowPropertiesDialogAsync(activeElement);
            items.Add(propertiesItem);
            items.Add(new Separator());
        }

        if (selectedElements.Count > 0)
        {
            var cutItem = new MenuItem { Header = "Cu_t" };
            cutItem.Click += async (_, _) => await CutSelectedToClipboardAsync();
            items.Add(cutItem);

            var copyItem = new MenuItem { Header = "_Copy" };
            copyItem.Click += async (_, _) => await CopySelectedToClipboardAsync();
            items.Add(copyItem);

            var deleteItem = new MenuItem { Header = "_Delete" };
            deleteItem.Click += (_, _) => CommandService?.DeleteSelected();
            items.Add(deleteItem);

            items.Add(new Separator());
        }

        var pasteItem = new MenuItem { Header = "_Paste" };
        pasteItem.Click += async (_, _) => await PasteFromClipboardAsync();
        items.Add(pasteItem);

        items.Add(new Separator());

        var addRoomItem = new MenuItem { Header = "Add _Room" };
        addRoomItem.Click += (_, _) => CommandService?.AddRoom(worldPos);
        items.Add(addRoomItem);

        var addLabelItem = new MenuItem { Header = "Add _Map Label" };
        addLabelItem.Click += (_, _) => CommandService?.AddMapLabel(worldPos);
        items.Add(addLabelItem);

        items.Add(new Separator());

        var mapSettingsItem = new MenuItem { Header = "_Map Settings..." };
        mapSettingsItem.Click += (_, _) => _ = ShowMapSettingsDialogAsync();
        items.Add(mapSettingsItem);

        var mapThemeItem = new MenuItem { Header = "Map _Theme..." };
        mapThemeItem.Click += (_, _) => _ = ShowMapThemeDialogAsync();
        items.Add(mapThemeItem);

        var appSettingsItem = new MenuItem { Header = "_Application Settings..." };
        appSettingsItem.Click += (_, _) => _ = ShowAppSettingsDialogAsync();
        items.Add(appSettingsItem);

        menu.ItemsSource = items;
        LastContextMenu = menu;
        menu.Open(this);
    }

    internal ContextMenu? LastContextMenu { get; private set; }

    internal Func<Element, Task<bool>>? PropertiesDialogOverride { get; set; }

    public async Task<bool> ShowPropertiesDialogAsync(Element element)
    {
        if (PropertiesDialogOverride != null)
        {
            return await PropertiesDialogOverride(element);
        }
        var window = TopLevel.GetTopLevel(this) as Window;
        var dialogService = App.Services?.GetService<IDialogService>();
        if (dialogService != null)
        {
            var changed = await dialogService.ShowElementPropertiesAsync(window, element);
            if (changed)
            {
                CommandService?.InvalidateCanvas();
                InvalidateVisual();
                return true;
            }
        }
        return false;
    }

    public async Task<bool> ShowMapSettingsDialogAsync()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var dialogService = App.Services?.GetService<IDialogService>();
        if (dialogService != null)
        {
            var changed = await dialogService.ShowMapSettingsAsync(window);
            if (changed)
            {
                CommandService?.InvalidateCanvas();
                InvalidateVisual();
                return true;
            }
        }
        return false;
    }

    public async Task<bool> ShowMapThemeDialogAsync()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var dialogService = App.Services?.GetService<IDialogService>();
        if (dialogService != null)
        {
            var changed = await dialogService.ShowMapThemeAsync(window);
            if (changed)
            {
                CommandService?.InvalidateCanvas();
                InvalidateVisual();
                return true;
            }
        }
        return false;
    }

    public async Task<bool> ShowAppSettingsDialogAsync()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        var dialogService = App.Services?.GetService<IDialogService>();
        if (dialogService != null)
        {
            var changed = await dialogService.ShowAppSettingsAsync(window);
            if (changed)
            {
                CommandService?.InvalidateCanvas();
                InvalidateVisual();
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Rendering Pipeline

    public override void Render(AvaloniaDrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        _transform.ViewportWidth = bounds.Width;
        _transform.ViewportHeight = bounds.Height;

        // 1. Fill Canvas Background
        var canvasBrush = DrawingAdapters.ToBrush(CoreColors.Canvas);
        context.DrawRectangle(canvasBrush, null, new AvaloniaRect(0, 0, bounds.Width, bounds.Height));

        // 2. Setup Matrix Transform
        var halfWidth = bounds.Width / 2.0;
        var halfHeight = bounds.Height / 2.0;
        var transformMatrix = Matrix.CreateTranslation(-Origin.X, -Origin.Y)
                              * Matrix.CreateScale(ZoomFactor, ZoomFactor)
                              * Matrix.CreateTranslation(halfWidth, halfHeight);

        using (context.PushTransform(transformMatrix))
        {
            // 3. Grid Lines/Dots
            var visibleWorld = _transform.GetVisibleWorldBounds();
            if (Settings.IsGridVisible)
            {
                DrawGrid(context, visibleWorld);
            }

            // 4. Frustum Culling
            const float margin = 100.0f;
            var culledWorld = new CoreRect(
                visibleWorld.Left - margin,
                visibleWorld.Top - margin,
                visibleWorld.Width + margin * 2,
                visibleWorld.Height + margin * 2);

            _visibleElements.Clear();
            foreach (var elem in Project.Current.Elements)
            {
                if (elem.Intersects(culledWorld))
                    _visibleElements.Add(elem);
            }
            _visibleElements.Sort();

            // 5. Render elements in Depth / ZOrder sequence (Rooms, Labels, Connections)
            if (_builtSpatialVersion != _spatialVersion)
            {
                ConnectionDrawingAdapter.RebuildSpatialIndex(Project.Current.Elements, _spatialLines, _spatialVersion);
                _builtSpatialVersion = _spatialVersion;
            }

            DrawElements(
                context,
                _visibleElements,
                _selectedSet,
                _hoverElement,
                _spatialLines,
                _spatialVersion);

            // 6. Render Active Ports
            if (_activePorts.Count > 0)
            {
                var portRadius = Settings.HandleSize / 2.0;
                foreach (var port in _activePorts)
                {
                    var isHover = port == _hoverPort;
                    var brush = isHover ? _hoverPortBrush : _normalPortBrush;
                    var pen = isHover ? _hoverPortPen : _normalPortPen;
                    var center = new Point(port.Position.X, port.Position.Y);
                    context.DrawEllipse(brush, pen, center, portRadius, portRadius);
                }
            }

            // 7. Render Curve Waypoint Handles for single selected connection
            if (_selectedSet.Count == 1 && _selectedSet.First() is Connection selectedConn)
            {
                foreach (var wp in GetVisibleWaypoints(selectedConn))
                {
                    var isSet = selectedConn.GetCurveWaypoint(wp).HasValue;
                    var isHover = wp == _hoverWaypoint || (_dragMode == CanvasDragMode.MoveWaypoint && wp == _dragWaypoint);
                    var wpBounds = GetWaypointBounds(selectedConn, wp);

                    IBrush brush;
                    IPen pen;
                    if (isHover)
                    {
                        brush = _hoverWaypointBrush;
                        pen = _hoverWaypointPen;
                    }
                    else if (isSet)
                    {
                        brush = _setWaypointBrush;
                        pen = _setWaypointPen;
                    }
                    else
                    {
                        brush = _ghostWaypointBrush;
                        pen = _ghostWaypointPen;
                    }

                    var center = new Point(wpBounds.Center.X, wpBounds.Center.Y);
                    var rx = wpBounds.Width / 2.0;
                    var ry = wpBounds.Height / 2.0;
                    context.DrawEllipse(brush, pen, center, rx, ry);
                }
            }

            // 8. Render Selection Resize Handles
            if (_selectedSet.Count == 1 && _selectedSet.First() is ISizeable sizeable)
            {
                foreach (var cp in _handlePoints)
                {
                    var handle = new ResizeHandle(cp, sizeable);
                    var rect = handle.Bounds;
                    var isHover = _hoverHandle != null && _hoverHandle.CompassPoint == cp;
                    var brush = isHover ? _hoverHandleBrush : _handleBrush;
                    context.DrawRectangle(brush, _handlePen, rect.ToRect());
                }
            }

            // 9. Render Marquee Box
            if (_dragMode == CanvasDragMode.Marquee)
            {
                var minX = Math.Min(_dragMarqueeStart.X, _dragMarqueeCurrent.X);
                var maxX = Math.Max(_dragMarqueeStart.X, _dragMarqueeCurrent.X);
                var minY = Math.Min(_dragMarqueeStart.Y, _dragMarqueeCurrent.Y);
                var maxY = Math.Max(_dragMarqueeStart.Y, _dragMarqueeCurrent.Y);
                var marqueeRect = new AvaloniaRect(minX, minY, Math.Max(0.01, maxX - minX), Math.Max(0.01, maxY - minY));
                context.DrawRectangle(_marqueeFillBrush, _marqueeBorderPen, marqueeRect);
            }
        }
    }

    private StreamGeometry? _cachedGridGeometry;
    private CoreRect _cachedGridBounds;
    private float _cachedGridSize;
    private float _cachedGridZoom;

    private void DrawGrid(AvaloniaDrawingContext context, CoreRect visibleWorld)
    {
        var gridSize = Settings.GridSize;
        if (gridSize <= 0) return;

        var minX = (float)(Math.Floor(visibleWorld.Left / gridSize) * gridSize);
        var maxX = (float)(Math.Ceiling(visibleWorld.Right / gridSize) * gridSize);
        var minY = (float)(Math.Floor(visibleWorld.Top / gridSize) * gridSize);
        var maxY = (float)(Math.Ceiling(visibleWorld.Bottom / gridSize) * gridSize);

        var gridBrush = DrawingAdapters.ToBrush(CoreColors.Grid);

        if (gridSize * ZoomFactor >= 4)
        {
            var boundsRounded = new CoreRect(minX, minY, maxX - minX, maxY - minY);
            if (_cachedGridGeometry == null
                || Math.Abs(_cachedGridSize - gridSize) > 0.01f
                || Math.Abs(_cachedGridZoom - ZoomFactor) > 0.1f
                || !_cachedGridBounds.Equals(boundsRounded))
            {
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    const double halfDot = 0.75;
                    for (var x = minX; x <= maxX; x += gridSize)
                    {
                        for (var y = minY; y <= maxY; y += gridSize)
                        {
                            ctx.BeginFigure(new Point(x - halfDot, y - halfDot), true);
                            ctx.LineTo(new Point(x + halfDot, y - halfDot));
                            ctx.LineTo(new Point(x + halfDot, y + halfDot));
                            ctx.LineTo(new Point(x - halfDot, y + halfDot));
                            ctx.EndFigure(true);
                        }
                    }
                }
                _cachedGridGeometry = geometry;
                _cachedGridBounds = boundsRounded;
                _cachedGridSize = gridSize;
                _cachedGridZoom = ZoomFactor;
            }

            if (_cachedGridGeometry != null)
            {
                context.DrawGeometry(gridBrush, null, _cachedGridGeometry);
            }
        }

        if (Settings.ShowOrigin)
        {
            var originPen = Settings.Color[CoreColors.Grid].ToPen(1.5 / ZoomFactor);
            context.DrawLine(originPen, new Point(-25, 0), new Point(25, 0));
            context.DrawLine(originPen, new Point(0, -25), new Point(0, 25));
        }
    }

    /// <summary>
    /// Computes the canvas bounds for all elements in the project, optionally with margins.
    /// </summary>
    public static CoreRect ComputeCanvasBounds(Project? project, bool includePadding = false)
    {
        return MinimapTransform.ComputeCanvasBounds(project?.Elements, includePadding);
    }

    /// <summary>
    /// Draws the provided elements in depth/Z-order sequence onto the given drawing context.
    /// </summary>
    public static void DrawElements(
        AvaloniaDrawingContext context,
        IReadOnlyList<Element> elements,
        HashSet<Element>? selectedSet = null,
        Element? hoverElement = null,
        SpatialLineIndex? spatialLines = null,
        int spatialVersion = 1)
    {
        if (spatialLines == null)
        {
            spatialLines = new SpatialLineIndex();
            ConnectionDrawingAdapter.RebuildSpatialIndex(elements, spatialLines, spatialVersion);
        }

        foreach (var element in elements)
        {
            var selected = selectedSet?.Contains(element) ?? false;
            var hover = element == hoverElement;

            if (element is Room room)
            {
                room.DrawRoom(context, selected, hover);
            }
            else if (element is MapLabel label)
            {
                label.DrawMapLabel(context, selected, hover);
            }
            else if (element is Connection conn)
            {
                conn.DrawConnection(context, selected, hover, spatialLines, spatialVersion);
            }
        }
    }

    /// <summary>
    /// Renders the map to an Avalonia drawing context at the given origin and scale.
    /// </summary>
    public static void RenderMap(
        AvaloniaDrawingContext context,
        Project project,
        CoreVector origin,
        double zoomFactor,
        double width,
        double height)
    {
        if (project == null || width <= 0 || height <= 0) return;

        // 1. Fill Canvas Background
        var canvasBrush = DrawingAdapters.ToBrush(CoreColors.Canvas);
        context.DrawRectangle(canvasBrush, null, new AvaloniaRect(0, 0, width, height));

        // 2. Setup Matrix Transform
        var halfWidth = width / 2.0;
        var halfHeight = height / 2.0;
        var transformMatrix = Matrix.CreateTranslation(-origin.X, -origin.Y)
                              * Matrix.CreateScale(zoomFactor, zoomFactor)
                              * Matrix.CreateTranslation(halfWidth, halfHeight);

        using (context.PushTransform(transformMatrix))
        {
            var sorted = project.Elements.ToList();
            sorted.Sort();
            DrawElements(context, sorted);
        }
    }

    /// <summary>
    /// Renders the full map extent of the project to a RenderTargetBitmap.
    /// Returns null if the map is empty or has zero extent.
    /// </summary>
    public static RenderTargetBitmap? RenderToBitmap(Project? project, double scale = 1.0)
    {
        if (project == null || project.Elements.Count == 0) return null;

        var bounds = MinimapTransform.ComputeCanvasBounds(project.Elements, includePadding: true);
        if (bounds == CoreRect.Empty || bounds.Width <= 0 || bounds.Height <= 0) return null;

        const int maxDimension = 8192;
        const int minDimension = 16;

        var requestedScale = scale > 0 ? scale : 1.0;
        var targetW = bounds.Width * requestedScale;
        var targetH = bounds.Height * requestedScale;

        // Scale down to fit within maxDimension if either axis exceeds it
        var fitScale = Math.Min(1.0, Math.Min((double)maxDimension / targetW, (double)maxDimension / targetH));
        var effectiveScale = requestedScale * fitScale;

        var width = Math.Clamp((int)Math.Ceiling(bounds.Width * effectiveScale), minDimension, maxDimension);
        var height = Math.Clamp((int)Math.Ceiling(bounds.Height * effectiveScale), minDimension, maxDimension);

        // Recompute effectiveScale against actual clamped dimensions to ensure everything fits inside
        effectiveScale = Math.Min((double)width / bounds.Width, (double)height / bounds.Height);

        var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new AvaloniaVector(96, 96));
        using (var ctx = bitmap.CreateDrawingContext())
        {
            RenderMap(ctx, project, bounds.Center, effectiveScale, width, height);
        }

        return bitmap;
    }

    #endregion
}
