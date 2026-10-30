using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using PdfSharp.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Misc;
using Trizbort.Properties;
using Trizbort.UI;
using Trizbort.Util;
using Settings = Trizbort.Setup.Settings;

namespace Trizbort.Domain.Elements;

/// <summary>
///   A connection between elements or points in space.
/// </summary>
/// <remarks>
///   Connections are multi-segment lines between vertices.
///   Each vertex is fixed either to a point in space or
///   to an element's port.
/// </remarks>
[SuppressMessage("ReSharper", "CanBeReplacedWithTryCastAndCheckForNull")]
public class Connection : Element
{
  public const string Up = "up";
  public const string Down = "down";
  public const string In = "in";
  public const string Out = "out";
  private const ConnectionStyle DefaultStyle = ConnectionStyle.Solid;
  private const ConnectionFlow DefaultFlow = ConnectionFlow.TwoWay;
  private const int CurveSubdivisions = 16;

  private static readonly string[] _curveWaypointAttributeNames =
    { "curveQuarter", "curveMiddle", "curveThreeQuarter" };

  private readonly Vector?[] _curveWaypoints = new Vector?[3];
  private readonly TextBlock _endText = new();
  private readonly TextBlock _midText = new();
  private readonly List<LineSegment> _smartSegments = new();
  private readonly TextBlock _startText = new();
  private Color _connectionColor = Color.Transparent;
  private string _description = string.Empty;
  private Door _door;
  private ConnectionFlow _flow = DefaultFlow;
  private string _name = string.Empty;
  private ConnectionStyle _style = DefaultStyle;

  public Connection()
  {
  }

  public Connection(Project project) : base(project)
  {
    InitEvents();
  }

  // Added this second constructor to be used when loading a room
  // This constructor is significantly faster as it doesn't look for gap in the element IDs
  public Connection(Project project, int totalIDs) : base(project, totalIDs)
  {
    InitEvents();
  }

  public Connection(Project project, Vertex a, Vertex b)
    : this(project)
  {
    VertexList.Add(a);
    VertexList.Add(b);
  }

  // Added to ignore ID gaps
  public Connection(Project project, Vertex a, Vertex b, int totalIDs)
    : this(project, totalIDs)
  {
    VertexList.Add(a);
    VertexList.Add(b);
  }

  public Connection(Project project, Vertex a, Vertex b, params Vertex[] args)
    : this(project, a, b)
  {
    foreach (var vertex in args)
      VertexList.Add(vertex);
  }

  // Added to ignore ID gaps
  public Connection(Project project, Vertex a, Vertex b, int totalIDs, params Vertex[] args)
    : this(project, a, b, totalIDs)
  {
    foreach (var vertex in args)
      VertexList.Add(vertex);
  }

  public Color ConnectionColor {
    get { return _connectionColor; }
    set {
      if (_connectionColor != value)
      {
        _connectionColor = value;
        RaiseChanged();
      }
    }
  }

  public override Depth Depth => Depth.High;

  public string Description {
    get { return _description; }
    set {
      if (_description != value)
      {
        _description = value;
        RaiseChanged();
      }
    }
  }

  public Door Door {
    get { return _door; }
    set {
      if (_door != value)
      {
        _door = value;
        RaiseChanged();
      }
    }
  }

  public string EndText {
    get { return _endText.Text; }
    set {
      if (_endText.Text != value)
      {
        _endText.Text = value;
        RaiseChanged();
      }
    }
  }

  public ConnectionFlow Flow {
    get { return _flow; }
    set {
      if (_flow != value)
      {
        _flow = value;
        RaiseChanged();
      }
    }
  }

  public override bool HasDialog => true;

  public string MidText {
    get { return _midText.Text; }
    set {
      if (_midText.Text != value)
      {
        _midText.Text = value;
        RaiseChanged();
      }
    }
  }

  public override string Name {
    get { return _name; }
    set {
      if (_name != value)
      {
        _name = value;
        RaiseChanged();
      }
    }
  }

  public string StartText {
    get { return _startText.Text; }
    set {
      if (_startText.Text != value)
      {
        _startText.Text = value;
        RaiseChanged();
      }
    }
  }

  public ConnectionStyle Style {
    get { return _style; }
    set {
      if (_style != value)
      {
        _style = value;
        RaiseChanged();
      }
    }
  }

  [JsonIgnore] public BoundList<Vertex> VertexList { get; set; } = new();

  /// <summary>
  ///   Curve waypoints are only supported on simple two-vertex connections.
  /// </summary>
  public bool SupportsCurveWaypoints => VertexList.Count == 2;

  public bool IsDangling =>
    VertexList.Count < 2 || VertexList[0].Port == null || VertexList[VertexList.Count - 1].Port == null;

  public bool HasCurveWaypoints => SupportsCurveWaypoints && _curveWaypoints.Any(w => w.HasValue);

  public Vector? GetCurveWaypoint(CurveWaypoint waypoint)
  {
    return _curveWaypoints[(int)waypoint];
  }

