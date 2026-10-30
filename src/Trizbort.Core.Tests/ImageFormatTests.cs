using NUnit.Framework;
using Shouldly;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 120: image format mapping.</summary>
[TestFixture]
[Category("Unit")]
public class ImageFormatTests
{
  [TestCase(".png", ImageFormat.Png)]
  [TestCase("PNG", ImageFormat.Png)]
  [TestCase(".jpg", ImageFormat.Jpeg)]
  [TestCase(".JPEG", ImageFormat.Jpeg)]
  [TestCase(".bmp", ImageFormat.Bmp)]
  public void TryFromExtension_KnownExtensions(string extension, ImageFormat expected)
  {
    ImageFormats.TryFromExtension(extension, out var format).ShouldBeTrue();
    format.ShouldBe(expected);
  }

  [TestCase("")]
  [TestCase(null)]
  [TestCase(".txt")]
  [TestCase(".emf")]
  public void TryFromExtension_UnknownExtensions(string extension)
  {
    ImageFormats.TryFromExtension(extension, out _).ShouldBeFalse();
    ImageFormats.FromExtension(extension, ImageFormat.Bmp).ShouldBe(ImageFormat.Bmp);
  }

  [TestCase(0, ImageFormat.Png)]
  [TestCase(1, ImageFormat.Jpeg)]
  [TestCase(2, ImageFormat.Bmp)]
  [TestCase(3, ImageFormat.Png)]
  [TestCase(99, ImageFormat.Png)]
  [TestCase(-1, ImageFormat.Png)]
  public void FromSetting_MapsEmfAndUnknownToPng(int setting, ImageFormat expected)
  {
    ImageFormats.FromSetting(setting).ShouldBe(expected);
  }

  [TestCase(ImageFormat.Png, ".png")]
  [TestCase(ImageFormat.Jpeg, ".jpg")]
  [TestCase(ImageFormat.Bmp, ".bmp")]
  public void Extension_RoundTrips(ImageFormat format, string extension)
  {
    ImageFormats.Extension(format).ShouldBe(extension);
    ImageFormats.FromExtension(extension, ImageFormat.Png).ShouldBe(format);
    ImageFormats.FromSetting(ImageFormats.ToSetting(format)).ShouldBe(format);
  }
}
