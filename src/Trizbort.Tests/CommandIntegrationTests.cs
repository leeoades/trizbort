using System.Drawing;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Cache;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.UI.Controls;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Tests;

[TestFixture]
[Category("Integration")]
public class CommandIntegrationTests : IsolatedProjectTests {
  [TestCase(SelectTypes.All, 6)]
  [TestCase(SelectTypes.Rooms, 3)]
  [TestCase(SelectTypes.Connections, 2)]
  [TestCase(SelectTypes.UnconnectedRooms, 1)]
  [TestCase(SelectTypes.DanglingConnections, 1)]
  [TestCase(SelectTypes.SelfLoopingConnections, 0)]
  [TestCase(SelectTypes.RoomsWithObjects, 1)]
  [TestCase(SelectTypes.RoomsWithOutObjects, 2)]
  [TestCase(SelectTypes.None, 0)]
  public void SelectionCommands_FilterMixedGraph(SelectTypes type, int count)
  {
    using var canvas = new Canvas();
    var first = ProjectRegressionTests.AddRoom("First");
    first.Objects = "Key";
    var second = ProjectRegressionTests.AddRoom("Second");
    ProjectRegressionTests.AddRoom("Isolated");
    ProjectRegressionTests.Connect(first, second);
    Project.Current.Elements.Add(new MapLabel(Project.Current));
    Project.Current.Elements.Add(
      new Connection(Project.Current, new Vertex(second.PortAt(CompassPoint.North)), new Vertex(new Vector(10, -100))));
    new CommandController(canvas).Select(type);
    canvas.SelectedElements.Count.ShouldBe(count);
    if (type == SelectTypes.Rooms) canvas.SelectedElements.All(element => element is Room).ShouldBeTrue();
  }

  [Test]
  public void Commands_UpdateOnlySelectedElementsAndCanvasDefaults()
  {
    using var canvas = new Canvas();
    var first = ProjectRegressionTests.AddRoom("First");
    var second = ProjectRegressionTests.AddRoom("Second");
    var line = ProjectRegressionTests.Connect(first, second);
    canvas.SelectedElement = first;
    var commands = new CommandController(canvas);
    commands.SetRoomLighting(LightingActionType.ForceDark);
    commands.SetRoomShape(RoomShape.Ellipse);
    commands.SetStartRoom();
    commands.SetEndRoom();
    first.IsDark.ShouldBeTrue();
    first.Shape.ShouldBe(RoomShape.Ellipse);
    first.IsStartRoom.ShouldBeTrue();
    first.IsEndRoom.ShouldBeTrue();
    second.IsDark.ShouldBeFalse();
    second.Shape.ShouldBe(RoomShape.SquareCorners);
    canvas.SelectedElement = second;
    commands.SetStartRoom();
    first.IsStartRoom.ShouldBeFalse();
    second.IsStartRoom.ShouldBeTrue();
    canvas.SelectedElement = line;
    commands.SetConnectionFlow(ConnectionFlow.OneWay);
    commands.SetConnectionStyle(ConnectionStyle.Dashed);
    commands.SetConnectionLabel(ConnectionLabel.Up);
    line.Flow.ShouldBe(ConnectionFlow.OneWay);
    line.Style.ShouldBe(ConnectionStyle.Dashed);
    line.StartText.ShouldBe("up");
    line.EndText.ShouldBe("down");
    canvas.NewConnectionFlow.ShouldBe(ConnectionFlow.OneWay);
    canvas.NewConnectionStyle.ShouldBe(ConnectionStyle.Dashed);
    canvas.NewConnectionLabel.ShouldBe(ConnectionLabel.Up);
    commands.ToggleConnectionFlow(ConnectionFlow.OneWay);
    line.Flow.ShouldBe(ConnectionFlow.TwoWay);
  }

  [Test]
  public void RegionSelectionAndZOrder_RespectExistingSelection()
  {
    using var canvas = new Canvas();
    var first = ProjectRegressionTests.AddRoom("First");
    var second = ProjectRegressionTests.AddRoom("Second");
    var third = ProjectRegressionTests.AddRoom("Third");
    first.Region = second.Region = "Forest";
    third.Region = "Town";
    canvas.SelectedElement = first;
    var commands = new CommandController(canvas);
    commands.SelectRegions();
    canvas.SelectedRooms.ShouldBe(new[] { first, second });
    commands.BringToFront();
    first.ZOrder.ShouldBeGreaterThan(third.ZOrder);
    second.ZOrder.ShouldBe(first.ZOrder);
    commands.SendToBack();
    first.ZOrder.ShouldBeLessThan(third.ZOrder);
  }

