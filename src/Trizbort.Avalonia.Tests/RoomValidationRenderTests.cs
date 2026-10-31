using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 130: invalid rooms get a red cross on the map and a red tooltip.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RoomValidationRenderTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        Trizbort.Setup.Settings.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
        Trizbort.Setup.Settings.Reset();
    }

    private static void AddDuplicatePair()
    {
        foreach (var position in new[] { new CoreVector(-50, -50), new CoreVector(500, 500) })
        {
            Project.Current.Elements.Add(new Room(Project.Current)
            {
                Name = "",
                Position = position,
                Size = new CoreVector(100, 100)
            });
        }
    }

    private static RenderTargetBitmap Render()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(300, 200), new Vector(96, 96));
        using (var ctx = bitmap.CreateDrawingContext())
        {
            MapCanvasControl.RenderMap(ctx, Project.Current, new CoreVector(0, 0), 1.0, 300, 200);
        }

        return bitmap;
    }

    private static (byte B, byte G, byte R, byte A) ReadPixel(RenderTargetBitmap bmp, int x, int y)
        => TestPixels.Read(bmp, x, y);

    [AvaloniaTest]
    public void UniqueNamesOn_DuplicateRoomHasRedCrossOnItsDiagonal()
    {
        AddDuplicatePair();
        Project.Current.MustHaveUniqueNames = true;

        using var bitmap = Render();

        var pixel = ReadPixel(bitmap, 170, 120);
        pixel.R.ShouldBeGreaterThan((byte)200);
        pixel.G.ShouldBeLessThan((byte)60);
        pixel.B.ShouldBeLessThan((byte)60);
    }

    [AvaloniaTest]
    public void UniqueNamesOff_NoRedCross()
    {
        AddDuplicatePair();

        using var bitmap = Render();

        var pixel = ReadPixel(bitmap, 170, 120);
        pixel.G.ShouldBeGreaterThan((byte)200);
        pixel.B.ShouldBeGreaterThan((byte)200);
    }

    [AvaloniaTest]
    public void InvalidRoomTooltip_IsRedAndListsTheMessage()
    {
        AddDuplicatePair();
        Project.Current.MustHaveUniqueNames = true;
        var room = Project.Current.Elements.OfType<Room>().First();
        var previous = ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips;
        ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips = true;
        try
        {
            var model = TooltipModel.Create(room);
            model.ShouldNotBeNull();

            var tip = ElementToolTipFactory.Build(model);

            ((Border)tip).Background.ShouldBeOfType<SolidColorBrush>().Color.ShouldBe(Colors.Red);
            ElementToolTipFactory.PlainText(tip).ShouldContain("The room name is not unique.");
        }
        finally
        {
            ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips = previous;
        }
    }
}
