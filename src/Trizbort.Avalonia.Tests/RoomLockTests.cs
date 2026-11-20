using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 20 Nov 2026: an automapped room's name and description are how automap knows it again, so the room dialog
///   shows them read-only, with a padlock that asks before unlocking them.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RoomLockTests
{
    private Project? _previous;

    [SetUp]
    public void SetUp()
    {
        _previous = Project.Current;
        Project.Current = new Project();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previous!;
        created.Dispose();
    }

    [Test]
    public void AnAutomappedRoom_IsLocked_UntilUnlocked_AndOneDrawnByHandIsnt()
    {
        var automapped = new Room(Project.Current) { Name = "Shell Room" };
        automapped.AddDescription("You're in a large room carved out of sedimentary rock.");
        var drawn = new Room(Project.Current) { Name = "Cave" };

        var vm = new RoomPropertiesViewModel();
        vm.Initialize(automapped);
        vm.IsAutomapLocked.ShouldBeTrue();
        vm.IsUnlocked.ShouldBeFalse();
        vm.Unlock();
        vm.IsAutomapLocked.ShouldBeFalse();

        var other = new RoomPropertiesViewModel();
        other.Initialize(drawn);
        other.IsAutomapLocked.ShouldBeFalse();
    }

    [AvaloniaTest]
    public void TheDialog_ShowsTheNameAndDescriptionReadOnly_WithPadlocks()
    {
        var room = new Room(Project.Current) { Name = "Shell Room" };
        room.AddDescription("You're in a large room carved out of sedimentary rock.");
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        var dialog = new RoomPropertiesDialog { DataContext = vm };
        dialog.Show();
        try
        {
            dialog.NameBox.IsReadOnly.ShouldBeTrue();
            dialog.DescriptionBox.IsReadOnly.ShouldBeTrue();
            dialog.NameLock.IsVisible.ShouldBeTrue();
            dialog.DescriptionLock.IsVisible.ShouldBeTrue();

            vm.Unlock();

            dialog.NameBox.IsReadOnly.ShouldBeFalse();
            dialog.DescriptionBox.IsReadOnly.ShouldBeFalse();
            dialog.NameLock.IsVisible.ShouldBeFalse();
        }
        finally
        {
            dialog.Close();
        }
    }
}
