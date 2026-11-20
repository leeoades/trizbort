using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Join properties name the room above each end's box, and Start is always the room on the left (or the upper one,
///   when they're one above the other), whichever end the join was drawn from.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ConnectionPropertiesRoomsTests
{
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

    private Room AddRoom(string name, int x, int y)
    {
        var room = new Room(_project) { Name = name, Position = new CoreVector(x, y) };
        _project.Elements.Add(room);
        return room;
    }

    private Connection Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide)
    {
        var join = new Connection(_project, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide)))
        {
            StartText = "from text",
            EndText = "to text"
        };
        _project.Elements.Add(join);
        return join;
    }

    [Test]
    public void DrawnLeftToRight_StartIsTheLeftRoom()
    {
        var hall = AddRoom("Hall", 0, 0);
        var garden = AddRoom("Garden", 300, 0);
        var join = Join(hall, CompassPoint.East, garden, CompassPoint.West);
        var vm = new ConnectionPropertiesViewModel();

        vm.Initialize(join);

        vm.ShownTheOtherWay.ShouldBeFalse();
        vm.StartRoomName.ShouldBe("Hall");
        vm.EndRoomName.ShouldBe("Garden");
        vm.StartText.ShouldBe("from text");
        vm.EndText.ShouldBe("to text");
    }

    [Test]
    public void DrawnRightToLeft_StillShowsTheLeftRoomAtTheStart_AndSavesEachTextAtItsOwnEnd()
    {
        var hall = AddRoom("Hall", 0, 0);
        var garden = AddRoom("Garden", 300, 0);
        // drawn from the garden, on the right, to the hall
        var join = Join(garden, CompassPoint.West, hall, CompassPoint.East);
        var vm = new ConnectionPropertiesViewModel();

        vm.Initialize(join);

        vm.ShownTheOtherWay.ShouldBeTrue();
        vm.StartRoomName.ShouldBe("Hall");
        vm.EndRoomName.ShouldBe("Garden");
        vm.StartText.ShouldBe("to text");
        vm.EndText.ShouldBe("from text");

        vm.StartText = "by the hall";
        vm.EndText = "by the garden";
        vm.ApplyTo(join);
        join.StartText.ShouldBe("by the garden");
        join.EndText.ShouldBe("by the hall");
    }

    [Test]
    public void OneAboveTheOther_StartIsTheUpperRoom()
    {
        var attic = AddRoom("Attic", 0, 0);
        var landing = AddRoom("Landing", 40, 300);
        var join = Join(landing, CompassPoint.North, attic, CompassPoint.South);
        var vm = new ConnectionPropertiesViewModel();

        vm.Initialize(join);

        vm.StartRoomName.ShouldBe("Attic");
        vm.EndRoomName.ShouldBe("Landing");
        vm.StartText.ShouldBe("to text");
    }

    [Test]
    public void AnUnlabelledEnd_ShowsItsSidesDirection_UnderItsOwnRoom_AndStaysHidden()
    {
        var hall = AddRoom("Hall", 0, 0);
        var garden = AddRoom("Garden", 300, 0);
        var join = new Connection(_project, new Vertex(garden.PortAt(CompassPoint.West)),
            new Vertex(hall.PortAt(CompassPoint.East)));
        _project.Elements.Add(join);
        var vm = new ConnectionPropertiesViewModel();

        vm.Initialize(join);
        vm.StartText.ShouldBe("east");
        vm.EndText.ShouldBe("west");

        vm.ApplyTo(join);
        join.StartText.ShouldBeEmpty();
        join.EndText.ShouldBeEmpty();
    }
}
