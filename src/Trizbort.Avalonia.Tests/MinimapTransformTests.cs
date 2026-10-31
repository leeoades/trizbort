using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

    [TestCase(-500f, -300f)]
    [TestCase(-200f, 100f)]
    [TestCase(0f, 0f)]
    [TestCase(500f, 500f)]
    public void MinimapTransform_NonZeroBoundsOffsetAndZoom_RoundTripsAccurately(float worldX, float worldY)
    {
        var canvasBounds = new CoreRect(-500, -300, 1000, 800);
        var clientArea = new CoreRect(10, 20, 180, 120);

        var worldPoint = new CoreVector(worldX, worldY);
        var minimapPoint = MinimapTransform.WorldToMinimap(worldPoint, canvasBounds, clientArea);

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

        // Zero client area
        var validBounds = new CoreRect(0, 0, 500, 400);
        MinimapTransform.WorldToMinimap(new CoreRect(10, 10, 50, 50), validBounds, zeroClient).ShouldBe(CoreRect.Empty);
        MinimapTransform.MinimapToWorld(new CoreVector(50, 50), validBounds, zeroClient).ShouldBe(validBounds.Center);
        MinimapTransform.CalculateOriginFromMinimapPoint(new CoreVector(50, 50), validBounds, zeroClient, new CoreVector(800, 600)).ShouldBe(validBounds.Center);
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
    public void MinimapTransform_OriginCalculation_WithNonZeroBoundsOffsetAndZoom()
    {
        var canvasBounds = new CoreRect(-500, -300, 1000, 800); // Center is (0, 100)
        var clientArea = new CoreRect(0, 0, 200, 200);
        var viewportWorldSize = new CoreVector(400, 300); // half width = 200, half height = 150

        // Center of minimap (100, 100) -> target world (0, 100)
        var originCenter = MinimapTransform.CalculateOriginFromMinimapPoint(
            new CoreVector(100, 100),
            canvasBounds,
            clientArea,
            viewportWorldSize);
        originCenter.X.ShouldBe(0f, 0.1f);
        originCenter.Y.ShouldBe(100f, 0.1f);

        // Top-left of minimap (0, 0) -> target (-500, -300), clamped to (-500 + 200, -300 + 150) = (-300, -150)
        var originTopLeft = MinimapTransform.CalculateOriginFromMinimapPoint(
            new CoreVector(0, 0),
            canvasBounds,
            clientArea,
            viewportWorldSize);
        originTopLeft.X.ShouldBe(-300f, 0.1f);
        originTopLeft.Y.ShouldBe(-150f, 0.1f);

        // Bottom-right of minimap (200, 200) -> target (500, 500), clamped to (500 - 200, 500 - 150) = (300, 350)
        var originBottomRight = MinimapTransform.CalculateOriginFromMinimapPoint(
            new CoreVector(200, 200),
            canvasBounds,
            clientArea,
            viewportWorldSize);
        originBottomRight.X.ShouldBe(300f, 0.1f);
        originBottomRight.Y.ShouldBe(350f, 0.1f);
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
            root.Measure(new Size(800, 600));
            root.Arrange(new global::Avalonia.Rect(0, 0, 800, 600));

            var canvasBounds = MinimapTransform.ComputeCanvasBounds(Project.Current.Elements);
            var clientArea = new CoreRect(6, 6, 200 - 12, 130 - 12);
            var viewportWorldSize = new CoreVector(800, 600);

            // Click minimap at (100, 65)
            var clickPoint = new CoreVector(100, 65);
            var expectedOriginClick = MinimapTransform.CalculateOriginFromMinimapPoint(
                clickPoint,
                canvasBounds,
                clientArea,
                viewportWorldSize);

            minimap.RaiseEvent(new PointerPressedEventArgs(
                minimap,
                new Pointer(1, PointerType.Mouse, true),
                minimap,
                new AvaloniaPoint(clickPoint.X, clickPoint.Y),
                0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None));

            canvas.Origin.X.ShouldBe(expectedOriginClick.X, 0.5f);
            canvas.Origin.Y.ShouldBe(expectedOriginClick.Y, 0.5f);

            // Drag minimap to (150, 100)
            var dragPoint = new CoreVector(150, 100);
            var expectedOriginDrag = MinimapTransform.CalculateOriginFromMinimapPoint(
                dragPoint,
                canvasBounds,
                clientArea,
                viewportWorldSize);

            minimap.RaiseEvent(new PointerEventArgs(
                InputElement.PointerMovedEvent,
                minimap,
                new Pointer(1, PointerType.Mouse, true),
                minimap,
                new AvaloniaPoint(dragPoint.X, dragPoint.Y),
                0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
                KeyModifiers.None));

            canvas.Origin.X.ShouldBe(expectedOriginDrag.X, 0.5f);
            canvas.Origin.Y.ShouldBe(expectedOriginDrag.Y, 0.5f);

            // Release pointer
            minimap.RaiseEvent(new PointerReleasedEventArgs(
                minimap,
                new Pointer(1, PointerType.Mouse, true),
                minimap,
                new AvaloniaPoint(dragPoint.X, dragPoint.Y),
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
    public void MinimapControl_RedrawsOnModelChangePanZoomAndResize()
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
            panel.Measure(new Size(800, 600));
            panel.Arrange(new global::Avalonia.Rect(0, 0, 800, 600));

            var initialInvalidations = minimap.InvalidationCount;

            // Pan moves service origin and updates view
            service.Origin = new CoreVector(300, 400);
            canvas.Origin.ShouldBe(new CoreVector(300, 400));
            minimap.InvalidationCount.ShouldBeGreaterThan(initialInvalidations);
            var count = minimap.InvalidationCount;

            // Zoom changes
            service.ZoomFactor = 2.5f;
            canvas.ZoomFactor.ShouldBe(2.5f);
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            // Model element added (Room)
            var room = new Room(Project.Current)
            {
                Name = "Test Room",
                Position = new CoreVector(100, 100),
                Size = new CoreVector(80, 50)
            };
            Project.Current.Elements.Add(room);
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            // Model element added (MapLabel)
            var label = new MapLabel(Project.Current)
            {
                Text = "Test Label",
                Position = new CoreVector(300, 300),
                Size = new CoreVector(120, 40)
            };
            Project.Current.Elements.Add(label);
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            // Model element added (Connection)
            var conn = new Connection(Project.Current, new Vertex(room.PortList[0]), new Vertex(room.PortList[1]));
            Project.Current.Elements.Add(conn);
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            // Mutating elements
            room.Position = new CoreVector(200, 200);
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            label.Text = "Updated Label";
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            // Removing elements
            Project.Current.Elements.Remove(conn);
            Project.Current.Elements.Remove(label);
            Project.Current.Elements.Remove(room);
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            // Project replaced
            var newProject = new Project();
            Project.Current = newProject;
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
            count = minimap.InvalidationCount;

            // Canvas bounds resize
            canvas.Width = 1024;
            canvas.Height = 768;
            panel.Measure(new Size(1024, 768));
            panel.Arrange(new global::Avalonia.Rect(0, 0, 1024, 768));
            minimap.InvalidationCount.ShouldBeGreaterThan(count);
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
            // Empty map measure/arrange and render frame capture
            panel.Measure(new Size(800, 600));
            panel.Arrange(new global::Avalonia.Rect(0, 0, 800, 600));

            using var emptyFrame = window.CaptureRenderedFrame();
            emptyFrame.ShouldNotBeNull();
            emptyFrame.PixelSize.Width.ShouldBeGreaterThan(0);
            emptyFrame.PixelSize.Height.ShouldBeGreaterThan(0);
            CountViewportBluePixels(emptyFrame).ShouldBe(0);

            // Populate map
            var roomA = new Room(Project.Current) { Name = "A", Position = new CoreVector(0, 0), Size = new CoreVector(100, 60) };
            var roomB = new Room(Project.Current) { Name = "B", Position = new CoreVector(200, 200), Size = new CoreVector(100, 60) };
            var label = new MapLabel(Project.Current) { Text = "Forest", Position = new CoreVector(100, 50), Size = new CoreVector(80, 30) };
            Project.Current.Elements.Add(roomA);
            Project.Current.Elements.Add(roomB);
            Project.Current.Elements.Add(label);
            var conn = new Connection(Project.Current, new Vertex(roomA.PortList[0]), new Vertex(roomB.PortList[0]));
            Project.Current.Elements.Add(conn);

            using var populatedFrame = window.CaptureRenderedFrame();
            populatedFrame.ShouldNotBeNull();
            populatedFrame.PixelSize.Width.ShouldBeGreaterThan(0);
            populatedFrame.PixelSize.Height.ShouldBeGreaterThan(0);
            CountViewportBluePixels(populatedFrame).ShouldBeGreaterThan(0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MinimapControl_Lifecycle_NoLeakOnDetachAndNoDoubleSubscription()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service, Width = 800, Height = 600 };

        // Set properties before attaching
        var minimap = new MinimapControl
        {
            CommandService = service,
            CanvasControl = canvas,
            Width = 200,
            Height = 130
        };

        var panel = new Panel { Width = 800, Height = 600 };
        panel.Children.Add(canvas);
        panel.Children.Add(minimap);

        var window = new Window { Content = panel, Width = 800, Height = 600 };
        window.Show();

        try
        {
            panel.Measure(new Size(800, 600));
            panel.Arrange(new global::Avalonia.Rect(0, 0, 800, 600));

            var countAfterAttach = minimap.InvalidationCount;

            // Invalidation when attached
            canvas.InvalidateVisual();
            minimap.InvalidationCount.ShouldBe(countAfterAttach + 1);

            // Detach minimap from visual tree
            panel.Children.Remove(minimap);
            var countAfterDetach = minimap.InvalidationCount;

            // Invalidation while detached should not affect minimap
            canvas.InvalidateVisual();
            minimap.InvalidationCount.ShouldBe(countAfterDetach);

            // Re-attach minimap to visual tree
            panel.Children.Add(minimap);
            var countAfterReattach = minimap.InvalidationCount;

            // Invalidation when re-attached should increment by exactly 1 (no double subscription)
            canvas.InvalidateVisual();
            minimap.InvalidationCount.ShouldBe(countAfterReattach + 1);
        }
        finally
        {
            window.Close();
        }
    }

    private static int CountViewportBluePixels(Bitmap frame)
    {
        using var bitmap = new WriteableBitmap(frame.PixelSize, frame.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var framebuffer = bitmap.Lock();
        frame.CopyPixels(framebuffer, AlphaFormat.Premul);

        var pixels = new byte[framebuffer.RowBytes * frame.PixelSize.Height];
        Marshal.Copy(framebuffer.Address, pixels, 0, pixels.Length);

        var count = 0;
        for (var y = 0; y < frame.PixelSize.Height; y++)
        {
            for (var x = 0; x < frame.PixelSize.Width; x++)
            {
                var offset = y * framebuffer.RowBytes + x * 4;
                var blue = pixels[offset];
                var green = pixels[offset + 1];
                var red = pixels[offset + 2];
                if (blue > 120 && green > 50 && blue > red * 1.5 && green > red * 1.25)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
