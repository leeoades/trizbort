using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using PdfSharp.Drawing;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Extensions;
using Trizbort.Setup;
using Trizbort.UI;

namespace Trizbort.Domain.Elements;

public static class MapLabelDrawingExtensions
{
  private static readonly ConditionalWeakTable<MapLabel, TextBlock> _textBlocks = new();

  private static TextBlock GetTextBlock(MapLabel label)
  {
    var tb = _textBlocks.GetOrCreateValue(label);
    if (tb.Text != label.Text)
    {
      tb.Text = label.Text;
    }
    return tb;
  }

  public static void Draw(this MapLabel label, XGraphics graphics, Palette palette, DrawingContext context)
  {
    if (label.Width <= 0 || label.Height <= 0) return;
    var path = label.CreatePath(palette);
    if (label.HasBackground) graphics.DrawPath(palette.Brush(label.BackgroundColor), path);
    if (label.BorderStyle != BorderDashStyle.None)
    {
      var pen = palette.Pen(label.BorderColor);
      pen.DashStyle = label.BorderStyle.ConvertToDashStyle();
      graphics.DrawPath(pen, path);
    }

    if (context.Selected || context.Hover)
      graphics.DrawPath(context.Selected ? palette.SelectedLinePen : palette.HoverLinePen, path);

    var bounds = label.InnerBounds;
    bounds.Inflate(label.Shape == RoomShape.Ellipse ? -11.5f : -5);
    if (bounds.Width > 0 && bounds.Height > 0)
    {
      var textBlock = GetTextBlock(label);
      textBlock.Draw(
        graphics,
        Settings.RoomNameFont,
        palette.Brush(label.TextColor),
        bounds.Position,
        bounds.Size,
        XStringFormats.Center);
    }
  }

  public static XGraphicsPath CreatePath(this MapLabel label, Palette palette)
  {
    var path = palette.Path();
    if (Settings.HandDrawn)
    {
      path.AddPolygon(label.CreateSketchOutline());
      return path;
    }

    if (label.Shape == RoomShape.Ellipse)
    {
      path.AddEllipse(label.InnerBounds.ToRectangleF());
    }
    else if (label.Shape == RoomShape.RoundedCorners)
    {
      var diameter = Math.Min(30, Math.Min(label.Width, label.Height));
      path.AddArc(label.X, label.Y, diameter, diameter, 180, 90);
      path.AddArc(label.X + label.Width - diameter, label.Y, diameter, diameter, 270, 90);
      path.AddArc(label.X + label.Width - diameter, label.Y + label.Height - diameter, diameter, diameter, 0, 90);
      path.AddArc(label.X, label.Y + label.Height - diameter, diameter, diameter, 90, 90);
      path.CloseFigure();
    }
    else if (label.Shape == RoomShape.Octagonal)
    {
      path.AddPolygon(
        new[] {
          new PointF(label.X + label.Width / 4, label.Y), new PointF(label.X + label.Width * 3 / 4, label.Y),
          new PointF(label.X + label.Width, label.Y + label.Height / 4), new PointF(label.X + label.Width, label.Y + label.Height * 3 / 4),
          new PointF(label.X + label.Width * 3 / 4, label.Y + label.Height), new PointF(label.X + label.Width / 4, label.Y + label.Height),
          new PointF(label.X, label.Y + label.Height * 3 / 4), new PointF(label.X, label.Y + label.Height / 4)
        });
    }
    else
    {
      path.AddRectangle(label.InnerBounds.ToRectangleF());
    }

    return path;
  }

  public static PointF[] CreateSketchOutline(this MapLabel label)
  {
    var rect = label.InnerBounds.ToRectangleF();
    var random = Sketch.Seeded(label.Id);
    if (label.Shape == RoomShape.Ellipse)
      return Sketch.ClosedCurve(Sketch.Ellipse(rect), random);
    if (label.Shape == RoomShape.RoundedCorners)
    {
      var radius = Math.Min(30, Math.Min(label.Width, label.Height)) / 2;
      return Sketch.ClosedCurve(Sketch.RoundedRectangle(rect, radius, radius, radius, radius), random);
    }

    if (label.Shape == RoomShape.Octagonal)
      return Sketch.Polygon(
        new[] {
          new PointF(label.X + label.Width / 4, label.Y), new PointF(label.X + label.Width * 3 / 4, label.Y),
          new PointF(label.X + label.Width, label.Y + label.Height / 4), new PointF(label.X + label.Width, label.Y + label.Height * 3 / 4),
          new PointF(label.X + label.Width * 3 / 4, label.Y + label.Height), new PointF(label.X + label.Width / 4, label.Y + label.Height),
          new PointF(label.X, label.Y + label.Height * 3 / 4), new PointF(label.X, label.Y + label.Height / 4)
        },
        random);
    return Sketch.Polygon(
      new[] {
        new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top),
        new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom)
      },
      random);
  }

  public static void ShowDialog(this MapLabel label)
  {
    using var dialog = new LabelPropertiesDialog(label);
    UserInteraction.ShowDialog(dialog, Program.MainForm);
  }
}
