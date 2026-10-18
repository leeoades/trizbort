using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using PdfSharp.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Misc;
using Trizbort.Properties;
using Trizbort.UI;
using Settings = Trizbort.Setup.Settings;

namespace Trizbort.Domain.Elements;

public static class ConnectionDrawingExtensions
{
  private class ConnectionTextBlocks
  {
    public TextBlock StartText { get; } = new();
    public TextBlock MidText { get; } = new();
    public TextBlock EndText { get; } = new();
    public List<LineSegment> SmartSegments { get; } = new();
  }

  private static readonly ConditionalWeakTable<Connection, ConnectionTextBlocks> _stateTable = new();

  private static ConnectionTextBlocks GetState(Connection connection)
  {
    var state = _stateTable.GetOrCreateValue(connection);
    if (state.StartText.Text != connection.StartText) state.StartText.Text = connection.StartText;
    if (state.MidText.Text != connection.MidText) state.MidText.Text = connection.MidText;
    if (state.EndText.Text != connection.EndText) state.EndText.Text = connection.EndText;
    return state;
  }

  public static void ShowDialog(this Connection connection)
  {
    using var dialog = new ConnectionPropertiesDialog();
    dialog.ConnectionName = connection.Name;
    dialog.ConnectionDescription = connection.Description;
    dialog.IsDotted = connection.Style == ConnectionStyle.Dashed;
    dialog.IsDirectional = connection.Flow == ConnectionFlow.OneWay;
    dialog.StartText = connection.StartText;
    dialog.MidText = connection.MidText;
    dialog.EndText = connection.EndText;
    dialog.ConnectionColor = connection.ConnectionColor;
    dialog.Door = connection.Door;
    if (UserInteraction.ShowDialog(dialog, TrizbortApplication.MainForm?.Canvas) == DialogResult.OK)
    {
      connection.Name = dialog.ConnectionName;
      connection.Description = dialog.ConnectionDescription;
      connection.Style = dialog.IsDotted ? ConnectionStyle.Dashed : ConnectionStyle.Solid;
      connection.Flow = dialog.IsDirectional ? ConnectionFlow.OneWay : ConnectionFlow.TwoWay;
      connection.ConnectionColor = dialog.ConnectionColor;
      connection.StartText = dialog.StartText;
      connection.MidText = dialog.MidText;
      connection.EndText = dialog.EndText;
      connection.Door = dialog.Door;
    }
  }

  public static void RecomputeSmartLineSegments(this Connection connection, DrawingContext context)
  {
    var state = GetState(connection);
    state.SmartSegments.Clear();
    foreach (var lineSegment in connection.GetSegments())
    {
      List<LineSegment> newSegments = null;
      if (Split(lineSegment, context, ref newSegments))
        foreach (var newSegment in newSegments)
          state.SmartSegments.Add(newSegment);
      else
        state.SmartSegments.Add(lineSegment);
    }

    foreach (var segment in state.SmartSegments)
      context.LinesDrawn.Add(segment);
  }

  public static void Draw(this Connection connection, XGraphics graphics, Palette palette, DrawingContext context)
  {
    var pen = palette.Pen(
      connection.ConnectionColor != Color.Transparent ? connection.ConnectionColor : Settings.Color[Colors.Line],
      Settings.LineWidth);
    if (connection.Style == ConnectionStyle.Dashed)
      pen.DashStyle = DashStyle.Custom;
    pen.DashPattern = new[] { 2.0f, 2.0f };

    var lineSegments = context.UseSmartLineSegments ? GetState(connection).SmartSegments : connection.GetSegments();
    var brush = palette.Brush(
      connection.ConnectionColor != Color.Transparent ? connection.ConnectionColor : Settings.Color[Colors.Line]);

    if (!ApplicationSettingsController.AppSettings.DebugDisableLineRendering)
    {
      if (connection.HasCurveWaypoints)
      {
        var path = palette.Path();
        path.AddLines(connection.GetSegments().Select(segment => segment.Start.ToPointF()).ToArray());
        graphics.DrawPath(pen, path);
      }
      else
      {
        foreach (var lineSegment in lineSegments)
        {
          var customDraw = false;
          if (lineSegment.Start.X != lineSegment.End.X && lineSegment.Start.Y != lineSegment.End.Y)
            if (lineSegment.Start.X == lineSegment.End.X + 1 ||
                lineSegment.Start.X == lineSegment.End.X - 1 ||
                lineSegment.Start.Y == lineSegment.End.Y + 1 ||
                lineSegment.Start.Y == lineSegment.End.Y - 1)
            {
              graphics.DrawLine(pen, lineSegment.Start.X, lineSegment.Start.Y, lineSegment.End.X, lineSegment.End.Y);
              customDraw = true;
            }

          if (!customDraw)
            DrawSmartLine(connection, graphics, palette, context, lineSegment, pen);
        }
      }

      if (context.Selected)
        foreach (var lineSegment in lineSegments)
          graphics.DrawLine(palette.SelectedLinePen, lineSegment.Start.ToPointF(), lineSegment.End.ToPointF());

      if (context.Hover)
        foreach (var lineSegment in lineSegments)
          graphics.DrawLine(palette.HoverLinePen, lineSegment.Start.ToPointF(), lineSegment.End.ToPointF());

      if (connection.Flow == ConnectionFlow.OneWay && lineSegments.Count > 0)
      {
        var lineSegment = lineSegments[lineSegments.Count - 1];
        var angle = (float)Math.Atan2(lineSegment.Delta.Y, lineSegment.Delta.X);
        var arrowSize = (float)Math.Max(8.0, 2.0 * Settings.LineWidth);
        Drawing.DrawChevron(
          graphics,
          lineSegment.End.ToPointF(),
          angle * 180 / (float)Math.PI + 90,
          arrowSize,
          brush,
          Settings.HandDrawn ? Sketch.Seeded(connection.Id) : null);
      }

      if (connection.Door != null && lineSegments.Count > 0)
      {
        var lineSegment = lineSegments[lineSegments.Count / 2];
        ShowDoorIcons(connection, graphics, lineSegment);
      }
    }

    Annotate(connection, graphics, palette, lineSegments);
  }

