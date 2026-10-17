using PdfSharp.Drawing;
using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

public static class ElementDrawingExtensions
{
  public static void Draw(this Element element, XGraphics graphics, Palette palette, DrawingContext context)
  {
    if (element is Room room)
    {
      room.Draw(graphics, palette, context);
    }
    else if (element is Connection conn)
    {
      conn.Draw(graphics, palette, context);
    }
    else if (element is MapLabel label)
    {
      label.Draw(graphics, palette, context);
    }
  }

  public static void PreDraw(this Element element, DrawingContext context)
  {
    if (element is Room room)
    {
      room.PreDraw(context);
    }
  }

  public static void RecomputeSmartLineSegments(this Element element, DrawingContext context)
  {
    if (element is Connection conn)
    {
      conn.RecomputeSmartLineSegments(context);
    }
  }
}
