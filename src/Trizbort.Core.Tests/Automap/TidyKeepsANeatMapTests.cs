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
///   Lee, 17 Nov 2026: he'd neatened the map by hand, and walking from the Projection Booth to South Junction, all
///   through rooms automap knew, tidied it into a mess. The Closet and the Top of Closet, side by side as he'd put them,
///   counted as rooms on top of each other, so the tidy after the move pushed the whole map aside to part them; and the
///   first tidy after automap started compared the map with an empty one, so it was "worse" and laid out again.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TidyKeepsANeatMapTests : IsolatedProjectTests
{
  private static Room AddRoom(string name, int x, int y, bool byHand = false)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y), PlacedByHand = byHand };
    room.AddDescription("This is the " + name.ToLower() + ".");
    Project.Current.Elements.Add(room);
    return room;
  }

  private static void Join(Room a, CompassPoint from, Room b, CompassPoint to) =>
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(a.PortAt(from)), new Vertex(b.PortAt(to))));

  private static System.Collections.Generic.Dictionary<Room, Vector> Positions() =>
    Project.Current.Elements.OfType<Room>().ToDictionary(room => room, room => room.Position);

  [Test]
  public void RoomsPlacedByHandSideBySide_AreNotTidiedApart()
  {
    using var session = new EditorSession();
    var hall = AddRoom("Hall", 0, 0, true);
    var kitchen = AddRoom("Kitchen", 160, 0, true);
    Join(hall, CompassPoint.East, kitchen, CompassPoint.West);
    // a pair drawn touching, as the user wanted them
    var closet = AddRoom("Closet", 0, 128, true);
    var top = AddRoom("Top of Closet", 96, 128, true);
    Join(hall, CompassPoint.South, closet, CompassPoint.North);
    Join(closet, CompassPoint.East, top, CompassPoint.West);
    var before = Positions();

    new AutomapCanvas(null, null, session).TidyAround(kitchen);

    Positions().ShouldBe(before);
  }

  [Test]
  public void TheWayRoomsWerePlacedByHand_IsNotAProblem()
  {
    var hall = AddRoom("Hall", 0, 0, true);
    var kitchen = AddRoom("Kitchen", 160, 0, true);
    // east from the hall, though the kitchen is drawn to its west... the user put it there
    Join(kitchen, CompassPoint.East, hall, CompassPoint.West);

    LayoutTidy.Problems(Project.Current.Elements.ToList()).ShouldBe(0);

    kitchen.PlacedByHand = false;
    LayoutTidy.Problems(Project.Current.Elements.ToList()).ShouldBe(1);
  }

  [Test]
  public async Task WalkingAroundAMapLaidOutAlready_LeavesItAlone()
  {
    using var session = new EditorSession();
    var hall = AddRoom("Hall", 0, 0);
    var kitchen = AddRoom("Kitchen", 160, 0);
    Join(hall, CompassPoint.East, kitchen, CompassPoint.West);
    // north from the kitchen, drawn off to the side: not as the tidy would draw it, but it's how the map was left
    var pantry = AddRoom("Pantry", 160, -320);
    var larder = AddRoom("Larder", -160, -160);
    Join(kitchen, CompassPoint.North, larder, CompassPoint.South);
    Join(larder, CompassPoint.East, pantry, CompassPoint.West);
    var before = Positions();
    var path = Files.File("walk.txt");
    File.WriteAllText(path,
      "Game\n\nHall\nThis is the hall.\n\n>e\n\nKitchen\nThis is the kitchen.\n\n>w\n\nHall\nThis is the hall.\n\n" +
      ">e\n\nKitchen\nThis is the kitchen.\n");
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.GuessExits = false;
    settings.AutoTidy = true;
    var engine = new Trizbort.Automap.Automap((message, title) => Assert.Fail(title + ": " + message),
      NonInteractiveEditorPrompts.Instance);

    await engine.StartCl(new AutomapCanvas(null, engine, session) { RepeatableLayout = true }, settings);

    engine.CurrentRoom.Name.ShouldBe("Kitchen");
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(4);
    Positions().ShouldBe(before);
  }
}