  public void SetCurveWaypoint(CurveWaypoint waypoint, Vector? position)
  {
    if (_curveWaypoints[(int)waypoint] == position) return;
    _curveWaypoints[(int)waypoint] = position;
    RaiseChanged();
  }

  public bool RemoveCurveWaypoint(CurveWaypoint waypoint)
  {
    if (!_curveWaypoints[(int)waypoint].HasValue) return false;
    SetCurveWaypoint(waypoint, null);
    return true;
  }

  public void ClearCurveWaypoints()
  {
    if (!_curveWaypoints.Any(w => w.HasValue)) return;
    for (var i = 0; i < _curveWaypoints.Length; ++i) _curveWaypoints[i] = null;
    RaiseChanged();
  }

  public void MoveCurveWaypointsBy(Vector delta)
  {
    if (delta == Vector.Zero || !_curveWaypoints.Any(w => w.HasValue)) return;
    for (var i = 0; i < _curveWaypoints.Length; ++i)
      if (_curveWaypoints[i].HasValue)
        _curveWaypoints[i] = _curveWaypoints[i].Value + delta;
    RaiseChanged();
  }

  /// <summary>
  ///   The middle waypoint can always be added; the quarter waypoints become available
  ///   once the connection has been bent.
  /// </summary>
  public bool CanAddCurveWaypoint(CurveWaypoint waypoint)
  {
    if (!SupportsCurveWaypoints || _curveWaypoints[(int)waypoint].HasValue) return false;
    return waypoint == CurveWaypoint.Middle || HasCurveWaypoints;
  }

  /// <summary>
  ///   Where to draw the handle for a waypoint: its position if set, otherwise the point on the
  ///   current line/curve at which a new waypoint would be inserted.
  /// </summary>
  public Vector GetCurveWaypointHandlePosition(CurveWaypoint waypoint)
  {
    var existing = _curveWaypoints[(int)waypoint];
    if (existing.HasValue) return existing.Value;

    GetCurveControlPoints(out var points, out var slots, out var before, out var after);
    if (points.Count == 2) return points[0] + (points[1] - points[0]) * 0.5f;

    // find the span this empty slot falls within
    var span = 0;
    while (span < slots.Count && slots[span] < (int)waypoint) ++span;
    var p0 = span == 0 ? before : points[span - 1];
    var p3 = span + 2 < points.Count ? points[span + 2] : after;
    return CurveGeometry.Evaluate(p0, points[span], points[span + 1], p3, 0.5f);
  }

  public object BeginLoad(XmlElementReader element)
  {
    if (element.Attribute("door").Text == "yes")
      Door = new Door {
        Lockable = element.Attribute("lockable").Text == "yes",
        Locked = element.Attribute("locked").Text == "yes",
        Open = element.Attribute("open").Text == "yes",
        Openable = element.Attribute("openable").Text == "yes"
      };

    switch (element.Attribute("style").Text)
    {
      default:
        Style = ConnectionStyle.Solid;
        break;
      case "dashed":
        Style = ConnectionStyle.Dashed;
        break;
    }

    switch (element.Attribute("flow").Text)
    {
      default:
        Flow = ConnectionFlow.TwoWay;
        break;
      case "oneWay":
        Flow = ConnectionFlow.OneWay;
        break;
    }

    Name = element.Attribute("name").Text;
    Description = element.Attribute("description").Text;
    StartText = element.Attribute("startText").Text;
    MidText = element.Attribute("midText").Text;
    EndText = element.Attribute("endText").Text;
    if (element.Attribute("color").Text != "")
      ConnectionColor = ColorTranslator.FromHtml(element.Attribute("color").Text);

    for (var i = 0; i < _curveWaypointAttributeNames.Length; ++i)
      _curveWaypoints[i] = ParseCurveWaypoint(element.Attribute(_curveWaypointAttributeNames[i]).Text);

    var vertexElementList = new List<XmlElementReader>();
    vertexElementList.AddRange(element.Children);
    vertexElementList.Sort((a, b) => a.Attribute("index").ToInt().CompareTo(b.Attribute("index").ToInt()));

    foreach (var vertexElement in vertexElementList)
      if (vertexElement.HasName("point"))
      {
        var vertex = new Vertex
          { Position = new Vector(vertexElement.Attribute("x").ToFloat(), vertexElement.Attribute("y").ToFloat()) };
        VertexList.Add(vertex);
      }
      else if (vertexElement.HasName("dock"))
      {
        var vertex = new Vertex();
        // temporarily leave this vertex as a positional vertex;
        // we can't safely dock it to a port until EndLoad().
        VertexList.Add(vertex);
      }

    return vertexElementList;
  }

