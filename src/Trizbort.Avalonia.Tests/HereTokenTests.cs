using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Where you are on the map: a token on your room, like a playing piece on a board (HereToken). Lee, 22 Nov 2026.
/// </summary>
[TestFixture]
[NonParallelizable]
public class HereTokenTests
{
    private Project? _previousProject;
    private string _token = string.Empty;
    private string _file = string.Empty;
    private bool _shown = true;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        _token = ApplicationSettingsController.AppSettings.HereToken;
        _shown = ApplicationSettingsController.AppSettings.HereTokenShown;
        _file = ApplicationSettingsController.AppSettings.HereTokenFile;
    }

    [TearDown]
    public void TearDown()
    {
        MapCanvasControl.HereRoomOverride = null;
        ApplicationSettingsController.AppSettings.HereToken = _token;
        ApplicationSettingsController.AppSettings.HereTokenFile = _file;
        ApplicationSettingsController.AppSettings.HereTokenShown = _shown;
        var created = Project.Current;
        Project.Current = _previousProject!;
        if (!ReferenceEquals(created, _previousProject)) created.Dispose();
    }

    [AvaloniaTest]
    [Category("Unit")]
    public void EachTokenIsAPicture_DrawnFromItsSvg()
    {
        foreach (var (key, _) in HereToken.Choices.Where(choice => choice.Key != HereToken.Own))
        {
            var image = HereToken.Image(key, 128);
            image.ShouldNotBeNull(key);
            Math.Max(image!.PixelSize.Width, image.PixelSize.Height).ShouldBe(128, key);
        }
    }

    [AvaloniaTest]
    [Category("Unit")]
    public void YourOwnSvgIsAToken_AndAFileThatIsntAPictureIsNone()
    {
        var folder = Path.Combine(Path.GetTempPath(), "trizbort-token-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var svg = Path.Combine(folder, "mine.svg");
            File.WriteAllText(svg,
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 20 20\"><circle cx=\"10\" cy=\"10\" r=\"9\" fill=\"#0a0\"/></svg>");
            HereToken.Image(svg, 64).ShouldNotBeNull();

            var notAPicture = Path.Combine(folder, "notes.png");
            File.WriteAllText(notAPicture, "not a picture");
            HereToken.Image(notAPicture, 64).ShouldBeNull();

            ApplicationSettingsController.AppSettings.HereToken = HereToken.Own;
            ApplicationSettingsController.AppSettings.HereTokenFile = svg;
            HereToken.Chosen.ShouldBe(svg);
            // turned off, nothing is drawn, but your choice is kept for when it's turned on again
            ApplicationSettingsController.AppSettings.HereTokenShown = false;
            HereToken.Chosen.ShouldBeNull();
            ApplicationSettingsController.AppSettings.HereToken.ShouldBe(HereToken.Own);
            ApplicationSettingsController.AppSettings.HereTokenShown = true;
            HereToken.Chosen.ShouldBe(svg);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [AvaloniaTest]
    [Category("Unit")]
    public void AppSettingsShowEveryToken_AndLoadingYourOwnChoosesIt()
    {
        var folder = Path.Combine(Path.GetTempPath(), "trizbort-token-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var config = AppPaths.OverrideConfigFolder;
        AppPaths.OverrideConfigFolder = Path.Combine(folder, "config");
        Directory.CreateDirectory(AppPaths.OverrideConfigFolder);
        try
        {
            var vm = new Trizbort.Avalonia.ViewModels.AppSettingsViewModel();
            vm.Initialize();
            vm.HereTokenTiles.Select(tile => tile.Key).ShouldBe(HereToken.Choices.Select(choice => choice.Key));
            vm.HereTokenTiles.Where(tile => tile.Key != HereToken.Own)
                .ShouldAllBe(tile => tile.Image != null);
            var own = vm.HereTokenTiles.Single(tile => tile.Key == HereToken.Own);
            own.HasNoImage.ShouldBeTrue();

            var svg = Path.Combine(folder, "mine.svg");
            File.WriteAllText(svg,
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 20 20\"><circle cx=\"10\" cy=\"10\" r=\"9\" fill=\"#0a0\"/></svg>");
            vm.UseOwnToken(svg);

            vm.SelectedHereToken.ShouldBe(own);
            own.Image.ShouldNotBeNull();
            vm.HereTokenFileName.ShouldBe("mine.svg");
            vm.ApplyToSettings();
            ApplicationSettingsController.AppSettings.HereToken.ShouldBe(HereToken.Own);
            ApplicationSettingsController.AppSettings.HereTokenFile.ShouldBe(svg);

            // off, saved, opened again, on, saved: back as it was
            vm.HereTokenShown = false;
            vm.ApplyToSettings();
            var again = new Trizbort.Avalonia.ViewModels.AppSettingsViewModel();
            again.Initialize();
            again.HereTokenShown.ShouldBeFalse();
            again.SelectedHereToken!.Key.ShouldBe(HereToken.Own);
            again.HereTokenShown = true;
            again.ApplyToSettings();
            ApplicationSettingsController.AppSettings.HereTokenShown.ShouldBeTrue();
            ApplicationSettingsController.AppSettings.HereToken.ShouldBe(HereToken.Own);
            ApplicationSettingsController.AppSettings.HereTokenFile.ShouldBe(svg);
        }
        finally
        {
            AppPaths.OverrideConfigFolder = config;
            Directory.Delete(folder, true);
        }
    }

    [Test]
    [Category("Unit")]
    public void TheTokenStaysBigEnoughToFind_ZoomedOut()
    {
        HereToken.SizeAt(1).ShouldBe(HereToken.Size);
        // a tenth of the size: still 30 pixels across on screen
        (HereToken.SizeAt(0.1) * 0.1).ShouldBe(HereToken.SmallestOnScreen, 0.001);
    }

    /// <summary>
    ///   The map with the token on the room you're in, beside a selected room, for each token and zoomed out. Set
    ///   TRIZBORT_CAPTURE_OUTPUT to write them.
    /// </summary>
    [AvaloniaTest]
    [Category("Capture")]
    public void CaptureTheTokenOnTheMap()
    {
        var root = Environment.GetEnvironmentVariable("TRIZBORT_CAPTURE_OUTPUT");
        if (string.IsNullOrWhiteSpace(root)) Assert.Ignore("Set TRIZBORT_CAPTURE_OUTPUT to capture the token.");
        Directory.CreateDirectory(root);

        using var project = new Project();
        Project.Current = project;
        Settings.Reset();
        var hall = new Room(project) { Name = "Hall of Mists", Position = new CoreVector(0, 0) };
        var maze = new Room(project) { Name = "Maze", Position = new CoreVector(0, 200) };
        var rod = new Room(project) { Name = "Maze", Objects = "black rod", Position = new CoreVector(260, 200) };
        var pit = new Room(project) { Name = "Top of Small Pit", Position = new CoreVector(260, 0) };
        foreach (var room in new[] { hall, maze, rod, pit }) project.Elements.Add(room);
        var canvas = new AutomapCanvas(project);
        canvas.Connect(hall, MappableDirection.South, maze, true);
        canvas.Connect(maze, MappableDirection.East, rod, true);
        canvas.Connect(hall, MappableDirection.East, pit, true);

        var service = new CommandService();
        service.SetViewportSize(800, 520);
        service.FitToMap();
        var map = new MapCanvasControl { CommandService = service, Width = 800, Height = 520 };
        var window = new Window { Width = 800, Height = 520, Content = map, ShowInTaskbar = false };
        window.Show();
        try
        {
            MapCanvasControl.HereRoomOverride = maze;
            service.Select(hall);
            foreach (var (key, _) in HereToken.Choices.Where(choice => choice.Key != HereToken.Own))
            {
                ApplicationSettingsController.AppSettings.HereToken = key;
                Capture(map, Path.Combine(root, $"here-token-{key}.png"));
            }

            ApplicationSettingsController.AppSettings.HereToken = HereToken.WoodenDisc;
            service.ZoomFactor = 0.35f;
            Capture(map, Path.Combine(root, "here-token-zoomed-out.png"));
        }
        finally
        {
            window.Close();
        }
    }

    private static void Capture(Control control, string path)
    {
        control.InvalidateVisual();
        control.Measure(new Size(800, 520));
        control.Arrange(new Rect(0, 0, 800, 520));
        Dispatcher.UIThread.RunJobs();
        using var bitmap = new RenderTargetBitmap(new PixelSize(800, 520));
        bitmap.Render(control);
        bitmap.Save(path);
    }
}
