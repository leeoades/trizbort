using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   "Which room does it mean?" in a maze of rooms all alike: each line shows the room's subtitle and the objects in
///   it, and the ways out only when those don't tell it apart; the description is beside the list, not in it. One
///   picked on the map is picked in the list.
/// </summary>
[TestFixture]
[NonParallelizable]
public class DisambiguateRoomsTellsTests
{
    private const string Alike = "You are in a maze of twisty little passages, all alike.";

    private Project? _previous;
    private Project _project = null!;

    [SetUp]
    public void SetUp()
    {
        _previous = Project.Current;
        _project = new Project();
        Project.Current = _project;
    }

    [TearDown]
    public void TearDown()
    {
        Project.Current = _previous!;
        _project.Dispose();
    }

    private Room Add(string objects, string subtitle = "", string description = Alike)
    {
        var room = new Room(_project) { Name = "Maze", Objects = objects, SubTitle = subtitle };
        room.AddDescription(description);
        _project.Elements.Add(room);
        return room;
    }

    [Test]
    public void RoomsAllAlike_ShowTheirObjects_AndOnePickedOnTheMapIsPickedInTheList()
    {
        var rod = Add("black rod");
        var bird = Add("bird", "by the pit");
        var other = new Room(_project) { Name = "Hall" };
        var vm = new DisambiguateRoomsViewModel("Maze", Alike, new[] { rod, bird });

        vm.Choices.Select(choice => choice.Objects).ShouldBe(new[] { "black rod", "bird", "" });
        vm.Choices.Select(choice => choice.Subtitle).ShouldBe(new[] { "", "by the pit", "" });
        vm.Choices[0].HasObjects.ShouldBeTrue();
        vm.Choices[0].HasSubtitle.ShouldBeFalse();
        vm.Choices[2].HasObjects.ShouldBeFalse();
        // the objects tell them apart: no ways
        vm.Choices.ShouldAllBe(choice => !choice.HasDetail);

        vm.PickFromMap(bird).ShouldBeTrue();
        vm.Selected!.Room.ShouldBe(bird);
        // the panel on the right: the description, then the objects in the room
        vm.SelectedDescription.ShouldBe(Alike + "\n\nObjects:\n  bird");
        vm.PickFromMap(other).ShouldBeFalse();
        vm.Selected!.Room.ShouldBe(bird);
    }

    [Test]
    public void TheDescription_IsntInTheList_AndTheWaysOutShowOnlyWhenNothingElseTellsRoomsApart()
    {
        var first = Add("", description: "A twisty maze. It smells of damp.");
        var second = Add("", description: "A twisty maze. A draught blows.");
        var deadEnd = new Room(_project) { Name = "Dead End" };
        _project.Elements.Add(deadEnd);
        _project.Elements.Add(new Connection(_project, new Vertex(first.PortAt(CompassPoint.East)),
            new Vertex(deadEnd.PortAt(CompassPoint.West))));

        var vm = new DisambiguateRoomsViewModel("Maze", "A twisty maze.", new[] { first, second });

        vm.Choices[0].Detail.ShouldBe("ways: e to Dead End");
        vm.Choices[1].Detail.ShouldBe("no ways out");
        vm.Choices.ShouldAllBe(choice => !choice.Detail.Contains("damp") && !choice.Detail.Contains("draught"));
    }
}
