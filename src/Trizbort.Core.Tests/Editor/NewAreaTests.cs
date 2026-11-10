using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Editor;

[TestFixture]
[Category("Unit")]
public class NewAreaTests : IsolatedProjectTests
{
  private static Room Add(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void StartingANewAreaAtTheFoyer_MovesItClearOfTheMap_WithPortalsBothWays()
  {
    var garden = Add("Garden", -400, 0);
    var porch = Add("Front Porch", 0, 0);
    var foyer = Add("Foyer", 0, -200);
    var canvas = new AutomapCanvas(Project.Current);
    canvas.Connect(garden, MappableDirection.East, porch, true);
    canvas.Connect(porch, MappableDirection.North, foyer, true);
    var old = foyer.Position;

    var portals = NewArea.Start(Project.Current, foyer);

    portals.Count.ShouldBe(2);
    var toFoyer = portals[0];
    var toPorch = portals[1];
    toFoyer.ReferenceRoom.ShouldBe(foyer);
    toFoyer.IsPortal.ShouldBeTrue();
    toFoyer.Position.ShouldBe(old);
    toPorch.ReferenceRoom.ShouldBe(porch);
    // clear of everything else, the porch's portal where the porch was relative to the foyer
    foyer.Position.X.ShouldBeGreaterThan(porch.Position.X + porch.Width * 2);
    (toPorch.Position - foyer.Position).ShouldBe(porch.Position - old);

    var joins = Project.Current.Elements.OfType<Connection>().Select(c =>
    {
      var source = c.GetSourceRoom(out var start);
      var target = c.GetTargetRoom(out var end);
      return $"{Name(source)} {start} -> {Name(target)} {end}";
    }).ToList();
    joins.ShouldContain("Front Porch North -> To Foyer South");
    joins.ShouldContain("To Front Porch North -> Foyer South");
    joins.ShouldContain("Garden East -> Front Porch West");
    joins.Count.ShouldBe(3);
  }

  private static string Name(Room room) => room.IsPortal ? "To " + room.ReferenceRoom.Name : room.Name;

  [Test]
  public void APortal_OrARoomWithNoJoins_CantStartAnArea()
  {
    var porch = Add("Front Porch", 0, 0);
    NewArea.CanStart(porch).ShouldBeFalse();
    var portal = Add("x", 300, 0);
    portal.AddReferenceRoom(porch.Id);
    NewArea.CanStart(portal).ShouldBeFalse();
  }
}
