using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;
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
    public void RightDrag_IncrementalSlowMove_DoesNotOpenMenu()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var start = new Point(200, 200);
            
            window.MouseDown(start, MouseButton.Right);
            
            // Perform several small moves (1px) to simulate slow pan. 
            // Total displacement 5px > 2px threshold.
            for (int i = 0; i < 5; i++)
            {
                window.MouseMove(new Point(start.X + i, start.Y));
            }
            
            window.MouseUp(new Point(start.X + 5, start.Y), MouseButton.Right);
            
            // Should have panned (so origin moved)
            service.Origin.X.ShouldNotBe(0f);
            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            canvas.LastContextMenu.ShouldBeNull();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void RightClick_WithoutMovement_OpensContextMenu()
    {
        var theme = new global::Avalonia.Themes.Fluent.FluentTheme();
        Application.Current!.Styles.Add(theme);
        var (window, canvas, _) = CreateTestHarness();
        try
        {
            var p = new Point(200, 200);
            window.MouseDown(p, MouseButton.Right);
            window.MouseUp(p, MouseButton.Right);

            canvas.LastContextMenu.ShouldNotBeNull();
            canvas.LastContextMenu!.IsOpen.ShouldBeTrue();
            canvas.LastContextMenu.Close();
        }
        finally
        {
            Application.Current!.Styles.Remove(theme);
            window.Close();
        }
    }

    [AvaloniaTest]
    public void DoubleClick_OnRoom_SelectsAndRequestsProperties()
    {
        var (window, canvas, _) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Name = "Dbl", Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            Element? requested = null;
            canvas.PropertiesDialogOverride = e => { requested = e; return Task.FromResult(false); };

            var p = canvas.CanvasToClient(new Vector(48, 32));
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);

            requested.ShouldBeSameAs(room);
            Project.Current.GetSelectedElements().ShouldContain(room);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void DoubleClick_OnEmptySpace_AddsRoomWhenSettingEnabled()
    {
        var original = ApplicationSettingsController.AppSettings.DoubleClickToAddRoom;
        var (window, _, _) = CreateTestHarness();
        try
        {
            ApplicationSettingsController.AppSettings.DoubleClickToAddRoom = true;
            var p = new Point(300, 300);
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);

            Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);

            ApplicationSettingsController.AppSettings.DoubleClickToAddRoom = false;
            var q = new Point(600, 500);
            window.MouseDown(q, MouseButton.Left);
            window.MouseUp(q, MouseButton.Left);
            window.MouseDown(q, MouseButton.Left);
            window.MouseUp(q, MouseButton.Left);
            Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);
        }
        finally
        {
            ApplicationSettingsController.AppSettings.DoubleClickToAddRoom = original;
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
                Position = new Vector(0, 0),
                Size = new Vector(100, 60)
            };
            room.Descriptions.Add("Hall description");
            Project.Current.Elements.Add(room);

            var centerClient = canvas.CanvasToClient(new Vector(50, 30));
            window.MouseMove(centerClient);

            canvas.HoverElement.ShouldBe(room);
            var tip = ToolTip.GetTip(canvas) as string;
            tip.ShouldNotBeNull();
            tip.ShouldContain("Grand Hall");
            tip.ShouldContain("Hall description");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void KeyboardShortcuts_SelectAllEscapeArrowNudgeAndDelete()
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
    public void CommandService_CopyAndPaste_RoundTripsElementsAndPreservesConnections()
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
    public void MarqueeSelection_ShiftOverUnselectedElement_AddsToExistingSelection()
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
    public void CommandService_PasteAfterCopyColors_AppliesToSelectedRoomWithoutMoving()
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

    // ---- Lifecycle / cancellation -------------------------------------------------------------

    private static (Room A, Room B, Room.CompassPort PortAEast, Room.CompassPort PortBWest) AddTwoRooms()
    {
        var roomA = new Room(Project.Current) { Position = new Vector(-150, 0), Size = new Vector(96, 64) };
        var roomB = new Room(Project.Current) { Position = new Vector(150, 0), Size = new Vector(96, 64) };
        Project.Current.Elements.Add(roomA);
        Project.Current.Elements.Add(roomB);
        var portA = roomA.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
        var portB = roomB.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.West);
        return (roomA, roomB, portA, portB);
    }

    private static IPointer CapturePointerOnNextMove(MapCanvasControl canvas, Window window, Point where)
    {
        IPointer? pointer = null;
        void Handler(object? sender, PointerEventArgs e) => pointer ??= e.Pointer;
        canvas.AddHandler(InputElement.PointerMovedEvent, Handler, RoutingStrategies.Tunnel);
        window.MouseMove(where);
        canvas.RemoveHandler(InputElement.PointerMovedEvent, Handler);
        pointer.ShouldNotBeNull();
        return pointer;
    }

    [AvaloniaTest]
    public void SecondButtonPress_DuringConnectionDrag_IsIgnoredAndDragStillDocks()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, _, portA, portB) = AddTwoRooms();
            var clientA = canvas.CanvasToClient(portA.Position);
            var clientB = canvas.CanvasToClient(portB.Position);
            var clientMid = canvas.CanvasToClient(new Vector(0, 32));

            window.MouseMove(clientA);
            window.MouseDown(clientA, MouseButton.Left);
            var pointer = CapturePointerOnNextMove(canvas, window, clientMid);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);

            // Secondary button press and release before primary button release must not cancel the primary drag
            window.MouseDown(clientMid, MouseButton.Right);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);
            window.MouseUp(clientMid, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);
            pointer.Captured.ShouldBe(canvas);

            window.MouseMove(clientB);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);
            window.MouseUp(clientB, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            service.Origin.ShouldBe(Vector.Zero);
            pointer.Captured.ShouldBeNull();

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            var conn = Project.Current.Elements.OfType<Connection>().Single();
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBe(portB);
            service.SelectedElements.ShouldContain(conn);

            // Detaching control also leaves captured as null without lingering captures
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            pointer.Captured.ShouldBeNull();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void SecondButtonPress_DuringResize_DoesNotSwitchToPan()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            service.Select(room);

            var handlePos = room.InnerBounds.GetCorner(CompassPoint.SouthEast);
            var handleClient = canvas.CanvasToClient(handlePos);
            window.MouseMove(handleClient);
            window.MouseDown(handleClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveResizeHandle);

            // Secondary button press and release before primary button release must not cancel resizing
            window.MouseDown(handleClient, MouseButton.Middle);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveResizeHandle);
            window.MouseUp(handleClient, MouseButton.Middle);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveResizeHandle);

            window.MouseMove(canvas.CanvasToClient(handlePos + new Vector(32, 32)));
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveResizeHandle);
            room.Size.X.ShouldBe(128f, 1.0f);
            service.Origin.ShouldBe(Vector.Zero);
            window.MouseUp(canvas.CanvasToClient(handlePos + new Vector(32, 32)), MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.None);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void PointerCaptureLost_WhileConnectionBackOnSourcePort_RemovesZeroLengthConnection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, _, portA, _) = AddTwoRooms();
            var clientA = canvas.CanvasToClient(portA.Position);
            var clientAway = canvas.CanvasToClient(portA.Position + new Vector(40, 40));

            window.MouseMove(clientA);
            window.MouseDown(clientA, MouseButton.Left);
            var pointer = CapturePointerOnNextMove(canvas, window, clientAway);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);
            Project.Current.Elements.OfType<Connection>().Count().ShouldBe(1);

            window.MouseMove(clientA);
            pointer.Capture(null);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            canvas.IsGestureActive.ShouldBeFalse();
            Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();
            service.SelectedElements.ShouldBeEmpty();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void PointerCaptureLost_DuringMarquee_CancelsWithoutChangingSelection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(-100, -100), Size = new Vector(60, 40) };
            Project.Current.Elements.Add(room);

            var start = canvas.CanvasToClient(new Vector(-150, -150));
            var end = canvas.CanvasToClient(new Vector(0, 0));
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            var pointer = CapturePointerOnNextMove(canvas, window, end);
            canvas.DragMode.ShouldBe(CanvasDragMode.Marquee);

            pointer.Capture(null);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            service.SelectedElements.ShouldBeEmpty();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Escape_DuringNewConnectionDrag_DiscardsConnectionAndEndsGesture()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, _, portA, portB) = AddTwoRooms();
            var clientA = canvas.CanvasToClient(portA.Position);
            var clientB = canvas.CanvasToClient(portB.Position);

            window.MouseMove(clientA);
            window.MouseDown(clientA, MouseButton.Left);
            window.MouseMove(clientB);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);
            Project.Current.Elements.OfType<Connection>().Count().ShouldBe(1);

            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();

            window.MouseUp(clientB, MouseButton.Left);
            Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Escape_DuringResize_StopsResizingAndKeepsSelection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            service.Select(room);

            var handlePos = room.InnerBounds.GetCorner(CompassPoint.SouthEast);
            var handleClient = canvas.CanvasToClient(handlePos);
            window.MouseMove(handleClient);
            window.MouseDown(handleClient, MouseButton.Left);
            window.MouseMove(canvas.CanvasToClient(handlePos + new Vector(32, 32)));
            room.Size.X.ShouldBe(128f, 1.0f);

            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            service.SelectedElements.ShouldContain(room);

            window.MouseMove(canvas.CanvasToClient(handlePos + new Vector(96, 96)));
            room.Size.X.ShouldBe(128f, 1.0f);
        }
        finally
        {
            window.Close();
        }
    }

    // ---- Selection modifiers -----------------------------------------------------------------

    [AvaloniaTest]
    public void MarqueeSelection_CtrlOverSelectedAndUnselected_TogglesBoth()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Position = new Vector(-100, -100), Size = new Vector(60, 40) };
            var room2 = new Room(Project.Current) { Position = new Vector(100, 100), Size = new Vector(60, 40) };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);
            service.Select(room1);

            var start = canvas.CanvasToClient(new Vector(-150, -150));
            var end = canvas.CanvasToClient(new Vector(200, 200));
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left, RawInputModifiers.Control);
            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Left, RawInputModifiers.Control);

            service.SelectedElements.ShouldNotContain(room1);
            service.SelectedElements.ShouldContain(room2);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MarqueeSelection_ShiftOverAlreadySelected_TogglesItOff()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Position = new Vector(-100, -100), Size = new Vector(60, 40) };
            var room2 = new Room(Project.Current) { Position = new Vector(100, 100), Size = new Vector(60, 40) };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);
            service.Select([room1, room2], clearExisting: true);

            var start = canvas.CanvasToClient(new Vector(-150, -150));
            var end = canvas.CanvasToClient(new Vector(0, 0));
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left, RawInputModifiers.Shift);
            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Left, RawInputModifiers.Shift);

            service.SelectedElements.ShouldNotContain(room1);
            service.SelectedElements.ShouldContain(room2);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ClickWithCtrl_TogglesElementSelection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room1 = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(80, 50) };
            var room2 = new Room(Project.Current) { Position = new Vector(200, 0), Size = new Vector(80, 50) };
            Project.Current.Elements.Add(room1);
            Project.Current.Elements.Add(room2);
            service.Select(room1);

            var c2 = canvas.CanvasToClient(new Vector(240, 25));
            window.MouseMove(c2);
            window.MouseDown(c2, MouseButton.Left, RawInputModifiers.Control);
            window.MouseUp(c2, MouseButton.Left, RawInputModifiers.Control);
            service.SelectedElements.Count.ShouldBe(2);

            var c1 = canvas.CanvasToClient(new Vector(40, 25));
            window.MouseMove(c1);
            window.MouseDown(c1, MouseButton.Left, RawInputModifiers.Control);
            window.MouseUp(c1, MouseButton.Left, RawInputModifiers.Control);
            service.SelectedElements.ShouldNotContain(room1);
            service.SelectedElements.ShouldContain(room2);
        }
        finally
        {
            window.Close();
        }
    }

    // ---- Pan / zoom / snapping ---------------------------------------------------------------

    [AvaloniaTest]
    public void Pan_MiddleButtonDrag_OffsetsOrigin()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var start = new Point(200, 200);
            var end = new Point(250, 170);
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Middle);
            canvas.DragMode.ShouldBe(CanvasDragMode.Pan);
            canvas.CursorType.ShouldBe(StandardCursorType.Hand);
            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Middle);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            service.Origin.X.ShouldBe(-50f, 0.5f);
            service.Origin.Y.ShouldBe(30f, 0.5f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MoveSelection_WithSnapToGrid_MovesInGridSteps()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            Settings.SnapToGrid = true;
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            service.Select(room);

            var start = canvas.CanvasToClient(new Vector(48, 32));
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveElement);

            window.MouseMove(canvas.CanvasToClient(new Vector(48 + 40, 32)));
            (room.Position.X % 32f).ShouldBe(0f, 0.01f);
            room.Position.X.ShouldBe(32f, 0.01f);

            window.MouseMove(canvas.CanvasToClient(new Vector(48 + 70, 32 + 20)));
            (room.Position.X % 32f).ShouldBe(0f, 0.01f);
            (room.Position.Y % 32f).ShouldBe(0f, 0.01f);
            room.Position.X.ShouldBeGreaterThanOrEqualTo(64f);

            window.MouseUp(canvas.CanvasToClient(new Vector(48 + 70, 32 + 20)), MouseButton.Left);
        }
        finally
        {
            Settings.SnapToGrid = false;
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ZoomShortcuts_PlusMinusAndCtrlZero_ChangeAndResetZoom()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            window.KeyPressQwerty(PhysicalKey.Equal, RawInputModifiers.None);
            service.ZoomFactor.ShouldBeGreaterThan(1.0f);

            window.KeyPressQwerty(PhysicalKey.Minus, RawInputModifiers.None);
            window.KeyPressQwerty(PhysicalKey.Minus, RawInputModifiers.None);
            service.ZoomFactor.ShouldBeLessThan(1.0f);

            service.Origin = new Vector(100, 100);
            window.KeyPressQwerty(PhysicalKey.Digit0, RawInputModifiers.Control);
            service.ZoomFactor.ShouldBe(1.0f, 0.001f);
            service.Origin.ShouldBe(Vector.Zero);
        }
        finally
        {
            window.Close();
        }
    }

    // ---- Re-docking / cursor ----------------------------------------------------------------

    [AvaloniaTest]
    public void Redock_ExistingConnectionEnd_MovesToAnotherRoomPort()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, roomB, portA, portB) = AddTwoRooms();
            var roomC = new Room(Project.Current) { Position = new Vector(150, 200), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(roomC);
            var portC = roomC.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.West);

            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portB));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var endClient = canvas.CanvasToClient(conn.VertexList[1].Position);
            window.MouseMove(endClient);
            canvas.HoverPort.ShouldBeAssignableTo<MoveablePort>();
            canvas.CursorType.ShouldBe(StandardCursorType.Hand);

            window.MouseDown(endClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);

            var targetClient = canvas.CanvasToClient(portC.Position);
            window.MouseMove(targetClient);
            canvas.HoverPort.ShouldBe(portC);
            window.MouseUp(targetClient, MouseButton.Left);

            Project.Current.Elements.OfType<Connection>().ShouldContain(conn);
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBe(portC);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Escape_DuringExistingConnectionEndpointDrag_RestoresOriginalDockedPort()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, _, portA, portB) = AddTwoRooms();
            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portB));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var endClient = canvas.CanvasToClient(conn.VertexList[1].Position);
            window.MouseMove(endClient);
            window.MouseDown(endClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);

            // Drag into empty space
            window.MouseMove(canvas.CanvasToClient(new Vector(0, 200)));
            conn.VertexList[1].Port.ShouldBeNull();

            // Press Escape to cancel the drag
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            Project.Current.Elements.OfType<Connection>().ShouldContain(conn);
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBe(portB);
            service.SelectedElements.ShouldContain(conn);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void PointerCaptureLost_DuringExistingConnectionEndpointDrag_RestoresOriginalDockedPort()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, _, portA, portB) = AddTwoRooms();
            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portB));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var endClient = canvas.CanvasToClient(conn.VertexList[1].Position);
            var emptyClient = canvas.CanvasToClient(new Vector(0, 200));

            window.MouseMove(endClient);
            window.MouseDown(endClient, MouseButton.Left);
            var pointer = CapturePointerOnNextMove(canvas, window, emptyClient);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);
            conn.VertexList[1].Port.ShouldBeNull();

            // Simulate pointer capture loss
            pointer.Capture(null);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            Project.Current.Elements.OfType<Connection>().ShouldContain(conn);
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBe(portB);
            service.SelectedElements.ShouldContain(conn);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Escape_DuringExistingUndockedConnectionEndpointDrag_RestoresOriginalPosition()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var origPos0 = new Vector(-100, 50);
            var origPos1 = new Vector(100, 50);
            var conn = new Connection(Project.Current, new Vertex(origPos0), new Vertex(origPos1));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var endClient = canvas.CanvasToClient(origPos1);
            window.MouseMove(endClient);
            window.MouseDown(endClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);

            // Drag to a new position
            window.MouseMove(canvas.CanvasToClient(new Vector(200, 300)));
            conn.VertexList[1].Position.X.ShouldBe(200f, 1.0f);

            // Cancel with Escape
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            Project.Current.Elements.OfType<Connection>().ShouldContain(conn);
            conn.VertexList[1].Position.ShouldBe(origPos1);
            service.SelectedElements.ShouldContain(conn);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MovePort_ReleaseExistingConnectionEndpointInEmptySpace_LeavesFreeEndpoint()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, _, portA, portB) = AddTwoRooms();
            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portB));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var endClient = canvas.CanvasToClient(conn.VertexList[1].Position);
            var emptyWorld = new Vector(0, 150);
            var emptyClient = canvas.CanvasToClient(emptyWorld);

            window.MouseMove(endClient);
            window.MouseDown(endClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);

            window.MouseMove(emptyClient);
            window.MouseUp(emptyClient, MouseButton.Left);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            Project.Current.Elements.OfType<Connection>().ShouldContain(conn);
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBeNull();
            conn.VertexList[1].Position.Distance(emptyWorld).ShouldBeLessThan(1.0f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void MovePort_ExistingConnection_DroppingEndOnOtherEndsPort_RestoresPreviousDockingAndDoesNotDeleteConnection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (_, _, portA, portB) = AddTwoRooms();
            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portB));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var endClient = canvas.CanvasToClient(conn.VertexList[1].Position);
            var targetClient = canvas.CanvasToClient(portA.Position);

            window.MouseMove(endClient);
            window.MouseDown(endClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);

            // Drag B's endpoint onto A's port (the same port as vertex 0)
            window.MouseMove(targetClient);
            canvas.HoverPort.ShouldBe(portA);
            window.MouseUp(targetClient, MouseButton.Left);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            Project.Current.Elements.OfType<Connection>().ShouldContain(conn);
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBe(portB);
            service.SelectedElements.ShouldContain(conn);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Connection_BetweenTouchingRooms_IsNotPrunedAsDegenerate()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            // Room A from (-96, 0) to (0, 64), East port at (0, 32)
            // Room B from (0, 0) to (96, 64), West port at (0, 32)
            var roomA = new Room(Project.Current) { Name = "RoomA", Position = new Vector(-96, 0), Size = new Vector(96, 64) };
            var roomB = new Room(Project.Current) { Name = "RoomB", Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(roomA);
            Project.Current.Elements.Add(roomB);
            var portA = roomA.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
            var portB = roomB.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.West);
            var portBEast = roomB.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);

            portA.Position.Distance(portB.Position).ShouldBeLessThan(0.01f);

            // Create a connection from roomA East (0, 32) to roomB East (96, 32)
            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portBEast));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            // Drag vertex 1 from roomB East (96, 32) to roomB West (0, 32) touching roomA East
            var eastClient = canvas.CanvasToClient(portBEast.Position);
            var endClient = canvas.CanvasToClient(portB.Position);

            window.MouseMove(eastClient);
            window.MouseDown(eastClient, MouseButton.Left);
            canvas.DragMode.ShouldBe(CanvasDragMode.MovePort);

            window.MouseMove(endClient);
            canvas.HoverPort.ShouldBe(portB);
            window.MouseUp(endClient, MouseButton.Left);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            Project.Current.Elements.OfType<Connection>().ShouldContain(conn);
            conn.VertexList[0].Port.ShouldBe(portA);
            conn.VertexList[1].Port.ShouldBe(portB);
            conn.VertexList[0].Position.Distance(conn.VertexList[1].Position).ShouldBeLessThan(0.01f);
            service.SelectedElements.ShouldContain(conn);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void Cursor_ReflectsHoveredTarget()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);

            window.MouseMove(canvas.CanvasToClient(new Vector(-300, -300)));
            canvas.CursorType.ShouldBe(StandardCursorType.Arrow);

            service.Select(room);
            window.MouseMove(canvas.CanvasToClient(new Vector(48, 32)));
            canvas.CursorType.ShouldBe(StandardCursorType.SizeAll);

            window.MouseMove(canvas.CanvasToClient(room.InnerBounds.GetCorner(CompassPoint.SouthEast)));
            canvas.CursorType.ShouldBe(StandardCursorType.TopLeftCorner);

            window.MouseMove(canvas.CanvasToClient(room.InnerBounds.GetCorner(CompassPoint.NorthEast)));
            canvas.CursorType.ShouldBe(StandardCursorType.TopRightCorner);

            service.ClearSelection();
            var port = room.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
            window.MouseMove(canvas.CanvasToClient(new Vector(48, 32)));
            window.MouseMove(canvas.CanvasToClient(port.Position));
            canvas.HoverPort.ShouldBe(port);
            canvas.CursorType.ShouldBe(StandardCursorType.Cross);
        }
        finally
        {
            window.Close();
        }
    }

    // ---- Tooltips ---------------------------------------------------------------------------

    [AvaloniaTest]
    public void ToolTip_UsesCoreHeaderTextFooter_AndHonoursShowTooltipsSetting()
    {
        var (window, canvas, service) = CreateTestHarness();
        var settings = ApplicationSettingsController.AppSettings;
        var oldDesc = settings.ShowDescriptionsInTooltips;
        var oldObjects = settings.ShowObjectsInTooltips;
        try
        {
            var room = new Room(Project.Current)
            {
                Name = "Cellar",
                IsDark = true,
                Region = "Underground",
                Position = new Vector(0, 0),
                Size = new Vector(96, 64)
            };
            room.Descriptions.Add("A damp cellar.");
            Project.Current.Elements.Add(room);

            var tip = MapCanvasControl.BuildToolTipText(room);
            tip.ShouldNotBeNull();
            tip.ShouldContain("Cellar");
            tip.ShouldContain("(Dark)");
            tip.ShouldContain("A damp cellar.");
            tip.ShouldContain("Underground");

            var conn = new Connection(Project.Current, new Vertex(new Vector(-100, 0)), new Vertex(new Vector(-50, 0)))
            {
                Name = "Rusty door"
            };
            Project.Current.Elements.Add(conn);
            MapCanvasControl.BuildToolTipText(conn).ShouldNotBeNull();

            window.MouseMove(canvas.CanvasToClient(new Vector(48, 32)));
            (ToolTip.GetTip(canvas) as string).ShouldNotBeNull().ShouldContain("Cellar");

            settings.ShowDescriptionsInTooltips = false;
            settings.ShowObjectsInTooltips = false;
            MapCanvasControl.BuildToolTipText(room).ShouldBeNull();
            MapCanvasControl.BuildToolTipText(null).ShouldBeNull();
        }
        finally
        {
            settings.ShowDescriptionsInTooltips = oldDesc;
            settings.ShowObjectsInTooltips = oldObjects;
            window.Close();
        }
    }

    // ---- Real canvas clipboard path ---------------------------------------------------------

    private static void PumpUntil(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            if (!condition()) System.Threading.Thread.Sleep(10);
        }
    }

    private static string? ReadClipboardText(MapCanvasControl canvas)
    {
        Dispatcher.UIThread.RunJobs();
        var clipboard = TopLevel.GetTopLevel(canvas)!.Clipboard!;
        return clipboard.TryGetTextAsync().GetAwaiter().GetResult();
    }

    [AvaloniaTest]
    public void CanvasClipboardShortcuts_CtrlC_CtrlV_DuplicateElementsAndConnection()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var (roomA, roomB, portA, portB) = AddTwoRooms();
            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portB));
            Project.Current.Elements.Add(conn);
            service.Select([roomA, roomB, conn], clearExisting: true);

            window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
            ReadClipboardText(canvas).ShouldNotBeNullOrWhiteSpace();
            Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);

            window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
            PumpUntil(() => Project.Current.Elements.OfType<Room>().Count() >= 4);

            Project.Current.Elements.OfType<Room>().Count().ShouldBe(4);
            var copy = Project.Current.Elements.OfType<Connection>().Single(c => c != conn);
            copy.VertexList[0].Port!.Owner.ShouldNotBe(roomA);
            copy.VertexList[1].Port!.Owner.ShouldNotBe(roomB);
            Project.Current.Elements.OfType<Room>().ShouldContain((Room)copy.VertexList[0].Port!.Owner);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void CanvasClipboardShortcuts_CtrlX_RemovesSelectionAndCtrlVRestoresIt()
    {
        var (window, canvas, service) = CreateTestHarness();
        try
        {
            var room = new Room(Project.Current) { Name = "Cut Me", Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            service.Select(room);

            window.KeyPressQwerty(PhysicalKey.X, RawInputModifiers.Control);
            PumpUntil(() => ReadClipboardText(canvas) != null && Project.Current.Elements.Count == 0);
            ReadClipboardText(canvas).ShouldNotBeNull().ShouldContain("Cut Me");
            Project.Current.Elements.OfType<Room>().ShouldBeEmpty();

            window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
            PumpUntil(() => Project.Current.Elements.OfType<Room>().Any());
            var pasted = Project.Current.Elements.OfType<Room>().Single();
            pasted.Name.ShouldBe("Cut Me");
        }
        finally
        {
            window.Close();
        }
    }
}
