using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Hollywood Hijinx (Lee, 15 Nov 2026): the house drawn away from its grounds, joined to the front porch by a split
///   join. Going out of the house onto the patio made a long join right across the map, and tidying then dragged the
///   house towards the gardens. A join between areas set apart like that is split too.
/// </summary>
[TestFixture]
[Category("Unit")]
public class SplitJoinBetweenAreasTests : IsolatedProjectTests
{
  private static Room AddRoom(string name, int x, int y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    room.AddDescription($"The {name.ToLowerInvariant()}.");
    Project.Current.Elements.Add(room);
    return room;
  }

  private static Connection Join(Room a, CompassPoint from, Room b, CompassPoint to, bool split = false)
  {
    var join = new Connection(Project.Current, new Vertex(a.PortAt(from)), new Vertex(b.PortAt(to))) { Split = split };
    Project.Current.Elements.Add(join);
    return join;
  }

  private static Connection Between(Room a, Room b) => Project.Current.Elements.OfType<Connection>().Single(c =>
    (c.GetSourceRoom() == a && c.GetTargetRoom() == b) || (c.GetSourceRoom() == b && c.GetTargetRoom() == a));

  private async Task Play(string transcript)
  {
    var path = Files.File("game.txt");
    File.WriteAllText(path, transcript);
    var settings = AutomapSettings.Default;
    settings.GuessExits = false;
    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, null);
    result.Succeeded.ShouldBeTrue(result.Error);
  }

  [Test]
  public async Task AWayFromOneAreaToAnother_SetApartWithASplitJoin_IsSplitToo_AndNothingIsDraggedAcross()
  {
    // the grounds
    var porch = AddRoom("Front Porch", 0, 0);
    var patio = AddRoom("Patio", 0, -256);
    Join(porch, CompassPoint.North, patio, CompassPoint.South);
    // the house, drawn well away to the right, joined to the porch by a split join
    var foyer = AddRoom("Foyer", 1600, 0);
    var living = AddRoom("Living Room", 1600, -128);
    Join(foyer, CompassPoint.North, living, CompassPoint.South);
    Join(porch, CompassPoint.NorthNorthEast, foyer, CompassPoint.South, split: true);
    var patioAt = patio.Position;
    var livingAt = living.Position;

    await Play(">look\n\nLiving Room\nThe living room.\n\n>south\n\nFoyer\nThe foyer.\n\n" +
               ">north\n\nLiving Room\nThe living room.\n\n>west\n\nPatio\nThe patio.\n\n>");

    var outside = Between(living, patio);
    outside.Split.ShouldBeTrue();
    // the areas stay where they were
    patio.Position.ShouldBe(patioAt);
    living.Position.X.ShouldBe(livingAt.X, 300);
  }

  [Test]
  public async Task RoomsThatSimplyHaventBeenJoined_GetAnOrdinaryJoin()
  {
    var hall = AddRoom("Hall", 0, 0);
    var garden = AddRoom("Garden", 1600, 0);

    await Play(">look\n\nHall\nThe hall.\n\n>east\n\nGarden\nThe garden.\n\n>");

    Between(hall, garden).Split.ShouldBeFalse();
  }
}
