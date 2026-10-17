using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class MapCanvasControlSyncTests
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
    public void CommandService_IsHookedOnceWhileAttached_AndUnhookedOnDetach()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };

        EventHandlerCount.For(service, "CanvasInvalidated").ShouldBe(0);
        EventHandlerCount.For(service, "ZoomChanged").ShouldBe(0);
        EventHandlerCount.For(service, "SelectionChanged").ShouldBe(0);

        var window = Show(canvas);
        try
        {
            canvas.IsAttachedToVisualTree().ShouldBeTrue();
            EventHandlerCount.For(service, "CanvasInvalidated").ShouldBe(1);
            EventHandlerCount.For(service, "ZoomChanged").ShouldBe(1);
            EventHandlerCount.For(service, "SelectionChanged").ShouldBe(1);
        }
        finally
        {
            window.Close();
        }

        canvas.IsAttachedToVisualTree().ShouldBeFalse();
        EventHandlerCount.For(service, "CanvasInvalidated").ShouldBe(0);
        EventHandlerCount.For(service, "ZoomChanged").ShouldBe(0);
        EventHandlerCount.For(service, "SelectionChanged").ShouldBe(0);
    }

    [AvaloniaTest]
    public void CommandService_AssignedAfterAttach_HooksExactlyOnce()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl();
        var window = Show(canvas);
        try
        {
            canvas.IsAttachedToVisualTree().ShouldBeTrue();
            canvas.CommandService = service;
            EventHandlerCount.For(service, "CanvasInvalidated").ShouldBe(1);
            EventHandlerCount.For(service, "SelectionChanged").ShouldBe(1);
        }
        finally
        {
            window.Close();
        }

        EventHandlerCount.For(service, "CanvasInvalidated").ShouldBe(0);
        EventHandlerCount.For(service, "SelectionChanged").ShouldBe(0);
    }

    [AvaloniaTest]
    public void ZoomAt_ThenSelectionChange_KeepsWorldPointUnderCursorAndServiceView()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var window = Show(canvas);
        try
        {
            canvas.Bounds.Width.ShouldBeGreaterThan(0);
            canvas.Bounds.Height.ShouldBeGreaterThan(0);

            var cursor = new Point(123, 456);
            var worldBefore = canvas.ClientToCanvas(cursor);

            canvas.ZoomAt(cursor, 5.0f);

            canvas.ZoomFactor.ShouldBe(5.0f);
            service.ZoomFactor.ShouldBe(5.0f);
            canvas.Origin.ShouldBe(service.Origin);
            var worldAfterZoom = canvas.ClientToCanvas(cursor);
            worldAfterZoom.X.ShouldBe(worldBefore.X, 0.001f);
            worldAfterZoom.Y.ShouldBe(worldBefore.Y, 0.001f);

            service.SelectAll();

            canvas.Origin.ShouldBe(service.Origin);
            canvas.ZoomFactor.ShouldBe(service.ZoomFactor);
            var worldAfterSelection = canvas.ClientToCanvas(cursor);
            worldAfterSelection.X.ShouldBe(worldBefore.X, 0.001f);
            worldAfterSelection.Y.ShouldBe(worldBefore.Y, 0.001f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void OriginSetter_WritesThroughService_AndSurvivesSelectionChange()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var window = Show(canvas);
        try
        {
            canvas.Origin = new Vector(5000f, 5000f);

            service.Origin.ShouldBe(new Vector(5000f, 5000f));
            canvas.Origin.ShouldBe(service.Origin);

            service.ClearSelection();

            canvas.Origin.ShouldBe(new Vector(5000f, 5000f));
            service.Origin.ShouldBe(new Vector(5000f, 5000f));
        }
        finally
        {
            window.Close();
        }
    }

    private static Window Show(MapCanvasControl canvas)
    {
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = canvas
        };
        window.Show();
        return window;
    }
}
