using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColors = Trizbort.Domain.Misc.Colors;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Controls;

public class MinimapControl : Control
{
    public static readonly StyledProperty<ICommandService?> CommandServiceProperty =
        AvaloniaProperty.Register<MinimapControl, ICommandService?>(nameof(CommandService));

    public static readonly StyledProperty<MapCanvasControl?> CanvasControlProperty =
        AvaloniaProperty.Register<MinimapControl, MapCanvasControl?>(nameof(CanvasControl));

    public ICommandService? CommandService
    {
        get => GetValue(CommandServiceProperty);
        set => SetValue(CommandServiceProperty, value);
    }

    public MapCanvasControl? CanvasControl
    {
        get => GetValue(CanvasControlProperty);
        set => SetValue(CanvasControlProperty, value);
    }

    private bool _isDragging;
    private Project? _hookedProject;
    private ICommandService? _hookedService;
    private MapCanvasControl? _hookedCanvas;

    public MinimapControl()
    {
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Project.ProjectChanged += OnProjectChanged;
        Settings.Changed += OnSettingsChanged;
        HookProject(Project.Current);
        HookService(CommandService);
        HookCanvas(CanvasControl);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Project.ProjectChanged -= OnProjectChanged;
        Settings.Changed -= OnSettingsChanged;
        UnhookProject(_hookedProject);
        UnhookService(_hookedService);
        UnhookCanvas(_hookedCanvas);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CommandServiceProperty)
        {
            UnhookService(_hookedService);
            HookService(change.GetNewValue<ICommandService?>());
            InvalidateVisual();
        }
        else if (change.Property == CanvasControlProperty)
        {
            UnhookCanvas(_hookedCanvas);
            HookCanvas(change.GetNewValue<MapCanvasControl?>());
            InvalidateVisual();
        }
    }

    private void HookService(ICommandService? service)
    {
        if (service == null) return;
        _hookedService = service;
        service.CanvasInvalidated += OnServiceCanvasInvalidated;
        service.ZoomChanged += OnServiceZoomChanged;
        service.SelectionChanged += OnServiceSelectionChanged;
        service.ProjectChanged += OnServiceProjectChanged;
    }

    private void UnhookService(ICommandService? service)
    {
        if (service == null) return;
        service.CanvasInvalidated -= OnServiceCanvasInvalidated;
        service.ZoomChanged -= OnServiceZoomChanged;
        service.SelectionChanged -= OnServiceSelectionChanged;
        service.ProjectChanged -= OnServiceProjectChanged;
        if (ReferenceEquals(_hookedService, service))
        {
            _hookedService = null;
        }
    }

    private void HookCanvas(MapCanvasControl? canvas)
    {
        if (canvas == null) return;
        _hookedCanvas = canvas;
        canvas.ViewChanged += OnCanvasViewChanged;
    }

    private void UnhookCanvas(MapCanvasControl? canvas)
    {
        if (canvas == null) return;
        canvas.ViewChanged -= OnCanvasViewChanged;
        if (ReferenceEquals(_hookedCanvas, canvas))
        {
            _hookedCanvas = null;
        }
    }

    private void HookProject(Project? project)
    {
        if (project == null || ReferenceEquals(_hookedProject, project)) return;

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

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs e)
    {
        if (e.OldProject != null) UnhookProject(e.OldProject);
        if (e.NewProject != null) HookProject(e.NewProject);
        InvalidateVisual();
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => InvalidateVisual();
    private void OnServiceCanvasInvalidated(object? sender, EventArgs e) => InvalidateVisual();
    private void OnServiceZoomChanged(object? sender, float e) => InvalidateVisual();
    private void OnServiceSelectionChanged(object? sender, EventArgs e) => InvalidateVisual();
    private void OnServiceProjectChanged(object? sender, EventArgs e) => InvalidateVisual();
    private void OnCanvasViewChanged(object? sender, EventArgs e) => InvalidateVisual();

    private void OnElementAdded(object? sender, ItemEventArgs<Element> e)
    {
        if (e.Item != null) e.Item.Changed += OnElementChanged;
        InvalidateVisual();
    }

    private void OnElementRemoved(object? sender, ItemEventArgs<Element> e)
    {
        if (e.Item != null) e.Item.Changed -= OnElementChanged;
        InvalidateVisual();
    }

    private void OnElementChanged(object? sender, EventArgs e) => InvalidateVisual();

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            e.Pointer.Capture(this);
            UpdateOriginFromPointer(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            UpdateOriginFromPointer(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
        InvalidateVisual();
    }

    private void UpdateOriginFromPointer(Point clientPos)
    {
        var bounds = Bounds;
        const double padding = 6.0;
        var clientArea = new CoreRect(
            (float)padding,
            (float)padding,
            (float)Math.Max(0, bounds.Width - 2 * padding),
            (float)Math.Max(0, bounds.Height - 2 * padding));

        if (clientArea.Width <= 0 || clientArea.Height <= 0) return;

        var project = CommandService?.Project ?? Project.Current;
        var canvasBounds = MinimapTransform.ComputeCanvasBounds(project?.Elements);
        if (canvasBounds == CoreRect.Empty || canvasBounds.Width <= 0 || canvasBounds.Height <= 0) return;

        var zoom = CanvasControl?.ZoomFactor ?? CommandService?.ZoomFactor ?? 1.0f;
        var viewportWidth = CanvasControl != null && CanvasControl.Bounds.Width > 0
            ? CanvasControl.Bounds.Width
            : (CommandService?.ViewportWidth ?? 800);
        var viewportHeight = CanvasControl != null && CanvasControl.Bounds.Height > 0
            ? CanvasControl.Bounds.Height
            : (CommandService?.ViewportHeight ?? 600);

        var viewportWorldSize = new CoreVector((float)(viewportWidth / zoom), (float)(viewportHeight / zoom));

        var newOrigin = MinimapTransform.CalculateOriginFromMinimapPoint(
            clientPos.ToVector(),
            canvasBounds,
            clientArea,
            viewportWorldSize);

        if (CanvasControl != null)
        {
            CanvasControl.Origin = newOrigin;
        }
        else if (CommandService != null)
        {
            CommandService.Origin = newOrigin;
        }

        InvalidateVisual();
    }

    public override void Render(AvaloniaDrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 10 || bounds.Height <= 10) return;

        const double padding = 6.0;
        var totalRect = new AvaloniaRect(0.5, 0.5, bounds.Width - 1, bounds.Height - 1);
        var roundedRect = new RoundedRect(totalRect, 4);

        // Draw background card and border
        var bgBrush = Settings.Color[CoreColors.Canvas].ToBrush();
        var borderPen = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(120, 128, 128, 128)), 1.0);
        context.DrawRectangle(bgBrush, borderPen, roundedRect);

        var clientArea = new CoreRect(
            (float)padding,
            (float)padding,
            (float)Math.Max(0, bounds.Width - 2 * padding),
            (float)Math.Max(0, bounds.Height - 2 * padding));

        if (clientArea.Width <= 0 || clientArea.Height <= 0) return;

        var project = CommandService?.Project ?? Project.Current;
        var elements = project?.Elements;
        var canvasBounds = MinimapTransform.ComputeCanvasBounds(elements);

        using (context.PushClip(new RoundedRect(clientArea.ToRect(), 2)))
        {
            if (elements != null && canvasBounds != CoreRect.Empty && canvasBounds.Width > 0 && canvasBounds.Height > 0)
            {
                var selectedSet = CommandService?.SelectedElements ?? Project.SelectedElementsProvider?.Invoke();

                // Draw connections
                var connPen = Settings.Color[CoreColors.Line].ToPen(1.0);
                var selPen = Settings.Color[CoreColors.SelectedLine].ToPen(1.2);

                foreach (var conn in elements.OfType<Connection>())
                {
                    var isSelected = selectedSet != null && selectedSet.Contains(conn);
                    var pen = isSelected
                        ? selPen
                        : (conn.ConnectionColor != System.Drawing.Color.Transparent ? conn.ConnectionColor.ToPen(1.0) : connPen);

                    var segments = conn.GetSegments();
                    if (segments != null)
                    {
                        foreach (var seg in segments)
                        {
                            var p1 = MinimapTransform.WorldToMinimap(seg.Start, canvasBounds, clientArea);
                            var p2 = MinimapTransform.WorldToMinimap(seg.End, canvasBounds, clientArea);
                            context.DrawLine(pen, p1.ToAvaloniaPoint(), p2.ToAvaloniaPoint());
                        }
                    }
                }

                // Draw rooms
                var roomDefaultBorderPen = Settings.Color[CoreColors.Border].ToPen(1.0);
                var roomDefaultFill = Settings.Color[CoreColors.Canvas].ToBrush();

                foreach (var room in elements.OfType<Room>())
                {
                    var isSelected = selectedSet != null && selectedSet.Contains(room);
                    var roomMinimap = MinimapTransform.WorldToMinimap(room.InnerBounds, canvasBounds, clientArea);
                    if (roomMinimap.Width < 1) roomMinimap.Width = 1;
                    if (roomMinimap.Height < 1) roomMinimap.Height = 1;

                    var fillBrush = isSelected
                        ? selPen.Brush
                        : (room.RoomFillColor != System.Drawing.Color.Transparent ? room.RoomFillColor.ToBrush() : roomDefaultFill);

                    var pen = isSelected
                        ? selPen
                        : (room.RoomBorderColor != System.Drawing.Color.Transparent ? room.RoomBorderColor.ToPen(1.0) : roomDefaultBorderPen);

                    if (fillBrush != null)
                    {
                        context.DrawRectangle(fillBrush, pen, roomMinimap.ToRect());
                    }
                }

                // Draw viewport handle
                var origin = CanvasControl?.Origin ?? CommandService?.Origin ?? CoreVector.Zero;
                var zoom = CanvasControl?.ZoomFactor ?? CommandService?.ZoomFactor ?? 1.0f;
                var viewportWidth = CanvasControl != null && CanvasControl.Bounds.Width > 0
                    ? CanvasControl.Bounds.Width
                    : (CommandService?.ViewportWidth ?? 800);
                var viewportHeight = CanvasControl != null && CanvasControl.Bounds.Height > 0
                    ? CanvasControl.Bounds.Height
                    : (CommandService?.ViewportHeight ?? 600);

                var canvasTransform = new CanvasTransform
                {
                    Origin = origin,
                    ZoomFactor = zoom,
                    ViewportWidth = viewportWidth,
                    ViewportHeight = viewportHeight
                };

                var viewportWorld = canvasTransform.GetVisibleWorldBounds();
                var vpMinimap = MinimapTransform.WorldToMinimap(viewportWorld, canvasBounds, clientArea);

                var vpRect = vpMinimap.ToRect();
                var vpFill = new SolidColorBrush(AvaloniaColor.FromArgb(40, 0, 120, 215));
                var vpBorder = new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(220, 0, 120, 215)), _isDragging ? 2.0 : 1.5);

                context.DrawRectangle(vpFill, vpBorder, vpRect);
            }
        }
    }
}
