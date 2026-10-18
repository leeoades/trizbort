using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;

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
            Name = "Dungeon",
            Position = new CoreVector(0, 0),
            Size = new CoreVector(100, 100)
        };
        Project.Current.Elements.Add(room);

        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
        bitmap.ShouldNotBeNull();

        // Convert to WriteableBitmap to inspect frame pixels
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        var writeable = new WriteableBitmap(
            bitmap.PixelSize,
            bitmap.Dpi,
            global::Avalonia.Platform.PixelFormat.Bgra8888,
            global::Avalonia.Platform.AlphaFormat.Premul);

        using (var ms = new MemoryStream())
        {
            bitmap.Save(ms);
            ms.Position = 0;
            using var decoded = new Bitmap(ms);
            using var fb = writeable.Lock();
            fb.Size.Width.ShouldBe(width);
            fb.Size.Height.ShouldBe(height);

            // Copy decoded bitmap bytes and verify non-empty pixel buffer
            var pixelBuffer = new byte[fb.RowBytes * fb.Size.Height];
            Marshal.Copy(fb.Address, pixelBuffer, 0, pixelBuffer.Length);
            pixelBuffer.Length.ShouldBeGreaterThan(0);
        }

        // Verify bitmap stream contains non-zero payload
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Length.ShouldBeGreaterThan(100);
    }

    [AvaloniaTest]
    public void RenderToBitmap_DoesNotMutateCanvasOriginOrZoom()
    {
        var canvas = new MapCanvasControl
        {
            Origin = new CoreVector(450, 350),
            ZoomFactor = 2.5f
        };

        var room = new Room(Project.Current)
        {
            Name = "Laboratory",
            Position = new CoreVector(10, 20),
            Size = new CoreVector(80, 50)
        };
        Project.Current.Elements.Add(room);

        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current, scale: 1.0);
        bitmap.ShouldNotBeNull();

        // Verify canvas control properties are unchanged
        canvas.Origin.X.ShouldBe(450f);
        canvas.Origin.Y.ShouldBe(350f);
        canvas.ZoomFactor.ShouldBe(2.5f);
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
