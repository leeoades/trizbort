using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Misc;
using Trizbort.Util;

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
  private Color _connectionColor = Color.Transparent;
  private string _description = string.Empty;
  private Door _door;
  private ConnectionFlow _flow = DefaultFlow;
  private string _name = string.Empty;
  private ConnectionStyle _style = DefaultStyle;
  private string _startText = string.Empty;
  private string _midText = string.Empty;
  private string _endText = string.Empty;

  public Connection()
  {
  }

  public Connection(Project project) : base(project)
  {
    InitEvents();
  }

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
    get { return _endText; }
    set {
      value = value ?? string.Empty;
      if (_endText == value) return;
      _endText = value;
      RaiseChanged();
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
    get { return _midText; }
    set {
      value = value ?? string.Empty;
      if (_midText == value) return;
      _midText = value;
      RaiseChanged();
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
    get { return _startText; }
    set {
      value = value ?? string.Empty;
      if (_startText == value) return;
      _startText = value;
      RaiseChanged();
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

  public BoundList<Vertex> VertexList { get; } = new();

  public bool SupportsCurveWaypoints => VertexList.Count == 2;

  public bool IsDangling =>
    VertexList.Count < 2 || VertexList[0].Port == null || VertexList[VertexList.Count - 1].Port == null;

  public bool HasCurveWaypoints => SupportsCurveWaypoints &&
    (_curveWaypoints[(int)CurveWaypoint.Quarter].HasValue ||
    _curveWaypoints[(int)CurveWaypoint.Middle].HasValue ||
    _curveWaypoints[(int)CurveWaypoint.ThreeQuarter].HasValue);

  public bool CanAddCurveWaypoint(CurveWaypoint waypoint)
  {
    if (!SupportsCurveWaypoints || _curveWaypoints[(int)waypoint].HasValue) return false;
    return waypoint == CurveWaypoint.Middle || HasCurveWaypoints;
  }

  public bool RemoveCurveWaypoint(CurveWaypoint waypoint)
  {
    if (!_curveWaypoints[(int)waypoint].HasValue) return false;
    SetCurveWaypoint(waypoint, null);
    return true;
  }

  public Vector? GetCurveWaypoint(CurveWaypoint waypoint)
  {
    return _curveWaypoints[(int)waypoint];
  }

  public void SetCurveWaypoint(CurveWaypoint waypoint, Vector? position)
  {
    if (position == _curveWaypoints[(int)waypoint]) return;
    _curveWaypoints[(int)waypoint] = position;
    RaiseChanged();
  }

  public void ClearCurveWaypoints()
  {
    if (!HasCurveWaypoints) return;
    for (var i = 0; i < _curveWaypoints.Length; i++) _curveWaypoints[i] = null;
    RaiseChanged();
  }

  public void MoveCurveWaypointsBy(Vector delta)
  {
    if (delta == Vector.Zero || !HasCurveWaypoints) return;
    for (var i = 0; i < _curveWaypoints.Length; i++)
      if (_curveWaypoints[i].HasValue)
        _curveWaypoints[i] = _curveWaypoints[i].Value + delta;
    RaiseChanged();
  }

  public Vector GetCurveWaypointHandlePosition(CurveWaypoint waypoint)
  {
    var existing = _curveWaypoints[(int)waypoint];
    if (existing.HasValue) return existing.Value;

    GetCurveControlPoints(out var points, out var slots, out var before, out var after);
    if (points.Count == 2) return points[0] + (points[1] - points[0]) * 0.5f;

    var span = 0;
    while (span < slots.Count && slots[span] < (int)waypoint) ++span;
    var p0 = span == 0 ? before : points[span - 1];
    var p3 = span + 2 < points.Count ? points[span + 2] : after;
    return CurveGeometry.Evaluate(p0, points[span], points[span + 1], p3, 0.5f);
  }

  public int ConnectedRoomToRotate(bool whichRoom)
  {
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

    return -1;
  }

  public override float Distance(Vector pos, bool includeMargins)
  {
    var minDistance = float.MaxValue;
    foreach (var segment in GetSegments())
      minDistance = Math.Min(minDistance, pos.DistanceFromLineSegment(segment));
    return minDistance;
  }

  public CompassPoint GetSourceCompassPoint()
  {
    GetSourceRoom(out var sourceCompassPoint);
    return sourceCompassPoint;
  }

  public Room GetSourceRoom()
  {
    return GetSourceRoom(out _);
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
        return compassPort.Owner as Room;
      }
    }

    sourceCompassPoint = CompassPoint.North;
    return null;
  }

  public CompassPoint GetTargetCompassPoint()
  {
    GetTargetRoom(out var targetCompassPoint);
    return targetCompassPoint;
  }

  public Room GetTargetRoom()
  {
    return GetTargetRoom(out _);
  }

  public Room GetTargetRoom(out CompassPoint targetCompassPoint)
  {
    if (VertexList.Count > 0)
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

  public override Vector GetPortPosition(Port port)
  {
    var vertexPort = (VertexPort)port;
    return vertexPort.Vertex.Position;
  }

  public override Vector GetPortStalkPosition(Port port)
  {
    return GetPortPosition(port);
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
    var count = whichWay ? 1 : -1;
    var newPoint = (int)dirToChange + count;
    if (newPoint > 15) newPoint = 0;
    if (newPoint < 0) newPoint = 15;
    VertexList[ConnectedRoomToRotate(upperRoom)].Port = connRoom.PortAt((CompassPoint)newPoint);
  }

  public object BeginLoad(XmlElementReader element)
  {
    if (element.Attribute("door").Text == "yes" || element.Attribute("door").ToBool())
      Door = new Door {
        Lockable = element.Attribute("lockable").Text == "yes" || element.Attribute("lockable").ToBool(),
        Locked = element.Attribute("locked").Text == "yes" || element.Attribute("locked").ToBool(),
        Open = element.Attribute("open").Text == "yes" || element.Attribute("open").ToBool(),
        Openable = element.Attribute("openable").Text == "yes" || element.Attribute("openable").ToBool()
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
    StartText = element.Attribute("startText").Text != "" ? element.Attribute("startText").Text : element.Attribute("start").Text;
    MidText = element.Attribute("midText").Text != "" ? element.Attribute("midText").Text : element.Attribute("mid").Text;
    EndText = element.Attribute("endText").Text != "" ? element.Attribute("endText").Text : element.Attribute("end").Text;
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
        VertexList.Add(vertex);
      }

    ZOrder = element.Attribute("ZOrder").ToInt();
    return vertexElementList;
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

  public void Load(XmlElementReader element)
  {
    var state = BeginLoad(element);
    if (state != null) EndLoad(state);
  }

  public void Save(XmlScribe scribe)
  {
    scribe.Attribute("name", Name);
    scribe.Attribute("description", Description);
    if (Door != null)
    {
      scribe.Attribute("door", "yes");
      scribe.Attribute("lockable", Door.Lockable ? "yes" : "no");
      scribe.Attribute("locked", Door.Locked ? "yes" : "no");
      scribe.Attribute("open", Door.Open ? "yes" : "no");
      scribe.Attribute("openable", Door.Openable ? "yes" : "no");
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

    scribe.Attribute("ZOrder", ZOrder);

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

  public List<LineSegment> GetSegments()
  {
    if (HasCurveWaypoints) return GetCurvedSegments(out _);

    var list = new List<LineSegment>();
    for (var index = 0; index < VertexList.Count - 1; ++index)
    {
      var a = VertexList[index];
      var b = VertexList[index + 1];

      var aHasStalk = a.Port != null && a.Port.HasStalk;
      var bHasStalk = b.Port != null && b.Port.HasStalk;

      var aPos = a.Position;
      var aStalkPos = aHasStalk ? a.Port.StalkPosition : aPos;
      var bPos = b.Position;
      var bStalkPos = bHasStalk ? b.Port.StalkPosition : bPos;

      // Same segments as the reference (legacy Connection.GetSegments): stalk, straight line, stalk. An earlier
      // special case through the stalk midpoints added extra arrowheads and bent routes (issue I-025 R2/R7).
      if (aHasStalk)
        list.Add(new LineSegment(aPos, aStalkPos));

      list.Add(new LineSegment(aStalkPos, bStalkPos));

      if (bHasStalk)
        list.Add(new LineSegment(bStalkPos, bPos));
    }

    return list;
  }

  public void GetCurveControlPoints(
    out List<Vector> points,
    out List<int> slots,
    out Vector before,
    out Vector after)
  {
    points = new List<Vector>();
    slots = new List<int>();
    if (VertexList.Count < 2)
    {
      before = Vector.Zero;
      after = Vector.Zero;
      return;
    }

    var startVertex = VertexList[0];
    var endVertex = VertexList[VertexList.Count - 1];
    var startHasStalk = startVertex.Port != null && startVertex.Port.HasStalk;
    var endHasStalk = endVertex.Port != null && endVertex.Port.HasStalk;

    var start = startHasStalk ? startVertex.Port.StalkPosition : startVertex.Position;
    var end = endHasStalk ? endVertex.Port.StalkPosition : endVertex.Position;

    points.Add(start);
    for (var i = 0; i < _curveWaypoints.Length; i++)
      if (_curveWaypoints[i].HasValue)
      {
        points.Add(_curveWaypoints[i].Value);
        slots.Add(i);
      }

    points.Add(end);

    before = startHasStalk ? startVertex.Position : start * 2 - points[1];
    after = endHasStalk ? endVertex.Position : end * 2 - points[points.Count - 2];
  }

  public List<LineSegment> GetCurvedSegments(out List<List<Vector>> spans)
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

public enum ConnectionFlow
{
  TwoWay,
  OneWay
}

public enum ConnectionStyle
{
  Solid,
  Dashed
}

public enum ConnectionLabel
{
  None,
  Up,
  Down,
  In,
  Out
}
