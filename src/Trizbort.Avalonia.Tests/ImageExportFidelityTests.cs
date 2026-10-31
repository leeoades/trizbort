using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Export;
using Trizbort.Setup;
using DrawingColor = System.Drawing.Color;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ImageExportFidelityTests
{
    private Project? _previousProject;
    private DrawingColor _previousCanvas;
    private bool _previousSpecify;
    private int _previousH;
    private int _previousV;
    private bool _previousDocMargins;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        var app = ApplicationSettingsController.AppSettings;
        _previousCanvas = Settings.Color[Colors.Canvas];
        _previousSpecify = app.SpecifyGenMargins;
        _previousH = app.GenHorizontalMargin;
        _previousV = app.GenVerticalMargin;
        _previousDocMargins = Settings.DocumentSpecificMargins;
        Settings.DocumentSpecificMargins = false;
    }

    [TearDown]
    public void TearDown()
    {
        var app = ApplicationSettingsController.AppSettings;
        Settings.Color[Colors.Canvas] = _previousCanvas;
        app.SpecifyGenMargins = _previousSpecify;
        app.GenHorizontalMargin = _previousH;
        app.GenVerticalMargin = _previousV;
        Settings.DocumentSpecificMargins = _previousDocMargins;
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private static Room AddRoom(float x, float y)
    {
        var room = new Room(Project.Current)
        {
            Name = "",
            Position = new CoreVector(x, y),
            Size = new CoreVector(100, 100)
        };
        Project.Current.Elements.Add(room);
        return room;
    }

    private static (byte B, byte G, byte R, byte A) ReadPixel(RenderTargetBitmap bmp, int x, int y)
        => TestPixels.Read(bmp, x, y);

    [AvaloniaTest]
    public void Background_UsesThemeCanvasColour()
    {
        Settings.Color[Colors.Canvas] = DrawingColor.FromArgb(255, 10, 200, 30);
        AddRoom(0, 0);

        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
        bitmap.ShouldNotBeNull();
        var pixel = ReadPixel(bitmap, 1, 1);
        pixel.R.ShouldBe((byte)10);
        pixel.G.ShouldBe((byte)200);
        pixel.B.ShouldBe((byte)30);
        pixel.A.ShouldBe((byte)255);
    }

    [AvaloniaTest]
    public void GeneralMargins_ChangeImageSizeByTheExpectedAmount()
    {
        AddRoom(0, 0);
        var app = ApplicationSettingsController.AppSettings;
        app.SpecifyGenMargins = true;
        app.GenHorizontalMargin = 10;
        app.GenVerticalMargin = 10;
        using var small = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);

        app.GenHorizontalMargin = 60;
        app.GenVerticalMargin = 40;
        using var big = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);

        small.ShouldNotBeNull();
        big.ShouldNotBeNull();
        (big.PixelSize.Width - small.PixelSize.Width).ShouldBeInRange(99, 101);
        (big.PixelSize.Height - small.PixelSize.Height).ShouldBeInRange(59, 61);
    }

    [AvaloniaTest]
    public void DocumentMargins_TakePrecedenceOverGeneralMargins()
    {
        AddRoom(0, 0);
        var app = ApplicationSettingsController.AppSettings;
        app.SpecifyGenMargins = true;
        app.GenHorizontalMargin = 500;
        app.GenVerticalMargin = 500;
        var previousH = Settings.DocHorizontalMargin;
        var previousV = Settings.DocVerticalMargin;
        try
        {
            Settings.DocumentSpecificMargins = true;
            Settings.DocHorizontalMargin = 20;
            Settings.DocVerticalMargin = 20;
            using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
            bitmap.ShouldNotBeNull();
            bitmap.PixelSize.Width.ShouldBeLessThan(300);
        }
        finally
        {
            Settings.DocHorizontalMargin = previousH;
            Settings.DocVerticalMargin = previousV;
        }
    }

    [AvaloniaTest]
    public void SelectionOnly_ProducesSmallerImageThanWholeMap()
    {
        var a = AddRoom(0, 0);
        AddRoom(1000, 0);
        var selection = new Selection();
        selection.Set(new Element[] { a });
        var settings = ApplicationSettingsController.AppSettings;

        var wholePlan = ImageExportPlanner.Plan(Project.Current, selection, null, new ImageExportOptions { Scale = 1.0 }, settings);
        var selPlan = ImageExportPlanner.Plan(Project.Current, selection, null, new ImageExportOptions { Scale = 1.0, SelectionOnly = true }, settings);
        using var whole = MapCanvasControl.RenderToBitmap(Project.Current, wholePlan);
        using var only = MapCanvasControl.RenderToBitmap(Project.Current, selPlan);

        whole.ShouldNotBeNull();
        only.ShouldNotBeNull();
        only.PixelSize.Width.ShouldBeLessThan(whole.PixelSize.Width);
    }
}
