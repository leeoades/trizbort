using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>
///   The pointer and key gesture state machine of the map canvas (pan, marquee, move, resize, move port, move
///   waypoint, new connection, hover, cursor), independent of any UI toolkit. A host converts its events to
///   <see cref="PointerInput" />, <see cref="WheelInput" /> and <see cref="KeyInput" />, calls the matching
///   method and draws <see cref="Adorners" />. Each drag runs inside one undo transaction: releasing records a
///   single step (or none if nothing changed); <see cref="Cancel" /> rolls the map back to how it was before the
///   drag. Note that rolling back replaces <c>Project.Current</c>, so callers must not hold element references
///   across a cancel.
/// </summary>
public sealed class CanvasInteraction : IDisposable
{
  // A right click that moves no further than this many pixels opens the context menu instead of panning.
  private const float ClickTolerance = 2;

  private readonly EditorSession _session;
  private readonly HitTester _hit;

  private CanvasDragMode _mode = CanvasDragMode.None;
  private bool _tooltipSuppressed;
  private PointerButton _gestureButton = PointerButton.None;
  private AdornerState _adorners = AdornerState.Empty;
  private IReadOnlyList<ResizeHandle> _handles = new List<ResizeHandle>();
  private IReadOnlyList<Port> _ports = new List<Port>();

  private Element _hoverElement;
  private Port _hoverPort;
  private ResizeHandle _hoverHandle;
  private CurveWaypoint? _hoverWaypoint;
  private bool _spaceHeld;
  private bool _referenceJumpHeld;

  private UndoTransaction _transaction;
  private Project _gestureProject;
  private Vector _lastWorld;

  private Vector _panLastClient;
  private Vector _panStartClient;
  private bool _panMoved;
  private Vector _contextWorld;

  private Vector _dragOffset;
  private Vector _marqueeStart;
  private Vector _marqueeCurrent;
  private ResizeHandle _dragHandle;
  private Vector _dragHandleLast;
  private CurveWaypoint _dragWaypoint;
  private Vector _dragWaypointOffset;
  private Vector _dragWaypointStartClient;
  private MoveablePort _dragPort;
  private Connection _dragNewConnection;
  private Port _newConnectionFrom;

  private Port _pendingPort;
  private Vector _pendingStartClient;
  private Vector _pendingStartWorld;

  public CanvasInteraction(EditorSession session, HitTester hit)
  {
    _session = session ?? throw new ArgumentNullException(nameof(session));
    _hit = hit ?? throw new ArgumentNullException(nameof(hit));
    _session.Changed += OnSessionChanged;
    _session.InteractionActiveProbe = () => IsGestureActive;
  }

  /// <summary>What to draw on top of the map right now.</summary>
  public AdornerState Adorners => _adorners;

  public CanvasDragMode Mode => _mode;

  /// <summary>True while a drag is running, or a port press is waiting for enough movement to start a connection.</summary>
  public bool IsGestureActive => _mode != CanvasDragMode.None || _pendingPort != null;

  /// <summary>The button that owns the running gesture; <see cref="PointerButton.None" /> when idle.</summary>
  public PointerButton GestureButton => _gestureButton;

  /// <summary>Raised when an element is double-clicked (open its properties).</summary>
  public event Action<Element> EditRequested;

  /// <summary>Raised with the world position when a right click ends without having panned.</summary>
  public event Action<Vector> ContextMenuRequested;

  public void Dispose()
  {
    _session.Changed -= OnSessionChanged;
    _session.InteractionActiveProbe = null;
    // The host is going away mid-gesture: close the transaction without rolling back (that would replace the project).
    CommitTransaction();
    ResetGestureState();
  }

  #region Pointer

