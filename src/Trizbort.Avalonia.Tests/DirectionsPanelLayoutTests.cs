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
using Trizbort.Domain.Elements;
using CompassPoint = Trizbort.Domain.Misc.CompassPoint;
using Vertex = Trizbort.Domain.Misc.Vertex;

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

    [AvaloniaTest]
    public void Go_ReadsStop_WhileDirectionsShow_AndStopKeepsFromAndTo_AndFromWhereYouAreOnlyShowsWhileAutomapping()
    {
        var previous = Project.Current;
        Project.Current = new Project();
        var hall = new Room(Project.Current) { Name = "Hall", Position = new Trizbort.Domain.Misc.Vector(0, 0) };
        var kitchen = new Room(Project.Current) { Name = "Kitchen", Position = new Trizbort.Domain.Misc.Vector(200, 0) };
        Project.Current.Elements.Add(hall);
        Project.Current.Elements.Add(kitchen);
        Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(hall.PortAt(CompassPoint.East)),
            new Vertex(kitchen.PortAt(CompassPoint.West))));
        var commands = new CommandService();
        var vm = new MainWindowViewModel(commands, null!, null!, new AvaloniaAutomapCanvas());
        var window = new Window { DataContext = vm, Width = 260, Height = 600, Content = new DirectionsPanel() };
        try
        {
            window.Show();
            var panel = (DirectionsPanel)window.Content!;
            var go = panel.FindControl<Button>("GoButton").ShouldNotBeNull();
            var directions = commands.Session.Directions;

            directions.NavigateFrom(hall);
            vm.RefreshDirections();
            Dispatcher.UIThread.RunJobs();
            go.Content.ShouldBe("Go");

            directions.NavigateTo(kitchen, null);
            vm.RefreshDirections();
            Dispatcher.UIThread.RunJobs();
            directions.HasRoute.ShouldBeTrue();
            go.Content.ShouldBe("Stop");

            vm.GoDirectionsCommand.Execute(null);
            vm.RefreshDirections();
            Dispatcher.UIThread.RunJobs();
            directions.HasRoute.ShouldBeFalse();
            directions.From.ShouldBe(hall);
            directions.To.ShouldBe(kitchen);
            go.Content.ShouldBe("Go");

            // automap isn't running: it doesn't know where you are
            vm.IsAutomapActive.ShouldBeFalse();
            panel.FindControl<Button>("WhereYouAreButton").ShouldNotBeNull().IsVisible.ShouldBeFalse();
        }
        finally
        {
            window.Close();
            Project.Current = previous;
        }
    }
}
