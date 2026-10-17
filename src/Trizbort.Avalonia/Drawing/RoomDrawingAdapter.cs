using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.Drawing;

public static class RoomDrawingAdapter
{
    public static void DrawRoom(this Room room, AvaloniaDrawingContext context, bool isSelected, bool isHover)
    {
        var bounds = room.InnerBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var handDrawn = room.HandDrawnStyle switch
        {
            HandDrawnStyle.HandDrawn => true,
            HandDrawnStyle.Straight => false,
            _ => Settings.HandDrawn
        };
        var seed = room.Id;

        // 1. Build Geometry for Room Shape
        StreamGeometry geometry = room.Shape switch
        {
            RoomShape.RoundedCorners => DrawingAdapters.CreateRoundedRectangleGeometry(bounds, room.Corners, handDrawn, seed),
            RoomShape.Ellipse => DrawingAdapters.CreateEllipseGeometry(bounds, handDrawn, seed),
            RoomShape.Octagonal => DrawingAdapters.CreateOctagonalGeometry(bounds, room.Corners, handDrawn, seed),
            _ => DrawingAdapters.CreateSquareGeometry(bounds, handDrawn, seed)
        };

        // 2. Determine Primary Fill Brush
        IBrush fillBrush;
        if (room.RoomFillColor != CoreColor.Transparent)
        {
            fillBrush = room.RoomFillColor.ToBrush();
        }
        else if (!string.IsNullOrEmpty(room.Region) && !string.Equals(room.Region, Region.DefaultRegion, StringComparison.OrdinalIgnoreCase))
        {
            var regionObj = Settings.Regions.Find(r => string.Equals(r.RegionName, room.Region, StringComparison.OrdinalIgnoreCase));
            if (regionObj != null && regionObj.RColor != CoreColor.Transparent)
            {
                fillBrush = regionObj.RColor.ToBrush();
            }
            else
            {
                fillBrush = DrawingAdapters.ToBrush(CoreColors.Canvas);
            }
        }
        else
        {
            fillBrush = DrawingAdapters.ToBrush(CoreColors.Canvas);
        }

        // 3. Determine Border Pen
        var borderColor = room.RoomBorderColor != CoreColor.Transparent ? room.RoomBorderColor : Settings.Color[CoreColors.Border];
        var dashStyle = room.BorderStyle.ToDashStyle();
        var borderPen = borderColor.ToPen(Settings.LineWidth, dashStyle);

        // 4. Draw Primary Fill & Base Geometry
        context.DrawGeometry(fillBrush, borderPen, geometry);

        // 5. Draw Second Fill (if configured)
        if (room.SecondFillColor != CoreColor.Transparent && !string.IsNullOrEmpty(room.SecondFillLocation))
        {
            var secondRect = GetSecondFillRect(bounds, room.SecondFillLocation);
            if (secondRect.Width > 0 && secondRect.Height > 0)
            {
                using (context.PushGeometryClip(geometry))
                {
                    context.DrawRectangle(room.SecondFillColor.ToBrush(), null, secondRect.ToRect());
                }
            }
        }

        // 6. Draw Darkness Indicator
        if (room.IsDark)
        {
            using (context.PushGeometryClip(geometry))
            {
                var darkSize = Math.Min(16f, Math.Min(bounds.Width, bounds.Height) * 0.4f);
                var darkGeo = new StreamGeometry();
                using (var darkCtx = darkGeo.Open())
                {
                    darkCtx.BeginFigure(new AvaloniaPoint(bounds.Left, bounds.Top), true);
                    darkCtx.LineTo(new AvaloniaPoint(bounds.Left + darkSize, bounds.Top));
                    darkCtx.LineTo(new AvaloniaPoint(bounds.Left, bounds.Top + darkSize));
                    darkCtx.EndFigure(true);
                }
                var darkBrush = new SolidColorBrush(AvaloniaColor.FromArgb(180, 20, 20, 20));
                context.DrawGeometry(darkBrush, null, darkGeo);
            }
        }

        // 7. Draw Start / End Room Inset Adornments
        if (room.IsStartRoom || room.IsEndRoom)
        {
            var insetBounds = bounds;
            insetBounds.Inflate(-4, -4);
            if (insetBounds.Width > 0 && insetBounds.Height > 0)
            {
                StreamGeometry insetGeo = room.Shape switch
                {
                    RoomShape.RoundedCorners => DrawingAdapters.CreateRoundedRectangleGeometry(insetBounds, room.Corners, handDrawn, seed),
                    RoomShape.Ellipse => DrawingAdapters.CreateEllipseGeometry(insetBounds, handDrawn, seed),
                    RoomShape.Octagonal => DrawingAdapters.CreateOctagonalGeometry(insetBounds, room.Corners, handDrawn, seed),
                    _ => DrawingAdapters.CreateSquareGeometry(insetBounds, handDrawn, seed)
                };
                var adornColor = room.IsStartRoom ? Settings.Color[CoreColors.StartRoom] : Settings.Color[CoreColors.EndRoom];
                var insetPen = adornColor.ToPen(Math.Max(1.0, Settings.LineWidth * 0.75));
                context.DrawGeometry(null, insetPen, insetGeo);
            }
        }

        // 8. Draw Selection / Hover Adorners
        if (isSelected)
        {
            var selPen = Settings.Color[CoreColors.SelectedLine].ToPen(Settings.LineWidth + 1.5);
            context.DrawGeometry(null, selPen, geometry);
        }
        else if (isHover)
        {
            var hoverPen = Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0);
            context.DrawGeometry(null, hoverPen, geometry);
        }