  /// <summary>Handle a button press. Returns true when the press was consumed.</summary>
  public bool OnPointerPressed(PointerInput input)
  {
    // A drag (or a not-yet-started connection drag) owns the pointer; ignore other buttons.
    if (IsGestureActive) return true;
    if (input.Button == PointerButton.None) return false;

    _tooltipSuppressed = true;
    _gestureProject = _session.Project;
    var world = _session.Viewport.ClientToCanvas(input.Client);
    _lastWorld = world;
    _gestureButton = input.Button;

    if (input.Button == PointerButton.Right)
    {
      _panMoved = false;
      _contextWorld = world;
    }

    if (input.Button == PointerButton.Middle || input.Button == PointerButton.Right ||
        (input.Button == PointerButton.Left && _spaceHeld))
    {
      _mode = CanvasDragMode.Pan;
      _panStartClient = input.Client;
      _panLastClient = input.Client;
      Refresh();
      return true;
    }

    if (input.Button != PointerButton.Left) return false;

    var shift = IsShift(input.Modifiers);
    var ctrl = IsCtrl(input.Modifiers);

    if (IsReferenceJumpModifier(input.Modifiers) && TryJumpToReferencedRoom(world))
    {
      _gestureButton = PointerButton.None;
      Refresh();
      return true;
    }

    if (input.ClickCount == 2)
    {
      var doubleHit = _hit.HitElement(world);
      if (doubleHit != null)
      {
        _session.Select(new[] { doubleHit }, true);
        _gestureButton = PointerButton.None;
        EditRequested?.Invoke(doubleHit);
        Refresh();
        return true;
      }

      // Legacy IsEmptySpace: no handle, port, waypoint or element under the pointer.
      if (ApplicationSettingsController.AppSettings.DoubleClickToAddRoom && IsEmptySpace(world))
      {
        _gestureButton = PointerButton.None;
        _session.AddRoom(world);
        Refresh();
        return true;
      }
    }

    RefreshLists();
    var selection = _session.Selection;

    var waypoint = _hit.HitWaypoint(world);
    if (waypoint.HasValue && selection.Count == 1 && selection.Items[0] is Connection connection)
    {
      BeginTransaction("Move waypoint");
      _mode = CanvasDragMode.MoveWaypoint;
      _dragWaypoint = waypoint.Value;
      _dragWaypointStartClient = input.Client;
      // A "ghost" handle only becomes a real (and selected) waypoint once it is dragged past the threshold.
      _session.SelectedWaypoint = connection.GetCurveWaypoint(_dragWaypoint).HasValue ? _dragWaypoint : null;
      _dragWaypointOffset = connection.GetCurveWaypointHandlePosition(_dragWaypoint) - world;
      Refresh();
      return true;
    }

    var handle = _hit.HitHandle(world, _handles);
    if (handle != null)
    {
      BeginTransaction("Resize");
      _mode = CanvasDragMode.MoveResizeHandle;
      _dragHandle = handle;
      _hoverHandle = handle;
      _dragHandleLast = world;
      Refresh();
      return true;
    }

    var port = _hit.HitPort(world, _ports);
    if (port != null)
    {
      if (port is MoveablePort moveable)
      {
        BeginTransaction("Move connection end");
        _mode = CanvasDragMode.MovePort;
        _dragPort = moveable;
        _dragOffset = Settings.Snap(world - moveable.Position);
      }
      else
      {
        _pendingPort = port;
        _pendingStartClient = input.Client;
        _pendingStartWorld = world;
      }

      Refresh();
      return true;
    }

    var element = _hit.HitElement(world);
    if (element != null)
    {
      if (ctrl)
      {
        _session.Select(element, true, false);
      }
      else if (shift)
      {
        _session.Select(element, false, true);
      }
      else if (!selection.Contains(element))
      {
        _session.Select(new[] { element }, true);
      }

      if (selection.Contains(element))
      {
        BeginTransaction("Move");
        _mode = CanvasDragMode.MoveElement;
        _dragOffset = Settings.Snap(world);
      }

      Refresh();
      return true;
    }

    if (!shift && !ctrl) _session.ClearSelection();
    _mode = CanvasDragMode.Marquee;
    _marqueeStart = world;
    _marqueeCurrent = world;
    Refresh();
    return true;
  }

