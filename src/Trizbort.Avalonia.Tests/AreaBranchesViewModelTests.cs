using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[Category("Unit")]
public class AreaBranchesViewModelTests
{
    [Test]
    public void BranchesStartTicked_UntickingOneLeavesItOut_AndThePreviewFollows()
    {
        var foyer = new Room { Name = "Foyer" };
        var living = new Room { Name = "Living Room" };
        var dining = new Room { Name = "Dining Room" };
        var stairs = new Room { Name = "Stairs" };
        var house = new AreaBranch(foyer, living, new[] { living, dining });
        var upstairs = new AreaBranch(foyer, stairs, new[] { stairs });
        var vm = new AreaBranchesViewModel("Move 'Foyer' to a New Area", new[] { foyer }, new[] { house, upstairs });
        IReadOnlyList<Room>? previewed = null;
        vm.Preview = rooms => previewed = rooms;

        vm.Branches.ShouldAllBe(item => item.IsChecked);
        vm.Branches[0].Description.ShouldBe("Living Room and 1 more room (off Foyer)");
        vm.Summary.ShouldStartWith("4 rooms will move");

        vm.Branches[1].IsChecked = false;
        previewed.ShouldBe(new[] { foyer, living, dining });
        vm.Summary.ShouldStartWith("3 rooms will move");
        vm.OkCommand.Execute(null);
        vm.Result.ShouldBe(new[] { house });

        var cancelled = new AreaBranchesViewModel("x", new[] { foyer }, new[] { house });
        cancelled.CancelCommand.Execute(null);
        cancelled.Result.ShouldBeNull();
    }
}
