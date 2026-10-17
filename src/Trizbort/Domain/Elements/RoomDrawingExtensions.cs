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
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Extensions;
using Trizbort.Properties;
using Trizbort.Setup;
using Trizbort.UI;
using Trizbort.UI.Controls;
using Trizbort.Util;

namespace Trizbort.Domain.Elements;

public static class RoomDrawingExtensions
{
  internal class RoomTextBlocks
  {
    public TextBlock Name { get; } = new();
    public TextBlock SubTitle { get; } = new();
    public TextBlock Objects { get; } = new();
    public TextBlock ObjectsDisplay { get; } = new();

    public RoomTextBlocks(Room room)
    {
      room.NameLayoutInvalidated += (s, e) => Name.InvalidateLayout();
      room.SubTitleLayoutInvalidated += (s, e) => SubTitle.InvalidateLayout();
      room.ObjectsLayoutInvalidated += (s, e) =>
      {
        Objects.InvalidateLayout();
        ObjectsDisplay.InvalidateLayout();
      };
    }
  }

  private static readonly ConditionalWeakTable<Room, RoomTextBlocks> _stateTable = new();

  internal static RoomTextBlocks GetTextBlocks(Room room)
  {
    var tb = _stateTable.GetOrCreateValue(room);
    if (tb.Name.Text != room.Name) tb.Name.Text = room.Name;
    if (tb.SubTitle.Text != room.SubTitle) tb.SubTitle.Text = room.SubTitle;
    if (tb.Objects.Text != room.Objects) tb.Objects.Text = room.Objects;
    if (tb.ObjectsDisplay.Text != room.Objects) tb.ObjectsDisplay.Text = room.Objects;
    return tb;
  }

  public static void PreDraw(this Room room, DrawingContext context)
  {
    var topLeft = room.InnerBounds.GetCorner(CompassPoint.NorthWest);
    var topRight = room.InnerBounds.GetCorner(CompassPoint.NorthEast);
    var bottomLeft = room.InnerBounds.GetCorner(CompassPoint.SouthWest);
    var bottomRight = room.InnerBounds.GetCorner(CompassPoint.SouthEast);

    context.LinesDrawn.Add(new LineSegment(topLeft, topRight));
    context.LinesDrawn.Add(new LineSegment(topRight, bottomRight));
    context.LinesDrawn.Add(new LineSegment(bottomRight, bottomLeft));
    context.LinesDrawn.Add(new LineSegment(bottomLeft, topLeft));
  }

