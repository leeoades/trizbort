using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Setup {
  [JsonObject(ItemRequired = Required.Always)]
  public sealed class MapTheme {
    public string Format { get; set; } = "trizbort-theme";
    public int Version { get; set; } = 1;
    public string Name { get; set; }
    public Dictionary<string, string> Colors { get; set; }
    public List<ThemeRegion> Regions { get; set; }
    public ThemeFont RoomFont { get; set; }
    public ThemeFont ObjectFont { get; set; }
    public ThemeFont SubtitleFont { get; set; }
    public ThemeFont LineFont { get; set; }
    public float LineWidth { get; set; }
    public float ArrowSize { get; set; }
    public float TextOffset { get; set; }
    public float DarknessStripeSize { get; set; }
    public float ObjectListOffset { get; set; }
    public float ConnectionStalkLength { get; set; }
    public RoomShape DefaultRoomShape { get; set; }
    public float GridSize { get; set; }
    public bool GridVisible { get; set; }
    public bool ShowOrigin { get; set; }
    public bool DocumentSpecificMargins { get; set; }
    public float HorizontalMargin { get; set; }
    public float VerticalMargin { get; set; }
    public bool WrapTextAtDashes { get; set; }

    // Optional so that themes exported before hand-drawn became a map setting still load.
    [JsonProperty(Required = Required.Default)]
    public bool HandDrawn { get; set; }

    public static MapTheme Capture(string name) {
      var colors = new Dictionary<string, string>();
      for (var index = 0; index < Domain.Misc.Colors.Count; index++) {
        Domain.Misc.Colors.ToName(index, out var colorName);
        colors.Add(colorName, ColorTranslator.ToHtml(Settings.Color[index]));
      }

      return new MapTheme {
        Name = name,
        Colors = colors,
        Regions = Settings.Regions.Select(region => new ThemeRegion {
          Name = region.RegionName, Fill = ColorTranslator.ToHtml(region.RColor),
          Text = ColorTranslator.ToHtml(region.TextColor)
        }).ToList(),
        RoomFont = ThemeFont.Capture(Settings.RoomNameFont),
        ObjectFont = ThemeFont.Capture(Settings.ObjectFont),
        SubtitleFont = ThemeFont.Capture(Settings.SubtitleFont),
        LineFont = ThemeFont.Capture(Settings.LineFont),
        LineWidth = Settings.LineWidth,
        ArrowSize = Settings.ConnectionArrowSize,
        TextOffset = Settings.TextOffsetFromConnection,
        DarknessStripeSize = Settings.DarknessStripeSize,
        ObjectListOffset = Settings.ObjectListOffsetFromRoom,
        ConnectionStalkLength = Settings.ConnectionStalkLength,
        DefaultRoomShape = Settings.DefaultRoomShape,
        GridSize = Settings.GridSize,
        GridVisible = Settings.IsGridVisible,
        ShowOrigin = Settings.ShowOrigin,
        DocumentSpecificMargins = Settings.DocumentSpecificMargins,
        HorizontalMargin = Settings.DocHorizontalMargin,
        VerticalMargin = Settings.DocVerticalMargin,
        WrapTextAtDashes = Settings.WrapTextAtDashes,
        HandDrawn = Settings.HandDrawn
      };
    }

    public static MapTheme Load(string fileName) {
      var theme = JsonConvert.DeserializeObject<MapTheme>(File.ReadAllText(fileName));
      if (theme == null) throw new InvalidDataException("The file does not contain a theme.");
      theme.Validate();
      return theme;
    }

    public void Save(string fileName) {
      Validate();
      File.WriteAllText(fileName, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public static bool HasIndividualStyles(Project project) {
      var defaultCorners = new CornerRadii();
      if (project.Elements.OfType<Room>().Any(room =>
        room.RoomBorderColor != Color.Transparent || room.RoomFillColor != Color.Transparent ||
        room.RoomNameColor != Color.Transparent || room.RoomSubtitleColor != Color.Transparent ||
        room.RoomObjectTextColor != Color.Transparent || room.SecondFillColor != Color.Transparent ||
        room.Shape != Settings.DefaultRoomShape || room.BorderStyle != BorderDashStyle.Solid ||
        room.HandDrawnStyle != HandDrawnStyle.MapDefault || !room.AllCornersEqual ||
        room.Corners.TopLeft != defaultCorners.TopLeft || room.Corners.TopRight != defaultCorners.TopRight ||
        room.Corners.BottomLeft != defaultCorners.BottomLeft || room.Corners.BottomRight != defaultCorners.BottomRight))
        return true;

      if (project.Elements.OfType<Connection>().Any(connection => connection.ConnectionColor != Color.Transparent))
        return true;

      var fill = Settings.Regions.FirstOrDefault(region => region.RegionName == Region.DefaultRegion)?.RColor ?? Color.White;
      return project.Elements.OfType<MapLabel>().Any(label =>
        !IsDefaultLabelColor(label.TextColor, Color.Black, Settings.Color[Domain.Misc.Colors.LineText]) ||
        !IsDefaultLabelColor(label.BorderColor, Color.Black, Settings.Color[Domain.Misc.Colors.Border]) ||
        !IsDefaultLabelColor(label.BackgroundColor, Color.White, fill));
    }

    private static bool IsDefaultLabelColor(Color color, Color initial, Color themed) {
      return color.ToArgb() == initial.ToArgb() || color.ToArgb() == themed.ToArgb();
    }

    public void Apply(bool replaceIndividualStyles) {
      Validate();
      // Resolve every value before changing the document, including font availability.
      var colors = Colors.ToDictionary(pair => pair.Key, pair => ParseColor(pair.Value));
      var regions = Regions.Select(region => new Region {
        RegionName = region.Name, RColor = ParseColor(region.Fill), TextColor = ParseColor(region.Text)
      }).ToList();
      var fonts = new List<Font>();
      var fontsCreated = false;
      try {
        foreach (var font in new[] { RoomFont, ObjectFont, SubtitleFont, LineFont })
          fonts.Add(font.CreateFont());
        fontsCreated = true;
      } finally {
        if (!fontsCreated)
          foreach (var font in fonts) font.Dispose();
      }

      foreach (var pair in colors) {
        Domain.Misc.Colors.FromName(pair.Key, out var index);
        Settings.Color[index] = pair.Value;
      }

      // Region membership is map content. Merge palettes without deleting map-only regions.
      foreach (var region in regions) {
        var existing = Settings.Regions.FirstOrDefault(item => string.Equals(item.RegionName, region.RegionName, StringComparison.OrdinalIgnoreCase));
        if (existing == null) Settings.Regions.Add(region);
        else {
          existing.RColor = region.RColor;
          existing.TextColor = region.TextColor;
        }
      }

      Settings.RoomNameFont = ReuseFont(fonts[0], Settings.RoomNameFont);
      Settings.ObjectFont = ReuseFont(fonts[1], Settings.ObjectFont);
      Settings.SubtitleFont = ReuseFont(fonts[2], Settings.SubtitleFont);
      Settings.LineFont = ReuseFont(fonts[3], Settings.LineFont);
      Settings.LineWidth = LineWidth;
      Settings.ConnectionArrowSize = ArrowSize;
      Settings.TextOffsetFromConnection = TextOffset;
      Settings.DarknessStripeSize = DarknessStripeSize;
      Settings.ObjectListOffsetFromRoom = ObjectListOffset;
      Settings.ConnectionStalkLength = ConnectionStalkLength;
      Settings.DefaultRoomShape = DefaultRoomShape;
      Settings.GridSize = GridSize;
      Settings.IsGridVisible = GridVisible;
      Settings.ShowOrigin = ShowOrigin;
      Settings.DocumentSpecificMargins = DocumentSpecificMargins;
      Settings.DocHorizontalMargin = HorizontalMargin;
      Settings.DocVerticalMargin = VerticalMargin;
      Settings.WrapTextAtDashes = WrapTextAtDashes;
      Settings.HandDrawn = HandDrawn;

      if (replaceIndividualStyles) {
        foreach (var room in Project.Current.Elements.OfType<Room>()) {
          room.RoomBorderColor = room.RoomFillColor = room.RoomNameColor =
            room.RoomSubtitleColor = room.RoomObjectTextColor = room.SecondFillColor = Color.Transparent;
          room.Shape = DefaultRoomShape;
          room.StraightEdges = DefaultRoomShape == RoomShape.SquareCorners;
          room.Corners = new CornerRadii();
          room.AllCornersEqual = true;
          room.HandDrawnStyle = HandDrawnStyle.MapDefault;
          room.BorderStyle = BorderDashStyle.Solid;
        }
        foreach (var connection in Project.Current.Elements.OfType<Connection>())
          connection.ConnectionColor = Color.Transparent;
        foreach (var label in Project.Current.Elements.OfType<MapLabel>()) {
          label.TextColor = colors["lineText"];
          label.BorderColor = colors["border"];
          label.BackgroundColor = regions.Single(region => region.RegionName == Region.DefaultRegion).RColor;
        }
      }

      Project.Current.IsDirty = true;
      Settings.NotifyThemeApplied();
    }

    public void Validate() {
      if (Format != "trizbort-theme" || Version != 1)
        throw new InvalidDataException("This is not a supported Trizbort theme (expected version 1).");
      if (string.IsNullOrWhiteSpace(Name)) throw new InvalidDataException("The theme needs a name.");
      if (Colors == null || Colors.Count != Domain.Misc.Colors.Count)
        throw new InvalidDataException("The theme must include all map colours.");
      foreach (var pair in Colors) {
        if (!Domain.Misc.Colors.FromName(pair.Key, out _))
          throw new InvalidDataException($"Unknown theme colour: {pair.Key}.");
        ParseColor(pair.Value);
      }
      for (var index = 0; index < Domain.Misc.Colors.Count; index++) {
        Domain.Misc.Colors.ToName(index, out var name);
        if (!Colors.ContainsKey(name)) throw new InvalidDataException($"Missing theme colour: {name}.");
      }
      if (Regions == null || Regions.Count == 0 || Regions.Any(region => region == null || !Region.ValidRegionName(region.Name)) ||
          Regions.Select(region => region.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Regions.Count ||
          Regions.All(region => region.Name != Region.DefaultRegion))
        throw new InvalidDataException("The theme needs uniquely named regions, including NoRegion.");
      foreach (var region in Regions) {
        ParseColor(region.Fill);
        ParseColor(region.Text);
      }
      foreach (var font in new[] { RoomFont, ObjectFont, SubtitleFont, LineFont }) {
        if (font == null) throw new InvalidDataException("The theme must include all four fonts.");
        font.Validate();
      }
      if (!Enum.IsDefined(typeof(RoomShape), DefaultRoomShape))
        throw new InvalidDataException("Invalid default room shape.");
      ValidateRange(LineWidth, 0, 100, "line width");
      ValidateRange(GridSize, 2, 4096, "grid size");
      ValidateRange(ArrowSize, 0, 4096, "arrow size");
      ValidateRange(TextOffset, 0, 4096, "text offset");
      ValidateRange(DarknessStripeSize, 0, 4096, "darkness stripe size");
      ValidateRange(ObjectListOffset, 0, 4096, "object list offset");
      ValidateRange(ConnectionStalkLength, 0, 4096, "connection stalk length");
      ValidateRange(HorizontalMargin, 0, 4096, "horizontal margin");
      ValidateRange(VerticalMargin, 0, 4096, "vertical margin");
    }

    internal static Color ParseColor(string value) {
      if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("Empty theme colour.");
      try {
        return ColorTranslator.FromHtml(value);
      } catch (ArgumentException exception) {
        throw new InvalidDataException($"Invalid theme colour: {value}.", exception);
      }
    }

    private static Font ReuseFont(Font candidate, Font current) {
      if (!Equals(candidate, current)) return candidate;
      candidate.Dispose();
      return current;
    }

    internal static void ValidateRange(float value, float min, float max, string name) {
      if (!float.IsFinite(value) || value < min || value > max)
        throw new InvalidDataException($"Theme {name} must be between {min} and {max}.");
    }

    public static IReadOnlyList<MapTheme> BuiltInThemes() {
      return new[] {
        CreateBuiltIn("Classic", Color.White, Color.White, Color.MidnightBlue, Color.LightGray, RoomShape.SquareCorners, "Arial"),
        CreateBuiltIn("Parchment", Color.FromArgb(245, 233, 205), Color.FromArgb(255, 248, 226),
          Color.FromArgb(83, 57, 35), Color.FromArgb(221, 205, 174), RoomShape.RoundedCorners, "Georgia"),
        CreateBuiltIn("Dark", Color.FromArgb(30, 33, 39), Color.FromArgb(48, 53, 62),
          Color.FromArgb(225, 230, 238), Color.FromArgb(65, 70, 80), RoomShape.RoundedCorners, "Segoe UI"),
        CreateBuiltIn("High contrast", Color.White, Color.White, Color.Black, Color.LightGray, RoomShape.SquareCorners, "Arial"),
        CreateBuiltIn("Sketch", Color.White, Color.White, Color.FromArgb(60, 60, 60), Color.FromArgb(232, 232, 232), RoomShape.SquareCorners, "Segoe Print", true)
      };
    }

    private static MapTheme CreateBuiltIn(string name, Color canvas, Color fill, Color ink, Color grid, RoomShape shape, string fontName, bool handDrawn = false) {
      var colors = new Dictionary<string, string>();
      for (var index = 0; index < Domain.Misc.Colors.Count; index++) {
        Domain.Misc.Colors.ToName(index, out var colorName);
        colors[colorName] = ColorTranslator.ToHtml(ink);
      }
      colors["canvas"] = ColorTranslator.ToHtml(canvas);
      colors["grid"] = ColorTranslator.ToHtml(grid);
      colors["selectedLine"] = "Gold";
      colors["hoverLine"] = "DarkOrange";
      colors["startRoom"] = "GreenYellow";
      colors["endRoom"] = "Red";
      return new MapTheme {
        Name = name, Colors = colors,
        Regions = new List<ThemeRegion> { new ThemeRegion {
          Name = Region.DefaultRegion, Fill = ColorTranslator.ToHtml(fill), Text = ColorTranslator.ToHtml(ink)
        } },
        RoomFont = new ThemeFont { Name = fontName, Size = 13 },
        ObjectFont = new ThemeFont { Name = fontName, Size = 11 },
        SubtitleFont = new ThemeFont { Name = fontName, Size = 9 },
        LineFont = new ThemeFont { Name = fontName, Size = 9 },
        LineWidth = 2, ArrowSize = 12, TextOffset = 4, DarknessStripeSize = 24,
        ObjectListOffset = 4, ConnectionStalkLength = 32, DefaultRoomShape = shape,
        GridSize = 32, GridVisible = true, ShowOrigin = true, WrapTextAtDashes = true, HandDrawn = handDrawn
      };
    }
  }

  [JsonObject(ItemRequired = Required.Always)]
  public sealed class ThemeRegion {
    public string Name { get; set; }
    public string Fill { get; set; }
    public string Text { get; set; }
  }

  [JsonObject(ItemRequired = Required.Always)]
  public sealed class ThemeFont {
    public string Name { get; set; }
    public float Size { get; set; }
    public FontStyle Style { get; set; }

    public static ThemeFont Capture(Font font) {
      return new ThemeFont { Name = font.FontFamily.Name, Size = font.Size, Style = font.Style };
    }

    public void Validate() {
      if (string.IsNullOrWhiteSpace(Name)) throw new InvalidDataException("Empty theme font name.");
      MapTheme.ValidateRange(Size, 2, 256, "font size");
      if ((Style & ~(FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout)) != 0)
        throw new InvalidDataException("Invalid theme font style.");
    }

    public Font CreateFont() {
      var font = new Font(Name, Size, Style, GraphicsUnit.World);
      if (!string.Equals(font.FontFamily.Name, Name, StringComparison.OrdinalIgnoreCase)) {
        font.Dispose();
        throw new InvalidDataException($"The theme font '{Name}' is not installed on this computer.");
      }
      return font;
    }
  }
}
