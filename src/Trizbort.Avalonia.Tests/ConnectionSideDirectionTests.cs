using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>Join properties show the direction an unlabelled end's side gives it, so it can be changed.</summary>
[TestFixture]
[NonParallelizable]
public class ConnectionSideDirectionTests
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

    private Connection JoinEastToWest()
    {
        var hall = new Room(_project) { Name = "Hall", Position = new CoreVector(0, 0) };
        var garden = new Room(_project) { Name = "Garden", Position = new CoreVector(300, 0) };
        _project.Elements.Add(hall);
        _project.Elements.Add(garden);
        var join = new Connection(_project, new Vertex(hall.PortAt(CompassPoint.East)), new Vertex(garden.PortAt(CompassPoint.West)));
        _project.Elements.Add(join);
        return join;
    }

    [Test]
    public void AnUnlabelledEnd_ShowsTheDirectionOfItsSide_AndLeftAlone_StaysHidden()
    {
        var join = JoinEastToWest();
        var vm = new ConnectionPropertiesViewModel();

        vm.Initialize(join);
        vm.StartText.ShouldBe("east");
        vm.EndText.ShouldBe("west");

        vm.ApplyTo(join);
        join.StartText.ShouldBeEmpty();
        join.EndText.ShouldBeEmpty();
    }

    [Test]
    public void ChangingTheDirection_WritesIt()
    {
        var join = JoinEastToWest();
        var vm = new ConnectionPropertiesViewModel();
        vm.Initialize(join);

        vm.StartText = "down";
        vm.ApplyTo(join);

        join.StartText.ShouldBe("down");
        ConnectionEndLabels.SideDirection(join, true).ShouldBeNull();
    }

    [Test]
    public void TheFarEndOfAOneWayJoin_ShowsNothing()
    {
        var join = JoinEastToWest();
        join.Flow = ConnectionFlow.OneWay;
        var vm = new ConnectionPropertiesViewModel();

        vm.Initialize(join);

        vm.StartText.ShouldBe("east");
        vm.EndText.ShouldBeEmpty();
    }
}
