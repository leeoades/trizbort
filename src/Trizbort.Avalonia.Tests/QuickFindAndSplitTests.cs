using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 19 Nov 2026: Ctrl+F showed Quick Find without the caret in its text box; and the join properties had no way
///   to split a join.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class QuickFindAndSplitTests
{
    [AvaloniaTest]
    public void QuickFind_OpensWithTheCaretInItsTextBox()
    {
        var app = Application.Current!;
        if (!app.Styles.OfType<FluentTheme>().Any()) app.Styles.Add(new FluentTheme());
        var dialog = new QuickFindDialog();
        dialog.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var text = dialog.FindBox.GetVisualDescendants().OfType<TextBox>().ShouldHaveSingleItem();
            dialog.FocusManager!.GetFocusedElement().ShouldBe(text);
        }
        finally
        {
            dialog.Close();
        }
    }

    [Test]
    public void JoinProperties_Split_RoundTrips()
    {
        var previous = Project.Current;
        var project = new Project();
        Project.Current = project;
        try
        {
            var join = new Connection(project, new Vertex(new Vector(0, 0)), new Vertex(new Vector(10, 10)));
            project.Elements.Add(join);
            var vm = new ConnectionPropertiesViewModel();
            vm.Initialize(join);
            vm.IsSplit.ShouldBeFalse();

            vm.IsSplit = true;
            vm.ApplyTo(join);
            join.Split.ShouldBeTrue();

            var again = new ConnectionPropertiesViewModel();
            again.Initialize(join);
            again.IsSplit.ShouldBeTrue();
            again.IsSplit = false;
            again.ApplyTo(join);
            join.Split.ShouldBeFalse();
        }
        finally
        {
            Project.Current = previous;
            project.Dispose();
        }
    }
}
