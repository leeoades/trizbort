using System.Drawing;

namespace Trizbort.Util;

internal class StringFormats
{
  static StringFormats()
  {
    Center = new StringFormat {
      Alignment = StringAlignment.Center,
      LineAlignment = StringAlignment.Center,
      Trimming = StringTrimming.EllipsisCharacter,
      FormatFlags = StringFormatFlags.LineLimit
    };

    Left = new StringFormat {
      Alignment = StringAlignment.Near,
      LineAlignment = StringAlignment.Center,
      Trimming = StringTrimming.EllipsisCharacter,
      FormatFlags = StringFormatFlags.LineLimit
    };
  }

  public static StringFormat Center { get; }

  public static StringFormat Left { get; }
}