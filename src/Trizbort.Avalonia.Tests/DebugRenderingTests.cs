using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using DrawingColor = System.Drawing.Color;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 108: the --debug rendering toggles change what the canvas draws, and only with the flag on.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class DebugRenderingTests
{
    private Project? _previousProject;
    private bool _savedElements;
    private bool _savedLines;
    private bool _savedFps;
    private bool _savedMouse;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        var settings = ApplicationSettingsController.AppSettings;
        _savedElements = settings.DebugDisableElementRendering;
        _savedLines = settings.DebugDisableLineRendering;
        _savedFps = settings.DebugShowFps;
        _savedMouse = settings.DebugShowMouseCoordinates;
        settings.DebugDisableElementRendering = false;
        settings.DebugDisableLineRendering = false;
        settings.DebugShowFps = false;
        settings.DebugShowMouseCoordinates = false;
    }

    [TearDown]
    public void TearDown()
    {
        var settings = ApplicationSettingsController.AppSettings;
        settings.DebugDisableElementRendering = _savedElements;
        settings.DebugDisableLineRendering = _savedLines;
        settings.DebugShowFps = _savedFps;
        settings.DebugShowMouseCoordinates = _savedMouse;
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private static Room AddRedRoom()
    {
        var room = new Room(Project.Current)
        {
            Name = string.Empty,
            Position = new CoreVector(100, 100),
            Size = new CoreVector(60, 60),
            RoomFillColor = DrawingColor.Red,
            RoomBorderColor = DrawingColor.Red
        };
        Project.Current.Elements.Add(room);
        return room;
    }

    // Renders a 400x300 canvas centred on the room at (130, 130), which lands at client (200, 150).
    private static (byte B, byte G, byte R, byte A) RenderAndReadRoomCentre(CommandService service)
    {
        service.SetView(new CoreVector(130, 130), 1.0f);
        var canvas = new MapCanvasControl { Width = 400, Height = 300, CommandService = service };
        var window = new Window { Width = 400, Height = 300, Content = canvas };
        window.Show();
        try
        {
            window.Measure(new Size(400, 300));
            window.Arrange(new Rect(0, 0, 400, 300));
            canvas.Measure(new Size(400, 300));
            canvas.Arrange(new Rect(0, 0, 400, 300));

            using var bitmap = new RenderTargetBitmap(new PixelSize(400, 300));
            bitmap.Render(canvas);
            return ReadPixel(bitmap, 200, 150);
        }
        finally
        {
            window.Close();
        }
    }

    private static (byte B, byte G, byte R, byte A) ReadPixel(RenderTargetBitmap bitmap, int x, int y)
        => TestPixels.Read(bitmap, x, y);

    [AvaloniaTest]
    public void ElementRenderingOff_WithoutDebug_StillDrawsRoom()
    {
        AddRedRoom();
        ApplicationSettingsController.AppSettings.DebugDisableElementRendering = true;
        var service = new CommandService();
        service.Session.DebugEnabled.ShouldBeFalse();

        var (b, g, r, _) = RenderAndReadRoomCentre(service);

        r.ShouldBeGreaterThan((byte)200);
        g.ShouldBeLessThan((byte)50);
        b.ShouldBeLessThan((byte)50);
    }

    [AvaloniaTest]
    public void ElementRenderingOff_WithDebug_DrawsNoRoomPixels()
    {
        AddRedRoom();
        ApplicationSettingsController.AppSettings.DebugDisableElementRendering = true;
        var service = new CommandService();
        service.Session.DebugEnabled = true;

        var (b, g, r, _) = RenderAndReadRoomCentre(service);

        // the canvas background shows through where the room would be
        g.ShouldBeGreaterThan((byte)200);
        b.ShouldBeGreaterThan((byte)200);
        r.ShouldBeGreaterThan((byte)200);
    }

    [AvaloniaTest]
    public void ElementRenderingOn_WithDebug_DrawsRoom()
    {
        AddRedRoom();
        var service = new CommandService();
        service.Session.DebugEnabled = true;

        var (b, g, r, _) = RenderAndReadRoomCentre(service);

        r.ShouldBeGreaterThan((byte)200);
        g.ShouldBeLessThan((byte)50);
        b.ShouldBeLessThan((byte)50);
    }

    [AvaloniaTest]
    public void FpsAndMouseCoordinates_WithDebug_RenderWithoutError()
    {
        AddRedRoom();
        var settings = ApplicationSettingsController.AppSettings;
        settings.DebugShowFps = true;
        settings.DebugShowMouseCoordinates = true;
        var service = new CommandService();
        service.Session.DebugEnabled = true;

        var (_, _, r, _) = RenderAndReadRoomCentre(service);

        // the room is still drawn under the overlay text, which sits in the top-left corner
        r.ShouldBeGreaterThan((byte)200);
    }

    [AvaloniaTest]
    public void LineRenderingFlag_FollowsSettingOnlyWithDebug()
    {
        var service = new CommandService();
        ApplicationSettingsController.AppSettings.DebugDisableLineRendering = true;

        service.Session.LineRenderingEnabled.ShouldBeTrue();
        service.Session.DebugEnabled = true;
        service.Session.LineRenderingEnabled.ShouldBeFalse();
    }
}