  [TestCase(ValidationType.RoomDescription)]
  [TestCase(ValidationType.RoomUniqueName)]
  [TestCase(ValidationType.RoomSubTitle)]
  [TestCase(ValidationType.RoomDanglingConnection)]
  public void ValidationCommands_ToggleAndRestoreOnlyTheirRule(ValidationType rule)
  {
    using var canvas = new Canvas();
    var commands = new CommandController(canvas);
    commands.SetValidation(rule);
    var flags = new[] {
      Project.Current.MustHaveDescription, Project.Current.MustHaveUniqueNames,
      Project.Current.MustHaveSubtitle, Project.Current.MustHaveNoDanglingConnectors
    };
    flags.Count(value => value).ShouldBe(1);
    commands.SetValidation(rule);
    new[] {
      Project.Current.MustHaveDescription, Project.Current.MustHaveUniqueNames,
      Project.Current.MustHaveSubtitle, Project.Current.MustHaveNoDanglingConnectors
    }.Any(value => value).ShouldBeFalse();
  }

  [Test]
  public void ProjectSwitch_RewiresDirtyEventsAndClearsStaleSelection()
  {
    using var canvas = new Canvas { Size = new Size(600, 400) };
    var oldProject = Project.Current;
    var old = canvas.AddRoom(false, false, false);
    oldProject.IsDirty = false;
    Project.Current = new Project();
    canvas.SelectedElements.ShouldBeEmpty();
    old.Name = "Old changed";
    oldProject.IsDirty.ShouldBeFalse();
    Project.Current.IsDirty.ShouldBeFalse();
    var current = canvas.AddRoom(false, false, false);
    Project.Current.IsDirty = false;
    current.Name = "Current changed";
    Project.Current.IsDirty.ShouldBeTrue();
    Project.Current.Elements.Remove(current);
    canvas.SelectedElements.ShouldBeEmpty();
    oldProject.Dispose();
  }

  [Test]
  public void SearchIndex_RebuildsRoomContentWithoutAnnotations()
  {
    var room = ProjectRegressionTests.AddRoom("Name");
    room.Objects = "Key";
    room.SubTitle = "Subtitle";
    room.AddDescription("Description");
    Project.Current.Elements.Add(new MapLabel(Project.Current) { Text = "Not a room" });
    var index = new Indexer();
    var entry = index.Index().Single();
    entry.Element.ShouldBeSameAs(room);
    entry.Name.ShouldBe("Name");
    entry.Objects.ShouldBe("Key");
    entry.Description.ShouldBe("Description");
    entry.Subtitle.ShouldBe("Subtitle");
    room.Name = "Renamed";
    index.Index().Single().Name.ShouldBe("Renamed");
  }

  [TestCase("map-stats-dead-ends.trizbort", 2)]
  [TestCase("map-stats-dead-end-only-room.trizbort", 1)]
  [TestCase("map-stats-dead-end-start-room.trizbort", 1)]
  public void ManualDeadEndMaps_HaveExplicitExpectedCounts(string fixture, int expected)
  {
    PersistenceRegressionTests.Load(
      Project.Current,
      Path.Combine(PersistenceRegressionTests.FixtureDirectory, fixture));
    MapStatistics.NumberOfDeadEnds.ShouldBe(expected);
  }

  [Test]
  public void Statistics_ClassifyAnnotatedBentAndDiagonalConnectionsWithoutMainForm()
  {
    var first = ProjectRegressionTests.AddRoom("First");
    var second = ProjectRegressionTests.AddRoom("Second");
    ProjectRegressionTests.Connect(first, second);
    var bend = new Connection(
      Project.Current,
      new Vertex(first.PortAt(CompassPoint.North)),
      new Vertex(second.PortAt(CompassPoint.West)));
    bend.StartText = "custom";
    bend.MidText = "via bridge";
    Project.Current.Elements.Add(bend);
    Project.Current.Elements.Add(
      new Connection(
        Project.Current,
        new Vertex(first.PortAt(CompassPoint.NorthEast)),
        new Vertex(second.PortAt(CompassPoint.SouthWest))));
    Project.Current.Elements.Add(
      new Connection(
        Project.Current,
        new Vertex(first.PortAt(CompassPoint.SouthEast)),
        new Vertex(second.PortAt(CompassPoint.North))));
    Project.Current.Elements.Add(
      new Connection(Project.Current, new Vertex(Vector.Zero), new Vertex(new Vector(100, 0))));
    MapStatistics.BentConnections(false).ShouldBe(1);
    MapStatistics.BentConnections(true).ShouldBe(2);
    MapStatistics.DiagonalConnections(0).ShouldBe(3);
    MapStatistics.DiagonalConnections(1).ShouldBe(1);
    MapStatistics.DiagonalConnections(2).ShouldBe(1);
    MapStatistics.CustomConnections.ShouldBe(1);
    MapStatistics.HasMiddleText.ShouldBe(1);
    MapStatistics.NumberOfDanglingConnections.ShouldBe(1);
  }

