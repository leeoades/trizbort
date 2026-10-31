using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Vector = Trizbort.Domain.Misc.Vector;
using CanvasDragMode = Trizbort.Editor.CanvasDragMode;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 133: I-027 (cancel a drag), I-028 (highlight keeps the dash style), I-031 (context menu cursor).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ChecklistCanvasFixesTests
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

    private static (Window Window, MapCanvasControl Canvas, CommandService Service) CreateHarness()
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

    [Test]
    public void HighlightPen_ForDashedConnection_KeepsADashPattern()
    {
        var pen = ConnectionDrawingAdapter.HighlightPen(Colors.SelectedLine, 1.5, ConnectionStyle.Dashed);

        pen.DashStyle.ShouldNotBeNull();
        pen.DashStyle!.Dashes.ShouldNotBeNull();
        pen.DashStyle.Dashes!.Count.ShouldBeGreaterThan(0);
        pen.Thickness.ShouldBe(Settings.LineWidth + 1.5, 0.01);
    }

    [Test]
    public void HighlightPen_ForDashedConnection_HasTheSameAbsoluteDashLengthAsTheLine()
    {
        var line = ConnectionStyle.Dashed.ToDashStyle()!;
        var pen = ConnectionDrawingAdapter.HighlightPen(Colors.HoverLine, 1.0, ConnectionStyle.Dashed);

        var expected = line.Dashes![0] * Settings.LineWidth;
        (pen.DashStyle!.Dashes![0] * pen.Thickness).ShouldBe(expected, 0.01);
    }

    [Test]
    public void HighlightPen_ForSolidConnection_IsSolid()
    {
        var pen = ConnectionDrawingAdapter.HighlightPen(Colors.SelectedLine, 1.5, ConnectionStyle.Solid);

        pen.DashStyle.ShouldBeNull();
    }

    [AvaloniaTest]
    public void TryCancelGesture_DuringRoomDrag_RestoresTheRoomOnTheFirstCall()
    {
        var (window, canvas, service) = CreateHarness();
        try
        {
            var room = new Room(Project.Current) { Position = new Vector(0, 0), Size = new Vector(96, 64) };
            Project.Current.Elements.Add(room);
            var roomId = room.Id;

            canvas.TryCancelGesture().ShouldBeFalse();

            var start = canvas.CanvasToClient(new Vector(48, 32));
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(canvas.CanvasToClient(new Vector(248, 32)));
            canvas.DragMode.ShouldBe(CanvasDragMode.MoveElement);

            canvas.TryCancelGesture().ShouldBeTrue();

            canvas.DragMode.ShouldBe(CanvasDragMode.None);
            var restored = Project.Current.Elements.OfType<Room>().Single(r => r.Id == roomId);
            restored.Position.X.ShouldBe(0f, 1.0f);
            service.Session.Undo.CanUndo.ShouldBeFalse();
            canvas.TryCancelGesture().ShouldBeFalse();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ContextMenu_OpenedFromTheCanvas_DoesNotUseTheCanvasCursor()
    {
        Application.Current!.Styles.Add(new global::Avalonia.Themes.Fluent.FluentTheme());
        var (window, canvas, _) = CreateHarness();
        try
        {
            canvas.Cursor = new Cursor(StandardCursorType.SizeAll);

            var p = new Point(200, 200);
            window.MouseDown(p, MouseButton.Right);
            window.MouseUp(p, MouseButton.Right);

            var menu = canvas.LastContextMenu;
            menu.ShouldNotBeNull();
            menu!.Cursor.ShouldNotBeNull();
            ReferenceEquals(menu.Cursor, canvas.Cursor).ShouldBeFalse();
            menu.Close();
        }
        finally
        {
            window.Close();
        }
    }
}
