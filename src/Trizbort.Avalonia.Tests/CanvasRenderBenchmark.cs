using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[Category("Benchmark")]
public class CanvasRenderBenchmark
{
    // Headless software-Skia baseline at the fitted full-map view:
    // ~21 ms/frame and ~4 MB/frame for 390 elements on 800x600.
    // Thresholds are ~2x that baseline for cross-machine robustness.
    // These numbers are CPU rasterization into RenderTargetBitmap; they do not
    // prove (or disprove) 60 FPS on a GPU/compositor path.
    private const double MaxAverageFrameMs = 45.0;
    private const double MaxAverageAllocKb = 8192.0;

    [AvaloniaTest]
    [Category("Benchmark")]
    public void Benchmark_Render_400Elements_MeasuresPerformanceAndAllocations()
    {
        var previousProject = Project.Current;
        var project = new Project();
        Project.Current = project;
        global::Avalonia.Controls.Window? window = null;

        try
        {
            var rooms = new Room[200];
            for (var i = 0; i < 200; i++)
            {
                var r = new Room(project)
                {
                    Name = $"Room {i}",
                    SubTitle = i % 2 == 0 ? $"Subtitle {i}" : "",
                    Objects = i % 3 == 0 ? $"lamp [s]\nsword\nkey {i} [cs]" : "",
                    Shape = (RoomShape)(i % 4),
                    IsDark = (i % 5 == 0),
                    SecondFillLocation = (i % 4 == 0) ? "BottomRight" : "Bottom",
                    Position = new Vector((i % 20) * 120, (i / 20) * 100),
                    Size = new Vector(80, 50)
                };
                if (i % 4 == 0) r.SecondFillColor = System.Drawing.Color.LightCyan;
                project.Elements.Add(r);
                rooms[i] = r;
            }

            for (var i = 0; i < 190; i++)
            {
                var conn = new Connection(project, new Vertex(rooms[i].PortList[0]), new Vertex(rooms[i + 1].PortList[2]));
                if (i % 2 == 0) conn.Flow = ConnectionFlow.OneWay;
                if (i % 3 == 0) conn.Style = ConnectionStyle.Dashed;
                if (i % 4 == 0) { conn.StartText = "in"; conn.EndText = "out"; }
                project.Elements.Add(conn);
            }

            var svc = new CommandService();
            svc.ShowGrid = true;
            svc.SetViewportSize(800, 600);
            var canvas = new MapCanvasControl { CommandService = svc };
            window = new global::Avalonia.Controls.Window { Width = 800, Height = 600, Content = canvas };
            window.Show();

            svc.FitToMap();
            var fitOrigin = svc.Origin;
            var fitZoom = svc.ZoomFactor;

            Assert.That(project.Elements.Count, Is.EqualTo(390));
            Assert.That(fitZoom, Is.LessThan(1.0f),
                "FitToMap must zoom out so the full ~2400x1000 map is in view; a 1.0 zoom crops to the top-left corner.");

            using var bmp = new RenderTargetBitmap(new PixelSize(800, 600));

            // Warmup around the fitted view so all 390 elements stay on screen.
            for (var i = 0; i < 100; i++)
            {
                ApplyFittedView(svc, fitOrigin, fitZoom, i);
                bmp.Render(canvas);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var startAlloc = GC.GetTotalAllocatedBytes(true);
            var sw = Stopwatch.StartNew();

            const int iterations = 50;
            for (var i = 0; i < iterations; i++)
            {
                ApplyFittedView(svc, fitOrigin, fitZoom, i);
                bmp.Render(canvas);
            }

            sw.Stop();
            var endAlloc = GC.GetTotalAllocatedBytes(true);

            var avgTimeMs = sw.Elapsed.TotalMilliseconds / iterations;
            var avgAllocKb = (endAlloc - startAlloc) / (double)iterations / 1024.0;

            TestContext.Out.WriteLine("=== BENCHMARK RESULTS (390 elements @ 800x600, fitted view + small pan/zoom) ===");
            TestContext.Out.WriteLine($"FitToMap origin={fitOrigin} zoom={fitZoom:F3}");
            TestContext.Out.WriteLine($"Total measured frames: {iterations}");
            TestContext.Out.WriteLine($"Average render time: {avgTimeMs:F2} ms (headless CPU/Skia RenderTargetBitmap)");
            TestContext.Out.WriteLine($"Average allocations: {avgAllocKb:F1} KB / frame");
            TestContext.Out.WriteLine($"Headless equivalent: {1000.0 / avgTimeMs:F1} FPS — 60 FPS on GPU/compositor is unverified by this test");

            Assert.That(avgTimeMs, Is.LessThan(MaxAverageFrameMs),
                $"Headless CPU render averaged {avgTimeMs:F2} ms/frame; threshold is ~2x the ~21 ms fitted-view baseline.");
            Assert.That(avgAllocKb, Is.LessThan(MaxAverageAllocKb),
                $"Headless CPU render averaged {avgAllocKb:F0} KB/frame; threshold is ~2x the ~4 MB fitted-view baseline.");
        }
        finally
        {
            window?.Close();
            Project.Current = previousProject;
            project.Dispose();
        }
    }

    /// <summary>
    /// Pans a few tens of world units and zooms ±10% around the FitToMap view so the full map stays in view.
    /// </summary>
    private static void ApplyFittedView(CommandService svc, Vector fitOrigin, float fitZoom, int i)
    {
        var panX = (i % 10 - 4.5f) * 6f;
        var panY = ((i / 10) % 10 - 4.5f) * 4f;
        var zoom = fitZoom * (0.9f + (i % 5) * 0.05f);
        svc.SetView(new Vector(fitOrigin.X + panX, fitOrigin.Y + panY), zoom);
    }
}