  private static void DrawSmartLine(
    Connection connection,
    XGraphics graphics,
    Palette palette,
    DrawingContext context,
    LineSegment lineSegment,
    Pen pen)
  {
    if (Settings.HandDrawn)
    {
      var path = palette.Path();
      Drawing.AddLine(path, lineSegment, Sketch.Seeded(connection.Id), false);
      graphics.DrawPath(pen, path);
    }
    else
    {
      graphics.DrawLine(pen, lineSegment.Start.ToPointF(), lineSegment.End.ToPointF());
    }
  }

  private static void ShowDoorIcons(Connection connection, XGraphics graphics, LineSegment lineSegment)
  {
    var door = connection.Door;
    if (door == null) return;
    var doorIcon = door.Open ? new Bitmap(Resources.Door_Open) : new Bitmap(Resources.Door);
    var doorLock = door.Locked ? new Bitmap(Resources.Lock) : new Bitmap(Resources.Unlocked);
    var iconBlock1 = new ConnectionIconBlock(lineSegment, 0) { Image = doorIcon };
    var iconBlock2 = new ConnectionIconBlock(lineSegment, 1) { Image = doorLock };
    iconBlock1.DrawBlock(graphics);
    iconBlock2.DrawBlock(graphics);
  }

  private static void Annotate(Connection connection, XGraphics graphics, Palette palette, List<LineSegment> lineSegments)
  {
    if (lineSegments.Count == 0)
      return;

    var state = GetState(connection);

    if (!string.IsNullOrEmpty(connection.StartText))
      Annotate(connection, graphics, palette, lineSegments[0], state.StartText, StringAlignment.Near);

    if (!string.IsNullOrEmpty(connection.EndText))
      Annotate(connection, graphics, palette, lineSegments[lineSegments.Count - 1], state.EndText, StringAlignment.Far);

    if (!string.IsNullOrEmpty(connection.MidText))
    {
      var totalLength = lineSegments.Sum(lineSegment => lineSegment.Length);
      var middle = totalLength / 2;
      foreach (var lineSegment in lineSegments)
      {
        var length = lineSegment.Length;
        if (middle > length)
        {
          middle -= length;
        }
        else
        {
          middle /= length;
          var pos = lineSegment.Start + lineSegment.Delta * middle;
          var fakeSegment = new LineSegment(
            pos - lineSegment.Delta * Numeric.Small,
            pos + lineSegment.Delta * Numeric.Small);
          Annotate(connection, graphics, palette, fakeSegment, state.MidText, StringAlignment.Center);
          break;
        }
      }
    }
  }

