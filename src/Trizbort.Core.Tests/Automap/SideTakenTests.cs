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
///   Hollywood Hijinx (Lee, 16 Nov 2026): the Foyer is drawn away from the grounds, joined south to the Front Porch by
///   a split join. In and out of the Closet put it below and to the right of the Foyer; going north from the Closet,
///   which also leads to the Foyer, then moved the Closet right underneath, as if south from the Foyer led there. It
///   doesn't: south is the Front Porch.
/// </summary>
[TestFixture]
[Category("Unit")]
public class SideTakenTests : IsolatedProjectTests
{
  private static Room AddRoom(string name, int x, int y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    room.AddDescription($"The {name.ToLowerInvariant()}.");
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public async Task AWayBackToASideAlreadyTaken_DoesNotPullTheRoomUnderneath()
  {
    var porch = AddRoom("Front Porch", 0, 0);
    var foyer = AddRoom("Foyer", 1600, -400);
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(porch.PortAt(CompassPoint.North)),
      new Vertex(foyer.PortAt(CompassPoint.South))) { Split = true });

    var path = Files.File("game.txt");
    File.WriteAllText(path, ">look\n\nFoyer\nThe foyer.\n\n>in\n\nCloset\nThe closet.\n\n>out\n\nFoyer\nThe foyer.\n\n" +
                            ">in\n\nCloset\nThe closet.\n\n>n\n\nFoyer\nThe foyer.\n\n>");
    var settings = AutomapSettings.Default;
    settings.GuessExits = false;
    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, null);
    result.Succeeded.ShouldBeTrue(result.Error);

    var closet = Project.Current.Elements.OfType<Room>().Single(room => room.Name == "Closet");

    // the Closet isn't put underneath the Foyer: nothing new uses the Foyer's south side, the Front Porch's
    var southOfFoyer = Project.Current.Elements.OfType<Connection>().Where(c => !c.Split).Count(c =>
      (c.GetSourceRoom(out var pa) == foyer && pa == CompassPoint.South) ||
      (c.GetTargetRoom(out var pb) == foyer && pb == CompassPoint.South));
    southOfFoyer.ShouldBe(0);
    var foyerCentre = foyer.InnerBounds.Center;
    var closetCentre = closet.InnerBounds.Center;
    (closetCentre.Y > foyerCentre.Y && System.Math.Abs(closetCentre.X - foyerCentre.X) < 1).ShouldBeFalse(
      "the Closet was put right underneath the Foyer");
  }
}