  public void OnPointerMoved(PointerInput input)
  {
    var world = _session.Viewport.ClientToCanvas(input.Client);
    _lastWorld = world;
    _referenceJumpHeld = IsReferenceJumpModifier(input.Modifiers);
    if (_mode == CanvasDragMode.None && _pendingPort == null) _tooltipSuppressed = false;

    if (_pendingPort != null)
    {
      var distance = (input.Client - _pendingStartClient).Length;
      if (distance >= Settings.DragDistanceToInitiateNewConnection) BeginNewConnection();
    }

    switch (_mode)
    {
      case CanvasDragMode.Pan:
        if (!_panMoved)
        {
          var fromStart = input.Client - _panStartClient;
          if (Math.Abs(fromStart.X) > ClickTolerance || Math.Abs(fromStart.Y) > ClickTolerance) _panMoved = true;
        }

        var deltaClient = input.Client - _panLastClient;
        _panLastClient = input.Client;
        _session.Viewport.PanClient(new Vector(-deltaClient.X, -deltaClient.Y));
        break;

      case CanvasDragMode.MoveElement:
        var moveDelta = Settings.Snap(world) - _dragOffset;
        if (moveDelta != Vector.Zero)
        {
          MapEditing.Move(_session.Project.Elements, _session.Selection.Items.ToList(), moveDelta);
          _dragOffset += moveDelta;
        }

        break;

      case CanvasDragMode.MoveResizeHandle:
        if (_dragHandle != null) _dragHandleLast = MapEditing.Resize(_dragHandle, _dragHandleLast, world);
        break;

      case CanvasDragMode.MovePort:
        MovePort(world);
        break;

      case CanvasDragMode.MoveWaypoint:
        if (_session.Selection.Count == 1 && _session.Selection.Items[0] is Connection waypointOwner)
        {
          if (!waypointOwner.GetCurveWaypoint(_dragWaypoint).HasValue)
          {
            if ((input.Client - _dragWaypointStartClient).Length <= Settings.DragDistanceToInitiateNewConnection) break;
            if (!waypointOwner.CanAddCurveWaypoint(_dragWaypoint)) break;
          }

          waypointOwner.SetCurveWaypoint(_dragWaypoint, Settings.Snap(world + _dragWaypointOffset));
          _session.SelectedWaypoint = _dragWaypoint;
        }

        break;

      case CanvasDragMode.Marquee:
        _marqueeCurrent = world;
        break;

      case CanvasDragMode.None:
        if (_pendingPort == null) UpdateHover(world);
        break;
    }

    Rebuild();
  }

  public void OnPointerReleased(PointerInput input)
  {
    if (!IsGestureActive) return;
    // Releasing a secondary button must not end a gesture owned by another button.
    if (_gestureButton != PointerButton.None && input.Button != _gestureButton) return;

    EndGesture(input.Modifiers);
  }

  /// <summary>
  ///   Abort the running gesture (Esc, pointer capture lost): the map goes back to how it was before the drag and
  ///   nothing is recorded. Does nothing when idle.
  /// </summary>
  public void Cancel()
  {
    if (!IsGestureActive) return;

    var hadTransaction = _transaction != null;
    ResetGestureState();
    CancelTransaction();
    if (hadTransaction) ClearHover();
    Refresh();
  }

  public void OnWheel(WheelInput input)
  {
    var direction = input.DeltaY > 0 ? 1 : input.DeltaY < 0 ? -1 : 0;
    _session.Viewport.ZoomWheel(direction, input.Client, IsCtrl(input.Modifiers),
      ApplicationSettingsController.AppSettings.InvertMouseWheel);
    _tooltipSuppressed = true;
    Refresh();
  }

  #endregion

  #region Keys

  /// <summary>Handle Space, Escape and the arrow keys. Returns true when the key was consumed.</summary>
  public bool OnKey(KeyInput input)
  {
    switch (input.Key)
    {
      case InputKey.Space:
        SetSpaceHeld(true);
        return true;

      case InputKey.Escape:
        if (IsGestureActive) Cancel();
        else _session.ClearSelection();
        return true;

      case InputKey.Left:
      case InputKey.Right:
      case InputKey.Up:
      case InputKey.Down:
        if (_session.Selection.Count == 0) return false;
        // Shift and Primary arrows are navigation commands (047, 048), never a nudge; only Primary+Alt resizes.
        if (input.Modifiers != ModifierKeys.None && !(IsCtrl(input.Modifiers) && (input.Modifiers & ModifierKeys.Alt) != 0))
          return false;
        HandleArrow(input);
        return true;
    }

    return false;
  }

  public void OnKeyUp(KeyInput input)
  {
    if (input.Key == InputKey.Space) SetSpaceHeld(false);
  }

  public void SetSpaceHeld(bool held)
  {
    if (_spaceHeld == held) return;
    _spaceHeld = held;
    Rebuild();
  }

