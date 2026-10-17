using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class CommandServiceViewTests
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

    [Test]
    public void SetView_UpdatesOriginAndZoomBeforeRaisingEventsInOrder()
    {
        var service = new CommandService();
        var events = new List<string>();
        Vector originAtZoomChanged = default;
        var zoomAtZoomChanged = 0f;
        Vector originAtInvalidated = default;
        var zoomAtInvalidated = 0f;

        service.ZoomChanged += (_, zoom) =>
        {
            events.Add("zoom");
            originAtZoomChanged = service.Origin;
            zoomAtZoomChanged = zoom;
        };
        service.CanvasInvalidated += (_, _) =>
        {
            events.Add("invalidated");
            originAtInvalidated = service.Origin;
            zoomAtInvalidated = service.ZoomFactor;
        };

        service.SetView(new Vector(10f, 20f), 2.0f);

        events.ShouldBe(new[] { "zoom", "invalidated" });
        originAtZoomChanged.ShouldBe(new Vector(10f, 20f));
        zoomAtZoomChanged.ShouldBe(2.0f);
        originAtInvalidated.ShouldBe(new Vector(10f, 20f));
        zoomAtInvalidated.ShouldBe(2.0f);
        service.Origin.ShouldBe(new Vector(10f, 20f));
        service.ZoomFactor.ShouldBe(2.0f);
    }

    [Test]
    public void SetView_OriginOnly_DoesNotRaiseZoomChanged()
    {
        var service = new CommandService();
        service.SetView(Vector.Zero, 1.5f);

        var events = new List<string>();
        service.ZoomChanged += (_, _) => events.Add("zoom");
        service.CanvasInvalidated += (_, _) => events.Add("invalidated");

        service.SetView(new Vector(40f, 50f), 1.5f);

        events.ShouldBe(new[] { "invalidated" });
        service.Origin.ShouldBe(new Vector(40f, 50f));
        service.ZoomFactor.ShouldBe(1.5f);
    }

    [Test]
    public void FitToMap_WithMapLargerThanViewport_ZoomsBelowOneAndCenters()
    {
        var service = new CommandService();
        service.SetViewportSize(400, 300);

        var room = new Room(Project.Current)
        {
            Position = new Vector(0, 0),
            Size = new Vector(2000, 2000)
        };
        Project.Current.Elements.Add(room);

        service.FitToMap();

        service.ZoomFactor.ShouldBeLessThan(1.0f);
        service.Origin.X.ShouldBe(1000f, 1.0f);
        service.Origin.Y.ShouldBe(1000f, 1.0f);
    }

    [Test]
    public void FitToMap_EmptyMap_ResetsOriginAndZoom()
    {
        var service = new CommandService();
        service.SetView(new Vector(125f, 250f), 2.5f);

        service.FitToMap();

        service.Origin.ShouldBe(Vector.Zero);
        service.ZoomFactor.ShouldBe(1.0f);
    }
}