  private static void Annotate(
    Connection connection,
    XGraphics graphics,
    Palette palette,
    LineSegment lineSegment,
    TextBlock text,
    StringAlignment alignment)
  {
    Vector point;
    var delta = lineSegment.Delta;
    var roomTypeAdjustments = Vector.Zero;

    RoomShape roomType;
    switch (alignment)
    {
      default:
        // detached vertex has no port, so need to check if it exists
        if (connection.VertexList.Count > 0 && connection.VertexList[0].Port != null)
        {
          roomType = connection.VertexList[0].Port.Owner.GetRoomType();
          if (roomType == RoomShape.Ellipse || roomType == RoomShape.Octagonal)
            roomTypeAdjustments = RoomTypeAdjustments(connection.VertexList[0]);
        }

        point = lineSegment.Start + roomTypeAdjustments;
        delta.Negate();
        break;
      case StringAlignment.Center:
        point = lineSegment.Mid;
        break;
      case StringAlignment.Far:
        // detached vertex has no port, so need to check if it exists
        if (connection.VertexList.Count > 1 && connection.VertexList[connection.VertexList.Count - 1].Port != null)
        {
          roomType = connection.VertexList[connection.VertexList.Count - 1].Port.Owner.GetRoomType();
          if (roomType == RoomShape.Ellipse || roomType == RoomShape.Octagonal)
            roomTypeAdjustments = RoomTypeAdjustments(connection.VertexList[connection.VertexList.Count - 1]);
        }

        point = lineSegment.End + roomTypeAdjustments;
        break;
    }

    var bounds = new Rect(point, Vector.Zero);
    bounds.Inflate(Settings.TextOffsetFromConnection);

    var compassPoint = CompassPointHelper.DirectionFromAngle(out var angle, delta);

    var pos = bounds.GetCorner(compassPoint);
    var format = new XStringFormat();
    Drawing.SetAlignmentFromCardinalOrOrdinalDirection(format, compassPoint);

    if (alignment == StringAlignment.Center && Math.Abs(angle) == 90)
    {
      // HACK: Adjust the anchoring for mid-line text for vertical lines to push
      // the start of the label a bit off the connection line.
      pos = bounds.GetCorner(CompassPoint.East);
      format.LineAlignment = XLineAlignment.Center;
    }

    if (alignment == StringAlignment.Center && Numeric.InRange(angle, -10, 10))
    {
      // HACK: if the line segment is pretty horizontal and we're drawing mid-line text,
      // move text below the line to get it out of the way of any labels at the ends,
      // and center the text so it fits onto a line between two proximal rooms.
      pos = bounds.GetCorner(CompassPoint.South);
      format.Alignment = XStringAlignment.Center;
      format.LineAlignment = XLineAlignment.Near;
    }

    if (!ApplicationSettingsController.AppSettings.DebugDisableTextRendering)
      text.Draw(graphics, Settings.LineFont, palette.LineTextBrush, pos, Vector.Zero, format);
  }

  private static Vector RoomTypeAdjustments(Vertex vertex)
  {
    var roomTypeAdjustments = Vector.Zero;
    CompassPointHelper.FromName(vertex.Port.Id, out var dir);
    switch (dir)
    {
      case CompassPoint.SouthEast:
        roomTypeAdjustments = new Vector(8, 6);
        break;
      case CompassPoint.NorthEast:
        roomTypeAdjustments = new Vector(10, -6);
        break;
      case CompassPoint.EastNorthEast:
      case CompassPoint.EastSouthEast:
        roomTypeAdjustments = new Vector(4, 0);
        break;
      case CompassPoint.NorthWest:
        roomTypeAdjustments = new Vector(-10, -4);
        break;
      case CompassPoint.SouthWest:
        roomTypeAdjustments = new Vector(-10, 4);
        break;
      case CompassPoint.WestSouthWest:
        roomTypeAdjustments = new Vector(-10, 0);
        break;
      case CompassPoint.WestNorthWest:
        roomTypeAdjustments = new Vector(-10, -4);
        break;
    }

    return roomTypeAdjustments;
  }

  private static bool Split(LineSegment lineSegment, DrawingContext context, ref List<LineSegment> newSegments)
  {
    foreach (var previousSegment in context.LinesDrawn)
    {
      var amount = Math.Max(1, Settings.LineWidth) * 3;
      if (lineSegment.Intersect(previousSegment, true, out var intersects))
        foreach (var intersect in intersects)
        {
          switch (intersect.Type)
          {
            case LineSegmentIntersectType.MidPointA:
              var one = new LineSegment(lineSegment.Start, intersect.Position);
              if (one.Shorten(amount))
                if (!Split(one, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(one);
                }

              var two = new LineSegment(intersect.Position, lineSegment.End);
              if (two.Forshorten(amount))
                if (!Split(two, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(two);
                }

              break;

            case LineSegmentIntersectType.StartA:
              if (lineSegment.Forshorten(amount))
                if (!Split(lineSegment, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(lineSegment);
                }

              break;

            case LineSegmentIntersectType.EndA:
              if (lineSegment.Shorten(amount))
                if (!Split(lineSegment, context, ref newSegments))
                {
                  if (newSegments == null)
                    newSegments = new List<LineSegment>();
                  newSegments.Add(lineSegment);
                }

              break;
          }

          return newSegments != null;
        }
    }

    return false;
  }
}
