using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PdfSharp.Drawing;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;
using Timer = System.Threading.Timer;

// ReSharper disable PossibleLossOfFraction
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable CanBeReplacedWithTryCastAndCheckForNull

namespace Trizbort.UI.Controls {
  public sealed partial class Canvas : UserControl, IAutomapCanvas {
    private const int RecomputeNMillisecondsAfterChange = 500;
    private static bool _smartLineSegmentsUpToDate;
    private readonly CommandController _commandController;
    private readonly List<ResizeHandle> _handles = new List<ResizeHandle>();
    private readonly List<Port> _ports = new List<Port>();
    private readonly Timer _recomputeTimer;
    private readonly List<Element> _selectedElements = new List<Element>();
    private Room _lastSelectedRoom;
    private bool _doNotUpdateScrollBarsNextPaint;
    private Vector _dragMarqueeLastPosition;
    private DragModes _dragMode;
    private MoveablePort _dragMovePort;
    private Vector _dragOffsetCanvas;
    private Vector _dragResizeHandleLastPosition;
    private Element _hoverElement;
    private ResizeHandle _hoverHandle;
    private Port _hoverPort;
    private CurveWaypoint? _hoverWaypoint;
    private CurveWaypoint? _selectedWaypoint;
    private CurveWaypoint _dragWaypoint;
    private Vector _dragWaypointOffset;
    private Point _lastKnownMousePosition;
    private Point _lastMouseDownPosition;
    private ConnectionFlow _newConnectionFlow;
    private ConnectionLabel _newConnectionLabel;
    private ConnectionStyle _newConnectionStyle;
    private bool _newRoomIsDark;
    private CompassPoint _newRoomObjectsPosition;
    private Vector _newRoomSize;
    private Room _newRoomStyleSource;
    private Vector _origin;
    private PointF _panPosition;
    private bool _updatingScrollBars;
    private float _zoomFactor;

    public Canvas() {
      InitializeComponent();

      _commandController = new CommandController(this);

      SetStyle(ControlStyles.Selectable, true);
      TabStop = true;
      TabIndex = 0;

      SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
      DoubleBuffered = true;

      PreviewKeyDown += OnPreviewKeyDown;
      _ctxCanvasMenu.Items.Insert(0, new ToolStripMenuItem("Add &Label", null, (_, __) => AddLabel(true)));

      _recomputeTimer = new Timer(OnRecomputeTimerTick);

      Project.ProjectChanged += OnProjectChanged;
      OnProjectChanged(this, new ProjectChangedEventArgs(null, Project.Current));

      Settings.Changed += OnSettingsChanged;
      OnSettingsChanged(this, EventArgs.Empty);

      _threadSafeAutomapCanvas = new MultithreadedAutomapCanvas(this);
      _minimap.Canvas = this;
    }

    public bool CanDrawLine => true;

    public bool CanSelectElements => true;

    public override Cursor Cursor {
      get {
        if (DragMode == DragModes.MoveWaypoint || DragMode == DragModes.None && HoverWaypoint.HasValue) return Cursors.SizeAll;

        if (CanDrawLine && (HoverPort != null && !(HoverPort is MoveablePort) || DragMode == DragModes.MovePort)) return Drawing.DrawLineCursor;

        if (HoverPort is MoveablePort) return Drawing.MoveLineCursor;

        var cursor = HoverHandle?.Cursor;
        if (cursor != null) return cursor;

        if (HoverElement is IMoveable && _selectedElements.Contains(HoverElement)) return Cursors.SizeAll;
        return base.Cursor;
      }
      set => base.Cursor = value;
    }

    public bool HasSelectedRooms => _selectedElements.OfType<Room>().Any();
    public bool HasSingleSelectedElement => SelectedElementCount == 1;
    public bool HasSingleSelectedRoom => _selectedElements.OfType<Room>().Count() == 1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Element HoverElement {
      get => _hoverElement;
      set {
        if (_hoverElement == value) return;
        _hoverElement = value;
        RecreatePorts();
      }
    }

