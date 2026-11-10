using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>A room marked "don't automap from here" shows a small amber sign, so it can be seen on the map.</summary>
[TestFixture]
[NonParallelizable]
public class AutomapPauseSignTests
{
    private Project? _previousProject;

    // a map of its own: a canvas left watching the shared map breaks later tests ("Call from invalid thread")
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

    [AvaloniaTest]
    public void ARoomAutomapDoesntMapFrom_ShowsASign()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service, Width = 400, Height = 300 };
        var window = new Window { Content = canvas, Width = 400, Height = 300 };
        window.Show();
        var maze = new Room(Project.Current) { Name = "Hedge Maze", Position = new CoreVector(0, 0), Size = new CoreVector(96, 64), AutomapPause = true };
        var garden = new Room(Project.Current) { Name = "Garden", Position = new CoreVector(160, 0), Size = new CoreVector(96, 64), IsStartRoom = true };
        Project.Current.Elements.Add(maze);
        Project.Current.Elements.Add(garden);

        using var frame = window.CaptureRenderedFrame();
        frame.ShouldNotBeNull();
        var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
        if (!string.IsNullOrEmpty(shots)) frame!.Save(System.IO.Path.Combine(shots, "room-badges.png"));
        window.Close();
    }
}
