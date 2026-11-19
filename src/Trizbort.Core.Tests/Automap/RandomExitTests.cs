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
///   Lee, 19 Nov 2026: north from Bedquilt (Colossal Cave) leads, at random, to the Large Low Room or the Junction of
///   Three Secret Canyons, and the map shows both. Each time, automap asked whether to keep both. A way the map already
///   shows leading to the room the player reached asks nothing.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RandomExitTests : IsolatedProjectTests
{
  private sealed class Counted : IEditorPrompts
  {
    public int SameWay { get; private set; }

    public Task<AutomapSameDirectionResult> ChooseSameWayAsync(Room from, string way, Room existing, string newRoomName,
      CancellationToken token = default)
    {
      SameWay++;
      return Task.FromResult(AutomapSameDirectionResult.SamePlace);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default) =>
      Task.FromResult(RoomDisambiguation.CreateNew());

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      Task.FromResult(true);
  }

  private static Room Add(string name, int x, int y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    room.AddDescription($"You are in {name}.");
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public async Task OnceTheMapShowsAWayLeadingToTwoRooms_ItAsksNoMore_WhicheverItLeadsTo()
  {
    Add("Bedquilt", 0, 0);
    var project = Project.Current;
    string Visit(string name) => $"\n\n{name}\nYou are in {name}.\n\n";
    var path = Files.File("game.txt");
    File.WriteAllText(path,
      ">look" + Visit("Bedquilt") +
      ">n" + Visit("Large Low Room") + ">s" + Visit("Bedquilt") +
      // north again, and somewhere else: asked, once, and both kept
      ">n" + Visit("Junction") + ">s" + Visit("Bedquilt") +
      // from now on the map shows north leading to both
      ">n" + Visit("Large Low Room") + ">s" + Visit("Bedquilt") +
      ">n" + Visit("Junction") + ">s" + Visit("Bedquilt") + ">");
    var settings = AutomapSettings.Default;
    settings.AutoTidy = false;
    settings.GuessExits = false;
    var prompts = new Counted();

    var result = await HeadlessAutomap.RunAsync(project, path, settings, prompts);

    result.Succeeded.ShouldBeTrue(result.Error);
    prompts.SameWay.ShouldBe(1);
    project.Elements.OfType<Room>().Select(room => room.Name)
      .ShouldBe(new[] { "Bedquilt", "Large Low Room", "Junction" }, ignoreOrder: true);
    result.CurrentRoom.Name.ShouldBe("Bedquilt");
  }
}
