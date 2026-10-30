using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Tests;

[TestFixture]
[Category("Unit")]
public class ProjectRegressionTests : IsolatedProjectTests
{
  [Test]
  public void IDs_UseFirstPositiveGap_AndRejectExistingIDs()
  {
    var first = AddRoom("First");
    var second = AddRoom("Second");
    first.Id.ShouldBe(1);
    second.Id.ShouldBe(2);
    second.Id = first.Id;
    second.Id.ShouldBe(2);
    Project.Current.Elements.Remove(first);
    var replacement = AddRoom("Replacement");
    replacement.Id.ShouldBe(1);
    Project.Current.FindElement(1, out var found).ShouldBeTrue();
    found.ShouldBeSameAs(replacement);
    Project.Current.FindElement(99, out found).ShouldBeFalse();
    found.ShouldBeNull();
  }

  [TestCase(false)]
  [TestCase(true)]
  public void RemovingNode_DeletesAllIncidentConnections_ButPreservesOthers(bool labelNode)
  {
    var first = AddRoom("First");
    var second = AddRoom("Second");
    Element node = labelNode ? new MapLabel(Project.Current) : new Room(Project.Current);
    Project.Current.Elements.Add(node);
    var incident = new Connection(
      Project.Current,
      new Vertex(first.PortAt(CompassPoint.East)),
      new Vertex(node.PortList[0]));
    Project.Current.Elements.Add(incident);
    var loop = new Connection(Project.Current, new Vertex(node.PortList[0]), new Vertex(node.PortList[1]));
    Project.Current.Elements.Add(loop);
    var kept = Connect(first, second);
    Project.Current.Elements.Remove(node);
    Project.Current.Elements.OfType<Connection>().ShouldBe(new[] { kept });
  }

  [Test]
  public void DockedVertex_FollowsRoomMovementAndResize_AndCanBecomeFree()
  {
    var room = AddRoom("Room");
    room.Position = new Vector(10, 20);
    room.Size = new Vector(100, 60);
    var vertex = new Vertex(room.PortAt(CompassPoint.East));
    vertex.Position.ShouldBe(new Vector(110, 50));
    room.Position += new Vector(-5, 10);
    room.Size = new Vector(80, 40);
    vertex.Position.ShouldBe(new Vector(85, 50));
    vertex.Position = new Vector(200, 100);
    vertex.Port.ShouldBeNull();
    room.Position += new Vector(10, 10);
    vertex.Position.ShouldBe(new Vector(200, 100));
  }

  [Test]
  public void ConnectionReverse_SwapsEndpoints_AndPreservesDirectionalLabelsAndDoor()
  {
    var first = AddRoom("First");
    var second = AddRoom("Second");
    var line = Connect(first, second);
    line.StartText = "up";
    line.EndText = "down";
    line.MidText = "bridge";
    line.Flow = ConnectionFlow.OneWay;
    var door = new Door { Locked = true, Lockable = true };
    line.Door = door;
    line.Reverse();
    line.GetSourceRoom().ShouldBeSameAs(second);
    line.GetTargetRoom().ShouldBeSameAs(first);
    line.StartText.ShouldBe("up");
    line.EndText.ShouldBe("down");
    line.MidText.ShouldBe("bridge");
    line.Flow.ShouldBe(ConnectionFlow.OneWay);
    line.Door.ShouldBeSameAs(door);
  }

  [Test]
  public void ReferenceRoom_ResolvesLiveGraph_AndStopsResolvingAfterDeletion()
  {
    var original = AddRoom("Original");
    var reference = AddRoom("Alias");
    reference.ReferenceRoomId = original.Id;
    reference.ReferenceRoom.ShouldBeSameAs(original);
    reference.IsReference.ShouldBeTrue();
    Project.Current.Elements.Remove(original);
    reference.ReferenceRoom.ShouldBeNull();
    reference.IsReference.ShouldBeFalse();
  }

  [Test]
  public void Validation_ReportsCorrectRuleTypes_AndRechecksWithoutStaleFailures()
  {
    var room = AddRoom("Same");
    AddRoom("Same");
    Project.Current.MustHaveDescription = true;
    Project.Current.MustHaveUniqueNames = true;
    Project.Current.MustHaveSubtitle = true;
    Project.Current.MustHaveNoDanglingConnectors = true;
    var dangling = new Connection(
      Project.Current,
      new Vertex(room.PortAt(CompassPoint.North)),
      new Vertex(new Vector(0, -100)));
    Project.Current.Elements.Add(dangling);
    room.CheckValidation();
    room.ValidationState.Select(state => state.Type).OrderBy(type => type)
        .ShouldBe(
          new[] {
            ValidationType.RoomUniqueName, ValidationType.RoomDescription, ValidationType.RoomSubTitle,
            ValidationType.RoomDanglingConnection
          });
    room.Name = "Unique";
    room.AddDescription("Description");
    room.SubTitle = "Subtitle";
    Project.Current.Elements.Remove(dangling);
    room.CheckValidation();
    room.ValidationState.ShouldBeEmpty();
  }

  internal static Room AddRoom(string name)
  {
    var room = new Room(Project.Current) { Name = name };
    Project.Current.Elements.Add(room);
    return room;
  }

  internal static Connection Connect(Room first, Room second)
  {
    var connection = new Connection(
      Project.Current,
      new Vertex(first.PortAt(CompassPoint.East)),
      new Vertex(second.PortAt(CompassPoint.West)));
    Project.Current.Elements.Add(connection);
    return connection;
  }
}