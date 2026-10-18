using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using AvaloniaPoint = Avalonia.Point;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class MinimapTransformTests
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

    [TestCase(100f, 200f)]
    [TestCase(0f, 0f)]
    [TestCase(500f, 350f)]
    [TestCase(1000f, 800f)]
    public void MinimapTransform_WorldToMinimapAndMinimapToWorld_RoundTripsAccurately(float worldX, float worldY)
    {
        var canvasBounds = new CoreRect(0, 0, 1000, 800);
        var clientArea = new CoreRect(6, 6, 188, 118);

        var worldPoint = new CoreVector(worldX, worldY);
        var minimapPoint = MinimapTransform.WorldToMinimap(worldPoint, canvasBounds, clientArea);

        // Minimap point must be within client area
        minimapPoint.X.ShouldBeGreaterThanOrEqualTo(clientArea.Left - 0.01f);
        minimapPoint.X.ShouldBeLessThanOrEqualTo(clientArea.Right + 0.01f);
        minimapPoint.Y.ShouldBeGreaterThanOrEqualTo(clientArea.Top - 0.01f);
        minimapPoint.Y.ShouldBeLessThanOrEqualTo(clientArea.Bottom + 0.01f);

        var roundTripped = MinimapTransform.MinimapToWorld(minimapPoint, canvasBounds, clientArea);

        roundTripped.X.ShouldBe(worldX, 0.01f);
        roundTripped.Y.ShouldBe(worldY, 0.01f);
    }

    [Test]
    public void MinimapTransform_EmptyMap_HandlesSafelyWithoutException()
    {
        var emptyBounds = CoreRect.Empty;
        var clientArea = new CoreRect(6, 6, 188, 118);
        var zeroClient = new CoreRect(0, 0, 0, 0);

        // Null / empty element collection
        MinimapTransform.ComputeCanvasBounds(null).ShouldBe(CoreRect.Empty);
        MinimapTransform.ComputeCanvasBounds(new List<Element>()).ShouldBe(CoreRect.Empty);

        // Empty canvas bounds
        MinimapTransform.WorldToMinimap(new CoreRect(10, 10, 50, 50), emptyBounds, clientArea).ShouldBe(CoreRect.Empty);
        MinimapTransform.WorldToMinimap(new CoreVector(10, 10), emptyBounds, clientArea).ShouldBe(new CoreVector(6, 6));
        MinimapTransform.MinimapToWorld(new CoreVector(50, 50), emptyBounds, clientArea).ShouldBe(CoreVector.Zero);
        MinimapTransform.CalculateOriginFromMinimapPoint(new CoreVector(50, 50), emptyBounds, clientArea, new CoreVector(800, 600)).ShouldBe(CoreVector.Zero);
        MinimapTransform.CalculateOriginDelta(new CoreVector(10, 10), emptyBounds, clientArea).ShouldBe(CoreVector.Zero);

        // Zero client area
        var validBounds = new CoreRect(0, 0, 500, 400);
        MinimapTransform.WorldToMinimap(new CoreRect(10, 10, 50, 50), validBounds, zeroClient).ShouldBe(CoreRect.Empty);
        MinimapTransform.MinimapToWorld(new CoreVector(50, 50), validBounds, zeroClient).ShouldBe(validBounds.Center);
        MinimapTransform.CalculateOriginFromMinimapPoint(new CoreVector(50, 50), validBounds, zeroClient, new CoreVector(800, 600)).ShouldBe(validBounds.Center);
        MinimapTransform.CalculateOriginDelta(new CoreVector(10, 10), validBounds, zeroClient).ShouldBe(CoreVector.Zero);
    }

    [Test]
    public void MinimapTransform_OriginCalculation_ClampsToOccupiedPortionsWhenViewportIsSmallerThanMap()
    {
        var canvasBounds = new CoreRect(0, 0, 1000, 1000);
        var clientArea = new CoreRect(0, 0, 200, 200);
        var viewportWorldSize = new CoreVector(400, 300); // half width = 200, half height = 150

        // Clicking center of minimap (100, 100) -> world center (500, 500)
        var originCenter = MinimapTransform.CalculateOriginFromMinimapPoint(
            new CoreVector(100, 100),
            canvasBounds,
            clientArea,
            viewportWorldSize);
        originCenter.X.ShouldBe(500f, 0.1f);
        originCenter.Y.ShouldBe(500f, 0.1f);

        // Clicking top-left of minimap (0, 0) -> target is (0, 0), clamped to half viewport (200, 150)
        var originTopLeft = MinimapTransform.CalculateOriginFromMinimapPoint(
            new CoreVector(0, 0),
            canvasBounds,
            clientArea,
            viewportWorldSize);
        originTopLeft.X.ShouldBe(200f, 0.1f);
        originTopLeft.Y.ShouldBe(150f, 0.1f);

        // Clicking bottom-right of minimap (200, 200) -> target is (1000, 1000), clamped to (800, 850)
        var originBottomRight = MinimapTransform.CalculateOriginFromMinimapPoint(
            new CoreVector(200, 200),
            canvasBounds,
            clientArea,
            viewportWorldSize);
        originBottomRight.X.ShouldBe(800f, 0.1f);
        originBottomRight.Y.ShouldBe(850f, 0.1f);
    }

    [Test]
    public void MinimapTransform_SmallMapInLargeViewport_CentersOriginOnMap()
    {
        var canvasBounds = new CoreRect(100, 100, 200, 100); // Center is (200, 150)
        var clientArea = new CoreRect(0, 0, 200, 200);
        var viewportWorldSize = new CoreVector(800, 600); // larger than map

        var origin = MinimapTransform.CalculateOriginFromMinimapPoint(
            new CoreVector(50, 50),
            canvasBounds,
            clientArea,
            viewportWorldSize);

        // When viewport is larger than map, origin must be fixed at map center
        origin.X.ShouldBe(200f, 0.1f);
        origin.Y.ShouldBe(150f, 0.1f);
    }

    [Test]
    public void MinimapTransform_CalculateOriginDelta_ScalesProportionally()
    {
        var canvasBounds = new CoreRect(0, 0, 1000, 500);
        var clientArea = new CoreRect(0, 0, 100, 50);

        var deltaMinimap = new CoreVector(10, 5);
        var deltaWorld = MinimapTransform.CalculateOriginDelta(deltaMinimap, canvasBounds, clientArea);

        deltaWorld.X.ShouldBe(100f, 0.01f);
        deltaWorld.Y.ShouldBe(50f, 0.01f);
    }

    [AvaloniaTest]
    public void MinimapControl_ClickAndDrag_MovesCanvasOrigin()
    {
        var room1 = new Room(Project.Current)
        {
            Name = "Room 1",
            Position = new CoreVector(0, 0),
            Size = new CoreVector(100, 60)
        };
        var room2 = new Room(Project.Current)
        {
            Name = "Room 2",
            Position = new CoreVector(900, 900),
            Size = new CoreVector(100, 60)
        };
        Project.Current.Elements.Add(room1);
        Project.Current.Elements.Add(room2);

        var service = new CommandService();
        var canvas = new MapCanvasControl
        {
            Width = 800,
            Height = 600,
            CommandService = service
        };
        var minimap = new MinimapControl
        {
            Width = 200,
            Height = 130,
            CanvasControl = canvas,
            CommandService = service
        };

        var root = new Panel { Width = 800, Height = 600 };
        root.Children.Add(canvas);
        root.Children.Add(minimap);

        var window = new Window { Content = root, Width = 800, Height = 600 };
        window.Show();

        try
        {
            // Initial origin
            var initialOrigin = canvas.Origin;

            // Click minimap at center (100, 65)
            minimap.RaiseEvent(new PointerPressedEventArgs(
                minimap,
                new Pointer(1, PointerType.Mouse, true),
                minimap,
                new AvaloniaPoint(100, 65),
                0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None));

            var originAfterClick = canvas.Origin;
            originAfterClick.ShouldNotBe(initialOrigin);

            // Drag minimap to (150, 100)
            minimap.RaiseEvent(new PointerEventArgs(
                InputElement.PointerMovedEvent,
                minimap,
                new Pointer(1, PointerType.Mouse, true),
                minimap,
                new AvaloniaPoint(150, 100),
                0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
                KeyModifiers.None));

            var originAfterDrag = canvas.Origin;
            originAfterDrag.ShouldNotBe(originAfterClick);

            // Release pointer
            minimap.RaiseEvent(new PointerReleasedEventArgs(
                minimap,
                new Pointer(1, PointerType.Mouse, true),
                minimap,
                new AvaloniaPoint(150, 100),
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                KeyModifiers.None,
                MouseButton.Left));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MinimapControl_RedrawsOnModelChangePanAndZoom()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service, Width = 800, Height = 600 };
        var minimap = new MinimapControl { CommandService = service, CanvasControl = canvas, Width = 200, Height = 130 };

        var panel = new Panel();
        panel.Children.Add(canvas);
        panel.Children.Add(minimap);

        var window = new Window { Content = panel, Width = 800, Height = 600 };
        window.Show();

        try
        {
            // Pan moves service origin and updates view
            service.Origin = new CoreVector(300, 400);
            canvas.Origin.ShouldBe(new CoreVector(300, 400));

            // Zoom changes
            service.ZoomFactor = 2.5f;
            canvas.ZoomFactor.ShouldBe(2.5f);

            // Model element added
            var room = new Room(Project.Current)
            {
                Name = "Test Room",
                Position = new CoreVector(100, 100),
                Size = new CoreVector(80, 50)
            };
            Project.Current.Elements.Add(room);

            var conn = new Connection(Project.Current, new Vertex(room.PortList[0]), new Vertex(room.PortList[1]));
            Project.Current.Elements.Add(conn);

            // Mutating element
            room.Position = new CoreVector(200, 200);

            // Removing element
            Project.Current.Elements.Remove(conn);
            Project.Current.Elements.Remove(room);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MinimapControl_EmptyAndPopulatedMap_RendersWithoutErrors()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service, Width = 800, Height = 600 };
        var minimap = new MinimapControl { CommandService = service, CanvasControl = canvas, Width = 200, Height = 130 };

        var panel = new Panel { Width = 800, Height = 600 };
        panel.Children.Add(canvas);
        panel.Children.Add(minimap);

        var window = new Window { Content = panel, Width = 800, Height = 600 };
        window.Show();

        try
        {
            // Empty map measure/arrange
            panel.Measure(new Size(800, 600));
            panel.Arrange(new global::Avalonia.Rect(0, 0, 800, 600));
            minimap.InvalidateVisual();

            // Populate map
            var roomA = new Room(Project.Current) { Name = "A", Position = new CoreVector(0, 0), Size = new CoreVector(100, 60) };
            var roomB = new Room(Project.Current) { Name = "B", Position = new CoreVector(200, 200), Size = new CoreVector(100, 60) };
            Project.Current.Elements.Add(roomA);
            Project.Current.Elements.Add(roomB);
            var conn = new Connection(Project.Current, new Vertex(roomA.PortList[0]), new Vertex(roomB.PortList[0]));
            Project.Current.Elements.Add(conn);

            service.Select(roomA);
            minimap.InvalidateVisual();

            service.ClearSelection();
            minimap.InvalidateVisual();
        }
        finally
        {
            window.Close();
        }
    }
}