  private void HandleArrow(KeyInput input)
  {
    var unit = input.Key switch
    {
      InputKey.Left => new Vector(-1, 0),
      InputKey.Right => new Vector(1, 0),
      InputKey.Up => new Vector(0, -1),
      _ => new Vector(0, 1)
    };

    if (IsCtrl(input.Modifiers) && (input.Modifiers & ModifierKeys.Alt) != 0)
    {
      var step = Settings.SnapToGrid ? Settings.GridSize : 2.0f;
      // One undo step per key press (key repeat makes one step per repeat).
      _session.ResizeSelected(unit * step);
    }
    else
    {
      var step = Settings.SnapToGrid ? Settings.GridSize : 2.0f;
      _session.NudgeSelected(unit * step);
    }
  }

  #endregion

  #region Gestures

  private void BeginNewConnection()
  {
    var port = _pendingPort;
    _pendingPort = null;
    BeginTransaction("Add connection");
    var project = _session.Project;
    var defaults = _session.NewConnection;
    var connection = new Connection(project, new Vertex(port), new Vertex(port))
    {
      Style = defaults.Style,
      Flow = defaults.Flow
    };
    connection.SetText(defaults.Label);
    project.Elements.Add(connection);
    _dragNewConnection = connection;
    _newConnectionFrom = port;
    _mode = CanvasDragMode.MovePort;
    _dragPort = (MoveablePort)connection.PortList[1];
    _dragOffset = Settings.Snap(_pendingStartWorld - connection.VertexList[0].Position);
    _hoverPort = null;
    _session.Selection.Set(new Element[] { connection });
  }

  private void MovePort(Vector world)
  {
    if (_dragPort == null) return;

    var target = _hit.HitElement(world, true, _dragPort.Owner);
    if (!ReferenceEquals(target, _hoverElement))
    {
      _hoverElement = target;
      RefreshLists();
    }

    _hoverPort = _hit.HitPort(world, _ports, true, _dragPort);
    if (_hoverPort != null && _hoverPort is not MoveablePort)
    {
      // Only dock when the hovered port changes, so the docked end stays stable while the pointer stays on it.
      if (!ReferenceEquals(_dragPort.DockedAt, _hoverPort)) _dragPort.DockAt(_hoverPort);
    }
    else
    {
      if (_dragPort.DockedAt != null) _dragPort.DockAt(null);
      _dragPort.SetPosition(Settings.Snap(world) - _dragOffset);
    }
  }

  private void UpdateHover(Vector world)
  {
    var element = _hit.HitElement(world);
    _hoverElement = element;
    RefreshLists();
    _hoverWaypoint = _hit.HitWaypoint(world);
    _hoverHandle = _hit.HitHandle(world, _handles);
    _hoverPort = _hit.HitPort(world, _ports);
  }

  private void EndGesture(ModifierKeys modifiers)
  {
    var mode = _mode;
    var port = _dragPort;
    var newConnection = _dragNewConnection;
    var wasRight = _gestureButton == PointerButton.Right;
    var panMoved = _panMoved;
    var contextWorld = _contextWorld;
    var marquee = MarqueeRect();

    // Reset first so re-entrant events (selection changes, capture lost) see an idle canvas.
    ResetGestureState();

    switch (mode)
    {
      case CanvasDragMode.MovePort:
        FinishPortDrag(port, newConnection);
        break;

      case CanvasDragMode.Marquee:
        ApplyMarquee(marquee, modifiers);
        CommitTransaction();
        break;

      default:
        CommitTransaction();
        break;
    }

    Refresh();

    if (mode == CanvasDragMode.Pan && wasRight && !panMoved) ContextMenuRequested?.Invoke(contextWorld);
  }

  private void FinishPortDrag(MoveablePort port, Connection newConnection)
  {
    if (port != null && port.Owner is Connection connection)
    {
      if (IsDegenerateConnection(connection))
      {
        // Put everything back: removes a connection created by this drag, restores an existing one's end.
        CancelTransaction();
        ClearHover();
        if (newConnection != null) _session.ClearSelection();
        return;
      }

      _session.Selection.Set(new Element[] { connection });
    }

    CommitTransaction();
  }