        // 9. Draw Room Text (Name, Subtitle, Objects)
        DrawRoomText(room, context, bounds);
    }

    private static CoreRect GetSecondFillRect(CoreRect bounds, string location)
    {
        var hw = bounds.Width / 2f;
        var hh = bounds.Height / 2f;

        return location.ToLowerInvariant() switch
        {
            "bottom" => new CoreRect(bounds.Left, bounds.Top + hh, bounds.Width, hh),
            "top" => new CoreRect(bounds.Left, bounds.Top, bounds.Width, hh),
            "left" => new CoreRect(bounds.Left, bounds.Top, hw, bounds.Height),
            "right" => new CoreRect(bounds.Left + hw, bounds.Top, hw, bounds.Height),
            "bottomright" => new CoreRect(bounds.Left + hw, bounds.Top + hh, hw, hh),
            "bottomleft" => new CoreRect(bounds.Left, bounds.Top + hh, hw, hh),
            "topright" => new CoreRect(bounds.Left + hw, bounds.Top, hw, hh),
            "topleft" => new CoreRect(bounds.Left, bounds.Top, hw, hh),
            _ => new CoreRect(bounds.Left, bounds.Top + hh, bounds.Width, hh)
        };
    }

    private static void DrawRoomText(Room room, AvaloniaDrawingContext context, CoreRect bounds)
    {
        var textBounds = bounds;
        if (room.Shape == RoomShape.Ellipse)
            textBounds.Inflate(-11.5f, -11.5f);
        else
            textBounds.Inflate(-5, -5);

        if (textBounds.Width <= 0 || textBounds.Height <= 0) return;

        var nameText = room.IsReference ? "To" : (room.Name ?? string.Empty);
        var subtitleText = room.IsReference ? (room.ReferenceRoom?.Name ?? string.Empty) : (room.SubTitle ?? string.Empty);

        var nameFont = Settings.RoomFontMetadata;
        var nameTypeface = nameFont.ToTypeface();
        var nameBrush = room.IsReference ? DrawingAdapters.ToBrush(CoreColors.SmallText) :
            (room.RoomNameColor != CoreColor.Transparent ? room.RoomNameColor.ToBrush() : DrawingAdapters.ToBrush(CoreColors.SmallText));

        var hasSubtitle = !string.IsNullOrWhiteSpace(subtitleText);
        var subFont = Settings.SubtitleFontMetadata;
        var subTypeface = subFont.ToTypeface();
        var subBrush = room.IsReference ? nameBrush :
            (room.RoomSubtitleColor != CoreColor.Transparent ? room.RoomSubtitleColor.ToBrush() : DrawingAdapters.ToBrush(CoreColors.Subtitle));

        FormattedText? formattedName = null;
        FormattedText? formattedSub = null;

        if (!string.IsNullOrEmpty(nameText))
        {
            formattedName = new FormattedText(
                nameText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                nameTypeface,
                nameFont.Size,
                nameBrush)
            {
                TextAlignment = TextAlignment.Center,
                MaxTextWidth = textBounds.Width,
                MaxTextHeight = textBounds.Height
            };
        }

        if (hasSubtitle)
        {
            formattedSub = new FormattedText(
                subtitleText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                subTypeface,
                subFont.Size,
                subBrush)
            {
                TextAlignment = TextAlignment.Center,
                MaxTextWidth = textBounds.Width,
                MaxTextHeight = textBounds.Height
            };
        }

        var totalTextHeight = (formattedName?.Height ?? 0) + (formattedSub?.Height ?? 0);
        var startY = textBounds.Top + Math.Max(0, (textBounds.Height - totalTextHeight) / 2.0);

        if (formattedName != null)
        {
            context.DrawText(formattedName, new AvaloniaPoint(textBounds.Left, startY));
            startY += formattedName.Height;
        }

        if (formattedSub != null)
        {
            context.DrawText(formattedSub, new AvaloniaPoint(textBounds.Left, startY));
        }

        // Draw Objects List
        if (!string.IsNullOrEmpty(room.Objects))
        {
            DrawObjectsList(room, context, bounds);
        }
    }

    private static void DrawObjectsList(Room room, AvaloniaDrawingContext context, CoreRect bounds)
    {
        var displayText = ObjectList.FormatForDisplay(room.Objects);
        if (string.IsNullOrWhiteSpace(displayText)) return;

        var objFont = Settings.ObjectFontMetadata;
        var objTypeface = objFont.ToTypeface();
        var objBrush = room.RoomObjectTextColor != CoreColor.Transparent
            ? room.RoomObjectTextColor.ToBrush()
            : DrawingAdapters.ToBrush(CoreColors.SmallText);

        var expandedBounds = bounds;
        expandedBounds.Inflate(Settings.ObjectListOffsetFromRoom, Settings.ObjectListOffsetFromRoom);

        var pos = expandedBounds.GetCorner(room.ObjectsPosition);
        pos.X += room.ObjectsCustomPosition ? room.ObjectsCustomPositionRight : 0;
        pos.Y += room.ObjectsCustomPosition ? room.ObjectsCustomPositionDown : 0;

        var alignment = TextAlignment.Left;
        switch (room.ObjectsPosition)
        {
            case CompassPoint.North:
            case CompassPoint.South:
                alignment = TextAlignment.Center;
                break;
            case CompassPoint.East:
            case CompassPoint.NorthEast:
            case CompassPoint.SouthEast:
                alignment = TextAlignment.Left;
                break;
            case CompassPoint.West:
            case CompassPoint.NorthWest:
            case CompassPoint.SouthWest:
                alignment = TextAlignment.Right;
                break;
        }

        var formattedObj = new FormattedText(
            displayText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            objTypeface,
            objFont.Size,
            objBrush)
        {
            TextAlignment = alignment
        };
        formattedObj.ConstrainToNaturalWidth();

        double drawX = pos.X;
        double drawY = pos.Y;

        if (alignment == TextAlignment.Center)
        {
            drawX -= formattedObj.Width / 2.0;
        }
        else if (alignment == TextAlignment.Right)
        {
            drawX -= formattedObj.Width;
        }

        if (room.ObjectsPosition == CompassPoint.North ||
            room.ObjectsPosition == CompassPoint.NorthEast ||
            room.ObjectsPosition == CompassPoint.NorthWest)
        {
            drawY -= formattedObj.Height;
        }

        context.DrawText(formattedObj, new AvaloniaPoint(drawX, drawY));
    }
}