  public int ConnectedRoomToRotate(bool whichRoom)
  {
    //first, let's take care of cases where the right room is forced, if there is one
    if (VertexList.Count < 2) return -1;
    var firstRoom = VertexList[0].Port?.Owner as Room;
    var secondRoom = VertexList[1].Port?.Owner as Room;
    if (firstRoom == null && secondRoom == null) return -1;
    if (secondRoom == null) return 0;
    if (firstRoom == null) return 1;

    var firstCenterY = firstRoom.Y + firstRoom.Height / 2;
    var secondCenterY = secondRoom.Y + secondRoom.Height / 2;

    if (firstCenterY < secondCenterY) return whichRoom ? 0 : 1;
    if (firstCenterY > secondCenterY) return whichRoom ? 1 : 0;

    var firstCenterX = firstRoom.Position.X + firstRoom.Height / 2;
    var secondCenterX = secondRoom.Position.X + secondRoom.Height / 2;

    if (firstCenterX < secondCenterX) return whichRoom ? 0 : 1;
    if (firstCenterX > secondCenterX) return whichRoom ? 1 : 0;

    return 1;
  }


  public override float Distance(Vector pos, bool includeMargins)
  {
    var distance = float.MaxValue;
    foreach (var segment in GetSegments())
      distance = Math.Min(distance, pos.DistanceFromLineSegment(segment));
    return distance;
  }

  public override void Draw(XGraphics graphics, Palette palette, DrawingContext context)
  {
    var lineSegments = context.UseSmartLineSegments ? _smartSegments : GetSegments();
    var curved = HasCurveWaypoints;
    var handDrawn = Settings.HandDrawn;
    var random = Sketch.Seeded(Id);
    var chain = new List<PointF>();
    var sketched = new List<PointF[]>();
    var chevrons = new List<(Vector position, Vector direction)>();
    Pen chainPen = null;

    void FlushChain()
    {
      if (chain.Count > 1)
      {
        var stroke = Sketch.Polyline(chain, random);
        sketched.Add(stroke);
        graphics.DrawLines(chainPen, stroke);
      }

      chain.Clear();
    }

    void AddChevron(Vector position, Vector direction)
    {
      if (handDrawn) chevrons.Add((position, direction));
      else DrawChevron(graphics, palette, context, position, direction, null);
    }

    bool ContinuesChain(LineSegment segment)
    {
      if (chain.Count < 2 || chain[chain.Count - 1] != segment.Start.ToPointF()) return false;
      var previous = chain[chain.Count - 2];
      var last = chain[chain.Count - 1];
      var before = Math.Atan2(last.Y - previous.Y, last.X - previous.X);
      var after = Math.Atan2(segment.End.Y - segment.Start.Y, segment.End.X - segment.Start.X);
      var turn = Math.Abs(Math.IEEERemainder(after - before, 2 * Math.PI));
      return turn < Math.PI / 9;
    }

    foreach (var lineSegment in lineSegments)
    {
      var pen = palette.GetLinePen(context.Selected, context.Hover, Style == ConnectionStyle.Dashed);
      Pen specialPen = null;

      if (!context.Hover)
        if (ConnectionColor != Color.Transparent && !context.Selected)
        {
          specialPen = (Pen)pen.Clone();
          specialPen.Color = ConnectionColor;
        }

      if (!ApplicationSettingsController.AppSettings.DebugDisableLineRendering)
      {
        if (!handDrawn)
        {
          graphics.DrawLine(specialPen ?? pen, lineSegment.Start.ToPointF(), lineSegment.End.ToPointF());
        }
        else
        {
          // join collinear stalks and the many tiny pieces of a flattened curve into single strokes,
          // so the wobble flows along the whole line instead of kinking at every joint
          if (!ContinuesChain(lineSegment)) FlushChain();
          chainPen = specialPen ?? pen;
          if (chain.Count == 0) chain.Add(lineSegment.Start.ToPointF());
          chain.Add(lineSegment.End.ToPointF());
        }
      }

      var delta = lineSegment.Delta;
      if (!curved && Flow == ConnectionFlow.OneWay && delta.Length > Settings.ConnectionArrowSize)
        AddChevron(lineSegment.Mid, delta);

      context.LinesDrawn.Add(lineSegment);
    }

    FlushChain();

    if (curved && Flow == ConnectionFlow.OneWay)
    {
      // one arrow per curve span, rather than one per flattened line segment
      GetCurvedSegments(out var spans);
      foreach (var span in spans)
      {
        var mid = CurveGeometry.PolylineMidpoint(span, out var direction);
        if (direction != Vector.Zero) AddChevron(mid, direction);
      }
    }

    if (chevrons.Count > 0)
    {
      // place each arrow on the wobbly stroke it belongs to, rather than on the ideal straight line
      var arrowRandom = Sketch.Seeded(Id + 5000);
      foreach (var (position, direction) in chevrons)
      {
        var target = position.ToPointF();
        var bestPoint = target;
        var arrowDirection = direction;
        var bestDistance = float.MaxValue;
        foreach (var stroke in sketched)
        {
          var point = Sketch.Nearest(stroke, target, out var strokeDirection);
          var distance = (point.X - target.X) * (point.X - target.X) + (point.Y - target.Y) * (point.Y - target.Y);
          if (distance >= bestDistance) continue;
          bestDistance = distance;
          bestPoint = point;
          // follow the stroke's local angle, but keep the arrow pointing the way the connection flows
          var sign = strokeDirection.X * direction.X + strokeDirection.Y * direction.Y >= 0 ? 1 : -1;
          arrowDirection = new Vector(strokeDirection.X * sign, strokeDirection.Y * sign);
        }

        if (arrowDirection == Vector.Zero) arrowDirection = direction;
        DrawChevron(graphics, palette, context, new Vector(bestPoint), arrowDirection, arrowRandom);
      }
    }

    if (_door != null && lineSegments.Count > 0)
      ShowDoorIcons(graphics, lineSegments[0]);

    Annotate(graphics, palette, lineSegments);
  }