  public static void Draw(this Room room, XGraphics graphics, Palette palette, DrawingContext context)
  {
    var handDrawn = room.IsHandDrawn;
    room.StraightEdges = !handDrawn;

    var random = Sketch.Seeded(room.Id + 1000);

    var topLeft = room.InnerBounds.GetCorner(CompassPoint.NorthWest);
    var topRight = room.InnerBounds.GetCorner(CompassPoint.NorthEast);
    var bottomLeft = room.InnerBounds.GetCorner(CompassPoint.SouthWest);
    var bottomRight = room.InnerBounds.GetCorner(CompassPoint.SouthEast);

    var topCenter = room.InnerBounds.GetCorner(CompassPoint.North);
    var rightCenter = room.InnerBounds.GetCorner(CompassPoint.East);
    var bottomCenter = room.InnerBounds.GetCorner(CompassPoint.South);
    var leftCenter = room.InnerBounds.GetCorner(CompassPoint.West);

    var top = new LineSegment(topLeft, topRight);
    var right = new LineSegment(topRight, bottomRight);
    var bottom = new LineSegment(bottomRight, bottomLeft);
    var left = new LineSegment(bottomLeft, topLeft);

    var halfTopRight = new LineSegment(topCenter, topRight);
    var halfTopLeft = new LineSegment(topCenter, topLeft);
    var halfBottomRight = new LineSegment(bottomRight, bottomCenter);
    var halfBottomLeft = new LineSegment(bottomLeft, bottomCenter);
    var centerVertical = new LineSegment(bottomCenter, topCenter);

    var centerHorizontal = new LineSegment(leftCenter, rightCenter);
    var halfRightBottom = new LineSegment(rightCenter, bottomRight);
    var halfLeftBottom = new LineSegment(bottomLeft, leftCenter);
    var halfRightTop = new LineSegment(rightCenter, topRight);
    var halfLeftTop = new LineSegment(topLeft, leftCenter);

    var slantUp = new LineSegment(bottomLeft, topRight);
    var slantDown = new LineSegment(bottomRight, topLeft);

    context.LinesDrawn.Add(top);
    context.LinesDrawn.Add(right);
    context.LinesDrawn.Add(bottom);
    context.LinesDrawn.Add(left);

    if (room.IsStartRoom || room.IsEndRoom || room.IsReference)
    {
      var tBounds = room.InnerBounds;
      tBounds.Inflate(5);

      var pathSelected = palette.Path();
      room.AddOutline(pathSelected, tBounds, handDrawn);

      SolidBrush brushSelected;
      if (room.IsReference)
        brushSelected = new SolidBrush(Color.LightBlue);
      else
        brushSelected = new SolidBrush(Settings.Color[room.IsStartRoom ? Colors.StartRoom : Colors.EndRoom]);
      graphics.DrawPath(brushSelected, pathSelected);
    }

    if (context.Selected)
    {
      var tBounds = room.InnerBounds;
      tBounds.Inflate(Project.Current.ActiveSelectedElement?.Id == room.Id ? 10 : 5);

      var pathSelected = palette.Path();
      room.AddOutline(pathSelected, tBounds, handDrawn);
      graphics.DrawPath(palette.Brush(Color.FromArgb(120, 255, 255, 0)), pathSelected);
    }

    var path = palette.Path();
    room.AddOutline(path, room.InnerBounds, handDrawn);

    if (room.RoomFillColor != Color.Transparent)
    {
      graphics.DrawPath(palette.Brush(room.RoomFillColor), path);
    }
    else
    {
      var region = Settings.Regions.FirstOrDefault(p => p.RegionName == room.Region);
      if (region?.RegionName != Misc.Region.DefaultRegion)
      {
        if (region != null && region.RColor != Color.Transparent)
          graphics.DrawPath(palette.Brush(region.RColor), path);
      }
      else
      {
        graphics.DrawPath(palette.Brush(Settings.Color[Colors.Canvas]), path);
      }
    }

    if (room.SecondFillColor != Color.Transparent)
    {
      var state = graphics.Save();
      graphics.IntersectClip(path);
      var pathSecondFill = palette.Path();
      switch (room.SecondFillLocation)
      {
        case "Bottom":
          pathSecondFill.AddPolygon(new[] {
            leftCenter.Start.ToPointF(),
            rightCenter.Start.ToPointF(),
            bottomRight.Start.ToPointF(),
            bottomLeft.Start.ToPointF()
          });
          break;
        case "BottomRight":
          pathSecondFill.AddPolygon(new[] {
            topRight.Start.ToPointF(),
            bottomLeft.Start.ToPointF(),
            bottomRight.Start.ToPointF()
          });
          break;
        case "TopRight":
          pathSecondFill.AddPolygon(new[] {
            topLeft.Start.ToPointF(),
            topRight.Start.ToPointF(),
            bottomRight.Start.ToPointF()
          });
          break;
        case "Right":
          pathSecondFill.AddPolygon(new[] {
            topCenter.Start.ToPointF(),
            topRight.Start.ToPointF(),
            bottomRight.Start.ToPointF(),
            bottomCenter.Start.ToPointF()
          });
          break;
      }

      graphics.DrawPath(palette.Brush(room.SecondFillColor), pathSecondFill);
      graphics.Restore(state);
    }

    if (!Settings.DebugDisableLineRendering)
    {
      var roomBorderPen = palette.Pen(
        room.RoomBorderColor != Color.Transparent ? room.RoomBorderColor : Settings.Color[Colors.Border],
        Settings.LineWidth);
      roomBorderPen.DashStyle = room.BorderStyle.ConvertToDashStyle();
      graphics.DrawPath(roomBorderPen, path);
    }

    var textBlocks = GetTextBlocks(room);
    room.DrawText(graphics, palette, textBlocks);
    room.DrawObjects(graphics, palette, textBlocks);
  }

