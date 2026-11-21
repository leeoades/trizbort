using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Navigation;

namespace Trizbort.Core.Tests.Navigation;

/// <summary>
///   Lee, 21 Nov 2026: some joins don't always lead where they're drawn (Bedquilt's passages in Colossal Cave go
///   somewhere at random; Hollywood Hijinx's closet is on whichever floor it was left at). Directions take them only
///   when there's no other way: a join marked Unreliable, and a command the map shows leading to more than one room.
/// </summary>
[TestFixture]
[Category("Unit")]
public class UnreliableJoinTests : IsolatedProjectTests
{
  private Room Add(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private Connection Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide, string startText = null)
  {
    var join = new Connection(Project.Current, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide)))
    {
      StartText = startText ?? string.Empty
    };
    Project.Current.Elements.Add(join);
    return join;
  }

  private string[] Commands(Room from, Room to) =>
    RouteFinder.Find(Project.Current.Elements, from, to)?.Select(step => step.Command).ToArray();

  [Test]
  public void AJoinMarkedUnreliable_IsTakenOnlyWhenTheresNoOtherWay()
  {
    // the closet goes straight up to the attic; the stairs go round by the landing
    var hall = Add("Hall", 0, 400);
    var landing = Add("Landing", 200, 200);
    var attic = Add("Attic", 0, 0);
    var closet = Join(hall, CompassPoint.North, attic, CompassPoint.South, "enter closet");
    Join(hall, CompassPoint.East, landing, CompassPoint.South);
    Join(landing, CompassPoint.North, attic, CompassPoint.East);

    Commands(hall, attic).ShouldBe(new[] { "enter closet" });

    closet.Unreliable = true;
    Commands(hall, attic).ShouldBe(new[] { "east", "north" });

    // no other way: it's taken, and said to be unreliable
    Project.Current.Elements.Remove(Project.Current.Elements.OfType<Connection>().Last());
    var route = RouteFinder.Find(Project.Current.Elements, hall, attic);
    route.ShouldHaveSingleItem().Command.ShouldBe("enter closet");
    route[0].Unreliable.ShouldBeTrue();
  }

  [Test]
  public void ACommandLeadingToMoreThanOneRoom_IsUnreliable()
  {
    // Bedquilt's north leads to the large low room one time and the slab room another
    var bedquilt = Add("Bedquilt", 200, 200);
    var low = Add("Large Low Room", 200, 0);
    var slab = Add("Slab Room", 0, 0);
    Join(bedquilt, CompassPoint.North, low, CompassPoint.South);
    Join(bedquilt, CompassPoint.NorthWest, slab, CompassPoint.SouthEast, "north");
    // and the slab room is also reached reliably, the long way
    var junction = Add("Junction", 0, 200);
    Join(bedquilt, CompassPoint.West, junction, CompassPoint.East);
    Join(junction, CompassPoint.North, slab, CompassPoint.South);

    Commands(bedquilt, slab).ShouldBe(new[] { "west", "north" });
    var toLow = RouteFinder.Find(Project.Current.Elements, bedquilt, low);
    toLow.ShouldHaveSingleItem().Command.ShouldBe("north");
    toLow[0].Unreliable.ShouldBeTrue();
  }

  [Test]
  public void Unreliable_IsSavedWithTheMap_AndOnlyWhenSet()
  {
    var a = Add("A", 0, 0);
    var b = Add("B", 200, 0);
    var c = Add("C", 400, 0);
    Join(a, CompassPoint.East, b, CompassPoint.West);
    Join(b, CompassPoint.East, c, CompassPoint.West).Unreliable = true;

    var path = Files.File("map.trizbort");
    new LegacyMapFileEngine(Project.Current).Save(path).ShouldBeTrue();
    var text = System.IO.File.ReadAllText(path);
    text.Split("unreliable=").Length.ShouldBe(2);

    using var reopened = new Project();
    PersistenceRegressionTests.Load(reopened, path);
    reopened.Elements.OfType<Connection>().Count(join => join.Unreliable).ShouldBe(1);
  }
}