    public bool MinimapVisible {
      get => _minimap.Visible;
      set {
        _minimap.Visible = value;
        if (!_minimap.Visible) {
          _vScrollBar.Top = 0;
          _vScrollBar.Height = Height - _hScrollBar.Height;
        } else {
          _vScrollBar.Top = _minimap.Bottom;
          _vScrollBar.Height = Height - _hScrollBar.Height - _minimap.Height;
        }
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConnectionFlow NewConnectionFlow {
      get => _newConnectionFlow;
      set {
        if (_newConnectionFlow == value) return;
        _newConnectionFlow = value;
        RaiseNewConnectionFlowChanged();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConnectionLabel NewConnectionLabel {
      get => _newConnectionLabel;
      set {
        if (_newConnectionLabel == value) return;
        _newConnectionLabel = value;
        RaiseNewConnectionLabelChanged();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConnectionStyle NewConnectionStyle {
      get => _newConnectionStyle;
      set {
        if (_newConnectionStyle == value) return;
        _newConnectionStyle = value;
        RaiseNewConnectionStyleChanged();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Vector Origin {
      private get => _origin;
      set {
        if (_origin == value) return;
        _origin = value;
        Invalidate();
      }
    }

    public List<Connection> SelectedConnections { get { return _selectedElements.Where(p => p is Connection).ToList().Cast<Connection>().ToList(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Element SelectedElement {
      get => _selectedElements.Count > 0 ? _selectedElements[_selectedElements.Count - 1] : null;
      set {
        var selectedElement = _selectedElements.Count > 0 ? _selectedElements[_selectedElements.Count - 1] : null;
        if (selectedElement != value) {
          _selectedElements.Clear();
          if (value != null) _selectedElements.Add(value);
          UpdateSelection();
        }
      }
    }

    public int SelectedElementCount => _selectedElements.Count;

    public IList<Element> SelectedElements => _selectedElements;

    public List<Room> SelectedRooms { get { return _selectedElements.Where(p => p is Room).ToList().Cast<Room>().ToList(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rect Viewport {
      get {
        var origin = Origin;
        var size = ClientToCanvas(new SizeF(Width, Height));
        return new Rect(origin.X - size.Width / 2, origin.Y - size.Height / 2, size.Width, size.Height);
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float ZoomFactor {
      get => _zoomFactor;
      set {
        if (_zoomFactor != value) {
          _zoomFactor = value;
          _lblZoom.Text = _zoomFactor.ToString("p0");
          Invalidate();
        }
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private DragModes DragMode {
      get => _dragMode;
      set {
        _dragMode = value;
        RecreatePorts();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private ResizeHandle HoverHandle {
      get => _hoverHandle;
      set {
        if (_hoverHandle == value) return;
        _hoverHandle = value;
        Invalidate();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private Port HoverPort {
      get => _hoverPort;
      set {
        if (_hoverPort == value) return;
        _hoverPort = value;
        Invalidate();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private CurveWaypoint? HoverWaypoint {
      get => _hoverWaypoint;
      set {
        if (_hoverWaypoint == value) return;
        _hoverWaypoint = value;
        Invalidate();
      }
    }

    /// <summary>
    ///   The connection whose curve waypoint handles are currently shown, if any.
    /// </summary>
    private Connection WaypointConnection => CanSelectElements && HasSingleSelectedElement && SelectedElement is Connection connection && connection.SupportsCurveWaypoints ? connection : null;

    private static float SnapToElementSizeAtCurrentZoomFactor => Settings.SnapToElementSize;

    public void RedrawAllRoomsWithDashes() {
      var rooms = Project.Current.Elements.OfType<Room>().ToList();
      if (rooms.Count == 0) return;

      DrawingContext context = new DrawingContext(ZoomFactor) {
        Selected = false,
        Hover = false
      };
      Palette palette = new Palette();

      // this is the only way I know of to redraw the room
      // if some other way is easier, best to implement Redraw method in the Room object
      var size = ComputeCanvasBounds(true).Size * (ApplicationSettingsController.AppSettings.SaveAt100 ? 1.0f : ZoomFactor);
      size.X = Numeric.Clamp(size.X, 16, 8192);
      size.Y = Numeric.Clamp(size.Y, 16, 8192);
      using var nativeGraphics = Graphics.FromHwnd(Handle);
      using var stream = new System.IO.MemoryStream();
      try {
        var dc = nativeGraphics.GetHdc();
        using var metafile = new System.Drawing.Imaging.Metafile(stream, dc);
        using var imageGraphics = Graphics.FromImage(metafile);
        using var graphics = XGraphics.FromGraphics(imageGraphics, new XSize(size.X, size.Y));
        foreach (var room in rooms) {
          if (room.Name.Contains("-")) {
            room.MarkNameInvalid();
            room.Draw(graphics, palette, context);
          }
        }
      }
      catch {
      }
      finally {
        nativeGraphics.ReleaseHdc();
      }
    }

    public void RemoveRoom(Room otherRoom) {
      Project.Current.Elements.Remove(otherRoom);
    }

    public MapLabel AddLabel(bool atCursor, bool showDialog = true) {
      var label = new MapLabel(Project.Current);
      var center = atCursor && ClientRectangle.Contains(PointToClient(MousePosition))
        ? ClientToCanvas(PointToClient(MousePosition)) : Origin;
      label.Position = Settings.Snap(center - label.Size / 2);
      Project.Current.Elements.Add(label);
      SelectedElement = label;
      if (showDialog) label.ShowDialog();
      return label;
    }

    public Room AddRoom(bool atCursor, bool insertRoom = false, bool doRefresh = true) {
      var room = new Room(Project.Current) {Size = _newRoomSize};
      if (ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms)
        room.CopyStyleFrom(_newRoomStyleSource);

      // Changed this to ignore ID gaps. ID gaps are resolved on load

      Vector pos;
      if (atCursor && ClientRectangle.Contains(PointToClient(MousePosition)))
        pos = ClientToCanvas(PointToClient(MousePosition));
      else
        pos = new Vector(Origin.X - room.Size.X / 2, Origin.Y - room.Size.Y / 2);

      // rooms' origins are in the top left corner
      pos -= room.Size / 2;

      // snap to the grid, if required
      pos = Settings.Snap(pos);

      var clash = true;
      while (clash) {
        clash = false;
        foreach (var element in Project.Current.Elements)
          if (element is IMoveable && ((IMoveable) element).Position == pos) {
            pos.X += Math.Max(2, Settings.GridSize);
            pos.Y += Math.Max(2, Settings.GridSize);
            clash = true;
          }
      }

      room.Position = pos;
      Project.Current.Elements.Add(room);

      if (insertRoom)
        if (SelectedElement is Connection) {
          var conn = (Connection) SelectedElement;

          var targetPort = conn.VertexList[conn.VertexList.Count - 1].Port as Room.CompassPort;
          var sourcePort = conn.VertexList[0].Port as Room.CompassPort;
          var target = targetPort?.Owner;
          var source = sourcePort?.Owner;
          var targetCompass = targetPort?.CompassPoint ?? CompassPoint.North;
          var sourceCompass = sourcePort?.CompassPoint ?? CompassPoint.North;

          if (target == null && source == null) {
            conn.VertexList.Add(new Vertex(room.PortAt(CompassPointHelper.GetOpposite(sourceCompass))));
          } else if (source == null) {
            conn.VertexList.RemoveAt(0);
            conn.VertexList.Add(new Vertex(room.PortAt(CompassPointHelper.GetOpposite(targetCompass))));
          } else if (target == null) {
            conn.VertexList.RemoveAt(conn.VertexList.Count - 1);
            conn.VertexList.Add(new Vertex(room.PortAt(CompassPointHelper.GetOpposite(sourceCompass))));
          } else {
            if (target is Room targetRoom && source is Room sourceRoom && targetRoom.Region == sourceRoom.Region)
              room.Region = targetRoom.Region;

            AddConnection(source, sourceCompass, room, targetCompass);
            AddConnection(room, sourceCompass, target, targetCompass);

            Project.Current.Elements.Remove(conn);
          }
        }

      SelectedElement = room;
      if (doRefresh)
        Refresh();

      return room;
    }

    public void ApplyNewPlainConnectionSettings() {
      // apply sequentially as each change will affect our defaults,
      // so setting the style will cause us to take the existing flow and label, etc.

      _commandController.SetConnectionStyle(ConnectionStyle.Solid);

      _commandController.SetConnectionFlow(ConnectionFlow.TwoWay);

      _commandController.SetConnectionLabel(ConnectionLabel.None);

      ClearMidText();

      SetDefaultConnectionColor();
    }

    public PointF CanvasToClient(Vector v) {
      v.X -= Origin.X;
      v.X *= ZoomFactor;
      v.X += Width / 2;
      v.Y -= Origin.Y;
      v.Y *= ZoomFactor;
      v.Y += Height / 2;
      return new PointF(v.X, v.Y);
    }

    public SizeF CanvasToClient(SizeF s) {
      s.Width *= ZoomFactor;
      s.Height *= ZoomFactor;
      return s;
    }

    public void ChangeZoom(float zoom) {
      _lblZoom.Text = zoom.ToString("p0");
      _zoomFactor = zoom;
      Invalidate();
    }

    public void ClearMidText() {
      foreach (var connection in SelectedConnections.Where(element => element != null)) connection.MidText = string.Empty;
      Invalidate();
    }

    public Vector ClientToCanvas(PointF p) {
      p.X -= Width / 2;
      p.X /= ZoomFactor;
      p.X += Origin.X;
      p.Y -= Height / 2;
      p.Y /= ZoomFactor;
      p.Y += Origin.Y;
      return new Vector(p.X, p.Y);
    }

    public SizeF ClientToCanvas(SizeF s) {
      s.Width /= ZoomFactor;
      s.Height /= ZoomFactor;
      return s;
    }

    public Rect ComputeCanvasBounds(bool includePadding) {
      var bounds = Project.Current.Elements.Aggregate(Rect.Empty, (current, element) => element.UnionBoundsWith(current, true));

      if (includePadding) {
        if (Settings.DocumentSpecificMargins) {
          bounds.Inflate(Settings.DocHorizontalMargin, Settings.DocVerticalMargin);
          return bounds;
        }

        if (ApplicationSettingsController.AppSettings.SpecifyGenMargins) {
          bounds.Inflate(ApplicationSettingsController.AppSettings.GenHorizontalMargin, ApplicationSettingsController.AppSettings.GenVerticalMargin);
          return bounds;
        }

        // HACK: fudge the canvas size to allow for overhanging line/object text
        var v1 = Settings.SubtitleFont.GetHeight();
        var v2 = Settings.ObjectFont.GetHeight() * 4;
        bounds.Inflate(Math.Max(v1, v2));
      }

      return bounds;
    }

    public void CopySelectedColor() {
      var controller = new CopyController();
      if (SelectedElement is Room room) controller.CopyColors(room);
    }

    public void CopySelectedElements() {
      var controller = new CopyController();
      controller.CopyElements(_selectedElements);
    }

    public void DeleteSelection() {
      var connection = WaypointConnection;
      if (connection != null && _selectedWaypoint.HasValue && connection.RemoveCurveWaypoint(_selectedWaypoint.Value)) {
        _selectedWaypoint = null;
        HoverWaypoint = null;
        Invalidate();
        return;
      }

      var doomedElements = new List<Element>(_selectedElements);
      foreach (var element in doomedElements) Project.Current.Elements.Remove(element);
      _selectedElements.Clear();
      UpdateSelection();
    }

    /// <summary>
    ///   Draw the current project.
    /// </summary>
    /// <param name="graphics">The graphics with which to draw.</param>
    /// <param name="finalRender">True if rendering to PDF, an image, etc.; false if rendering to a window.</param>
    /// <param name="width">The width of the drawing area.</param>
    /// <param name="height">The height of the drawing area.</param>
    public void Draw(XGraphics graphics, bool finalRender, float width, float height) {
      var stopwatch = new Stopwatch();
      stopwatch.Start();

      var zoomFactor = ZoomFactor;
      var origin = Origin;
      if (finalRender) {
        // zoom to fit (0,0)-(width,height)
        var canvasBounds = ComputeCanvasBounds(true);
        ZoomFactor = Math.Min(canvasBounds.Width > 0 ? width / canvasBounds.Width : 1.0f, canvasBounds.Height > 0 ? height / canvasBounds.Height : 1.0f);
        Origin = new Vector(canvasBounds.X + canvasBounds.Width / 2, canvasBounds.Y + canvasBounds.Height / 2);
      }

      using (var palette = new Palette()) {
        // XGraphics.Graphics is null for PDF targets, so fill via XGraphics rather than Graphics.Clear.
        if (finalRender) graphics.DrawRectangle(palette.CanvasBrush, 0, 0, width, height);

        if (!finalRender) DrawGrid(graphics, palette);

        graphics.TranslateTransform(width / 2, height / 2);
        graphics.ScaleTransform(ZoomFactor, ZoomFactor);
        graphics.TranslateTransform(-Origin.X, -Origin.Y);

        if (ApplicationSettingsController.AppSettings.DebugShowFps && !finalRender) {
          var canvasBounds = ComputeCanvasBounds(true);
          graphics.DrawRectangle(XPens.Purple, canvasBounds.ToRectangleF());
        }

        if (Settings.ShowOrigin && !finalRender) {
          var pen = palette.Pen(Drawing.Mix(Settings.Color[Colors.Canvas], Settings.Color[Colors.SmallText], 3, 1));
          var n = Settings.GridSize;
          graphics.DrawLine(pen, -n, 0, n, 0);
          graphics.DrawLine(pen, 0, -n, 0, n);
        }

        graphics.SmoothingMode = XSmoothingMode.AntiAlias;

        DrawElements(graphics, palette, finalRender);
        if (!finalRender) {
          DrawHandles(graphics, palette);
          DrawPorts(graphics, palette);
          DrawMarquee(graphics, palette);
        }

        stopwatch.Stop();
        if (ApplicationSettingsController.AppSettings.DebugShowFps && !finalRender) {
          var fps = 1.0f / (float) stopwatch.Elapsed.TotalSeconds;
          graphics.Graphics.Transform = new Matrix();
          graphics.DrawString($"{stopwatch.Elapsed.TotalMilliseconds} ms ({fps} fps) {TextBlock.RebuildCount} rebuilds", Settings.RoomNameFont, Brushes.Red, new PointF(10, 20 + Settings.RoomNameFont.GetHeight()));
        }

        if (ApplicationSettingsController.AppSettings.DebugShowMouseCoordinates && !finalRender) {
          var mouseCoord = MousePosition;
          graphics.Graphics.Transform = new Matrix();
          graphics.DrawString($"X:{mouseCoord.X}  Y:{mouseCoord.Y}", Settings.RoomNameFont, Brushes.Green, new PointF(10, 40 + Settings.RoomNameFont.GetHeight()));
          graphics.DrawString(HoverElement == null ? new Point(0, 0).ToString() : PointToClient(HoverElement.Position.ToPoint()).ToString(), Settings.RoomNameFont, new SolidBrush(Color.YellowGreen), new PointF(10, 60 + Settings.RoomNameFont.GetHeight()));
        }
      }

      ZoomFactor = zoomFactor;
      Origin = origin;
    }

    public bool EqualEnough(CompassPoint dirOne, CompassPoint dirTwo) =>
      CompassPointHelper.IsSameApproximateDirection(dirOne, dirTwo);

    public int GetHighestZOrderIndex() {
      if (Project.Current.Elements.Count <= 0) return 0;
      var high = Project.Current.Elements.Select(p => p.ZOrder).OrderByDescending(p => p).FirstOrDefault();
      return high + 1;
    }

    public int GetLowestZOrderIndex() {
      if (Project.Current.Elements.Count <= 0) return 0;
      var low = Project.Current.Elements.Select(p => p.ZOrder).OrderBy(p => p).FirstOrDefault();
      return low - 1;
    }

    public bool HasSelectedElement<T>() where T : Element {
      return _selectedElements.OfType<T>().Any();
    }

    public void JoinSelectedRooms(Room room1, Room room2) {
      var rect1 = room1.InnerBounds;
      var rect2 = room2.InnerBounds;

      var dx = rect1.X - rect2.X;
      var dy = rect1.Y - rect2.Y;

      if (dy == 0 && dx != 0) {
        if (dx > 0)
          AddConnection(room1, CompassPoint.West, room2, CompassPoint.East);
        else
          AddConnection(room1, CompassPoint.East, room2, CompassPoint.West);
      } else if (dy != 0 && dx == 0) {
        if (dy > 0)
          AddConnection(room1, CompassPoint.North, room2, CompassPoint.South);
        else
          AddConnection(room1, CompassPoint.South, room2, CompassPoint.North);
      } else {
        if (Math.Abs(dy) >= Math.Abs(dx))
          if (dy > 0)
            AddConnection(room1, CompassPoint.North, room2, CompassPoint.South);
          else
            AddConnection(room1, CompassPoint.South, room2, CompassPoint.North);
        else if (dx > 0)
          AddConnection(room1, CompassPoint.West, room2, CompassPoint.East);
        else
          AddConnection(room1, CompassPoint.East, room2, CompassPoint.West);
      }
    }

    public event EventHandler NewConnectionFlowChanged;

    public event EventHandler NewConnectionLabelChanged;


    public event EventHandler NewConnectionStyleChanged;

    public void Paste(bool atCursor) {
      var controller = new CopyController();
      var objs = controller.PasteElements();

      if (objs != null)
        if (objs.GetType() == typeof(CopyController.CopyObject)) {
          var xx = objs as CopyController.CopyObject;
          PasteRooms(atCursor, xx, controller);
        } else if (objs.GetType() == typeof(CopyController.CopyColorsObj)) {
          var xx = objs as CopyController.CopyColorsObj;
          PasteColors(xx);
        }
    }

    public void ResetZoomOrigin() {
      Origin = ComputeCanvasBounds(false).Center;
      ZoomFactor = 2.0f;
    }

    public void ReverseLineDirection() {
      foreach (var element in _selectedElements)
        if (element is Connection) {
          var connection = (Connection) element;
          connection.Reverse();
        }
    }

    public CompassPoint? RoughOpposite(CompassPoint? cp) {
      if (cp == null) return null;

      switch (cp) {
        case CompassPoint.North:
        case CompassPoint.NorthNorthEast:
        case CompassPoint.NorthNorthWest:
          return CompassPoint.South;

        case CompassPoint.South:
        case CompassPoint.SouthSouthEast:
        case CompassPoint.SouthSouthWest:
          return CompassPoint.North;

        case CompassPoint.East:
        case CompassPoint.EastSouthEast:
        case CompassPoint.EastNorthEast:
          return CompassPoint.West;

        case CompassPoint.West:
        case CompassPoint.WestSouthWest:
        case CompassPoint.WestNorthWest:
          return CompassPoint.East;

        case CompassPoint.SouthWest:
          return CompassPoint.NorthEast;

        case CompassPoint.SouthEast:
          return CompassPoint.NorthWest;

        case CompassPoint.NorthWest:
          return CompassPoint.SouthEast;

        case CompassPoint.NorthEast:
          return CompassPoint.SouthWest;
      }

      return CompassPoint.North;
    }

    public void SelectAll() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements);
      UpdateSelection();
    }

    public void SelectAllConnections() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements.OfType<Connection>());
      UpdateSelection();
    }

    public void SelectAllRegion(IEnumerable<string> regions) {
      _selectedElements.Clear();
      var regionRooms = Project.Current.Elements.OfType<Room>().ToList().Where(p => regions.Contains(p.Region));
      _selectedElements.AddRange(regionRooms);
      UpdateSelection();
    }

    public void SelectAllRooms() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements.OfType<Room>());
      UpdateSelection();
    }

    public void SelectAllUnconnectedRooms() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements.OfType<Room>().Where(p => p.GetConnections().Count == 0));
      UpdateSelection();
    }

    public void SelectDanglingConnections() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements.OfType<Connection>().Where(p => p.IsDangling));
      UpdateSelection();
    }

    public void SelectElements(List<Element> elements) {
      _selectedElements.Clear();
      _selectedElements.AddRange(elements);
    }

    public void SelectRoomsWithObjects() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements.OfType<Room>().Where(p => p.ListOfObjects().Count > 0));
      UpdateSelection();
    }

    public void SelectRoomsWithoutObjects() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements.OfType<Room>().Where(p => p.ListOfObjects().Count == 0));
      UpdateSelection();
    }

    public void SelectSelfLoopingConnections() {
      _selectedElements.Clear();
      _selectedElements.AddRange(Project.Current.Elements.OfType<Connection>().Where(p => {
        var sourceRoom = p.GetSourceRoom();
        var targetRoom = p.GetTargetRoom();
        return sourceRoom != null && targetRoom != null && sourceRoom == targetRoom;
      }));
      UpdateSelection();
    }

    public void SetDefaultConnectionColor() {
      foreach (var connection in SelectedConnections.Where(element => element != null)) connection.ConnectionColor = Color.Transparent;
      Invalidate();
    }

    public void SwapRoomFill() {
      var selectedRooms = SelectedRooms;
      if (selectedRooms.Count != 2) return;

      var room1 = selectedRooms.First();
      var room2 = selectedRooms.Last();

      var tBs = room1.BorderStyle;
      var tRb = room1.RoomBorderColor;
      var tRf = room1.RoomFillColor;
      var tSf = room1.SecondFillColor;
      var tSfl = room1.SecondFillLocation;
      var tShape = room1.Shape;

      room1.BorderStyle = room2.BorderStyle;
      room1.RoomBorderColor = room2.RoomBorderColor;
      room1.RoomFillColor = room2.RoomFillColor;
      room1.SecondFillLocation = room2.SecondFillLocation;
      room1.SecondFillColor = room2.SecondFillColor;
      room1.Shape = room2.Shape;

      room2.BorderStyle = tBs;
      room2.RoomBorderColor = tRb;
      room2.RoomFillColor = tRf;
      room2.SecondFillLocation = tSfl;
      room2.SecondFillColor = tSf;
      room2.Shape = tShape;
    }

    public void SwapRoomNames() {
      var selectedRooms = SelectedRooms;
      if (selectedRooms.Count != 2) return;

      var room1 = selectedRooms.First();
      var room2 = selectedRooms.Last();

      var tName = room1.Name;
      room1.Name = room2.Name;
      room2.Name = tName;
    }

    public void SwapRoomRegions() {
      var selectedRooms = SelectedRooms;
      if (selectedRooms.Count != 2) return;

      var room1 = selectedRooms.First();
      var room2 = selectedRooms.Last();

      var tRegion = room1.Region;
      room1.Region = room2.Region;
      room2.Region = tRegion;
    }

    public void SwapRooms() {
      var selectedRooms = SelectedRooms;
      if (selectedRooms.Count != 2) return;

      var room1 = selectedRooms.First();
      var room2 = selectedRooms.Last();
      var objects = room1.Objects;
      room1.Objects = room2.Objects;
      room2.Objects = objects;
    }

    public void ToggleText() {
      ApplicationSettingsController.AppSettings.DebugDisableTextRendering = !ApplicationSettingsController.AppSettings.DebugDisableTextRendering;
      Invalidate();
    }

    public void UpdateScrollBars() {
      _updatingScrollBars = true;

      var topLeft = PointF.Empty;
      var displaySize = new PointF(Math.Max(0, Width - _vScrollBar.Width), Math.Max(0, Height - _hScrollBar.Height));

      Rect clientBounds;
      if (Project.Current.Elements.Count > 0) {
        var canvasBounds = Rect.Empty;
        foreach (var element in Project.Current.Elements) canvasBounds = element.UnionBoundsWith(canvasBounds, true);

        var tl = CanvasToClient(canvasBounds.Position);
        var br = CanvasToClient(canvasBounds.GetCorner(CompassPoint.SouthEast));
        clientBounds = new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
      } else {
        // if there's nothing on the canvas, don't include the origin (0,0) as a "thing" to scroll to
        clientBounds = new Rect(topLeft.X, topLeft.Y, displaySize.X, displaySize.Y);
      }

      if (!ApplicationSettingsController.AppSettings.InfiniteScrollBounds && topLeft.Y <= clientBounds.Top && topLeft.Y + displaySize.Y >= clientBounds.Bottom) {
        _vScrollBar.Enabled = false;
      } else {
        _vScrollBar.Enabled = true;
        _vScrollBar.Minimum = (int) Math.Min(topLeft.Y, clientBounds.Top);
        _vScrollBar.Maximum = (int) Math.Max(topLeft.Y + displaySize.Y, clientBounds.Bottom) - 1; // -1 since Maximum is actually maximum value + 1; see MSDN.
        _vScrollBar.Value = (int) Math.Max(_vScrollBar.Minimum, Math.Min(_vScrollBar.Maximum, topLeft.Y));
        _vScrollBar.LargeChange = (int) displaySize.Y;
        _vScrollBar.SmallChange = (int) (displaySize.Y / 10);
      }

      if (!ApplicationSettingsController.AppSettings.InfiniteScrollBounds && topLeft.X <= clientBounds.Left && topLeft.X + displaySize.X >= clientBounds.Right) {
        _hScrollBar.Enabled = false;
      } else {
        _hScrollBar.Enabled = true;
        _hScrollBar.Minimum = (int) Math.Min(topLeft.X, clientBounds.Left);
        _hScrollBar.Maximum = (int) Math.Max(topLeft.X + displaySize.X, clientBounds.Right) - 1; // -1 since Maximum is actually maximum value + 1; see MSDN.
        _hScrollBar.Value = (int) Math.Max(_hScrollBar.Minimum, Math.Min(_hScrollBar.Maximum, topLeft.X));
        _hScrollBar.LargeChange = (int) displaySize.X;
        _hScrollBar.SmallChange = (int) (displaySize.X / 10);
      }

      _updatingScrollBars = false;
    }

    public void ZoomIn() {
      if (ZoomFactor < 100.0f) ZoomFactor *= 1.25f;
    }

    public void ZoomInMicro() {
      if (ZoomFactor < 100.0f) ZoomFactor += 0.01f;
    }

    public void ZoomOut() {
      if (ZoomFactor > 1 / 10.00f) ZoomFactor /= 1.25f;
    }

    public void ZoomOutMicro() {
      if (ZoomFactor > 1 / 10.00f) ZoomFactor -= 0.01f;
    }

    public void ZoomToFit() {
      ResetZoomOrigin();
      var canvasBounds = ComputeCanvasBounds(true);

      if (!Viewport.Contains(canvasBounds)) {
        var xRatio = Width / canvasBounds.Width;
        var yRatio = Height / canvasBounds.Height;

        ZoomFactor = Math.Max(Math.Min(xRatio, yRatio), 1 / 10f);
      }
    }

    protected override void Dispose(bool disposing) {
      if (disposing) {
        StopAutomapping();
        Project.ProjectChanged -= OnProjectChanged;
        Settings.Changed -= OnSettingsChanged;
        Project.Current.Elements.Added -= OnElementAdded;
        Project.Current.Elements.Removed -= OnElementRemoved;
        foreach (var element in Project.Current.Elements) element.Changed -= OnElementChanged;
        _recomputeTimer?.Dispose();
        _trizbortToolTip1?.Dispose();
        _components?.Dispose();
      }

      base.Dispose(disposing);
    }

    protected override void OnKeyDown(KeyEventArgs e) {
      switch (e.KeyCode) {
        case Keys.Enter:
          if (SelectedElement == null && Project.Current.ActiveSelectedElement == null)
            _commandController.SelectRoomClosestToCenterOfViewport();
          else if (HasSingleSelectedElement)
            _commandController.ShowElementProperties(SelectedElement);
          else if (Project.Current.ActiveSelectedElement != null) _commandController.ShowElementProperties(Project.Current.ActiveSelectedElement);
          break;

        case Keys.Escape:
          _commandController.Select(SelectTypes.None);
          break;

        case Keys.D0:
        case Keys.NumPad0:
          _commandController.SelectStartRoom();
          break;

        case Keys.A:
          switch (ModifierKeys) {
            case Keys.Control | Keys.Shift:
              _commandController.SelectRegions();
              break;
            case Keys.Control:
              _commandController.Select(SelectTypes.All);
              break;
            default:
              if (e.Modifiers == Keys.None) _commandController.ToggleConnectionFlow(NewConnectionFlow);
              break;
          }

          break;

        case Keys.Add:
        case Keys.Oemplus:
          ZoomIn();
          break;

        case Keys.Subtract:
        case Keys.OemMinus:
          ZoomOut();
          break;

        case Keys.Home:
          switch (ModifierKeys) {
            case Keys.Control:
              ZoomToFit();
              break;
            case Keys.Shift:
              ShiftArrowHandler(Keys.Home);
              break;
            default:
              ResetZoomOrigin();
              break;
          }

          break;

        case Keys.PageUp:
        case Keys.PageDown:
        case Keys.End:
          if (ModifierKeys == Keys.Shift)
            ShiftArrowHandler(e.KeyCode);
          break;


        case Keys.Right:
        case Keys.Left:
        case Keys.Up:
        case Keys.Down:
          switch (ModifierKeys) {
            case Keys.Alt | Keys.Control:
              ResizeRoom(e.KeyCode);
              break;
            case Keys.Control:
              CtrlArrowHandler(e.KeyCode);
              break;
            case Keys.Shift:
              ShiftArrowHandler(e.KeyCode);
              break;
            default:
              MoveArrowKeyHandler(e.KeyCode, e.Shift);
              break;
          }

          break;

        case Keys.H:
          if (ModifierKeys == Keys.Control) _commandController.SetRoomShape(RoomShape.SquareCorners);
          break;

        case Keys.E:
          if (ModifierKeys == Keys.Control) _commandController.SetRoomShape(RoomShape.SquareCorners);
          break;

        case Keys.L:
          if (ModifierKeys == Keys.None) AddLabel(true);
          break;

        case Keys.R:
          switch (ModifierKeys) {
            case Keys.Control:
              _commandController.SetRoomShape(RoomShape.RoundedCorners);
              break;
            case Keys.None:
              AddRoom(true, true);
              break;
          }

          break;

        case Keys.B:
          if (ModifierKeys == Keys.Control) Project.Current.Backup();
          break;

        case Keys.T:
          if (ModifierKeys == Keys.None) _commandController.ToggleConnectionStyle(NewConnectionStyle);
          break;
        case Keys.P:
          if (ModifierKeys == Keys.None) ApplyNewPlainConnectionSettings();
          break;
        case Keys.U:
          if (ModifierKeys == Keys.None) _commandController.SetConnectionLabel(ConnectionLabel.Up);
          break;
        case Keys.D:
          switch (ModifierKeys) {
            case Keys.None:
              _commandController.SetConnectionLabel(ConnectionLabel.Down);
              break;
            case Keys.Control:
              if (HasSingleSelectedElement && SelectedElement.GetType() == typeof(Room)) {
                var x = (Room) SelectedElement;
                x.DeleteAllRoomConnections();
              }

              break;
          }

          break;
        case Keys.OemSemicolon:
        case Keys.Oem5:
          switch (ModifierKeys) {
            case Keys.None:
              if (HasSingleSelectedElement && SelectedElement.GetType() == typeof(Room)) {
                var room = (Room) SelectedElement;
                room.AdjustAllRoomConnections();
              }

              break;
            case Keys.Control:
              var desc = new string[3];
              desc[0] = "NSEW";
              desc[1] = "diagonals";
              desc[2] = "all ports";
              ApplicationSettingsController.AppSettings.PortAdjustDetail++;
              ApplicationSettingsController.AppSettings.PortAdjustDetail %= 3;
              var x = 4 << ApplicationSettingsController.AppSettings.PortAdjustDetail; // yeah this is cutesy code but it does the job
              if ((ModifierKeys & Keys.Shift) == Keys.Shift) //Shift pops up current port adjust detail
                UserInteraction.ShowMessage($"Available ports for readjustment {(ApplicationSettingsController.AppSettings.PortAdjustDetail == 0 ? "de" : "in")}creased to {x} ({desc[ApplicationSettingsController.AppSettings.PortAdjustDetail]}).", "Port Detail Adjust");
              break;
          }

          break;
        case Keys.I:
          if (ModifierKeys == Keys.None) _commandController.SetConnectionLabel(ConnectionLabel.In);
          break;
        case Keys.O:
          if (ModifierKeys == Keys.None) _commandController.SetConnectionLabel(ConnectionLabel.Out);
          break;

        case Keys.V:
          switch (ModifierKeys) {
            case Keys.Control:
              Paste(true);
              break;
            case Keys.None:
              ReverseLineDirection();
              break;
          }

          break;

        case Keys.OemCloseBrackets:
        case Keys.OemOpenBrackets:
          if (HasSingleSelectedElement && SelectedElement.GetType() == typeof(Connection)) {
            var x = (Connection) SelectedElement; //first we see if there is a control key, then, which bracket
            x.RotateConnector(ModifierKeys != Keys.Control, e.KeyCode == Keys.OemOpenBrackets);
          }

          break;

        case Keys.J:
          if (ModifierKeys == Keys.None) {
            var selectedRooms = SelectedRooms;
            if (selectedRooms.Count == 2 && !Project.Current.AreRoomsConnected(SelectedRooms))
              JoinSelectedRooms(selectedRooms[0], selectedRooms[1]);
          }

          break;

        case Keys.W:
          switch (ModifierKeys) {
            case Keys.Shift:
              SwapRoomFill();
              break;

            case Keys.Alt:
              SwapRoomRegions();
              break;

            case Keys.Control:
              SwapRoomNames();
              break;

            case Keys.None:
              SwapRooms();
              break;
          }

          break;

        case Keys.F:
          switch (ModifierKeys) {
            case Keys.Control:
              var qf = new QuickFind();
              UserInteraction.ShowDialog(qf);
              break;
          }

          break;

        case Keys.K:

          switch (ModifierKeys) {
            case Keys.None:
              _commandController.SetRoomLighting(LightingActionType.Toggle);
              break;
            case Keys.Control | Keys.Shift:
              _commandController.SetRoomLighting(LightingActionType.ForceLight);
              break;
            case Keys.Control:
              _commandController.SetRoomLighting(LightingActionType.ForceDark);
              break;
          }


          break;

        case Keys.F1:
          switch (ModifierKeys) {
            case Keys.Control:
              ApplicationSettingsController.AppSettings.DebugShowFps = !ApplicationSettingsController.AppSettings.DebugShowFps;
              Invalidate();
              break;

            case Keys.Shift:
              ApplicationSettingsController.AppSettings.DebugShowMouseCoordinates = !ApplicationSettingsController.AppSettings.DebugShowMouseCoordinates;
              Invalidate();
              break;
          }

          break;

        case Keys.F2:
          switch (ModifierKeys) {
            case Keys.Control:
              ApplicationSettingsController.AppSettings.DebugDisableElementRendering = !ApplicationSettingsController.AppSettings.DebugDisableElementRendering;
              Invalidate();
              break;
          }

          break;

        case Keys.F3:
          switch (ModifierKeys) {
            case Keys.Control:
              ApplicationSettingsController.AppSettings.DebugDisableLineRendering = !ApplicationSettingsController.AppSettings.DebugDisableLineRendering;
              Invalidate();
              break;

            case Keys.Shift:
              MoveActiveSelected(false);

              break;

            default:
              MoveActiveSelected();

              break;
          }

          break;

        case Keys.F5:
          switch (ModifierKeys) {
            case Keys.Control:
              ApplicationSettingsController.AppSettings.DebugDisableGridPolyline = !ApplicationSettingsController.AppSettings.DebugDisableGridPolyline;
              Invalidate();
              break;

            default:
              if (IsAutomapping)
                _automap.RunToCompletion();

              break;
          }

          break;

        case Keys.F11:
          if (IsAutomapping)
            _automap.Step();

          break;

        case Keys.NumPad8:
        case Keys.NumPad9:
        case Keys.NumPad6:
        case Keys.NumPad3:
        case Keys.NumPad2:
        case Keys.NumPad1:
        case Keys.NumPad4:
        case Keys.NumPad7:
          if (ModifierKeys == Keys.Shift)
            ShiftArrowHandler(e.KeyCode);
          else
            AddOrSelectRooms(IndicatedDirection(e.KeyCode));
          break;
      }

      base.OnKeyDown(e);
    }

    protected override void OnMouseClick(MouseEventArgs e) {
      if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Control && SelectedElement != null && SelectedElement.GetType() == typeof(Room)) {
        var room = (Room) SelectedElement;
        if (room.IsReference) {
          SelectedElement = room.ReferenceRoom;
          _commandController.MakeVisible(room.ReferenceRoom);
        }
      }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e) {
      if (e.Button == MouseButtons.Left) {
        if (CanSelectElements && HasSingleSelectedElement)
          _commandController.ShowElementProperties(SelectedElement);
        else if (ApplicationSettingsController.AppSettings.DoubleClickToAddRoom && CanSelectElements && IsEmptySpace(e.Location))
          AddRoom(true);
      }
      base.OnMouseDoubleClick(e);
    }

    private bool IsEmptySpace(Point clientPos) {
      if (HoverHandle != null || HoverPort != null || HoverWaypoint.HasValue) return false;
      return HitTestElement(ClientToCanvas(new PointF(clientPos.X, clientPos.Y)), false) == null;
    }

    protected override void OnMouseDown(MouseEventArgs e) {
      HideElementToolTip();
      var clientPos = new PointF(e.X, e.Y);
      var canvasPos = ClientToCanvas(clientPos);
      _lastMouseDownPosition = e.Location;

      if (DragMode != DragModes.None)
        return;

      if (IsDragButton(e)) {
        BeginDragPan(clientPos);
      } else if (e.Button == MouseButtons.Left) {
        if (CanSelectElements) BeginDragMove(canvasPos);
        if (DragMode == DragModes.None)
          if (HoverPort != null && CanDrawLine)
            BeginDragDrawLine();
      } else if (e.Button == MouseButtons.Right) {
        if (CanSelectElements)
          BeginDragMove(canvasPos);
      }

      base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e) {
      // ignore spurious mouse move events
      if (_lastKnownMousePosition == e.Location) return;
      _lastKnownMousePosition = e.Location;

      UpdateDragHover(e.Location);
      base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e) {
      EndDrag();

      base.OnMouseUp(e);
    }

    protected override void OnMouseLeave(EventArgs e) {
      HideElementToolTip();
      base.OnMouseLeave(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
      HideElementToolTip();
      if (e.X < 0 || e.X > Width || e.Y < 0 || e.Y > Height)
        return;

      var pos = ClientToCanvas(new PointF(e.X, e.Y));

      if (IsZoomIn(e.Delta))
        if (ModifierKeys == Keys.Control)
          ZoomInMicro();
        else
          ZoomIn();
      else if (IsZoomOut(e.Delta) && ZoomFactor > 1 / 100.0f)
        if (ModifierKeys == Keys.Control)
          ZoomOutMicro();
        else
          ZoomOut();

      var newPos = ClientToCanvas(new PointF(e.X, e.Y));
      Origin = Origin - (newPos - pos);

      Invalidate();
      UpdateDragHover(e.Location);

      base.OnMouseWheel(e);
    }

    protected override void OnPaint(PaintEventArgs e) {
      if (DesignMode) {
        e.Graphics.Clear(Settings.Color[Colors.Canvas]);
        return;
      }

      using (var nativeGraphics = Graphics.FromHdc(e.Graphics.GetHdc())) {
        using (var graphics = XGraphics.FromGraphics(nativeGraphics, new XSize(Width, Height))) {
          Draw(graphics, false, Width, Height);
        }
      }

      e.Graphics.ReleaseHdc();

      // update our scroll bars, unless this paint event was caused by the scroll bars,
      // in which case messing with them may cause the scroll bars to throw exceptions.
      if (!_doNotUpdateScrollBarsNextPaint) UpdateScrollBars();
      _doNotUpdateScrollBarsNextPaint = false;

      // update the minimap
      _minimap.Invalidate();
      _minimap.Update();
    }

    protected override void WndProc(ref Message m) {
      switch (m.Msg) {
        case 0x0007: // WM_SETFOCUS
          // do not pass focus to our child controls
          m.Result = IntPtr.Zero;
          return;
      }

      base.WndProc(ref m);
    }

    /// <summary>
    ///   Add a new connection between the given map nodes.
    /// </summary>
    /// <param name="roomOne">The first room.</param>
    /// <param name="compassPointOne">The direction of the connection in the first room.</param>
    /// <param name="roomTwo">The second room.</param>
    /// <param name="compassPointTwo">The direction of the connection in the second room.</param>
    private Connection AddConnection(Element roomOne, CompassPoint compassPointOne, Element roomTwo, CompassPoint compassPointTwo) {
      var vertexOne = new Vertex(roomOne.PortList.OfType<Room.CompassPort>().First(port => port.CompassPoint == compassPointOne));
      var vertexTwo = new Vertex(roomTwo.PortList.OfType<Room.CompassPort>().First(port => port.CompassPoint == compassPointTwo));
      var connection = new Connection(Project.Current, vertexOne, vertexTwo) {
        Style = NewConnectionStyle,
        Flow = NewConnectionFlow
      };
      connection.SetText(NewConnectionLabel);
      Project.Current.Elements.Add(connection);

      return connection;
    }


    /// <summary>
    ///   Find a room adjacent to the selected room in the given direction;
    ///   if found, connect the rooms. If not, create a new room in that direction.
    /// </summary>
    /// <param name="compassPoint">The direction to consider.</param>
    /// <returns>True if a new connection/room was made; false otherwise.</returns>
    private void AddOrConnectRoomRelativeToSelectedRoom(CompassPoint compassPoint) {
      if (SelectedElement is Room element) {
        var room = element;
        var rect = room.InnerBounds;
        rect.Inflate(Settings.PreferredDistanceBetweenRooms + room.Width / 2, Settings.PreferredDistanceBetweenRooms + room.Height / 2);
        var centerOfNewRoom = rect.GetCorner(compassPoint);

        var existing = HitTestElement(centerOfNewRoom, false);
        if (existing is Room two) {
          // just connect the rooms together
          AddConnection(room, compassPoint, two, CompassPointHelper.GetOpposite(compassPoint));
          SelectedElement = existing;
          _commandController.MakeVisible(SelectedElement);
        } else {
          // new room entirely
          var newRoom = new Room(Project.Current) {
            Position = new Vector(centerOfNewRoom.X - room.Width / 2, centerOfNewRoom.Y - room.Height / 2),
            Region = room.Region,
            Size = room.Size,
            Shape = room.Shape,
            StraightEdges = room.StraightEdges,
            HandDrawnStyle = room.HandDrawnStyle,
            IsDark = room.IsDark,
            Corners = room.Corners
          };

          Project.Current.Elements.Add(newRoom);
          AddConnection(room, compassPoint, newRoom, CompassPointHelper.GetOpposite(compassPoint));
          SelectedElement = newRoom;
          _commandController.MakeVisible(SelectedElement);
          Refresh();
          newRoom.ShowDialog();
        }
      }
    }

    private void AddOrSelectRooms(CompassPoint? compassPoint) {
      if (compassPoint != null && !SelectRoomRelativeToSelectedRoom(compassPoint.Value))
        if (ModifierKeys == Settings.KeypadNavigationCreationModifier) {
          AddOrConnectRoomRelativeToSelectedRoom(compassPoint.Value);
          SelectRoomRelativeToSelectedConnection(compassPoint.Value);
        } else if (ModifierKeys == Settings.KeypadNavigationUnexploredModifier) {
          AddUnexploredConnectionToSelectedRoom(compassPoint.Value);
        }
    }

    private void AddRoomToolStripMenuItemClick(object sender, EventArgs e) {
      AddRoom(true, true);
    }

    /// <summary>
    ///   Add an "unexplored" (loopback) connection from
    /// </summary>
    /// <param name="compassPoint"></param>
    private void AddUnexploredConnectionToSelectedRoom(CompassPoint compassPoint) {
      if (SelectedElement is Room element) {
        var room = element;
        AddConnection(room, compassPoint, room, compassPoint);
      }
    }

    private void ApplicationSettingsToolStripMenuItemClick(object sender, EventArgs e) {
      ApplicationSettingsController.ShowAppDialog();
    }

    private void BeginDragDrawLine() {
      DragMode = DragModes.DrawLine;
      Capture = true;
    }

    private void BeginDragMove(Vector canvasPos) {
      if (HoverWaypoint.HasValue && WaypointConnection != null) {
        _dragWaypoint = HoverWaypoint.Value;
        _dragWaypointOffset = WaypointConnection.GetCurveWaypointHandlePosition(_dragWaypoint) - canvasPos;
        // a "ghost" handle only becomes a real waypoint once it is actually dragged
        _selectedWaypoint = WaypointConnection.GetCurveWaypoint(_dragWaypoint).HasValue ? _dragWaypoint : (CurveWaypoint?) null;
        DragMode = DragModes.MoveWaypoint;
        Capture = true;
      } else if (HoverHandle != null) {
        DragMode = DragModes.MoveResizeHandle;
        _dragResizeHandleLastPosition = canvasPos; // unsnapped
        Capture = true;
      } else if (HoverPort != null) {
        if (HoverPort is MoveablePort) {
          _dragMovePort = (MoveablePort) HoverPort;
          _dragOffsetCanvas = Settings.Snap(canvasPos - HoverPort.Position);
          DragMode = DragModes.MovePort;
          Capture = true;
        }
      } else {
        var hitElement = HitTestElement(canvasPos, false);

        var alreadySelected = _selectedElements.Contains(hitElement);
        if (!alreadySelected && (ModifierKeys & (Keys.Control | Keys.Shift)) == Keys.None)
          _selectedElements.Clear();
        else if (hitElement != null) _selectedElements.Remove(hitElement);
        if ((ModifierKeys & Keys.Shift) == Keys.Shift) {
          if (!alreadySelected && hitElement != null) _selectedElements.Add(hitElement);
        } else if (hitElement != null) {
          // if we're not holding shift, ensure the current element is selected.
          // we're safe to re-add it since it will definitely have been removed already
          // if it was selected, by the above logic.
          _selectedElements.Add(hitElement);
        }

        // now we've finished messing with the set of selected elements,
        // update handles, ports, and take defaults for new elements from the most recently selected element.
        UpdateSelection();

        if (hitElement != null && _selectedElements.Contains(hitElement)) {
          // if we ended up with the hit element being selected, initiate a drag move.
          DragMode = DragModes.MoveElement;
          canvasPos = Settings.Snap(canvasPos);
          _dragOffsetCanvas = canvasPos;
          Capture = true;
        } else if (hitElement == null) {
          // if we didn't hit anything at all, begin a new marquee selection.
          DragMode = DragModes.Marquee;
          _dragOffsetCanvas = canvasPos;
          _dragMarqueeLastPosition = canvasPos;
          Capture = true;
        }
      }

      Invalidate();
    }


    private void BeginDragPan(PointF clientPos) {
      DragMode = DragModes.Pan;
      _panPosition = clientPos;
      Cursor = Cursors.NoMove2D;
      Capture = true;
    }

    private void BeginDrawConnection(Vector canvasPos) {
      Connection connection;
      HoverPort = HitTestPort(canvasPos);
      if (HoverPort != null && !(HoverPort is MoveablePort)) {
        // Only from non-moveable ports, until we fix docking.
        // See also DoDragMovePort().
        // Updated to ignore ID gaps. ID gaps are resolved on load
        connection = new Connection(Project.Current, new Vertex(HoverPort), new Vertex(HoverPort));
      } else {
        var pos = Settings.Snap(canvasPos);
        connection = new Connection(Project.Current, new Vertex(pos), new Vertex(pos));
      }

      connection.Style = NewConnectionStyle;
      connection.Flow = NewConnectionFlow;
      connection.SetText(NewConnectionLabel);
      Project.Current.Elements.Add(connection);
      SelectedElement = connection;
      _dragMovePort = (MoveablePort) connection.PortList[1];
      _dragOffsetCanvas = Settings.Snap(canvasPos - connection.VertexList[0].Position);
      HoverPort = null;
      DragMode = DragModes.MovePort;
      Capture = true;
    }

    private void BringToFrontToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.BringToFront();
    }

    private void CtrlArrowHandler(Keys keyCode) {
      var direction = IndicatedDirection(keyCode);

      if (!SelectRoomRelativeToSelectedRoom(direction) && !SelectRoomRelativeToSelectedConnection(direction))
        AddOrConnectRoomRelativeToSelectedRoom(direction);
    }

    private void CtxCanvasMenuOpening(object sender, CancelEventArgs e) {
      if ((_minimap.Visible && _minimap.IsMouseOverMe()) || DragMode == DragModes.Pan)
        e.Cancel = true;

      var clientPos = new PointF(_lastMouseDownPosition.X, _lastMouseDownPosition.Y);
      var canvasPos = ClientToCanvas(clientPos);
      var hitElement = HitTestElement(canvasPos, false);
      var regionMenu = _regionToolStripMenuItem;

      if (hitElement != null) {
        if (hitElement is Room) {
          _lastSelectedRoom = (Room) hitElement;

          regionMenu.DropDownItems.Clear();

          foreach (var region in Settings.Regions.OrderBy(p => p.RegionName != Domain.Misc.Region.DefaultRegion).ThenBy(p => p.RegionName)) {
            var item = regionMenu.DropDownItems.Add(region.RegionName, null, RegionContextClick);
            item.Image = GenerateRegionImage(region);
            if (region.RegionName == _lastSelectedRoom.Region)
              ((ToolStripMenuItem) item).Checked = true;
          }

          _addRoomToolStripMenuItem.Visible = true;
          _renameToolStripMenuItem.Visible = true;
          _darkToolStripMenuItem.Visible = true;
          _regionToolStripMenuItem.Visible = true;
          _roomShapeToolStripMenuItem.Visible = true;
          _joinRoomsToolStripMenuItem.Visible = true;
          _swapObjectsToolStripMenuItem.Visible = true;
          _roomPropertiesToolStripMenuItem.Visible = true;

          _toolStripSeparator6.Visible = true;
          _startRoomToolStripMenuItem.Visible = true;
          _endRoomToolStripMenuItem.Visible = true;

          _startRoomToolStripMenuItem.Enabled = SelectedRooms.Count == 1;
          _endRoomToolStripMenuItem.Enabled = HasSelectedRooms;

          _startRoomToolStripMenuItem.Checked = _lastSelectedRoom.IsStartRoom && HasSingleSelectedElement;
          _endRoomToolStripMenuItem.Checked = HasSelectedRooms && _lastSelectedRoom.IsEndRoom;

          _sendToBackToolStripMenuItem.Visible = true;
          _bringToFrontToolStripMenuItem.Visible = true;
          _toolStripSeparator7.Visible = true;

          _toolStripMenuItem1.Visible = true;
          _toolStripMenuItem2.Visible = true;
          _toolStripSeparator1.Visible = true;
          _toolStripSeparator2.Visible = true;

          _lineStylesMenuItem.Visible = false;
          _reverseLineMenuItem.Visible = false;

          _swapObjectsToolStripMenuItem.Enabled = SelectedRooms.Count == 2;
          _joinRoomsToolStripMenuItem.Enabled = SelectedRooms.Count == 2 && !Project.Current.AreRoomsConnected(SelectedRooms);


          _darkToolStripMenuItem.Checked = _lastSelectedRoom.IsDark;
        }

        if (hitElement is Connection || hitElement is MapLabel) {
          _addRoomToolStripMenuItem.Visible = true;

          _renameToolStripMenuItem.Visible = false;
          _darkToolStripMenuItem.Visible = false;
          _regionToolStripMenuItem.Visible = false;
          _roomShapeToolStripMenuItem.Visible = false;
          _joinRoomsToolStripMenuItem.Visible = false;
          _swapObjectsToolStripMenuItem.Visible = false;
          _roomPropertiesToolStripMenuItem.Visible = true;

          _sendToBackToolStripMenuItem.Visible = false;
          _bringToFrontToolStripMenuItem.Visible = false;
          _toolStripSeparator7.Visible = false;

          _startRoomToolStripMenuItem.Visible = false;
          _endRoomToolStripMenuItem.Visible = false;
          _toolStripSeparator6.Visible = false;

          _lineStylesMenuItem.Visible = true;
          _reverseLineMenuItem.Visible = true;

          _toolStripMenuItem1.Visible = false;
          _toolStripMenuItem2.Visible = false;
          _toolStripSeparator1.Visible = true;
          _toolStripSeparator2.Visible = true;

          _roomPropertiesToolStripMenuItem.Enabled = true;
          _sendToBackToolStripMenuItem.Visible = hitElement is MapLabel;
          _bringToFrontToolStripMenuItem.Visible = hitElement is MapLabel;
          _toolStripSeparator7.Visible = hitElement is MapLabel;
          _lineStylesMenuItem.Visible = hitElement is Connection;
          _reverseLineMenuItem.Visible = hitElement is Connection;
        }
      } else {
        _renameToolStripMenuItem.Visible = false;
        _darkToolStripMenuItem.Visible = false;
        _regionToolStripMenuItem.Visible = false;
        _roomShapeToolStripMenuItem.Visible = false;
        _joinRoomsToolStripMenuItem.Visible = false;
        _swapObjectsToolStripMenuItem.Visible = false;
        _roomPropertiesToolStripMenuItem.Visible = false;

        _startRoomToolStripMenuItem.Visible = false;
        _endRoomToolStripMenuItem.Visible = false;
        _toolStripSeparator6.Visible = false;

        _sendToBackToolStripMenuItem.Visible = false;
        _bringToFrontToolStripMenuItem.Visible = false;
        _toolStripSeparator7.Visible = false;

        _lineStylesMenuItem.Visible = false;
        _reverseLineMenuItem.Visible = false;

        _addRoomToolStripMenuItem.Visible = true;

        _toolStripMenuItem1.Visible = false;
        _toolStripMenuItem2.Visible = false;
        _toolStripSeparator1.Visible = false;
        _toolStripSeparator2.Visible = false;
      }
    }

    private void DarkToolStripMenuItemClick1(object sender, EventArgs e) {
      foreach (var room in SelectedRooms) room.IsDark = !room.IsDark;
    }

    private List<Element> DepthSortElements() {
      var elements = new List<Element>();
      elements.AddRange(Project.Current.Elements);
      elements.Sort();
      return elements;
    }

    private void DoDragMoveElement(Vector canvasPos) {
      canvasPos = Settings.Snap(canvasPos);
      var delta = canvasPos - _dragOffsetCanvas;
      MoveSelectedElements(delta);
      _dragOffsetCanvas = canvasPos;
    }

    private void MoveSelectedElements(Vector delta) {
      MapEditing.Move(Project.Current.Elements, _selectedElements, delta);
      HideElementToolTip();
    }

    private void HideElementToolTip() {
      _trizbortToolTip1.SetToolTip(this, null);
      _trizbortToolTip1.Hide(this);
    }

    private void DoDragMoveWaypoint(Point mousePosition, Vector canvasPos) {
      var connection = WaypointConnection;
      if (connection == null) return;

      if (!connection.GetCurveWaypoint(_dragWaypoint).HasValue) {
        if (new Vector(_lastMouseDownPosition).Distance(new Vector(mousePosition)) <= Settings.DragDistanceToInitiateNewConnection) return;
        if (!connection.CanAddCurveWaypoint(_dragWaypoint)) return;
      }

      connection.SetCurveWaypoint(_dragWaypoint, Settings.Snap(canvasPos + _dragWaypointOffset));
      _selectedWaypoint = _dragWaypoint;
    }

    private void DoDragMovePort(Vector canvasPos) {
      if (HoverPort != null && HoverPort != _dragMovePort) {
        if (_dragMovePort.DockedAt != HoverPort && (!(HoverPort is MoveablePort) || ((MoveablePort) HoverPort).DockedAt != _dragMovePort))
          if (!(HoverPort is MoveablePort)) {
            _dragMovePort.DockAt(HoverPort);
          } else {
            canvasPos = Settings.Snap(canvasPos);
            _dragMovePort.SetPosition(canvasPos - _dragOffsetCanvas);
          }
      } else {
        canvasPos = Settings.Snap(canvasPos);
        _dragMovePort.SetPosition(canvasPos - _dragOffsetCanvas);
      }
    }

    private void DoDragMoveResizeHandle(Vector canvasPos) {
      if (HoverHandle != null)
        _dragResizeHandleLastPosition = MapEditing.Resize(HoverHandle, _dragResizeHandleLastPosition, canvasPos);
    }

    private void DoDragPan(PointF clientPos) {
      var delta = Drawing.Subtract(_panPosition, clientPos);
      delta = Drawing.Divide(delta, ZoomFactor);
      Origin = new Vector(Origin.X + delta.X, Origin.Y + delta.Y);
      _panPosition = clientPos;
      HideElementToolTip();
    }

    private void DrawElements(XGraphics graphics, Palette palette, bool finalRender) {
      if (ApplicationSettingsController.AppSettings.DebugDisableElementRendering)
        return;

      var context = new DrawingContext(ZoomFactor) {UseSmartLineSegments = _smartLineSegmentsUpToDate};
      var elements = DepthSortElements();

      if (!context.UseSmartLineSegments)
        foreach (var element in elements) {
          element.PreDraw(context);
          element.Flagged = false;
        }
      else
        foreach (var element in elements)
          element.Flagged = false;

      foreach (var element in _selectedElements) element.Flagged = true;

      var clipToScreen = new RectangleF(Origin.X - Width / 2 / ZoomFactor, Origin.Y - Height / 2 / ZoomFactor, Width / ZoomFactor, Height / ZoomFactor);

      foreach (var element in elements) {
        context.Selected = element.Flagged && !finalRender;
        context.Hover = !context.Selected && element == HoverElement && !finalRender;
        if (context.Hover && DragMode == DragModes.MovePort) context.Hover = false;

        try {
          var elementBounds = element.UnionBoundsWith(Rect.Empty, true).ToRectangleF();
          if (finalRender || clipToScreen.IntersectsWith(elementBounds)) element.Draw(graphics, palette, context);
        }
        catch (Exception) {
          // avoid GDI+ exceptions (vast shapes, etc.) taking down the canvas
        }
      }
    }

    private void DrawGrid(XGraphics graphics, Palette palette) {
      if (Settings.IsGridVisible && Settings.GridSize * ZoomFactor > 10) {
        var topLeft = Settings.Snap(ClientToCanvas(new PointF(-Settings.GridSize * ZoomFactor, -Settings.GridSize * ZoomFactor)));
        var bottomRight = Settings.Snap(ClientToCanvas(new PointF(Width + Settings.GridSize * ZoomFactor, Height + Settings.GridSize * ZoomFactor)));
        var points = new List<PointF>();
        var even = true;
        for (var x = topLeft.X; x <= bottomRight.X; x += Settings.GridSize) {
          var start = CanvasToClient(new Vector(x, topLeft.Y));
          var end = CanvasToClient(new Vector(x, bottomRight.Y));
          if (even) {
            points.Add(start);
            points.Add(end);
          } else {
            points.Add(end);
            points.Add(start);
          }

          even = !even;
          if (ApplicationSettingsController.AppSettings.DebugDisableGridPolyline) graphics.DrawLine(palette.GridPen, start, end);
        }

        if (!ApplicationSettingsController.AppSettings.DebugDisableGridPolyline) graphics.DrawLines(palette.GridPen, points.ToArray());
        points = new List<PointF>();
        for (var y = topLeft.Y; y <= bottomRight.Y; y += Settings.GridSize) {
          var start = CanvasToClient(new Vector(topLeft.X, y));
          var end = CanvasToClient(new Vector(bottomRight.X, y));
          if (even) {
            points.Add(start);
            points.Add(end);
          } else {
            points.Add(end);
            points.Add(start);
          }

          even = !even;
          if (ApplicationSettingsController.AppSettings.DebugDisableGridPolyline) graphics.DrawLine(palette.GridPen, start, end);
        }

        if (!ApplicationSettingsController.AppSettings.DebugDisableGridPolyline) graphics.DrawLines(palette.GridPen, points.ToArray());
      }
    }

    private void DrawHandles(XGraphics graphics, Palette palette) {
      DrawWaypointHandles(graphics, palette);
      if (_handles.Count == 0) return;

      var context = new DrawingContext(ZoomFactor);

      if (_handles.Count > 1) {
        var bounds = _handles.Aggregate(Rect.Empty, (current, handle) => current == Rect.Empty ? new Rect(handle.Position, Vector.Zero) : current.Union(handle.Position));

        bounds.X += Settings.HandleSize / 2f;
        bounds.Y += Settings.HandleSize / 2f;
      }


      foreach (var handle in _handles) {
        context.Selected = handle == HoverHandle;
        handle.Draw(this, graphics, palette, context);
      }
    }

    private IEnumerable<CurveWaypoint> VisibleWaypoints(Connection connection) {
      if (connection == null) yield break;
      // the middle handle is listed last so it is drawn on top and wins hit tests
      foreach (var waypoint in new[] {CurveWaypoint.Quarter, CurveWaypoint.ThreeQuarter, CurveWaypoint.Middle})
        if (connection.GetCurveWaypoint(waypoint).HasValue || connection.CanAddCurveWaypoint(waypoint))
          yield return waypoint;
    }

    // Handle size in canvas units; never shrinks below Settings.HandleSize on screen when zoomed out.
    private float WaypointHandleScale => Settings.HandleSize * Math.Max(1f, 1f / ZoomFactor);

    private Rect WaypointHandleBounds(Connection connection, CurveWaypoint waypoint) {
      var size = WaypointHandleScale * (connection.GetCurveWaypoint(waypoint).HasValue ? 2f : 1.5f);
      var position = connection.GetCurveWaypointHandlePosition(waypoint);
      return new Rect(position.X - size / 2, position.Y - size / 2, size, size);
    }

    private void DrawWaypointHandles(XGraphics graphics, Palette palette) {
      var connection = WaypointConnection;
      if (connection == null) return;

      var context = new DrawingContext(ZoomFactor);
      foreach (var waypoint in VisibleWaypoints(connection)) {
        var isSet = connection.GetCurveWaypoint(waypoint).HasValue;
        context.Selected = waypoint == HoverWaypoint || waypoint == _selectedWaypoint && isSet;
        Drawing.DrawHandle(this, graphics, palette, WaypointHandleBounds(connection, waypoint), context, !isSet, true);
      }
    }

    private CurveWaypoint? HitTestWaypoint(Vector canvasPos) {
      var connection = WaypointConnection;
      if (connection == null) return null;

      CurveWaypoint? hit = null;
      foreach (var waypoint in VisibleWaypoints(connection)) {
        var bounds = WaypointHandleBounds(connection, waypoint);
        // be generous so the handles are easy to grab
        bounds.Inflate(WaypointHandleScale / 2);
        if (bounds.Contains(canvasPos)) hit = waypoint;
      }

      return hit;
    }

    private void DrawMarquee(XGraphics graphics, Palette palette) {
      var marqueeRect = GetMarqueeCanvasBounds();
      if (!(marqueeRect.Width > 0) || !(marqueeRect.Height > 0)) return;

      graphics.DrawRectangle(palette.MarqueeFillBrush, marqueeRect.ToRectangleF());
      var topLeft = new PointF(marqueeRect.Left, marqueeRect.Top);
      var topRight = new PointF(marqueeRect.Right, marqueeRect.Top);
      var bottomLeft = new PointF(marqueeRect.Left, marqueeRect.Bottom);
      var bottomRight = new PointF(marqueeRect.Right, marqueeRect.Bottom);
      graphics.DrawLine(palette.MarqueeBorderPen, topLeft, topRight);
      graphics.DrawLine(palette.MarqueeBorderPen, topRight, bottomRight);
      graphics.DrawLine(palette.MarqueeBorderPen, bottomLeft, bottomRight);
      graphics.DrawLine(palette.MarqueeBorderPen, topLeft, bottomLeft);
    }

    private void DrawPorts(XGraphics graphics, Palette palette) {
      var context = new DrawingContext(ZoomFactor);

      // draw all non-selected ports
      foreach (var port in _ports.Where(port => HoverPort != port)) {
        context.Selected = false;
        port.Draw(this, graphics, palette, context);
      }

      if (HoverPort == null) return;

      // lastly, always the port under the mouse, if any
      context.Selected = true;
      HoverPort.Draw(this, graphics, palette, context);
    }

    private void EllipseToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.Ellipse);
    }

    private void EndDrag() {
      if (DragMode == DragModes.MovePort) {
        // clear the selection now the line is drawn
        SelectedElement = null;

        if (_dragMovePort.Owner is Connection) {
          // remove dead connections
          var connection = (Connection) _dragMovePort.Owner;
          var same = true;
          if (connection.VertexList.Count > 0) {
            var pos = connection.VertexList[0].Position;
            foreach (var v in connection.VertexList) {
              if (v.Port?.Owner is Room) same = false;

              var distance = v.Position.Distance(pos);
              if (distance > Numeric.Small) same = false;
            }
          }

          if (same) Project.Current.Elements.Remove(connection);
          SelectedElement = connection;
        }
      } else if (DragMode == DragModes.Marquee) {
        var marqueeRect = GetMarqueeCanvasBounds();
        if ((ModifierKeys & (Keys.Shift | Keys.Control)) == Keys.None) _selectedElements.Clear();
        foreach (var element in HitTest(marqueeRect, false))
          if (!_selectedElements.Contains(element))
            _selectedElements.Add(element);
          else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            if (_selectedElements.Contains(element))
              _selectedElements.Remove(element);
        UpdateSelection();
      }

      DragMode = DragModes.None;
      HoverHandle = null;
      Capture = false;
      Cursor = null;
      Invalidate();
    }

    private void EndRoomToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetEndRoom();
    }

    private void FormatsFillsToolStripMenuItemClick(object sender, EventArgs e) {
      SwapRoomFill();
    }

    private Image GenerateRegionImage(Region region) {
      var image = new Bitmap(24, 20);
      var g = Graphics.FromImage(image);
      using var palette = new Palette();
      g.FillRectangle(palette.Brush(region.RColor), 0, 0, 24, 20);

      return image;
    }

    private Rect GetMarqueeCanvasBounds() {
      if (DragMode != DragModes.Marquee) return Rect.Empty;
      var topLeft = _dragOffsetCanvas;
      var bottomRight = ClientToCanvas(PointToClient(MousePosition));
      if (bottomRight.X < topLeft.X) Numeric.Swap(ref bottomRight.X, ref topLeft.X);
      if (bottomRight.Y < topLeft.Y) Numeric.Swap(ref bottomRight.Y, ref topLeft.Y);
      return new Rect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }

    /// <summary>
    ///   Get a room which can be found in the given direction from the given room.
    /// </summary>
    /// <param name="room">The initial room.</param>
    /// <param name="compassPoint">The direction to consider.</param>
    /// <returns>The room which can be found in that direction, or null if none.</returns>
    /// <remarks>
    ///   If no room can be found exactly in that direction, then consider the directions
    ///   either side. For example, after checking east and finding nothing, check
    ///   east-north-east and east-south-east.
    /// </remarks>
    private Room GetRoomInApproximateDirectionFromRoom(Room room, CompassPoint compassPoint) {
      var nextRoom = GetRoomInExactDirectionFromRoom(room, compassPoint) ?? GetRoomInExactDirectionFromRoom(room, CompassPointHelper.RotateAntiClockwise(compassPoint));

      return nextRoom ?? GetRoomInExactDirectionFromRoom(room, CompassPointHelper.RotateClockwise(compassPoint));
    }

    /// <summary>
    ///   Get a room which can be found in the given direction from the given room.
    /// </summary>
    /// <param name="room">The initial room.</param>
    /// <param name="compassPoint">The direction to consider.</param>
    /// <returns>The room which can be found in that direction, or null if none.</returns>
    private Room GetRoomInExactDirectionFromRoom(Room room, CompassPoint compassPoint) {
      var connections = room.GetConnections(compassPoint);
      foreach (var connection in connections)
      foreach (var vertex in connection.VertexList) {
        var port = vertex.Port;
        if (port != null && port.Owner != room && port.Owner is Room) return (Room) port.Owner;
      }

      return null;
    }

    private void HandDrawnToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.SquareCorners);
    }

    private List<Element> HitTest(Rect rect, bool roomsOnly) {
      return Project.Current.Elements.Where(element => (!roomsOnly || element is Room) && element.Intersects(rect)).ToList();
    }

    private Element HitTestElement(Vector canvasPos, bool includeMargins) {
      var closest = new List<Element>();
      var closestDistance = float.MaxValue;
      foreach (var element in DepthSortElements()) // sort into drawing order
      {
        if (DragMode == DragModes.MovePort && _dragMovePort.Owner == element) continue;

        var distance = element.Distance(canvasPos, includeMargins);
        if (distance <= SnapToElementSizeAtCurrentZoomFactor)
          if (Numeric.ApproxEqual(distance, closestDistance)) {
            closest.Add(element);
          } else if (distance < closestDistance) {
            closest.Clear();
            closest.Add(element);
            closestDistance = distance;
          }
      }

      if (closest.Count == 0) return null;
      return closest[closest.Count - 1]; // choose the topmost element
    }

    private ResizeHandle HitTestHandle(Vector canvasPos) {
      // examine handles, topmost (drawn) to lowermost
      for (var index = _handles.Count - 1; index >= 0; --index) {
        var handle = _handles[index];
        if (handle.HitTest(canvasPos)) return handle;
      }

      return null;
    }

    private Port HitTestPort(Vector canvasPos) {
      Port closest = null;
      var closestDistance = float.MaxValue;

      foreach (var port in _ports) {
        if (DragMode == DragModes.MovePort && port == _dragMovePort) continue;

        var distance = port.Distance(canvasPos);

        var snapDistance = SnapToElementSizeAtCurrentZoomFactor;

        var bounds = port.Owner.UnionBoundsWith(Rect.Empty, true);
        if (bounds.Contains(canvasPos)) snapDistance = DragMode == DragModes.MovePort ? float.MaxValue : 0;

        if (distance <= snapDistance && distance < closestDistance) {
          closest = port;
          closestDistance = distance;
        }
      }

      return closest;
    }

    private CompassPoint IndicatedDirection(Keys keyCode) {
      var returnDir = CompassPoint.North;

      switch (keyCode) {
        case Keys.Left:
        case Keys.NumPad4:
          returnDir = CompassPoint.West;
          break;

        case Keys.Right:
        case Keys.NumPad6:
          returnDir = CompassPoint.East;
          break;

        case Keys.Up:
        case Keys.NumPad8:
          returnDir = CompassPoint.North;
          break;

        case Keys.Down:
        case Keys.NumPad2:
          returnDir = CompassPoint.South;
          break;

        case Keys.NumPad1:
        case Keys.End:
          returnDir = CompassPoint.SouthWest;
          break;

        case Keys.NumPad3:
        case Keys.PageDown:
          returnDir = CompassPoint.SouthEast;
          break;

        case Keys.NumPad7:
        case Keys.Home:
          returnDir = CompassPoint.NorthWest;
          break;

        case Keys.NumPad9:
        case Keys.PageUp:
          returnDir = CompassPoint.NorthEast;
          break;
      }

      return returnDir;
    }

    private static bool IsDragButton(MouseEventArgs e) {
      return e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right && ModifierKeys == Keys.Shift;
    }

    private static bool IsZoomIn(int delta) {
      return !ApplicationSettingsController.AppSettings.InvertMouseWheel && delta < 0 || ApplicationSettingsController.AppSettings.InvertMouseWheel && delta > 0;
    }

    private static bool IsZoomOut(int delta) {
      return !ApplicationSettingsController.AppSettings.InvertMouseWheel && delta > 0 || ApplicationSettingsController.AppSettings.InvertMouseWheel && delta < 0;
    }

    private void JoinRoomsToolStripMenuItemClick(object sender, EventArgs e) {
      JoinSelectedRooms(SelectedRooms.First(), SelectedRooms.Last());
    }

    private void DownLinesMenuItemClick(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Down);
    }

    private void InLinesMenuItemClick(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.In);
    }

    private void OutLinesMenuItemClick(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Out);
    }

    private void PlainLinesMenuItemClick(object sender, EventArgs e) {
      ApplyNewPlainConnectionSettings();
    }

    private void ReverseLineMenuItemClick(object sender, EventArgs e) {
      ReverseLineDirection();
    }

    private void ToggleDirectionalLinesMenuItemClick(object sender, EventArgs e) {
      _commandController.ToggleConnectionFlow(NewConnectionFlow);
    }

    private void ToggleDottedLinesMenuItemClick(object sender, EventArgs e) {
      _commandController.ToggleConnectionStyle(NewConnectionStyle);
    }

    private void UpLinesMenuItemClick(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Up);
    }

