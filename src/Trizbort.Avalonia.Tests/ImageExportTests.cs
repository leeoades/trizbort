using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Setup;
using DrawingColor = System.Drawing.Color;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ImageExportTests
{
    private Project? _previousProject;

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

    [Test]
    public void RenderToBitmap_EmptyMapOrNull_ReturnsNull()
    {
        // Null project
        MapCanvasControl.RenderToBitmap(null).ShouldBeNull();

        // Empty project
        var emptyProject = new Project();
        MapCanvasControl.RenderToBitmap(emptyProject).ShouldBeNull();
    }

    [AvaloniaTest]
    public void RenderToBitmap_PopulatedMap_RendersNonZeroBitmapAndSavesPng()
    {
        var room = new Room(Project.Current)
        {
            Name = "Great Underground Empire",
            Position = new CoreVector(100, 100),
            Size = new CoreVector(120, 80)
        };
        Project.Current.Elements.Add(room);

        var bounds = MinimapTransform.ComputeCanvasBounds(Project.Current.Elements, includePadding: true);
        bounds.ShouldNotBe(CoreRect.Empty);
        bounds.Width.ShouldBeGreaterThan(0);
        bounds.Height.ShouldBeGreaterThan(0);

        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
        bitmap.ShouldNotBeNull();
        bitmap.PixelSize.Width.ShouldBe((int)Math.Ceiling(bounds.Width));
        bitmap.PixelSize.Height.ShouldBe((int)Math.Ceiling(bounds.Height));

        // Save to PNG stream
        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Length.ShouldBeGreaterThan(0);

        var bytes = ms.ToArray();
        // Check PNG signature bytes (89 50 4E 47 0D 0A 1A 0A)
        bytes.Length.ShouldBeGreaterThan(8);
        bytes[0].ShouldBe((byte)0x89);
        bytes[1].ShouldBe((byte)0x50); // 'P'
        bytes[2].ShouldBe((byte)0x4E); // 'N'
        bytes[3].ShouldBe((byte)0x47); // 'G'
        bytes[4].ShouldBe((byte)0x0D);
        bytes[5].ShouldBe((byte)0x0A);
        bytes[6].ShouldBe((byte)0x1A);
        bytes[7].ShouldBe((byte)0x0A);
    }

    [AvaloniaTest]
    public void RenderToBitmap_ScaleFactor_ScalesDimensionsProportionally()
    {
        var room = new Room(Project.Current)
        {
            Name = "Hallway",
            Position = new CoreVector(50, 50),
            Size = new CoreVector(100, 60)
        };
        Project.Current.Elements.Add(room);

        var bounds = MinimapTransform.ComputeCanvasBounds(Project.Current.Elements, includePadding: true);

        using var bitmap1x = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
        using var bitmap2x = MapCanvasControl.RenderToBitmap(Project.Current, scale: 2.0);

        bitmap1x.ShouldNotBeNull();
        bitmap2x.ShouldNotBeNull();

        bitmap1x.PixelSize.Width.ShouldBe((int)Math.Ceiling(bounds.Width));
        bitmap1x.PixelSize.Height.ShouldBe((int)Math.Ceiling(bounds.Height));

        bitmap2x.PixelSize.Width.ShouldBe((int)Math.Ceiling(bounds.Width * 2.0));
        bitmap2x.PixelSize.Height.ShouldBe((int)Math.Ceiling(bounds.Height * 2.0));
    }

    [AvaloniaTest]
    public void RenderToBitmap_ContainsRenderedPixels()
    {
        var room = new Room(Project.Current)
        {
            Name = "", // Empty name for font independence
            Position = new CoreVector(0, 0),
            Size = new CoreVector(100, 100),
            RoomFillColor = DrawingColor.Red,
            RoomBorderColor = DrawingColor.Red
        };
        Project.Current.Elements.Add(room);

        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
        bitmap.ShouldNotBeNull();

        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;

        // Sample interior point, away from the center which might be affected by text centering
        var x = width / 2 + 10;
        var y = height / 2 + 10;
        
        var centerPixel = ReadPixel(bitmap, x, y);
        centerPixel.R.ShouldBeGreaterThan((byte)200);
        centerPixel.G.ShouldBeLessThan((byte)50);
        centerPixel.B.ShouldBeLessThan((byte)50);
        centerPixel.A.ShouldBe((byte)255);

        // Corner pixel is canvas background padding (White)
        var cornerPixel = ReadPixel(bitmap, 2, 2);
        cornerPixel.R.ShouldBe((byte)255);
        cornerPixel.G.ShouldBe((byte)255);
        cornerPixel.B.ShouldBe((byte)255);
        cornerPixel.A.ShouldBe((byte)255);

        // Verify bitmap stream contains non-zero payload
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Length.ShouldBeGreaterThan(100);
    }

    [AvaloniaTest]
    public void RenderToBitmap_TransparentRoomFill_UsesDefaultRegionColor()
    {
        var previousRegions = Settings.Regions.Select(CloneRegion).ToList();

        try
        {
            Settings.Regions = new List<Region>
            {
                new()
                {
                    RegionName = Region.DefaultRegion,
                    RColor = DrawingColor.FromArgb(32, 192, 96),
                    TextColor = DrawingColor.Black
                }
            };

            var room = new Room(Project.Current)
            {
                Name = string.Empty,
                Position = new CoreVector(0, 0),
                Size = new CoreVector(100, 100),
                Region = Region.DefaultRegion,
                RoomFillColor = DrawingColor.Transparent,
                RoomBorderColor = DrawingColor.Transparent
            };
            Project.Current.Elements.Add(room);

            using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
            bitmap.ShouldNotBeNull();

            var pixel = ReadPixel(bitmap, bitmap.PixelSize.Width / 2, bitmap.PixelSize.Height / 2);
            pixel.R.ShouldBeLessThan((byte)80);
            pixel.G.ShouldBeGreaterThan((byte)160);
            pixel.B.ShouldBeLessThan((byte)140);
            pixel.A.ShouldBe((byte)255);
        }
        finally
        {
            Settings.Regions = previousRegions;
        }
    }

    [AvaloniaTest]
    public void RenderToBitmap_LargeMap_FitsWithin8192WithoutCroppingExtremities()
    {
        // Place two rooms 20,000 units apart
        var room1 = new Room(Project.Current)
        {
            Name = "Northwest Outpost",
            Position = new CoreVector(-10000, -10000),
            Size = new CoreVector(100, 100),
            RoomFillColor = DrawingColor.Red,
            RoomBorderColor = DrawingColor.Red
        };
        var room2 = new Room(Project.Current)
        {
            Name = "Southeast Outpost",
            Position = new CoreVector(10000, 10000),
            Size = new CoreVector(100, 100),
            RoomFillColor = DrawingColor.Blue,
            RoomBorderColor = DrawingColor.Blue
        };
        Project.Current.Elements.Add(room1);
        Project.Current.Elements.Add(room2);

        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
        bitmap.ShouldNotBeNull();

        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;

        // Dimensions must be capped at 8192
        width.ShouldBeLessThanOrEqualTo(8192);
        height.ShouldBeLessThanOrEqualTo(8192);
        width.ShouldBe(8192);
        height.ShouldBe(8192);

        var bounds = MinimapTransform.ComputeCanvasBounds(Project.Current.Elements, includePadding: true);
        var effectiveScale = Math.Min((double)width / bounds.Width, (double)height / bounds.Height);

        // Calculate expected pixel positions for center of each room
        var room1Center = new CoreVector(room1.Position.X + room1.Size.X / 2f, room1.Position.Y + room1.Size.Y / 2f);
        var room2Center = new CoreVector(room2.Position.X + room2.Size.X / 2f, room2.Position.Y + room2.Size.Y / 2f);

        var px1 = (int)Math.Round((room1Center.X - bounds.Center.X) * effectiveScale + width / 2.0);
        var py1 = (int)Math.Round((room1Center.Y - bounds.Center.Y) * effectiveScale + height / 2.0);

        var px2 = (int)Math.Round((room2Center.X - bounds.Center.X) * effectiveScale + width / 2.0);
        var py2 = (int)Math.Round((room2Center.Y - bounds.Center.Y) * effectiveScale + height / 2.0);

        px1.ShouldBeGreaterThanOrEqualTo(0);
        py1.ShouldBeGreaterThanOrEqualTo(0);
        px2.ShouldBeLessThan(width);
        py2.ShouldBeLessThan(height);

        // Northwest room must be visible as Red
        var pixel1 = ReadPixel(bitmap, px1, py1);
        pixel1.R.ShouldBeGreaterThan((byte)200);
        pixel1.B.ShouldBeLessThan((byte)50);

        // Southeast room must be visible as Blue
        var pixel2 = ReadPixel(bitmap, px2, py2);
        pixel2.B.ShouldBeGreaterThan((byte)200);
        pixel2.R.ShouldBeLessThan((byte)50);
    }

    private static (byte B, byte G, byte R, byte A) ReadPixel(RenderTargetBitmap bmp, int x, int y)
    {
        var buf = new byte[4];
        var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
        try
        {
            bmp.CopyPixels(new PixelRect(x, y, 1, 1), handle.AddrOfPinnedObject(), buf.Length, 4);
        }
        finally
        {
            handle.Free();
        }
        return (buf[0], buf[1], buf[2], buf[3]);
    }

    private static Region CloneRegion(Region region)
    {
        return new Region
        {
            RegionName = region.RegionName,
            RegionId = region.RegionId,
            RColor = region.RColor,
            TextColor = region.TextColor
        };
    }

    [Test]
    public void ComputeCanvasBounds_PaddingAndMargins_CalculatesCorrectExtents()
    {
        var room = new Room(Project.Current)
        {
            Position = new CoreVector(100, 100),
            Size = new CoreVector(100, 100)
        };
        Project.Current.Elements.Add(room);

        var boundsWithoutPadding = MapCanvasControl.ComputeCanvasBounds(Project.Current, includePadding: false);
        boundsWithoutPadding.Width.ShouldBeGreaterThan(0);
        boundsWithoutPadding.Height.ShouldBeGreaterThan(0);

        var boundsWithPadding = MapCanvasControl.ComputeCanvasBounds(Project.Current, includePadding: true);
        boundsWithPadding.Width.ShouldBeGreaterThan(boundsWithoutPadding.Width);
        boundsWithPadding.Height.ShouldBeGreaterThan(boundsWithoutPadding.Height);
    }

    private class MockDialogService : IDialogService
    {
        public string? LastMessageTitle { get; private set; }
        public string? LastMessageText { get; private set; }

        public Task ShowAboutDialogAsync(Window? owner) => Task.CompletedTask;

        public Task ShowMessageAsync(Window? owner, string title, string message)
        {
            LastMessageTitle = title;
            LastMessageText = message;
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmationAsync(Window? owner, string title, string message) => Task.FromResult(true);
        public Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message) => Task.FromResult(ConfirmationResult.Discard);
        public Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room) => Task.FromResult(false);
        public Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection) => Task.FromResult(false);
        public Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label) => Task.FromResult(false);
        public Task<bool> ShowElementPropertiesAsync(Window? owner, Element element) => Task.FromResult(false);
        public Task<bool> ShowMapSettingsAsync(Window? owner) => Task.FromResult(false);
        public Task<bool> ShowMapThemeAsync(Window? owner) => Task.FromResult(false);
        public Task<bool> ShowAppSettingsAsync(Window? owner) => Task.FromResult(false);
        public Task<Trizbort.Automap.AutomapSettings?> ShowAutomapSettingsAsync(Window? owner) => Task.FromResult<Trizbort.Automap.AutomapSettings?>(null);
    }

    [AvaloniaTest]
    public async Task ExportImageAsync_EmptyMap_ShowsMessageAndReturnsFalse()
    {
        var mockDialog = new MockDialogService();
        var storage = new StorageService(mockDialog);

        var window = new Window();
        var result = await storage.ExportImageAsync(window);

        result.ShouldBeFalse();
        mockDialog.LastMessageTitle.ShouldBe("Export Image");
        mockDialog.LastMessageText.ShouldBe("Cannot export an empty map.");
    }

    [Test]
    public async Task ExportImageAsync_NullTopLevel_ReturnsFalse()
    {
        var mockDialog = new MockDialogService();
        var storage = new StorageService(mockDialog);

        var result = await storage.ExportImageAsync(null);
        result.ShouldBeFalse();
    }
}
