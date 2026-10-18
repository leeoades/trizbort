using PdfSharp.Drawing;
using Trizbort.UI.Controls;

namespace Trizbort.Domain.Misc;

public static class PortExtensions
{
  public static void Draw(this Port port, Canvas canvas, XGraphics graphics, Palette palette, DrawingContext context)
  {
    Drawing.DrawHandle(canvas, graphics, palette, port.VisualBounds, context, false, true);
  }
}
