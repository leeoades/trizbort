using System.Globalization;
using System.Runtime.CompilerServices;
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
using AvaloniaPoint = Avalonia.Point;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.Drawing;

public static class RoomDrawingAdapter
{
    private static readonly IBrush _selHaloBrush = new SolidColorBrush(AvaloniaColor.FromArgb(120, 255, 255, 0));

    private record struct GeometryKey(CoreRect Bounds, RoomShape Shape, double Tl, double Tr, double Br, double Bl, bool HandDrawn);

    private class RoomTextCache
    {
        public GeometryKey LastBaseKey;
        public StreamGeometry? CachedGeometry;

        public GeometryKey LastHaloKey;
        public StreamGeometry? CachedHaloGeometry;

        public GeometryKey LastSelHaloKey;
        public StreamGeometry? CachedSelHaloGeometry;

        public string? LastSecondFillLoc;
        public CoreRect LastSecondFillBounds;
        public StreamGeometry? CachedSecondFillGeometry;

        public CoreRect LastDarkBounds;
        public StreamGeometry? CachedDarkGeometry;

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
        public double CachedNameX;
        public double CachedSubtitleX;

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

    internal static StreamGeometry? GetCachedGeometry(Room room)
    {
        return _textCaches.TryGetValue(room, out var c) ? c.CachedGeometry : null;
    }

    internal static FormattedText? GetCachedName(Room room)
    {
        return _textCaches.TryGetValue(room, out var c) ? c.CachedName : null;
    }

    internal static string? GetCachedNameText(Room room)
    {
        return _textCaches.TryGetValue(room, out var c) ? c.LastName : null;
    }

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
        var fillBrush = ResolveRoomFillBrush(room);

        // 5. Determine Border Pen
        var borderColor = room.RoomBorderColor != CoreColor.Transparent ? room.RoomBorderColor : Settings.Color[CoreColors.Border];
        var dashStyle = room.BorderStyle.ToDashStyle();
        var borderPen = borderColor.ToPen(Settings.LineWidth, dashStyle);

        // 6. Draw Primary Fill & Base Geometry
        // Legacy Room.cs:803 `BorderStyle != BorderDashStyle.None` guards the whole fill/second fill/dark/border block,
        // so a room with no border style draws only its text (I-025 R4, spiral "Fall to Carriage").
        var drawBody = room.BorderStyle != BorderDashStyle.None;
        if (drawBody) context.DrawGeometry(fillBrush, borderPen, geometry);

        // 7. Draw Second Fill (if configured)
        if (drawBody && room.SecondFillColor != CoreColor.Transparent && !string.IsNullOrEmpty(room.SecondFillLocation))
        {
            if (cache.CachedSecondFillGeometry == null
                || cache.LastSecondFillLoc != room.SecondFillLocation
                || !cache.LastSecondFillBounds.Equals(bounds))
            {
                var poly = GetSecondFillPolygon(bounds, room.SecondFillLocation);
                if (poly != null && poly.Length > 0)
                {
                    cache.CachedSecondFillGeometry = DrawingAdapters.ToPolygonGeometry(poly, true);
                    cache.LastSecondFillLoc = room.SecondFillLocation;
                    cache.LastSecondFillBounds = bounds;
                }
                else
                {
                    cache.CachedSecondFillGeometry = null;
                }
            }

            if (cache.CachedSecondFillGeometry != null)
            {
                using (context.PushGeometryClip(geometry))
                {
                    context.DrawGeometry(room.SecondFillColor.ToBrush(), null, cache.CachedSecondFillGeometry);
                }
            }
        }

