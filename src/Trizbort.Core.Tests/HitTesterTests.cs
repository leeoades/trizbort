using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class HitTesterTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private HitTester _hit;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _hit = new HitTester(_session);
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private Room AddRoom(float x, float y, float size = 64)
  {
    var room = new Room(_project) { Position = new Vector(x, y), Size = new Vector(size, size) };
    _project.Elements.Add(room);
    return room;
  }

  private Connection AddConnection(float x1, float y1, float x2, float y2)
  {
    var connection = new Connection(_project, new Vertex(new Vector(x1, y1)), new Vertex(new Vector(x2, y2)));
    _project.Elements.Add(connection);
    return connection;
  }

  private void Select(params Element[] elements)
  {
    _session.Selection.Set(elements);
  }

  [Test]
  public void HitElement_OverlappingRooms_ReturnsHigherZOrder()
  {
    var low = AddRoom(0, 0);
    var high = AddRoom(32, 0);
    low.ZOrder = 5;
    high.ZOrder = 1;

    _hit.HitElement(new Vector(40, 32)).ShouldBe(low);
  }

  [Test]
  public void HitElement_EqualZOrder_ReturnsLaterInList()
  {
    AddRoom(0, 0);
    var later = AddRoom(32, 0);

    _hit.HitElement(new Vector(40, 32)).ShouldBe(later);
  }

  [Test]
  public void HitElement_RoomUnderPointer_BeatsNearbyConnection()
  {
    // A connection drawn above rooms but a few pixels away must not steal the click from the room
    // the pointer is inside (reference: nearest wins, drawing order only breaks ties).
    var room = AddRoom(0, 0);
    AddConnection(-50, 70, 200, 70);

    _hit.HitElement(new Vector(32, 62)).ShouldBe(room);
  }

  [Test]
  public void HitElement_FarPoint_ReturnsNull()
  {
    AddRoom(0, 0);

    _hit.HitElement(new Vector(1000, 1000)).ShouldBeNull();
  }

  [Test]
  public void HitElement_Label_Hit()
  {
    var label = new MapLabel(_project) { Position = new Vector(200, 200), Size = new Vector(80, 30) };
    _project.Elements.Add(label);

    _hit.HitElement(new Vector(220, 210)).ShouldBe(label);
  }

  [Test]
  public void HitElement_Connection_NearLine_Hit()
  {
    var connection = AddConnection(0, 500, 200, 500);

    _hit.HitElement(new Vector(100, 508)).ShouldBe(connection);
    _hit.HitElement(new Vector(100, 560)).ShouldBeNull();
  }

  [Test]
  public void HitHandle_OnlyWhenSingleSizeableSelected()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var nwCenter = new Vector(0, 0);

    _hit.ActiveHandles().ShouldBeEmpty();
    _hit.HitHandle(nwCenter).ShouldBeNull();

    Select(a, b);
    _hit.ActiveHandles().ShouldBeEmpty();
    _hit.HitHandle(nwCenter).ShouldBeNull();

    Select(a);
    _hit.ActiveHandles().Count.ShouldBe(8);
    _hit.HitHandle(nwCenter).ShouldNotBeNull();
  }

  [Test]
  public void HitHandle_CornerAndEdgePositions()
  {
    var room = AddRoom(0, 0);
    Select(room);

    _hit.HitHandle(new Vector(0, 0)).CompassPoint.ShouldBe(CompassPoint.NorthWest);
    _hit.HitHandle(new Vector(32, 0)).CompassPoint.ShouldBe(CompassPoint.North);
    _hit.HitHandle(new Vector(64, 0)).CompassPoint.ShouldBe(CompassPoint.NorthEast);
    _hit.HitHandle(new Vector(64, 32)).CompassPoint.ShouldBe(CompassPoint.East);
    _hit.HitHandle(new Vector(64, 64)).CompassPoint.ShouldBe(CompassPoint.SouthEast);
    _hit.HitHandle(new Vector(32, 64)).CompassPoint.ShouldBe(CompassPoint.South);
    _hit.HitHandle(new Vector(0, 64)).CompassPoint.ShouldBe(CompassPoint.SouthWest);
    _hit.HitHandle(new Vector(0, 32)).CompassPoint.ShouldBe(CompassPoint.West);
    _hit.HitHandle(new Vector(32, 32)).ShouldBeNull();
  }

  [Test]
  public void HitPort_HoveredRoomPorts()
  {
    var room = AddRoom(0, 0);
    var north = room.PortAt(CompassPoint.North);

    _hit.ActivePorts(room).ShouldNotBeEmpty();
    _hit.HitPort(north.Position, room).ShouldBe(north);
    _hit.HitPort(north.Position, null).ShouldBeNull();

    Select(room);
    _hit.ActivePorts(room).ShouldBeEmpty();
  }

  [Test]
  public void HitPort_MovablePortOfSelectedConnection()
  {
    var connection = AddConnection(0, 500, 200, 500);
    var port = connection.PortList.OfType<MoveablePort>().First();

    _hit.HitPort(port.Position).ShouldBeNull();

    Select(connection);
    _hit.ActivePorts(null).ShouldContain(port);
    _hit.HitPort(port.Position).ShouldBe(port);
  }

  [Test]
  public void HitWaypoint_OnlySingleSelectedConnection()
  {
    var connection = AddConnection(0, 500, 100, 500);
    var other = AddRoom(1000, 1000);
    var middle = new Vector(50, 500);

    _hit.HitWaypoint(middle).ShouldBeNull();

    Select(connection);
    _hit.HitWaypoint(middle).ShouldBe(CurveWaypoint.Middle);

    Select(connection, other);
    _hit.HitWaypoint(middle).ShouldBeNull();
  }

  [Test]
  public void HitAll_PrefersWaypointOverHandleOverPortOverElement()
  {
    // HitAll reports every kind found under the point; the documented press priority is
    // waypoint, handle, port, element. Each scenario below has the higher kinds and the element together.
    var connection = AddConnection(0, 500, 100, 500);
    Select(connection);
    var onWaypoint = _hit.HitAll(new Vector(50, 500));
    onWaypoint.Waypoint.ShouldBe(CurveWaypoint.Middle);
    onWaypoint.Element.ShouldBe(connection);

    var room = AddRoom(2000, 2000);
    Select(room);
    var onHandle = _hit.HitAll(new Vector(2000, 2000));
    onHandle.Handle.ShouldNotBeNull();
    onHandle.Waypoint.ShouldBeNull();
    onHandle.Element.ShouldBe(room);

    Select();
    var north = room.PortAt(CompassPoint.North);
    var onPort = _hit.HitAll(north.Position, room);
    onPort.Port.ShouldBe(north);
    onPort.Handle.ShouldBeNull();
    onPort.Element.ShouldBe(room);
  }

  [Test]
  public void HitRect_ReturnsIntersectingElements()
  {
    var inside = AddRoom(0, 0);
    AddRoom(1000, 1000);

    var hits = _hit.HitRect(new Rect(-10, -10, 50, 50));

    hits.Count.ShouldBe(1);
    hits[0].ShouldBe(inside);
  }
}
