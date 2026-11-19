using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 19 Nov 2026: beside "which room does it mean?", the objects in the room picked go in an Objects box of their
///   own, side by side as pills, so a room with many long ones (Hollywood Hijinx) takes little height.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class DisambiguateObjectsBoxTests
{
    private const string Many =
        "red wax statuette\ngreen wax statuette\nblue wax statuette\nyellow wax statuette\nhuge stuffed gorilla\n" +
        "large brass-framed mirror\npiece of yellowed parchment\nsteel key\nbird\ncage";

    [AvaloniaTest]
    public void ManyLongObjects_ArePillsInABoxOfTheirOwn_ThatStaysShort()
    {
        var previous = Project.Current;
        var project = new Project();
        Project.Current = project;
        var one = new Room(project) { Name = "Hall", Objects = Many };
        one.AddDescription("A hall.");
        var two = new Room(project) { Name = "Hall" };
        two.AddDescription("A hall.");
        var vm = new DisambiguateRoomsViewModel("Hall", "A hall.", new[] { one, two });
        var dialog = new DisambiguateRoomsDialog { DataContext = vm };
        dialog.Show();
        try
        {
            dialog.ObjectsBox.IsEffectivelyVisible.ShouldBeFalse();

            vm.Selected = vm.Choices[0];
            Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();

            vm.SelectedDescription.ShouldBe("A hall.");
            dialog.ObjectsBox.IsEffectivelyVisible.ShouldBeTrue();
            var pills = (ItemsControl)dialog.ObjectsBox.Content!;
            pills.Items.Cast<string>().ShouldBe(Many.Split('\n'));
            // a few lines of pills at most; more scroll
            dialog.ObjectsBox.MaxHeight.ShouldBeLessThanOrEqualTo(92);
        }
        finally
        {
            dialog.Close();
            Project.Current = previous;
            project.Dispose();
        }
    }
}
