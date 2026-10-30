using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
using Trizbort.Editor;
using Trizbort.Editor.Input;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColors = Trizbort.Domain.Misc.Colors;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaVector = Avalonia.Vector;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;
using CoreModifierKeys = Trizbort.Domain.Enums.ModifierKeys;

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

    // Interaction state machine lives in Core (CanvasInteraction); this control converts events and draws.
    private CanvasInteraction? _interaction;
    private EditorSession? _interactionSession;
    private HitTester? _hit;
    private EditorSession? _hitSession;
    private EditorSession? _fallbackSession;
    private bool _isSecondaryButtonRelease;
    private Element? _toolTipTarget;

    internal CanvasDragMode DragMode => _interaction?.Mode ?? CanvasDragMode.None;
    internal Element? HoverElement => _interaction?.Adorners.HoverElement;
    internal Port? HoverPort => _interaction?.Adorners.HoverPort;
    internal ResizeHandle? HoverHandle => _interaction?.Adorners.HoverHandle;
    internal CurveWaypoint? HoverWaypoint => _interaction?.Adorners.HoverWaypoint;
    internal bool IsGestureActive => _interaction?.IsGestureActive ?? false;

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
        DisposeInteraction();
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
            DisposeInteraction();
            TryUnhookCommandService();
            TryHookCommandService();
        }
        else if (change.Property == BoundsProperty)
        {
            var bounds = Bounds;
            _transform.ViewportWidth = bounds.Width;
            _transform.ViewportHeight = bounds.Height;
            CommandService?.SetViewportSize(bounds.Width, bounds.Height);
            _fallbackSession?.Viewport.SetSize(bounds.Width, bounds.Height);
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
        UpdateCursor();
        UpdateToolTip();
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
        UpdateCursor();
        InvalidateVisual();
    }

    private void OnCommandServiceProjectChanged(object? sender, EventArgs e)
    {
        _spatialVersion++;
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

    private EditorSession ActiveSession
    {
        get
        {
            var session = (CommandService as Services.CommandService)?.Session;
            if (session == null)
            {
                if (_fallbackSession == null)
                {
                    _fallbackSession = new EditorSession();
                    _fallbackSession.Viewport.SetSize(Bounds.Width, Bounds.Height);
                }
                session = _fallbackSession;
            }
            return session;
        }
    }

    private HitTester Hit
    {
        get
        {
            var session = ActiveSession;
            if (_hit == null || !ReferenceEquals(_hitSession, session))
            {
                _hitSession = session;
                _hit = new HitTester(session);
            }
            return _hit;
        }
    }

    /// <summary>The gesture state machine for the current session (recreated if the session changes).</summary>
    private CanvasInteraction Interaction
    {
        get
        {
            var session = ActiveSession;
            if (_interaction == null || !ReferenceEquals(_interactionSession, session))
            {
                DisposeInteraction();
                _interactionSession = session;
                _interaction = new CanvasInteraction(session, Hit);
                _interaction.EditRequested += OnEditRequested;
                _interaction.ContextMenuRequested += ShowContextMenuAt;
            }
            return _interaction;
        }
    }

    private void DisposeInteraction()
    {
        if (_interaction == null) return;

        _interaction.EditRequested -= OnEditRequested;
        _interaction.ContextMenuRequested -= ShowContextMenuAt;
        _interaction.Dispose();
        _interaction = null;
        _interactionSession = null;
    }

    private void OnEditRequested(Element element)
    {
        _ = ShowPropertiesDialogAsync(element);
    }

    internal StandardCursorType CursorType { get; private set; } = StandardCursorType.Arrow;

    private void UpdateCursor()
    {
        var type = InputMapper.ToCursor(_interaction?.Adorners.Cursor ?? CursorKind.Default);
        if (type == CursorType && Cursor != null) return;

        CursorType = type;
        Cursor = new Cursor(type);
    }

    private void UpdateToolTip()
    {
        var target = _interaction?.Adorners.TooltipTarget;
        if (ReferenceEquals(target, _toolTipTarget)) return;

        _toolTipTarget = target;
        ToolTip.SetTip(this, BuildToolTipText(target));
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

    private static PointerInput ToPointerInput(PointerButton button, KeyModifiers modifiers, Point position, int clickCount)
    {
        return new PointerInput(button, InputMapper.ToModifiers(modifiers), position.ToVector(), clickCount);
    }

    private void AfterInteraction()
    {
        UpdateCursor();
        UpdateToolTip();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetCurrentPoint(this);
        var button = point.Properties.IsMiddleButtonPressed ? PointerButton.Middle
            : point.Properties.IsRightButtonPressed ? PointerButton.Right
            : PointerButton.Left;

        var interaction = Interaction;
        var handled = interaction.OnPointerPressed(ToPointerInput(button, e.KeyModifiers, point.Position, e.ClickCount));
        if (interaction.IsGestureActive)
        {
            e.Pointer.Capture(this);
        }
        AfterInteraction();
        e.Handled = handled;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var clientPos = e.GetPosition(this);
        CommandService?.SetPointerClientPosition(clientPos.X, clientPos.Y);
        if (CommandService?.Session?.ShowMouseCoordinates == true)
        {
            InvalidateVisual();
        }

        var interaction = Interaction;
        interaction.OnPointerMoved(ToPointerInput(PointerButton.None, e.KeyModifiers, clientPos, 0));

        switch (interaction.Mode)
        {
            case CanvasDragMode.MoveElement:
            case CanvasDragMode.MoveResizeHandle:
            case CanvasDragMode.MovePort:
            case CanvasDragMode.MoveWaypoint:
                _spatialVersion++;
                InvalidateVisual();
                break;
        }

        AfterInteraction();
        e.Handled = interaction.Mode != CanvasDragMode.None;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        CommandService?.ClearPointerPosition();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var interaction = Interaction;
        var releasedButton = InputMapper.ToButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind.GetMouseButton());
        if (interaction.IsGestureActive && interaction.GestureButton != PointerButton.None &&
            releasedButton != interaction.GestureButton)
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

        var wasActive = interaction.IsGestureActive;
        interaction.OnPointerReleased(ToPointerInput(releasedButton, e.KeyModifiers, e.GetPosition(this), 0));
        e.Pointer.Capture(null);
        _isSecondaryButtonRelease = false;
        _spatialVersion++;
        InvalidateVisual();
        AfterInteraction();
        e.Handled = wasActive;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_isSecondaryButtonRelease)
        {
            _isSecondaryButtonRelease = false;
            return;
        }

        if (_interaction == null || !_interaction.IsGestureActive) return;

        _interaction.Cancel();
        _spatialVersion++;
        InvalidateVisual();
        AfterInteraction();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        Interaction.OnWheel(new WheelInput((float)e.Delta.Y, InputMapper.ToModifiers(e.KeyModifiers), e.GetPosition(this).ToVector()));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var interaction = Interaction;
        var keyInput = AvaloniaKeyMapper.ToKeyInput(e.Key, e.KeyModifiers, AvaloniaKeyMapper.IsMacOS);

        if (e.Key == Key.Space || e.Key == Key.Escape)
        {
            interaction.OnKey(keyInput);
            if (e.Key == Key.Escape)
            {
                _spatialVersion++;
                InvalidateVisual();
            }
            AfterInteraction();
            e.Handled = true;
            return;
        }

        if (interaction.IsGestureActive)
        {
            // While a drag owns the map (its undo transaction is open) no other command may run.
            e.Handled = true;
            return;
        }

        if (CanvasCommandBridge.TryRunCanvasGesture(this, e))
        {
            e.Handled = true;
            return;
        }

        var isCtrl = (e.KeyModifiers & KeyModifiers.Control) != 0;

        switch (e.Key)
        {
            case Key.D0 or Key.NumPad0 when isCtrl:
                CommandService?.ResetZoom();
                e.Handled = true;
                break;

            case Key.Left or Key.Right or Key.Up or Key.Down:
                if (interaction.OnKey(keyInput))
                {
                    _spatialVersion++;
                    InvalidateVisual();
                    e.Handled = true;
                }
                break;
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        if (_interaction != null)
        {
            _interaction.SetSpaceHeld(false);
            AfterInteraction();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == Key.Space)
        {
            Interaction.OnKeyUp(AvaloniaKeyMapper.ToKeyInput(e.Key, e.KeyModifiers, AvaloniaKeyMapper.IsMacOS));
            AfterInteraction();
            e.Handled = true;
        }
    }

    private void ShowContextMenuAt(CoreVector worldPos)
    {
        var hitElem = Hit.HitElement(worldPos);
        if (hitElem != null && !_selectedSet.Contains(hitElem))
        {
            CommandService?.Select([hitElem], clearExisting: true);
        }

        var session = CommandService?.Session;
        if (session == null) return;
        _ = session.RefreshClipboardStateAsync();

        // Properties acts on the element under the pointer, never on a stale selection.
        var activeElement = hitElem;
        var target = hitElem switch
        {
            Room => Trizbort.Editor.Commands.ContextTargets.Room,
            Connection => Trizbort.Editor.Commands.ContextTargets.Connection,
            MapLabel => Trizbort.Editor.Commands.ContextTargets.Label,
            _ => Trizbort.Editor.Commands.ContextTargets.Empty
        };

        var menu = CatalogueMenuBuilder.BuildContextMenu(
            CanvasCommandBridge.Resolve(this),
            session,
            target,
            command => CanvasCommandBridge.ContextCommandFor(this, command, worldPos, activeElement));
        LastContextMenu = menu;
        menu.Open(this);
    }

    /// <summary>The command catalogue the menus and canvas keys come from. Set by the host window; falls back to the application's.</summary>
    internal Trizbort.Editor.Commands.CommandCatalogue? Catalogue { get; set; }

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

            var adorners = _interaction?.Adorners ?? AdornerState.Empty;

            // --debug toggles (ticket 108); both are true unless debug is on and the toggle is off
            var debugSession = CommandService?.Session;
            DrawElements(
                context,
                _visibleElements,
                _selectedSet,
                adorners.HoverElement,
                _spatialLines,
                _spatialVersion,
                debugSession?.ElementRenderingEnabled ?? true,
                debugSession?.LineRenderingEnabled ?? true);

            // 6. Render Active Ports
            if (adorners.Ports.Count > 0)
            {
                var portRadius = Settings.HandleSize / 2.0;
                foreach (var port in adorners.Ports)
                {
                    var isHover = port == adorners.HoverPort;
                    var brush = isHover ? _hoverPortBrush : _normalPortBrush;
                    var pen = isHover ? _hoverPortPen : _normalPortPen;
                    var center = new Point(port.Position.X, port.Position.Y);
                    context.DrawEllipse(brush, pen, center, portRadius, portRadius);
                }
            }

            // 7. Render Curve Waypoint Handles for single selected connection
            if (_selectedSet.Count == 1 && _selectedSet.First() is Connection selectedConn)
            {
                foreach (var wp in Hit.VisibleWaypoints(selectedConn))
                {
                    var isSet = selectedConn.GetCurveWaypoint(wp).HasValue;
                    var isHover = wp == adorners.HoverWaypoint || wp == adorners.DragWaypoint;
                    var wpBounds = Hit.WaypointBounds(selectedConn, wp);

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
            foreach (var handle in adorners.Handles)
            {
                var rect = handle.Bounds;
                var isHover = adorners.HoverHandle != null && adorners.HoverHandle.CompassPoint == handle.CompassPoint;
                var brush = isHover ? _hoverHandleBrush : _handleBrush;
                context.DrawRectangle(brush, _handlePen, rect.ToRect());
            }

            // 9. Render Marquee Box
            if (adorners.Marquee is { } marquee)
            {
                var marqueeRect = new AvaloniaRect(marquee.X, marquee.Y, marquee.Width, marquee.Height);
                context.DrawRectangle(_marqueeFillBrush, _marqueeBorderPen, marqueeRect);
            }
        }

        var overlaySession = CommandService?.Session;
        if (overlaySession != null && (overlaySession.ShowFps || overlaySession.ShowMouseCoordinates))
        {
            DrawDebugOverlay(context, overlaySession);
        }
    }

    private readonly System.Diagnostics.Stopwatch _fpsWatch = System.Diagnostics.Stopwatch.StartNew();
    private int _fpsFrames;
    private int _fps;

    /// <summary>Debug text in client pixels: frames per second and the pointer position (ticket 108).</summary>
    private void DrawDebugOverlay(AvaloniaDrawingContext context, EditorSession session)
    {
        var y = 10.0;
        if (session.ShowFps)
        {
            _fpsFrames++;
            if (_fpsWatch.ElapsedMilliseconds >= 1000)
            {
                _fps = (int)(_fpsFrames * 1000 / _fpsWatch.ElapsedMilliseconds);
                _fpsFrames = 0;
                _fpsWatch.Restart();
            }

            DrawDebugLine(context, $"{_fps} fps", Brushes.Red, ref y);
        }

        if (session.ShowMouseCoordinates)
        {
            var pointer = session.PointerMapPosition;
            DrawDebugLine(context, pointer == null ? "X:-  Y:-" : $"X:{pointer.Value.X:0}  Y:{pointer.Value.Y:0}", Brushes.Green, ref y);
        }
    }

    private static void DrawDebugLine(AvaloniaDrawingContext context, string text, IBrush brush, ref double y)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            brush);
        context.DrawText(formatted, new Point(10, y));
        y += formatted.Height + 2;
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
        int spatialVersion = 1,
        bool drawRoomsAndLabels = true,
        bool drawConnections = true)
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
                if (drawRoomsAndLabels) room.DrawRoom(context, selected, hover);
            }
            else if (element is MapLabel label)
            {
                if (drawRoomsAndLabels) label.DrawMapLabel(context, selected, hover);
            }
            else if (element is Connection conn)
            {
                if (drawConnections) conn.DrawConnection(context, selected, hover, spatialLines, spatialVersion);
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
