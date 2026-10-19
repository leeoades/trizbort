using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using PdfSharp.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Misc;
using Trizbort.Extensions;
using Trizbort.UI;
using Settings = Trizbort.Setup.Settings;

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
    var tb = _stateTable.GetValue(room, key => new RoomTextBlocks(key));
    if (tb.Name.Text != room.Name) tb.Name.Text = room.Name;
    if (tb.SubTitle.Text != room.SubTitle) tb.SubTitle.Text = room.SubTitle;
    if (tb.Objects.Text != room.Objects) tb.Objects.Text = room.Objects;
    if (tb.ObjectsDisplay.Text != room.Objects) tb.ObjectsDisplay.Text = room.Objects;
    return tb;
  }

  public static void PreDraw(this Room room, DrawingContext context)
  {
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

    graphics.DrawPath(palette.Brush(room.ResolveRoomFillColor()), path);

    if (room.SecondFillColor != Color.Transparent)
    {
      var state = graphics.Save();
      graphics.IntersectClip(path);
      var pathSecondFill = palette.Path();
      switch (room.SecondFillLocation)
      {
        case "Bottom":
          pathSecondFill.AddPolygon(new[] {
            leftCenter.ToPointF(),
            rightCenter.ToPointF(),
            bottomRight.ToPointF(),
            bottomLeft.ToPointF()
          });
          break;
        case "BottomRight":
          pathSecondFill.AddPolygon(new[] {
            topRight.ToPointF(),
            bottomLeft.ToPointF(),
            bottomRight.ToPointF()
          });
          break;
        case "TopRight":
          pathSecondFill.AddPolygon(new[] {
            topLeft.ToPointF(),
            topRight.ToPointF(),
            bottomRight.ToPointF()
          });
          break;
        case "Right":
          pathSecondFill.AddPolygon(new[] {
            topCenter.ToPointF(),
            topRight.ToPointF(),
            bottomRight.ToPointF(),
            bottomCenter.ToPointF()
          });
          break;
      }

      graphics.DrawPath(palette.Brush(room.SecondFillColor), pathSecondFill);
      graphics.Restore(state);
    }

    if (!ApplicationSettingsController.AppSettings.DebugDisableLineRendering)
    {
      var roomBorderPen = palette.Pen(
        room.RoomBorderColor != Color.Transparent ? room.RoomBorderColor : Settings.Color[Colors.Border],
        Settings.LineWidth);
      roomBorderPen.DashStyle = room.BorderStyle.ConvertToDashStyle();
      graphics.DrawPath(roomBorderPen, path);
    }

    var textBlocks = GetTextBlocks(room);
    room.DrawText(graphics, palette, textBlocks);
  }

  private static Color ResolveRoomFillColor(this Room room)
  {
    if (room.RoomFillColor != Color.Transparent)
      return room.RoomFillColor;

    var regionName = string.IsNullOrWhiteSpace(room.Region)
      ? Trizbort.Domain.Misc.Region.DefaultRegion
      : room.Region;
    var region = Settings.Regions.FirstOrDefault(p => string.Equals(
                   p.RegionName,
                   regionName,
                   StringComparison.OrdinalIgnoreCase)) ??
                 Settings.Regions.FirstOrDefault(p => string.Equals(
                   p.RegionName,
                   Trizbort.Domain.Misc.Region.DefaultRegion,
                   StringComparison.OrdinalIgnoreCase));

    if (region != null && region.RColor != Color.Transparent)
      return region.RColor;

    return Settings.Color[Colors.Canvas];
  }

  public static void AddOutline(this Room room, XGraphicsPath path, Rect bounds, bool handDrawn)
  {
    var random = Sketch.Seeded(room.Id);

    if (room.Shape == RoomShape.RoundedCorners)
    {
      if (handDrawn)
      {
        path.AddPolygon(Sketch.ClosedCurve(
          Sketch.RoundedRectangle(
            bounds,
            (float)room.Corners.TopLeft,
            (float)room.Corners.TopRight,
            (float)room.Corners.BottomRight,
            (float)room.Corners.BottomLeft),
          random).ToPointFArray());
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
        path.AddPolygon(Sketch.ClosedCurve(Sketch.Ellipse(bounds), random).ToPointFArray());
        return;
      }

      path.AddEllipse(bounds.ToRectangleF());
      return;
    }

    Vector[] vertices;
    if (room.Octagonal)
    {
      var qw = bounds.Width / 4;
      var qh = bounds.Height / 4;
      vertices = new[] {
        new Vector(bounds.Left, bounds.Bottom - qh), new Vector(bounds.Left, bounds.Top + qh),
        new Vector(bounds.Left + qw, bounds.Top), new Vector(bounds.Right - qw, bounds.Top),
        new Vector(bounds.Right, bounds.Top + qh), new Vector(bounds.Right, bounds.Bottom - qh),
        new Vector(bounds.Right - qw, bounds.Bottom), new Vector(bounds.Left + qw, bounds.Bottom)
      };
    }
    else
    {
      vertices = new[] {
        new Vector(bounds.Left, bounds.Top), new Vector(bounds.Right, bounds.Top),
        new Vector(bounds.Right, bounds.Bottom), new Vector(bounds.Left, bounds.Bottom)
      };
    }

    path.AddPolygon(handDrawn ? Sketch.Polygon(vertices, random).ToPointFArray() : vertices.ToPointFArray());
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
    var textBounds = room.InnerBounds;
    if (room.Ellipse)
      textBounds.Inflate(-11.5f);
    else
      textBounds.Inflate(-5, -5);

    if (textBounds.Width > 0 && textBounds.Height > 0)
    {
      if (!ApplicationSettingsController.AppSettings.DebugDisableTextRendering)
      {
        var font = Settings.RoomNameFont;
        var roombrush = room.IsReference ? palette.Brush(Color.LightBlue) :
          room.RoomNameColor != Color.Transparent ? palette.Brush(room.RoomNameColor) : palette.SmallTextBrush;
        var tName = room.IsReference ? new TextBlock { Text = "To" } : textBlocks.Name;
        var tSubtitle = room.IsReference ? new TextBlock { Text = room.ReferenceRoom?.Name ?? string.Empty } : textBlocks.SubTitle;
        var roomTextRect = tName.Draw(
          graphics,
          font,
          roombrush,
          textBounds.Position,
          textBounds.Size,
          XStringFormats.Center);

        // draw subtitle text
        var subTitleBrush = room.IsReference ? roombrush :
          room.RoomSubtitleColor != Color.Transparent ? palette.Brush(room.RoomSubtitleColor) : palette.SubtitleTextBrush;
        var subtitleTextRect = new Rect(
          roomTextRect.Left,
          roomTextRect.Bottom,
          roomTextRect.Right - roomTextRect.Left,
          textBounds.Bottom - roomTextRect.Bottom);
        tSubtitle.Draw(
          graphics,
          Settings.SubtitleFont,
          subTitleBrush,
          subtitleTextRect.Position,
          subtitleTextRect.Size,
          XStringFormats.Center);
      }
    }

    var expandedBounds = room.InnerBounds;
    expandedBounds.Inflate(Settings.ObjectListOffsetFromRoom, Settings.ObjectListOffsetFromRoom);
    var drawnObjectList = false;

    var objFont = Settings.ObjectFont;
    var brush = palette.SmallTextBrush;
    // Room specific fill brush (White shows global color)
    var bUseObjectRoomBrush = false;
    if (room.RoomObjectTextColor != Color.Transparent)
    {
      bUseObjectRoomBrush = true;
      brush = palette.Brush(room.RoomObjectTextColor);
    }

    if (!string.IsNullOrEmpty(room.Objects))
    {
      var format = new XStringFormat();
      var pos = expandedBounds.GetCorner(room.ObjectsPosition);

      var displayText = ObjectList.FormatForDisplay(room.Objects);
      if (textBlocks.ObjectsDisplay.Text != displayText) textBlocks.ObjectsDisplay.Text = displayText;

      if (!Drawing.SetAlignmentFromCardinalOrOrdinalDirection(format, room.ObjectsPosition))
      {
        // object list appears inside the room below its name
        format.LineAlignment = XLineAlignment.Far;
        format.Alignment = XStringAlignment.Near;
        var height = room.InnerBounds.Height / 2 - objFont.Height / 2;
        var bounds = new Rect(
          room.InnerBounds.Left + Settings.ObjectListOffsetFromRoom,
          room.InnerBounds.Bottom - height,
          room.InnerBounds.Width - Settings.ObjectListOffsetFromRoom,
          height - Settings.ObjectListOffsetFromRoom);
        if (bUseObjectRoomBrush)
          brush = palette.Brush(room.RoomObjectTextColor);
        pos = bounds.Position;
        pos.X += room.ObjectsCustomPosition ? room.ObjectsCustomPositionRight : 0;
        pos.Y += room.ObjectsCustomPosition ? room.ObjectsCustomPositionDown : 0;
        if (bounds.Width > 0 && bounds.Height > 0)
          textBlocks.ObjectsDisplay.Draw(graphics, objFont, brush, pos, bounds.Size, format);
        drawnObjectList = true;
      }
      else if (room.ObjectsPosition == CompassPoint.North || room.ObjectsPosition == CompassPoint.South)
      {
        pos.X += Settings.ObjectListOffsetFromRoom + (room.ObjectsCustomPosition ? room.ObjectsCustomPositionRight : 0);
        pos.Y += room.ObjectsCustomPosition ? room.ObjectsCustomPositionDown : 0;
      }
      else
      {
        pos.X += room.ObjectsCustomPosition ? room.ObjectsCustomPositionRight : 0;
        pos.Y += room.ObjectsCustomPosition ? room.ObjectsCustomPositionDown : 0;
      }

      if (!drawnObjectList)
        if (!ApplicationSettingsController.AppSettings.DebugDisableTextRendering)
          textBlocks.ObjectsDisplay.Draw(graphics, objFont, brush, pos, Vector.Zero, format);
    }
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
    dialog.ObjectTextColor = room.RoomObjectTextColor;

    dialog.Shape = room.Shape;
    dialog.Corners = room.Corners;
    dialog.AllCornersEqual = room.AllCornersEqual;

    dialog.RoomRegion = room.Region;

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
    room.RoomObjectTextColor = dialog.ObjectTextColor;

    room.ChangeRoomShape(dialog.Shape);
    room.Corners = dialog.Corners;
    room.AllCornersEqual = dialog.AllCornersEqual;

    room.Region = dialog.RoomRegion;
  }
}
