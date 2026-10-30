using System;

namespace Trizbort.Export;

/// <summary>Raster image formats the map can be exported to.</summary>
public enum ImageFormat
{
  Png,
  Jpeg,
  Bmp
}

/// <summary>Extensions, display names and <c>DefaultImageType</c> setting mapping for <see cref="ImageFormat" />.</summary>
public static class ImageFormats
{
  /// <summary>The <c>DefaultImageType</c> setting value of the legacy EMF format (not ported).</summary>
  public const int EmfSettingValue = 3;

  public static string Extension(ImageFormat format)
  {
    switch (format)
    {
      case ImageFormat.Png: return ".png";
      case ImageFormat.Jpeg: return ".jpg";
      case ImageFormat.Bmp: return ".bmp";
      default: throw new ArgumentOutOfRangeException(nameof(format), format, null);
    }
  }

  public static string DisplayName(ImageFormat format)
  {
    switch (format)
    {
      case ImageFormat.Png: return "PNG";
      case ImageFormat.Jpeg: return "JPEG";
      case ImageFormat.Bmp: return "BMP";
      default: throw new ArgumentOutOfRangeException(nameof(format), format, null);
    }
  }

  /// <summary>Maps a file extension (with or without dot, any case; .jpeg is accepted) to a format.</summary>
  public static bool TryFromExtension(string extension, out ImageFormat format)
  {
    format = ImageFormat.Png;
    if (string.IsNullOrEmpty(extension)) return false;
    var text = extension.TrimStart('.').ToLowerInvariant();
    switch (text)
    {
      case "png": format = ImageFormat.Png; return true;
      case "jpg":
      case "jpeg": format = ImageFormat.Jpeg; return true;
      case "bmp": format = ImageFormat.Bmp; return true;
      default: return false;
    }
  }

  /// <summary>The format for an extension, or <paramref name="fallback" /> when the extension is unknown.</summary>
  public static ImageFormat FromExtension(string extension, ImageFormat fallback)
  {
    return TryFromExtension(extension, out var format) ? format : fallback;
  }

  /// <summary>
  ///   Maps the <c>DefaultImageType</c> setting (0 png, 1 jpg, 2 bmp, 3 emf) to a format. EMF is Windows-only and is
  ///   not ported, so it and any unknown value map to PNG.
  /// </summary>
  public static ImageFormat FromSetting(int value)
  {
    switch (value)
    {
      case 1: return ImageFormat.Jpeg;
      case 2: return ImageFormat.Bmp;
      default: return ImageFormat.Png;
    }
  }

  /// <summary>The setting value for a format (inverse of <see cref="FromSetting" />).</summary>
  public static int ToSetting(ImageFormat format)
  {
    switch (format)
    {
      case ImageFormat.Jpeg: return 1;
      case ImageFormat.Bmp: return 2;
      default: return 0;
    }
  }
}
