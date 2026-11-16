using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 16 Nov 2026: the Directions panel sets where you choose the rooms (From, To, Go) apart from the directions
///   themselves: the rooms in a box of their own, the steps under a heading of their own.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DirectionsPanelLayoutTests
{
    [AvaloniaTest]
    public void WhereYouChooseTheRooms_IsABoxOfItsOwn_AboveTheSteps()
    {
        var previous = Project.Current;
        Project.Current = new Project();
        var vm = new MainWindowViewModel(new CommandService(), null!, null!, new AvaloniaAutomapCanvas());
        var window = new Window { DataContext = vm, Width = 260, Height = 600, Content = new DirectionsPanel() };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var panel = (DirectionsPanel)window.Content!;
            var card = panel.FindControl<Border>("RouteCard").ShouldNotBeNull();
            var heading = panel.FindControl<StackPanel>("StepsHeading").ShouldNotBeNull();
            var steps = panel.FindControl<ListBox>("Steps").ShouldNotBeNull();

            // From, To and Go are in the box; the status of the directions is with the steps
            card.GetLogicalDescendants().OfType<Button>().Select(button => button.Name)
                .ShouldBe(new[] { "WhereYouAreButton", "GoButton", "SwapButton" }, ignoreOrder: true);
            heading.Children.OfType<TextBlock>().ShouldContain(text => text.Name == "Status");

            // top to bottom: the box, the heading, the steps
            var cardBottom = card.TranslatePoint(new Point(0, card.Bounds.Height), panel)!.Value.Y;
            var headingTop = heading.TranslatePoint(new Point(0, 0), panel)!.Value.Y;
            var stepsTop = steps.TranslatePoint(new Point(0, 0), panel)!.Value.Y;
            cardBottom.ShouldBeLessThan(headingTop);
            headingTop.ShouldBeLessThan(stepsTop);
        }
        finally
        {
            window.Close();
            Project.Current = previous;
        }
    }
}