  private void DrawChevron(
    XGraphics graphics,
    Palette palette,
    DrawingContext context,
    Vector position,
    Vector direction,
    Random sketch)
  {
    var brush = (SolidBrush)palette.GetLineBrush(context.Selected, context.Hover);
    SolidBrush specialBrush = null;

    if (!context.Hover)
      if (ConnectionColor != Color.Transparent && !context.Selected)
      {
        specialBrush = (SolidBrush)brush.Clone();
        specialBrush.Color = ConnectionColor;
      }

    Drawing.DrawChevron(
      graphics,
      position.ToPointF(),
      (float)(Math.Atan2(direction.Y, direction.X) / Math.PI * 180),
      Settings.ConnectionArrowSize,
      specialBrush ?? brush,
      sketch);
  }

  public void EndLoad(object state)
  {
    var elements = (List<XmlElementReader>)state;
    for (var index = 0; index < elements.Count; ++index)
    {
      var element = elements[index];
      if (element.HasName("dock"))
        if (Project.FindElement(element.Attribute("id").ToInt(), out var target))
        {
          var portId = element.Attribute("port").Text;
          foreach (var port in target.PortList)
            if (StringComparer.InvariantCultureIgnoreCase.Compare(portId, port.Id) == 0)
            {
              var vertex = VertexList[index];
              vertex.Port = port;
              break;
            }
        }
    }
  }

  public override Vector GetPortPosition(Port port)
  {
    var vertexPort = (VertexPort)port;
    return vertexPort.Vertex.Position;
  }

  public override Vector GetPortStalkPosition(Port port)
  {
    return GetPortPosition(port);
  }

  public Room GetSourceRoom()
  {
    return GetSourceRoom(out var t);
  }

  public Room GetSourceRoom(out CompassPoint sourceCompassPoint)
  {
    if (VertexList.Count > 0)
    {
      var port = VertexList[0].Port;
      if (port is Room.CompassPort)
      {
        var compassPort = (Room.CompassPort)port;
        sourceCompassPoint = compassPort.CompassPoint;
        return port.Owner as Room;
      }
    }

    sourceCompassPoint = CompassPoint.North;
    return null;
  }

  public Room GetTargetRoom()
  {
    CompassPoint t;
    return GetTargetRoom(out t);
  }

  public Room GetTargetRoom(out CompassPoint targetCompassPoint)
  {
    if (VertexList.Count > 1)
    {
      var port = VertexList[VertexList.Count - 1].Port;
      if (port is Room.CompassPort)
      {
        var compassPort = (Room.CompassPort)port;
        targetCompassPoint = compassPort.CompassPoint;
        return compassPort.Owner as Room;
      }
    }

    targetCompassPoint = CompassPoint.North;
    return null;
  }

  public static void GetText(ConnectionLabel label, out string start, out string end)
  {
    start = string.Empty;
    end = string.Empty;
    switch (label)
    {
      case ConnectionLabel.None:
        start = string.Empty;
        end = string.Empty;
        break;
      case ConnectionLabel.Up:
        start = Up;
        end = Down;
        break;
      case ConnectionLabel.Down:
        start = Down;
        end = Up;
        break;
      case ConnectionLabel.In:
        start = In;
        end = Out;
        break;
      case ConnectionLabel.Out:
        start = Out;
        end = In;
        break;
    }
  }

  public override Color GetToolTipColor()
  {
    return Door == null ? Color.Yellow : Color.GreenYellow;
  }

  public override string GetToolTipFooter()
  {
    return string.Empty;
  }

  public override string GetToolTipHeader()
  {
    return $"{Name}{(Door != null ? " (Door)" : string.Empty)}";
  }

  public override string GetToolTipText()
  {
    var desc = string.Empty;
    if (ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips)
    {
      desc = $"{Description}";
      var charsToShow = ApplicationSettingsController.AppSettings.ToolTipConnectionDescriptionCharactersToShow;
      if (ApplicationSettingsController.AppSettings.LimitConnectionDescriptionCharactersInTooltip &
          desc.Length >= charsToShow)
      {
        desc = desc.Substring(0, charsToShow);
        desc = desc + "...";
      }
    }

    return desc;
  }

