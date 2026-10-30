using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 072: validation, start-room confirmation, corner group visibility, last tab, undo.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RoomPropertiesViewModelTests
{
    private Project? _previousProject;
    private Project? _project;
    private int _previousLastTab;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        _project = new Project();
        Project.Current = _project;
        _previousLastTab = ApplicationSettingsController.AppSettings.RoomDialogLastTab;
    }

    [TearDown]
    public void TearDown()
    {
        ApplicationSettingsController.AppSettings.RoomDialogLastTab = _previousLastTab;
        Project.Current = _previousProject!;
        _project!.Dispose();
    }

    private static Room AddRoom(string name, int id, bool start = false)
    {
        var room = new Room(Project.Current) { Name = name, Id = id, IsStartRoom = start };
        Project.Current.Elements.Add(room);
        return room;
    }

    [Test]
    public void Validate_EmptyName_ReturnsLegacyMessage()
    {
        var vm = new RoomPropertiesViewModel { Name = "  " };
        vm.Validate().ShouldBe("The room name can't be empty");
    }

    [Test]
    public void Validate_NoLetter_ReturnsLegacyMessage()
    {
        var vm = new RoomPropertiesViewModel { Name = "123" };
        vm.Validate().ShouldBe("The room name must contain at least one letter");
    }

    [Test]
    public void Validate_GoodName_ReturnsNull()
    {
        var vm = new RoomPropertiesViewModel { Name = "Room 1" };
        vm.Validate().ShouldBeNull();
    }

    [Test]
    public void TryAccept_InvalidName_SetsErrorAndRefuses()
    {
        var vm = new RoomPropertiesViewModel { Name = "" };
        var prompts = new FakePrompts(true);
        vm.TryAcceptAsync(prompts).GetAwaiter().GetResult().ShouldBeFalse();
        vm.NameError.ShouldNotBeNull();
        prompts.Asked.ShouldBe(0);
    }

    [Test]
    public void TryAccept_OtherStartRoom_No_UnticksFlagAndStaysOpen()
    {
        AddRoom("Hall", 1, start: true);
        var editing = AddRoom("Cave", 2);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(editing);
        vm.IsStartRoom = true;
        var prompts = new FakePrompts(false);

        vm.TryAcceptAsync(prompts).GetAwaiter().GetResult().ShouldBeFalse();

        prompts.Asked.ShouldBe(1);
        prompts.LastMessage.ShouldBe("The room 'Hall' is set as the starting room.  Do you want to change it to this room?");
        vm.IsStartRoom.ShouldBeFalse();
    }

    [Test]
    public void TryAccept_OtherStartRoom_Yes_Accepts()
    {
        AddRoom("Hall", 1, start: true);
        var editing = AddRoom("Cave", 2);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(editing);
        vm.IsStartRoom = true;

        vm.TryAcceptAsync(new FakePrompts(true)).GetAwaiter().GetResult().ShouldBeTrue();
        vm.IsStartRoom.ShouldBeTrue();
    }

    [Test]
    public void TryAccept_AlreadyStartRoomItself_DoesNotAsk()
    {
        var editing = AddRoom("Cave", 2, start: true);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(editing);
        var prompts = new FakePrompts(false);

        vm.TryAcceptAsync(prompts).GetAwaiter().GetResult().ShouldBeTrue();
        prompts.Asked.ShouldBe(0);
    }

    [Test]
    public void ShowCornerRadii_OnlyForRoundedCorners()
    {
        var vm = new RoomPropertiesViewModel();
        vm.ShowCornerRadii.ShouldBeFalse();
        vm.Shape = RoomShape.RoundedCorners;
        vm.ShowCornerRadii.ShouldBeTrue();
        vm.Shape = RoomShape.Ellipse;
        vm.ShowCornerRadii.ShouldBeFalse();
    }

    [Test]
    public void LastTab_IsSavedAndRestored()
    {
        var room = AddRoom("Cave", 1);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        vm.SelectedTabIndex = RoomPropertiesViewModel.TabShapes;
        vm.SaveLastTab();

        var vm2 = new RoomPropertiesViewModel();
        vm2.Initialize(room);
        vm2.SelectedTabIndex.ShouldBe(RoomPropertiesViewModel.TabShapes);

        var vm3 = new RoomPropertiesViewModel();
        vm3.Initialize(room, RoomPropertiesViewModel.TabRegion);
        vm3.SelectedTabIndex.ShouldBe(RoomPropertiesViewModel.TabRegion);
    }

    [Test]
    public void ApplyThroughSession_IsOneUndoStep()
    {
        var room = AddRoom("Cave", 1);
        using var session = new EditorSession();
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        vm.Name = "Grotto";
        vm.IsDark = true;
        vm.Shape = RoomShape.Ellipse;

        session.Apply("Edit room properties", room, () => vm.ApplyTo(room));
        var edited = Project.Current.Elements.OfType<Room>().Single();
        edited.Name.ShouldBe("Grotto");

        session.PerformUndo();

        var restored = Project.Current.Elements.OfType<Room>().Single();
        restored.Name.ShouldBe("Cave");
        restored.IsDark.ShouldBeFalse();
        restored.Shape.ShouldNotBe(RoomShape.Ellipse);
    }

    private sealed class FakePrompts(bool answer) : IEditorPrompts
    {
        public int Asked { get; private set; }
        public string? LastMessage { get; private set; }

        public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, CancellationToken token = default)
            => NonInteractiveEditorPrompts.Instance.ChooseSameDirectionAsync(existing, newRoomName, token);

        public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default)
            => NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);

        public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default)
        {
            Asked++;
            LastMessage = message;
            return Task.FromResult(answer);
        }
    }
}
