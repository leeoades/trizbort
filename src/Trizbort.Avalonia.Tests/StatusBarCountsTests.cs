using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class StatusBarCountsTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    [AvaloniaTest]
    public void AddingRooms_UpdatesElementCountWithoutProjectChange()
    {
        // I-022: the counts used to refresh only when the Project object was replaced.
        var service = new CommandService();
        try
        {
            var vm = new MainWindowViewModel(service, null!, null!, new AvaloniaAutomapCanvas());
            vm.ElementCount.ShouldStartWith("Rooms: 0 | Connections: 0");

            service.AddRoom(new Vector(0, 0));
            service.AddRoom(new Vector(200, 0));

            vm.ElementCount.ShouldStartWith("Rooms: 2 | Connections: 0");
        }
        finally
        {
            service.Session.Dispose();
        }
    }
}
