using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 16 Nov 2026: the new room goes on the map straight away and any tidy comes afterwards, worked out away from
///   the window, as an undo step of its own, so undoing a tidy that went wrong keeps the new room. And automap no
///   longer selects the room you're in: the blue ring shows that, and the selection stays what the user chose.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TidyAfterTheMoveTests : IsolatedProjectTests
{
  private static Room AddRoom(string name, int x, int y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private static void Join(Room a, CompassPoint from, Room b, CompassPoint to) =>
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(a.PortAt(from)), new Vertex(b.PortAt(to))));

  // a move that put the new room right on top of the one it came from
  private static (Room Hall, Room Kitchen) AMoveOntoAnotherRoom(EditorSession session)
  {
    var hall = AddRoom("Hall", 0, 0);
    Room kitchen;
    using (session.BeginTransaction("Automap: Kitchen"))
    {
      kitchen = AddRoom("Kitchen", 16, 0);
      Join(hall, CompassPoint.East, kitchen, CompassPoint.West);
    }

    return (hall, kitchen);
  }

  [Test]
  public void TheTidy_IsAnUndoStepOfItsOwn_AndUndoingItKeepsTheNewRoom()
  {
    using var session = new EditorSession();
    var (hall, kitchen) = AMoveOntoAnotherRoom(session);
    var canvas = new AutomapCanvas(null, null, session);

    canvas.TidyAround(kitchen);

    hall.InnerBounds.IntersectsWith(kitchen.InnerBounds).ShouldBeFalse();
    session.Undo.UndoTitle.ShouldBe("Tidy the map");

    session.Undo.Undo(session);

    // the tidy is undone, and the new room is still there, on top of the hall as the move left it
    // undo puts back a copy of the map
    var rooms = session.Project.Elements.OfType<Room>().ToList();
    rooms.Select(room => room.Name).ShouldBe(new[] { "Hall", "Kitchen" }, ignoreOrder: true);
    rooms.Single(room => room.Name == "Hall").InnerBounds
      .IntersectsWith(rooms.Single(room => room.Name == "Kitchen").InnerBounds).ShouldBeTrue();
    session.Undo.UndoTitle.ShouldBe("Automap: Kitchen");
  }

  [Test]
  public async Task TheTidy_CanBeWorkedOutOnAnotherThread()
  {
    using var session = new EditorSession();
    var (hall, kitchen) = AMoveOntoAnotherRoom(session);
    var canvas = new AutomapCanvas(null, null, session);

    var tidy = canvas.PlanTidyAround(kitchen);
    var moves = await Task.Run(() => tidy.Run());
    // nothing moves until it's put on the map
    kitchen.Position.ShouldBe(new Vector(16, 0));

    canvas.FinishTidyAround(tidy, moves).ShouldBeGreaterThan(0);

    hall.InnerBounds.IntersectsWith(kitchen.InnerBounds).ShouldBeFalse();
    session.Undo.UndoTitle.ShouldBe("Tidy the map");
  }

  [Test]
  public void ATidyWorkedOutBeforeTheMapChanged_IsLeftOut()
  {
    using var session = new EditorSession();
    var (_, kitchen) = AMoveOntoAnotherRoom(session);
    var canvas = new AutomapCanvas(null, null, session);

    var tidy = canvas.PlanTidyAround(kitchen);
    var moves = tidy.Run();
    using (session.BeginTransaction("Add room")) AddRoom("Garden", 400, 400);

    canvas.FinishTidyAround(tidy, moves).ShouldBe(0);

    kitchen.Position.ShouldBe(new Vector(16, 0));
    session.Undo.UndoTitle.ShouldBe("Add room");
  }

  [Test]
  public async Task Automap_LeavesTheSelectionAlone()
  {
    using var session = new EditorSession();
    var label = AddRoom("Notes", 2000, 2000);
    session.Selection.Set(new Element[] { label });
    var path = Files.File("game.txt");
    File.WriteAllText(path, "Game\n\nHall\nA hall.\n\n>e\n\nKitchen\nA kitchen.\n\n>w\n\nHall\nA hall.\n");
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.GuessExits = false;
    var engine = new Trizbort.Automap.Automap((message, title) => Assert.Fail(title + ": " + message),
      NonInteractiveEditorPrompts.Instance);

    await engine.StartCl(new AutomapCanvas(null, engine, session), settings);

    Project.Current.Elements.OfType<Room>().Count().ShouldBe(3);
    engine.CurrentRoom.Name.ShouldBe("Hall");
    session.Selection.Items.ShouldBe(new Element[] { label });
  }
}
