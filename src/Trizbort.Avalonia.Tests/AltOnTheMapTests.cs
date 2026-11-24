using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Setup;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 24 Nov 2026: Alt+drag a background, then let go of Alt, and File on the menu bar was selected (Windows'
///   Alt-alone-goes-to-the-menu). Alt used on the map isn't for the menu.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AltOnTheMapTests
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

    private static (Window Window, Menu Menu, MapCanvasControl Canvas) Harness()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var menu = new Menu { Items = { new MenuItem { Header = "_File", Items = { new MenuItem { Header = "_New" } } } } };
        var dock = new DockPanel();
        DockPanel.SetDock(menu, Dock.Top);
        dock.Children.Add(menu);
        dock.Children.Add(canvas);
        var window = new Window { Width = 800, Height = 600, Content = dock };
        window.Show();
        window.UpdateLayout();
        canvas.Focus();
        service.SetView(Vector.Zero, 1.0f);
        var background = new Background(Project.Current) { Position = new Vector(-300, -200), Size = new Vector(600, 400) };
        Project.Current.Elements.Add(background);
        return (window, menu, canvas);
    }

    [AvaloniaTest]
    public void AltAlone_StillGoesToTheMenu()
    {
        var (window, menu, _) = Harness();
        try
        {
            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();

            menu.IsOpen.ShouldBeTrue();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void AltDraggingABackground_ThenLettingGoOfAlt_LeavesTheMenuAlone()
    {
        var (window, menu, canvas) = Harness();
        try
        {
            var start = canvas.CanvasToClient(new Vector(0, 0));
            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            window.MouseMove(start, RawInputModifiers.Alt);
            window.MouseDown(start, MouseButton.Left, RawInputModifiers.Alt);
            window.MouseMove(canvas.CanvasToClient(new Vector(64, 0)), RawInputModifiers.Alt);
            window.MouseUp(canvas.CanvasToClient(new Vector(64, 0)), MouseButton.Left, RawInputModifiers.Alt);
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();

            Project.Current.Elements.OfType<Background>().Single().Position.ShouldBe(new Vector(-236, -200));
            menu.IsOpen.ShouldBeFalse();
            canvas.IsFocused.ShouldBeTrue();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void AltHeldOverABackground_ShowsTheMoveCursor()
    {
        var (window, _, canvas) = Harness();
        try
        {
            var inside = canvas.CanvasToClient(new Vector(0, 0));
            window.MouseMove(inside);
            Dispatcher.UIThread.RunJobs();
            canvas.CursorKind.ShouldBe(Trizbort.Editor.CursorKind.Default);

            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();
            canvas.CursorKind.ShouldBe(Trizbort.Editor.CursorKind.SizeAll);

            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();
            canvas.CursorKind.ShouldBe(Trizbort.Editor.CursorKind.Default);
        }
        finally
        {
            window.Close();
        }
    }

    // Lee, 24 Nov 2026, on Windows: no move cursor, and File selected after all. The map may not have the keyboard, and
    // the menu's access keys take Alt first
    [AvaloniaTest]
    public void AltHeldOverABackground_ShowsTheMoveCursor_EvenWhenSomethingElseHasTheKeyboard()
    {
        var (window, menu, canvas) = Harness();
        try
        {
            var inside = canvas.CanvasToClient(new Vector(0, 0));
            window.MouseMove(inside);
            var box = new TextBox();
            ((DockPanel)window.Content!).Children.Insert(0, box);
            DockPanel.SetDock(box, Dock.Bottom);
            window.UpdateLayout();
            box.Focus();
            Dispatcher.UIThread.RunJobs();

            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();
            canvas.CursorKind.ShouldBe(Trizbort.Editor.CursorKind.SizeAll);

            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();
            canvas.CursorKind.ShouldBe(Trizbort.Editor.CursorKind.Default);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void AnAltClickOnTheMap_TellsTheMenuToIgnoreTheAltRelease()
    {
        var (window, menu, canvas) = Harness();
        var opened = 0;
        menu.Opened += (_, _) => opened++;
        try
        {
            var start = canvas.CanvasToClient(new Vector(0, 0));
            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            window.MouseMove(start, RawInputModifiers.Alt);
            window.MouseDown(start, MouseButton.Left, RawInputModifiers.Alt);
            window.MouseUp(start, MouseButton.Left, RawInputModifiers.Alt);
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();

            // never opened at all, not opened and closed again
            opened.ShouldBe(0);
            menu.IsOpen.ShouldBeFalse();
        }
        finally
        {
            window.Close();
        }
    }
}
