using System.Drawing;
using PdfSharp.Drawing;

namespace Trizbort.Domain.Misc {
  public class ConnectionIconBlock {
    private const int HorizontalLineOffset = 15;
    private const int VerticalStemOffset = 15;
    private const int VerticalLineOffset = 5;
    private const int HorizontalStemOffset = 5;

    public ConnectionIconBlock(LineSegment segment, int offset) {
      Segment = segment;
      Offset = offset;
    }

    public Bitmap Image { get; set; }

    //public Vector Position => CalculateBlockPosition();
    public int Offset { get; set; }

    public LineSegment Segment { get; set; }

    public void DrawBlock(XGraphics graphics) {
      var bounds = new Rect(Segment.Start, Vector.Zero);

      var compassPoint = CompassPointHelper.GetCompassPointFromDirectionVector(Segment.Delta);
      var pos = bounds.GetCorner(compassPoint);
      var offsets = new Size(0, 0);

      switch (compassPoint) {
        case CompassPoint.NorthWest:
          offsets.Height = -(Offset * 12);
          offsets.Width = -(Offset * 10) - HorizontalLineOffset;
          break;

        case CompassPoint.NorthEast:
          offsets.Height = -(Offset * 12);
          offsets.Width = Offset * 10 + HorizontalStemOffset;
          break;

        case CompassPoint.SouthEast:
          offsets.Height = Offset * 12;
          offsets.Width = Offset * 10 + HorizontalLineOffset;
          break;

        case CompassPoint.SouthWest:
          offsets.Height = Offset * 12 + VerticalLineOffset;
          offsets.Width = -(Offset * 10);
          break;

        case CompassPoint.East:
          offsets.Height = VerticalLineOffset;
          offsets.Width = Offset * 16 + HorizontalStemOffset;
          break;

        case CompassPoint.West:
          offsets.Height = VerticalLineOffset;
          offsets.Width = -(Offset * 16) - HorizontalLineOffset;
          break;

        case CompassPoint.South:
          offsets.Height = Offset * 16 + VerticalStemOffset;
          offsets.Width = -HorizontalLineOffset;
          break;

        case CompassPoint.North:
          offsets.Height = -(Offset * 16) - VerticalStemOffset;
          offsets.Width = -HorizontalLineOffset;
          break;
      }

      //      int dist = 15;
//      if (compassPoint == CompassPoint.NorthWest)
//        bounds.Inflate(-dist, -dist + 9);
//
//      if (compassPoint == CompassPoint.NorthEast)
//        bounds.Inflate(-dist - 5, -dist);
//
//      if (compassPoint == CompassPoint.SouthEast)
//        bounds.Inflate(-dist, dist - 9);
//
//      if (compassPoint == CompassPoint.SouthWest)
//        bounds.Inflate(-dist, -dist);
//


      graphics.DrawImage(Image, pos.ToPointF() + offsets);
    }
  }
}