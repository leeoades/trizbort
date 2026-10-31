using Avalonia.Media.Imaging;
using SkiaSharp;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

/// <summary>Encodes a rendered map bitmap as PNG, JPEG or BMP.</summary>
public static class ImageEncoder
{
    /// <summary>JPEG quality (0-100).</summary>
    public const int JpegQuality = 90;

    public static void Encode(Bitmap bitmap, ImageFormat format, Stream output)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(output);

        if (format == ImageFormat.Png)
        {
            bitmap.Save(output);
            return;
        }

        // Re-read the pixels through PNG, then flatten onto white: JPEG and BMP have no (usable) alpha.
        using var png = new MemoryStream();
        bitmap.Save(png);
        png.Position = 0;
        using var decoded = SKBitmap.Decode(png);
        if (decoded == null) throw new InvalidOperationException("The rendered map image could not be read back.");

        using var flat = new SKBitmap(decoded.Width, decoded.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(flat))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(decoded, 0, 0);
        }

        if (format == ImageFormat.Jpeg)
        {
            using var image = SKImage.FromBitmap(flat);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            if (data == null) throw new InvalidOperationException("The image could not be encoded as JPEG.");
            data.SaveTo(output);
            return;
        }

        WriteBmp(flat, output);
    }

    /// <summary>Writes an uncompressed 24-bit BMP (Skia has no BMP encoder).</summary>
    private static void WriteBmp(SKBitmap bitmap, Stream output)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var rowSize = (width * 3 + 3) & ~3;
        var pixelBytes = rowSize * height;
        const int headerSize = 14 + 40;

        using var writer = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(headerSize + pixelBytes);
        writer.Write(0);
        writer.Write(headerSize);

        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(pixelBytes);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);

        var pixels = bitmap.Pixels;
        var row = new byte[rowSize];
        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var color = pixels[y * width + x];
                row[x * 3] = color.Blue;
                row[x * 3 + 1] = color.Green;
                row[x * 3 + 2] = color.Red;
            }
            writer.Write(row);
        }
        writer.Flush();
    }
}
