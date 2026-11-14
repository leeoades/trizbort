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
  public void ASquareOfRooms_KeepsItsShape_WhenANewRoomLandsOnIt()
  {
    // four rooms in a square, then a room south-east of the first lands where the fourth already is
    var a = AddRoomAt("A", 0, 0);
    var b = AddRoomAt("B", 1, 0);
    var c = AddRoomAt("C", 0, 1);
    var d = AddRoomAt("D", 1, 1);
    Join(a, CompassPoint.East, b, CompassPoint.West);
    Join(a, CompassPoint.South, c, CompassPoint.North);
    Join(b, CompassPoint.South, d, CompassPoint.North);
    Join(c, CompassPoint.East, d, CompassPoint.West);
    var e = AddRoomAt("E", 1, 1);
    Join(a, CompassPoint.SouthEast, e, CompassPoint.NorthWest);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), e).ShouldBeGreaterThan(0);

    Overlaps().ShouldBe(0);
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((5, 5));
  }

  [Test]
  public void ANewRoomUnderAJoin_MovesDown_TakingTheRoomsBelowWithIt()
  {
    // Colossal Cave: north from the Tall E/W Canyon reaches the Swiss Cheese Room, in the gap just under the
    // one-way join from the Hall of the Mountain King's south-west corner to the Secret E/W Canyon. The join
    // skims the room's top; the room should move down instead, and the canyons below it make space.
    var hall = AddRoomAt("Hall of the Mountain King", 0, -1);
    var side = AddRoomAt("In South Side Chamber", 0, 0);
    var secret = AddRoomAt("Secret E/W Canyon", -2, 0);
    var tall = AddRoomAt("In Tall E/W Canyon", -1, 1);
    var narrow = AddRoomAt("N/S Canyon", -1, 2);
    var swiss = AddRoomAt("In Swiss Cheese Room", -1, 0);
    Join(hall, CompassPoint.South, side, CompassPoint.North);
    Join(hall, CompassPoint.SouthWest, secret, CompassPoint.East).Flow = ConnectionFlow.OneWay;
    Join(narrow, CompassPoint.North, tall, CompassPoint.South).Flow = ConnectionFlow.OneWay;
    Join(tall, CompassPoint.North, swiss, CompassPoint.South).Flow = ConnectionFlow.OneWay;

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), swiss).ShouldBeGreaterThan(0);

    Overlaps().ShouldBe(0);
    var (_, secretRow) = Cell(secret);
    Cell(swiss).Row.ShouldBeGreaterThan(secretRow);
    Cell(tall).ShouldBe((Cell(swiss).Column, Cell(swiss).Row + 1));
  }

  [Test]
  public void ARoomAJoinRunsAcross_StepsUpOrDown_SoTheJoinPassesItBy()
  {
    // east from the Hall, a long way, to the Gallery; the Statue Room sits in the way, two rooms short of it
    // (the Gallery has a way on east too, so it keeps its place)
    var hall = AddRoomAt("Hall", 0, 0);
    var statue = AddRoomAt("Statue Room", 2, 0);
    var gallery = AddRoomAt("Gallery", 4, 0);
    var garden = AddRoomAt("Garden", 5, 0);
    Join(hall, CompassPoint.East, gallery, CompassPoint.West);
    Join(gallery, CompassPoint.East, garden, CompassPoint.West);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), gallery).ShouldBeGreaterThan(0);

    Cell(hall).ShouldBe((0f, 0f));
    Cell(gallery).ShouldBe((4f, 0f));
    Cell(statue).Column.ShouldBe(2f);
    Cell(statue).Row.ShouldNotBe(0f);
  }

  [Test]
  public void TheSpaceInFrontOfAnUnexploredExit_StaysClear()
  {
    // Colossal Cave: the Debris Room has an unexplored way west; going up reaches the Sloping E/W Canyon, whose way
    // back is east. The canyon belongs to the west, but not right in front of the west exit, where the two would
    // run into each other.
    var debris = AddRoomAt("Debris Room", 0, 0);
    Join(debris, CompassPoint.West, debris, CompassPoint.West);
    var canyon = AddRoomAt("Sloping E/W Canyon", -1, 0);
    Join(debris, CompassPoint.NorthNorthWest, canyon, CompassPoint.East);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), canyon).ShouldBeGreaterThan(0);

    Overlaps().ShouldBe(0);
    var (dc, dr) = Cell(debris);
    Cell(canyon).ShouldNotBe((dc - 1, dr));
    Cell(canyon).Column.ShouldBeLessThan(dc);
  }

  [Test]
  public void TwoWaysToTheSameRoom_OnlyTheOneThatFitsCounts()
  {
    // east and north from the Hall both reach the Garden; the Garden sits east, so the north way just bends round
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 1, 0);
    Join(hall, CompassPoint.East, garden, CompassPoint.West);
    Join(hall, CompassPoint.North, garden, CompassPoint.South);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), garden).ShouldBe(0);
  }

  [Test]
  public void TheFarEndOfAOneWayJoin_DoesntSayWhereItsRoomGoes()
  {
    // east from the Hall drops into the Cellar, and there's no way back: the arrow meets the Cellar on the side
    // facing the Hall, which says nothing about where the Cellar should be, so it goes east
    var hall = AddRoomAt("Hall", 0, 0);
    var cellar = AddRoomAt("Cellar", 0, -1);
    var join = Join(hall, CompassPoint.East, cellar, CompassPoint.South);
    join.Flow = ConnectionFlow.OneWay;

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), cellar).ShouldBeGreaterThan(0);

    Cell(hall).ShouldBe((0f, 0f));
    Cell(cellar).ShouldBe((1f, 0f));
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

  [Test]
  public void GoingSouthIntoTheGarden_MovesEverythingAboveUpToMakeSpace()
  {
    // Lee's Hollywood Hijinx map (HH-Nov-6-garden): south from the Entrance to Hedge Maze is Garden, North, but the
    // Patio is in that spot. The Entrance and everything above it should move up, so the garden sits south of it.
    var start = AddRoomAt("South Junction", 0, 1);
    var porch = AddRoomAt("Front Porch", 0, 0);
    var patio = AddRoomAt("Patio", 0, -1);
    var southeast = AddRoomAt("Southeast Junction", 1, -1);
    var southwest = AddRoomAt("Southwest Junction", -1, -1);
    var entrance = AddRoomAt("Entrance to Hedge Maze", 0, -2);
    var northeast = AddRoomAt("Northeast Junction", 1, -2);
    var northwest = AddRoomAt("Northwest Junction", -2, -3);
    var cannon = AddRoomAt("Cannon Emplacement", 0, -3);
    var cliff = AddRoomAt("Cliff", 0, -4);
    var landing = AddRoomAt("Top Landing", 2, -2);
    var stairs = AddRoomAt("Middle of Beach Stairs", 2, -1);
    Join(start, CompassPoint.East, southeast, CompassPoint.South);
    Join(southeast, CompassPoint.West, patio, CompassPoint.East);
    Join(start, CompassPoint.North, porch, CompassPoint.South);
    Join(start, CompassPoint.West, southwest, CompassPoint.South);
    Join(southwest, CompassPoint.East, patio, CompassPoint.West);
    Join(southwest, CompassPoint.North, northwest, CompassPoint.South);
    Join(northwest, CompassPoint.East, entrance, CompassPoint.West);
    Join(entrance, CompassPoint.East, northeast, CompassPoint.West);
    Join(northeast, CompassPoint.South, southeast, CompassPoint.North);
    Join(northwest, CompassPoint.North, cannon, CompassPoint.West);
    Join(cannon, CompassPoint.East, northeast, CompassPoint.North);
    Join(cannon, CompassPoint.North, cliff, CompassPoint.South);
    Join(northeast, CompassPoint.East, landing, CompassPoint.West);
    Join(landing, CompassPoint.SouthSouthWest, stairs, CompassPoint.NorthNorthWest);
    var garden = AddRoomAt("Garden, North", 0, -1); // automap puts it south of the Entrance, on the Patio
    Join(entrance, CompassPoint.South, garden, CompassPoint.North);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), garden).ShouldBeGreaterThan(0);

    Overlaps().ShouldBe(0);
    var (ec, er) = Cell(entrance);
    Cell(garden).ShouldBe((ec, er + 1));
    // the rooms above moved up together, keeping their shape
    Cell(cannon).ShouldBe((ec, er - 1));
    Cell(cliff).ShouldBe((ec, er - 2));
  }

  private static string RepositoryFile(params string[] parts)
  {
    var directory = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "Trizbort.sln")))
      directory = directory.Parent;
    directory.ShouldNotBeNull();
    return System.IO.Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
  }

  [Test]
  public void LeesLongForest_MovingItASquare_KeepsTheStreambedArrowOnTheSideFacingTheStreambed()
  {
    // Long forest snap.trizbort: Colossal Cave's In Forest stretched tall, with arrows arriving on its east side
    // from the Streambed and the Outside Grate. Moving the forest one square jumped the Streambed's arrow round to
    // north-north-east, because every point near the east side was taken or beside a known exit.
    var path = RepositoryFile("tests", "automap", "colossal-cave", "long-forest.trizbort");
    new MapLoader(Project.Current).LoadMap(path).ShouldBeTrue();
    var forest = Project.Current.Elements.OfType<Room>().Single(room => room.Name == "In Forest");
    var streambed = Project.Current.Elements.OfType<Connection>()
      .Single(join => join.GetSourceRoom()?.Name == "At Slit In Streambed" && join.GetTargetRoom() == forest);

    foreach (var dx in new[] { -32f, 32f, 32f })
    {
      forest.Position = new Vector(forest.Position.X + dx, forest.Position.Y);
      GuessedEnds.Face(Project.Current.Elements, new[] { forest });
      streambed.GetTargetRoom(out var point);
      ((int)point).ShouldBeInRange((int)CompassPoint.EastNorthEast, (int)CompassPoint.SouthSouthEast);
    }
  }

  [Test]
  public void ARoomPlacedByHand_StaysWhereTheUserPutIt()
  {
    // as ARoomAJoinRunsAcross_StepsUpOrDown, but the user put the Statue Room there
    var hall = AddRoomAt("Hall", 0, 0);
    var statue = AddRoomAt("Statue Room", 2, 0);
    statue.PlacedByHand = true;
    var gallery = AddRoomAt("Gallery", 4, 0);
    var garden = AddRoomAt("Garden", 5, 0);
    Join(hall, CompassPoint.East, gallery, CompassPoint.West);
    Join(gallery, CompassPoint.East, garden, CompassPoint.West);
    var before = statue.Position;

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), gallery);

    statue.Position.ShouldBe(before);
  }

  [Test]
  public void ARoomPlacedByHand_IsPushedAside_WhenANewRoomNeedsItsPlace()
  {
    // east from the Hall leads to the Kitchen, but the user put the Shed there: the Kitchen takes its place east of
    // the Hall and the Shed slides out of the way, still marked as placed by hand
    var hall = AddRoomAt("Hall", 0, 0);
    var shed = AddRoomAt("Shed", 1, 0);
    shed.PlacedByHand = true;
    var kitchen = AddRoomAt("Kitchen", 1, 1);
    Join(hall, CompassPoint.East, kitchen, CompassPoint.West);
    var shedWas = shed.Position;

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), kitchen);

    Cell(kitchen).ShouldBe((1f, 0f));
    shed.Position.ShouldNotBe(shedWas);
    shed.PlacedByHand.ShouldBeTrue();
    Overlaps().ShouldBe(0);
  }

  [Test]
  public void AnAssumedWayBack_ThatAnotherWayContradicts_LetsThePathBend()
  {
    // Hollywood Hijinx, move 12 of the old transcript: east from South Junction to Southeast Junction (the way back
    // west only assumed), then west from Southeast Junction to the Patio. The Patio belongs west of Southeast
    // Junction, where South Junction is, so the assumed way back is wrong: the path round the house bends, and
    // Southeast Junction moves off South Junction's row to make room, instead of the Patio going somewhere odd.
    var south = AddRoomAt("South Junction", 0, 0);
    var porch = AddRoomAt("Front Porch", 0, -1);
    var southEast = AddRoomAt("Southeast Junction", 1, 0);
    var patio = AddRoomAt("Patio", -1, 1);
    var toPorch = Join(south, CompassPoint.North, porch, CompassPoint.South);
    toPorch.StartTravelled = "north";
    toPorch.EndTravelled = "south";
    Join(south, CompassPoint.East, southEast, CompassPoint.West).StartTravelled = "east";
    Join(southEast, CompassPoint.West, patio, CompassPoint.East).StartTravelled = "west";

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), patio);

    Cell(patio).Row.ShouldBe(Cell(southEast).Row);
    Cell(patio).Column.ShouldBeLessThan(Cell(southEast).Column);
    Cell(southEast).Row.ShouldNotBe(Cell(south).Row);
    Overlaps().ShouldBe(0);
  }

  [Test]
  public void ARethink_TurnsTheMapRoundWhenAJoinPointsTheWrongWay_TheSameWayEveryTime()
  {
    // automap put the Garden and the Shed above the Hall, then learnt that the Garden is south of it: nudging can't
    // fix that (the Shed is in the way), the rethink lays the rooms out again with the Garden below
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 0, -1);
    var shed = AddRoomAt("Shed", 0, -2);
    Join(hall, CompassPoint.South, garden, CompassPoint.North);
    Join(garden, CompassPoint.South, shed, CompassPoint.North);
    var elements = Project.Current.Elements.ToList();
    var rooms = elements.OfType<Room>().ToList();

    var first = LayoutTidy.PlanRethink(elements, rooms, 300).Run();
    var second = LayoutTidy.PlanRethink(elements, rooms, 300).Run();
    LayoutTidy.TidyPlan.Apply(first);

    first.ShouldBe(second);
    Cell(garden).Row.ShouldBeGreaterThan(Cell(hall).Row);
    Cell(shed).Row.ShouldBeGreaterThan(Cell(garden).Row);
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
  }

  [Test]
  public void ARoomPlacedByHand_IsNotPulledBackIntoLine()
  {
    // the user put the Garden a little below the Hall's east: the tidy would line it up, but leaves it be
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 2, 1);
    garden.PlacedByHand = true;
    var cellar = AddRoomAt("Cellar", 0, 1);
    Join(hall, CompassPoint.East, garden, CompassPoint.West);
    Join(hall, CompassPoint.South, cellar, CompassPoint.North);
    var gardenWas = garden.Position;

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), hall);

    garden.Position.ShouldBe(gardenWas);
  }

  [Test]
  public void ANewRoomSlightlyOutOfLine_LinesUpWithTheRoomItCameFrom()
  {
    // east from the Hall to the Garden, drawn a little low; north-east from the Garden to the Shed, a little left
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 1, 0.25f);
    Join(hall, CompassPoint.East, garden, CompassPoint.West);

    LayoutTidy.TidyAround(Project.Current.Elements.ToList(), garden).ShouldBe(1);

    garden.Position.Y.ShouldBe(hall.Position.Y);
    LayoutTidy.ExactJoins(Project.Current.Elements.ToList()).ShouldBe((1, 1));
  }

  [Test]
  public void TidyMap_LinesRoomsUp_InRowsColumnsAndDiagonals()
  {
    // all naturally placed already, but out of line
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 1, 0.25f);
    var shed = AddRoomAt("Shed", 1.75f, -1);
    var cellar = AddRoomAt("Cellar", 0.25f, 1);
    Join(hall, CompassPoint.East, garden, CompassPoint.West);
    Join(garden, CompassPoint.NorthEast, shed, CompassPoint.SouthWest);
    Join(hall, CompassPoint.South, cellar, CompassPoint.North);
    var elements = Project.Current.Elements.ToList();
    LayoutTidy.NaturalJoins(elements).ShouldBe((3, 3));
    LayoutTidy.ExactJoins(elements).Exact.ShouldBe(0);

    LayoutTidy.Tidy(elements, elements.OfType<Room>(), 2000, rearrange: true);

    LayoutTidy.ExactJoins(Project.Current.Elements.ToList()).ShouldBe((3, 3));
    Overlaps().ShouldBe(0);
  }

  [Test]
  public void LeesIdealHollywoodHijinxMap_StaysNeat_WhenTidied()
  {
    // Lee laid it out evenly spaced and lined up: tidying the whole map must not make it less neat
    var path = RepositoryFile("tests", "automap", "hollywood-hijinx-nov-6", "ideal.trizbort");
    new MapLoader(Project.Current).LoadMap(path).ShouldBeTrue();
    var exact = LayoutTidy.ExactJoins(Project.Current.Elements.ToList());
    TestContext.Out.WriteLine($"exact before: {exact}");

    var elements = Project.Current.Elements.ToList();
    LayoutTidy.Tidy(elements, elements.OfType<Room>(), 3000, rearrange: true);

    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).Natural.ShouldBe(20);
    LayoutTidy.ExactJoins(Project.Current.Elements.ToList()).Exact.ShouldBeGreaterThanOrEqualTo(exact.Exact);
  }

  [Test]
  public void LeesIdealHollywoodHijinxMap_HasEveryJoinNaturallyPlaced_AndTidyingKeepsItThatWay()
  {
    // HH-NOV-6-ideal.trizbort: Lee's hand layout of the first part of Hollywood Hijinx. Every join points the
    // way its directions say; several are long. Tidying around any room must not make it worse.
    var path = RepositoryFile("tests", "automap", "hollywood-hijinx-nov-6", "ideal.trizbort");
    new MapLoader(Project.Current).LoadMap(path).ShouldBeTrue();
    var elements = Project.Current.Elements.ToList();
    LayoutTidy.NaturalJoins(elements).ShouldBe((20, 20));

    foreach (var room in Project.Current.Elements.OfType<Room>().ToList())
      LayoutTidy.TidyAround(Project.Current.Elements.ToList(), room);

    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).Natural.ShouldBe(20);
    Overlaps().ShouldBe(0);
  }
}
