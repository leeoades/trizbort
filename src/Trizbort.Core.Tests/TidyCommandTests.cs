using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Core.Tests;

/// <summary>Edit > Rooms > Tidy Map and Tidy Selected Rooms.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class TidyCommandTests
{
  private Project _previousProject;
  private EditorSession _session;
  private CommandCatalogue _catalogue;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    Project.Current = new Project();
    _session = new EditorSession();
    _catalogue = CommandCatalogue.CreateDefault();
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    Project.Current = _previousProject;
  }

  private static Room Room(string name, float column, float row)
  {
    var room = new Room(Project.Current) { Name = name };
    room.Position = new Vector(column * 160, row * 128);
    Project.Current.Elements.Add(room);
    return room;
  }

  private static void Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide) =>
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide))));

  private (Room Hall, Room Garden, Room Shed) AMessyMap()
  {
    // north from the Hall to the Garden, east from the Garden to the Shed; drawn all over the place
    var hall = Room("Hall", 0, 0);
    var garden = Room("Garden", 3, 2);
    var shed = Room("Shed", -2, 1);
    Join(hall, CompassPoint.North, garden, CompassPoint.South);
    Join(garden, CompassPoint.East, shed, CompassPoint.West);
    return (hall, garden, shed);
  }

  [Test]
  public void TidyMap_PutsEveryJoinTheWayItPoints_AsOneUndoStep()
  {
    var (hall, garden, shed) = AMessyMap();
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).Natural.ShouldBe(0);
    var before = (hall.Position, garden.Position, shed.Position);

    var tidy = _catalogue.Find("map.tidy");
    tidy.CanExecute(_session).ShouldBeTrue();
    tidy.ExecuteAsync(_session).Wait();

    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
    _session.PerformUndo();
    // (undo puts back copies of the rooms as they were)
    Vector At(string name) => Project.Current.Elements.OfType<Room>().Single(room => room.Name == name).Position;
    (At("Hall"), At("Garden"), At("Shed")).ShouldBe(before);
  }

  [Test]
  public void TidySelectedRooms_MovesOnlyTheSelection()
  {
    var (hall, garden, shed) = AMessyMap();
    var hallBefore = hall.Position;

    var tidy = _catalogue.Find("map.tidySelection");
    _session.Selection.Set(new Element[] { hall });
    tidy.CanExecute(_session).ShouldBeFalse();
    _session.Selection.Set(new Element[] { garden, shed });
    tidy.CanExecute(_session).ShouldBeTrue();
    tidy.ExecuteAsync(_session).Wait();

    hall.Position.ShouldBe(hallBefore);
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
  }
}
