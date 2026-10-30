using System;
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
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Parser = Trizbort.Automap.Automap;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Integration")]
public class AutomapPromptTests : IsolatedProjectTests
{
  private sealed class ScriptedPrompts : IEditorPrompts
  {
    public readonly Queue<AutomapSameDirectionResult> SameDirectionResults = new();
    public readonly Queue<Func<IReadOnlyList<Room>, RoomDisambiguation>> DisambiguationResults = new();
    public readonly List<(Room Existing, string NewName)> SameDirectionCalls = new();
    public readonly List<(string Line, string Description, IReadOnlyList<Room> Candidates)> DisambiguationCalls = new();
    public Task Gate;

    public async Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Room existing,
      string newRoomName,
      CancellationToken token = default)
    {
      SameDirectionCalls.Add((existing, newRoomName));
      if (Gate != null) await Gate;
      return SameDirectionResults.Dequeue();
    }

    public async Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine,
      string roomDescription,
      IReadOnlyList<Room> candidates,
      CancellationToken token = default)
    {
      DisambiguationCalls.Add((transcriptLine, roomDescription, candidates));
      if (Gate != null) await Gate;
      return DisambiguationResults.Dequeue()(candidates);
    }
  }

  private static Parser CreateParser(IEditorPrompts prompts)
  {
    return new Parser((message, title) => throw new AssertionException(title + ": " + message), prompts);
  }

  private AutomapSettings SettingsFor(string text, bool sameNameIsSameRoom)
  {
    var settings = AutomapSettings.Default;
    settings.FileName = Files.File("transcript.txt");
    File.WriteAllText(settings.FileName, text);
    settings.VerboseTranscript = true;
    settings.AssumeRoomsWithSameNameAreSameRoom = sameNameIsSameRoom;
    settings.AssumeTwoWayConnections = true;
    settings.GuessExits = false;
    return settings;
  }

  private const string SameDirectionTranscript =
    "Example Game\n\nFirst Room\nA small room.\n\n>e\n\nReplacement Room\nAnother room.\n\n>n\n\nThird Room\nThird description.\n";

  private static Room AddSameDirectionRooms(out Room original)
  {
    var first = ProjectRegressionTests.AddRoom("First Room");
    original = ProjectRegressionTests.AddRoom("Original Room");
    first.Position = Vector.Zero;
    original.Position = new Vector(200, 0);
    ProjectRegressionTests.Connect(first, original);
    return first;
  }

  private static Room AddHall(string description)
  {
    var room = ProjectRegressionTests.AddRoom("Hall");
    if (description != null) room.AddDescription(description);
    return room;
  }

  private static string[] RoomNames()
  {
    return Project.Current.Elements.OfType<Room>().Select(room => room.Name).ToArray();
  }

  [Test]
  public async Task SameDirection_KeepRoom1_ReusesExistingRoom()
  {
    AddSameDirectionRooms(out var original);
    var prompts = new ScriptedPrompts();
    prompts.SameDirectionResults.Enqueue(AutomapSameDirectionResult.KeepRoom1);

    await CreateParser(prompts).StartCl(new AutomapCanvas(), SettingsFor(SameDirectionTranscript, true));

    prompts.SameDirectionCalls.Count.ShouldBe(1);
    prompts.SameDirectionCalls[0].Existing.ShouldBeSameAs(original);
    prompts.SameDirectionCalls[0].NewName.ShouldBe("Replacement Room");
    RoomNames().ShouldNotContain("Replacement Room");
    Project.Current.Elements.OfType<Room>().ShouldContain(original);
  }

  [Test]
  public async Task SameDirection_KeepRoom2_CreatesNewRemovesOld()
  {
    AddSameDirectionRooms(out var original);
    var prompts = new ScriptedPrompts();
    prompts.SameDirectionResults.Enqueue(AutomapSameDirectionResult.KeepRoom2);

    await CreateParser(prompts).StartCl(new AutomapCanvas(), SettingsFor(SameDirectionTranscript, true));

    RoomNames().ShouldContain("Replacement Room");
    Project.Current.Elements.OfType<Room>().ShouldNotContain(original);
  }

  [Test]
  public async Task SameDirection_KeepBoth_CreatesNewKeepsOld()
  {
    AddSameDirectionRooms(out var original);
    var prompts = new ScriptedPrompts();
    prompts.SameDirectionResults.Enqueue(AutomapSameDirectionResult.KeepBoth);

    await CreateParser(prompts).StartCl(new AutomapCanvas(), SettingsFor(SameDirectionTranscript, true));

    RoomNames().ShouldContain("Replacement Room");
    Project.Current.Elements.OfType<Room>().ShouldContain(original);
  }

  [Test]
  public async Task SameDirection_Cancel_StopsRun()
  {
    AddSameDirectionRooms(out _);
    var prompts = new ScriptedPrompts();
    prompts.SameDirectionResults.Enqueue(AutomapSameDirectionResult.Cancel);
    var parser = CreateParser(prompts);

    await parser.StartCl(new AutomapCanvas(), SettingsFor(SameDirectionTranscript, true));

    prompts.SameDirectionCalls.Count.ShouldBe(1);
    RoomNames().ShouldNotContain("Replacement Room");
    RoomNames().ShouldNotContain("Third Room");
    parser.Running.ShouldBeFalse();
    parser.Status.ShouldBe("Automap is not running.");
  }

  [Test]
  public async Task Disambiguate_Chosen_UsesChosenRoom()
  {
    AddHall("Alpha hall.");
    var second = AddHall("Beta hall.");
    var prompts = new ScriptedPrompts();
    prompts.DisambiguationResults.Enqueue(candidates => RoomDisambiguation.Choose(candidates[1]));

    await CreateParser(prompts)
      .StartCl(new AutomapCanvas(), SettingsFor("Example Game\n\nHall\nZulu description.\n", false));

    prompts.DisambiguationCalls.Count.ShouldBe(1);
    prompts.DisambiguationCalls[0].Line.ShouldBe("Hall");
    prompts.DisambiguationCalls[0].Candidates.Count.ShouldBe(2);
    RoomNames().Length.ShouldBe(2);
    second.Descriptions.ShouldContain("Zulu description.");
  }

  [Test]
  public async Task Disambiguate_NewRoom_CreatesRoom()
  {
    AddHall("Alpha hall.");
    AddHall("Beta hall.");
    var prompts = new ScriptedPrompts();
    prompts.DisambiguationResults.Enqueue(_ => RoomDisambiguation.CreateNew());

    await CreateParser(prompts)
      .StartCl(new AutomapCanvas(), SettingsFor("Example Game\n\nHall\nZulu description.\n", false));

    prompts.DisambiguationCalls.Count.ShouldBe(1);
    RoomNames().Length.ShouldBe(3);
  }

  [Test]
  public async Task Disambiguate_AutoChooseRest_AsksOnlyOnce()
  {
    var first = AddHall("Alpha hall.");
    AddHall("Beta hall.");
    var prompts = new ScriptedPrompts();
    prompts.DisambiguationResults.Enqueue(candidates => RoomDisambiguation.ChooseFirstAndStopAsking(candidates[0]));

    await CreateParser(prompts)
      .StartCl(
        new AutomapCanvas(),
        SettingsFor("Example Game\n\nHall\nZulu description.\n\n>look\n\nHall\nYankee description.\n", false));

    prompts.DisambiguationCalls.Count.ShouldBe(1);
    RoomNames().Length.ShouldBe(2);
    first.Descriptions.ShouldContain("Zulu description.");
    first.Descriptions.ShouldContain("Yankee description.");
  }

  [Test]
  public async Task Disambiguate_SingleCandidateNoDescription_DoesNotAsk()
  {
    var existing = AddHall(null);
    var prompts = new ScriptedPrompts();

    await CreateParser(prompts)
      .StartCl(new AutomapCanvas(), SettingsFor("Example Game\n\nHall\nZulu description.\n", false));

    prompts.DisambiguationCalls.ShouldBeEmpty();
    RoomNames().Length.ShouldBe(1);
    existing.Descriptions.ShouldContain("Zulu description.");
  }

  [Test]
  public async Task Disambiguate_FlagResetsOnNextRun()
  {
    AddHall("Alpha hall.");
    AddHall("Beta hall.");
    var prompts = new ScriptedPrompts();
    prompts.DisambiguationResults.Enqueue(candidates => RoomDisambiguation.ChooseFirstAndStopAsking(candidates[0]));
    prompts.DisambiguationResults.Enqueue(candidates => RoomDisambiguation.Choose(candidates[0]));
    var parser = CreateParser(prompts);
    var canvas = new AutomapCanvas();

    await parser.StartCl(canvas, SettingsFor("Example Game\n\nHall\nZulu description.\n", false));
    prompts.DisambiguationCalls.Count.ShouldBe(1);

    await parser.StartCl(canvas, SettingsFor("Example Game\n\nHall\nXray description.\n", false));
    prompts.DisambiguationCalls.Count.ShouldBe(2);
  }

  [Test]
  public async Task Prompts_AreAwaitedWithoutBlocking()
  {
    AddSameDirectionRooms(out var original);
    var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var prompts = new ScriptedPrompts { Gate = gate.Task };
    prompts.SameDirectionResults.Enqueue(AutomapSameDirectionResult.KeepRoom2);

    var run = CreateParser(prompts).StartCl(new AutomapCanvas(), SettingsFor(SameDirectionTranscript, true));
    // Wait until the engine is parked on the prompt, then check it is not blocking or finishing.
    for (var i = 0; i < 100 && prompts.SameDirectionCalls.Count == 0; i++) await Task.Delay(50);
    prompts.SameDirectionCalls.Count.ShouldBe(1);
    await Task.Delay(100);
    run.IsCompleted.ShouldBeFalse();
    RoomNames().ShouldNotContain("Replacement Room");

    gate.TrySetResult(true);
    await run.WaitAsync(TimeSpan.FromSeconds(10));

    RoomNames().ShouldContain("Replacement Room");
    Project.Current.Elements.OfType<Room>().ShouldNotContain(original);
  }

  [Test]
  public async Task NonInteractive_KeepsLegacyDefaults()
  {
    AddSameDirectionRooms(out _);
    AddHall("Alpha hall.");
    AddHall("Beta hall.");
    var parser = CreateParser(NonInteractiveEditorPrompts.Instance);

    await parser.StartCl(new AutomapCanvas(), SettingsFor(SameDirectionTranscript, true));
    RoomNames().ShouldNotContain("Replacement Room");

    await parser.StartCl(
      new AutomapCanvas(),
      SettingsFor("Example Game\n\nHall\nZulu description.\n", false));
    RoomNames().Count(name => name == "Hall").ShouldBe(2);

    new Parser().Prompts.ShouldBeSameAs(NonInteractiveEditorPrompts.Instance);
  }
}