        // 8. Draw Darkness Indicator
        if (drawBody && room.IsDark)
        {
            using (context.PushGeometryClip(geometry))
            {
                var tri = GetDarknessTriangle(bounds, room.Shape, corners, Settings.DarknessStripeSize);
                var darkGeo = new StreamGeometry();
                using (var darkCtx = darkGeo.Open())
                {
                    darkCtx.BeginFigure(tri[0].ToAvaloniaPoint(), true);
                    darkCtx.LineTo(tri[1].ToAvaloniaPoint());
                    darkCtx.LineTo(tri[2].ToAvaloniaPoint());
                    darkCtx.EndFigure(true);
                }
                // Legacy fills with palette.BorderBrush (Room.cs:893-895).
                context.DrawGeometry(Settings.Color[CoreColors.Border].ToBrush(), null, darkGeo);
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

        // 11. Validation: red cross through the room when it breaks a project rule.
        room.CheckValidation();
        if (!room.Valid())
        {
            var crossPen = new Pen(Brushes.Red, 2.0);
            context.DrawLine(crossPen, new AvaloniaPoint(bounds.Left, bounds.Top), new AvaloniaPoint(bounds.Right, bounds.Bottom));
            context.DrawLine(crossPen, new AvaloniaPoint(bounds.Right, bounds.Top), new AvaloniaPoint(bounds.Left, bounds.Bottom));
        }
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
            // Vertex order follows the legacy Room.Draw segment start points.
            "bottom" => [leftCenter, rightCenter, bottomRight, bottomLeft],
            "bottomright" => [bottomLeft, topRight, bottomRight],
            "bottomleft" => [bottomRight, topLeft, bottomLeft],
            "left" => [topCenter, topLeft, bottomLeft, bottomCenter],
            "right" => [topCenter, topRight, bottomRight, bottomCenter],
            "topright" => [topLeft, topRight, bottomRight],
            "topleft" => [topLeft, topRight, bottomLeft],
            "top" => [leftCenter, rightCenter, topRight, topLeft],
            _ => null
        };
    }

    /// <summary>
    /// The dark-room marker triangle in the top-right corner, sized as in legacy Room.cs:889-917
    /// (stripe size, or shape-specific fractions for ellipse, rounded and octagonal rooms).
    /// </summary>
    internal static CoreVector[] GetDarknessTriangle(CoreRect bounds, RoomShape shape, CornerRadii? corners, float stripeSize)
    {
        var dx = stripeSize;
        var dy = stripeSize;
        if (shape == RoomShape.Ellipse)
        {
            dy = 2 * bounds.Height / 5;
            dx = 2 * bounds.Width / 5;
        }
        else if (shape == RoomShape.RoundedCorners)
        {
            if (corners != null && corners.TopRight > 2 * stripeSize)
                dy = dx = (float)corners.TopRight / 2;
        }
        else if (shape == RoomShape.Octagonal)
        {
            dx = bounds.Width * 7 / 20;
            dy = bounds.Height * 7 / 20;
        }

        var topRight = bounds.GetCorner(CompassPoint.NorthEast);
        return
        [
            topRight,
            new CoreVector(topRight.X - dx, topRight.Y),
            new CoreVector(topRight.X, topRight.Y + dy)
        ];
    }

