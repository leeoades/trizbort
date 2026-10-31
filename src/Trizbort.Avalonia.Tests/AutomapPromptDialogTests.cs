using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class AutomapPromptDialogTests
{
    private Project? _previousProject;
    private Project? _project;
    private string? _transcriptPath;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        _project = new Project();
        Project.Current = _project;
    }

    [TearDown]
    public void TearDown()
    {
        Project.Current = _previousProject!;
        _project?.Dispose();
        _project = null;
        if (_transcriptPath != null && File.Exists(_transcriptPath))
        {
            File.Delete(_transcriptPath);
            _transcriptPath = null;
        }
    }

    private static Room AddRoom(string name, string? description = null)
    {
        var room = new Room(Project.Current) { Name = name };
        if (description != null)
        {
            room.AddDescription(description);
        }

        Project.Current.Elements.Add(room);
        return room;
    }

    private static DisambiguateRoomsViewModel CreateDisambiguation(params Room[] candidates)
    {
        return new DisambiguateRoomsViewModel("Hall", "A hall.", candidates);
    }

    [Test]
    public void SameDirection_Message_HasBothNamesAndClosingQuote()
    {
        var vm = new AutomapSameDirectionViewModel("Kitchen", "Pantry");

        vm.Message.ShouldBe(
            "Room 'Kitchen' is already defined in the same direction as room 'Pantry'.  What would you like to do?");
    }

    [Test]
    public void SameDirection_ButtonLabels_UseRoomNames()
    {
        var vm = new AutomapSameDirectionViewModel("Kitchen", "Pantry");

        vm.KeepExistingLabel.ShouldBe("Keep 'Kitchen'");
        vm.KeepNewLabel.ShouldBe("Keep 'Pantry'");
        vm.KeepBothLabel.ShouldBe("Keep Both");
    }

    [Test]
    public void SameDirection_CloseWithoutChoice_IsKeepRoom1()
    {
        var vm = new AutomapSameDirectionViewModel("Kitchen", "Pantry");

        vm.Result.ShouldBe(AutomapSameDirectionResult.KeepRoom1);
    }

    [Test]
    public void SameDirection_EachButton_ReturnsMatchingResult()
    {
        var vm = new AutomapSameDirectionViewModel("Kitchen", "Pantry");

        vm.KeepNew();
        vm.Result.ShouldBe(AutomapSameDirectionResult.KeepRoom2);
        vm.KeepBoth();
        vm.Result.ShouldBe(AutomapSameDirectionResult.KeepBoth);
        vm.KeepExisting();
        vm.Result.ShouldBe(AutomapSameDirectionResult.KeepRoom1);
    }

    [Test]
    public void Disambiguate_ThisRoom_DisabledUntilSelection()
    {
        var vm = CreateDisambiguation(AddRoom("Hall", "One."), AddRoom("Hall", "Two."));

        vm.ThisRoomCommand.CanExecute(null).ShouldBeFalse();
        vm.SelectedRoom = vm.Candidates[1];
        vm.ThisRoomCommand.CanExecute(null).ShouldBeTrue();
    }

    [Test]
    public void Disambiguate_SingleCandidate_PreSelectedAndEnabled()
    {
        var room = AddRoom("Hall", "One.");
        var vm = CreateDisambiguation(room);

        vm.SelectedRoom.ShouldBeSameAs(room);
        vm.ThisRoomCommand.CanExecute(null).ShouldBeTrue();
    }

    [Test]
    public void Disambiguate_Selection_ShowsPrimaryDescription()
    {
        var vm = CreateDisambiguation(AddRoom("Hall", "One."), AddRoom("Hall", "Two."));

        vm.SelectedDescription.ShouldBe(string.Empty);
        vm.SelectedRoom = vm.Candidates[1];
        vm.SelectedDescription.ShouldBe("Two.");
    }

    [Test]
    public void Disambiguate_ThisRoom_ReturnsChosenRoom()
    {
        var vm = CreateDisambiguation(AddRoom("Hall", "One."), AddRoom("Hall", "Two."));
        vm.SelectedRoom = vm.Candidates[1];

        vm.ThisRoomCommand.Execute(null);

        vm.Result.NewRoom.ShouldBeFalse();
        vm.Result.Chosen.ShouldBeSameAs(vm.Candidates[1]);
    }

    [Test]
    public void Disambiguate_NewRoom_ReturnsCreateNew()
    {
        var vm = CreateDisambiguation(AddRoom("Hall", "One."), AddRoom("Hall", "Two."));

        vm.NewRoomCommand.Execute(null);

        vm.Result.NewRoom.ShouldBeTrue();
        vm.Result.Chosen.ShouldBeNull();
        vm.Result.AutoChooseRest.ShouldBeFalse();
    }

    [Test]
    public void Disambiguate_AutoChooseRest_ReturnsFlag()
    {
        var vm = CreateDisambiguation(AddRoom("Hall", "One."), AddRoom("Hall", "Two."));

        vm.AutoChooseRestCommand.Execute(null);

        vm.Result.AutoChooseRest.ShouldBeTrue();
        vm.Result.Chosen.ShouldBeSameAs(vm.Candidates[0]);
    }

    [Test]
    public void Disambiguate_Close_IsNewRoom()
    {
        var vm = CreateDisambiguation(AddRoom("Hall", "One."), AddRoom("Hall", "Two."));

        vm.Result.NewRoom.ShouldBeTrue();
        vm.Result.Chosen.ShouldBeNull();
    }

    [Test]
    public void Disambiguate_ContextBox_NormalisesNewlines()
    {
        var vm = new DisambiguateRoomsViewModel("Hall\r\n", "Line one.\r\nLine two.\rLine three.", new[] { AddRoom("Hall") });

        vm.ContextText.ShouldNotContain("\r");
        vm.ContextText.ShouldBe("Hall\n\nLine one.\nLine two.\nLine three.");
    }

    [Test]
    public async Task AvaloniaEditorPrompts_NoOwner_FallsBackToDefaults()
    {
        var prompts = new AvaloniaEditorPrompts(() => null);
        var first = AddRoom("Hall", "One.");
        var second = AddRoom("Hall", "Two.");

        var same = await prompts.ChooseSameDirectionAsync(first, "Other");
        var disambiguation = await prompts.DisambiguateRoomAsync("Hall", "x", new[] { first, second });

        same.ShouldBe(AutomapSameDirectionResult.KeepRoom1);
        disambiguation.Chosen.ShouldBeSameAs(first);
        disambiguation.NewRoom.ShouldBeFalse();
    }

    [AvaloniaTest]
    public void Dialogs_Construct_AndShowTheirButtons()
    {
        EnsureStyles();

        var sameDirection = new AutomapSameDirectionDialog
        {
            DataContext = new AutomapSameDirectionViewModel("Kitchen", "Pantry")
        };
        sameDirection.Show();
        sameDirection.GetVisualDescendants().OfType<Button>().Count().ShouldBe(3);
        sameDirection.Close();

        var disambiguate = new DisambiguateRoomsDialog
        {
            DataContext = CreateDisambiguation(AddRoom("Hall", "One."), AddRoom("Hall", "Two."))
        };
        disambiguate.Show();
        disambiguate.GetVisualDescendants().OfType<Button>().Count(b => b.Command != null).ShouldBe(3);
        disambiguate.Close();
    }

    private static void EnsureStyles()
    {
        var app = global::Avalonia.Application.Current;
        if (app != null && !app.Styles.OfType<FluentTheme>().Any())
        {
            app.Styles.Add(new FluentTheme());
        }
    }

    private sealed class ScriptedDialogPrompts : IEditorPrompts
    {
        public int SameDirectionCalls { get; private set; }

        public int DisambiguationCalls { get; private set; }

        public AutomapSameDirectionResult SameDirectionChoice { get; set; } = AutomapSameDirectionResult.KeepBoth;

        public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
            Room existing,
            string newRoomName,
            CancellationToken token = default)
        {
            SameDirectionCalls++;
            var vm = new AutomapSameDirectionViewModel(existing, newRoomName);
            switch (SameDirectionChoice)
            {
                case AutomapSameDirectionResult.KeepRoom2:
                    vm.KeepNew();
                    break;
                case AutomapSameDirectionResult.KeepBoth:
                    vm.KeepBoth();
                    break;
                default:
                    vm.KeepExisting();
                    break;
            }

            return Task.FromResult(vm.Result);
        }

        public Task<RoomDisambiguation> DisambiguateRoomAsync(
            string transcriptLine,
            string roomDescription,
            IReadOnlyList<Room> candidates,
            CancellationToken token = default)
        {
            DisambiguationCalls++;
            var vm = new DisambiguateRoomsViewModel(transcriptLine, roomDescription, candidates);
            vm.NewRoomCommand.Execute(null);
            return Task.FromResult(vm.Result);
        }
    }

    private async Task RunAutomapUntilAsync(IEditorPrompts prompts, string transcript, Func<bool> done)
    {
        _transcriptPath = Path.Combine(Path.GetTempPath(), $"trizbort_prompt_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(_transcriptPath, transcript);

        var settings = AutomapSettings.Default;
        settings.FileName = _transcriptPath;
        settings.VerboseTranscript = true;
        settings.AssumeRoomsWithSameNameAreSameRoom = false;
        settings.AssumeTwoWayConnections = true;
        settings.GuessExits = false;

        var automap = new AutomapEngine((message, title) => throw new AssertionException(title + ": " + message), prompts);
        var run = automap.Start(new AutomapCanvas(), settings);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!done() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        automap.Stop();
        try
        {
            await run;
        }
        catch (OperationCanceledException)
        {
        }
    }

    [Test]
    public async Task AutomapEndToEnd_WithScriptedDialogViewModels_SameDirectionKeepBoth()
    {
        var first = AddRoom("First Room", "A small room.");
        var original = AddRoom("Original Room", "Old room.");
        first.Position = Vector.Zero;
        original.Position = new Vector(200, 0);
        Project.Current.Elements.Add(new Connection(
            Project.Current,
            new Vertex(first.PortAt(CompassPoint.East)),
            new Vertex(original.PortAt(CompassPoint.West))));
        var prompts = new ScriptedDialogPrompts();

        await RunAutomapUntilAsync(
            prompts,
            "Example Game\n\nFirst Room\nA small room.\n\n>e\n\nReplacement Room\nAnother room.\n\n>look\n\n",
            () => Project.Current.Elements.OfType<Room>().Any(r => r.Name == "Replacement Room"));

        prompts.SameDirectionCalls.ShouldBe(1);
        var names = Project.Current.Elements.OfType<Room>().Select(r => r.Name).ToList();
        names.ShouldContain("Original Room");
        names.ShouldContain("Replacement Room");
    }

    [Test]
    public async Task AutomapEndToEnd_WithScriptedDialogViewModels_AmbiguousNameCreatesNewRoom()
    {
        AddRoom("Hall", "Alpha hall.");
        AddRoom("Hall", "Beta hall.");
        var prompts = new ScriptedDialogPrompts();

        await RunAutomapUntilAsync(
            prompts,
            "Example Game\n\nHall\nZulu description.\n\n>look\n\n",
            () => Project.Current.Elements.OfType<Room>().Count() >= 3);

        prompts.DisambiguationCalls.ShouldBeGreaterThan(0);
        Project.Current.Elements.OfType<Room>().Count().ShouldBe(3);
    }
}
