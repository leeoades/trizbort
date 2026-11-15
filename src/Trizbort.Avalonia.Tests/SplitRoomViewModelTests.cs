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
    public void OkGivesTheTickedJoins_AndWhereYouAre_CancelGivesNothing()
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
        vm.AsksWhere.ShouldBeTrue();
        vm.InNew.ShouldBeTrue();
        vm.Joins.Count.ShouldBe(2);
        vm.Joins[0].Moves.ShouldBeTrue();
        vm.Joins[1].Moves.ShouldBeFalse();
        vm.Joins[1].Moves = true;
        vm.InNew = false;
        vm.InOriginal = true;
        vm.OkCommand.Execute(null);

        vm.Result.ShouldNotBeNull();
        vm.Result!.Moving.Count.ShouldBe(2);
        vm.Result.Where.ShouldBe(RoomSplitWhere.Original);

        var cancelled = new SplitRoomViewModel(maze, "Maze (2)", joins, new[] { joins[0] }, null);
        cancelled.AsksWhere.ShouldBeFalse();
        cancelled.CancelCommand.Execute(null);
        cancelled.Result.ShouldBeNull();
    }
}
