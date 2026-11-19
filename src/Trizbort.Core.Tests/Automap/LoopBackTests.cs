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
///   Lee, 19 Nov 2026: a Maze room with a loop labelled "s" (south leads straight back in). Going south always asked
///   which Maze it was: the map's ways ignored loops. A loop that lists the way says the player is still there.
/// </summary>
[TestFixture]
[Category("Unit")]
public class LoopBackTests : IsolatedProjectTests
{
  private const string Alike = "You are in a maze of twisty little passages, all alike.";

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

  [TestCase(true)]
  [TestCase(false)]
  public async Task GoingTheWayALoopShows_StaysInTheRoom(bool oneWay)
  {
    var hall = AddRoom("Hall", 0, 0, "A hall.");
    var maze = AddRoom("Maze", 320, 0, Alike);
    AddRoom("Maze", 640, 320, Alike);
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(hall.PortAt(CompassPoint.East)),
      new Vertex(maze.PortAt(CompassPoint.West))));
    var loop = new Connection(Project.Current, new Vertex(maze.PortAt(CompassPoint.South)),
      new Vertex(maze.PortAt(CompassPoint.SouthSouthEast)))
    {
      StartText = "s",
      Flow = oneWay ? ConnectionFlow.OneWay : ConnectionFlow.TwoWay
    };
    Project.Current.Elements.Add(loop);
    var connections = Project.Current.Elements.OfType<Connection>().Count();
    var prompts = new Counted();
    var path = Path.GetTempFileName();
    File.WriteAllText(path, $">look\n\nHall\nA hall.\n\n>e\n\nMaze\n{Alike}\n\n>s\n\nMaze\n{Alike}\n\n>s\n\nMaze\n{Alike}\n\n>");
    var settings = AutomapSettings.Default;
    settings.GuessExits = false;
    settings.AutoTidy = false;

    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, prompts);

    prompts.Asked.ShouldBe(0);
    result.CurrentRoom.ShouldBe(maze);
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(3);
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(connections);
  }
}
