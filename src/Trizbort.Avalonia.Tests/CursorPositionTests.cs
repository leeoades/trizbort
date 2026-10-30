using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class CursorPositionTests
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

    private static MainWindowViewModel CreateViewModel(CommandService service)
    {
        // The storage and dialog services are not used by the cursor readout.
        return new MainWindowViewModel(service, null!, null!, new AvaloniaAutomapCanvas());
    }

    [AvaloniaTest]
    public void PointerMove_OverCanvas_ShowsMapCoordinates()
    {
        var service = new CommandService();
        var vm = CreateViewModel(service);
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Width = 800, Height = 600, Content = canvas };
        try
        {
            window.Show();
            window.UpdateLayout();
            service.SetView(new Vector(100, 50), 1.0f);
            var centre = new Point(canvas.Bounds.Width / 2, canvas.Bounds.Height / 2);

            window.MouseMove(new Point(centre.X + 10, centre.Y + 20));

            vm.CursorPositionText.ShouldBe("X: 110, Y: 70");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void PointerMove_WhenZoomed_ShowsZoomAdjustedMapCoordinates()
    {
        var service = new CommandService();
        var vm = CreateViewModel(service);
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Width = 800, Height = 600, Content = canvas };
        try
        {
            window.Show();
            window.UpdateLayout();
            service.SetView(new Vector(0, 0), 2.0f);
            var centre = new Point(canvas.Bounds.Width / 2, canvas.Bounds.Height / 2);

            window.MouseMove(new Point(centre.X + 20, centre.Y + 10));

            vm.CursorPositionText.ShouldBe("X: 10, Y: 5");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void PointerLeavingCanvas_ClearsMapCoordinates()
    {
        var service = new CommandService();
        var vm = CreateViewModel(service);
        var canvas = new MapCanvasControl { CommandService = service };
        var statusBar = new Border { Height = 30, Background = Brushes.Gray };
        var layout = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(statusBar, Dock.Bottom);
        layout.Children.Add(statusBar);
        layout.Children.Add(canvas);
        var window = new Window { Width = 800, Height = 600, Content = layout };
        try
        {
            window.Show();
            window.UpdateLayout();

            window.MouseMove(new Point(100, 100));
            vm.CursorPositionText.ShouldStartWith("X: ");

            // The status bar sits below the canvas, so this move exits the canvas.
            window.MouseMove(new Point(100, 590));
            vm.CursorPositionText.ShouldBe(string.Empty);
        }
        finally
        {
            window.Close();
        }
    }
}
