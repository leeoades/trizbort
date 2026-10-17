using System;
using System.Collections.Generic;
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

    public CoreVector Origin
    {
        get => _origin;
        set
        {
            if (_origin != value)
            {
                _origin = value;
                InvalidateVisual();
            }
        }
    }

    public float ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            var clamped = Math.Clamp(value, 0.1f, 10.0f);
            if (Math.Abs(_zoomFactor - clamped) > 0.0001f)
            {
                _zoomFactor = clamped;
                InvalidateVisual();
            }
        }
    }

    public MapCanvasControl()
    {
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Project.ProjectChanged += OnProjectChanged;
        Settings.Changed += OnSettingsChanged;

        if (CommandService != null)
        {
            HookCommandService(CommandService);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Project.ProjectChanged -= OnProjectChanged;
        Settings.Changed -= OnSettingsChanged;

        if (CommandService != null)
        {
            UnhookCommandService(CommandService);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CommandServiceProperty)
        {
            if (change.OldValue is ICommandService oldService)
            {
                UnhookCommandService(oldService);
            }
            if (change.NewValue is ICommandService newService)
            {
                HookCommandService(newService);
                Origin = newService.Origin;
                ZoomFactor = newService.ZoomFactor;
            }
        }
    }

    private void HookCommandService(ICommandService service)
    {
        service.CanvasInvalidated += OnCanvasInvalidated;
        service.ZoomChanged += OnServiceZoomChanged;
        service.SelectionChanged += OnCanvasInvalidated;
    }

    private void UnhookCommandService(ICommandService service)
    {
        service.CanvasInvalidated -= OnCanvasInvalidated;
        service.ZoomChanged -= OnServiceZoomChanged;
        service.SelectionChanged -= OnCanvasInvalidated;
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void OnCanvasInvalidated(object? sender, EventArgs e)
    {
        if (CommandService != null)
        {
            if (Origin != CommandService.Origin)
            {
                _origin = CommandService.Origin;
            }
            if (Math.Abs(_zoomFactor - CommandService.ZoomFactor) > 0.0001f)
            {
                _zoomFactor = CommandService.ZoomFactor;
            }
        }
        InvalidateVisual();
    }

    private void OnServiceZoomChanged(object? sender, float zoom)
    {
        ZoomFactor = zoom;
    }

    #region Coordinate Transformations

    public Point CanvasToClient(CoreVector world) =>
        new((world.X - Origin.X) * ZoomFactor + Bounds.Width / 2.0,
            (world.Y - Origin.Y) * ZoomFactor + Bounds.Height / 2.0);

    public CoreVector ClientToCanvas(Point screen) =>
        new((float)((screen.X - Bounds.Width / 2.0) / ZoomFactor + Origin.X),
            (float)((screen.Y - Bounds.Height / 2.0) / ZoomFactor + Origin.Y));

    public CoreRect GetVisibleWorldBounds()
    {
        var topLeft = ClientToCanvas(new Point(0, 0));
        var bottomRight = ClientToCanvas(new Point(Bounds.Width, Bounds.Height));
        return new CoreRect(topLeft.X, topLeft.Y, Math.Max(0, bottomRight.X - topLeft.X), Math.Max(0, bottomRight.Y - topLeft.Y));
    }

    public void ZoomAt(Point cursorScreen, float newZoom)
    {
        var worldBefore = ClientToCanvas(cursorScreen);
        ZoomFactor = Math.Clamp(newZoom, 0.1f, 10.0f);
        var worldAfter = ClientToCanvas(cursorScreen);
        Origin += (worldBefore - worldAfter);
        if (CommandService != null)
        {
            CommandService.ZoomFactor = ZoomFactor;
            CommandService.Origin = Origin;
        }
        InvalidateVisual();
    }

    public void FitToMap()
    {
        var elements = Project.Current.Elements;
        if (elements.Count == 0)
        {
            Origin = CoreVector.Zero;
            ZoomFactor = 1.0f;
            if (CommandService != null)
            {
                CommandService.Origin = Origin;
                CommandService.ZoomFactor = ZoomFactor;
            }
            InvalidateVisual();
            return;
        }

        var bounds = CoreRect.Empty;
        foreach (var el in elements)
        {
            bounds = el.UnionBoundsWith(bounds, true);
        }

        if (bounds.Width > 0 && bounds.Height > 0)
        {
            Origin = new CoreVector(bounds.X + bounds.Width / 2.0f, bounds.Y + bounds.Height / 2.0f);
            
            var margin = 40.0;
            var availableWidth = Math.Max(100.0, Bounds.Width - margin * 2.0);
            var availableHeight = Math.Max(100.0, Bounds.Height - margin * 2.0);

            var zoomX = (float)(availableWidth / bounds.Width);
            var zoomY = (float)(availableHeight / bounds.Height);
            var bestZoom = Math.Min(1.0f, Math.Min(zoomX, zoomY));
            ZoomFactor = Math.Clamp(bestZoom, 0.1f, 10.0f);

            if (CommandService != null)
            {
                CommandService.Origin = Origin;
                CommandService.ZoomFactor = ZoomFactor;
            }
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
            var allElements = Project.Current.Elements;
            var selectedList = Project.Current.GetSelectedElements();
            var selectedSet = new HashSet<Element>(selectedList);
            if (selectedSet.Count == 0 && Project.Current.ActiveSelectedElement != null)
            {
                selectedSet.Add(Project.Current.ActiveSelectedElement);
            }

            var visibleElements = allElements.Where(e => e.Intersects(culledWorld)).ToList();

            // 5. Render Connections
            var linesDrawn = new List<CoreLineSegment>();
            var connections = visibleElements.OfType<Connection>().OrderBy(c => (int)c.Depth).ThenBy(c => c.ZOrder);
            foreach (var conn in connections)
            {
                var isSelected = selectedSet.Contains(conn);
                conn.DrawConnection(context, isSelected, false, linesDrawn);
            }

            // 6. Render Rooms
            var rooms = visibleElements.OfType<Room>().OrderBy(r => (int)r.Depth).ThenBy(r => r.ZOrder);
            foreach (var room in rooms)
            {
                var isSelected = selectedSet.Contains(room);
                room.DrawRoom(context, isSelected, false);
            }

            // 7. Render Map Labels
            var labels = visibleElements.OfType<MapLabel>().OrderBy(l => (int)l.Depth).ThenBy(l => l.ZOrder);
            foreach (var label in labels)
            {
                var isSelected = selectedSet.Contains(label);
                label.DrawMapLabel(context, isSelected, false);
            }

            // 8. Render Selection Resize Handles
            if (selectedSet.Count == 1)
            {
                var singleSelected = selectedSet.First();
                if (singleSelected is ISizeable sizeable)
                {
                    DrawSelectionHandles(context, sizeable);
                }
            }
        }
    }

    private void DrawGrid(AvaloniaDrawingContext context, CoreRect visibleWorld)
    {
        var gridSize = Settings.GridSize;
        var minX = (float)(Math.Floor(visibleWorld.Left / gridSize) * gridSize);
        var maxX = (float)(Math.Ceiling(visibleWorld.Right / gridSize) * gridSize);
        var minY = (float)(Math.Floor(visibleWorld.Top / gridSize) * gridSize);
        var maxY = (float)(Math.Ceiling(visibleWorld.Bottom / gridSize) * gridSize);

        var gridBrush = DrawingAdapters.ToBrush(CoreColors.Grid);

        if (gridSize * ZoomFactor >= 4)
        {
            for (var x = minX; x <= maxX; x += gridSize)
            {
                for (var y = minY; y <= maxY; y += gridSize)
                {
                    context.DrawRectangle(gridBrush, null, new AvaloniaRect(x - 0.75, y - 0.75, 1.5, 1.5));
                }
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
        var handleBrush = Brushes.White;
        var handlePen = new Pen(Brushes.Black, 1.0);

        CompassPoint[] points =
        [
            CompassPoint.NorthWest, CompassPoint.North, CompassPoint.NorthEast,
            CompassPoint.East, CompassPoint.SouthEast, CompassPoint.South,
            CompassPoint.SouthWest, CompassPoint.West
        ];

        foreach (var cp in points)
        {
            var handle = new ResizeHandle(cp, sizeable);
            var rect = handle.Bounds;
            context.DrawRectangle(handleBrush, handlePen, rect.ToRect());
        }
    }

    #endregion
}