  public static void AddOutline(this Room room, XGraphicsPath path, Rect bounds, bool handDrawn)
  {
    var rect = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    var random = Sketch.Seeded(room.Id);

    if (room.Shape == RoomShape.RoundedCorners)
    {
      if (handDrawn)
      {
        path.AddPolygon(Sketch.ClosedCurve(
          Sketch.RoundedRectangle(
            rect,
            room.Corners.TopLeft,
            room.Corners.TopRight,
            room.Corners.BottomRight,
            room.Corners.BottomLeft),
          random));
        return;
      }

      var topLeftCorner = bounds.GetCorner(CompassPoint.NorthWest);
      var topRightCorner = bounds.GetCorner(CompassPoint.NorthEast);
      var bottomLeftCorner = bounds.GetCorner(CompassPoint.SouthWest);

      var topSeg = new LineSegment(topLeftCorner, topRightCorner);
      var leftSeg = new LineSegment(bottomLeftCorner, topLeftCorner);

      room.CreateRoomPath(path, topSeg, leftSeg);
      return;
    }

    if (room.Shape == RoomShape.Ellipse)
    {
      if (handDrawn)
      {
        path.AddPolygon(Sketch.ClosedCurve(Sketch.Ellipse(rect), random));
        return;
      }

      path.AddEllipse(rect);
      return;
    }

    PointF[] vertices;
    if (room.Octagonal)
    {
      var qw = rect.Width / 4;
      var qh = rect.Height / 4;
      vertices = new[] {
        new PointF(rect.Left, rect.Bottom - qh), new PointF(rect.Left, rect.Top + qh),
        new PointF(rect.Left + qw, rect.Top), new PointF(rect.Right - qw, rect.Top),
        new PointF(rect.Right, rect.Top + qh), new PointF(rect.Right, rect.Bottom - qh),
        new PointF(rect.Right - qw, rect.Bottom), new PointF(rect.Left + qw, rect.Bottom)
      };
    }
    else
    {
      vertices = new[] {
        new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top),
        new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom)
      };
    }

    path.AddPolygon(handDrawn ? Sketch.Polygon(vertices, random) : vertices);
  }

  private static void CreateRoomPath(this Room room, XGraphicsPath path, LineSegment top, LineSegment left)
  {
    path.AddArc(
      top.Start.X + top.Length - room.Corners.TopRight * 2,
      top.Start.Y,
      room.Corners.TopRight * 2,
      room.Corners.TopRight * 2,
      270,
      90);
    path.AddArc(
      top.Start.X + top.Length - room.Corners.BottomRight * 2,
      top.Start.Y + left.Length - room.Corners.BottomRight * 2,
      room.Corners.BottomRight * 2,
      room.Corners.BottomRight * 2,
      0,
      90);
    path.AddArc(
      top.Start.X,
      top.Start.Y + left.Length - room.Corners.BottomLeft * 2,
      room.Corners.BottomLeft * 2,
      room.Corners.BottomLeft * 2,
      90,
      90);
    path.AddArc(top.Start.X, top.Start.Y, room.Corners.TopLeft * 2, room.Corners.TopLeft * 2, 180, 90);
    path.CloseFigure();
  }

  private static void DrawText(this Room room, XGraphics graphics, Palette palette, RoomTextBlocks textBlocks)
  {
    var name = room.Name;
    if (string.IsNullOrEmpty(name)) return;

    var font = Settings.RoomNameFont;
    var brush = palette.Brush(room.RoomNameColor != Color.Transparent ? room.RoomNameColor : Settings.Color[Colors.SmallText]);

    var bounds = room.InnerBounds;
    bounds.Inflate(-Settings.TextPadding);

    var textPosition = bounds.Position;
    var textSize = bounds.Size;

    if (!string.IsNullOrEmpty(room.SubTitle))
    {
      room.DrawSubTitle(graphics, palette, textBlocks, ref bounds);
      textPosition = bounds.Position;
      textSize = bounds.Size;
    }

    if (textSize.X > 0 && textSize.Y > 0)
    {
      textBlocks.Name.Draw(graphics, font, brush, textPosition, textSize, XStringFormats.Center);
    }
  }

  private static void DrawSubTitle(this Room room, XGraphics graphics, Palette palette, RoomTextBlocks textBlocks, ref Rect bounds)
  {
    var font = Settings.SubtitleFont;
    var brush = palette.Brush(room.RoomSubtitleColor != Color.Transparent ? room.RoomSubtitleColor : Settings.Color[Colors.Subtitle]);

    var subTitleSize = textBlocks.SubTitle.Measure(graphics, font);
    var subTitlePos = new Vector(bounds.X + (bounds.Width - subTitleSize.X) / 2, bounds.Bottom - subTitleSize.Y);

    textBlocks.SubTitle.Draw(graphics, font, brush, subTitlePos, subTitleSize, XStringFormats.Center);
    bounds.Height -= subTitleSize.Y;
  }

  private static void DrawObjects(this Room room, XGraphics graphics, Palette palette, RoomTextBlocks textBlocks)
  {
    var objects = room.Objects;
    if (string.IsNullOrEmpty(objects)) return;

    var font = Settings.ObjectFont;
    var brush = palette.Brush(room.RoomObjectTextColor != Color.Transparent ? room.RoomObjectTextColor : Settings.Color[Colors.SmallText]);

    var bounds = room.InnerBounds;
    var objSize = textBlocks.ObjectsDisplay.Measure(graphics, font);
    Vector objPos;

    if (room.ObjectsCustomPosition)
    {
      objPos = new Vector(bounds.X + room.ObjectsCustomPositionRight, bounds.Y + room.ObjectsCustomPositionDown);
    }
    else
    {
      switch (room.ObjectsPosition)
      {
        case CompassPoint.North:
          objPos = new Vector(bounds.X + (bounds.Width - objSize.X) / 2, bounds.Top - objSize.Y - Settings.TextPadding);
          break;
        case CompassPoint.NorthEast:
          objPos = new Vector(bounds.Right + Settings.TextPadding, bounds.Top - objSize.Y);
          break;
        case CompassPoint.East:
          objPos = new Vector(bounds.Right + Settings.TextPadding, bounds.Y + (bounds.Height - objSize.Y) / 2);
          break;
        case CompassPoint.SouthEast:
          objPos = new Vector(bounds.Right + Settings.TextPadding, bounds.Bottom);
          break;
        case CompassPoint.South:
        default:
          objPos = new Vector(bounds.X + (bounds.Width - objSize.X) / 2, bounds.Bottom + Settings.TextPadding);
          break;
        case CompassPoint.SouthWest:
          objPos = new Vector(bounds.Left - objSize.X - Settings.TextPadding, bounds.Bottom);
          break;
        case CompassPoint.West:
          objPos = new Vector(bounds.Left - objSize.X - Settings.TextPadding, bounds.Y + (bounds.Height - objSize.Y) / 2);
          break;
        case CompassPoint.NorthWest:
          objPos = new Vector(bounds.Left - objSize.X - Settings.TextPadding, bounds.Top - objSize.Y);
          break;
      }
    }

    textBlocks.ObjectsDisplay.Draw(graphics, font, brush, objPos, objSize, XStringFormats.Center);
  }

  public static void ShowDialog(this Room room, PropertiesStartType startPoint)
  {
    room.ShowRoomDialog(startPoint);
  }

  public static void ShowDialog(this Room room)
  {
    room.ShowRoomDialog(PropertiesStartType.Objects);
  }

  public static void ShowRoomDialog(this Room room, PropertiesStartType start = PropertiesStartType.Objects)
  {
    var dialog = new RoomPropertiesDialog(start, room.Id);
    var result = DialogResult.None;
    room.ShowRoomDialog(ref dialog, ref result, start);
  }

  public static void ShowRoomDialog(
    this Room room,
    ref RoomPropertiesDialog dialog,
    ref DialogResult result,
    PropertiesStartType start = PropertiesStartType.Objects)
  {
    dialog.RoomName = room.Name;
    dialog.Description = room.PrimaryDescription;
    dialog.RoomSubTitle = room.SubTitle;
    dialog.IsDark = room.IsDark;
    dialog.IsStartRoom = room.IsStartRoom;
    dialog.IsEndRoom = room.IsEndRoom;
    dialog.HandDrawnStyle = room.HandDrawnStyle;
    dialog.Objects = room.Objects;
    dialog.ObjectsPosition = room.ObjectsPosition;
    dialog.ObjectsCustomPosition = room.ObjectsCustomPosition;
    dialog.ObjectsCustomPositionDown = room.ObjectsCustomPositionDown;
    dialog.ObjectsCustomPositionRight = room.ObjectsCustomPositionRight;
    dialog.BorderStyle = room.BorderStyle;

    dialog.RoomFillColor = room.RoomFillColor;
    dialog.SecondFillColor = room.SecondFillColor;
    dialog.SecondFillLocation = room.SecondFillLocation;
    dialog.RoomBorderColor = room.RoomBorderColor;
    dialog.RoomNameColor = room.RoomNameColor;
    dialog.RoomSubtitleColor = room.RoomSubtitleColor;
    dialog.RoomObjectTextColor = room.RoomObjectTextColor;

    dialog.Shape = room.Shape;
    dialog.Corners = room.Corners;
    dialog.AllCornersEqual = room.AllCornersEqual;

    dialog.Region = room.Region;

    result = UserInteraction.ShowDialog(dialog, TrizbortApplication.MainForm?.Canvas);
    if (result == DialogResult.OK)
    {
      room.UpdateFromRoomDialog(dialog);
    }
  }

  public static void UpdateFromRoomDialog(this Room room, RoomPropertiesDialog dialog)
  {
    room.Name = dialog.RoomName;
    room.ClearDescriptions();
    room.AddDescription(dialog.Description);
    room.SubTitle = dialog.RoomSubTitle;
    room.IsDark = dialog.IsDark;
    room.IsStartRoom = dialog.IsStartRoom;
    room.IsEndRoom = dialog.IsEndRoom;
    room.HandDrawnStyle = dialog.HandDrawnStyle;
    room.Objects = dialog.Objects;
    room.ObjectsPosition = dialog.ObjectsPosition;
    room.ObjectsCustomPosition = dialog.ObjectsCustomPosition;
    room.ObjectsCustomPositionDown = dialog.ObjectsCustomPositionDown;
    room.ObjectsCustomPositionRight = dialog.ObjectsCustomPositionRight;
    room.BorderStyle = dialog.BorderStyle;

    room.RoomFillColor = dialog.RoomFillColor;
    room.SecondFillColor = dialog.SecondFillColor;
    room.SecondFillLocation = dialog.SecondFillLocation;
    room.RoomBorderColor = dialog.RoomBorderColor;
    room.RoomNameColor = dialog.RoomNameColor;
    room.RoomSubtitleColor = dialog.RoomSubtitleColor;
    room.RoomObjectTextColor = dialog.RoomObjectTextColor;

    room.ChangeRoomShape(dialog.Shape);
    room.Corners = dialog.Corners;
    room.AllCornersEqual = dialog.AllCornersEqual;

    room.Region = dialog.Region;
  }
}
