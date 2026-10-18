using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Trizbort.Domain.Application;
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
    private static readonly IBrush _selHaloBrush = new SolidColorBrush(AvaloniaColor.FromArgb(120, 255, 255, 0));
    private static readonly IBrush _darkBrush = new SolidColorBrush(AvaloniaColor.FromArgb(180, 20, 20, 20));

    private record struct GeometryKey(CoreRect Bounds, RoomShape Shape, double Tl, double Tr, double Br, double Bl, bool HandDrawn);

    private class RoomTextCache
    {
        public GeometryKey LastBaseKey;
        public StreamGeometry? CachedGeometry;

        public GeometryKey LastHaloKey;
        public StreamGeometry? CachedHaloGeometry;

        public GeometryKey LastSelHaloKey;
        public StreamGeometry? CachedSelHaloGeometry;

        public string? LastName;
        public string? LastSubtitle;
        public string? LastNameFontName;
        public int LastNameFontStyle;
        public float LastNameFontSize;
        public string? LastSubFontName;
        public int LastSubFontStyle;
        public float LastSubFontSize;
        public uint LastNameColor;
        public uint LastSubColor;
        public CoreRect LastBounds;
        public FormattedText? CachedName;
        public FormattedText? CachedSubtitle;
        public double CachedStartY;

        public string? LastObjectsText;
        public CompassPoint LastObjectsPos;
        public bool LastObjectsCustom;
        public double LastCustomRight;
        public double LastCustomDown;
        public string? LastObjFontName;
        public int LastObjFontStyle;
        public float LastObjectFontSize;
        public uint LastObjectColor;
        public float LastObjectOffset;
        public CoreRect LastObjectBounds;
        public FormattedText? CachedObjects;
        public double CachedObjectsDrawX;
        public double CachedObjectsDrawY;
    }

    private static readonly ConditionalWeakTable<Room, RoomTextCache> _textCaches = new();

    public static StreamGeometry CreateShapeGeometry(CoreRect bounds, RoomShape shape, CornerRadii? corners, bool handDrawn, int seed)
    {
        var nonNullCorners = corners ?? new CornerRadii();
        return shape switch
        {
            RoomShape.RoundedCorners => DrawingAdapters.CreateRoundedRectangleGeometry(bounds, nonNullCorners, handDrawn, seed),
            RoomShape.Ellipse => DrawingAdapters.CreateEllipseGeometry(bounds, handDrawn, seed),
            RoomShape.Octagonal => DrawingAdapters.CreateOctagonalGeometry(bounds, nonNullCorners, handDrawn, seed),
            _ => DrawingAdapters.CreateSquareGeometry(bounds, handDrawn, seed)
        };
    }

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
        var cache = _textCaches.GetOrCreateValue(room);
        var corners = room.Corners;
        var (tl, tr, br, bl) = corners != null ? (corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft) : (0f, 0f, 0f, 0f);

        // 1. Draw Start / End / Reference Outer Halo (behind room)
        if (room.IsStartRoom || room.IsEndRoom || room.IsReference)
        {
            var haloBounds = bounds;
            haloBounds.Inflate(5);
            var haloKey = new GeometryKey(haloBounds, room.Shape, tl, tr, br, bl, handDrawn);
            if (cache.CachedHaloGeometry == null || !cache.LastHaloKey.Equals(haloKey))
            {
                cache.LastHaloKey = haloKey;
                cache.CachedHaloGeometry = CreateShapeGeometry(haloBounds, room.Shape, corners, handDrawn, seed);
            }

            var haloBrush = room.IsReference
                ? CoreColor.LightBlue.ToBrush()
                : (room.IsStartRoom ? Settings.Color[CoreColors.StartRoom] : Settings.Color[CoreColors.EndRoom]).ToBrush();

            context.DrawGeometry(haloBrush, null, cache.CachedHaloGeometry);
        }

        // 2. Draw Selection Halo (behind room)
        if (isSelected)
        {
            var selBounds = bounds;
            var isPrimary = Project.Current.ActiveSelectedElement?.Id == room.Id;
            selBounds.Inflate(isPrimary ? 10 : 5);
            var selKey = new GeometryKey(selBounds, room.Shape, tl, tr, br, bl, handDrawn);
            if (cache.CachedSelHaloGeometry == null || !cache.LastSelHaloKey.Equals(selKey))
            {
                cache.LastSelHaloKey = selKey;
                cache.CachedSelHaloGeometry = CreateShapeGeometry(selBounds, room.Shape, corners, handDrawn, seed);
            }

            context.DrawGeometry(_selHaloBrush, null, cache.CachedSelHaloGeometry);
        }

        // 3. Build Base Geometry for Room Shape (Cached)
        var baseKey = new GeometryKey(bounds, room.Shape, tl, tr, br, bl, handDrawn);
        if (cache.CachedGeometry == null || !cache.LastBaseKey.Equals(baseKey))
        {
            cache.LastBaseKey = baseKey;
            cache.CachedGeometry = CreateShapeGeometry(bounds, room.Shape, corners, handDrawn, seed);
        }
        var geometry = cache.CachedGeometry;

        // 4. Determine Primary Fill Brush
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

        // 5. Determine Border Pen
        var borderColor = room.RoomBorderColor != CoreColor.Transparent ? room.RoomBorderColor : Settings.Color[CoreColors.Border];
        var dashStyle = room.BorderStyle.ToDashStyle();
        var borderPen = borderColor.ToPen(Settings.LineWidth, dashStyle);

        // 6. Draw Primary Fill & Base Geometry
        context.DrawGeometry(fillBrush, borderPen, geometry);

        // 7. Draw Second Fill (if configured)
        if (room.SecondFillColor != CoreColor.Transparent && !string.IsNullOrEmpty(room.SecondFillLocation))
        {
            var poly = GetSecondFillPolygon(bounds, room.SecondFillLocation);
            if (poly != null && poly.Length > 0)
            {
                var secondGeo = DrawingAdapters.ToPolygonGeometry(poly, true);
                using (context.PushGeometryClip(geometry))
                {
                    context.DrawGeometry(room.SecondFillColor.ToBrush(), null, secondGeo);
                }
            }
        }

        // 8. Draw Darkness Indicator
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

        // 9. Draw Hover Adorner
        if (isHover && !isSelected)
        {
            var hoverPen = Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0);
            context.DrawGeometry(null, hoverPen, geometry);
        }

        // 10. Draw Room Text (Name, Subtitle, Objects)
        DrawRoomText(room, context, bounds, cache);
    }

    public static CoreVector[]? GetSecondFillPolygon(CoreRect bounds, string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;

        var leftCenter = bounds.GetCorner(CompassPoint.West);
        var rightCenter = bounds.GetCorner(CompassPoint.East);
        var topCenter = bounds.GetCorner(CompassPoint.North);
        var bottomCenter = bounds.GetCorner(CompassPoint.South);
        var topLeft = bounds.GetCorner(CompassPoint.NorthWest);
        var topRight = bounds.GetCorner(CompassPoint.NorthEast);
        var bottomLeft = bounds.GetCorner(CompassPoint.SouthWest);
        var bottomRight = bounds.GetCorner(CompassPoint.SouthEast);

        return location.ToLowerInvariant() switch
        {
            "bottom" => [leftCenter, rightCenter, bottomRight, bottomLeft],
            "bottomright" => [topRight, bottomLeft, bottomRight],
            "topright" => [topLeft, topRight, bottomRight],
            "right" => [topCenter, topRight, bottomRight, bottomCenter],
            _ => null
        };
    }

    private static void DrawRoomText(Room room, AvaloniaDrawingContext context, CoreRect bounds, RoomTextCache cache)
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
        var nameColor = room.IsReference
            ? Settings.Color[CoreColors.SmallText]
            : (room.RoomNameColor != CoreColor.Transparent ? room.RoomNameColor : Settings.Color[CoreColors.SmallText]);

        var hasSubtitle = !string.IsNullOrWhiteSpace(subtitleText);
        var subFont = Settings.SubtitleFontMetadata;
        var subColor = room.IsReference
            ? nameColor
            : (room.RoomSubtitleColor != CoreColor.Transparent ? room.RoomSubtitleColor : Settings.Color[CoreColors.Subtitle]);

        var isCacheValid = cache.LastName == nameText
                           && cache.LastSubtitle == subtitleText
                           && cache.LastNameFontName == nameFont.Name
                           && cache.LastNameFontStyle == (int)nameFont.Style
                           && Math.Abs(cache.LastNameFontSize - nameFont.Size) < 0.01f
                           && cache.LastSubFontName == subFont.Name
                           && cache.LastSubFontStyle == (int)subFont.Style
                           && Math.Abs(cache.LastSubFontSize - subFont.Size) < 0.01f
                           && cache.LastNameColor == (uint)nameColor.ToArgb()
                           && cache.LastSubColor == (uint)subColor.ToArgb()
                           && cache.LastBounds.Equals(textBounds);

        if (!isCacheValid)
        {
            cache.LastName = nameText;
            cache.LastSubtitle = subtitleText;
            cache.LastNameFontName = nameFont.Name;
            cache.LastNameFontStyle = (int)nameFont.Style;
            cache.LastNameFontSize = nameFont.Size;
            cache.LastSubFontName = subFont.Name;
            cache.LastSubFontStyle = (int)subFont.Style;
            cache.LastSubFontSize = subFont.Size;
            cache.LastNameColor = (uint)nameColor.ToArgb();
            cache.LastSubColor = (uint)subColor.ToArgb();
            cache.LastBounds = textBounds;

            var nameTypeface = nameFont.ToTypeface();
            var nameBrush = nameColor.ToBrush();
            var subTypeface = subFont.ToTypeface();
            var subBrush = subColor.ToBrush();

            cache.CachedName = !string.IsNullOrEmpty(nameText)
                ? new FormattedText(
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
                }
                : null;

            cache.CachedSubtitle = hasSubtitle
                ? new FormattedText(
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
                }
                : null;

            var totalTextHeight = (cache.CachedName?.Height ?? 0) + (cache.CachedSubtitle?.Height ?? 0);
            cache.CachedStartY = textBounds.Top + Math.Max(0, (textBounds.Height - totalTextHeight) / 2.0);
        }

        var startY = cache.CachedStartY;
        if (cache.CachedName != null)
        {
            context.DrawText(cache.CachedName, new AvaloniaPoint(textBounds.Left, startY));
            startY += cache.CachedName.Height;
        }

        if (cache.CachedSubtitle != null)
        {
            context.DrawText(cache.CachedSubtitle, new AvaloniaPoint(textBounds.Left, startY));
        }

        // Draw Objects List
        if (!string.IsNullOrEmpty(room.Objects))
        {
            DrawObjectsList(room, context, bounds, cache);
        }
    }

    private static void DrawObjectsList(Room room, AvaloniaDrawingContext context, CoreRect bounds, RoomTextCache cache)
    {
        var displayText = ObjectList.FormatForDisplay(room.Objects);
        if (string.IsNullOrWhiteSpace(displayText)) return;

        var objFont = Settings.ObjectFontMetadata;
        var objColor = room.RoomObjectTextColor != CoreColor.Transparent
            ? room.RoomObjectTextColor
            : Settings.Color[CoreColors.SmallText];

        var isObjectsCacheValid = cache.LastObjectsText == displayText
                                  && cache.LastObjectsPos == room.ObjectsPosition
                                  && cache.LastObjectsCustom == room.ObjectsCustomPosition
                                  && Math.Abs(cache.LastCustomRight - room.ObjectsCustomPositionRight) < 0.01
                                  && Math.Abs(cache.LastCustomDown - room.ObjectsCustomPositionDown) < 0.01
                                  && cache.LastObjFontName == objFont.Name
                                  && cache.LastObjFontStyle == (int)objFont.Style
                                  && Math.Abs(cache.LastObjectFontSize - objFont.Size) < 0.01f
                                  && cache.LastObjectColor == (uint)objColor.ToArgb()
                                  && Math.Abs(cache.LastObjectOffset - Settings.ObjectListOffsetFromRoom) < 0.01f
                                  && cache.LastObjectBounds.Equals(bounds);

        if (!isObjectsCacheValid || cache.CachedObjects == null)
        {
            cache.LastObjectsText = displayText;
            cache.LastObjectsPos = room.ObjectsPosition;
            cache.LastObjectsCustom = room.ObjectsCustomPosition;
            cache.LastCustomRight = room.ObjectsCustomPositionRight;
            cache.LastCustomDown = room.ObjectsCustomPositionDown;
            cache.LastObjFontName = objFont.Name;
            cache.LastObjFontStyle = (int)objFont.Style;
            cache.LastObjectFontSize = objFont.Size;
            cache.LastObjectColor = (uint)objColor.ToArgb();
            cache.LastObjectOffset = Settings.ObjectListOffsetFromRoom;
            cache.LastObjectBounds = bounds;

            var objTypeface = objFont.ToTypeface();
            var objBrush = objColor.ToBrush();

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

            cache.CachedObjects = formattedObj;
            cache.CachedObjectsDrawX = drawX;
            cache.CachedObjectsDrawY = drawY;
        }

        context.DrawText(cache.CachedObjects, new AvaloniaPoint(cache.CachedObjectsDrawX, cache.CachedObjectsDrawY));
    }
}