  [Test]
  public void Statistics_RegionsAndDuplicateExitsIgnoreDanglingTargets()
  {
    var forest = new Region { RegionName = "Forest" };
    var town = new Region { RegionName = "Town" };
    Settings.Regions.Add(forest);
    Settings.Regions.Add(town);
    var first = ProjectRegressionTests.AddRoom("First");
    first.Region = forest.RegionName;
    first.Objects = "Key\nChest\n  Coin";
    first.SubTitle = "Subtitle";
    first.AddDescription("Description");
    var second = ProjectRegressionTests.AddRoom("Second");
    second.Region = town.RegionName;
    ProjectRegressionTests.AddRoom("Unassigned");
    Project.Current.Elements.Add(
      new Connection(Project.Current, new Vertex(first.PortAt(CompassPoint.North)), new Vertex(Vector.Zero)));
    MapStatistics.RegionsLinked(forest, town).ShouldBeFalse();
    MapStatistics.RegionsLinked(forest, forest).ShouldBeFalse();
    var line = ProjectRegressionTests.Connect(first, second);
    line.SetText(ConnectionLabel.In);
    var duplicate = ProjectRegressionTests.Connect(first, second);
    duplicate.SetText(ConnectionLabel.In);
    MapStatistics.RegionsLinked(forest, town).ShouldBeTrue();
    MapStatistics.RegionsLinked(town, forest).ShouldBeTrue();
    MapStatistics.NumberOfRoomsInRegion("Forest").ShouldBe(1);
    MapStatistics.NumberOfRoomsWithoutRegion().ShouldBe(1);
    MapStatistics.NumberOfRegions.ShouldBe(2);
    MapStatistics.NumberOfRoomsWithObjects.ShouldBe(1);
    MapStatistics.NumberOfTotalObjectsInRooms.ShouldBe(3);
    MapStatistics.NumberOfRoomsWithXObjects(3, false).ShouldBe(1);
    MapStatistics.NumberOfRoomsWithXObjects(2, true).ShouldBe(1);
    MapStatistics.NumberOfRoomsWithSubtitles.ShouldBe(1);
    MapStatistics.NumberOfDescribedRooms.ShouldBe(1);
    MapStatistics.InOut.ShouldBe(2);
    MapStatistics.RoomHasDupConnection(first, "in").ShouldBeTrue();
    MapStatistics.DupConnectionList("in").ShouldBe("Rooms with duplicate in exits: First.");
    MapStatistics.DupConnectionList("up").ShouldBe("No rooms with duplicate up exits.");
  }

  [Test]
  public void Statistics_EmptyDocumentAndMixedGraphHaveMeaningfulResults()
  {
    MapStatistics.StartRoomName.ShouldBe("(None)");
    MapStatistics.EndRoomName.ShouldBe("(None)");
    MapStatistics.NumberOfRooms.ShouldBe(0);
    var first = ProjectRegressionTests.AddRoom("Duplicate");
    var second = ProjectRegressionTests.AddRoom("duplicate");
    first.IsDark = true;
    first.IsStartRoom = true;
    second.IsEndRoom = true;
    var line = ProjectRegressionTests.Connect(first, second);
    line.Flow = ConnectionFlow.OneWay;
    line.Door = new Door { Locked = true, Lockable = true };
    Project.Current.Elements.Add(new MapLabel(Project.Current));
    MapStatistics.NumberOfRooms.ShouldBe(2);
    MapStatistics.NumberOfConnections.ShouldBe(1);
    MapStatistics.NumberOfDarkRooms.ShouldBe(1);
    MapStatistics.NumberOfLockedDoors.ShouldBe(1);
    MapStatistics.NumberOfDeadEnds.ShouldBe(1);
    MapStatistics.NumberOfFloatingRooms.ShouldBe(0);
    MapStatistics.NumberOfDanglingConnections.ShouldBe(0);
    MapStatistics.StartRoomName.ShouldBe("Duplicate");
    MapStatistics.EndRoomName.ShouldBe("duplicate");
    MapStatistics.DuplicateNamedRooms.ShouldBe("Duplicate(2)");
    MapStatistics.UnlabeledConnections.ShouldBe(1);
    line.SetText(ConnectionLabel.Up);
    MapStatistics.UnlabeledConnections.ShouldBe(0);
    MapStatistics.UpDown.ShouldBe(1);
  }
}