    private void MapSettingsToolStripMenuItemClick(object sender, EventArgs e) {
      Settings.ShowMapDialog();
      Refresh();
    }

    private static void MoveActiveSelected(bool moveForward = true) {
      var list = Project.Current.GetSelectedElements();
      var element = list.Find(p => p.Id == Project.Current.ActiveSelectedElement?.Id);
      if (element == null) return;

      var index = list.IndexOf(element);
      if (index == -1) return;

      Element newElement;


      //        newElement = index + 1 > list.Count ? list[0] : list[index+1];
      if (moveForward) {
        index++;
        newElement = list[index == list.Count ? 0 : index % list.Count];
      } else {
        index--;
        newElement = list[index == -1 ? list.Count - 1 : index % list.Count];
      }

      var controller = new CanvasController();
      controller.EnsureVisible(newElement);
      Project.Current.ActiveSelectedElement = newElement;
    }

    private void MoveArrowKeyHandler(Keys keyCode, bool shift) {
      var bHorizontal = keyCode == Keys.Left || keyCode == Keys.Right;
      var bNegative = keyCode == Keys.Right || keyCode == Keys.Down;

      if (SelectedElementCount == 0) {
        if (bHorizontal)
          Origin += new Vector((bNegative ? -1 : 1) * Viewport.Width / (shift ? 5 : 10), 0);
        else
          Origin += new Vector(0, (bNegative ? -1 : 1) * Viewport.Width / (shift ? 5 : 10));
        HideElementToolTip();
      } else {
        var delta = Settings.SnapToGrid ? Settings.GridSize : 2.0f;
        var offset = bHorizontal ? new Vector(bNegative ? delta : -delta, 0) : new Vector(0, bNegative ? delta : -delta);
        MoveSelectedElements(offset);
      }
    }

