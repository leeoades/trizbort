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
    [AvaloniaTest]
    [Category("Benchmark")]
    public void Benchmark_Render_400Elements_MeasuresPerformanceAndAllocations()
    {
        var project = new Project();
        Project.Current = project;

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
        var canvas = new MapCanvasControl { CommandService = svc };
        var window = new global::Avalonia.Controls.Window { Width = 800, Height = 600, Content = canvas };
        window.Show();

        try
        {
            svc.FitToMap();

            using var bmp = new RenderTargetBitmap(new PixelSize(800, 600));

            // 100 warmup frames with viewport variations
            for (var i = 0; i < 100; i++)
            {
                var panX = (i % 10) * 15.0f;
                var panY = (i / 10) * 10.0f;
                var zoom = 0.8f + (i % 5) * 0.1f;
                svc.SetView(new Vector(panX, panY), zoom);
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
                var panX = (i % 10) * 15.0f;
                var panY = (i / 10) * 10.0f;
                var zoom = 0.8f + (i % 5) * 0.1f;
                svc.SetView(new Vector(panX, panY), zoom);
                bmp.Render(canvas);
            }

            sw.Stop();
            var endAlloc = GC.GetTotalAllocatedBytes(true);

            var avgTimeMs = sw.Elapsed.TotalMilliseconds / iterations;
            var avgAllocKb = (endAlloc - startAlloc) / (double)iterations / 1024.0;

            TestContext.Out.WriteLine($"=== BENCHMARK RESULTS (400 elements @ 800x600, varied pan/zoom) ===");
            TestContext.Out.WriteLine($"Total measured frames: {iterations}");
            TestContext.Out.WriteLine($"Average render time: {avgTimeMs:F2} ms");
            TestContext.Out.WriteLine($"Average allocations: {avgAllocKb:F1} KB / frame");
            TestContext.Out.WriteLine($"Frame rate potential: {1000.0 / avgTimeMs:F1} FPS");

            Assert.That(avgTimeMs, Is.LessThan(50)); // Render smoothly within 50ms
            Assert.That(avgAllocKb, Is.LessThan(5000)); // Under 5MB per frame during active pan/zoom
        }
        finally
        {
            window.Close();
        }
    }
}