  public override bool HasTooltip()
  {
    return true;
  }

  public override bool Intersects(Rect rect)
  {
    foreach (var segment in GetSegments())
      if (segment.IntersectsWith(rect))
        return true;
    return false;
  }

  public override void RecomputeSmartLineSegments(DrawingContext context)
  {
    _smartSegments.Clear();
    foreach (var lineSegment in GetSegments())
    {
      List<LineSegment> newSegments = null;
      if (Split(lineSegment, context, ref newSegments))
        foreach (var newSegment in newSegments)
          _smartSegments.Add(newSegment);
      else
        _smartSegments.Add(lineSegment);
    }

    foreach (var segment in _smartSegments)
      context.LinesDrawn.Add(segment);
  }

  public void Reverse()
  {
    VertexList.Reverse();
    var quarter = _curveWaypoints[(int)CurveWaypoint.Quarter];
    _curveWaypoints[(int)CurveWaypoint.Quarter] = _curveWaypoints[(int)CurveWaypoint.ThreeQuarter];
    _curveWaypoints[(int)CurveWaypoint.ThreeQuarter] = quarter;
    RaiseChanged();
  }

  public void RotateConnector(bool upperRoom, bool whichWay)
  {
    var upEnd = ConnectedRoomToRotate(upperRoom);
    if (upEnd == -1) return;
    var pointToChange = (Room.CompassPort)VertexList[ConnectedRoomToRotate(upperRoom)].Port;
    var connRoom = (Room)pointToChange.Owner;
    var dirToChange = pointToChange.CompassPoint;
    var startDir = dirToChange;
    do
    {
      if (whichWay)
        dirToChange--;
      else
        dirToChange++;
      if (dirToChange < CompassPoint.Min) dirToChange = CompassPoint.Max;
      if (dirToChange > CompassPoint.Max) dirToChange = CompassPoint.Min;
    } while (dirToChange != startDir && connRoom.GetConnections(dirToChange).Count > 0);

    if (startDir == dirToChange)
    {
      UserInteraction.ShowMessage(
        $"There are no free ports in room {connRoom.Name}",
        "Connector rotate failed",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
      return;
    }

    if (VertexList[upEnd].Port != connRoom.PortList[(int)dirToChange])
    {
      //this should always be different, but just in case...
      VertexList[upEnd].Port = connRoom.PortList[(int)dirToChange];
      RaiseChanged();
    }
  }

  public void Save(XmlScribe scribe)
  {
    scribe.Attribute("name", Name);
    scribe.Attribute("description", Description);
    if (Door != null)
    {
      scribe.Attribute("door", true);
      scribe.Attribute("lockable", _door.Lockable);
      scribe.Attribute("openable", _door.Openable);
      scribe.Attribute("locked", _door.Locked);
      scribe.Attribute("open", _door.Open);
    }

    if (ConnectionColor != Color.Transparent)
      scribe.Attribute("color", Colors.SaveColor(ConnectionColor));


    if (Style != DefaultStyle)
      switch (Style)
      {
        case ConnectionStyle.Solid:
          scribe.Attribute("style", "solid");
          break;
        case ConnectionStyle.Dashed:
          scribe.Attribute("style", "dashed");
          break;
      }

    if (Flow != DefaultFlow)
      switch (Flow)
      {
        case ConnectionFlow.OneWay:
          scribe.Attribute("flow", "oneWay");
          break;
        case ConnectionFlow.TwoWay:
          scribe.Attribute("flow", "twoWay");
          break;
      }

    if (!string.IsNullOrEmpty(StartText))
      scribe.Attribute("startText", StartText);
    if (!string.IsNullOrEmpty(MidText))
      scribe.Attribute("midText", MidText);
    if (!string.IsNullOrEmpty(EndText))
      scribe.Attribute("endText", EndText);

    if (HasCurveWaypoints)
      for (var i = 0; i < _curveWaypointAttributeNames.Length; ++i)
        if (_curveWaypoints[i].HasValue)
          scribe.Attribute(_curveWaypointAttributeNames[i], FormatCurveWaypoint(_curveWaypoints[i].Value));

    var index = 0;
    foreach (var vertex in VertexList)
    {
      if (vertex.Port != null)
      {
        scribe.StartElement("dock");
        scribe.Attribute("index", index);
        scribe.Attribute("id", vertex.Port.Owner.Id);
        scribe.Attribute("port", vertex.Port.Id);
        scribe.EndElement();
      }
      else
      {
        scribe.StartElement("point");
        scribe.Attribute("index", index);
        scribe.Attribute("x", vertex.Position.X);
        scribe.Attribute("y", vertex.Position.Y);
        scribe.EndElement();
      }

      ++index;
    }
  }

  public void SetText(string start, string mid, string end)
  {
    StartText = start;
    MidText = mid ?? MidText;
    EndText = end;
  }

  public void SetText(ConnectionLabel label)
  {
    GetText(label, out var start, out var end);
    SetText(start, null, end);
  }

  public override void ShowDialog()
  {
    using var dialog = new ConnectionPropertiesDialog();
    dialog.ConnectionName = Name;
    dialog.ConnectionDescription = Description;
    dialog.IsDotted = Style == ConnectionStyle.Dashed;
    dialog.IsDirectional = Flow == ConnectionFlow.OneWay;
    dialog.StartText = StartText;
    dialog.MidText = MidText;
    dialog.EndText = EndText;
    dialog.ConnectionColor = ConnectionColor;
    dialog.Door = Door;
    if (UserInteraction.ShowDialog(dialog, TrizbortApplication.MainForm?.Canvas) == DialogResult.OK)
    {
      Name = dialog.ConnectionName;
      Description = dialog.ConnectionDescription;
      Style = dialog.IsDotted ? ConnectionStyle.Dashed : ConnectionStyle.Solid;
      Flow = dialog.IsDirectional ? ConnectionFlow.OneWay : ConnectionFlow.TwoWay;
      ConnectionColor = dialog.ConnectionColor;
      StartText = dialog.StartText;
      MidText = dialog.MidText;
      EndText = dialog.EndText;
      Door = dialog.Door;
    }
  }


  public override Rect UnionBoundsWith(Rect rect, bool includeMargins)
  {
    foreach (var vertex in VertexList)
      rect = rect.Union(vertex.Position);

    if (HasCurveWaypoints)
      foreach (var segment in GetCurvedSegments(out _))
      {
        rect = rect.Union(segment.Start);
        rect = rect.Union(segment.End);
      }

    return rect;
  }

  private void Annotate(XGraphics graphics, Palette palette, List<LineSegment> lineSegments)
  {
    if (lineSegments.Count == 0)
      return;

    if (!string.IsNullOrEmpty(StartText))
      Annotate(graphics, palette, lineSegments[0], _startText, StringAlignment.Near);

    if (!string.IsNullOrEmpty(EndText))
      Annotate(graphics, palette, lineSegments[lineSegments.Count - 1], _endText, StringAlignment.Far);

    if (!string.IsNullOrEmpty(MidText))
    {
      var totalLength = lineSegments.Sum(lineSegment => lineSegment.Length);
      var middle = totalLength / 2;
      foreach (var lineSegment in lineSegments)
      {
        var length = lineSegment.Length;
        if (middle > length)
        {
          middle -= length;
        }
        else
        {
          middle /= length;
          var pos = lineSegment.Start + lineSegment.Delta * middle;
          var fakeSegment = new LineSegment(
            pos - lineSegment.Delta * Numeric.Small,
            pos + lineSegment.Delta * Numeric.Small);
          Annotate(graphics, palette, fakeSegment, _midText, StringAlignment.Center);
          break;
        }
      }
    }
  }

  private void Annotate(
    XGraphics graphics,
    Palette palette,
    LineSegment lineSegment,
    TextBlock text,
    StringAlignment alignment)
  {
    Vector point;
    var delta = lineSegment.Delta;
    var roomTypeAdjustments = Vector.Zero;

    RoomShape roomType;
    switch (alignment)
    {
      default:
        // detached vertex has no port, so need to check if it exists
        if (VertexList[0].Port != null)
        {
          roomType = VertexList[0].Port.Owner.GetRoomType();
          if (roomType == RoomShape.Ellipse || roomType == RoomShape.Octagonal)
            roomTypeAdjustments = RoomTypeAdjustments(VertexList[0]);
        }

        point = lineSegment.Start + roomTypeAdjustments;
        delta.Negate();
        break;
      case StringAlignment.Center:
        point = lineSegment.Mid;
        break;
      case StringAlignment.Far:
        // detached vertex has no port, so need to check if it exists
        if (VertexList[1].Port != null)
        {
          roomType = VertexList[1].Port.Owner.GetRoomType();
          if (roomType == RoomShape.Ellipse || roomType == RoomShape.Octagonal)
            roomTypeAdjustments = RoomTypeAdjustments(VertexList[1]);
        }

        point = lineSegment.End + roomTypeAdjustments;
        break;
    }

    var bounds = new Rect(point, Vector.Zero);
    bounds.Inflate(Settings.TextOffsetFromConnection);

    var compassPoint = CompassPointHelper.DirectionFromAngle(out var angle, delta);

    var pos = bounds.GetCorner(compassPoint);
    var format = new XStringFormat();
    Drawing.SetAlignmentFromCardinalOrOrdinalDirection(format, compassPoint);

    if (alignment == StringAlignment.Center && Math.Abs(angle) == 90)
    {
      // HACK: Adjust the anchoring for mid-line text for vertical lines to push
      // the start of the label a bit off the connection line.
      pos = bounds.GetCorner(CompassPoint.East);
      format.LineAlignment = XLineAlignment.Center;
    }

    if (alignment == StringAlignment.Center && Numeric.InRange(angle, -10, 10))
    {
      // HACK: if the line segment is pretty horizontal and we're drawing mid-line text,
      // move text below the line to get it out of the way of any labels at the ends,
      // and center the text so it fits onto a line between two proximal rooms.
      pos = bounds.GetCorner(CompassPoint.South);
      format.Alignment = XStringAlignment.Center;
      format.LineAlignment = XLineAlignment.Near;
    }


    if (!ApplicationSettingsController.AppSettings.DebugDisableTextRendering)
      text.Draw(graphics, Settings.LineFont, palette.LineTextBrush, pos, Vector.Zero, format);
  }

  private List<LineSegment> GetSegments()
  {
    if (HasCurveWaypoints) return GetCurvedSegments(out _);

    var list = new List<LineSegment>();
    if (VertexList.Count > 0)
    {
      var first = VertexList[0];

      var index = 0;
      var a = VertexList[index++].Position;

      if (first.Port != null && first.Port.HasStalk)
      {
        var stalkPos = first.Port.StalkPosition;
        list.Add(new LineSegment(a, stalkPos));
        a = stalkPos;
      }

      while (index < VertexList.Count)
      {
        var v = VertexList[index++];
        var b = v.Position;

        if (index == VertexList.Count && v.Port != null && v.Port.HasStalk)
        {
          var stalkPos = v.Port.StalkPosition;
          list.Add(new LineSegment(a, stalkPos));
          a = stalkPos;
        }

        list.Add(new LineSegment(a, b));
        a = b;
      }
    }

    return list;
  }

  /// <summary>
  ///   Build the control points the curve passes through: the start anchor, any waypoints in
  ///   order, then the end anchor. Anchors are the port stalk ends where stalks exist, and the
  ///   phantom points make the curve leave/enter along the stalk direction.
  /// </summary>
  private void GetCurveControlPoints(out List<Vector> points, out List<int> slots, out Vector before, out Vector after)
  {
    var startVertex = VertexList[0];
    var endVertex = VertexList[VertexList.Count - 1];
    var start = startVertex.Position;
    var end = endVertex.Position;
    var startHasStalk = startVertex.Port != null && startVertex.Port.HasStalk;
    var endHasStalk = endVertex.Port != null && endVertex.Port.HasStalk;
    if (startHasStalk) start = startVertex.Port.StalkPosition;
    if (endHasStalk) end = endVertex.Port.StalkPosition;

    points = new List<Vector> { start };
    slots = new List<int>();
    for (var i = 0; i < _curveWaypoints.Length; ++i)
      if (_curveWaypoints[i].HasValue)
      {
        points.Add(_curveWaypoints[i].Value);
        slots.Add(i);
      }

    points.Add(end);

    before = startHasStalk ? startVertex.Position : start * 2 - points[1];
    after = endHasStalk ? endVertex.Position : end * 2 - points[points.Count - 2];
  }

  private List<LineSegment> GetCurvedSegments(out List<List<Vector>> spans)
  {
    var list = new List<LineSegment>();
    GetCurveControlPoints(out var points, out _, out var before, out var after);
    var startVertex = VertexList[0];
    var endVertex = VertexList[VertexList.Count - 1];

    if (startVertex.Port != null && startVertex.Port.HasStalk)
      list.Add(new LineSegment(startVertex.Position, points[0]));

    spans = CurveGeometry.Flatten(points, before, after, CurveSubdivisions);
    foreach (var span in spans)
      for (var i = 1; i < span.Count; ++i)
        if (span[i] != span[i - 1])
          list.Add(new LineSegment(span[i - 1], span[i]));

    if (endVertex.Port != null && endVertex.Port.HasStalk)
      list.Add(new LineSegment(points[points.Count - 1], endVertex.Position));

    return list;
  }

  private static Vector? ParseCurveWaypoint(string text)
  {
    if (string.IsNullOrEmpty(text)) return null;
    var parts = text.Split(',');
    if (parts.Length != 2) return null;
    if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) return null;
    if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) return null;
    return new Vector(x, y);
  }

  private static string FormatCurveWaypoint(Vector position)
  {
    return string.Format(CultureInfo.InvariantCulture, "{0},{1}", position.X, position.Y);
  }

  private void InitEvents()
  {
    VertexList.Added += OnVertexAdded;
    VertexList.Removed += OnVertexRemoved;
  }

  private void OnVertexAdded(object sender, ItemEventArgs<Vertex> e)
  {
    e.Item.Connection = this;
    e.Item.Changed += OnVertexChanged;
    PortList.Add(new VertexPort(e.Item, this));
  }

  private void OnVertexChanged(object sender, EventArgs e)
  {
    RaiseChanged();
  }

  private void OnVertexRemoved(object sender, ItemEventArgs<Vertex> e)
  {
    e.Item.Connection = null;
    e.Item.Changed -= OnVertexChanged;
    foreach (var port1 in PortList)
    {
      var port = (VertexPort)port1;
      if (port.Vertex == e.Item)
      {
        PortList.Remove(port);
        break;
      }
    }
  }

  private Vector RoomTypeAdjustments(Vertex vertex)
  {
    var roomTypeAdjustments = Vector.Zero;
    CompassPointHelper.FromName(vertex.Port.Id, out var dir);
    switch (dir)
    {
      case CompassPoint.SouthEast:
        roomTypeAdjustments = new Vector(8, 6);
        break;
      case CompassPoint.NorthEast:
        roomTypeAdjustments = new Vector(10, -6);
        break;
      case CompassPoint.EastNorthEast:
        roomTypeAdjustments = new Vector(4, 0);
        break;
      case CompassPoint.EastSouthEast:
        roomTypeAdjustments = new Vector(4, 0);
        break;
      case CompassPoint.NorthWest:
        roomTypeAdjustments = new Vector(-10, -4);
        break;
      case CompassPoint.SouthWest:
        roomTypeAdjustments = new Vector(-10, 4);
        break;
      case CompassPoint.WestSouthWest:
        roomTypeAdjustments = new Vector(-10, 0);
        break;
      case CompassPoint.WestNorthWest:
        roomTypeAdjustments = new Vector(-10, -4);
        break;
    }

    return roomTypeAdjustments;
  }

  private void ShowDoorIcons(XGraphics graphics, LineSegment lineSegment)
  {
    var doorIcon = _door.Open ? new Bitmap(Resources.Door_Open) : new Bitmap(Resources.Door);
    var doorLock = _door.Locked ? new Bitmap(Resources.Lock) : new Bitmap(Resources.Unlocked);
    lineSegment.IconBlock1.Image = doorIcon;
    lineSegment.IconBlock2.Image = doorLock;

    lineSegment.DrawIcons(graphics);
  }

  /// <summary>
  ///   Split the given line segment if it crosses line segments we've already drawn.
  /// </summary>
  /// <param name="lineSegment">The line segment to consider.</param>
  /// <param name="context">The context in which we've been drawing line segments.</param>
  /// <param name="newSegments">
  ///   The results of splitting the given line segment, if any. Call with a reference to a null
  ///   list.
  /// </param>
  /// <returns>True if the line segment was split and newSegments now exists and contains line segments; false otherwise.</returns>
  private bool Split(LineSegment lineSegment, DrawingContext context, ref List<LineSegment> newSegments)
  {
    foreach (var previousSegment in context.LinesDrawn)
    {
      var amount = Math.Max(1, Settings.LineWidth) * 3;
      if (lineSegment.Intersect(previousSegment, true, out var intersects))
        foreach (var intersect in intersects)
        {
          switch (intersect.Type)
          {
            case LineSegmentIntersectType.MidPointA:
              var one = new LineSegment(lineSegment.Start, intersect.Position);
              if (one.Shorten(amount))
                if (!Split(one, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(one);
                }

              var two = new LineSegment(intersect.Position, lineSegment.End);
              if (two.Forshorten(amount))
                if (!Split(two, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(two);
                }

              break;

            case LineSegmentIntersectType.StartA:
              if (lineSegment.Forshorten(amount))
                if (!Split(lineSegment, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(lineSegment);
                }

              break;

            case LineSegmentIntersectType.EndA:
              if (lineSegment.Shorten(amount))
                if (!Split(lineSegment, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(lineSegment);
                }

              break;
          }

          // don't check other intersects;
          // we've already split this line, and tested the parts for further intersects.
          return newSegments != null;
        }
    }

    return false;
  }

  public class VertexPort : MoveablePort
  {
    public VertexPort(Vertex vertex, Connection connection) : base(connection)
    {
      Vertex = vertex;
      Connection = connection;
    }

    public Connection Connection { get; }

    public override Port DockedAt => Vertex.Port;

    public override string Id => Connection.VertexList.IndexOf(Vertex).ToString(CultureInfo.InvariantCulture);

    public Vertex Vertex { get; }

    public override void DockAt(Port port)
    {
      Vertex.Port = port;
      Connection.RaiseChanged();
    }

    public override void SetPosition(Vector pos)
    {
      Vertex.Position = pos;
      Connection.RaiseChanged();
    }
  }
}

/// <summary>
///   The waypoint slots through which a connection can be bent into a curve.
/// </summary>
public enum CurveWaypoint
{
  Quarter = 0,
  Middle = 1,
  ThreeQuarter = 2
}

/// <summary>
///   The visual style of a connection.
/// </summary>
public enum ConnectionStyle
{
  Solid,
  Dashed
}

/// <summary>
///   The direction in which a connection flows.
/// </summary>
public enum ConnectionFlow
{
  TwoWay,
  OneWay
}

/// <summary>
///   The style of label to display on a line.
///   This is a simple set of defaults; lines may have entirely custom labels.
/// </summary>
public enum ConnectionLabel
{
  None,
  Up,
  Down,
  In,
  Out
}