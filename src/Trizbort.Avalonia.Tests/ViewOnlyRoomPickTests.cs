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
using Trizbort.Domain.Elements;
using Trizbort.Setup;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 19 Nov 2026: with "which room does it mean?" open, clicking a room on the map didn't pick it in the list. The
///   map is only for looking at then (every button drags it), so a click that doesn't drag now picks the room under it.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ViewOnlyRoomPickTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        Settings.SnapToGrid = false;
    }

    [TearDown]
    public void TearDown()
    {
        MapCanvasControl.ViewOnly = false;
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    [AvaloniaTest]
    public void WhileOnlyLooking_AClickPicksTheRoom_AndADragDoesnt()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Content = canvas };
        window.Show();
        window.UpdateLayout();
        service.SetView(Vector.Zero, 1.0f);
        var room = new Room(Project.Current) { Name = "Maze", Position = new Vector(0, 0), Size = new Vector(96, 64) };
        Project.Current.Elements.Add(room);
        var picked = new List<Room>();
        void Picked(Room r) => picked.Add(r);
        MapCanvasControl.RoomPicked += Picked;
        MapCanvasControl.ViewOnly = true;
        try
        {
            var inside = canvas.CanvasToClient(new Vector(48, 32));
            var origin = service.Origin;
            window.MouseMove(inside);
            window.MouseDown(inside, MouseButton.Left);
            window.MouseUp(inside, MouseButton.Left);

            picked.ShouldBe(new[] { room });
            service.Session.Selection.Count.ShouldBe(0);
            service.Origin.ShouldBe(origin);

            // a drag looks around, and picks nothing
            window.MouseDown(inside, MouseButton.Left);
            var away = inside + new Point(80, 0);
            window.MouseMove(away);
            window.MouseUp(away, MouseButton.Left);
            picked.Count.ShouldBe(1);
            service.Origin.ShouldNotBe(origin);

            // nothing there: nothing picked
            var empty = canvas.CanvasToClient(new Vector(300, 300));
            window.MouseDown(empty, MouseButton.Left);
            window.MouseUp(empty, MouseButton.Left);
            picked.Count.ShouldBe(1);
        }
        finally
        {
            MapCanvasControl.RoomPicked -= Picked;
            window.Close();
        }
    }
}
