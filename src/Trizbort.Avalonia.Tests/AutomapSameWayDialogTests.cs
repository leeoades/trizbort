using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///     Lee, 17 Nov 2026: when a way already seen leading to one room now leads to another, automap asks in a dialog, as
///     it asks what moved a room: both are right, the room left moves, or it was another room with its name.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AutomapSameWayDialogTests
{
    [Test]
    public void TheQuestion_SaysWhichWay_FromWhere_AndBothRooms()
    {
        var vm = new AutomapSameWayViewModel("Hall", "east", "Pantry", "Kitchen");

        vm.Message.ShouldBe("East from Hall now leads to Kitchen, and it led to Pantry before.");
        vm.BothRightTitle.ShouldBe("Both Are Right");
        vm.MovesTitle.ShouldBe("'Hall' Moves");
        vm.DifferentTitle.ShouldBe("A Different 'Hall'");
        vm.DifferentText.ShouldContain("'Hall (2)'");
    }

    [Test]
    public void EachAnswer_ReturnsItsResult_AndClosingKeepsBoth()
    {
        new AutomapSameWayViewModel("Hall", "east", "Pantry", "Kitchen").Result
            .ShouldBe(AutomapSameDirectionResult.SamePlace);

        var vm = new AutomapSameWayViewModel("Hall", "east", "Pantry", "Kitchen");
        var closed = 0;
        vm.CloseRequested += (_, _) => closed++;
        vm.MovesCommand.Execute(null);
        vm.Result.ShouldBe(AutomapSameDirectionResult.Moves);
        vm.DifferentRoomCommand.Execute(null);
        vm.Result.ShouldBe(AutomapSameDirectionResult.DifferentRoom);
        vm.BothRightCommand.Execute(null);
        vm.Result.ShouldBe(AutomapSameDirectionResult.SamePlace);
        closed.ShouldBe(3);
    }

    [Test]
    public void NotKnowingTheRoomLeft_OnlyBothAreRight()
    {
        var vm = new AutomapSameWayViewModel(null, "enter closet", "Closet", "Top of Closet");

        vm.CanSayWhichRoom.ShouldBeFalse();
        vm.MovesCommand.CanExecute(null).ShouldBeFalse();
        vm.Message.ShouldBe("Enter closet now leads to Top of Closet, and it led to Closet before.");
    }

    [AvaloniaTest]
    public void TheDialog_ShowsTheThreeAnswers_AndAButtonCloses()
    {
        var app = global::Avalonia.Application.Current;
        if (app != null && !app.Styles.OfType<FluentTheme>().Any()) app.Styles.Add(new FluentTheme());
        var vm = new AutomapSameWayViewModel("Hall", "east", "Pantry", "Kitchen");
        var dialog = new AutomapSameWayDialog { DataContext = vm };
        dialog.Show();

        dialog.FindControl<Button>("BothRightButton")!.IsVisible.ShouldBeTrue();
        dialog.FindControl<Button>("MovesButton")!.IsVisible.ShouldBeTrue();
        dialog.FindControl<Button>("DifferentButton")!.IsVisible.ShouldBeTrue();
        dialog.FindControl<Button>("MovesButton")!.Command!.Execute(null);

        vm.Result.ShouldBe(AutomapSameDirectionResult.Moves);
        dialog.IsVisible.ShouldBeFalse();
    }

    [AvaloniaTest]
    public async Task WithNoWindowToAskFrom_BothAreKept()
    {
        IEditorPrompts prompts = new AvaloniaEditorPrompts(() => null);
        var room = new Room(new Trizbort.Domain.Application.Project()) { Name = "Pantry" };

        var answer = await prompts.ChooseSameWayAsync(room, "east", room, "Kitchen");

        answer.ShouldBe(AutomapSameDirectionResult.SamePlace);
    }
}
