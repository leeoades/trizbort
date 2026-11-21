using Avalonia.Controls;
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

    private const string Before = "You are at the west end of the twopit room.";
    private const string After = "The top of a 12-foot-tall beanstalk is poking out of the west pit.";

    [AvaloniaTest]
    public void TheOtherDescriptions_AreListed_AndRemovableOnlyOnceUnlocked()
    {
        // Lee, 21 Nov 2026: see the room's other descriptions in its properties
        var room = new Room(Project.Current) { Name = "At West End of Twopit Room" };
        room.AddDescription(Before);
        room.AddDescription(After);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);

        vm.Description.ShouldBe(Before);
        vm.HasOtherDescriptions.ShouldBeTrue();
        vm.OtherDescriptions.Select(other => other.Text).ShouldBe(new[] { After });
        vm.OtherDescriptions[0].CanRemove.ShouldBeFalse();
        vm.OtherDescriptions[0].RemoveCommand.Execute(null);
        vm.OtherDescriptions.Count.ShouldBe(1);

        vm.Unlock();
        vm.OtherDescriptions[0].CanRemove.ShouldBeTrue();
        vm.OtherDescriptions[0].RemoveCommand.Execute(null);
        vm.HasOtherDescriptions.ShouldBeFalse();
        vm.ApplyTo(room);

        room.Descriptions.ShouldBe(new[] { Before });
    }

    [AvaloniaTest]
    public void ApplyingTheDialog_KeepsTheOtherDescriptions()
    {
        var room = new Room(Project.Current) { Name = "At West End of Twopit Room" };
        room.AddDescription(Before);
        room.AddDescription(After);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        vm.Unlock();
        vm.Description = "Changed.";

        vm.ApplyTo(room);

        room.Descriptions.ShouldBe(new[] { "Changed.", After });
    }

    [AvaloniaTest]
    public void TheDialog_ShowsTheOtherDescriptions()
    {
        var room = new Room(Project.Current) { Name = "At West End of Twopit Room" };
        room.AddDescription(Before);
        room.AddDescription(After);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        var dialog = new RoomPropertiesDialog { DataContext = vm };
        dialog.Show();
        try
        {
            dialog.FindControl<StackPanel>("OtherDescriptionsPanel")!.IsVisible.ShouldBeTrue();
            dialog.FindControl<ItemsControl>("OtherDescriptionsList")!.ItemCount.ShouldBe(1);
        }
        finally
        {
            dialog.Close();
        }
    }
}
