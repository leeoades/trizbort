using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Navigation;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>The directions' route glows blue on the map.</summary>
[TestFixture]
[NonParallelizable]
public class RouteGlowTests
{
    [AvaloniaTest]
    public void TheRoute_GlowsBlue_RoundItsRooms()
    {
        var previous = Project.Current;
        var project = new Project();
        Project.Current = project;
        global::Avalonia.Controls.Window? window = null;
        try
        {
            var hall = new Room(project) { Name = "Hall", Position = new Vector(0, 0), Size = new Vector(96, 64) };
            var kitchen = new Room(project) { Name = "Kitchen", Position = new Vector(200, 0), Size = new Vector(96, 64) };
            var attic = new Room(project) { Name = "Attic", Position = new Vector(200, 160), Size = new Vector(96, 64) };
            project.Elements.Add(hall);
            project.Elements.Add(kitchen);
            project.Elements.Add(attic);
            project.Elements.Add(new Connection(project, new Vertex(hall.PortAt(CompassPoint.East)),
                new Vertex(kitchen.PortAt(CompassPoint.West))));
            project.Elements.Add(new Connection(project, new Vertex(kitchen.PortAt(CompassPoint.South)),
                new Vertex(attic.PortAt(CompassPoint.North))));

            var service = new CommandService();
            service.SetViewportSize(600, 400);
            var canvas = new MapCanvasControl { CommandService = service };
            window = new global::Avalonia.Controls.Window { Width = 600, Height = 400, Content = canvas };
            window.Show();
            service.SetView(new Vector(148, 112), 1f);

            // just outside the attic's left edge: inside the halo, clear of the lines
            var probe = new PixelPoint(300 + (200 - 4 - 148), 200 + (192 - 112));
            var plain = Pixel(canvas, probe);

            RouteGlow.Route = RouteFinder.Find(project.Elements, hall, attic);
            RouteGlow.Route!.Count.ShouldBe(2);
            var glowing = Pixel(canvas, probe, "route-glow.png");

            glowing.ShouldNotBe(plain);
            // bluer than it was
            (glowing.B - glowing.R).ShouldBeGreaterThan(plain.B - plain.R);
        }
        finally
        {
            RouteGlow.Route = null;
            window?.Close();
            Project.Current = previous;
        }
    }

    private static global::Avalonia.Media.Color Pixel(MapCanvasControl canvas, PixelPoint at, string? shot = null)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(600, 400));
        bitmap.Render(canvas);
        var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
        if (shot != null && !string.IsNullOrEmpty(shots)) bitmap.Save(System.IO.Path.Combine(shots, shot));
        var pixels = new byte[4];
        var buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(4);
        try
        {
            bitmap.CopyPixels(new PixelRect(at.X, at.Y, 1, 1), buffer, 4, 4);
            System.Runtime.InteropServices.Marshal.Copy(buffer, pixels, 0, 4);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
        }

        // BGRA on Windows and Linux, RGBA on macOS
        var rgba = bitmap.Format?.Equals(global::Avalonia.Platform.PixelFormats.Rgba8888) == true;
        return rgba
            ? global::Avalonia.Media.Color.FromArgb(pixels[3], pixels[0], pixels[1], pixels[2])
            : global::Avalonia.Media.Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
    }
}
