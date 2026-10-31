using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Export;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 120: PNG, JPEG and BMP image export.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ImageFormatExportTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        Project.Current.Elements.Add(new Room(Project.Current)
        {
            Name = "",
            Position = new CoreVector(0, 0),
            Size = new CoreVector(60, 40)
        });
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private static byte[] EncodeMap(ImageFormat format)
    {
        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current);
        bitmap.ShouldNotBeNull();
        using var stream = new MemoryStream();
        ImageEncoder.Encode(bitmap, format, stream);
        return stream.ToArray();
    }

    [AvaloniaTest]
    public void Encode_Png_HasPngSignature()
    {
        var bytes = EncodeMap(ImageFormat.Png);
        bytes.Take(4).ShouldBe(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
    }

    [AvaloniaTest]
    public void Encode_Jpeg_HasJpegSignature()
    {
        var bytes = EncodeMap(ImageFormat.Jpeg);
        bytes.Take(2).ShouldBe(new byte[] { 0xFF, 0xD8 });
    }

    [AvaloniaTest]
    public void Encode_Bmp_HasBmpSignatureAndConsistentSize()
    {
        var bytes = EncodeMap(ImageFormat.Bmp);
        bytes.Take(2).ShouldBe(new byte[] { 0x42, 0x4D });
        BitConverter.ToInt32(bytes, 2).ShouldBe(bytes.Length);
    }

    [TestCase("map.png", ImageFormat.Jpeg, "map.png", ImageFormat.Png)]
    [TestCase("map.jpg", ImageFormat.Png, "map.jpg", ImageFormat.Jpeg)]
    [TestCase("map.JPEG", ImageFormat.Png, "map.JPEG", ImageFormat.Jpeg)]
    [TestCase("map.bmp", ImageFormat.Png, "map.bmp", ImageFormat.Bmp)]
    [TestCase("map", ImageFormat.Jpeg, "map.jpg", ImageFormat.Jpeg)]
    [TestCase("map.txt", ImageFormat.Bmp, "map.txt.bmp", ImageFormat.Bmp)]
    public void ResolveImageExportPath_ExtensionDecidesFormat(
        string picked, ImageFormat defaultFormat, string expectedName, ImageFormat expectedFormat)
    {
        var path = StorageService.ResolveImageExportPath(picked, defaultFormat, out var format);
        path.ShouldBe(expectedName);
        format.ShouldBe(expectedFormat);
    }
}