  // Legacy EndDrag marquee: no modifier replaces the selection; Ctrl adds; Shift (with or without Ctrl) toggles.
  private void ApplyMarquee(Rect rect, ModifierKeys modifiers)
  {
    var hits = _hit.HitRect(rect);
    var shift = IsShift(modifiers);
    if (!shift && !IsCtrl(modifiers))
    {
      _session.Selection.Set(hits);
      return;
    }

    foreach (var element in hits)
    {
      if (!_session.Selection.Contains(element)) _session.Selection.Add(element);
      else if (shift) _session.Selection.Remove(element);
    }
  }

  private bool IsEmptySpace(Vector world)
  {
    RefreshLists();
    if (_hit.HitWaypoint(world).HasValue) return false;
    if (_hit.HitHandle(world, _handles) != null) return false;
    if (_hit.HitPort(world, _ports) != null) return false;
    return _hit.HitElement(world) == null;
  }

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

  private Rect MarqueeRect()
  {
    var minX = Math.Min(_marqueeStart.X, _marqueeCurrent.X);
    var maxX = Math.Max(_marqueeStart.X, _marqueeCurrent.X);
    var minY = Math.Min(_marqueeStart.Y, _marqueeCurrent.Y);
    var maxY = Math.Max(_marqueeStart.Y, _marqueeCurrent.Y);
    return new Rect(minX, minY, Math.Max(0.01f, maxX - minX), Math.Max(0.01f, maxY - minY));
  }

  private void ResetGestureState()
  {
    _mode = CanvasDragMode.None;
    _gestureButton = PointerButton.None;
    _dragHandle = null;
    _dragPort = null;
    _dragNewConnection = null;
    _newConnectionFrom = null;
    _pendingPort = null;
    _panMoved = false;
  }

  private void BeginTransaction(string title)
  {
    CommitTransaction();
    _transaction = _session.BeginTransaction(title);
  }

  private void CommitTransaction()
  {
    var transaction = _transaction;
    _transaction = null;
    transaction?.Dispose();
  }

  private void CancelTransaction()
  {
    var transaction = _transaction;
    _transaction = null;
    transaction?.Cancel();
  }

  #endregion

  #region Adorners

  private void OnSessionChanged(EditorChange change)
  {
    if ((change & (EditorChange.Model | EditorChange.Selection)) == 0) return;
    if (_session.Project == null) return;

    if (IsGestureActive)
    {
      // The map was swapped under the gesture (a different document was opened): nothing to roll back to.
      if (!ReferenceEquals(_gestureProject, _session.Project)) Abort();
      return;
    }

    if (_hoverElement != null && !_session.Project.Elements.Any(e => ReferenceEquals(e, _hoverElement)))
      _hoverElement = null;
    Refresh();
  }

  private void Abort()
  {
    ResetGestureState();
    CommitTransaction();
    ClearHover();
    Refresh();
  }

  private void ClearHover()
  {
    _hoverElement = null;
    _hoverPort = null;
    _hoverHandle = null;
    _hoverWaypoint = null;
  }

  private void RefreshLists()
  {
    _handles = _hit.ActiveHandles();
    _ports = _hit.ActivePorts(_hoverElement, _mode == CanvasDragMode.MovePort);

    if (_hoverHandle != null)
    {
      var previous = _hoverHandle;
      _hoverHandle = _handles.FirstOrDefault(h =>
        h.CompassPoint == previous.CompassPoint && ReferenceEquals(h.Owner, previous.Owner));
    }

    if (_hoverPort != null && !_ports.Contains(_hoverPort)) _hoverPort = null;
  }

  private void Refresh()
  {
    RefreshLists();
    Rebuild();
  }

  private CursorKind DetermineCursor()
  {
    if (_mode == CanvasDragMode.Pan || _spaceHeld) return CursorKind.Pan;
    if (_mode == CanvasDragMode.None && _referenceJumpHeld && _hoverElement is Room { IsReference: true })
      return CursorKind.Hand;
    if (_mode == CanvasDragMode.MoveWaypoint || (_mode == CanvasDragMode.None && _hoverWaypoint.HasValue))
      return CursorKind.SizeAll;
    if (_mode == CanvasDragMode.MovePort || (_hoverPort != null && _hoverPort is not MoveablePort))
      return CursorKind.DrawLine;
    if (_hoverPort is MoveablePort) return CursorKind.Hand;
    if (_hoverHandle != null)
    {
      switch (_hoverHandle.CompassPoint)
      {
        case CompassPoint.NorthWest:
        case CompassPoint.SouthEast:
          return CursorKind.ResizeNWSE;
        case CompassPoint.NorthEast:
        case CompassPoint.SouthWest:
          return CursorKind.ResizeNESW;
        case CompassPoint.North:
        case CompassPoint.South:
          return CursorKind.ResizeNS;
        case CompassPoint.East:
        case CompassPoint.West:
          return CursorKind.ResizeEW;
        default:
          return CursorKind.Default;
      }
    }

    if (_hoverElement != null && _session.Selection.Contains(_hoverElement) && _hoverElement is IMoveable)
      return CursorKind.SizeAll;
    return CursorKind.Default;
  }