    private static Region? ResolveRegion(Room room)
    {
        var regionName = string.IsNullOrWhiteSpace(room.Region) ? Region.DefaultRegion : room.Region;
        return Settings.Regions.Find(r =>
            string.Equals(r.RegionName, regionName, StringComparison.OrdinalIgnoreCase)) ??
            Settings.Regions.Find(r => string.Equals(r.RegionName, Region.DefaultRegion, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Room name colour: the room's own colour, otherwise the region's text colour (legacy Room.cs:913-916).
    /// </summary>
    internal static CoreColor ResolveRoomNameColor(Room room)
    {
        if (room.RoomNameColor != CoreColor.Transparent) return room.RoomNameColor;
        var region = ResolveRegion(room);
        return region != null ? region.TextColor : Settings.Color[CoreColors.SmallText];
    }

    /// <summary>
    /// Legacy TextBlock.RebuildCachedLayout (TextBlock.cs:131-205): single-line text wraps only at spaces
    /// (and at dashes when <paramref name="splitDashes"/>); a word wider than the box stays on one line.
    /// </summary>
    internal static string WrapLikeLegacy(string text, double maxWidth, Func<string, double> measure, bool splitDashes)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (text.IndexOf('\n') != -1 || maxWidth <= 0 || measure(text) <= maxWidth) return text;

        var spaceLength = (float)(measure("M M") - measure("M") * 2);
        var hyphenLength = (float)measure("-");

        var step1 = new List<(string Text, float Length)>();
        foreach (var word in text.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (step1.Count != 0) step1.Add((" ", spaceLength));
            step1.Add((word, (float)measure(word)));
        }

        var words = new List<(string Text, float Length)>();
        foreach (var item in step1.Where(p => !string.IsNullOrWhiteSpace(p.Text)))
        {
            var splits = splitDashes ? item.Text.Split('-') : new[] { item.Text };
            if (splits.Length > 1)
            {
                var list = new List<(string Text, float Length)>();
                foreach (var part in splits)
                {
                    if (words.Count != 0 && list.Count == 0) list.Add((" ", spaceLength));
                    else if (list.Count != 0 && splitDashes) list.Add(("-", hyphenLength));
                    list.Add((part, (float)measure(part)));
                }
                words.AddRange(list);
            }
            else
            {
                if (words.Count != 0) words.Add((" ", spaceLength));
                words.Add((splits[0], (float)measure(splits[0])));
            }
        }

        var lineLength = 0.0f;
        var total = string.Empty;
        var line = string.Empty;
        foreach (var word in words)
        {
            if (word.Text != " " && word.Length > Math.Max(0, maxWidth - lineLength) && lineLength > 0)
            {
                if (line.Length > 0)
                {
                    if (total.Length > 0) total += "\n";
                    total += line;
                    lineLength = word.Length + spaceLength;
                    line = word.Text;
                }
            }
            else
            {
                line += word.Text;
                lineLength += word.Length + spaceLength;
            }
        }

        if (line.Length > 0)
        {
            if (total.Length > 0) total += "\n";
            total += line;
        }

        return total;
    }

    private static FormattedText BuildWrappedText(string text, double maxWidth, Typeface typeface, double size, IBrush brush, bool splitDashes)
    {
        double Measure(string s) => new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, Brushes.Black).WidthIncludingTrailingWhitespace;

        var wrapped = WrapLikeLegacy(text, maxWidth, Measure, splitDashes);
        var lines = wrapped.Split('\n').Select(l => l.TrimEnd(' '));
        var formatted = new FormattedText(string.Join("\n", lines), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush)
        {
            TextAlignment = TextAlignment.Center
        };
        // No wrapping inside the layout: width is the widest line so each line is centred on it.
        formatted.MaxTextWidth = formatted.WidthIncludingTrailingWhitespace + 1;
        return formatted;
    }

    internal static IBrush ResolveRoomFillBrush(Room room)
    {
        return ResolveRoomFillColor(room).ToBrush();
    }

    private static CoreColor ResolveRoomFillColor(Room room)
    {
        if (room.RoomFillColor != CoreColor.Transparent)
        {
            return room.RoomFillColor;
        }

        var regionObj = ResolveRegion(room);

        if (regionObj != null && regionObj.RColor != CoreColor.Transparent)
        {
            return regionObj.RColor;
        }

        return Settings.Color[CoreColors.Canvas];
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
        // Legacy Room.cs:912-916: name (and, for reference rooms, subtitle) colour is the room colour or the region text colour.
        var nameColor = ResolveRoomNameColor(room);

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

            // Legacy TextBlock.Draw wraps only at word boundaries and clips to the box (TextBlock.cs:84-86, 131-205).
            var splitDashes = Settings.WrapTextAtDashes;
            cache.CachedName = !string.IsNullOrEmpty(nameText)
                ? BuildWrappedText(nameText, textBounds.Width, nameTypeface, nameFont.Size, nameBrush, splitDashes)
                : null;

            cache.CachedSubtitle = hasSubtitle
                ? BuildWrappedText(subtitleText, textBounds.Width, subTypeface, subFont.Size, subBrush, splitDashes)
                : null;

            var totalTextHeight = (cache.CachedName?.Height ?? 0) + (cache.CachedSubtitle?.Height ?? 0);
            cache.CachedStartY = textBounds.Top + Math.Max(0, (textBounds.Height - totalTextHeight) / 2.0);
            cache.CachedNameX = cache.CachedName != null
                ? textBounds.Left + (textBounds.Width - cache.CachedName.MaxTextWidth) / 2.0
                : textBounds.Left;
            cache.CachedSubtitleX = cache.CachedSubtitle != null
                ? textBounds.Left + (textBounds.Width - cache.CachedSubtitle.MaxTextWidth) / 2.0
                : textBounds.Left;
        }

        var startY = cache.CachedStartY;
        using (context.PushClip(new global::Avalonia.Rect(textBounds.Left, textBounds.Top, textBounds.Width, textBounds.Height)))
        {
            if (cache.CachedName != null)
            {
                if (TextRenderingGate.IsEnabled) context.DrawText(cache.CachedName, new AvaloniaPoint(cache.CachedNameX, startY));
                startY += cache.CachedName.Height;
            }

            if (cache.CachedSubtitle != null)
            {
                if (TextRenderingGate.IsEnabled) context.DrawText(cache.CachedSubtitle, new AvaloniaPoint(cache.CachedSubtitleX, startY));
            }
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

            var formattedObj = new FormattedText(
                displayText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                objTypeface,
                objFont.Size,
                objBrush)
            {
                TextAlignment = TextAlignment.Left
            };
            formattedObj.ConstrainToNaturalWidth();

            double drawX;
            double drawY;

            var isOutside = room.ObjectsPosition switch
            {
                CompassPoint.North or CompassPoint.NorthEast or CompassPoint.East or
                CompassPoint.SouthEast or CompassPoint.South or CompassPoint.SouthWest or
                CompassPoint.West or CompassPoint.NorthWest => true,
                _ => false
            };

            if (!isOutside)
            {
                // Object list appears inside the room below its name
                // Matches WinForms RoomDrawingExtensions.DrawText / SetAlignmentFromCardinalOrOrdinalDirection
                var height = bounds.Height / 2 - objFont.Size / 2;
                var insideBounds = new CoreRect(
                    bounds.Left + Settings.ObjectListOffsetFromRoom,
                    bounds.Bottom - height,
                    bounds.Width - Settings.ObjectListOffsetFromRoom,
                    height - Settings.ObjectListOffsetFromRoom);

                drawX = insideBounds.Left + (room.ObjectsCustomPosition ? room.ObjectsCustomPositionRight : 0);
                drawY = bounds.Bottom - Settings.ObjectListOffsetFromRoom - formattedObj.Height + (room.ObjectsCustomPosition ? room.ObjectsCustomPositionDown : 0);
            }
            else
            {
                var expandedBounds = bounds;
                expandedBounds.Inflate(Settings.ObjectListOffsetFromRoom, Settings.ObjectListOffsetFromRoom);
                var pos = expandedBounds.GetCorner(room.ObjectsPosition);

                if (room.ObjectsPosition == CompassPoint.North || room.ObjectsPosition == CompassPoint.South)
                {
                    pos.X += Settings.ObjectListOffsetFromRoom + (room.ObjectsCustomPosition ? room.ObjectsCustomPositionRight : 0);
                    pos.Y += room.ObjectsCustomPosition ? room.ObjectsCustomPositionDown : 0;
                }
                else
                {
                    pos.X += room.ObjectsCustomPosition ? room.ObjectsCustomPositionRight : 0;
                    pos.Y += room.ObjectsCustomPosition ? room.ObjectsCustomPositionDown : 0;
                }

                drawX = pos.X;
                drawY = pos.Y;

                if (room.ObjectsPosition is CompassPoint.West or CompassPoint.NorthWest or CompassPoint.SouthWest)
                {
                    drawX -= formattedObj.Width;
                    formattedObj.TextAlignment = TextAlignment.Right;
                }

                if (room.ObjectsPosition is CompassPoint.North or CompassPoint.NorthEast or CompassPoint.NorthWest)
                {
                    drawY -= formattedObj.Height;
                }
            }

            cache.CachedObjects = formattedObj;
            cache.CachedObjectsDrawX = drawX;
            cache.CachedObjectsDrawY = drawY;
        }

        if (TextRenderingGate.IsEnabled) context.DrawText(cache.CachedObjects, new AvaloniaPoint(cache.CachedObjectsDrawX, cache.CachedObjectsDrawY));
    }
}
