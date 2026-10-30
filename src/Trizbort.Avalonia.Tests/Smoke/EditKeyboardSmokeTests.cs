using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests.Smoke;

/// <summary>
/// Automated versions of smoke-test-script.md sections 2 (Edit &amp; selection) and 4 (Canvas keyboard).
/// Steps left manual: none in section 2 except the OS-clipboard part of step 13;
/// none in section 4 (visual checks are covered by the assertions below).
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Smoke")]
public class EditKeyboardSmokeTests
{
    private Project? _previousProject;
    private bool _previousSnap;
    private float _previousGrid;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        _previousSnap = Settings.SnapToGrid;
        _previousGrid = Settings.GridSize;
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
        Settings.SnapToGrid = _previousSnap;
        Settings.GridSize = _previousGrid;
    }

    // ---- helpers ------------------------------------------------------------------------------

    private static (Window Window, MapCanvasControl Canvas, CommandService Service) CreateHarness()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Content = canvas, Width = 1200, Height = 900 };
        window.Show();
        window.UpdateLayout();
        canvas.Focus();
        service.SetView(Vector.Zero, 1.0f);
        return (window, canvas, service);
    }

    private static Room AddRoom(float x, float y, string name = "Room")
    {
        var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y), Size = new Vector(96, 64) };
        Project.Current.Elements.Add(room);
        return room;
    }

    private static void RaiseKey(MapCanvasControl canvas, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        canvas.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers,
            Source = canvas
        });
    }

    private static void PumpUntil(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            if (!condition()) System.Threading.Thread.Sleep(10);
        }
    }

    private static string ReadMainWindowXaml()
    {
        // Compiled AXAML is not available as a raw resource, so read the source file from the repository.
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Trizbort.Avalonia", "Views", "MainWindow.axaml");
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not find src/Trizbort.Avalonia/Views/MainWindow.axaml above the test directory.");
    }

    // ---- Section 2: Edit & selection ----------------------------------------------------------

    /// <summary>Step 10: Press Ctrl+A. Expected: all elements are selected.</summary>
    [AvaloniaTest]
    public void S2_10_CtrlA_SelectsAllElements()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            var a = AddRoom(0, 0, "A");
            var b = AddRoom(200, 0, "B");

            window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);

            service.SelectedElements.Count.ShouldBe(2);
            service.SelectedElements.ShouldContain(a);
            service.SelectedElements.ShouldContain(b);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 11: Click an empty part of the canvas. Expected: selection clears.</summary>
    [AvaloniaTest]
    public void S2_11_ClickEmptyCanvas_ClearsSelection()
    {
        var (window, canvas, service) = CreateHarness();
        try
        {
            var room = AddRoom(0, 0);
            service.Select(room);
            service.SelectedElements.Count.ShouldBe(1);

            var p = canvas.CanvasToClient(new Vector(300, 200)); // empty space, inside the 1200x900 view
            window.MouseMove(p);
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
            service.SelectedElements.Count.ShouldBe(0);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 12: Select one room, press Ctrl+C then Ctrl+V. Expected: a copy of the room is pasted (K043: format/ID handling unverified).</summary>
    [AvaloniaTest]
    public void S2_12_CtrlC_CtrlV_PastesCopyOfRoom()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            var room = AddRoom(0, 0, "Original");
            service.Select(room);

            window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();
            window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
            PumpUntil(() => Project.Current.Elements.OfType<Room>().Count() >= 2);

            var rooms = Project.Current.Elements.OfType<Room>().ToList();
            rooms.Count.ShouldBe(2);
            rooms.ShouldContain(room);
            rooms.Single(r => r != room).Name.ShouldBe("Original");
        }
        finally { window.Close(); }
    }

    /// <summary>Step 13: Paste when the clipboard holds plain text. Expected: nothing should crash and no elements are added. (OS clipboard interplay with other apps stays manual.)</summary>
    [AvaloniaTest]
    public void S2_13_PastePlainText_DoesNotCrashOrAddElements()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            AddRoom(0, 0);

            Should.NotThrow(() => service.Paste("just some plain text, not trizbort data"));

            Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 14: Select one room and press Delete. Expected: the room and its connections are removed (K046: not compared against reference).</summary>
    [AvaloniaTest]
    public void S2_14_Delete_RemovesRoomAndItsConnections()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            var a = AddRoom(-150, 0, "A");
            var b = AddRoom(150, 0, "B");
            var portA = a.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
            var portB = b.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.West);
            var conn = new Connection(Project.Current, new Vertex(portA), new Vertex(portB));
            Project.Current.Elements.Add(conn);
            service.Select(a);

            window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);

            Project.Current.Elements.OfType<Room>().ShouldNotContain(a);
            Project.Current.Elements.OfType<Room>().ShouldContain(b);
            Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();
        }
        finally { window.Close(); }
    }

    /// <summary>Step 15: Press R. Expected (reference): R adds a room. Avalonia has no R key (known gap: M030).</summary>
    [AvaloniaTest]
    [Ignore("known gap: M030")]
    public void S2_15_RKey_AddsRoom()
    {
        var (window, _, _) = CreateHarness();
        try
        {
            window.KeyPressQwerty(PhysicalKey.R, RawInputModifiers.None);

            Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);
        }
        finally { window.Close(); }
    }

    // ---- Section 4: Canvas keyboard -----------------------------------------------------------

    /// <summary>Step 30 (first half): Select one room and press an arrow key. Expected: the room moves one step.</summary>
    [AvaloniaTest]
    public void S4_30_ArrowKeys_NudgeSelection()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            var room = AddRoom(0, 0);
            service.Select(room);

            window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
            room.Position.X.ShouldBe(2f, 0.1f);

            window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            room.Position.Y.ShouldBe(2f, 0.1f);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 30 (second half): With no selection an arrow key should pan the canvas (known gap: K012).</summary>
    [AvaloniaTest]
    [Ignore("known gap: K012")]
    public void S4_30_ArrowKeys_NoSelection_PansCanvas()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            AddRoom(0, 0);
            service.ClearSelection();
            var before = service.Origin;

            window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);

            service.Origin.ShouldNotBe(before);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 31: Select one room and press Ctrl+Arrow. Expected: Avalonia nudges the selection one step (differs: K013).</summary>
    [AvaloniaTest]
    public void S4_31_CtrlArrow_NudgesSelectionOneStep()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            var room = AddRoom(0, 0);
            service.Select(room);

            window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Control);

            room.Position.X.ShouldBe(2f, 0.1f);
            room.Position.Y.ShouldBe(0f, 0.1f);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 32: Select one room and press Shift+Arrow. Expected: Avalonia moves the selection by twice the grid (differs: K014).</summary>
    [AvaloniaTest]
    public void S4_32_ShiftArrow_MovesSelectionTwiceGrid()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            var room = AddRoom(0, 0);
            service.Select(room);

            window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Shift);

            room.Position.X.ShouldBe(Settings.GridSize * 2, 0.1f);
            room.Position.Y.ShouldBe(0f, 0.1f);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 33: Start dragging a room, then press Esc before releasing. Expected: the drag is cancelled and the room returns to its start position.</summary>
    [AvaloniaTest]
    [Ignore("known gap: I-010 (Esc does not restore a dragged room)")]
    public void S4_33_EscDuringRoomDrag_CancelsAndRestoresPosition()
    {
        var (window, canvas, service) = CreateHarness();
        try
        {
            var room = AddRoom(0, 0);
            service.Select(room);
            var start = canvas.CanvasToClient(new Vector(48, 32));
            var moved = canvas.CanvasToClient(new Vector(148, 82));

            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(moved);
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveElement);
            room.Position.X.ShouldNotBe(0f);

            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            room.Position.X.ShouldBe(0f, 0.5f);
            room.Position.Y.ShouldBe(0f, 0.5f);

            window.MouseUp(moved, MouseButton.Left);
            room.Position.X.ShouldBe(0f, 0.5f);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 34: Press + then - (and NumPad+ / NumPad-). Expected: canvas zooms in, then out.</summary>
    [AvaloniaTest]
    public void S4_34_PlusMinus_ZoomInThenOut()
    {
        var (window, canvas, service) = CreateHarness();
        try
        {
            RaiseKey(canvas, Key.OemPlus);
            var zoomedIn = service.ZoomFactor;
            zoomedIn.ShouldBeGreaterThan(1.0f);

            RaiseKey(canvas, Key.OemMinus);
            service.ZoomFactor.ShouldBeLessThan(zoomedIn);

            service.SetView(Vector.Zero, 1.0f);
            RaiseKey(canvas, Key.Add);
            service.ZoomFactor.ShouldBeGreaterThan(1.0f);

            service.SetView(Vector.Zero, 1.0f);
            RaiseKey(canvas, Key.Subtract);
            service.ZoomFactor.ShouldBeLessThan(1.0f);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 35: Press Ctrl+0. Expected: zoom resets to 100% (differs: K030).</summary>
    [AvaloniaTest]
    public void S4_35_CtrlZero_ResetsZoomTo100Percent()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            service.SetView(Vector.Zero, 2.0f);

            window.KeyPressQwerty(PhysicalKey.Digit0, RawInputModifiers.Control);

            service.ZoomFactor.ShouldBe(1.0f, 0.001f);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 36: Press Ctrl+Shift+A. Expected (reference): selects the rooms in the selected rooms' regions, not everything (known gap: K004 / I-004).</summary>
    [AvaloniaTest]
    [Ignore("known gap: K004 / I-004")]
    public void S4_36_CtrlShiftA_SelectsRoomsInSelectedRegions()
    {
        var (window, _, service) = CreateHarness();
        try
        {
            var a1 = AddRoom(0, 0, "A1");
            var a2 = AddRoom(200, 0, "A2");
            var b1 = AddRoom(400, 0, "B1");
            a1.Region = "North";
            a2.Region = "North";
            b1.Region = "South";
            service.Select(a1);

            window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control | RawInputModifiers.Shift);

            service.SelectedElements.ShouldContain(a1);
            service.SelectedElements.ShouldContain(a2);
            service.SelectedElements.ShouldNotContain(b1);
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// Step 37 (first half): Alt+Enter opens Properties. The binding lives on MainWindow (Window.KeyBindings),
    /// which cannot be instantiated headlessly without App resources, so this checks the declared binding.
    /// </summary>
    [AvaloniaTest]
    public void S4_37_AltEnter_IsBoundToProperties()
    {
        var xaml = ReadMainWindowXaml();

        xaml.ShouldContain("Gesture=\"Alt+Enter\" Command=\"{Binding ShowSelectedElementPropertiesCommand}\"");
    }

    /// <summary>Step 37 (second half): Plain Enter with a selected room should open Properties at canvas level, as in the reference (known gap: K026 / M029).</summary>
    [AvaloniaTest]
    [Ignore("known gap: K026 / M029")]
    public void S4_37_PlainEnter_OpensPropertiesOnCanvas()
    {
        var (window, canvas, service) = CreateHarness();
        try
        {
            var room = AddRoom(0, 0);
            service.Select(room);
            Element? requested = null;
            canvas.PropertiesDialogOverride = e => { requested = e; return Task.FromResult(false); };

            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

            requested.ShouldBeSameAs(room);
        }
        finally { window.Close(); }
    }

    /// <summary>Step 38: Press F1. Expected: About dialog opens (differs: K024). Checks the MainWindow key binding to the About command.</summary>
    [AvaloniaTest]
    public void S4_38_F1_IsBoundToAbout()
    {
        var xaml = ReadMainWindowXaml();

        xaml.ShouldContain("Gesture=\"F1\" Command=\"{Binding ShowAboutCommand}\"");
    }

    /// <summary>Step 39: Press Ctrl+, . Expected (reference): opens Map Settings. Avalonia has no binding (known gap: M065).</summary>
    [AvaloniaTest]
    [Ignore("known gap: M065")]
    public void S4_39_CtrlComma_IsBoundToMapSettings()
    {
        var xaml = ReadMainWindowXaml();

        xaml.ShouldContain("Gesture=\"Ctrl+OemComma\" Command=\"{Binding ShowMapSettingsCommand}\"");
    }
}