  private void Rebuild()
  {
    Rect? marquee = _mode == CanvasDragMode.Marquee ? MarqueeRect() : null;

    PendingLine? pending = null;
    if (_dragNewConnection != null && _dragPort != null)
      pending = new PendingLine(_newConnectionFrom, _dragPort.Position);
    else if (_pendingPort != null)
      pending = new PendingLine(_pendingPort, _lastWorld);

    // No tooltip while a button is down, the wheel was just used, or a drag is under way (legacy hid it on mouse down/wheel).
    var tooltip = _mode == CanvasDragMode.None && _pendingPort == null && !_tooltipSuppressed
      ? TooltipModel.Create(_hoverElement)
      : null;

    var state = new AdornerState(
      _hoverElement,
      _hoverPort,
      _hoverHandle,
      _hoverWaypoint,
      _mode == CanvasDragMode.MoveWaypoint ? _dragWaypoint : null,
      _handles,
      _ports,
      marquee,
      pending,
      DetermineCursor(),
      tooltip);

    if (SameState(_adorners, state)) return;

    _adorners = state;
    _session.Invalidate(EditorChange.Adorners);
  }

  private static bool SameState(AdornerState a, AdornerState b)
  {
    return ReferenceEquals(a.HoverElement, b.HoverElement)
           && ReferenceEquals(a.HoverPort, b.HoverPort)
           && SameHandle(a.HoverHandle, b.HoverHandle)
           && a.HoverWaypoint == b.HoverWaypoint
           && a.DragWaypoint == b.DragWaypoint
           && a.Cursor == b.Cursor
           && ReferenceEquals(a.TooltipTarget, b.TooltipTarget)
           && a.Tooltip?.Kind == b.Tooltip?.Kind
           && Equals(a.Marquee, b.Marquee)
           && Equals(a.PendingConnection, b.PendingConnection)
           && SameHandles(a.Handles, b.Handles)
           && a.Ports.SequenceEqual(b.Ports);
  }

  private static bool SameHandle(ResizeHandle a, ResizeHandle b)
  {
    if (a == null || b == null) return a == null && b == null;
    return a.CompassPoint == b.CompassPoint && ReferenceEquals(a.Owner, b.Owner);
  }

  private static bool SameHandles(IReadOnlyList<ResizeHandle> a, IReadOnlyList<ResizeHandle> b)
  {
    if (a.Count != b.Count) return false;
    for (var i = 0; i < a.Count; i++)
    {
      if (!SameHandle(a[i], b[i])) return false;
    }

    return true;
  }

  #endregion

  // Ctrl alone (no Shift or Alt), as the reference's jump to the original of a reference room (062).
  private static bool IsReferenceJumpModifier(ModifierKeys modifiers)
  {
    return modifiers == ModifierKeys.Control || modifiers == ModifierKeys.Primary;
  }

  private bool TryJumpToReferencedRoom(Vector world)
  {
    var selection = _session.Selection;
    if (selection.Count != 1 || selection.Primary is not Room { IsReference: true } room) return false;
    if (_hit.HitElement(world) != room) return false;

    return _session.JumpToReferencedRoom(room);
  }

  private static bool IsShift(ModifierKeys modifiers)
  {
    return (modifiers & ModifierKeys.Shift) != 0;
  }

  // Hosts report Ctrl as Primary for key gestures but as Control for pointer events; both mean "Ctrl" here.
  private static bool IsCtrl(ModifierKeys modifiers)
  {
    return (modifiers & (ModifierKeys.Control | ModifierKeys.Primary)) != 0;
  }
}
