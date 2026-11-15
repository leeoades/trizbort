using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[Category("Unit")]
public class WhatMovedItViewModelTests
{
    private static readonly DoneInRoom[] Done =
    {
        new("pull first peg"),
        new("second", "pull peg"),
        new("open door")
    };

    [Test]
    public void TheCommandsTyped_AreListed_AQuestionWithItsAnswer_AsOne()
    {
        var vm = new WhatMovedItViewModel("Closet", "Cellar", Done, 1);

        vm.Options.Select(option => option.Title).ShouldBe(new[]
        {
            "pull first peg", "pull peg → second", "open door",
            "Nothing I did (it moves by itself, or time passed)", "Something else..."
        });
        vm.Selected.ShouldBe(vm.Options[1]);
        vm.IntroText.ShouldContain("Cellar");
        vm.OkCommand.Execute(null);

        vm.Result.ShouldBe(new WhatMovedItChoice("pull peg, second"));
    }

    [Test]
    public void NoSuggestion_PicksNothingIDid()
    {
        var vm = new WhatMovedItViewModel("Bus", "Market", Done, -1);

        vm.Selected.Kind.ShouldBe(WhatMovedItKind.ByItself);
        vm.OkCommand.Execute(null);

        vm.Result.ShouldBe(WhatMovedItChoice.Itself);
    }

    [Test]
    public void SomethingElse_TakesTheWordsTyped_CancelGivesNothing()
    {
        var vm = new WhatMovedItViewModel("Lift", "Roof", Done, 0);
        vm.AsksOther.ShouldBeFalse();
        vm.Selected = vm.Options[^1];
        vm.AsksOther.ShouldBeTrue();
        vm.OtherText = " push button ";
        vm.OkCommand.Execute(null);
        vm.Result.ShouldBe(new WhatMovedItChoice("push button"));

        var cancelled = new WhatMovedItViewModel("Lift", "Roof", Done, 0);
        cancelled.CancelCommand.Execute(null);
        cancelled.Result.ShouldBeNull();
    }
}
