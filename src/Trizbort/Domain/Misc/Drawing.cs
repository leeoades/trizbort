using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PdfSharp.Drawing;
using Trizbort.Domain.Elements;
using Trizbort.Properties;
using Trizbort.UI.Controls;
using Trizbort.Util;
using Settings = Trizbort.Setup.Settings;

namespace Trizbort.Domain.Misc;

internal static class Drawing
{
  private static readonly Cursor _drawLineCursor;
  private static readonly Cursor _drawLineInvertedCursor;
  private static readonly Cursor _moveLineCursor;
  private static readonly Cursor _moveLineInvertedCursor;
  private static XGraphicsPath _chevronPath;

  static Drawing()
  {
    _drawLineCursor = LoadCursor(Resources.DrawLineCursor);
    _drawLineInvertedCursor = LoadCursor(Resources.DrawLineInvertedCursor);
    _moveLineCursor = LoadCursor(Resources.MoveLineCursor);
    _moveLineInvertedCursor = LoadCursor(Resources.MoveLineInvertedCursor);
  }

  public static Cursor DrawLineCursor =>
    IsDark(Settings.Color[Colors.Canvas]) ? _drawLineInvertedCursor : _drawLineCursor;

  public static Cursor MoveLineCursor =>
    IsDark(Settings.Color[Colors.Canvas]) ? _moveLineInvertedCursor : _moveLineCursor;

  public static void AddLine(XGraphicsPath path, LineSegment segment, Random random, bool straightEdges)
  {
    if (!straightEdges)
      path.AddLines(Sketch.Line(segment.Start.ToPointF(), segment.End.ToPointF(), random));
    else
      path.AddLine(segment.Start.ToPointF(), segment.End.ToPointF());
  }

  public static PointF Divide(PointF pos, float scalar)
  {
    return new PointF(pos.X / scalar, pos.Y / scalar);
  }

  public static void DrawChevron(
    XGraphics graphics,
    PointF pos,
    float angle,
    float size,
    Brush fillBrush,
    Random sketch = null)
  {
    if (_chevronPath == null)
    {
      var apex = new PointF(0.5f, 0);
      var leftCorner = new PointF(-0.5f, 0.5f);
      var rightCorner = new PointF(-0.5f, -0.5f);
      _chevronPath = new XGraphicsPath();
      _chevronPath.AddLine(apex, rightCorner);
      _chevronPath.AddLine(rightCorner, leftCorner);
      _chevronPath.AddLine(leftCorner, apex);
    }

    var path = _chevronPath;
    if (sketch != null)
    {
      // an irregular, slightly lopsided arrowhead, as if inked by hand
      float Jitter()
      {
        return (float)(sketch.NextDouble() - 0.5) * 0.14f;
      }

      path = new XGraphicsPath();
      path.AddPolygon(
        new[] {
          new PointF(0.5f + Jitter(), Jitter()),
          new PointF(-0.5f + Jitter(), -0.5f + Jitter()),
          new PointF(-0.38f + Jitter(), Jitter() * 0.5f),
          new PointF(-0.5f + Jitter(), 0.5f + Jitter())
        });
    }

    var state = graphics.Save();
    graphics.TranslateTransform(pos.X, pos.Y);
    graphics.RotateTransform(angle);
    graphics.ScaleTransform(size, size);
    graphics.DrawPath(fillBrush, path);
    graphics.Restore(state);
  }

  public static void DrawHandle(
    Canvas canvas,
    XGraphics graphics,
    Palette palette,
    Rect bounds,
    DrawingContext context,
    bool alwaysAlpha,
    bool round)
  {
    if (bounds.Width <= 0 || bounds.Height <= 0) return;

    using var quality = new Smoothing(graphics, XSmoothingMode.Default);
    XBrush brush;
    Pen pen;
    var alpha = 180;

    if (context.Selected)
    {
      if (!alwaysAlpha) alpha = 255;
      brush = palette.Gradient(
        bounds,
        Color.FromArgb(alpha, Color.LemonChiffon),
        Color.FromArgb(alpha, Color.DarkOrange));
      pen = palette.Pen(Color.FromArgb(alpha, Color.Chocolate), 0);
    }
    else
    {
      brush = palette.Gradient(bounds, Color.FromArgb(alpha, Color.LightCyan), Color.FromArgb(alpha, Color.SteelBlue));
      pen = palette.Pen(Color.FromArgb(alpha, Color.Navy), 0);
    }

    if (round)
    {
      graphics.DrawEllipse(brush, bounds.ToRectangleF());
      //          graphics.DrawRectangle(new XPen(Color.Red), bounds.ToRectangleF() );
      graphics.DrawEllipse(pen, bounds.ToRectangleF());
    }
    else
    {
      graphics.DrawRectangle(brush, bounds.ToRectangleF());
      graphics.DrawRectangle(pen, bounds.ToRectangleF());
    }
  }

  public static string FontName(Font font)
  {
    if (!string.IsNullOrEmpty(font.OriginalFontName)) return font.OriginalFontName;
    return font.Name;
  }

  public static bool IsDark(Color color)
  {
    return Math.Max(color.R, Math.Max(color.G, color.B)) < 128;
  }

  public static Color Mix(Color a, Color b, int propA, int propB)
  {
    return Color.FromArgb(
      (byte)((a.R * propA + b.R * propB) / (propA + propB)),
      (byte)((a.G * propA + b.G * propB) / (propA + propB)),
      (byte)((a.B * propA + b.B * propB) / (propA + propB)));
  }

  public static bool SetAlignmentFromCardinalOrOrdinalDirection(
    XStringFormat format,
    CompassPoint compassPoint,
    RoomShape? rs = null)
  {
    switch (compassPoint)
    {
      case CompassPoint.North:
      case CompassPoint.NorthEast:
        format.LineAlignment = XLineAlignment.Far;
        format.Alignment = XStringAlignment.Near;
        break;
      case CompassPoint.East:
      case CompassPoint.SouthEast:
      case CompassPoint.South:
        format.LineAlignment = XLineAlignment.Near;
        format.Alignment = XStringAlignment.Near;
        break;
      case CompassPoint.West:
      case CompassPoint.SouthWest:
        format.LineAlignment = XLineAlignment.Near;
        format.Alignment = XStringAlignment.Far;
        break;
      case CompassPoint.NorthWest:
      case CompassPoint.NorthNorthWest:
        format.LineAlignment = XLineAlignment.Far;
        format.Alignment = XStringAlignment.Far;
        break;
      default:
        return false;
    }

    return true;
  }

  public static PointF Subtract(PointF a, PointF b)
  {
    return new PointF(a.X - b.X, a.Y - b.Y);
  }

  public static Rectangle ToRectangle(RectangleF rect)
  {
    return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
  }

  private static Cursor LoadCursor(byte[] bytes)
  {
    using var stream = new MemoryStream(bytes);
    return new Cursor(stream);
  }
}