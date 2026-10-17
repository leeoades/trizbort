using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Util;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Setup;

[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
public static class Settings
{
  private const float MinFontSize = 2;

  private const float MaxFontSize = 256;

  // per-map settings, saved with the map
  private static readonly Color[] _color = new Color[Colors.Count];
  private static ThemeFont _roomFontMeta = new() { Name = "Arial", Size = 13.0f, Style = FontStyle.Regular };
  private static ThemeFont _objectFontMeta = new() { Name = "Arial", Size = 11.0f, Style = FontStyle.Regular };
  private static ThemeFont _subtitleFontMeta = new() { Name = "Arial", Size = 9.0f, Style = FontStyle.Regular };
  private static ThemeFont _lineFontMeta = new() { Name = "Arial", Size = 9.0f, Style = FontStyle.Regular };
  private static Font _roomNameFont;
  private static Font _objectFont;
  private static Font _subtitleFont;
  private static Font _lineFont;
  private static float _lineWidth;
  private static bool _handDrawn;
  private static bool _snapToGrid;
  private static bool _isGridVisible;
  private static bool _showOrigin;
  private static float _gridSize;
  private static float _darknessStripeSize;
  private static float _objectListOffsetFromRoom;
  private static float _connectionStalkLength;
  private static float _preferredDistanceBetweenRooms;
  private static float _textOffsetFromConnection;
  private static float _handleSize;
  private static float _snapToElementSize;
  private static bool _docSpecificMargins;
  private static float _docHorizontalMargin;
  private static float _docVerticalMargin;
  private static bool _wrapTextAtDashes;
  private static float _dragDistanceToInitiateNewConnection;
  private static float _connectionArrowSize;
  private static ModifierKeys _keypadNavigationCreationModifier;

  private static ModifierKeys _keypadNavigationUnexploredModifier;

  static Settings()
  {
    Color = new ColorSettings();
    Regions = new List<Region> {
      new() { RegionName = Region.DefaultRegion, RColor = System.Drawing.Color.White }
    };
    Reset();
  }


  public static ColorSettings Color { get; }

  public static float ConnectionArrowSize {
    get { return _connectionArrowSize; }
    set {
      if (_connectionArrowSize != value)
      {
        _connectionArrowSize = value;
        RaiseChanged();
      }
    }
  }

  public static float ConnectionStalkLength {
    get { return _connectionStalkLength; }
    set {
      if (_connectionStalkLength != value)
      {
        _connectionStalkLength = value;
        RaiseChanged();
      }
    }
  }

  public static float DarknessStripeSize {
    get { return _darknessStripeSize; }
    set {
      if (_darknessStripeSize != value)
      {
        _darknessStripeSize = value;
        RaiseChanged();
      }
    }
  }

  public static string DefaultRoomName { get; set; } = "Cave";
  public static RoomShape DefaultRoomShape { get; set; }

  public static float DocHorizontalMargin {
    get { return _docHorizontalMargin; }
    set {
      if (_docHorizontalMargin != value)
      {
        _docHorizontalMargin = value;
        RaiseChanged();
      }
    }
  }

  public static float DocVerticalMargin {
    get { return _docVerticalMargin; }
    set {
      if (_docVerticalMargin != value)
      {
        _docVerticalMargin = value;
        RaiseChanged();
      }
    }
  }

  public static bool DocumentSpecificMargins {
    get { return _docSpecificMargins; }
    set {
      if (_docSpecificMargins != value)
      {
        _docSpecificMargins = value;
        RaiseChanged();
      }
    }
  }

  public static float DragDistanceToInitiateNewConnection {
    get { return _dragDistanceToInitiateNewConnection; }
    set {
      if (_dragDistanceToInitiateNewConnection != value)
      {
        _dragDistanceToInitiateNewConnection = value;
        RaiseChanged();
      }
    }
  }

  public static bool EndRoomLoaded { get; set; }

  public static float GridSize {
    get { return _gridSize; }
    set {
      if (_gridSize != value)
      {
        _gridSize = value;
        RaiseChanged();
      }
    }
  }

  public static float HandleSize {
    get { return _handleSize; }
    set {
      if (_handleSize != value)
      {
        _handleSize = value;
        RaiseChanged();
      }
    }
  }

  public static bool HandDrawn {
    get { return _handDrawn; }
    set {
      if (_handDrawn != value)
      {
        _handDrawn = value;
        RaiseChanged();
      }
    }
  }

  public static bool IsGridVisible {
    get { return _isGridVisible; }
    set {
      if (_isGridVisible != value)
      {
        _isGridVisible = value;
        RaiseChanged();
      }
    }
  }

  public static ModifierKeys KeypadNavigationCreationModifier {
    get { return _keypadNavigationCreationModifier; }
    set {
      if (_keypadNavigationCreationModifier != value)
      {
        _keypadNavigationCreationModifier = value;
        RaiseChanged();
      }
    }
  }

  public static ModifierKeys KeypadNavigationUnexploredModifier {
    get { return _keypadNavigationUnexploredModifier; }
    set {
      if (_keypadNavigationUnexploredModifier != value)
      {
        _keypadNavigationUnexploredModifier = value;
        RaiseChanged();
      }
    }
  }

  public static ThemeFont RoomFontMetadata {
    get => _roomFontMeta;
    set => SetMetadata(ref _roomFontMeta, ref _roomNameFont, value, 13.0f);
  }

  public static ThemeFont ObjectFontMetadata {
    get => _objectFontMeta;
    set => SetMetadata(ref _objectFontMeta, ref _objectFont, value, 11.0f);
  }

  public static ThemeFont SubtitleFontMetadata {
    get => _subtitleFontMeta;
    set => SetMetadata(ref _subtitleFontMeta, ref _subtitleFont, value, 9.0f);
  }

  public static ThemeFont LineFontMetadata {
    get => _lineFontMeta;
    set => SetMetadata(ref _lineFontMeta, ref _lineFont, value, 9.0f);
  }

  public static Font LineFont {
    get { return GetFont(ref _lineFont, _lineFontMeta); }
    set { SetFont(ref _lineFontMeta, ref _lineFont, value); }
  }

  public static float LineWidth {
    get { return _lineWidth; }
    set {
      if (_lineWidth != value)
      {
        _lineWidth = value;
        RaiseChanged();
      }
    }
  }

  public static bool MinMaxHanddrawn { get; set; }

  public static Font ObjectFont {
    get { return GetFont(ref _objectFont, _objectFontMeta); }
    set { SetFont(ref _objectFontMeta, ref _objectFont, value); }
  }

  public static float ObjectListOffsetFromRoom {
    get { return _objectListOffsetFromRoom; }
    set {
      if (_objectListOffsetFromRoom != value)
      {
        _objectListOffsetFromRoom = value;
        RaiseChanged();
      }
    }
  }

  public static float PreferredDistanceBetweenRooms {
    get { return _preferredDistanceBetweenRooms; }
    set {
      if (_preferredDistanceBetweenRooms != value)
      {
        _preferredDistanceBetweenRooms = value;
        RaiseChanged();
      }
    }
  }

  public static List<Region> Regions { get; set; }

  public static Font RoomNameFont {
    get { return GetFont(ref _roomNameFont, _roomFontMeta); }
    set { SetFont(ref _roomFontMeta, ref _roomNameFont, value); }
  }

  public static bool ShowOrigin {
    get { return _showOrigin; }
    set {
      if (_showOrigin != value)
      {
        _showOrigin = value;
        RaiseChanged();
      }
    }
  }

  public static float SnapToElementSize {
    get { return _snapToElementSize; }
    set {
      if (_snapToElementSize != value)
      {
        _snapToElementSize = value;
        RaiseChanged();
      }
    }
  }

  public static bool SnapToGrid {
    get { return _snapToGrid; }
    set {
      if (_snapToGrid != value)
      {
        _snapToGrid = value;
        RaiseChanged();
      }
    }
  }

  public static bool StartRoomLoaded { get; set; }

  public static Font SubtitleFont {
    get { return GetFont(ref _subtitleFont, _subtitleFontMeta); }
    set { SetFont(ref _subtitleFontMeta, ref _subtitleFont, value); }
  }

  public static float TextOffsetFromConnection {
    get { return _textOffsetFromConnection; }
    set {
      if (_textOffsetFromConnection != value)
      {
        _textOffsetFromConnection = value;
        RaiseChanged();
      }
    }
  }

  public static bool WrapTextAtDashes {
    get { return _wrapTextAtDashes; }
    set {
      if (_wrapTextAtDashes != value)
      {
        _wrapTextAtDashes = value;
        RaiseChanged();
        WrappingChanged = true;
      }
    }
  }

  public static bool WrappingChanged { get; set; }

  public static bool DebugDisableLineRendering { get; set; }

  public static float TextPadding => 4;

  public static event EventHandler Changed;

  public static void NotifyThemeApplied()
  {
    RaiseChanged();
  }

  public static void Load(XmlElementReader element)
  {
    var colors = element["colors"];
    foreach (var color in colors.Children)
      if (Colors.FromName(color.Name, out var index))
        Color[index] = color.ToColor(Color[index]);

    var regions = element["regions"];
    Regions = new List<Region>();

    if (regions.Children.Count <= 0)
      Regions.Add(
        new Region {
          RColor = System.Drawing.Color.White, TextColor = System.Drawing.Color.Blue, RegionName = Region.DefaultRegion
        });
    else
      foreach (var region in regions.Children)
      {
        var tRegion = new Region {
          TextColor = region.Attribute("TextColor").Text == string.Empty
            ? System.Drawing.Color.Blue
            : ColorTranslator.FromHtml(region.Attribute("TextColor").Text)
        };

        var node = region.Attribute("name");
        tRegion.RegionName = node != null && node.Text != string.Empty ? node.Text : region.Name;

        tRegion.RegionName = tRegion.ClearRegionNameObfuscation();
        tRegion.RColor = region.ToColor(System.Drawing.Color.White);
        Regions.Add(tRegion);
      }

    var fonts = element["fonts"];
    foreach (var font in fonts.Children)
    {
      var style = FontStyle.Regular;
      if (font.Attribute("bold").ToBool()) style |= FontStyle.Bold;
      if (font.Attribute("italic").ToBool()) style |= FontStyle.Italic;
      if (font.Attribute("underline").ToBool()) style |= FontStyle.Underline;
      if (font.Attribute("strikeout").ToBool()) style |= FontStyle.Strikeout;
      if (font.Name == "room")
        RoomFontMetadata = ReadFont(font, _roomFontMeta, 13.0f, style);
      else if (font.Name == "object")
        ObjectFontMetadata = ReadFont(font, _objectFontMeta, 11.0f, style);
      else if (font.Name == "subTitle" || font.Name == "subtitle")
        SubtitleFontMetadata = ReadFont(font, _subtitleFontMeta, 9.0f, style);
      else if (font.Name == "line")
        LineFontMetadata = ReadFont(font, _lineFontMeta, 9.0f, style);
    }

    SnapToGrid = element["grid"]["snapTo"].ToBool(_snapToGrid);
    IsGridVisible = element["grid"]["visible"].ToBool(_isGridVisible);
    GridSize = element["grid"]["size"].ToFloat(_gridSize);
    ShowOrigin = element["grid"]["showOrigin"].ToBool(_showOrigin);

    LineWidth = element["lines"]["width"].ToFloat(_lineWidth);
    ConnectionArrowSize = element["lines"]["arrowSize"].ToFloat(_connectionArrowSize);
    TextOffsetFromConnection = element["lines"]["textOffset"].ToFloat(_textOffsetFromConnection);
    HandDrawn = element["lines"]["handDrawn"].ToBool(false);

    DarknessStripeSize = element["rooms"]["darknessStripeSize"].ToFloat(_darknessStripeSize);
    ObjectListOffsetFromRoom = element["rooms"]["objectListOffset"].ToFloat(_objectListOffsetFromRoom);
    ConnectionStalkLength = element["rooms"]["connectionStalkLength"].ToFloat(_connectionStalkLength);
    PreferredDistanceBetweenRooms =
      element["rooms"]["preferredDistanceBetweenRooms"]
        .ToFloat(_connectionStalkLength * 2);

    DefaultRoomShape = (RoomShape)element["rooms"]["defaultRoomShape"].ToInt();
    DefaultRoomName = element["rooms"]["defaultRoomName"].Text;
    if (string.IsNullOrEmpty(DefaultRoomName))
      DefaultRoomName = "Cave";

    HandleSize = element["ui"]["handleSize"].ToFloat(_handleSize);
    SnapToElementSize = element["ui"]["snapToElementSize"].ToFloat(_snapToElementSize);

    DocumentSpecificMargins = element["margins"]["documentSpecific"].ToBool(_docSpecificMargins);
    DocHorizontalMargin = element["margins"]["horizontal"].ToFloat(_docHorizontalMargin);
    DocVerticalMargin = element["margins"]["vertical"].ToFloat(_docVerticalMargin);
    WrapTextAtDashes = element["margins"]["wrapDashes"].ToBool(true);

    KeypadNavigationCreationModifier = StringToModifierKeys(
      element["keypadNavigation"]["creationModifier"].Text,
      _keypadNavigationCreationModifier);
    KeypadNavigationUnexploredModifier = StringToModifierKeys(
      element["keypadNavigation"]["unexploredModifier"].Text,
      _keypadNavigationUnexploredModifier);
  }

  public static void MutateFonts(Func<Font, Font> mutator)
  {
    RoomNameFont = mutator(RoomNameFont);
    ObjectFont = mutator(ObjectFont);
    SubtitleFont = mutator(SubtitleFont);
    LineFont = mutator(LineFont);
  }

  public static void Reset()
  {
    Reset(true);
  }

  internal static void Reset(bool resetDocumentMetadata)
  {
    Color[Colors.Canvas] = System.Drawing.Color.White;
    Color[Colors.Border] = System.Drawing.Color.MidnightBlue;
    Color[Colors.Line] = System.Drawing.Color.MidnightBlue;
    Color[Colors.HoverLine] = System.Drawing.Color.DarkOrange;
    Color[Colors.SelectedLine] = System.Drawing.Color.Gold;
    Color[Colors.Subtitle] = System.Drawing.Color.MidnightBlue;
    Color[Colors.SmallText] = System.Drawing.Color.MidnightBlue;
    Color[Colors.LineText] = System.Drawing.Color.MidnightBlue;
    Color[Colors.Grid] = Colors.Mix(System.Drawing.Color.White, System.Drawing.Color.Black, 10, 1);
    Color[Colors.StartRoom] = System.Drawing.Color.GreenYellow;
    Color[Colors.EndRoom] = System.Drawing.Color.Red;

    if (resetDocumentMetadata)
      Project.Current.Title = Project.Current.Author = Project.Current.History = Project.Current.Description = "";

    var defaultFont = DefaultFontFamilyName();
    RoomFontMetadata = new ThemeFont { Name = defaultFont, Size = 13.0f, Style = FontStyle.Regular };
    ObjectFontMetadata = new ThemeFont { Name = defaultFont, Size = 11.0f, Style = FontStyle.Regular };
    SubtitleFontMetadata = new ThemeFont { Name = defaultFont, Size = 9.0f, Style = FontStyle.Regular };
    LineFontMetadata = new ThemeFont { Name = defaultFont, Size = 9.0f, Style = FontStyle.Regular };

    DocumentSpecificMargins = ApplicationSettingsController.AppSettings.SpecifyGenMargins;

    if (ApplicationSettingsController.AppSettings.SpecifyGenMargins)
    {
      DocHorizontalMargin = ApplicationSettingsController.AppSettings.GenHorizontalMargin;
      DocVerticalMargin = ApplicationSettingsController.AppSettings.GenVerticalMargin;
    }
    else
    {
      DocHorizontalMargin = DocVerticalMargin = 0;
    }

    WrapTextAtDashes = ApplicationSettingsController.AppSettings.SpecifyWrapping;

    LineWidth = 2.0f;
    HandDrawn = false;

    SnapToGrid = true;
    IsGridVisible = true;
    GridSize = 32.0f;
    ShowOrigin = true;

    StartRoomLoaded = false;
    EndRoomLoaded = false;

    DarknessStripeSize = 24.0f;
    ObjectListOffsetFromRoom = 4.0f;

    ConnectionStalkLength = 32.0f;
    PreferredDistanceBetweenRooms = ConnectionStalkLength * 2;
    TextOffsetFromConnection = 4.0f;
    HandleSize = 12.0f;
    SnapToElementSize = 16.0f;
    DragDistanceToInitiateNewConnection = 32f;
    ConnectionArrowSize = 12.0f;

    KeypadNavigationCreationModifier = ModifierKeys.Control;
    KeypadNavigationUnexploredModifier = ModifierKeys.Alt;

    Regions = new List<Region> {
      new() { RegionName = Region.DefaultRegion, RColor = System.Drawing.Color.White }
    };
  }

  public static void Save(XmlScribe scribe)
  {
    scribe.StartElement("colors");
    for (var index = 0; index < Colors.Count; ++index)
      if (Colors.ToName(index, out var colorName))
        scribe.Element(colorName, Color[index]);
    scribe.EndElement();

    scribe.StartElement("regions");
    foreach (var region in Regions.OrderBy(p => p.RegionName))
    {
      scribe.StartElement(region.FixupRegionNameForSave());
      scribe.Attribute("Name", region.RegionName);
      scribe.Attribute("TextColor", region.TextColor);
      scribe.Value(region.RColor);
      scribe.EndElement();
    }
    scribe.EndElement();

    scribe.StartElement("fonts");
    SaveFont(scribe, _roomFontMeta, "room");
    SaveFont(scribe, _objectFontMeta, "object");
    SaveFont(scribe, _subtitleFontMeta, "subTitle");
    SaveFont(scribe, _lineFontMeta, "line");
    scribe.EndElement();

    scribe.StartElement("grid");
    scribe.Element("snapTo", _snapToGrid);
    scribe.Element("visible", _isGridVisible);
    scribe.Element("showOrigin", _showOrigin);
    scribe.Element("size", _gridSize);
    scribe.EndElement();

    scribe.StartElement("lines");
    scribe.Element("width", _lineWidth);
    scribe.Element("arrowSize", _connectionArrowSize);
    scribe.Element("textOffset", _textOffsetFromConnection);
    scribe.Element("handDrawn", _handDrawn);
    scribe.EndElement();

    scribe.StartElement("rooms");
    scribe.Element("darknessStripeSize", _darknessStripeSize);
    scribe.Element("objectListOffset", _objectListOffsetFromRoom);
    scribe.Element("connectionStalkLength", _connectionStalkLength);
    scribe.Element("preferredDistanceBetweenRooms", _preferredDistanceBetweenRooms);
    scribe.Element("defaultRoomName", DefaultRoomName);
    scribe.Element("defaultRoomShape", (int)DefaultRoomShape);
    scribe.EndElement();

    scribe.StartElement("ui");
    scribe.Element("handleSize", _handleSize);
    scribe.Element("snapToElementSize", _snapToElementSize);
    scribe.EndElement();

    scribe.StartElement("margins");
    scribe.Element("documentSpecific", _docSpecificMargins);
    scribe.Element("horizontal", _docHorizontalMargin);
    scribe.Element("vertical", _docVerticalMargin);
    scribe.Element("wrapDashes", _wrapTextAtDashes);
    scribe.EndElement();

    scribe.StartElement("keypadNavigation");
    scribe.Element("creationModifier", ModifierKeysToString(_keypadNavigationCreationModifier));
    scribe.Element("unexploredModifier", ModifierKeysToString(_keypadNavigationUnexploredModifier));
    scribe.EndElement();
  }

  public static float Snap(float value)
  {
    float offset = 0;
    while (value < GridSize)
    {
      value += GridSize;
      offset += GridSize;
    }

    var mod = value % GridSize;
    if (SnapToGrid && mod != 0)
      if (mod < GridSize / 2)
        value -= mod;
      else
        value = value + GridSize - mod;

    return value - offset;
  }

  public static Vector Snap(Vector pos)
  {
    if (SnapToGrid)
    {
      pos.X = Snap(pos.X);
      pos.Y = Snap(pos.Y);
    }

    return pos;
  }

  private static void SaveFont(XmlScribe scribe, ThemeFont font, string name)
  {
    if (font == null) return;
    scribe.StartElement(name);
    scribe.Attribute("size", font.Size);
    if ((font.Style & FontStyle.Bold) == FontStyle.Bold)
      scribe.Attribute("bold", true);
    if ((font.Style & FontStyle.Italic) == FontStyle.Italic)
      scribe.Attribute("italic", true);
    if ((font.Style & FontStyle.Underline) == FontStyle.Underline)
      scribe.Attribute("underline", true);
    if ((font.Style & FontStyle.Strikeout) == FontStyle.Strikeout)
      scribe.Attribute("strikeout", true);
    scribe.Value(font.Name);
    scribe.EndElement();
  }

  private static ThemeFont ReadFont(XmlElementReader font, ThemeFont current, float defaultSize, FontStyle style)
  {
    return new ThemeFont {
      Name = font.ToText(current?.Name ?? DefaultFontFamilyName()),
      Size = Numeric.Clamp(font.Attribute("size").ToFloat(current?.Size ?? defaultSize), MinFontSize, MaxFontSize),
      Style = style
    };
  }

  private static string DefaultFontFamilyName()
  {
    var name = ApplicationSettingsController.AppSettings.DefaultFontName;
    return string.IsNullOrEmpty(name) ? "Arial" : name;
  }

  private static Font GetFont(ref Font cache, ThemeFont meta)
  {
    if (cache != null) return cache;
    if (!OperatingSystem.IsWindows() || meta == null) return null;
    cache = new Font(meta.Name, meta.Size, meta.Style, GraphicsUnit.World);
    return cache;
  }

  private static void SetMetadata(ref ThemeFont meta, ref Font cache, ThemeFont value, float defaultSize)
  {
    meta = value?.Clone() ?? new ThemeFont {
      Name = DefaultFontFamilyName(), Size = defaultSize, Style = FontStyle.Regular
    };
    cache = null;
    RaiseChanged();
  }

  private static void SetFont(ref ThemeFont meta, ref Font cache, Font value)
  {
    if (value != null)
      meta = ThemeFont.Capture(value);
    if (!Equals(cache, value))
    {
      cache = value;
      RaiseChanged();
    }
  }

  private static string ModifierKeysToString(ModifierKeys key)
  {
    var builder = new StringBuilder();
    if ((key & ModifierKeys.Shift) == ModifierKeys.Shift)
    {
      if (builder.Length != 0)
        builder.Append('|');
      builder.Append("shift");
    }

    if ((key & ModifierKeys.Control) == ModifierKeys.Control)
    {
      if (builder.Length != 0)
        builder.Append('|');
      builder.Append("control");
    }

    if ((key & ModifierKeys.Alt) == ModifierKeys.Alt)
    {
      if (builder.Length != 0)
        builder.Append('|');
      builder.Append("alt");
    }

    if (builder.Length == 0)
      builder.Append("none");

    return builder.ToString();
  }

  private static void RaiseChanged()
  {
    var changed = Changed;
    changed?.Invoke(null, EventArgs.Empty);
  }

  private static ModifierKeys StringToModifierKeys(string text, ModifierKeys defaultValue)
  {
    if (string.IsNullOrEmpty(text)) return defaultValue;

    var value = ModifierKeys.None;
    foreach (var part in text.Split('|'))
      if (StringComparer.InvariantCultureIgnoreCase.Compare(part, "shift") == 0)
        value |= ModifierKeys.Shift;
      else if (StringComparer.InvariantCultureIgnoreCase.Compare(part, "control") == 0)
        value |= ModifierKeys.Control;
      else if (StringComparer.InvariantCultureIgnoreCase.Compare(part, "alt") == 0) value |= ModifierKeys.Alt;
    return value;
  }

  public class ColorSettings
  {
    public Color this[int index] {
      get { return _color[index]; }
      set {
        if (_color[index] != value)
        {
          _color[index] = value;
          RaiseChanged();
        }
      }
    }
  }
}
