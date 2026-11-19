using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 19 Nov 2026: a dotted "?" join (a way through automap didn't map) from a Maze to a Dead End, labelled "east /
///   south" by hand. Going east asked which Dead End it was, as if the map said nothing, and choosing the Dead End
///   added a new join east and took "east" off the label. A way through labelled with directions leads those ways,
///   and going that way is recorded on it.
/// </summary>
[TestFixture]
[Category("Unit")]
public class LabelledWayThroughTests : IsolatedProjectTests
{
  private sealed class Counted : IEditorPrompts
  {
    public int Asked { get; private set; }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default)
    {
      Asked++;
      return Task.FromResult(RoomDisambiguation.CreateNew());
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      Task.FromResult(true);
  }

  private static Room AddRoom(string name, int x, int y, string description)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    room.AddDescription(description);
    Project.Current.Elements.Add(room);
    return room;
  }

  [TestCase("east / south", true)]
  [TestCase("", false)]
  public async Task GoingAWayWrittenOnAWayThrough_FollowsIt(string label, bool follows)
  {
    var maze = AddRoom("Twisty Maze", 0, 0, "You are in a maze.");
    var deadEnd = AddRoom("Dead End", 320, 0, "You have reached a dead end.");
    var other = AddRoom("Dead End", 0, 320, "You have reached a dead end.");
    var wayThrough = new Connection(Project.Current, new Vertex(maze.PortAt(CompassPoint.East)),
      new Vertex(deadEnd.PortAt(CompassPoint.West)))
    {
      Style = ConnectionStyle.Dashed,
      AutomapCommand = Trizbort.Automap.Automap.UnmappedWay,
      StartText = label,
      MidText = label.Length == 0 ? Trizbort.Automap.Automap.UnmappedWay : string.Empty
    };
    Project.Current.Elements.Add(wayThrough);
    var prompts = new Counted();
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\n\nTwisty Maze\nYou are in a maze.\n\n>e\n\nDead End\nYou have reached a dead end.\n\n>");
    var settings = AutomapSettings.Default;
    settings.GuessExits = false;
    settings.AutoTidy = false;

    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, prompts);

    if (follows)
    {
      prompts.Asked.ShouldBe(0);
      result.CurrentRoom.ShouldBe(deadEnd);
      Project.Current.Elements.OfType<Room>().Count().ShouldBe(3);
      // and the way is recorded on that join: no new join east, and east isn't taken off it
      Project.Current.Elements.OfType<Connection>().ShouldBe(new[] { wayThrough });
      JoinWays.Of(wayThrough).ShouldContain(way => way.AtStart && way.IsDirection && way.Seen &&
                                                   way.Direction == MappableDirection.East);
    }
    else
    {
      // the side a "?" join leaves from says nothing about where east leads
      prompts.Asked.ShouldBe(1);
    }

    other.ShouldNotBeNull();
  }
}
