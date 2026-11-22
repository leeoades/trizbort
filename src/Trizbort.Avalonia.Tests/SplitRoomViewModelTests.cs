using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class SplitRoomViewModelTests
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
        if (!ReferenceEquals(created, _previous)) created.Dispose();
    }

    [Test]
    public void OkGivesTheTickedJoins_YouAreInTheNewOne_CancelGivesNothing()
    {
        var hall = new Room(Project.Current) { Name = "Hall", Position = new Vector(0, 0) };
        var maze = new Room(Project.Current) { Name = "Maze", Position = new Vector(0, -300) };
        var pit = new Room(Project.Current) { Name = "Pit", Position = new Vector(300, -300) };
        Project.Current.Elements.Add(hall);
        Project.Current.Elements.Add(maze);
        Project.Current.Elements.Add(pit);
        var canvas = new AutomapCanvas(Project.Current);
        canvas.Connect(hall, MappableDirection.North, maze, true);
        canvas.Connect(maze, MappableDirection.East, pit, true);
        var joins = RoomSplit.JoinsOf(maze);

        var vm = new SplitRoomViewModel(maze, "Maze (2)", joins, new[] { joins[0] }, RoomSplitWhere.New);
        vm.NewHeading.ShouldBe("New: you are here");
        // each join both ways: out, and back in
        vm.Joins.Count.ShouldBe(4);
        // the way in by the newest join is ticked: the way the player just came
        var ticked = vm.Joins.Single(join => join.Moves);
        ticked.Join.ShouldBe(joins[0]);
        ticked.Exit.Inward.ShouldBeTrue();
        vm.Joins.Single(join => join.Join == joins[1] && !join.Exit.Inward).ToggleCommand.Execute(null);
        vm.OkCommand.Execute(null);

        vm.Result.ShouldNotBeNull();
        vm.Result!.Moving.Count.ShouldBe(2);
        vm.Result.Exits!.Count.ShouldBe(2);
        vm.Result.Where.ShouldBe(RoomSplitWhere.New);

        var cancelled = new SplitRoomViewModel(maze, "Maze (2)", joins, new[] { joins[0] }, null);
        cancelled.NewHeading.ShouldBe("New");
        cancelled.CancelCommand.Execute(null);
        cancelled.Result.ShouldBeNull();
    }

    [Test]
    public void BothRoomsAreShown_AndClickingAWayMovesItAcross_AtItsSide()
    {
        // Lee, 21 Nov 2026: depict both rooms, the original on the left, and click the exits across
        var hall = new Room(Project.Current) { Name = "Hall", Position = new Vector(0, 0) };
        var maze = new Room(Project.Current) { Name = "Maze", Position = new Vector(0, -300) };
        var pit = new Room(Project.Current) { Name = "Pit", Position = new Vector(300, -300) };
        maze.AddDescription("You are in a maze of twisty little passages, all alike.");
        maze.Objects = "food";
        Project.Current.Elements.Add(hall);
        Project.Current.Elements.Add(maze);
        Project.Current.Elements.Add(pit);
        var canvas = new AutomapCanvas(Project.Current);
        canvas.Connect(hall, MappableDirection.North, maze, true);
        canvas.Connect(maze, MappableDirection.East, pit, true);
        var joins = RoomSplit.JoinsOf(maze);

        var vm = new SplitRoomViewModel(maze, "Maze", joins, Array.Empty<Connection>(), RoomSplitWhere.New);
        vm.NewName.ShouldBe("Maze");
        vm.Description.ShouldStartWith("You are in a maze");
        vm.Objects.ShouldBe("food");
        var east = vm.Joins.Single(join => join.Text.StartsWith("east"));
        east.Text.ShouldBe("east → Pit");
        // east: the middle row, right-hand column
        vm.OriginalCells[5].Joins.ShouldContain(east);
        vm.NewCells[5].Joins.ShouldBeEmpty();

        east.ToggleCommand.Execute(null);
        east.Moves.ShouldBeTrue();
        vm.OriginalCells[5].Joins.ShouldNotContain(east);
        vm.NewCells[5].Joins.ShouldContain(east);

        vm.OkCommand.Execute(null);
        vm.Result!.Moving.ShouldBe(new[] { east.Join });
        vm.Result.Where.ShouldBe(RoomSplitWhere.New);
    }

    [AvaloniaTest]
    public void TheRoomsAreDrawn_EachWithItsWays()
    {
        // Lee, 22 Nov 2026: a depiction of the rooms with the exits, as the map draws them
        var hall = new Room(Project.Current) { Name = "Hall", Position = new Vector(0, 0) };
        var maze = new Room(Project.Current) { Name = "Maze", Position = new Vector(0, -300) };
        Project.Current.Elements.Add(hall);
        Project.Current.Elements.Add(maze);
        new AutomapCanvas(Project.Current).Connect(hall, MappableDirection.North, maze, true);
        var vm = new SplitRoomViewModel(maze, "Maze", RoomSplit.JoinsOf(maze), Array.Empty<Connection>(), null);
        var original = new Trizbort.Avalonia.Controls.SplitRoomDiagram { DataContext = vm };
        var other = new Trizbort.Avalonia.Controls.SplitRoomDiagram { DataContext = vm, ShowsNew = true };

        original.Shown.Count.ShouldBe(2);
        other.Shown.ShouldBeEmpty();
        var wayOut = vm.Joins.Single(join => !join.Exit.Inward);
        wayOut.Exit.Way.ShouldBe("south");
        wayOut.Exit.Other.ShouldBe("Hall");

        wayOut.ToggleCommand.Execute(null);

        original.Shown.ShouldHaveSingleItem();
        other.Shown.ShouldBe(new[] { wayOut });
    }
}
