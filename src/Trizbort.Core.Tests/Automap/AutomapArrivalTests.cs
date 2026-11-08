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
}
