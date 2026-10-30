using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests.Smoke;

/// <summary>
/// Automated versions of smoke-test-script.md sections 3 (Canvas mouse) and 5 (Dialogs).
///
/// Steps already covered by MapCanvasControlGestureTests (not duplicated here):
///  18 marquee -> MarqueeSelection_SelectsIntersectingElements
///  17 ctrl-click -> ClickWithCtrl_TogglesElementSelection
///  19 drag + snap -> MoveSelection_WithSnapToGrid_MovesInGridSteps / MoveSelection_DragsMultipleElements
///  21 redock end -> Redock_ExistingConnectionEnd_MovesToAnotherRoomPort
///  23 waypoint drag -> Connection_CurveWaypointDragging_UpdatesSplineHandle
///  24 double-click room -> DoubleClick_OnRoom_SelectsAndRequestsProperties
///  26 right-drag pan -> Pan_RightButtonDrag_OffsetsOrigin
///  28 hover tooltip -> HoverHitTesting_UpdatesHoverStateAndTooltip
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Smoke")]
public class MouseDialogSmokeTests
{
    private Project? _previousProject;
    private bool _prevSnap;
    private float _prevGrid;
    private bool _prevInvert;
    private bool _prevShowFullPath;
    private bool _prevDescTips;
    private bool _prevObjTips;
    private List<Region>? _prevRegions;
    private FluentTheme? _addedTheme;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _prevSnap = Settings.SnapToGrid;
        _prevGrid = Settings.GridSize;
        _prevRegions = Settings.Regions;
        Settings.SnapToGrid = false;
        Settings.GridSize = 32;
        var app = ApplicationSettingsController.AppSettings;
        _prevInvert = app.InvertMouseWheel;
        _prevShowFullPath = app.ShowFullPathInTitleBar;
        _prevDescTips = app.ShowDescriptionsInTooltips;
        _prevObjTips = app.ShowObjectsInTooltips;
    }

    [TearDown]
    public void TearDown()
    {
        var app = ApplicationSettingsController.AppSettings;
        app.InvertMouseWheel = _prevInvert;
        app.ShowFullPathInTitleBar = _prevShowFullPath;
        app.ShowDescriptionsInTooltips = _prevDescTips;
        app.ShowObjectsInTooltips = _prevObjTips;
        Settings.SnapToGrid = _prevSnap;
        Settings.GridSize = _prevGrid;
        Settings.Regions = _prevRegions!;
        if (_addedTheme != null && Application.Current != null)
        {
            Application.Current.Styles.Remove(_addedTheme);
            _addedTheme = null;
        }
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    // ---- helpers -----------------------------------------------------------------------------

    private static (Window Window, MapCanvasControl Canvas, CommandService Service) CreateCanvas()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Content = canvas };
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

    private static void Click(Window window, Point p, RawInputModifiers mods = RawInputModifiers.None)
    {
        window.MouseMove(p);
        window.MouseDown(p, MouseButton.Left, mods);
        window.MouseUp(p, MouseButton.Left, mods);
    }

    private void EnsureTheme()
    {
        if (Application.Current != null && !Application.Current.Styles.OfType<FluentTheme>().Any())
        {
            _addedTheme = new FluentTheme();
            Application.Current.Styles.Add(_addedTheme);
        }
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static Window ShowDialog(Window window)
    {
        window.ShowInTaskbar = false;
        if (double.IsNaN(window.Width)) window.Width = 800;
        if (double.IsNaN(window.Height)) window.Height = 600;
        window.Show();
        Settle(window);
        return window;
    }

    /// <summary>Collects visible text from every tab of a dialog.</summary>
    private static List<string> CollectTexts(Window window)
    {
        var texts = new List<string>();

        void CollectCurrent()
        {
            foreach (var d in window.GetVisualDescendants())
            {
                switch (d)
                {
                    case TextBlock tb when !string.IsNullOrEmpty(tb.Text): texts.Add(tb.Text!); break;
                    case CheckBox cb when cb.Content is string s: texts.Add(s); break;
                    case Button b when b.Content is string s2: texts.Add(s2); break;
                    case TabItem ti when ti.Header is string s4: texts.Add(s4); break;
                    case global::Avalonia.Controls.Primitives.HeaderedContentControl h when h.Header is string s3: texts.Add(s3); break;
                }
            }
        }

        var tabs = window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault();
        if (tabs == null)
        {
            CollectCurrent();
        }
        else
        {
            var count = tabs.Items.OfType<TabItem>().Count();
            for (var i = 0; i < count; i++)
            {
                tabs.SelectedIndex = i;
                Settle(window);
                CollectCurrent();
            }
        }
        return texts;
    }

    // ==== Section 3: Canvas mouse ===========================================================

    /// <summary>Smoke step 16: click one room; it is selected.</summary>
    [AvaloniaTest]
    public void S3_16_ClickRoom_SelectsRoom()
    {
        var (window, canvas, service) = CreateCanvas();
        try
        {
            var room = AddRoom(0, 0);
            Click(window, canvas.CanvasToClient(new Vector(48, 32)));

            service.SelectedElements.Count.ShouldBe(1);
            service.SelectedElements.ShouldContain(room);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 17: Ctrl-click a second room, Shift-click a third; each is added to the selection.</summary>
    [AvaloniaTest]
    public void S3_17_CtrlThenShiftClick_AddsRoomsToSelection()
    {
        var (window, canvas, service) = CreateCanvas();
        try
        {
            var r1 = AddRoom(0, 0);
            var r2 = AddRoom(200, 0);
            var r3 = AddRoom(400, 0);

            Click(window, canvas.CanvasToClient(new Vector(48, 32)));
            Click(window, canvas.CanvasToClient(new Vector(248, 32)), RawInputModifiers.Control);
            Click(window, canvas.CanvasToClient(new Vector(448, 32)), RawInputModifiers.Shift);

            service.SelectedElements.Count.ShouldBe(3);
            service.SelectedElements.ShouldContain(r1);
            service.SelectedElements.ShouldContain(r2);
            service.SelectedElements.ShouldContain(r3);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 18 (known gap G005): reference Ctrl-marquee only adds; it must not deselect an already selected room.</summary>
    [AvaloniaTest]
    [Ignore("known gap: G005")]
    public void S3_18_CtrlMarquee_OverSelectedRoom_KeepsItSelected()
    {
        var (window, canvas, service) = CreateCanvas();
        try
        {
            var r1 = AddRoom(-100, -100);
            service.Select(r1);

            var start = canvas.CanvasToClient(new Vector(-150, -150));
            var end = canvas.CanvasToClient(new Vector(0, 0));
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left, RawInputModifiers.Control);
            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Left, RawInputModifiers.Control);

            service.SelectedElements.ShouldContain(r1);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 20: drag from a room port onto empty canvas; a connection docked at the port with a free end is created. (Toolbar style/flow/label defaults, G008, are manual.)</summary>
    [AvaloniaTest]
    public void S3_20_DragFromPortToEmptyCanvas_CreatesConnectionWithFreeEnd()
    {
        var (window, canvas, service) = CreateCanvas();
        try
        {
            var room = AddRoom(-150, 0);
            var port = room.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
            var start = canvas.CanvasToClient(port.Position);
            var emptyWorld = new Vector(200, 300);
            var end = canvas.CanvasToClient(emptyWorld);

            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(canvas.CanvasToClient(new Vector(0, 150)));
            window.MouseMove(end);
            window.MouseUp(end, MouseButton.Left);

            var conns = Project.Current.Elements.OfType<Connection>().ToList();
            conns.Count.ShouldBe(1);
            conns[0].VertexList[0].Port.ShouldBe(port);
            conns[0].VertexList[1].Port.ShouldBeNull();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 22 (known gap G011): a single click (no drag) on a curve ghost handle adds no waypoint in the reference.</summary>
    [AvaloniaTest]
    [Ignore("known gap: G011")]
    public void S3_22_SingleClickOnGhostHandle_DoesNotAddWaypoint()
    {
        var (window, canvas, service) = CreateCanvas();
        try
        {
            var conn = new Connection(Project.Current, new Vertex(new Vector(-100, 0)), new Vertex(new Vector(100, 0)));
            Project.Current.Elements.Add(conn);
            service.Select(conn);

            var handle = canvas.CanvasToClient(conn.GetCurveWaypointHandlePosition(CurveWaypoint.Middle));
            window.MouseMove(handle);
            window.MouseDown(handle, MouseButton.Left);
            window.MouseUp(handle, MouseButton.Left);

            conn.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBeNull();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 25: right-click empty canvas; menu has Add Room, Map Settings, App Settings and no Properties.</summary>
    [AvaloniaTest]
    public void S3_25_RightClickEmptyCanvas_MenuHasExpectedItemsAndNoProperties()
    {
        EnsureTheme();
        var (window, canvas, _) = CreateCanvas();
        try
        {
            var p = new Point(200, 200);
            window.MouseDown(p, MouseButton.Right);
            window.MouseUp(p, MouseButton.Right);

            canvas.LastContextMenu.ShouldNotBeNull();
            var headers = canvas.LastContextMenu!.Items.OfType<MenuItem>()
                .Select(m => (m.Header?.ToString() ?? string.Empty).Replace("_", ""))
                .ToList();

            headers.ShouldContain("Add Room");
            headers.ShouldContain("Map Settings...");
            headers.ShouldContain("Application Settings...");
            headers.ShouldNotContain("Properties...");
            canvas.LastContextMenu.Close();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 27: mouse wheel over the canvas zooms (up = in, down = out).</summary>
    [AvaloniaTest]
    public void S3_27_MouseWheel_ZoomsCanvas()
    {
        var (window, canvas, service) = CreateCanvas();
        try
        {
            var p = new Point(300, 300);
            window.MouseMove(p);
            var before = service.ZoomFactor;

            window.MouseWheel(p, new global::Avalonia.Vector(0, 1));
            var zoomedIn = service.ZoomFactor;
            zoomedIn.ShouldBeGreaterThan(before);

            window.MouseWheel(p, new global::Avalonia.Vector(0, -1));
            service.ZoomFactor.ShouldBeLessThan(zoomedIn);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 27 (known gap G017 / I-006): reference honours Invert Mouse Wheel, so wheel-up zooms out when it is on.</summary>
    [AvaloniaTest]
    [Ignore("known gap: G017 / I-006")]
    public void S3_27_MouseWheel_HonoursInvertMouseWheelSetting()
    {
        var (window, canvas, service) = CreateCanvas();
        try
        {
            ApplicationSettingsController.AppSettings.InvertMouseWheel = true;
            var p = new Point(300, 300);
            window.MouseMove(p);
            var before = service.ZoomFactor;

            window.MouseWheel(p, new global::Avalonia.Vector(0, 1));

            service.ZoomFactor.ShouldBeLessThan(before);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 29 (known gap A020 / I-002): status-bar X/Y readout follows the pointer.</summary>
    [AvaloniaTest]
    [Ignore("known gap: A020 / I-002")]
    public void S3_29_PointerMove_UpdatesStatusBarReadout()
    {
        EnsureTheme();
        var vm = App.ConfigureServices().GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = vm };
        try
        {
            window.Show();
            Settle(window);
            window.MouseMove(new Point(400, 300));
            window.MouseMove(new Point(450, 320));
            Settle(window);

            vm.CursorPositionText.ShouldNotBe("X: 0, Y: 0");
        }
        finally
        {
            window.Close();
        }
    }

    // ==== Section 5: Dialogs ================================================================

    /// <summary>Smoke step 40 (known gap D001): Room Properties has Position, Down and Right fields.</summary>
    [AvaloniaTest]
    [Ignore("known gap: D001")]
    public void S5_40_RoomProperties_HasPositionDownRightFields()
    {
        EnsureTheme();
        var room = AddRoom(0, 0, "Hall");
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        var window = ShowDialog(new RoomPropertiesDialog { DataContext = vm });
        try
        {
            var texts = CollectTexts(window).Select(t => t.TrimEnd(':').Trim()).ToList();
            texts.ShouldContain("Down");
            texts.ShouldContain("Right");
            // "Position:" already exists for the objects position, so require a second distinct label for the room position.
            texts.Count(t => t == "Position").ShouldBeGreaterThan(1);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 41 (known gap D002): Connection Properties Description is multiline.</summary>
    [AvaloniaTest]
    public void S5_41_ConnectionProperties_DescriptionIsMultiline()
    {
        EnsureTheme();
        var a = AddRoom(-150, 0);
        var b = AddRoom(150, 0);
        var pa = a.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.East);
        var pb = b.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == CompassPoint.West);
        var conn = new Connection(Project.Current, new Vertex(pa), new Vertex(pb));
        Project.Current.Elements.Add(conn);

        var vm = new ConnectionPropertiesViewModel();
        vm.Initialize(conn);
        var window = ShowDialog(new ConnectionPropertiesDialog { DataContext = vm });
        try
        {
            var box = window.GetVisualDescendants().OfType<TextBox>().First(t => t.Name == "DescriptionBox");
            box.AcceptsReturn.ShouldBeTrue();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 42 (known gap D003): Label Properties enables Shape with Background alone (no Outline).</summary>
    [AvaloniaTest]
    [Ignore("known gap: D003")]
    public void S5_42_LabelProperties_ShapeEnabledWithBackgroundOnly()
    {
        EnsureTheme();
        var label = new MapLabel(Project.Current) { Text = "Sign" };
        Project.Current.Elements.Add(label);
        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.HasOutline = false;
        vm.HasBackground = true;
        var window = ShowDialog(new MapLabelPropertiesDialog { DataContext = vm });
        try
        {
            var shapeLabel = window.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Shape:");
            var grid = shapeLabel.Parent.ShouldBeOfType<Grid>();
            var combo = grid.Children.OfType<ComboBox>().First(c => Grid.GetRow(c) == Grid.GetRow(shapeLabel));
            combo.IsEffectivelyEnabled.ShouldBeTrue();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 43 (known gap D004): Map Settings exposes handle size, snap-to-element distance and document margins.</summary>
    [AvaloniaTest]
    [Ignore("known gap: D004")]
    public void S5_43_MapSettings_ExposesHandleSnapDistanceAndMargins()
    {
        EnsureTheme();
        var vm = new MapSettingsViewModel();
        vm.Initialize();
        var window = ShowDialog(new MapSettingsDialog { DataContext = vm });
        try
        {
            var all = string.Join("\n", CollectTexts(window));
            all.ShouldContain("Handle", Case.Insensitive);
            all.ShouldContain("Snap", Case.Insensitive); // "Snap Elements to Grid" exists; the distance label must too
            all.ShouldContain("Distance", Case.Insensitive);
            all.ShouldContain("Margin", Case.Insensitive);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 44 (known gap I-007): renaming a region in Map Settings moves rooms that use it to the new name.</summary>
    [AvaloniaTest]
    [Ignore("known gap: I-007")]
    public void S5_44_MapSettings_RenameRegion_MovesRoomsToNewName()
    {
        Settings.Regions = new List<Region>
        {
            new() { RegionName = Region.DefaultRegion, RColor = System.Drawing.Color.White, TextColor = System.Drawing.Color.MidnightBlue },
            new() { RegionName = "Old", RColor = System.Drawing.Color.LightBlue, TextColor = System.Drawing.Color.MidnightBlue }
        };
        var room = AddRoom(0, 0);
        room.Region = "Old";

        var vm = new MapSettingsViewModel();
        vm.Initialize();
        vm.Regions.Single(r => r.Name == "Old").Name = "New";
        vm.ApplyToSettings();

        room.Region.ShouldBe("New");
    }

    /// <summary>Smoke step 45 (known gap I-005): unticking Show Canvas Tooltips and pressing OK persists to application settings.</summary>
    [AvaloniaTest]
    public void S5_45_AppSettings_UntickShowTooltips_IsApplied()
    {
        var vm = new AppSettingsViewModel();
        vm.Initialize();
        vm.ShowTooltips = false;
        vm.ApplyToSettings();

        ApplicationSettingsController.AppSettings.ShowTooltips.ShouldBeFalse();
        // ApplyToSettings persists to disk; write the restored values back.
        ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips = _prevDescTips;
        ApplicationSettingsController.AppSettings.ShowObjectsInTooltips = _prevObjTips;
        ApplicationSettingsController.AppSettings.InvertMouseWheel = _prevInvert;
        ApplicationSettingsController.AppSettings.ShowFullPathInTitleBar = _prevShowFullPath;
        ApplicationSettingsController.SaveSettings();
    }

    /// <summary>Smoke step 45: with tooltips off, hovering a room shows no tooltip.</summary>
    [AvaloniaTest]
    public void S5_45_TooltipsOff_HoverRoom_ShowsNoTooltip()
    {
        var (window, canvas, _) = CreateCanvas();
        try
        {
            var room = AddRoom(0, 0, "Grand Hall");
            room.Descriptions.Add("Hall description");
            var app = ApplicationSettingsController.AppSettings;
            app.ShowDescriptionsInTooltips = false;
            app.ShowObjectsInTooltips = false;

            window.MouseMove(canvas.CanvasToClient(new Vector(48, 32)));

            canvas.HoverElement.ShouldBe(room);
            ToolTip.GetTip(canvas).ShouldBeNull();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Smoke step 46 (I-006 / A004, ticket 114): Show Full Path in Title Bar puts the full path in the window title.</summary>
    [AvaloniaTest]
    public void S5_46_ShowFullPathInTitleBar_TitleContainsFullPath()
    {
        var vm = App.ConfigureServices().GetRequiredService<MainWindowViewModel>();
        Project.Current.FileName = Path.Combine(Path.GetTempPath(), "smoke-maps", "region-accent-one.trizbort");
        ApplicationSettingsController.AppSettings.ShowFullPathInTitleBar = true;

        vm.UpdateState();

        vm.WindowTitle.ShouldContain(Project.Current.FileName);
    }

    /// <summary>Smoke step 47 (known gap D015): About shows version, credits, licence and repo links.</summary>
    [AvaloniaTest]
    [Ignore("known gap: D015")]
    public void S5_47_About_ShowsVersionLicenceAndRepoLinks()
    {
        EnsureTheme();
        var window = ShowDialog(new AboutDialog());
        try
        {
            var all = string.Join("\n", CollectTexts(window));
            all.ShouldContain("Version", Case.Insensitive);
            all.ShouldContain("Licen", Case.Insensitive);
            all.ShouldContain("github.com", Case.Insensitive);
        }
        finally
        {
            window.Close();
        }
    }
}
