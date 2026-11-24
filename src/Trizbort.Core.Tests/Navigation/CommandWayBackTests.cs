using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Navigation;

namespace Trizbort.Core.Tests.Navigation;

/// <summary>
///   Lee, 24 Nov 2026: directions from the building back into the cave didn't say "xyzzy". Automap had only seen it
///   from the debris room to the building, but the join is drawn both ways with "xyzzy" on it, and that's how the map
///   reads: the same command back, as a way not yet tried.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CommandWayBackTests : IsolatedProjectTests
{
  private Room Add(string name, float x)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, 0) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private Connection Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide, params JoinWay[] ways)
  {
    var join = new Connection(Project.Current, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide)));
    Project.Current.Elements.Add(join);
    JoinWays.Write(join, ways, DirectionWords.Word);
    return join;
  }

  private (Room Building, Room Debris, Connection Magic) TheCave()
  {
    var building = Add("Inside Building", 0);
    var road = Add("End of Road", 200);
    var grate = Add("Outside Grate", 400);
    var debris = Add("In Debris Room", 600);
    Join(building, CompassPoint.East, road, CompassPoint.West,
      JoinWay.Go(true, MappableDirection.East, true), JoinWay.Go(false, MappableDirection.West, true));
    Join(road, CompassPoint.East, grate, CompassPoint.West,
      JoinWay.Go(true, MappableDirection.East, true), JoinWay.Go(false, MappableDirection.West, true));
    Join(grate, CompassPoint.East, debris, CompassPoint.West,
      JoinWay.Go(true, MappableDirection.Down, true), JoinWay.Go(false, MappableDirection.Up, true));
    var magic = Join(debris, CompassPoint.North, building, CompassPoint.North, JoinWay.Typed(true, "xyzzy"));
    return (building, debris, magic);
  }

  [Test]
  public void ACommandSeenOneWay_OnAJoinDrawnBothWays_IsTakenBack()
  {
    var (building, debris, _) = TheCave();

    var route = RouteFinder.Find(Project.Current.Elements, building, debris)!;

    route.Select(step => step.Command).ShouldBe(new[] { "xyzzy" });
    route.Single().Assumed.ShouldBeTrue();
  }

  [Test]
  public void NotOnAOneWayJoin()
  {
    var (building, debris, magic) = TheCave();
    magic.Flow = ConnectionFlow.OneWay;

    RouteFinder.Find(Project.Current.Elements, building, debris)!.Select(step => step.Command)
      .ShouldBe(new[] { "east", "east", "down" });
  }

  [Test]
  public void NotWhenTheWayBackIsKnown()
  {
    var (building, debris, magic) = TheCave();
    JoinWays.Write(magic, new[] { JoinWay.Typed(true, "xyzzy"), JoinWay.Typed(false, "plugh") }, DirectionWords.Word);

    RouteFinder.Find(Project.Current.Elements, building, debris)!.Select(step => step.Command)
      .ShouldBe(new[] { "plugh" });
  }
}
