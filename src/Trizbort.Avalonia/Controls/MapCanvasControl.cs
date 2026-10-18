using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
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
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Controls;

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

    private static readonly IBrush _handleBrush = Brushes.White;
    private static readonly IPen _handlePen = new Pen(Brushes.Black, 1.0);
    private static readonly CompassPoint[] _handlePoints =
    [
        CompassPoint.NorthWest, CompassPoint.North, CompassPoint.NorthEast,
        CompassPoint.East, CompassPoint.SouthEast, CompassPoint.South,
        CompassPoint.SouthWest, CompassPoint.West
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

    public MapCanvasControl()
    {
        ClipToBounds = true;
    }

    public new void InvalidateVisual()
    {
        InvalidationCount++;
        base.InvalidateVisual();
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
        InvalidateVisual();
    }

    private void OnElementRemoved(object? sender, ItemEventArgs<Element> e)
    {
        if (e.Item != null)
        {
            e.Item.Changed -= OnElementChanged;
        }
        _spatialVersion++;
        InvalidateVisual();
    }

    private void OnElementChanged(object? sender, EventArgs e)
    {
        _spatialVersion++;
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CommandServiceProperty)
        {
            TryUnhookCommandService();
            if (change.NewValue is ICommandService newService)
            {
                SyncViewFromService(newService);
                if (Bounds.Width > 0 && Bounds.Height > 0)
                {
                    newService.SetViewportSize(Bounds.Width, Bounds.Height);
                }
                if (this.IsAttachedToVisualTree())
                {
                    HookCommandService(newService);
                }
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _transform.ViewportWidth = e.NewSize.Width;
        _transform.ViewportHeight = e.NewSize.Height;
        if (CommandService != null && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            CommandService.SetViewportSize(e.NewSize.Width, e.NewSize.Height);
        }
    }

    private void TryHookCommandService()
    {
        if (CommandService != null)
        {
            HookCommandService(CommandService);
        }
    }

    private void TryUnhookCommandService()
    {
        if (_hookedService != null)
        {
            UnhookCommandService(_hookedService);
        }
    }

    private void HookCommandService(ICommandService service)
    {
        if (ReferenceEquals(_hookedService, service))
        {
            return;
        }

        TryUnhookCommandService();
        service.CanvasInvalidated += OnCanvasInvalidated;
        service.ZoomChanged += OnServiceZoomChanged;
        service.SelectionChanged += OnCanvasInvalidated;
        _hookedService = service;
    }

    private void UnhookCommandService(ICommandService service)
    {
        service.CanvasInvalidated -= OnCanvasInvalidated;
        service.ZoomChanged -= OnServiceZoomChanged;
        service.SelectionChanged -= OnCanvasInvalidated;
        if (ReferenceEquals(_hookedService, service))
        {
            _hookedService = null;
        }
    }

    private void SyncViewFromService(ICommandService service)
    {
        _origin = service.Origin;
        _zoomFactor = service.ZoomFactor;
        _transform.Origin = _origin;
        _transform.ZoomFactor = _zoomFactor;
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
        InvalidateVisual();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _spatialVersion++;
        InvalidateVisual();
    }

    private void OnCanvasInvalidated(object? sender, EventArgs e)
    {
        if (CommandService != null)
        {
            SyncViewFromService(CommandService);
        }
        InvalidateVisual();
    }

    private void OnServiceZoomChanged(object? sender, float zoom)
    {
        if (CommandService != null)
        {
            SyncViewFromService(CommandService);
        }
        else
        {
            _zoomFactor = zoom;
            _transform.ZoomFactor = zoom;
        }
        InvalidateVisual();
    }

    #region Coordinate Transformations

    public Point CanvasToClient(CoreVector world)
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        var p = _transform.CanvasToClient(world);
        return new Point(p.X, p.Y);
    }

    public CoreVector ClientToCanvas(Point screen)
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        return _transform.ClientToCanvas(new CoreVector((float)screen.X, (float)screen.Y));
    }

    public CoreRect GetVisibleWorldBounds()
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        return _transform.GetVisibleWorldBounds();
    }

    public void ZoomAt(Point cursorScreen, float newZoom)
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        _transform.ZoomAt(new CoreVector((float)cursorScreen.X, (float)cursorScreen.Y), newZoom);

        if (CommandService != null)
        {
            CommandService.SetView(_transform.Origin, _transform.ZoomFactor);
        }
        else
        {
            _origin = _transform.Origin;
            _zoomFactor = _transform.ZoomFactor;
            InvalidateVisual();
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
            _transform.ViewportWidth = Bounds.Width;
            _transform.ViewportHeight = Bounds.Height;
            _transform.FitToMap(Project.Current.Elements);
            _origin = _transform.Origin;
            _zoomFactor = _transform.ZoomFactor;
            InvalidateVisual();
        }
    }

    #endregion

    #region Pointer & Interaction

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var delta = e.Delta.Y;
        if (Math.Abs(delta) > 0.001)
        {
            var factor = delta > 0 ? 1.15f : 1.0f / 1.15f;
            var currentPos = e.GetPosition(this);
            ZoomAt(currentPos, ZoomFactor * factor);
            e.Handled = true;
        }
    }

    #endregion

    #region Rendering Pipeline

    public override void Render(AvaloniaDrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        // 1. Draw Canvas Background
        context.DrawRectangle(DrawingAdapters.ToBrush(CoreColors.Canvas), null, new AvaloniaRect(0, 0, Bounds.Width, Bounds.Height));

        var visibleWorld = GetVisibleWorldBounds();
        var culledWorld = visibleWorld;
        culledWorld.Inflate(120f, 120f);

        // 2. Push Matrix Transform for World-to-Screen rendering
        var transformMatrix = Matrix.CreateTranslation(-Origin.X, -Origin.Y)
                              * Matrix.CreateScale(ZoomFactor, ZoomFactor)
                              * Matrix.CreateTranslation(Bounds.Width / 2.0, Bounds.Height / 2.0);

        using (context.PushTransform(transformMatrix))
        {
            // 3. Draw Grid
            if (Settings.IsGridVisible && (CommandService?.ShowGrid ?? true) && Settings.GridSize > 0)
            {
                DrawGrid(context, visibleWorld);
            }

            // 4. Viewport Culling & Element Collection
            _selectedSet.Clear();
            var selectedList = Project.Current.GetSelectedElements();
            foreach (var sel in selectedList) _selectedSet.Add(sel);
            if (_selectedSet.Count == 0 && Project.Current.ActiveSelectedElement != null)
            {
                _selectedSet.Add(Project.Current.ActiveSelectedElement);
            }

            _visibleElements.Clear();
            foreach (var elem in Project.Current.Elements)
            {
                if (elem.Intersects(culledWorld))
                    _visibleElements.Add(elem);
            }
            _visibleElements.Sort();

            // 5. Render elements in Depth / ZOrder sequence (Rooms, Labels, Connections)
            // Pre-populate room boundary and connection segments for smart line segment crossing calculation
            if (_builtSpatialVersion != _spatialVersion)
            {
                ConnectionDrawingAdapter.RebuildSpatialIndex(Project.Current.Elements, _spatialLines, _spatialVersion);
                _builtSpatialVersion = _spatialVersion;
            }

            foreach (var element in _visibleElements)
            {
                var isSelected = _selectedSet.Contains(element);
                if (element is Room room)
                {
                    room.DrawRoom(context, isSelected, false);
                }
                else if (element is MapLabel label)
                {
                    label.DrawMapLabel(context, isSelected, false);
                }
                else if (element is Connection conn)
                {
                    conn.DrawConnection(context, isSelected, false, _spatialLines, _spatialVersion);
                }
            }

            // 6. Render Selection Resize Handles
            if (_selectedSet.Count == 1)
            {
                var singleSelected = _selectedSet.First();
                if (singleSelected is ISizeable sizeable)
                {
                    DrawSelectionHandles(context, sizeable);
                }
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

    private static void DrawSelectionHandles(AvaloniaDrawingContext context, ISizeable sizeable)
    {
        foreach (var cp in _handlePoints)
        {
            var handle = new ResizeHandle(cp, sizeable);
            var rect = handle.Bounds;
            context.DrawRectangle(_handleBrush, _handlePen, rect.ToRect());
        }
    }

    #endregion
}
