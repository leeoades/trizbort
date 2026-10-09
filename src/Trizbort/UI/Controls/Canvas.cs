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
    private const int RECOMPUTE_N_MILLISECONDS_AFTER_CHANGE = 500;
    private static bool sMSmartLineSegmentsUpToDate;
    private readonly CommandController _commandController;
    private readonly List<ResizeHandle> _mHandles = new List<ResizeHandle>();
    private readonly List<Port> _mPorts = new List<Port>();
    private readonly Timer _mRecomputeTimer;
    private readonly List<Element> _mSelectedElements = new List<Element>();
    private Room _lastSelectedRoom;
    private bool _mDoNotUpdateScrollBarsNextPaint;
    private Vector _mDragMarqueeLastPosition;
    private DragModes _mDragMode;
    private MoveablePort _mDragMovePort;
    private Vector _mDragOffsetCanvas;
    private Vector _mDragResizeHandleLastPosition;
    private Element _mHoverElement;
    private ResizeHandle _mHoverHandle;
    private Port _mHoverPort;
    private CurveWaypoint? _mHoverWaypoint;
    private CurveWaypoint? _mSelectedWaypoint;
    private CurveWaypoint _mDragWaypoint;
    private Vector _mDragWaypointOffset;
    private Point _mLastKnownMousePosition;
    private Point _mLastMouseDownPosition;
    private ConnectionFlow _mNewConnectionFlow;
    private ConnectionLabel _mNewConnectionLabel;
    private ConnectionStyle _mNewConnectionStyle;
    private bool _mNewRoomIsDark;
    private CompassPoint _mNewRoomObjectsPosition;
    private Vector _mNewRoomSize;
    private Room _mNewRoomStyleSource;
    private Vector _mOrigin;
    private PointF _mPanPosition;
    private bool _mUpdatingScrollBars;
    private float _mZoomFactor;

    public Canvas() {
      InitializeComponent();

      _commandController = new CommandController(this);

      SetStyle(ControlStyles.Selectable, true);
      TabStop = true;
      TabIndex = 0;

      SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
      DoubleBuffered = true;

      PreviewKeyDown += onPreviewKeyDown;
      ctxCanvasMenu.Items.Insert(0, new ToolStripMenuItem("Add &Label", null, (_, __) => AddLabel(true)));

      _mRecomputeTimer = new Timer(onRecomputeTimerTick);

      Project.ProjectChanged += onProjectChanged;
      onProjectChanged(this, new ProjectChangedEventArgs(null, Project.Current));

      Settings.Changed += onSettingsChanged;
      onSettingsChanged(this, EventArgs.Empty);

      _mThreadSafeAutomapCanvas = new MultithreadedAutomapCanvas(this);
      m_minimap.Canvas = this;
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

        if (HoverElement is IMoveable && _mSelectedElements.Contains(HoverElement)) return Cursors.SizeAll;
        return base.Cursor;
      }
      set => base.Cursor = value;
    }

    public bool HasSelectedRooms => _mSelectedElements.OfType<Room>().Any();
    public bool HasSingleSelectedElement => SelectedElementCount == 1;
    public bool HasSingleSelectedRoom => _mSelectedElements.OfType<Room>().Count() == 1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Element HoverElement {
      get => _mHoverElement;
      set {
        if (_mHoverElement == value) return;
        _mHoverElement = value;
        recreatePorts();
      }
    }

    public bool MinimapVisible {
      get => m_minimap.Visible;
      set {
        m_minimap.Visible = value;
        if (!m_minimap.Visible) {
          m_vScrollBar.Top = 0;
          m_vScrollBar.Height = Height - m_hScrollBar.Height;
        } else {
          m_vScrollBar.Top = m_minimap.Bottom;
          m_vScrollBar.Height = Height - m_hScrollBar.Height - m_minimap.Height;
        }
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConnectionFlow NewConnectionFlow {
      get => _mNewConnectionFlow;
      set {
        if (_mNewConnectionFlow == value) return;
        _mNewConnectionFlow = value;
        raiseNewConnectionFlowChanged();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConnectionLabel NewConnectionLabel {
      get => _mNewConnectionLabel;
      set {
        if (_mNewConnectionLabel == value) return;
        _mNewConnectionLabel = value;
        raiseNewConnectionLabelChanged();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConnectionStyle NewConnectionStyle {
      get => _mNewConnectionStyle;
      set {
        if (_mNewConnectionStyle == value) return;
        _mNewConnectionStyle = value;
        raiseNewConnectionStyleChanged();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Vector Origin {
      private get => _mOrigin;
      set {
        if (_mOrigin == value) return;
        _mOrigin = value;
        Invalidate();
      }
    }

    public List<Connection> SelectedConnections { get { return _mSelectedElements.Where(p => p is Connection).ToList().Cast<Connection>().ToList(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Element SelectedElement {
      get => _mSelectedElements.Count > 0 ? _mSelectedElements[_mSelectedElements.Count - 1] : null;
      set {
        var selectedElement = _mSelectedElements.Count > 0 ? _mSelectedElements[_mSelectedElements.Count - 1] : null;
        if (selectedElement != value) {
          _mSelectedElements.Clear();
          if (value != null) _mSelectedElements.Add(value);
          updateSelection();
        }
      }
    }

    public int SelectedElementCount => _mSelectedElements.Count;

    public IList<Element> SelectedElements => _mSelectedElements;

    public List<Room> SelectedRooms { get { return _mSelectedElements.Where(p => p is Room).ToList().Cast<Room>().ToList(); } }

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
      get => _mZoomFactor;
      set {
        if (_mZoomFactor != value) {
          _mZoomFactor = value;
          lblZoom.Text = _mZoomFactor.ToString("p0");
          Invalidate();
        }
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private DragModes DragMode {
      get => _mDragMode;
      set {
        _mDragMode = value;
        recreatePorts();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private ResizeHandle HoverHandle {
      get => _mHoverHandle;
      set {
        if (_mHoverHandle == value) return;
        _mHoverHandle = value;
        Invalidate();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private Port HoverPort {
      get => _mHoverPort;
      set {
        if (_mHoverPort == value) return;
        _mHoverPort = value;
        Invalidate();
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    private CurveWaypoint? HoverWaypoint {
      get => _mHoverWaypoint;
      set {
        if (_mHoverWaypoint == value) return;
        _mHoverWaypoint = value;
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

    public void RemoveRoom(Room mOtherRoom) {
      Project.Current.Elements.Remove(mOtherRoom);
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
      var room = new Room(Project.Current) {Size = _mNewRoomSize};
      if (ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms)
        room.CopyStyleFrom(_mNewRoomStyleSource);

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

            addConnection(source, sourceCompass, room, targetCompass);
            addConnection(room, sourceCompass, target, targetCompass);

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

    public void ChangeZoom(float mZoom) {
      lblZoom.Text = mZoom.ToString("p0");
      _mZoomFactor = mZoom;
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
      controller.CopyElements(_mSelectedElements);
    }

    public void DeleteSelection() {
      var connection = WaypointConnection;
      if (connection != null && _mSelectedWaypoint.HasValue && connection.RemoveCurveWaypoint(_mSelectedWaypoint.Value)) {
        _mSelectedWaypoint = null;
        HoverWaypoint = null;
        Invalidate();
        return;
      }

      var doomedElements = new List<Element>(_mSelectedElements);
      foreach (var element in doomedElements) Project.Current.Elements.Remove(element);
      _mSelectedElements.Clear();
      updateSelection();
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

        if (!finalRender) drawGrid(graphics, palette);

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

        drawElements(graphics, palette, finalRender);
        if (!finalRender) {
          drawHandles(graphics, palette);
          drawPorts(graphics, palette);
          drawMarquee(graphics, palette);
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
      return _mSelectedElements.OfType<T>().Any();
    }

    public void JoinSelectedRooms(Room room1, Room room2) {
      var rect1 = room1.InnerBounds;
      var rect2 = room2.InnerBounds;

      var dx = rect1.X - rect2.X;
      var dy = rect1.Y - rect2.Y;

      if (dy == 0 && dx != 0) {
        if (dx > 0)
          addConnection(room1, CompassPoint.West, room2, CompassPoint.East);
        else
          addConnection(room1, CompassPoint.East, room2, CompassPoint.West);
      } else if (dy != 0 && dx == 0) {
        if (dy > 0)
          addConnection(room1, CompassPoint.North, room2, CompassPoint.South);
        else
          addConnection(room1, CompassPoint.South, room2, CompassPoint.North);
      } else {
        if (Math.Abs(dy) >= Math.Abs(dx))
          if (dy > 0)
            addConnection(room1, CompassPoint.North, room2, CompassPoint.South);
          else
            addConnection(room1, CompassPoint.South, room2, CompassPoint.North);
        else if (dx > 0)
          addConnection(room1, CompassPoint.West, room2, CompassPoint.East);
        else
          addConnection(room1, CompassPoint.East, room2, CompassPoint.West);
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
          pasteColors(xx);
        }
    }

    public void ResetZoomOrigin() {
      Origin = ComputeCanvasBounds(false).Center;
      ZoomFactor = 2.0f;
    }

    public void ReverseLineDirection() {
      foreach (var element in _mSelectedElements)
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
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements);
      updateSelection();
    }

    public void SelectAllConnections() {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements.OfType<Connection>());
      updateSelection();
    }

    public void SelectAllRegion(IEnumerable<string> regions) {
      _mSelectedElements.Clear();
      var regionRooms = Project.Current.Elements.OfType<Room>().ToList().Where(p => regions.Contains(p.Region));
      _mSelectedElements.AddRange(regionRooms);
      updateSelection();
    }

    public void SelectAllRooms() {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements.OfType<Room>());
      updateSelection();
    }

    public void SelectAllUnconnectedRooms() {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements.OfType<Room>().Where(p => p.GetConnections().Count == 0));
      updateSelection();
    }

    public void SelectDanglingConnections() {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements.OfType<Connection>().Where(p => p.IsDangling));
      updateSelection();
    }

    public void SelectElements(List<Element> elements) {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(elements);
    }

    public void SelectRoomsWithObjects() {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements.OfType<Room>().Where(p => p.ListOfObjects().Count > 0));
      updateSelection();
    }

    public void SelectRoomsWithoutObjects() {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements.OfType<Room>().Where(p => p.ListOfObjects().Count == 0));
      updateSelection();
    }

    public void SelectSelfLoopingConnections() {
      _mSelectedElements.Clear();
      _mSelectedElements.AddRange(Project.Current.Elements.OfType<Connection>().Where(p => {
        var sourceRoom = p.GetSourceRoom();
        var targetRoom = p.GetTargetRoom();
        return sourceRoom != null && targetRoom != null && sourceRoom == targetRoom;
      }));
      updateSelection();
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
      _mUpdatingScrollBars = true;

      var topLeft = PointF.Empty;
      var displaySize = new PointF(Math.Max(0, Width - m_vScrollBar.Width), Math.Max(0, Height - m_hScrollBar.Height));

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
        m_vScrollBar.Enabled = false;
      } else {
        m_vScrollBar.Enabled = true;
        m_vScrollBar.Minimum = (int) Math.Min(topLeft.Y, clientBounds.Top);
        m_vScrollBar.Maximum = (int) Math.Max(topLeft.Y + displaySize.Y, clientBounds.Bottom) - 1; // -1 since Maximum is actually maximum value + 1; see MSDN.
        m_vScrollBar.Value = (int) Math.Max(m_vScrollBar.Minimum, Math.Min(m_vScrollBar.Maximum, topLeft.Y));
        m_vScrollBar.LargeChange = (int) displaySize.Y;
        m_vScrollBar.SmallChange = (int) (displaySize.Y / 10);
      }

      if (!ApplicationSettingsController.AppSettings.InfiniteScrollBounds && topLeft.X <= clientBounds.Left && topLeft.X + displaySize.X >= clientBounds.Right) {
        m_hScrollBar.Enabled = false;
      } else {
        m_hScrollBar.Enabled = true;
        m_hScrollBar.Minimum = (int) Math.Min(topLeft.X, clientBounds.Left);
        m_hScrollBar.Maximum = (int) Math.Max(topLeft.X + displaySize.X, clientBounds.Right) - 1; // -1 since Maximum is actually maximum value + 1; see MSDN.
        m_hScrollBar.Value = (int) Math.Max(m_hScrollBar.Minimum, Math.Min(m_hScrollBar.Maximum, topLeft.X));
        m_hScrollBar.LargeChange = (int) displaySize.X;
        m_hScrollBar.SmallChange = (int) (displaySize.X / 10);
      }

      _mUpdatingScrollBars = false;
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
        Project.ProjectChanged -= onProjectChanged;
        Settings.Changed -= onSettingsChanged;
        Project.Current.Elements.Added -= onElementAdded;
        Project.Current.Elements.Removed -= onElementRemoved;
        foreach (var element in Project.Current.Elements) element.Changed -= onElementChanged;
        _mRecomputeTimer?.Dispose();
        trizbortToolTip1?.Dispose();
        components?.Dispose();
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
              shiftArrowHandler(Keys.Home);
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
            shiftArrowHandler(e.KeyCode);
          break;


        case Keys.Right:
        case Keys.Left:
        case Keys.Up:
        case Keys.Down:
          switch (ModifierKeys) {
            case Keys.Alt | Keys.Control:
              resizeRoom(e.KeyCode);
              break;
            case Keys.Control:
              ctrlArrowHandler(e.KeyCode);
              break;
            case Keys.Shift:
              shiftArrowHandler(e.KeyCode);
              break;
            default:
              moveArrowKeyHandler(e.KeyCode, e.Shift);
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
              moveActiveSelected(false);

              break;

            default:
              moveActiveSelected();

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
                _mAutomap.RunToCompletion();

              break;
          }

          break;

        case Keys.F11:
          if (IsAutomapping)
            _mAutomap.Step();

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
            shiftArrowHandler(e.KeyCode);
          else
            addOrSelectRooms(indicatedDirection(e.KeyCode));
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
        else if (ApplicationSettingsController.AppSettings.DoubleClickToAddRoom && CanSelectElements && isEmptySpace(e.Location))
          AddRoom(true);
      }
      base.OnMouseDoubleClick(e);
    }

    private bool isEmptySpace(Point clientPos) {
      if (HoverHandle != null || HoverPort != null || HoverWaypoint.HasValue) return false;
      return hitTestElement(ClientToCanvas(new PointF(clientPos.X, clientPos.Y)), false) == null;
    }

    protected override void OnMouseDown(MouseEventArgs e) {
      hideElementToolTip();
      var clientPos = new PointF(e.X, e.Y);
      var canvasPos = ClientToCanvas(clientPos);
      _mLastMouseDownPosition = e.Location;

      if (DragMode != DragModes.None)
        return;

      if (isDragButton(e)) {
        beginDragPan(clientPos);
      } else if (e.Button == MouseButtons.Left) {
        if (CanSelectElements) beginDragMove(canvasPos);
        if (DragMode == DragModes.None)
          if (HoverPort != null && CanDrawLine)
            beginDragDrawLine();
      } else if (e.Button == MouseButtons.Right) {
        if (CanSelectElements)
          beginDragMove(canvasPos);
      }

      base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e) {
      // ignore spurious mouse move events
      if (_mLastKnownMousePosition == e.Location) return;
      _mLastKnownMousePosition = e.Location;

      updateDragHover(e.Location);
      base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e) {
      endDrag();

      base.OnMouseUp(e);
    }

    protected override void OnMouseLeave(EventArgs e) {
      hideElementToolTip();
      base.OnMouseLeave(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
      hideElementToolTip();
      if (e.X < 0 || e.X > Width || e.Y < 0 || e.Y > Height)
        return;

      var pos = ClientToCanvas(new PointF(e.X, e.Y));

      if (isZoomIn(e.Delta))
        if (ModifierKeys == Keys.Control)
          ZoomInMicro();
        else
          ZoomIn();
      else if (isZoomOut(e.Delta) && ZoomFactor > 1 / 100.0f)
        if (ModifierKeys == Keys.Control)
          ZoomOutMicro();
        else
          ZoomOut();

      var newPos = ClientToCanvas(new PointF(e.X, e.Y));
      Origin = Origin - (newPos - pos);

      Invalidate();
      updateDragHover(e.Location);

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
      if (!_mDoNotUpdateScrollBarsNextPaint) UpdateScrollBars();
      _mDoNotUpdateScrollBarsNextPaint = false;

      // update the minimap
      m_minimap.Invalidate();
      m_minimap.Update();
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
    private Connection addConnection(Element roomOne, CompassPoint compassPointOne, Element roomTwo, CompassPoint compassPointTwo) {
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
    private void addOrConnectRoomRelativeToSelectedRoom(CompassPoint compassPoint) {
      if (SelectedElement is Room element) {
        var room = element;
        var rect = room.InnerBounds;
        rect.Inflate(Settings.PreferredDistanceBetweenRooms + room.Width / 2, Settings.PreferredDistanceBetweenRooms + room.Height / 2);
        var centerOfNewRoom = rect.GetCorner(compassPoint);

        var existing = hitTestElement(centerOfNewRoom, false);
        if (existing is Room two) {
          // just connect the rooms together
          addConnection(room, compassPoint, two, CompassPointHelper.GetOpposite(compassPoint));
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
          addConnection(room, compassPoint, newRoom, CompassPointHelper.GetOpposite(compassPoint));
          SelectedElement = newRoom;
          _commandController.MakeVisible(SelectedElement);
          Refresh();
          newRoom.ShowDialog();
        }
      }
    }

    private void addOrSelectRooms(CompassPoint? compassPoint) {
      if (compassPoint != null && !selectRoomRelativeToSelectedRoom(compassPoint.Value))
        if (ModifierKeys == Settings.KeypadNavigationCreationModifier) {
          addOrConnectRoomRelativeToSelectedRoom(compassPoint.Value);
          selectRoomRelativeToSelectedConnection(compassPoint.Value);
        } else if (ModifierKeys == Settings.KeypadNavigationUnexploredModifier) {
          addUnexploredConnectionToSelectedRoom(compassPoint.Value);
        }
    }

    private void addRoomToolStripMenuItem_Click(object sender, EventArgs e) {
      AddRoom(true, true);
    }

    /// <summary>
    ///   Add an "unexplored" (loopback) connection from
    /// </summary>
    /// <param name="compassPoint"></param>
    private void addUnexploredConnectionToSelectedRoom(CompassPoint compassPoint) {
      if (SelectedElement is Room element) {
        var room = element;
        addConnection(room, compassPoint, room, compassPoint);
      }
    }

    private void applicationSettingsToolStripMenuItem_Click(object sender, EventArgs e) {
      ApplicationSettingsController.ShowAppDialog();
    }

    private void beginDragDrawLine() {
      DragMode = DragModes.DrawLine;
      Capture = true;
    }

    private void beginDragMove(Vector canvasPos) {
      if (HoverWaypoint.HasValue && WaypointConnection != null) {
        _mDragWaypoint = HoverWaypoint.Value;
        _mDragWaypointOffset = WaypointConnection.GetCurveWaypointHandlePosition(_mDragWaypoint) - canvasPos;
        // a "ghost" handle only becomes a real waypoint once it is actually dragged
        _mSelectedWaypoint = WaypointConnection.GetCurveWaypoint(_mDragWaypoint).HasValue ? _mDragWaypoint : (CurveWaypoint?) null;
        DragMode = DragModes.MoveWaypoint;
        Capture = true;
      } else if (HoverHandle != null) {
        DragMode = DragModes.MoveResizeHandle;
        _mDragResizeHandleLastPosition = canvasPos; // unsnapped
        Capture = true;
      } else if (HoverPort != null) {
        if (HoverPort is MoveablePort) {
          _mDragMovePort = (MoveablePort) HoverPort;
          _mDragOffsetCanvas = Settings.Snap(canvasPos - HoverPort.Position);
          DragMode = DragModes.MovePort;
          Capture = true;
        }
      } else {
        var hitElement = hitTestElement(canvasPos, false);

        var alreadySelected = _mSelectedElements.Contains(hitElement);
        if (!alreadySelected && (ModifierKeys & (Keys.Control | Keys.Shift)) == Keys.None)
          _mSelectedElements.Clear();
        else if (hitElement != null) _mSelectedElements.Remove(hitElement);
        if ((ModifierKeys & Keys.Shift) == Keys.Shift) {
          if (!alreadySelected && hitElement != null) _mSelectedElements.Add(hitElement);
        } else if (hitElement != null) {
          // if we're not holding shift, ensure the current element is selected.
          // we're safe to re-add it since it will definitely have been removed already
          // if it was selected, by the above logic.
          _mSelectedElements.Add(hitElement);
        }

        // now we've finished messing with the set of selected elements,
        // update handles, ports, and take defaults for new elements from the most recently selected element.
        updateSelection();

        if (hitElement != null && _mSelectedElements.Contains(hitElement)) {
          // if we ended up with the hit element being selected, initiate a drag move.
          DragMode = DragModes.MoveElement;
          canvasPos = Settings.Snap(canvasPos);
          _mDragOffsetCanvas = canvasPos;
          Capture = true;
        } else if (hitElement == null) {
          // if we didn't hit anything at all, begin a new marquee selection.
          DragMode = DragModes.Marquee;
          _mDragOffsetCanvas = canvasPos;
          _mDragMarqueeLastPosition = canvasPos;
          Capture = true;
        }
      }

      Invalidate();
    }


    private void beginDragPan(PointF clientPos) {
      DragMode = DragModes.Pan;
      _mPanPosition = clientPos;
      Cursor = Cursors.NoMove2D;
      Capture = true;
    }

    private void beginDrawConnection(Vector canvasPos) {
      Connection connection;
      HoverPort = hitTestPort(canvasPos);
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
      _mDragMovePort = (MoveablePort) connection.PortList[1];
      _mDragOffsetCanvas = Settings.Snap(canvasPos - connection.VertexList[0].Position);
      HoverPort = null;
      DragMode = DragModes.MovePort;
      Capture = true;
    }

    private void bringToFrontToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.BringToFront();
    }

    private void ctrlArrowHandler(Keys keyCode) {
      var direction = indicatedDirection(keyCode);

      if (!selectRoomRelativeToSelectedRoom(direction) && !selectRoomRelativeToSelectedConnection(direction))
        addOrConnectRoomRelativeToSelectedRoom(direction);
    }

    private void ctxCanvasMenu_Opening(object sender, CancelEventArgs e) {
      if ((m_minimap.Visible && m_minimap.IsMouseOverMe()) || DragMode == DragModes.Pan)
        e.Cancel = true;

      var clientPos = new PointF(_mLastMouseDownPosition.X, _mLastMouseDownPosition.Y);
      var canvasPos = ClientToCanvas(clientPos);
      var hitElement = hitTestElement(canvasPos, false);
      var regionMenu = regionToolStripMenuItem;

      if (hitElement != null) {
        if (hitElement is Room) {
          _lastSelectedRoom = (Room) hitElement;

          regionMenu.DropDownItems.Clear();

          foreach (var region in Settings.Regions.OrderBy(p => p.RegionName != Domain.Misc.Region.DefaultRegion).ThenBy(p => p.RegionName)) {
            var item = regionMenu.DropDownItems.Add(region.RegionName, null, regionContextClick);
            item.Image = generateRegionImage(region);
            if (region.RegionName == _lastSelectedRoom.Region)
              ((ToolStripMenuItem) item).Checked = true;
          }

          addRoomToolStripMenuItem.Visible = true;
          renameToolStripMenuItem.Visible = true;
          darkToolStripMenuItem.Visible = true;
          regionToolStripMenuItem.Visible = true;
          roomShapeToolStripMenuItem.Visible = true;
          joinRoomsToolStripMenuItem.Visible = true;
          swapObjectsToolStripMenuItem.Visible = true;
          roomPropertiesToolStripMenuItem.Visible = true;

          toolStripSeparator6.Visible = true;
          startRoomToolStripMenuItem.Visible = true;
          endRoomToolStripMenuItem.Visible = true;

          startRoomToolStripMenuItem.Enabled = SelectedRooms.Count == 1;
          endRoomToolStripMenuItem.Enabled = HasSelectedRooms;

          startRoomToolStripMenuItem.Checked = _lastSelectedRoom.IsStartRoom && HasSingleSelectedElement;
          endRoomToolStripMenuItem.Checked = HasSelectedRooms && _lastSelectedRoom.IsEndRoom;

          sendToBackToolStripMenuItem.Visible = true;
          bringToFrontToolStripMenuItem.Visible = true;
          toolStripSeparator7.Visible = true;

          toolStripMenuItem1.Visible = true;
          toolStripMenuItem2.Visible = true;
          toolStripSeparator1.Visible = true;
          toolStripSeparator2.Visible = true;

          m_lineStylesMenuItem.Visible = false;
          m_reverseLineMenuItem.Visible = false;

          swapObjectsToolStripMenuItem.Enabled = SelectedRooms.Count == 2;
          joinRoomsToolStripMenuItem.Enabled = SelectedRooms.Count == 2 && !Project.Current.AreRoomsConnected(SelectedRooms);


          darkToolStripMenuItem.Checked = _lastSelectedRoom.IsDark;
        }

        if (hitElement is Connection || hitElement is MapLabel) {
          addRoomToolStripMenuItem.Visible = true;

          renameToolStripMenuItem.Visible = false;
          darkToolStripMenuItem.Visible = false;
          regionToolStripMenuItem.Visible = false;
          roomShapeToolStripMenuItem.Visible = false;
          joinRoomsToolStripMenuItem.Visible = false;
          swapObjectsToolStripMenuItem.Visible = false;
          roomPropertiesToolStripMenuItem.Visible = true;

          sendToBackToolStripMenuItem.Visible = false;
          bringToFrontToolStripMenuItem.Visible = false;
          toolStripSeparator7.Visible = false;

          startRoomToolStripMenuItem.Visible = false;
          endRoomToolStripMenuItem.Visible = false;
          toolStripSeparator6.Visible = false;

          m_lineStylesMenuItem.Visible = true;
          m_reverseLineMenuItem.Visible = true;

          toolStripMenuItem1.Visible = false;
          toolStripMenuItem2.Visible = false;
          toolStripSeparator1.Visible = true;
          toolStripSeparator2.Visible = true;

          roomPropertiesToolStripMenuItem.Enabled = true;
          sendToBackToolStripMenuItem.Visible = hitElement is MapLabel;
          bringToFrontToolStripMenuItem.Visible = hitElement is MapLabel;
          toolStripSeparator7.Visible = hitElement is MapLabel;
          m_lineStylesMenuItem.Visible = hitElement is Connection;
          m_reverseLineMenuItem.Visible = hitElement is Connection;
        }
      } else {
        renameToolStripMenuItem.Visible = false;
        darkToolStripMenuItem.Visible = false;
        regionToolStripMenuItem.Visible = false;
        roomShapeToolStripMenuItem.Visible = false;
        joinRoomsToolStripMenuItem.Visible = false;
        swapObjectsToolStripMenuItem.Visible = false;
        roomPropertiesToolStripMenuItem.Visible = false;

        startRoomToolStripMenuItem.Visible = false;
        endRoomToolStripMenuItem.Visible = false;
        toolStripSeparator6.Visible = false;

        sendToBackToolStripMenuItem.Visible = false;
        bringToFrontToolStripMenuItem.Visible = false;
        toolStripSeparator7.Visible = false;

        m_lineStylesMenuItem.Visible = false;
        m_reverseLineMenuItem.Visible = false;

        addRoomToolStripMenuItem.Visible = true;

        toolStripMenuItem1.Visible = false;
        toolStripMenuItem2.Visible = false;
        toolStripSeparator1.Visible = false;
        toolStripSeparator2.Visible = false;
      }
    }

    private void darkToolStripMenuItem_Click_1(object sender, EventArgs e) {
      foreach (var room in SelectedRooms) room.IsDark = !room.IsDark;
    }

    private List<Element> depthSortElements() {
      var elements = new List<Element>();
      elements.AddRange(Project.Current.Elements);
      elements.Sort();
      return elements;
    }

    private void doDragMoveElement(Vector canvasPos) {
      canvasPos = Settings.Snap(canvasPos);
      var delta = canvasPos - _mDragOffsetCanvas;
      moveSelectedElements(delta);
      _mDragOffsetCanvas = canvasPos;
    }

    private void moveSelectedElements(Vector delta) {
      MapEditing.Move(Project.Current.Elements, _mSelectedElements, delta);
      hideElementToolTip();
    }

    private void hideElementToolTip() {
      trizbortToolTip1.SetToolTip(this, null);
      trizbortToolTip1.Hide(this);
    }

    private void doDragMoveWaypoint(Point mousePosition, Vector canvasPos) {
      var connection = WaypointConnection;
      if (connection == null) return;

      if (!connection.GetCurveWaypoint(_mDragWaypoint).HasValue) {
        if (new Vector(_mLastMouseDownPosition).Distance(new Vector(mousePosition)) <= Settings.DragDistanceToInitiateNewConnection) return;
        if (!connection.CanAddCurveWaypoint(_mDragWaypoint)) return;
      }

      connection.SetCurveWaypoint(_mDragWaypoint, Settings.Snap(canvasPos + _mDragWaypointOffset));
      _mSelectedWaypoint = _mDragWaypoint;
    }

    private void doDragMovePort(Vector canvasPos) {
      if (HoverPort != null && HoverPort != _mDragMovePort) {
        if (_mDragMovePort.DockedAt != HoverPort && (!(HoverPort is MoveablePort) || ((MoveablePort) HoverPort).DockedAt != _mDragMovePort))
          if (!(HoverPort is MoveablePort)) {
            _mDragMovePort.DockAt(HoverPort);
          } else {
            canvasPos = Settings.Snap(canvasPos);
            _mDragMovePort.SetPosition(canvasPos - _mDragOffsetCanvas);
          }
      } else {
        canvasPos = Settings.Snap(canvasPos);
        _mDragMovePort.SetPosition(canvasPos - _mDragOffsetCanvas);
      }
    }

    private void doDragMoveResizeHandle(Vector canvasPos) {
      if (HoverHandle != null)
        _mDragResizeHandleLastPosition = MapEditing.Resize(HoverHandle, _mDragResizeHandleLastPosition, canvasPos);
    }

    private void doDragPan(PointF clientPos) {
      var delta = Drawing.Subtract(_mPanPosition, clientPos);
      delta = Drawing.Divide(delta, ZoomFactor);
      Origin = new Vector(Origin.X + delta.X, Origin.Y + delta.Y);
      _mPanPosition = clientPos;
      hideElementToolTip();
    }

    private void drawElements(XGraphics graphics, Palette palette, bool finalRender) {
      if (ApplicationSettingsController.AppSettings.DebugDisableElementRendering)
        return;

      var context = new DrawingContext(ZoomFactor) {UseSmartLineSegments = sMSmartLineSegmentsUpToDate};
      var elements = depthSortElements();

      if (!context.UseSmartLineSegments)
        foreach (var element in elements) {
          element.PreDraw(context);
          element.Flagged = false;
        }
      else
        foreach (var element in elements)
          element.Flagged = false;

      foreach (var element in _mSelectedElements) element.Flagged = true;

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

    private void drawGrid(XGraphics graphics, Palette palette) {
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

    private void drawHandles(XGraphics graphics, Palette palette) {
      drawWaypointHandles(graphics, palette);
      if (_mHandles.Count == 0) return;

      var context = new DrawingContext(ZoomFactor);

      if (_mHandles.Count > 1) {
        var bounds = _mHandles.Aggregate(Rect.Empty, (current, handle) => current == Rect.Empty ? new Rect(handle.Position, Vector.Zero) : current.Union(handle.Position));

        bounds.X += Settings.HandleSize / 2f;
        bounds.Y += Settings.HandleSize / 2f;
      }


      foreach (var handle in _mHandles) {
        context.Selected = handle == HoverHandle;
        handle.Draw(this, graphics, palette, context);
      }
    }

    private IEnumerable<CurveWaypoint> visibleWaypoints(Connection connection) {
      if (connection == null) yield break;
      // the middle handle is listed last so it is drawn on top and wins hit tests
      foreach (var waypoint in new[] {CurveWaypoint.Quarter, CurveWaypoint.ThreeQuarter, CurveWaypoint.Middle})
        if (connection.GetCurveWaypoint(waypoint).HasValue || connection.CanAddCurveWaypoint(waypoint))
          yield return waypoint;
    }

    // Handle size in canvas units; never shrinks below Settings.HandleSize on screen when zoomed out.
    private float WaypointHandleScale => Settings.HandleSize * Math.Max(1f, 1f / ZoomFactor);

    private Rect waypointHandleBounds(Connection connection, CurveWaypoint waypoint) {
      var size = WaypointHandleScale * (connection.GetCurveWaypoint(waypoint).HasValue ? 2f : 1.5f);
      var position = connection.GetCurveWaypointHandlePosition(waypoint);
      return new Rect(position.X - size / 2, position.Y - size / 2, size, size);
    }

    private void drawWaypointHandles(XGraphics graphics, Palette palette) {
      var connection = WaypointConnection;
      if (connection == null) return;

      var context = new DrawingContext(ZoomFactor);
      foreach (var waypoint in visibleWaypoints(connection)) {
        var isSet = connection.GetCurveWaypoint(waypoint).HasValue;
        context.Selected = waypoint == HoverWaypoint || waypoint == _mSelectedWaypoint && isSet;
        Drawing.DrawHandle(this, graphics, palette, waypointHandleBounds(connection, waypoint), context, !isSet, true);
      }
    }

    private CurveWaypoint? hitTestWaypoint(Vector canvasPos) {
      var connection = WaypointConnection;
      if (connection == null) return null;

      CurveWaypoint? hit = null;
      foreach (var waypoint in visibleWaypoints(connection)) {
        var bounds = waypointHandleBounds(connection, waypoint);
        // be generous so the handles are easy to grab
        bounds.Inflate(WaypointHandleScale / 2);
        if (bounds.Contains(canvasPos)) hit = waypoint;
      }

      return hit;
    }

    private void drawMarquee(XGraphics graphics, Palette palette) {
      var marqueeRect = getMarqueeCanvasBounds();
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

    private void drawPorts(XGraphics graphics, Palette palette) {
      var context = new DrawingContext(ZoomFactor);

      // draw all non-selected ports
      foreach (var port in _mPorts.Where(port => HoverPort != port)) {
        context.Selected = false;
        port.Draw(this, graphics, palette, context);
      }

      if (HoverPort == null) return;

      // lastly, always the port under the mouse, if any
      context.Selected = true;
      HoverPort.Draw(this, graphics, palette, context);
    }

    private void ellipseToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.Ellipse);
    }

    private void endDrag() {
      if (DragMode == DragModes.MovePort) {
        // clear the selection now the line is drawn
        SelectedElement = null;

        if (_mDragMovePort.Owner is Connection) {
          // remove dead connections
          var connection = (Connection) _mDragMovePort.Owner;
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
        var marqueeRect = getMarqueeCanvasBounds();
        if ((ModifierKeys & (Keys.Shift | Keys.Control)) == Keys.None) _mSelectedElements.Clear();
        foreach (var element in hitTest(marqueeRect, false))
          if (!_mSelectedElements.Contains(element))
            _mSelectedElements.Add(element);
          else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            if (_mSelectedElements.Contains(element))
              _mSelectedElements.Remove(element);
        updateSelection();
      }

      DragMode = DragModes.None;
      HoverHandle = null;
      Capture = false;
      Cursor = null;
      Invalidate();
    }

    private void endRoomToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetEndRoom();
    }

    private void formatsFillsToolStripMenuItem_Click(object sender, EventArgs e) {
      SwapRoomFill();
    }

    private Image generateRegionImage(Region region) {
      var image = new Bitmap(24, 20);
      var g = Graphics.FromImage(image);
      using var palette = new Palette();
      g.FillRectangle(palette.Brush(region.RColor), 0, 0, 24, 20);

      return image;
    }

    private Rect getMarqueeCanvasBounds() {
      if (DragMode != DragModes.Marquee) return Rect.Empty;
      var topLeft = _mDragOffsetCanvas;
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
    private Room getRoomInApproximateDirectionFromRoom(Room room, CompassPoint compassPoint) {
      var nextRoom = getRoomInExactDirectionFromRoom(room, compassPoint) ?? getRoomInExactDirectionFromRoom(room, CompassPointHelper.RotateAntiClockwise(compassPoint));

      return nextRoom ?? getRoomInExactDirectionFromRoom(room, CompassPointHelper.RotateClockwise(compassPoint));
    }

    /// <summary>
    ///   Get a room which can be found in the given direction from the given room.
    /// </summary>
    /// <param name="room">The initial room.</param>
    /// <param name="compassPoint">The direction to consider.</param>
    /// <returns>The room which can be found in that direction, or null if none.</returns>
    private Room getRoomInExactDirectionFromRoom(Room room, CompassPoint compassPoint) {
      var connections = room.GetConnections(compassPoint);
      foreach (var connection in connections)
      foreach (var vertex in connection.VertexList) {
        var port = vertex.Port;
        if (port != null && port.Owner != room && port.Owner is Room) return (Room) port.Owner;
      }

      return null;
    }

    private void handDrawnToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.SquareCorners);
    }

    private List<Element> hitTest(Rect rect, bool roomsOnly) {
      return Project.Current.Elements.Where(element => (!roomsOnly || element is Room) && element.Intersects(rect)).ToList();
    }

    private Element hitTestElement(Vector canvasPos, bool includeMargins) {
      var closest = new List<Element>();
      var closestDistance = float.MaxValue;
      foreach (var element in depthSortElements()) // sort into drawing order
      {
        if (DragMode == DragModes.MovePort && _mDragMovePort.Owner == element) continue;

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

    private ResizeHandle hitTestHandle(Vector canvasPos) {
      // examine handles, topmost (drawn) to lowermost
      for (var index = _mHandles.Count - 1; index >= 0; --index) {
        var handle = _mHandles[index];
        if (handle.HitTest(canvasPos)) return handle;
      }

      return null;
    }

    private Port hitTestPort(Vector canvasPos) {
      Port closest = null;
      var closestDistance = float.MaxValue;

      foreach (var port in _mPorts) {
        if (DragMode == DragModes.MovePort && port == _mDragMovePort) continue;

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

    private CompassPoint indicatedDirection(Keys keyCode) {
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

    private static bool isDragButton(MouseEventArgs e) {
      return e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right && ModifierKeys == Keys.Shift;
    }

    private static bool isZoomIn(int delta) {
      return !ApplicationSettingsController.AppSettings.InvertMouseWheel && delta < 0 || ApplicationSettingsController.AppSettings.InvertMouseWheel && delta > 0;
    }

    private static bool isZoomOut(int delta) {
      return !ApplicationSettingsController.AppSettings.InvertMouseWheel && delta > 0 || ApplicationSettingsController.AppSettings.InvertMouseWheel && delta < 0;
    }

    private void joinRoomsToolStripMenuItem_Click(object sender, EventArgs e) {
      JoinSelectedRooms(SelectedRooms.First(), SelectedRooms.Last());
    }

    private void m_downLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Down);
    }

    private void m_inLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.In);
    }

    private void m_outLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Out);
    }

    private void m_plainLinesMenuItem_Click(object sender, EventArgs e) {
      ApplyNewPlainConnectionSettings();
    }

    private void m_reverseLineMenuItem_Click(object sender, EventArgs e) {
      ReverseLineDirection();
    }

    private void m_toggleDirectionalLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.ToggleConnectionFlow(NewConnectionFlow);
    }

    private void m_toggleDottedLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.ToggleConnectionStyle(NewConnectionStyle);
    }

    private void m_upLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Up);
    }

    private void mapSettingsToolStripMenuItem_Click(object sender, EventArgs e) {
      Settings.ShowMapDialog();
      Refresh();
    }

    private static void moveActiveSelected(bool moveForward = true) {
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

    private void moveArrowKeyHandler(Keys keyCode, bool shift) {
      var bHorizontal = keyCode == Keys.Left || keyCode == Keys.Right;
      var bNegative = keyCode == Keys.Right || keyCode == Keys.Down;

      if (SelectedElementCount == 0) {
        if (bHorizontal)
          Origin += new Vector((bNegative ? -1 : 1) * Viewport.Width / (shift ? 5 : 10), 0);
        else
          Origin += new Vector(0, (bNegative ? -1 : 1) * Viewport.Width / (shift ? 5 : 10));
        hideElementToolTip();
      } else {
        var delta = Settings.SnapToGrid ? Settings.GridSize : 2.0f;
        var offset = bHorizontal ? new Vector(bNegative ? delta : -delta, 0) : new Vector(0, bNegative ? delta : -delta);
        moveSelectedElements(offset);
      }
    }

    private void namesToolStripMenuItem_Click(object sender, EventArgs e) {
      SwapRoomNames();
    }

    private void objectsToolStripMenuItem_Click(object sender, EventArgs e) {
      SwapRooms();
    }

    private void octagonalEdgesToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.Octagonal);
    }

    private void onElementAdded(object sender, ItemEventArgs<Element> e) {
      if (e.Item is Room item) {
        var room = item;
        room.Size = _mNewRoomSize;
        room.IsDark = _mNewRoomIsDark;
        room.ObjectsPosition = _mNewRoomObjectsPosition;
      }

      e.Item.Changed += onElementChanged;
      Project.Current.IsDirty = true;
      requestRecomputeSmartSegments();
      Invalidate();
    }

    private void onElementChanged(object sender, EventArgs e) {
      if (sender is Room room) setRoomDefaultsFrom(room);

      if (sender is Connection connection) setConnectionDefaultsFrom(connection);
      Invalidate();
      Project.Current.IsDirty = true;
      requestRecomputeSmartSegments();
    }

    private void onElementRemoved(object sender, ItemEventArgs<Element> e) {
      _mSelectedElements.Remove(e.Item);
      updateSelection();
      endDrag();
      updateDragHover(PointToClient(MousePosition));

      Project.Current.IsDirty = true;
      e.Item.Changed -= onElementChanged;
      requestRecomputeSmartSegments();
      Invalidate();
    }

    private void onPreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
      if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) e.IsInputKey = true;
    }

    private void onProjectChanged(object sender, ProjectChangedEventArgs e) {
      if (e.OldProject != null) {
        e.OldProject.Elements.Added -= onElementAdded;
        e.OldProject.Elements.Removed -= onElementRemoved;

        foreach (var element in e.OldProject.Elements) element.Changed -= onElementChanged;
        e.OldProject.Dispose();
      }

      if (e.NewProject != null) {
        e.NewProject.Elements.Added += onElementAdded;
        e.NewProject.Elements.Removed += onElementRemoved;

        foreach (var element in e.NewProject.Elements) element.Changed += onElementChanged;
      }

      reset();
      ZoomToFit();
    }

    private void onRecomputeTimerTick(object state) {
      _mRecomputeTimer.Change(Timeout.Infinite, Timeout.Infinite);

      var context = new DrawingContext(ZoomFactor);
      var elements = depthSortElements();

      foreach (var element in elements) element.RecomputeSmartLineSegments(context);

      sMSmartLineSegmentsUpToDate = true;
      Invalidate();
    }

    private void onSettingsChanged(object sender, EventArgs e) {
      requestRecomputeSmartSegments();
      if (Settings.WrappingChanged) {
        RedrawAllRoomsWithDashes();
        Settings.WrappingChanged = false; // might as well go at the end of the RedrawAllRoomsWithDashes method
      }
      BackColor = Settings.Color[Colors.Canvas];
      Invalidate();
    }

    private void pasteColors(CopyController.CopyColorsObj xx) {
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

        _mSelectedElements.Clear();
        _mSelectedElements.AddRange(newRooms);
        _mSelectedElements.AddRange(newLabels);
        _mSelectedElements.AddRange(newConnections);
        updateSelection();
      }
    }

    private void raiseNewConnectionFlowChanged() {
      var changed = NewConnectionFlowChanged;
      changed?.Invoke(this, EventArgs.Empty);
    }

    private void raiseNewConnectionLabelChanged() {
      var changed = NewConnectionLabelChanged;
      changed?.Invoke(this, EventArgs.Empty);
    }

    private void raiseNewConnectionStyleChanged() {
      var changed = NewConnectionStyleChanged;
      changed?.Invoke(this, EventArgs.Empty);
    }

    private void recreateHandles() {
      HoverHandle = null;
      _mHandles.Clear();
      var element = SelectedElement;
      if (CanSelectElements && element is ISizeable && HasSingleSelectedElement) {
        var sizeable = (ISizeable) element;
        _mHandles.Add(new ResizeHandle(CompassPoint.North, sizeable));
        _mHandles.Add(new ResizeHandle(CompassPoint.South, sizeable));
        _mHandles.Add(new ResizeHandle(CompassPoint.East, sizeable));
        _mHandles.Add(new ResizeHandle(CompassPoint.West, sizeable));
        _mHandles.Add(new ResizeHandle(CompassPoint.NorthWest, sizeable));
        _mHandles.Add(new ResizeHandle(CompassPoint.NorthEast, sizeable));
        _mHandles.Add(new ResizeHandle(CompassPoint.SouthWest, sizeable));
        _mHandles.Add(new ResizeHandle(CompassPoint.SouthEast, sizeable));
      }

      Invalidate();
    }

    private void recreatePorts() {
      HoverPort = null;
      _mPorts.Clear();

      // decide if we want ports on the element under the mouse cursor; if so, add them
      if ((HoverElement is Room || HoverElement is MapLabel) && !_mSelectedElements.Contains(HoverElement))
        if (DragMode == DragModes.MovePort || CanDrawLine && SelectedElement == null)
          _mPorts.AddRange(HoverElement.PortList);

      // decide if we want movable ports on the selected element; if so, add them
      // (currently movable ports only apply to connections, and if we want to be able
      // to move a connection we must show them.)
      var needMovablePortsOnSelectedElement = CanSelectElements;
      if (needMovablePortsOnSelectedElement && HasSingleSelectedElement)
        if (SelectedElement != null)
          foreach (var port in SelectedElement.PortList.OfType<MoveablePort>())
            _mPorts.Add(port);

      Invalidate();
    }

    // context menu event to change region of room(s)
    private void regionContextClick(object sender, EventArgs e) {
      var selectedRooms = _mSelectedElements.Where(p => p is Room).ToList();

      if (!selectedRooms.Any())
        selectedRooms.Add(_lastSelectedRoom);

      var regionSelected = (ToolStripMenuItem) sender;

      foreach (var selectedRoom in selectedRooms.Cast<Room>()) selectedRoom.Region = regionSelected.Text;
    }

    private void regionsToolStripMenuItem_Click(object sender, EventArgs e) {
      SwapRoomRegions();
    }

    private void renameToolStripMenuItem_Click(object sender, EventArgs e) {
      if (HasSingleSelectedElement) _commandController.ShowElementProperties(SelectedElement);
    }

    private void requestRecomputeSmartSegments() {
      sMSmartLineSegmentsUpToDate = false;
      _mRecomputeTimer.Change(RECOMPUTE_N_MILLISECONDS_AFTER_CHANGE, RECOMPUTE_N_MILLISECONDS_AFTER_CHANGE);
    }

    private void reset() {
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
      _mNewRoomSize = new Vector(Settings.GridSize * 3, Settings.GridSize * 2);
      _mNewRoomIsDark = false;
      _mNewRoomObjectsPosition = CompassPoint.South;
      _mNewRoomStyleSource = null;
      requestRecomputeSmartSegments();
      StopAutomapping();
      // roomTooltip.SetSuperTooltip(this, null);
    }

    private void resizeRoom(Keys keyCode) {
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

    private void roomPropertiesToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.ShowElementProperties(SelectedElement);
    }

    private void roundedEdgesToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetRoomShape(RoomShape.RoundedCorners);
    }

    private void ScrollBar_Scroll(object sender, ScrollEventArgs e) {
      if (_mUpdatingScrollBars) return;

      // the scroll bar will Invalidate() and Update() us; avoid exceptions
      _mDoNotUpdateScrollBarsNextPaint = true;

      var clientDelta = e.NewValue - e.OldValue;
      if (ApplicationSettingsController.AppSettings.InfiniteScrollBounds && e.Type == ScrollEventType.SmallIncrement || e.Type == ScrollEventType.SmallDecrement)
        if (Math.Abs(clientDelta) != m_vScrollBar.SmallChange)
          clientDelta = m_vScrollBar.SmallChange * (e.Type == ScrollEventType.SmallIncrement ? 1 : -1);
      if (clientDelta != 0)
        if (sender == m_vScrollBar)
          Origin += new Vector(ClientToCanvas(new SizeF(0, clientDelta)));
        else
          Origin += new Vector(ClientToCanvas(new SizeF(clientDelta, 0)));
    }

    private bool selectRoomRelativeToSelectedConnection(CompassPoint compassPoint) {
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
    private bool selectRoomRelativeToSelectedRoom(CompassPoint compassPoint) {
      if (SelectedElement is Room element) {
        var room = element;
        var nextRoom = getRoomInApproximateDirectionFromRoom(room, compassPoint);
        if (nextRoom != null) {
          SelectedElement = nextRoom;
          _commandController.MakeVisible(SelectedElement);
          return true;
        }
      }

      return false;
    }

    private void sendToBackToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.SendToBack();
    }

    private void setConnectionDefaultsFrom(Connection connection) {
      NewConnectionFlow = connection.Flow;
      NewConnectionStyle = connection.Style;
    }

    private void setRoomDefaultsFrom(Room room) {
      _mNewRoomSize = room.Size;
      _mNewRoomIsDark = room.IsDark;
      _mNewRoomObjectsPosition = room.ObjectsPosition;
      _mNewRoomStyleSource = room;
    }

    private void shiftArrowHandler(Keys keyCode) {
      if (!HasSingleSelectedElement) return;
      if (SelectedElement.GetType() == typeof(Connection)) {
        ctrlArrowHandler(keyCode);
        return;
      }

      if (SelectedElement.GetType() != typeof(Room)) return;

      var thisRoom = SelectedElement as Room;
      var direction = indicatedDirection(keyCode);

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

    private void startRoomToolStripMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetStartRoom();
    }

    private void updateDragHover(Point mousePosition) {
      _mLastKnownMousePosition = mousePosition;

      var clientPos = new PointF(mousePosition.X, mousePosition.Y);
      var canvasPos = ClientToCanvas(clientPos);

      switch (DragMode) {
        case DragModes.Pan:
          doDragPan(clientPos);
          break;
        case DragModes.MoveElement:
          doDragMoveElement(canvasPos);
          break;
        case DragModes.MoveResizeHandle:
          doDragMoveResizeHandle(canvasPos);
          break;
        case DragModes.MoveWaypoint:
          doDragMoveWaypoint(mousePosition, canvasPos);
          break;
        case DragModes.MovePort:
          HoverElement = hitTestElement(canvasPos, true);
          HoverPort = hitTestPort(canvasPos);
          doDragMovePort(canvasPos);
          break;
        case DragModes.None:
          HoverWaypoint = hitTestWaypoint(canvasPos);
          HoverHandle = hitTestHandle(canvasPos); // set first; it will RecreatePorts() if the value changes
          HoverPort = hitTestPort(canvasPos);
          var hoverElement = hitTestElement(canvasPos, false);
          HoverElement = hoverElement;

          Cursor.Current = hoverElement is Room && ((Room) hoverElement).IsReference && ModifierKeys == Keys.Control ? Cursors.Hand : Cursors.Default;

          if (hoverElement == null || !ApplicationSettingsController.AppSettings.ShowTooltips ||
              !hoverElement.HasTooltip()) {
            hideElementToolTip();
          } else {
            if (trizbortToolTip1.HoverElement == hoverElement) return;
            hideElementToolTip();
            if (hoverElement.GetToolTipHeader() == string.Empty && hoverElement.GetToolTipText() == string.Empty) return;

            trizbortToolTip1.BodyText = hoverElement.GetToolTipText();
            trizbortToolTip1.FooterText = hoverElement.GetToolTipFooter();
            trizbortToolTip1.TitleText = hoverElement.GetToolTipHeader();

            if (hoverElement is Room) {
              trizbortToolTip1.BackColor = Color.LightBlue;
            } else if (hoverElement is Connection) {
              trizbortToolTip1.BackColor = Color.LemonChiffon;
            }

            trizbortToolTip1.HoverElement = hoverElement;
            trizbortToolTip1.SetToolTip(this, string.IsNullOrEmpty(trizbortToolTip1.TitleText)
              ? trizbortToolTip1.BodyText : trizbortToolTip1.TitleText);
          }

          break;
        case DragModes.DrawLine:
          if (new Vector(_mLastMouseDownPosition).Distance(new Vector(mousePosition)) > Settings.DragDistanceToInitiateNewConnection) {
            var startPos = new PointF(_mLastMouseDownPosition.X, _mLastMouseDownPosition.Y);
            beginDrawConnection(ClientToCanvas(startPos));
          }

          break;
        case DragModes.Marquee:
          if (_mDragMarqueeLastPosition != canvasPos) {
            _mDragMarqueeLastPosition = canvasPos;
            Invalidate();
          }

          break;
      }
    }

    private void updateSelection() {
      _mSelectedWaypoint = null;
      HoverWaypoint = null;
      recreateHandles();
      recreatePorts();
      // only if we have a single element selected;
      // otherwise selecting multiple items will cause one to override the others' settings!
      var selectedElement = SelectedElement;
      if (selectedElement is Connection)
        setConnectionDefaultsFrom((Connection) selectedElement);
      else if (selectedElement is Room) setRoomDefaultsFrom((Room) selectedElement);
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