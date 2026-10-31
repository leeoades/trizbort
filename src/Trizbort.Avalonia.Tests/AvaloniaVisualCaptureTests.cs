using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Capture")]
public class AvaloniaVisualCaptureTests
{
    private const int CaptureWidth = 1200;
    private const int CaptureHeight = 900;
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        if (!ReferenceEquals(created, _previousProject))
        {
            created.Dispose();
        }
    }

    [AvaloniaTest]
    public void CaptureAvaloniaManualFixtureScreenshots()
    {
        var root = Environment.GetEnvironmentVariable("TRIZBORT_CAPTURE_OUTPUT");
        if (string.IsNullOrWhiteSpace(root))
        {
            Assert.Ignore("Set TRIZBORT_CAPTURE_OUTPUT to generate Avalonia visual captures.");
        }

        var output = Path.Combine(root, "avalonia");
        Directory.CreateDirectory(output);
        var manifest = new List<string>
        {
            "frontend,state,fixture,width,height,bytes,sha256,path"
        };

        foreach (var fixturePath in ManualMaps())
        {
            using var project = new Project();
            Project.Current = project;
            Settings.Reset();
            Load(Project.Current, fixturePath);
            var fixture = Path.GetFileName(fixturePath);

            var service = new CommandService();
            service.SetViewportSize(CaptureWidth, CaptureHeight);
            service.FitToMap();
            var canvas = new MapCanvasControl
            {
                CommandService = service,
                Width = CaptureWidth,
                Height = CaptureHeight
            };
            var window = new Window
            {
                Width = CaptureWidth,
                Height = CaptureHeight,
                Content = canvas,
                ShowInTaskbar = false
            };

            window.Show();
            try
            {
                Layout(canvas);
                using (var export = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0))
                {
                    if (export != null)
                    {
                        CaptureBitmap(output, manifest, fixture, "export", export);
                    }
                    else
                    {
                        CaptureControl(output, manifest, fixture, "export", canvas);
                    }
                }

                CaptureControl(output, manifest, fixture, "edit-none", canvas);

                var selected = RepresentativeElement();
                if (selected != null)
                {
                    service.Select(selected);
                }
                Layout(canvas);
                CaptureControl(output, manifest, fixture, "edit-selected", canvas);
            }
            finally
            {
                window.Close();
            }
        }

        File.WriteAllLines(Path.Combine(output, "manifest.csv"), manifest, Encoding.UTF8);
    }

    private static IEnumerable<string> ManualMaps()
    {
        var directory = FindManualFixtureDirectory();
        return Directory.GetFiles(directory, "*.trizbort").OrderBy(Path.GetFileName);
    }

    private static string FindManualFixtureDirectory()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "tests", "manual");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not find tests/manual from " + TestContext.CurrentContext.TestDirectory);
        return string.Empty;
    }

    private static void Load(Project project, string path)
    {
        Exception? failure = null;
        var loaded = new LegacyMapFileEngine(
            project,
            error => failure = error,
            _ => { },
            (_, _) => { }).Load(path);
        loaded.ShouldBeTrue(failure?.ToString());
    }

    private static Element? RepresentativeElement()
    {
        return Project.Current.Elements.OfType<Room>().OrderBy(room => room.Id).Cast<Element>().FirstOrDefault()
               ?? Project.Current.Elements.OrderBy(element => element.Id).FirstOrDefault();
    }

    private static void Layout(Control control)
    {
        control.Measure(new Size(CaptureWidth, CaptureHeight));
        control.Arrange(new Rect(0, 0, CaptureWidth, CaptureHeight));
        Dispatcher.UIThread.RunJobs();
    }

    private static void CaptureControl(
        string output,
        ICollection<string> manifest,
        string fixture,
        string state,
        Control control)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(CaptureWidth, CaptureHeight));
        bitmap.Render(control);
        CaptureBitmap(output, manifest, fixture, state, bitmap);
    }

    private static void CaptureBitmap(
        string output,
        ICollection<string> manifest,
        string fixture,
        string state,
        Bitmap bitmap)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(fixture)}--{state}.png";
        var path = Path.Combine(output, fileName);
        bitmap.Save(path);
        var info = new FileInfo(path);
        manifest.Add(
            $"avalonia,{state},{Escape(fixture)},{bitmap.PixelSize.Width},{bitmap.PixelSize.Height},{info.Length},{Sha256(path)},{Escape(path)}");
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Escape(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
