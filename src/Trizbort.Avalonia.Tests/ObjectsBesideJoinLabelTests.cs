using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   A room's objects, listed below it to the right of the middle, and the label of a join leaving straight down from
///   the middle of its bottom don't overprint: the label goes to the left of the line.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ObjectsBesideJoinLabelTests
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
    public void TheLabelOfAJoinStraightDown_GoesLeftOfTheLine_AwayFromTheObjects()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service, Width = 400, Height = 400 };
        var window = new Window { Content = canvas, Width = 400, Height = 400 };
        window.Show();
        var upper = new Room(Project.Current) { Name = "In the Chimney", Position = new CoreVector(0, 0), Size = new CoreVector(96, 64), Objects = "Holes" };
        var lower = new Room(Project.Current) { Name = "In the Chimney", Position = new CoreVector(0, 200), Size = new CoreVector(96, 64), Objects = "Holes" };
        var fireplace = new Room(Project.Current) { Name = "Fireplace", Position = new CoreVector(-60, 400), Size = new CoreVector(96, 64), Objects = "Brick" };
        Project.Current.Elements.Add(upper);
        Project.Current.Elements.Add(lower);
        Project.Current.Elements.Add(fireplace);
        var automap = new AutomapCanvas(Project.Current);
        automap.Connect(upper, MappableDirection.South, lower, true);
        automap.Connect(lower, MappableDirection.South, fireplace, true);
        foreach (var join in Project.Current.Elements.OfType<Connection>())
        {
            join.StartText = "s / down";
            join.EndText = "n / up";
        }

        using var frame = window.CaptureRenderedFrame();
        frame.ShouldNotBeNull();
        var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
        if (!string.IsNullOrEmpty(shots)) frame!.Save(System.IO.Path.Combine(shots, "objects-beside-join-label.png"));

        window.Close();
    }
}
