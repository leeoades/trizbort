using System.Windows.Forms;
using PdfSharp.Drawing;
using Trizbort.UI.Controls;

namespace Trizbort.Domain.Misc;

public static class ResizeHandleExtensions
{
  public static Cursor GetCursor(this ResizeHandle handle)
  {
    switch (handle.CompassPoint)
    {
      case CompassPoint.NorthWest:
      case CompassPoint.SouthEast:
        return Cursors.SizeNWSE;
      case CompassPoint.NorthEast:
      case CompassPoint.SouthWest:
        return Cursors.SizeNESW;
      case CompassPoint.North:
      case CompassPoint.South:
        return Cursors.SizeNS;
      case CompassPoint.East:
      case CompassPoint.West:
        return Cursors.SizeWE;
      default:
        return null;
    }
  }

  public static void Draw(this ResizeHandle handle, Canvas canvas, XGraphics graphics, Palette palette, DrawingContext context)
  {
    Drawing.DrawHandle(canvas, graphics, palette, handle.Bounds, context, false, false);
  }
}