    private void NamesToolStripMenuItemClick(object sender, EventArgs e) {
      SwapRoomNames();
    }

    private void ObjectsToolStripMenuItemClick(object sender, EventArgs e) {
      SwapRooms();
    }

    private void OctagonalEdgesToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.Octagonal);
    }

    private void OnElementAdded(object sender, ItemEventArgs<Element> e) {
      if (e.Item is Room item) {
        var room = item;
        room.Size = _newRoomSize;
        room.IsDark = _newRoomIsDark;
        room.ObjectsPosition = _newRoomObjectsPosition;
      }

      e.Item.Changed += OnElementChanged;
      Project.Current.IsDirty = true;
      RequestRecomputeSmartSegments();
      Invalidate();
    }

    private void OnElementChanged(object sender, EventArgs e) {
      if (sender is Room room) SetRoomDefaultsFrom(room);

      if (sender is Connection connection) SetConnectionDefaultsFrom(connection);
      Invalidate();
      Project.Current.IsDirty = true;
      RequestRecomputeSmartSegments();
    }

    private void OnElementRemoved(object sender, ItemEventArgs<Element> e) {
      _selectedElements.Remove(e.Item);
      UpdateSelection();
      EndDrag();
      UpdateDragHover(PointToClient(MousePosition));

      Project.Current.IsDirty = true;
      e.Item.Changed -= OnElementChanged;
      RequestRecomputeSmartSegments();
      Invalidate();
    }

    private void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
      if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) e.IsInputKey = true;
    }

    private void OnProjectChanged(object sender, ProjectChangedEventArgs e) {
      if (e.OldProject != null) {
        e.OldProject.Elements.Added -= OnElementAdded;
        e.OldProject.Elements.Removed -= OnElementRemoved;

        foreach (var element in e.OldProject.Elements) element.Changed -= OnElementChanged;
        e.OldProject.Dispose();
      }

      if (e.NewProject != null) {
        e.NewProject.Elements.Added += OnElementAdded;
        e.NewProject.Elements.Removed += OnElementRemoved;

        foreach (var element in e.NewProject.Elements) element.Changed += OnElementChanged;
      }

      Reset();
      ZoomToFit();
    }

    private void OnRecomputeTimerTick(object state) {
      _recomputeTimer.Change(Timeout.Infinite, Timeout.Infinite);

      var context = new DrawingContext(ZoomFactor);
      var elements = DepthSortElements();

      foreach (var element in elements) element.RecomputeSmartLineSegments(context);

      _smartLineSegmentsUpToDate = true;
      Invalidate();
    }

    private void OnSettingsChanged(object sender, EventArgs e) {
      RequestRecomputeSmartSegments();
      if (Settings.WrappingChanged) {
        RedrawAllRoomsWithDashes();
        Settings.WrappingChanged = false; // might as well go at the end of the RedrawAllRoomsWithDashes method
      }
      BackColor = Settings.Color[Colors.Canvas];
      Invalidate();
    }

    private void PasteColors(CopyController.CopyColorsObj xx) {
      foreach (var element in SelectedElements.OfType<Room>()) {
        foreach (var obj in xx.Colors) {
          var propertyInfo = element.GetType().GetProperty(obj.Name);
          if (propertyInfo != null) propertyInfo.SetValue(element, obj.Color);
        }

        element.SecondFillLocation = xx.SecondFillLocation;
      }
    }

    internal void PasteRooms(bool atCursor, CopyController.CopyObject xx, CopyController controller) {
      var newRooms = new List<Room>();
      var newLabels = new List<MapLabel>();
      var copiedNodes = new Dictionary<int, Element>();
      var newConnections = new List<Connection>();

      if (xx != null) {
        var firstElement = true;

        float offsetX = 0;
        float offsetY = 0;

        foreach (var room in xx.Rooms) {
          var newRoom = AddRoom(atCursor, false, false);
          newRooms.Add(newRoom);

          // set room position
          if (firstElement) {
            var firstX = room.Position.X;
            var firstY = room.Position.Y;
            var newFirstX = newRoom.X;
            var newFirstY = newRoom.Y;
            firstElement = false;
            offsetX = firstX - newFirstX;
            offsetY = firstY - newFirstY;
          } else {
            newRoom.Position = new Vector(room.Position.X - offsetX, room.Position.Y - offsetY);
          }

          // set room properties
          controller.SetRoom(newRoom, room);
          copiedNodes[room.OldId] = newRoom;
        }

        foreach (var label in xx.Labels) {
          var newLabel = AddLabel(atCursor, false);
          if (firstElement) {
            offsetX = label.Position.X - newLabel.X;
            offsetY = label.Position.Y - newLabel.Y;
            firstElement = false;
          }
          controller.SetLabel(newLabel, label);
          newLabel.Position = label.Position - new Vector(offsetX, offsetY);
          copiedNodes[label.OldId] = newLabel;
          newLabels.Add(newLabel);
        }

        Refresh();

        foreach (var room in newRooms)
          room.ReferenceRoomId = copiedNodes.TryGetValue(room.ReferenceRoomId, out var reference) && reference is Room
            ? reference.Id : -1;
        newConnections.AddRange(controller.PasteConnections(Project.Current, xx.Connections, copiedNodes, new Vector(offsetX, offsetY)));

        _selectedElements.Clear();
        _selectedElements.AddRange(newRooms);
        _selectedElements.AddRange(newLabels);
        _selectedElements.AddRange(newConnections);
        UpdateSelection();
      }
    }

    private void RaiseNewConnectionFlowChanged() {
      var changed = NewConnectionFlowChanged;
      changed?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseNewConnectionLabelChanged() {
      var changed = NewConnectionLabelChanged;
      changed?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseNewConnectionStyleChanged() {
      var changed = NewConnectionStyleChanged;
      changed?.Invoke(this, EventArgs.Empty);
    }

    private void RecreateHandles() {
      HoverHandle = null;
      _handles.Clear();
      var element = SelectedElement;
      if (CanSelectElements && element is ISizeable && HasSingleSelectedElement) {
        var sizeable = (ISizeable) element;
        _handles.Add(new ResizeHandle(CompassPoint.North, sizeable));
        _handles.Add(new ResizeHandle(CompassPoint.South, sizeable));
        _handles.Add(new ResizeHandle(CompassPoint.East, sizeable));
        _handles.Add(new ResizeHandle(CompassPoint.West, sizeable));
        _handles.Add(new ResizeHandle(CompassPoint.NorthWest, sizeable));
        _handles.Add(new ResizeHandle(CompassPoint.NorthEast, sizeable));
        _handles.Add(new ResizeHandle(CompassPoint.SouthWest, sizeable));
        _handles.Add(new ResizeHandle(CompassPoint.SouthEast, sizeable));
      }

      Invalidate();
    }

    private void RecreatePorts() {
      HoverPort = null;
      _ports.Clear();

      // decide if we want ports on the element under the mouse cursor; if so, add them
      if ((HoverElement is Room || HoverElement is MapLabel) && !_selectedElements.Contains(HoverElement))
        if (DragMode == DragModes.MovePort || CanDrawLine && SelectedElement == null)
          _ports.AddRange(HoverElement.PortList);

      // decide if we want movable ports on the selected element; if so, add them
      // (currently movable ports only apply to connections, and if we want to be able
      // to move a connection we must show them.)
      var needMovablePortsOnSelectedElement = CanSelectElements;
      if (needMovablePortsOnSelectedElement && HasSingleSelectedElement)
        if (SelectedElement != null)
          foreach (var port in SelectedElement.PortList.OfType<MoveablePort>())
            _ports.Add(port);

      Invalidate();
    }

    // context menu event to change region of room(s)
    private void RegionContextClick(object sender, EventArgs e) {
      var selectedRooms = _selectedElements.Where(p => p is Room).ToList();

      if (!selectedRooms.Any())
        selectedRooms.Add(_lastSelectedRoom);

      var regionSelected = (ToolStripMenuItem) sender;

      foreach (var selectedRoom in selectedRooms.Cast<Room>()) selectedRoom.Region = regionSelected.Text;
    }

    private void RegionsToolStripMenuItemClick(object sender, EventArgs e) {
      SwapRoomRegions();
    }

    private void RenameToolStripMenuItemClick(object sender, EventArgs e) {
      if (HasSingleSelectedElement) _commandController.ShowElementProperties(SelectedElement);
    }

    private void RequestRecomputeSmartSegments() {
      _smartLineSegmentsUpToDate = false;
      _recomputeTimer.Change(RecomputeNMillisecondsAfterChange, RecomputeNMillisecondsAfterChange);
    }

    private void Reset() {
      ZoomFactor = 1;
      Origin = Vector.Zero;
      SelectedElement = null;
      HoverElement = null;
      HoverHandle = null;
      HoverPort = null;
      DragMode = DragModes.None;
      NewConnectionStyle = ConnectionStyle.Solid;
      NewConnectionFlow = ConnectionFlow.TwoWay;
      NewConnectionLabel = ConnectionLabel.None;
      _newRoomSize = new Vector(Settings.GridSize * 3, Settings.GridSize * 2);
      _newRoomIsDark = false;
      _newRoomObjectsPosition = CompassPoint.South;
      _newRoomStyleSource = null;
      RequestRecomputeSmartSegments();
      StopAutomapping();
      // roomTooltip.SetSuperTooltip(this, null);
    }

    private void ResizeRoom(Keys keyCode) {
      foreach (var element in SelectedElements.OfType<ISizeable>()) {
        var delta = 2.0f;
        if (Settings.SnapToGrid)
          delta = Settings.GridSize;
        var room = element;


        switch (keyCode) {
          case Keys.Left:
            var f = room.Width - delta;
            if (f >= Settings.GridSize)
              room.Size = new Vector(f, room.Height);
            break;

          case Keys.Right:
            room.Size = new Vector(room.Width + delta, room.Height);
            break;

          case Keys.Up:
            var fUp = room.Height - delta;
            if (fUp >= Settings.GridSize)
              room.Size = new Vector(room.Width, fUp);
            break;

          case Keys.Down:
            room.Size = new Vector(room.Width, room.Height + delta);
            break;
        }
      }
    }

    private void RoomPropertiesToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.ShowElementProperties(SelectedElement);
    }

    private void RoundedEdgesToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.RoundedCorners);
    }

    private void ScrollBar_Scroll(object sender, ScrollEventArgs e) {
      if (_updatingScrollBars) return;

      // the scroll bar will Invalidate() and Update() us; avoid exceptions
      _doNotUpdateScrollBarsNextPaint = true;

      var clientDelta = e.NewValue - e.OldValue;
      if (ApplicationSettingsController.AppSettings.InfiniteScrollBounds && e.Type == ScrollEventType.SmallIncrement || e.Type == ScrollEventType.SmallDecrement)
        if (Math.Abs(clientDelta) != _vScrollBar.SmallChange)
          clientDelta = _vScrollBar.SmallChange * (e.Type == ScrollEventType.SmallIncrement ? 1 : -1);
      if (clientDelta != 0)
        if (sender == _vScrollBar)
          Origin += new Vector(ClientToCanvas(new SizeF(0, clientDelta)));
        else
          Origin += new Vector(ClientToCanvas(new SizeF(clientDelta, 0)));
    }

    private bool SelectRoomRelativeToSelectedConnection(CompassPoint compassPoint) {
      if (SelectedElement is Connection element) {
        var conn = element;

        var firstEndPoint = (Room.CompassPort) conn.VertexList[0].Port;
        var secondEndPoint = (Room.CompassPort) conn.VertexList[1].Port;

        var firstRoomConnectionDir = firstEndPoint?.CompassPoint;
        var secondRoomConnectionDir = secondEndPoint?.CompassPoint;

        var firstOutDirection = RoughOpposite(firstRoomConnectionDir);
        var secondOutDirection = RoughOpposite(secondRoomConnectionDir);

        var overrideDir =
          firstOutDirection == CompassPoint.NorthEast && (compassPoint == CompassPoint.North || compassPoint == CompassPoint.East) ||
          firstOutDirection == CompassPoint.NorthWest && (compassPoint == CompassPoint.North || compassPoint == CompassPoint.West) ||
          firstOutDirection == CompassPoint.SouthEast && (compassPoint == CompassPoint.South || compassPoint == CompassPoint.East) ||
          firstOutDirection == CompassPoint.SouthWest && (compassPoint == CompassPoint.South || compassPoint == CompassPoint.West);

        if (overrideDir || firstOutDirection != null && EqualEnough(compassPoint, (CompassPoint) firstOutDirection)) {
          var tSelectedElement = conn.VertexList[0]?.Port?.Owner;
          if (tSelectedElement != null) {
            _commandController.MakeVisible(tSelectedElement);
            HoverElement = null;
            SelectedElement = tSelectedElement;
            Refresh();
            return true;
          }

          return false;
        }

        overrideDir =
          secondOutDirection == CompassPoint.NorthEast && (compassPoint == CompassPoint.North || compassPoint == CompassPoint.East) ||
          secondOutDirection == CompassPoint.NorthWest && (compassPoint == CompassPoint.North || compassPoint == CompassPoint.West) ||
          secondOutDirection == CompassPoint.SouthEast && (compassPoint == CompassPoint.South || compassPoint == CompassPoint.East) ||
          secondOutDirection == CompassPoint.SouthWest && (compassPoint == CompassPoint.South || compassPoint == CompassPoint.West);

        if (overrideDir || secondOutDirection != null && EqualEnough(compassPoint, (CompassPoint) secondOutDirection)) {
          var tSelectedElement = conn.VertexList[1]?.Port?.Owner;
          if (tSelectedElement != null) {
            _commandController.MakeVisible(tSelectedElement);
            HoverElement = null;
            SelectedElement = tSelectedElement;
            Refresh();
            return true;
          }

          return false;
        }
      }

      return false;
    }


    /// <summary>
    ///   Select the room in the given direction from the selected room;
    /// </summary>
    /// <param name="compassPoint">The direction to consider.</param>
    /// <returns>True if a new room was found and selected; false otherwise.</returns>
    private bool SelectRoomRelativeToSelectedRoom(CompassPoint compassPoint) {
      if (SelectedElement is Room element) {
        var room = element;
        var nextRoom = GetRoomInApproximateDirectionFromRoom(room, compassPoint);
        if (nextRoom != null) {
          SelectedElement = nextRoom;
          _commandController.MakeVisible(SelectedElement);
          return true;
        }
      }

      return false;
    }

    private void SendToBackToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SendToBack();
    }

    private void SetConnectionDefaultsFrom(Connection connection) {
      NewConnectionFlow = connection.Flow;
      NewConnectionStyle = connection.Style;
    }

    private void SetRoomDefaultsFrom(Room room) {
      _newRoomSize = room.Size;
      _newRoomIsDark = room.IsDark;
      _newRoomObjectsPosition = room.ObjectsPosition;
      _newRoomStyleSource = room;
    }

    private void ShiftArrowHandler(Keys keyCode) {
      if (!HasSingleSelectedElement) return;
      if (SelectedElement.GetType() == typeof(Connection)) {
        CtrlArrowHandler(keyCode);
        return;
      }

      if (SelectedElement.GetType() != typeof(Room)) return;

      var thisRoom = SelectedElement as Room;
      var direction = IndicatedDirection(keyCode);

      //this seems prohibitively time consuming as Genstein pointed out elsewhere, and I don't like the code. It can probably be simpler.
      //But the basic idea is to try the main direction, then the direction clockwise, then the direction counterclockwise.
      if (thisRoom == null) return;

      foreach (var tSelectedElement in thisRoom.GetConnections(direction))
        if (tSelectedElement != null) {
          _commandController.MakeVisible(tSelectedElement);
          SelectedElement = tSelectedElement;
          Refresh();
          return;
        }

      foreach (var tSelectedElement in thisRoom.GetConnections(CompassPointHelper.RotateClockwise(direction)))
        if (tSelectedElement != null) {
          _commandController.MakeVisible(tSelectedElement);
          SelectedElement = tSelectedElement;
          Refresh();
          return;
        }

      foreach (var tSelectedElement in thisRoom.GetConnections(CompassPointHelper.RotateAntiClockwise(direction)))
        if (tSelectedElement != null) {
          _commandController.MakeVisible(tSelectedElement);
          SelectedElement = tSelectedElement;
          Refresh();
          return;
        }
    }

    private void StartRoomToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetStartRoom();
    }

    private void UpdateDragHover(Point mousePosition) {
      _lastKnownMousePosition = mousePosition;

      var clientPos = new PointF(mousePosition.X, mousePosition.Y);
      var canvasPos = ClientToCanvas(clientPos);

      switch (DragMode) {
        case DragModes.Pan:
          DoDragPan(clientPos);
          break;
        case DragModes.MoveElement:
          DoDragMoveElement(canvasPos);
          break;
        case DragModes.MoveResizeHandle:
          DoDragMoveResizeHandle(canvasPos);
          break;
        case DragModes.MoveWaypoint:
          DoDragMoveWaypoint(mousePosition, canvasPos);
          break;
        case DragModes.MovePort:
          HoverElement = HitTestElement(canvasPos, true);
          HoverPort = HitTestPort(canvasPos);
          DoDragMovePort(canvasPos);
          break;
        case DragModes.None:
          HoverWaypoint = HitTestWaypoint(canvasPos);
          HoverHandle = HitTestHandle(canvasPos); // set first; it will RecreatePorts() if the value changes
          HoverPort = HitTestPort(canvasPos);
          var hoverElement = HitTestElement(canvasPos, false);
          HoverElement = hoverElement;

          Cursor.Current = hoverElement is Room && ((Room) hoverElement).IsReference && ModifierKeys == Keys.Control ? Cursors.Hand : Cursors.Default;

          if (hoverElement == null || !ApplicationSettingsController.AppSettings.ShowTooltips ||
              !hoverElement.HasTooltip()) {
            HideElementToolTip();
          } else {
            if (_trizbortToolTip1.HoverElement == hoverElement) return;
            HideElementToolTip();
            if (hoverElement.GetToolTipHeader() == string.Empty && hoverElement.GetToolTipText() == string.Empty) return;

            _trizbortToolTip1.BodyText = hoverElement.GetToolTipText();
            _trizbortToolTip1.FooterText = hoverElement.GetToolTipFooter();
            _trizbortToolTip1.TitleText = hoverElement.GetToolTipHeader();

            if (hoverElement is Room) {
              _trizbortToolTip1.BackColor = Color.LightBlue;
            } else if (hoverElement is Connection) {
              _trizbortToolTip1.BackColor = Color.LemonChiffon;
            }

            _trizbortToolTip1.HoverElement = hoverElement;
            _trizbortToolTip1.SetToolTip(this, string.IsNullOrEmpty(_trizbortToolTip1.TitleText)
              ? _trizbortToolTip1.BodyText : _trizbortToolTip1.TitleText);
          }

          break;
        case DragModes.DrawLine:
          if (new Vector(_lastMouseDownPosition).Distance(new Vector(mousePosition)) > Settings.DragDistanceToInitiateNewConnection) {
            var startPos = new PointF(_lastMouseDownPosition.X, _lastMouseDownPosition.Y);
            BeginDrawConnection(ClientToCanvas(startPos));
          }

          break;
        case DragModes.Marquee:
          if (_dragMarqueeLastPosition != canvasPos) {
            _dragMarqueeLastPosition = canvasPos;
            Invalidate();
          }

          break;
      }
    }

    private void UpdateSelection() {
      _selectedWaypoint = null;
      HoverWaypoint = null;
      RecreateHandles();
      RecreatePorts();
      // only if we have a single element selected;
      // otherwise selecting multiple items will cause one to override the others' settings!
      var selectedElement = SelectedElement;
      if (selectedElement is Connection)
        SetConnectionDefaultsFrom((Connection) selectedElement);
      else if (selectedElement is Room) SetRoomDefaultsFrom((Room) selectedElement);
      Invalidate();
    }

    private enum DragModes {
      None,
      Pan,
      MoveElement,
      MoveResizeHandle,
      MoveWaypoint,
      MovePort,
      Marquee,
      DrawLine
    }
  }
}