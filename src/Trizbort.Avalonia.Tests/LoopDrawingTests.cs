using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Trizbort.Automap;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
/// <summary>A way out that leads straight back in is drawn as a small curl with one arrowhead.</summary>
public class LoopDrawingTests
{
    [AvaloniaTest]
    public void LoopsBackIntoTheirRoom_Render()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service, Width = 500, Height = 400 };
        var window = new Window { Content = canvas, Width = 500, Height = 400 };
        window.Show();
        var forest = new Room(Project.Current) { Name = "Forest", Position = new CoreVector(0, 0), Size = new CoreVector(96, 64) };
        Project.Current.Elements.Add(forest);
        var clearing = new Room(Project.Current) { Name = "Clearing", Position = new CoreVector(160, 0), Size = new CoreVector(96, 64) };
        Project.Current.Elements.Add(clearing);
        var automap = new AutomapCanvas(Project.Current) { AbbreviateDirections = true };
        automap.Connect(forest, MappableDirection.East, clearing, true);
        automap.ConnectLoop(forest, MappableDirection.North);
        automap.ConnectLoop(clearing, MappableDirection.Up);
        using var frame = window.CaptureRenderedFrame();
        Shouldly.ShouldBeNullExtensions.ShouldNotBeNull(frame);
        var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
        if (!string.IsNullOrEmpty(shots)) frame!.Save(System.IO.Path.Combine(shots, "loop.png"));
        window.Close();
    }
}
