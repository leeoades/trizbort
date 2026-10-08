using Trizbort.UI;
using System;
using System.Drawing;
using System.Windows.Forms;
using Trizbort.Extensions;

namespace Trizbort.Domain.Misc {
  internal static class Colors {
    public static readonly int Canvas = 0;
    public static readonly int Border = 1;
    public static readonly int Line = 2;
    public static readonly int SelectedLine = 3;
    public static readonly int HoverLine = 4;
    public static readonly int Subtitle = 5;
    public static readonly int SmallText = 6;
    public static readonly int LineText = 7;
    public static readonly int Grid = 8;
    public static readonly int StartRoom = 9;
    public static readonly int EndRoom = 10;
    public static readonly int Count = 11;

    private static readonly string[] Names = {
      "canvas",
      "border",
      "line",
      "selectedLine",
      "hoverLine",
      "subTitle",
      "smallText",
      "lineText",
      "grid",
      "startRoom",
      "endRoom"
    };

    public static bool FromName(string name, out int color) {
      for (var index = 0; index < Names.Length; ++index)
        if (StringComparer.InvariantCultureIgnoreCase.Compare(name ?? string.Empty, Names[index]) == 0) {
          color = index;
          return true;
        }

      color = -1;
      return false;
    }

    public static string SaveColor(Color colorAttribute) {
      if (colorAttribute == Color.Transparent)
        return string.Empty;

      var colorValue = colorAttribute.ToHex();
      return colorValue;
    }

    public static Color ShowColorDialog(Color color, Form parent) {
      using (var dialog = new ColorDialog()) {
        dialog.Color = color == Color.Transparent ? Color.White : color;

        return UserInteraction.ShowDialog(dialog, parent) == DialogResult.OK ? dialog.Color : color;
      }
    }

    public static bool ToName(int color, out string name) {
      if (color >= 0 && color < Names.Length) {
        name = Names[color];
        return true;
      }

      name = string.Empty;
      return false;
    }
  }
}