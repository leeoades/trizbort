using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Automap.Utility;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export.Languages;
using Trizbort.Setup;
using Trizbort.UI;
using Trizbort.UI.Controls;
using Parser = Trizbort.Automap.Automap;

namespace Trizbort.Tests {
  [TestFixture, Category("Integration")]
  public class AutomapRegressionTests : IsolatedProjectTests {
    private static Parser CreateParser() => new Parser(
      (message, title) => throw new AssertionException(title + ": " + message),
      (_, __) => throw new AssertionException("Unexpected ambiguity"));

    private AutomapSettings SettingsFor(string text, bool twoWay = true, bool guess = false) {
      var settings = AutomapSettings.Default;
      settings.FileName = Files.File("transcript.txt");
      File.WriteAllText(settings.FileName, text);
      settings.AssumeRoomsWithSameNameAreSameRoom = true;
      settings.AssumeTwoWayConnections = twoWay;
      settings.GuessExits = guess;
      return settings;
    }

    [TestCase(">north", "north")]
    [TestCase("Score: 0 > go east", "go east")]
    [TestCase("> ", "")]
    [TestCase(">> west", "west")]
    public void Prompt_ExtractsTypedCommand(string line, string expected) {
      CreateParser().IsPrompt(line, out var command).ShouldBeTrue();
      command.ShouldBe(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("There is no prompt")]
    [TestCase("                                          >n")]
    public void Prompt_RejectsNonPromptsAndLateMarkers(string line) {
      CreateParser().IsPrompt(line, out var command).ShouldBeFalse();
      command.ShouldBeNull();
    }

    [TestCase("West of House", "West of House")]
    [TestCase("Bedroom, on the bed", "Bedroom")]
    [TestCase("Bedroom (on the bed)", "Bedroom")]
    [TestCase("Bedroom [on the bed]", "Bedroom")]
    [TestCase("Bedroom - on the bed", "Bedroom")]
    [TestCase("Room 12", "Room 12")]
    public void RoomTitle_StripsDecorationsButKeepsRoomIdentity(string line, string expected) {
      CreateParser().ExtractRoomName(line, "", out var name).ShouldBeTrue();
      name.ShouldBe(expected);
    }

    [TestCase(" bedroom")]
    [TestCase("bedroom")]
    [TestCase("GAME OVER")]
    [TestCase("A very ordinary sentence about a room.")]
    [TestCase("")]
    [TestCase(null)]
    public void RoomTitle_RejectsWhitespaceProseAndBanners(string line) {
      CreateParser().ExtractRoomName(line, "", out _).ShouldBeFalse();
    }

    [Test]
    public void Description_ContinuesWrappedLines_AndStopsAtBlankOrPrompt() {
      var first = "you are standing in a large hallway with polished floors and several doors opening onto it.";
      var parser = CreateParser();
      parser.ExtractParagraph(new List<string> {first, "the light is dim.", ">east"}, 0, out var paragraph).ShouldBeTrue();
      paragraph.ShouldBe(first + " the light is dim.");
      parser.ExtractParagraph(new List<string> {"[Previous turn undone.]", ""}, 0, out paragraph).ShouldBeFalse();
      paragraph.ShouldBeNull();
    }

    [TestCase("n", MappableDirection.North)]
    [TestCase("go to the north", MappableDirection.North)]
    [TestCase("walk east", MappableDirection.East)]
    [TestCase("move west", MappableDirection.West)]
    [TestCase("fore", MappableDirection.North)]
    [TestCase("aft", MappableDirection.South)]
    [TestCase("starboard", MappableDirection.East)]
    [TestCase("port", MappableDirection.West)]
    [TestCase("ne", MappableDirection.NorthEast)]
    [TestCase("nw", MappableDirection.NorthWest)]
    [TestCase("se", MappableDirection.SouthEast)]
    [TestCase("sw", MappableDirection.SouthWest)]
    [TestCase("up", MappableDirection.Up)]
    [TestCase("down", MappableDirection.Down)]
    [TestCase("inside", MappableDirection.In)]
    [TestCase("outside", MappableDirection.Out)]
    public async Task MovementAliases_CreateRealDockedGraph_AndProcessFinalRoomAtEOF(string command, MappableDirection direction) {
      using (var canvas = new Canvas()) {
        var parser = CreateParser();
        await parser.StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>" + command + "\n\nSecond Room\nAnother room.\n"));
        var rooms = Project.Current.Elements.OfType<Room>().ToArray();
        rooms.Length.ShouldBe(2);
        rooms.Single(room => room.Name == "First Room").IsStartRoom.ShouldBeTrue();
        var connection = Project.Current.Elements.OfType<Connection>().Single();
        connection.GetSourceRoom(out var port).Name.ShouldBe("First Room");
        port.ShouldBe(CompassPointHelper.GetCompassDirection(direction));
        connection.GetTargetRoom().Name.ShouldBe("Second Room");
        connection.Flow.ShouldBe(ConnectionFlow.TwoWay);
        connection.IsDangling.ShouldBeFalse();
      }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RevisitingRooms_DoesNotDuplicateGraph_AndReturnTravelMakesConnectionTwoWay(bool twoWay) {
      using (var canvas = new Canvas()) {
        var parser = CreateParser();
        await parser.StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\n\nSecond Room\nAnother room.\n\n>w\n\nFirst Room\nA small room.\n", twoWay));
        Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
        Project.Current.Elements.OfType<Connection>().Single().Flow.ShouldBe(ConnectionFlow.TwoWay);
      }
    }

    [Test]
    public async Task OneWaySettings_ArePreservedThroughSaveLoadAndExport() {
      using (var canvas = new Canvas()) {
        await CreateParser().StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\n\nSecond Room\nAnother room.\n", false));
        Project.Current.Elements.OfType<Connection>().Single().Flow.ShouldBe(ConnectionFlow.OneWay);
        new LegacyMapFileEngine(Project.Current).Save(Files.File("automap.trizbort")).ShouldBeTrue();
        var loaded = new Project();
        PersistenceRegressionTests.Load(loaded, Files.File("automap.trizbort"));
        Project.Current = loaded;
        using (var exporter = new Inform7Exporter()) {
          var output = exporter.Export();
          output.ShouldContain("First Room");
          output.ShouldContain("Second Room");
          output.ShouldContain("nowhere");
        }
        loaded.Elements.OfType<Connection>().Single().Flow.ShouldBe(ConnectionFlow.OneWay);
      }
    }

    [Test]
    public async Task ObjectRegionAndExitCommands_ModifyCurrentRoomWithoutCreatingRooms() {
      using (var canvas = new Canvas()) {
        await CreateParser().StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>tb see Key\n>tb see key\n>tb region Forest\n>tb exit north\n>tb noexit north\n"));
        var room = Project.Current.Elements.OfType<Room>().Single();
        room.Objects.ShouldBe("Key");
        room.Region.ShouldBe("Forest");
        Settings.Regions.Count(region => region.RegionName == "Forest").ShouldBe(1);
        Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();
      }
    }

    [Test]
    public async Task FailedMoveAndDisconnectedTravel_DoNotInventConnections() {
      using (var canvas = new Canvas()) {
        await CreateParser().StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\nYou cannot go that way.\n\n>look\n\nFirst Room\nA small room.\n\n>teleport\n\nDistant Room\nFar away.\n"));
        Project.Current.Elements.OfType<Room>().Select(room => room.Name).ShouldBe(new[] {"First Room", "Distant Room"});
        Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();
      }
    }

    [Test]
    public async Task GuessExits_CreatesDirectionalStubsOnlyWhenEnabled() {
      using (var canvas = new Canvas()) {
        await CreateParser().StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nExits lead north and east.\n", guess: true));
        var lines = Project.Current.Elements.OfType<Connection>().ToArray();
        lines.Length.ShouldBe(2);
        lines.All(line => line.GetSourceRoom() == line.GetTargetRoom()).ShouldBeTrue();
      }
    }

    [Test]
    public async Task ConsecutiveRuns_DoNotReusePreviousRoomDirectionOrGameTitle() {
      using (var canvas = new Canvas()) {
        var parser = CreateParser();
        await parser.StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\n"));
        Project.Current = new Project();
        await parser.StartCL(canvas, SettingsFor("Different Game\n\nNew Room\nA different room.\n"));
        Project.Current.Elements.OfType<Room>().Single().Name.ShouldBe("New Room");
        Project.Current.Elements.OfType<Room>().Single().IsStartRoom.ShouldBeTrue();
        Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();
      }
    }

    [Test]
    public async Task MissingTranscript_ReportsErrorAndDoesNotClaimCompletion() {
      string failure = null;
      var parser = new Parser((message, _) => failure = message, (_, __) => AutomapSameDirectionResult.KeepRoom1);
      using (var canvas = new Canvas()) {
        var settings = AutomapSettings.Default;
        settings.FileName = Files.File("missing.txt");
        await parser.StartCL(canvas, settings);
        failure.ShouldContain("Error opening transcript");
        parser.Status.ShouldBe("Automapping halted.");
        Project.Current.Elements.ShouldBeEmpty();
      }
    }

    [TestCase(AutomapSameDirectionResult.KeepRoom1)]
    [TestCase(AutomapSameDirectionResult.KeepRoom2)]
    [TestCase(AutomapSameDirectionResult.KeepBoth)]
    public async Task ConflictDecision_UsesChosenRoomAsSourceOfNextMovement(AutomapSameDirectionResult decision) {
      using (var canvas = new Canvas()) {
        var first = ProjectRegressionTests.AddRoom("First Room");
        var original = ProjectRegressionTests.AddRoom("Original Room");
        first.Position = Vector.Zero;
        original.Position = new Vector(200, 0);
        ProjectRegressionTests.Connect(first, original);
        var decisions = 0;
        var parser = new Parser((message, _) => throw new AssertionException(message),
          (existing, name) => {
            existing.ShouldBeSameAs(original);
            name.ShouldBe("Replacement Room");
            decisions++;
            return decision;
          });
        await parser.StartCL(canvas, SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\n\nReplacement Room\nAnother room.\n\n>n\n\nThird Room\nThird description.\n"));
        decisions.ShouldBe(1);
        var third = Project.Current.Elements.OfType<Room>().Single(room => room.Name == "Third Room");
        var next = Project.Current.Elements.OfType<Connection>().Single(line => line.GetTargetRoom() == third);
        next.GetSourceRoom().Name.ShouldBe(decision == AutomapSameDirectionResult.KeepRoom1 ? "Original Room" : "Replacement Room");
        Project.Current.Elements.Contains(original).ShouldBe(decision != AutomapSameDirectionResult.KeepRoom2);
      }
    }

    [Test]
    public async Task LiveReader_CanBeStoppedWhileWaitingForText() {
      using (var canvas = new Canvas()) {
        var parser = CreateParser();
        var run = parser.Start(canvas, SettingsFor(""));
        parser.Running.ShouldBeTrue();
        parser.Stop();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
        parser.Running.ShouldBeFalse();
      }
    }

    [Test]
    public async Task LiveReader_CanBeStoppedWhileSingleStepping() {
      using (var canvas = new Canvas())
      using (var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5))) {
        var parser = CreateParser();
        var settings = SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\n");
        settings.SingleStep = true;
        var run = parser.Start(canvas, settings);
        try {
          while (!parser.Status.Contains("waiting for you to step")) {
            deadline.Token.ThrowIfCancellationRequested();
            run.IsCompleted.ShouldBeFalse();
            await Task.Yield();
          }
          parser.Running.ShouldBeTrue();
        } finally {
          parser.Stop();
          await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
        parser.Running.ShouldBeFalse();
      }
    }

    [Test]
    public async Task PeekingReader_PreservesLookaheadOrderAndEOF_ForSyncAndAsyncReads() {
      using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("first\nsecond\nthird")))
      using (var reader = new PeekingStreamReader(stream)) {
        reader.PeekReadLine().ShouldBe("first");
        reader.PeekReadLine().ShouldBe("second");
        reader.ReadLine().ShouldBe("first");
        (await reader.ReadLineAsync()).ShouldBe("second");
        reader.ReadLine().ShouldBe("third");
        reader.PeekReadLine().ShouldBeNull();
        reader.ReadLine().ShouldBeNull();
      }
    }
  }
}
