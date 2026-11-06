using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Automap's tidy: rooms make space for each other as the map grows.</summary>
[TestFixture]
[Category("Unit")]
public class LayoutTidyTests : IsolatedProjectTests
{
  private static float StepX(Room room) => Settings.PreferredDistanceBetweenRooms + room.Width;
  private static float StepY(Room room) => Settings.PreferredDistanceBetweenRooms + room.Height;

  private static Room AddRoomAt(string name, float column, float row)
  {
    var room = ProjectRegressionTests.AddRoom(name);
    room.Position = new Vector(column * StepX(room), row * StepY(room));
    return room;
  }

  private static Connection Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide)
  {
    var connection = new Connection(Project.Current, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  private static (float Column, float Row) Cell(Room room) =>
    ((float)System.Math.Round(room.Position.X / StepX(room)), (float)System.Math.Round(room.Position.Y / StepY(room)));

  private static int Overlaps() =>
    Project.Current.Elements.OfType<Room>().ToList() is var rooms
      ? rooms.SelectMany((a, i) => rooms.Skip(i + 1).Where(b => a.InnerBounds.IntersectsWith(b.InnerBounds))).Count()
      : 0;

  [Test]
  public void ATidyMap_IsLeftAlone()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 1, 0);
    var attic = AddRoomAt("Kitchen", 0, 1);
    Join(hall, CompassPoint.East, garden, CompassPoint.West);
    Join(hall, CompassPoint.South, attic, CompassPoint.North);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), garden).ShouldBe(0);
  }

  [Test]
  public void ARoomOnTopOfAnother_MovesToWhereItsJoinSays()
  {
    // the Patio case: west of Southeast Junction is already South Junction
    var junction = AddRoomAt("South Junction", 0, 0);
    var east = AddRoomAt("Southeast Junction", 1, 0);
    Join(junction, CompassPoint.East, east, CompassPoint.West);
    var patio = AddRoomAt("Patio", 0, 0);
    Join(east, CompassPoint.West, patio, CompassPoint.East);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), patio).ShouldBeGreaterThan(0);

    Overlaps().ShouldBe(0);
    // nothing sits on the junction's join either
    Project.Current.Elements.OfType<Connection>().First().Intersects(patio.InnerBounds).ShouldBeFalse();
  }

  [Test]
  public void RoomsMakeSpace_SoANewRoomCanSitWhereItsDirectionSays()
  {
    // north of the Hall is the Landing; the Study (also reached north, from the Hall's neighbour) lands on it
    var hall = AddRoomAt("Hall", 0, 0);
    var landing = AddRoomAt("Landing", 0, -1);
    Join(hall, CompassPoint.North, landing, CompassPoint.South);
    var lobby = AddRoomAt("Lobby", 1, 0);
    Join(hall, CompassPoint.East, lobby, CompassPoint.West);
    var study = AddRoomAt("Study", 0, -1);
    Join(lobby, CompassPoint.North, study, CompassPoint.South);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), study);

    Overlaps().ShouldBe(0);
    Cell(study).Row.ShouldBeLessThan(Cell(lobby).Row);
  }

  [Test]
  public void TeleportJoins_DontPull()
  {
    var building = AddRoomAt("Inside Building", 0, 0);
    var debris = AddRoomAt("Debris Room", 6, 3);
    var join = Join(building, CompassPoint.East, debris, CompassPoint.West);
    join.AutomapCommand = "xyzzy";

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), debris).ShouldBe(0);
  }

  [Test]
  public void ConnectionStyle_DoesNotMatter()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 3, 2);
    var join = Join(hall, CompassPoint.East, garden, CompassPoint.West);
    join.Style = ConnectionStyle.Dashed;
    join.StartText = "through the arch";

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), garden).ShouldBeGreaterThan(0);

    Cell(garden).ShouldBe((1f, 0f));
  }

  [Test]
  public void RoomsFarAway_StayPut()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 1, 0);
    Join(hall, CompassPoint.East, garden, CompassPoint.West);
    var faraway = AddRoomAt("Lighthouse", 10, 10);
    var island = AddRoomAt("Island", 13, 10);
    Join(faraway, CompassPoint.East, island, CompassPoint.West);
    var before = island.Position;

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), garden);

    island.Position.ShouldBe(before);
  }

  [Test]
  public void BothEndsOfAJoinDecideWhereTheRoomGoes_AndTheOthersMakeSpace()
  {
    // Hollywood Hijinx: east from South Junction reaches Southeast Junction, and south from there leads back,
    // so Southeast Junction belongs to the north-east; Northeast Junction (north of it) moves up to make space
    // and the Patio (west of it) ends up beside it. This is the map Lee corrected by hand.
    var start = AddRoomAt("South Junction", 0, 0);
    var southeast = AddRoomAt("Southeast Junction", 1, 0);
    var patio = AddRoomAt("Patio", 0, -1);
    var northeast = AddRoomAt("Northeast Junction", 1, -1);
    Join(start, CompassPoint.East, southeast, CompassPoint.South);
    Join(southeast, CompassPoint.West, patio, CompassPoint.East);
    Join(southeast, CompassPoint.North, northeast, CompassPoint.South);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), start).ShouldBeGreaterThan(0);

    Overlaps().ShouldBe(0);
    var (sc, sr) = Cell(start);
    Cell(southeast).ShouldBe((sc + 1, sr - 1));
    Cell(patio).ShouldBe((sc, sr - 1));
    Cell(northeast).ShouldBe((sc + 1, sr - 2));
  }

  [Test]
  public void ANewRoomWhoseSpotIsTaken_StaysNextToItsRoom_AndTheOthersMoveAside()
  {
    // east of the Hall is the Lobby; a new room reached north from the Lobby lands where the Study already is
    var hall = AddRoomAt("Hall", 0, 0);
    var lobby = AddRoomAt("Lobby", 1, 0);
    Join(hall, CompassPoint.East, lobby, CompassPoint.West);
    var study = AddRoomAt("Study", 1, -1);
    Join(hall, CompassPoint.NorthEast, study, CompassPoint.SouthWest);
    var cloakroom = AddRoomAt("Cloakroom", 1, -1); // same spot as the Study
    Join(lobby, CompassPoint.North, cloakroom, CompassPoint.South);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), cloakroom);

    Overlaps().ShouldBe(0);
    var (lc, lr) = Cell(lobby);
    Cell(cloakroom).ShouldBe((lc, lr - 1));
  }
}
