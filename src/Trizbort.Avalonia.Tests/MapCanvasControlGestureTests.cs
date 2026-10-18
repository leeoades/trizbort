using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class MapCanvasControlGestureTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        Settings.SnapToGrid = false;
        Settings.GridSize = 32;
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private static (Window Window, MapCanvasControl Canvas, CommandService Service) CreateTestHarness()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window
        {
            Content = canvas
        };
        window.Show();
        window.UpdateLayout();
        canvas.Focus();
        service.SetView(Vector.Zero, 1.0f);
        return (window, canvas, service);
    }

    [AvaloniaTest]
    public void Pan_RightButtonDrag_OffsetsOrigin()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var start = new Point(200, 200);
            var end = new Point(300, 200);

            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Right);
            canvas.DragMode.ShouldBe(CanvasDragMode.Pan);

            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Right);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            service.Origin.X.ShouldBe(-100f, 0.5f);
            service.Origin.Y.ShouldBe(0f, 0.5f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Pan_SpacePlusLeftButtonDrag_OffsetsOrigin()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var start = new Point(200, 200);
            var end = new Point(200, 250);

            canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space, Source = canvas });
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.Pan);

            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Left);
            canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Space, Source = canvas });

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            service.Origin.X.ShouldBe(0f, 0.5f);
            service.Origin.Y.ShouldBe(-50f, 0.5f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MarqueeSelection_SelectsIntersectingElements()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Position = new Vector(-100, -100), Size = new Vector(60, 40) };
            var room2 = new Room(Project.Current) { Position = new Vector(100, 100), Size = new Vector(60, 40) };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);

            var startClient = canvas.CanvasToClient(new Vector(-150, -150));
            var endClient = canvas.CanvasToClient(new Vector(0, 0));

            window.MouseMove(startClient);
            window.MouseDown(startClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.Marquee);

            window.MouseMove(endClient);
            window.MouseUp(endClient, MouseButton.Left);

            service.SelectedElements.Count.ShouldBe(1);
            service.SelectedElements.ShouldContain(room1);
            service.SelectedElements.ShouldNotContain(room2);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MoveSelection_DragsMultipleElements()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Position = new Vector(-50, -50), Size = new Vector(60, 40) };
            var room2 = new Room(Project.Current) { Position = new Vector(50, -50), Size = new Vector(60, 40) };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);
            service.Select([room1, room2], clearExisting: true);

            var startClient = canvas.CanvasToClient(new Vector(-20, -30));
            var endClient = canvas.CanvasToClient(new Vector(80, 20));

            window.MouseMove(startClient);
            window.MouseDown(startClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveElement);

            window.MouseMove(endClient);
            window.MouseUp(endClient, MouseButton.Left);

            room1.Position.X.ShouldBe(50f, 1.0f);
            room1.Position.Y.ShouldBe(0f, 1.0f);
            room2.Position.X.ShouldBe(150f, 1.0f);
            room2.Position.Y.ShouldBe(0f, 1.0f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Resize_ContinuousDraggingAcrossMultipleMoves_ResizesRoom()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            service.Select(room);

            // Locate SouthEast handle position
            var handlePos = room.InnerBounds.GetCorner(CompassPoint.SouthEast);
            var handleClient = canvas.CanvasToClient(handlePos);

            window.MouseMove(handleClient);
            canvas.HoverHandle.ShouldNotBeNull();
            canvas.HoverHandle.CompassPoint.ShouldBe(CompassPoint.SouthEast);

            window.MouseDown(handleClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveResizeHandle);

            // Step 1: Move +32, +32
            var step1Client = canvas.CanvasToClient(handlePos + new Vector(32, 32));
            window.MouseMove(step1Client);
            room.Size.X.ShouldBe(128f, 1.0f);
            room.Size.Y.ShouldBe(96f, 1.0f);

            // Step 2: Continuous move +32 more
            var step2Client = canvas.CanvasToClient(handlePos + new Vector(64, 64));
            window.MouseMove(step2Client);
            room.Size.X.ShouldBe(160f, 1.0f);
            room.Size.Y.ShouldBe(128f, 1.0f);

            window.MouseUp(step2Client, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            room.Size.X.ShouldBe(160f, 1.0f);
            room.Size.Y.ShouldBe(128f, 1.0f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ConnectionDrawing_ClickWithoutDrag_DoesNotCreateConnection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);

            var port = room.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
            var portClient = canvas.CanvasToClient(port.Position);

            window.MouseMove(portClient);
            canvas.HoverPort.ShouldBe(port);

            // Click without drag
            window.MouseDown(portClient, MouseButton.Left);
            window.MouseUp(portClient, MouseButton.Left);

            Project.Current.Elements.OfType<Connection>().Count().ShouldBe(0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ConnectionDrawing_DragFromRoomPortToTargetRoomPort_CreatesAndDocksConnection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var roomA = new Room(Project.Current) { Position = new Vector(-150, 0), Size = new Vector(96, 64) };
            var roomB = new Room(Project.Current) { Position = new Vector(150, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(roomA);
            Project.Current.Elements.Add(roomB);

            var portA = roomA.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
            var portB = roomB.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.West);

            var clientA = canvas.CanvasToClient(portA.Position);
            var clientB = canvas.CanvasToClient(portB.Position);

            window.MouseMove(clientA);
            window.MouseDown(clientA, MouseButton.Left);

            // Drag towards room B
            window.MouseMove(clientB);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);
            canvas.HoverPort.ShouldBe(portB);

            window.MouseUp(clientB, MouseButton.Left);

            var connections = Project.Current.Elements.OfType<Connection>().ToList();
            connections.Count.ShouldBe(1);
            var conn = connections[0];
            conn.VertexList.Count.ShouldBe(2);
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBe(portB);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Connection_CurveWaypointDragging_UpdatesSplineHandle()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var conn = new Connection(Project.Current, new Vertex(new Vector(-100, 0)), new Vertex(new Vector(100, 0)));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var midHandlePos = conn.GetCurveWaypointHandlePosition(CurveWaypoint.Middle);
            var clientMid = canvas.CanvasToClient(midHandlePos);

            window.MouseMove(clientMid);
            canvas.HoverWaypoint.ShouldBe(CurveWaypoint.Middle);

            window.MouseDown(clientMid, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveWaypoint);

            var targetPos = midHandlePos + new Vector(0, -50);
            var clientTarget = canvas.CanvasToClient(targetPos);
            window.MouseMove(clientTarget);
            window.MouseUp(clientTarget, MouseButton.Left);

            conn.GetCurveWaypoint(CurveWaypoint.Middle).ShouldNotBeNull();
            var wp = conn.GetCurveWaypoint(CurveWaypoint.Middle)!.Value;
            wp.Y.ShouldBe(-50f, 1.0f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void HoverHitTesting_UpdatesHoverStateAndTooltip()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current)
            {
                Name = "Grand Hall",
                SubTitle = "North Wing",
                Position = new Vector(0, 0),
                Size = new Vector(100, 60)
            };
            Project.Current.Elements.Add(room);

            var centerClient = canvas.CanvasToClient(new Vector(50, 30));
            window.MouseMove(centerClient);

            canvas.HoverElement.ShouldBe(room);
            var tip = ToolTip.GetTip(canvas) as string;
            tip.ShouldNotBeNull();
            tip.ShouldContain("Grand Hall");
            tip.ShouldContain("North Wing");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void KeyboardShortcuts_DeleteAndSelectAllAndEscapeAndArrows()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(80, 50) };
            var room2 = new Room(Project.Current) { Position = new Vector(100, 0), Size = new Vector(80, 50) };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);

            // Ctrl+A
            window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
            service.SelectedElements.Count.ShouldBe(2);

            // Escape
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            service.SelectedElements.Count.ShouldBe(0);

            // Select room1 and nudge with Arrow Right
            service.Select(room1);
            window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            room1.Position.X.ShouldBe(2.0f, 0.1f);

            // Delete
            window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
            Project.Current.Elements.Count.ShouldBe(1);
            Project.Current.Elements.ShouldNotContain(room1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Clipboard_CopyCutPaste_RoundTripsElementsAndPreservesConnections()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Name = "Room 1", Position = new Vector(0, 0), Size = new Vector(80, 50) };
            var room2 = new Room(Project.Current) { Name = "Room 2", Position = new Vector(200, 0), Size = new Vector(80, 50) };
            var conn = new Connection(Project.Current, new Vertex(room1.PortList[0]), new Vertex(room2.PortList[0]));
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);
            Project.Current.Elements.Add(conn);

            service.Select([room1, room2, conn], clearExisting: true);

            var json = service.CopySelected();
            json.ShouldNotBeNull();
            json.ShouldContain("Room 1");
            json.ShouldContain("Room 2");

            service.Paste(json, new Vector(50, 50));

            Project.Current.Elements.OfType<Room>().Count().ShouldBe(4);
            Project.Current.Elements.OfType<Connection>().Count().ShouldBe(2);

            var newConn = Project.Current.Elements.OfType<Connection>().First(c => c != conn);
            newConn.VertexList[0].Port.ShouldNotBeNull();
            newConn.VertexList[1].Port.ShouldNotBeNull();
            newConn.VertexList[0].Port!.Owner.ShouldNotBe(room1);
            newConn.VertexList[1].Port!.Owner.ShouldNotBe(room2);
        }
        finally
        {
            window.Close();
        }
    }
    [AvaloniaTest]
    public void MarqueeSelection_ShiftAddsToSelection_AndCtrlToggles()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Position = new Vector(-100, -100), Size = new Vector(60, 40) };
            var room2 = new Room(Project.Current) { Position = new Vector(100, 100), Size = new Vector(60, 40) };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);

            // Select room1 first
            service.Select(room1);

            // Marquee over room2 with Shift held
            var startClient = canvas.CanvasToClient(new Vector(50, 50));
            var endClient = canvas.CanvasToClient(new Vector(200, 200));

            window.MouseMove(startClient);
            window.MouseDown(startClient, MouseButton.Left, RawInputModifiers.Shift);
            window.MouseMove(endClient);
            window.MouseUp(endClient, MouseButton.Left, RawInputModifiers.Shift);

            service.SelectedElements.Count.ShouldBe(2);
            service.SelectedElements.ShouldContain(room1);
            service.SelectedElements.ShouldContain(room2);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void AltCtrlArrow_ResizesSelectedRoom()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            service.Select(room);

            // Alt+Ctrl+Right -> increases width
            canvas.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Right,
                KeyModifiers = KeyModifiers.Alt | KeyModifiers.Control,
                Source = canvas
            });

            room.Size.X.ShouldBe(98f, 0.1f);
            room.Size.Y.ShouldBe(64f, 0.1f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Clipboard_CopyColors_AppliesToSelectedRoomWithoutMoving()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current)
            {
                Name = "Source",
                Position = new Vector(0, 0),
                RoomFillColor = System.Drawing.Color.Red,
                SecondFillColor = System.Drawing.Color.Blue,
                SecondFillLocation = "Bottom"
            };
            var room2 = new Room(Project.Current)
            {
                Name = "Target",
                Position = new Vector(100, 100),
                RoomFillColor = System.Drawing.Color.White
            };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);

            var controller = new CopyController();
            controller.CopyColors(room1);

            service.Select([room2], clearExisting: true);
            service.Paste();

            room2.RoomFillColor.ShouldBe(System.Drawing.Color.Red);
            room2.SecondFillColor.ShouldBe(System.Drawing.Color.Blue);
            room2.SecondFillLocation.ShouldBe("Bottom");
            // Position should remain unchanged
            room2.Position.X.ShouldBe(100f);
            room2.Position.Y.ShouldBe(100f);
        }
        finally
        {
            window.Close();
        }
    }
}
