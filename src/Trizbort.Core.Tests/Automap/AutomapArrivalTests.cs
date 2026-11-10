using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Trizbort.Setup;

namespace Trizbort.Core.Tests.Automap;

/// <summary>Where automap's joins meet the rooms they arrive at, and their up and down ends.</summary>
[TestFixture]
[Category("Unit")]
public class AutomapArrivalTests : IsolatedProjectTests
{
  private static async Task WaitFor(Func<bool> condition)
  {
    var clock = Stopwatch.StartNew();
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 5000) Assert.Fail("Timed out waiting for automap.");
      await Task.Delay(10);
    }
  }

  private static List<string> Stubs() =>
    Project.Current.Elements.OfType<Connection>()
      .Select(connection => (source: connection.GetSourceRoom(out var from), target: connection.GetTargetRoom(out var to), from, to))
      .Where(x => x.source != null && x.source == x.target && x.from == x.to)
      .Select(x => x.source.Name + " " + x.from).OrderBy(name => name).ToList();

  [TestCase(true)]
  [TestCase(false)]
  public async Task GoingWestToANewRoom_ArrivesAtItsEastSide_TakenToBeTheWayBack(bool assumeTwoWay)
  {
    // the Study mentions a way east: that is taken to be the way back, so no stub there and the arrow arrives there
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall. A door leads west.", "",
      ">west", "Study", "A quiet study. The door to the east leads back to the hall.", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AssumeTwoWayConnections = assumeTwoWay;
    try
    {
      var run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count == 9);
      await Task.Delay(200);

      var join = Project.Current.Elements.OfType<Connection>().Single(connection =>
        connection.GetSourceRoom() != connection.GetTargetRoom());
      join.GetSourceRoom(out var from).Name.ShouldBe("Hall");
      from.ShouldBe(CompassPoint.West);
      join.GetTargetRoom(out var to).Name.ShouldBe("Study");
      to.ShouldBe(CompassPoint.East);
      Stubs().ShouldNotContain("Study East");

      engine.Stop();
      await run;
    }
    finally
    {
      engine.Stop();
      File.Delete(path);
    }
  }

  private static Room AddRoomAt(string name, float column, float row)
  {
    var room = ProjectRegressionTests.AddRoom(name);
    room.Position = new Vector(column * (Settings.PreferredDistanceBetweenRooms + room.Width),
      row * (Settings.PreferredDistanceBetweenRooms + room.Height));
    return room;
  }

  private static Connection Join(Room from, CompassPoint fromPoint, Room to, CompassPoint toPoint)
  {
    var connection = new Connection(Project.Current, new Vertex(from.PortAt(fromPoint)), new Vertex(to.PortAt(toPoint)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  private static bool Crosses(Connection a, Connection b)
  {
    static double Side(Vector o, Vector x, Vector y) => (x.X - o.X) * (y.Y - o.Y) - (x.Y - o.Y) * (y.X - o.X);
    Vector p = a.VertexList[0].Position, q = a.VertexList[1].Position, r = b.VertexList[0].Position, s = b.VertexList[1].Position;
    return Side(r, s, p) * Side(r, s, q) < -0.25 && Side(p, q, r) * Side(p, q, s) < -0.25;
  }

  [Test]
  public void UpAndDownEnds_MoveRoundTheirRoom_SoTheyDontCrossAnotherJoin()
  {
    // south from the Hall to the Cellar, and down to it too; down starts left of the south exit, and up arrives
    // right of the north one, so the two lines cross
    var hall = AddRoomAt("Hall", 0, 0);
    var cellar = AddRoomAt("Cellar", 0, 1);
    var south = Join(hall, CompassPoint.South, cellar, CompassPoint.North);
    south.StartTravelled = "s";
    var down = Join(hall, CompassPoint.SouthSouthWest, cellar, CompassPoint.NorthNorthEast);
    down.StartText = "d";
    down.EndText = "u";
    down.StartTravelled = "d";
    Crosses(south, down).ShouldBeTrue();

    GuessedEnds.Face(Project.Current.Elements.ToList()).ShouldBeGreaterThan(0);

    Crosses(south, down).ShouldBeFalse();
    // still between the compass points, so they don't look like compass exits
    down.GetSourceRoom(out var start);
    down.GetTargetRoom(out var end);
    ((int)start % 2).ShouldBe(1);
    ((int)end % 2).ShouldBe(1);
  }

  [Test]
  public void UpAndDownEnds_DrawnByHand_StayWhereTheyAre()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var cellar = AddRoomAt("Cellar", 0, 1);
    Join(hall, CompassPoint.South, cellar, CompassPoint.North);
    var down = Join(hall, CompassPoint.SouthSouthWest, cellar, CompassPoint.NorthNorthEast);
    down.StartText = "d";
    down.EndText = "u";

    GuessedEnds.Face(Project.Current.Elements.ToList()).ShouldBe(0);
  }
  [Test]
  public void MergeRooms_MovesTheJoinsOntoTheRoomKept_AndRemovesTheOthers()
  {
    // Colossal Cave: "Darkness" east of the debris room, which turned out to be the awkward canyon
    var debris = AddRoomAt("Debris Room", 0, 0);
    var dark = AddRoomAt("Darkness", 1, 0);
    var canyon = AddRoomAt("Awkward Canyon", 2, 0);
    dark.AddDescription("It is now pitch dark.");
    Join(debris, CompassPoint.East, dark, CompassPoint.West);
    Join(dark, CompassPoint.East, canyon, CompassPoint.West); // between the merged rooms: goes
    Join(canyon, CompassPoint.West, debris, CompassPoint.East); // doubles the first once merged: goes
    var stub = Join(dark, CompassPoint.North, dark, CompassPoint.North); // an unexplored exit: kept, on the canyon

    RoomMerge.Merge(Project.Current.Elements, canyon, new[] { dark, canyon }).ShouldBe(1);

    Project.Current.Elements.OfType<Room>().Select(room => room.Name).OrderBy(name => name)
      .ShouldBe(new[] { "Awkward Canyon", "Debris Room" });
    var joins = Project.Current.Elements.OfType<Connection>().ToList();
    joins.Count.ShouldBe(2);
    joins.Count(join => join.GetSourceRoom() == debris && join.GetTargetRoom() == canyon).ShouldBe(1);
    stub.GetSourceRoom(out var point).ShouldBe(canyon);
    point.ShouldBe(CompassPoint.North);
    canyon.Descriptions.ShouldContain("It is now pitch dark.");
  }

  [TestCase("Darkness", true)]
  [TestCase("Dark", true)]
  [TestCase("It is pitch black", true)]
  [TestCase("In the Darkness", true)]
  [TestCase("Dark Forest", false)]
  [TestCase("Darkroom", false)]
  public void DarkHeadings_AreRecognised(string heading, bool dark)
  {
    Trizbort.Automap.Automap.IsDarkness(heading).ShouldBe(dark);
  }

  [Test]
  public async Task SomewhereDark_IsMappedWhenItCanBeSeen_AsReachedTheWayYouCameIn()
  {
    var path = Path.GetTempFileName();
    File.WriteAllText(path, string.Join("\n", new[]
    {
      ">look", "Debris Room", "A room full of debris.", "",
      ">east", "Darkness", "It is now pitch dark. If you proceed you will likely fall into a pit.", "",
      ">light lamp", "Your lamp is now on.", "", "Awkward Canyon", "You are in an awkward sloping canyon.", "",
      ">west", "Debris Room", "A room full of debris.", "",
      ">east", "Awkward Canyon", "You are in an awkward sloping canyon.", "",
      ">"
    }));
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      var clock = Stopwatch.StartNew();
      while (engine.ScreenLines().Count < 23 && clock.ElapsedMilliseconds < 5000) await Task.Delay(10);
      engine.ScreenLines().Count.ShouldBe(23, "screen: " + string.Join(" | ", engine.ScreenLines()) + " status: " + engine.Status);
      await Task.Delay(200);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }

    var rooms = Project.Current.Elements.OfType<Room>().ToList();
    rooms.Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Awkward Canyon", "Debris Room" });
    rooms.Single(room => room.Name == "Awkward Canyon").IsDark.ShouldBeTrue();
    rooms.Single(room => room.Name == "Debris Room").IsDark.ShouldBeFalse();
    var joins = Project.Current.Elements.OfType<Connection>()
      .Where(connection => connection.GetSourceRoom() != connection.GetTargetRoom()).ToList();
    var join = joins.ShouldHaveSingleItem();
    join.GetSourceRoom(out var from).Name.ShouldBe("Debris Room");
    from.ShouldBe(CompassPoint.East);
    join.GetTargetRoom().Name.ShouldBe("Awkward Canyon");
    string.IsNullOrEmpty(join.MidText).ShouldBeTrue();
  }
}
