using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.UI;
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
  private static Keys _keypadNavigationCreationModifier;

  private static Keys _keypadNavigationUnexploredModifier;

  // application settings, saved for the user
  // TODO: private static AutomapSettings sAutomap;

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

  public static bool DocumentSpecificMargins {
    get { return _docSpecificMargins; }
    set {
      if (_docSpecificMargins == value) return;
      _docSpecificMargins = value;
      RaiseChanged();
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

  public static bool WrapTextAtDashes {
    get { return _wrapTextAtDashes; }
    set {
      if (_wrapTextAtDashes == value) return;
      _wrapTextAtDashes = value;
      WrappingChanged = true;
      RaiseChanged();
    }
  }

  public static bool WrappingChanged { get; set; }

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

  /// <summary>
  ///   Get/set the modifier keys required, along with a numeric keypad key,
  ///   to create new rooms from the currently selected room.
  /// </summary>
  public static Keys KeypadNavigationCreationModifier {
    get { return _keypadNavigationCreationModifier; }
    set {
      if (_keypadNavigationCreationModifier != value)
      {
        _keypadNavigationCreationModifier = value;
        RaiseChanged();
      }
    }
  }

  /// <summary>
  ///   Get/set the modifier keys required, along with a numeric keypad key,
  ///   to mark "unexplored" connections from the currently selected room.
  /// </summary>
  public static Keys KeypadNavigationUnexploredModifier {
    get { return _keypadNavigationUnexploredModifier; }
    set {
      if (_keypadNavigationUnexploredModifier != value)
      {
        _keypadNavigationUnexploredModifier = value;
        RaiseChanged();
      }
    }
  }

  public static Font LineFont {
    get { return _lineFont; }
    set {
      if (!Equals(_lineFont, value))
      {
        _lineFont = value;
        RaiseChanged();
      }
    }
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

  /// <summary>Map-wide hand-drawn style for room outlines, labels and connections.</summary>
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

  public static Font ObjectFont {
    get { return _objectFont; }
    set {
      if (!Equals(_objectFont, value))
      {
        _objectFont = value;
        RaiseChanged();
      }
    }
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

  public static List<Region> Regions { get; private set; }

  public static Font RoomNameFont {
    get { return _roomNameFont; }
    set {
      if (!Equals(_roomNameFont, value))
      {
        _roomNameFont = value;
        RaiseChanged();
      }
    }
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
    get { return _subtitleFont; }
    set {
      if (!Equals(_subtitleFont, value))
      {
        _subtitleFont = value;
        RaiseChanged();
      }
    }
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

  public static event EventHandler Changed;

  internal static void NotifyThemeApplied()
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
        RoomNameFont = new Font(
          font.ToText(RoomNameFont.Name),
          Numeric.Clamp(font.Attribute("size").ToFloat(RoomNameFont.Size), MinFontSize, MaxFontSize),
          style,
          GraphicsUnit.World);
      else if (font.Name == "object")
        ObjectFont = new Font(
          font.ToText(ObjectFont.Name),
          Numeric.Clamp(font.Attribute("size").ToFloat(ObjectFont.Size), MinFontSize, MaxFontSize),
          style,
          GraphicsUnit.World);
      else if (font.Name == "subTitle")
        SubtitleFont = new Font(
          font.ToText(SubtitleFont.Name),
          Numeric.Clamp(font.Attribute("size").ToFloat(SubtitleFont.Size), MinFontSize, MaxFontSize),
          style,
          GraphicsUnit.World);
      else if (font.Name == "line")
        LineFont = new Font(
          font.ToText(LineFont.Name),
          Numeric.Clamp(font.Attribute("size").ToFloat(LineFont.Size), MinFontSize, MaxFontSize),
          style,
          GraphicsUnit.World);
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
        .ToFloat(_connectionStalkLength * 2); // introduced in v1.2, hence default based on existing setting

    DefaultRoomShape = (RoomShape)element["rooms"]["defaultRoomShape"].ToInt();
    DefaultRoomName = element["rooms"]["defaultRoomName"].Text;
    if (string.IsNullOrEmpty(DefaultRoomName))
      DefaultRoomName = "Cave";

    HandleSize = element["ui"]["handleSize"].ToFloat(_handleSize);
    SnapToElementSize = element["ui"]["snapToElementSize"].ToFloat(_snapToElementSize);

    DocumentSpecificMargins = element["margins"]["documentSpecific"].ToBool(_docSpecificMargins);
    DocHorizontalMargin = element["margins"]["horizontal"].ToFloat(_docHorizontalMargin);
    DocVerticalMargin = element["margins"]["vertical"].ToFloat(_docVerticalMargin);
    WrapTextAtDashes = element["margins"]["wrapDashes"].ToBool(true); // maybe should be somewhere else

    KeypadNavigationCreationModifier = StringToModifierKeys(
      element["keypadNavigation"]["creationModifier"].Text,
      _keypadNavigationCreationModifier);
    KeypadNavigationUnexploredModifier = StringToModifierKeys(
      element["keypadNavigation"]["unexploredModifier"].Text,
      _keypadNavigationUnexploredModifier);
  }


  public static void Reset()
  {
    Reset(true);
  }

  internal static void Reset(bool resetDocumentMetadata)
  {
    Color[Colors.Canvas] = System.Drawing.Color.White;
    //Color[Colors.Fill] = System.Drawing.Color.White;
    Color[Colors.Border] = System.Drawing.Color.MidnightBlue;
    Color[Colors.Line] = System.Drawing.Color.MidnightBlue;
    Color[Colors.HoverLine] = System.Drawing.Color.DarkOrange;
    Color[Colors.SelectedLine] = System.Drawing.Color.Gold;
    Color[Colors.Subtitle] = System.Drawing.Color.MidnightBlue;
    Color[Colors.SmallText] = System.Drawing.Color.MidnightBlue;
    Color[Colors.LineText] = System.Drawing.Color.MidnightBlue;
    Color[Colors.Grid] = Drawing.Mix(System.Drawing.Color.White, System.Drawing.Color.Black, 10, 1);
    Color[Colors.StartRoom] = System.Drawing.Color.GreenYellow;
    Color[Colors.EndRoom] = System.Drawing.Color.Red;

    if (resetDocumentMetadata)
      Project.Current.Title = Project.Current.Author = Project.Current.History = Project.Current.Description = "";

    RoomNameFont = new Font(
      ApplicationSettingsController.AppSettings.DefaultFontName,
      13.0f,
      FontStyle.Regular,
      GraphicsUnit.World);
    ObjectFont = new Font(
      ApplicationSettingsController.AppSettings.DefaultFontName,
      11.0f,
      FontStyle.Regular,
      GraphicsUnit.World);
    SubtitleFont = new Font(
      ApplicationSettingsController.AppSettings.DefaultFontName,
      9.0f,
      FontStyle.Regular,
      GraphicsUnit.World);
    LineFont = new Font(
      ApplicationSettingsController.AppSettings.DefaultFontName,
      9.0f,
      FontStyle.Regular,
      GraphicsUnit.World);

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

    KeypadNavigationCreationModifier = Keys.Control;
    KeypadNavigationUnexploredModifier = Keys.Alt;

    Regions = new List<Region> {
      new() { RegionName = Region.DefaultRegion, RColor = System.Drawing.Color.White }
    };
  }

  public static void Save(XmlScribe scribe)
  {
    // save colors
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


    // save fonts
    scribe.StartElement("fonts");
    SaveFont(scribe, _roomNameFont, "room");
    SaveFont(scribe, _objectFont, "object");
    SaveFont(scribe, _subtitleFont, "subTitle");
    SaveFont(scribe, _lineFont, "line");
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
    scribe.Element("wrapDashes", _wrapTextAtDashes); // maybe should be somewhere else
    scribe.EndElement();

    scribe.StartElement("keypadNavigation");
    scribe.Element("creationModifier", ModifierKeysToString(_keypadNavigationCreationModifier));
    scribe.Element("unexploredModifier", ModifierKeysToString(_keypadNavigationUnexploredModifier));
    scribe.EndElement();
  }

  public static void ShowMapDialog()
  {
    using var dialog = new SettingsDialog();
    for (var index = 0; index < Colors.Count; ++index) dialog.ElementColors[index] = Color[index];

    //below is code for pulling the region names, text color and background color from Settings.Regions.
    //it is set up so that Trizbort can check after we click OK or Cancel, and we can see if anything changed.
    //Currently Trizbort only can check for region names of individual rooms changing.
    //After talking with Jason, We'll need to refactor code to make this run smoother, but this is the best for now.

    var regCount = Regions.Count;
    var regNameList = Regions.Select(p => p.RegionName).ToList();
    var regTextColorList = Regions.Select(p => p.TextColor).ToList();
    var regBkgdColorList = Regions.Select(p => p.RColor).ToList();

    dialog.Title = Project.Current.Title;
    dialog.Author = Project.Current.Author;
    dialog.Description = Project.Current.Description;
    dialog.History = Project.Current.History;
    dialog.DefaultRoomName = DefaultRoomName;
    dialog.LargeFont = RoomNameFont;
    dialog.SmallFont = ObjectFont;
    dialog.LineFont = LineFont;
    dialog.SubtitleFont = SubtitleFont;
    dialog.LineWidth = LineWidth;
    dialog.HandDrawn = HandDrawn;
    dialog.SnapToGrid = SnapToGrid;
    dialog.GridSize = GridSize;
    dialog.IsGridVisible = IsGridVisible;
    dialog.ShowOrigin = ShowOrigin;
    dialog.DarknessStripeSize = DarknessStripeSize;
    dialog.ObjectListOffsetFromRoom = ObjectListOffsetFromRoom;
    dialog.ConnectionStalkLength = ConnectionStalkLength;
    dialog.PreferredDistanceBetweenRooms = PreferredDistanceBetweenRooms;
    dialog.TextOffsetFromConnection = TextOffsetFromConnection;
    dialog.HandleSize = HandleSize;
    dialog.SnapToElementSize = SnapToElementSize;
    dialog.DocumentSpecificMargins = DocumentSpecificMargins;
    dialog.DocHorizontalMargin = DocHorizontalMargin;
    dialog.DocVerticalMargin = DocVerticalMargin;
    dialog.WrapTextAtDashes = WrapTextAtDashes;
    dialog.ConnectionArrowSize = ConnectionArrowSize;
    dialog.DefaultRoomShape = DefaultRoomShape;
    if (UserInteraction.ShowDialog(dialog) == DialogResult.OK)
    {
      for (var index = 0; index < Colors.Count; ++index)
      {
        if (Color[index] != dialog.ElementColors[index]) Project.Current.IsDirty = true;
        Color[index] = dialog.ElementColors[index];
      }

      if (Project.Current.Title != dialog.Title) Project.Current.IsDirty = true;
      Project.Current.Title = dialog.Title;
      if (Project.Current.Author != dialog.Author) Project.Current.IsDirty = true;
      Project.Current.Author = dialog.Author;
      if (Project.Current.Description != dialog.Description) Project.Current.IsDirty = true;
      Project.Current.Description = dialog.Description;
      if (Project.Current.History != dialog.History) Project.Current.IsDirty = true;
      Project.Current.History = dialog.History;
      if (DefaultRoomName != dialog.DefaultRoomName) Project.Current.IsDirty = true;
      DefaultRoomName = dialog.DefaultRoomName;
      if (!Equals(RoomNameFont, dialog.LargeFont)) Project.Current.IsDirty = true;
      RoomNameFont = dialog.LargeFont;
      if (!Equals(ObjectFont, dialog.SmallFont)) Project.Current.IsDirty = true;
      ObjectFont = dialog.SmallFont;
      if (!Equals(SubtitleFont, dialog.SubtitleFont)) Project.Current.IsDirty = true;
      SubtitleFont = dialog.SubtitleFont;
      if (!Equals(LineFont, dialog.LineFont)) Project.Current.IsDirty = true;
      LineFont = dialog.LineFont;
      if (LineWidth != dialog.LineWidth) Project.Current.IsDirty = true;
      LineWidth = dialog.LineWidth;
      if (HandDrawn != dialog.HandDrawn) Project.Current.IsDirty = true;
      HandDrawn = dialog.HandDrawn;
      if (SnapToGrid != dialog.SnapToGrid) Project.Current.IsDirty = true;
      SnapToGrid = dialog.SnapToGrid;
      if (GridSize != dialog.GridSize) Project.Current.IsDirty = true;
      GridSize = dialog.GridSize;
      if (IsGridVisible != dialog.IsGridVisible) Project.Current.IsDirty = true;
      IsGridVisible = dialog.IsGridVisible;
      if (ShowOrigin != dialog.ShowOrigin) Project.Current.IsDirty = true;
      ShowOrigin = dialog.ShowOrigin;
      if (DarknessStripeSize != dialog.DarknessStripeSize) Project.Current.IsDirty = true;
      DarknessStripeSize = dialog.DarknessStripeSize;
      if (ObjectListOffsetFromRoom != dialog.ObjectListOffsetFromRoom) Project.Current.IsDirty = true;
      ObjectListOffsetFromRoom = dialog.ObjectListOffsetFromRoom;
      if (ConnectionStalkLength != dialog.ConnectionStalkLength) Project.Current.IsDirty = true;
      ConnectionStalkLength = dialog.ConnectionStalkLength;
      if (PreferredDistanceBetweenRooms != dialog.PreferredDistanceBetweenRooms) Project.Current.IsDirty = true;
      PreferredDistanceBetweenRooms = dialog.PreferredDistanceBetweenRooms;
      if (TextOffsetFromConnection != dialog.TextOffsetFromConnection) Project.Current.IsDirty = true;
      TextOffsetFromConnection = dialog.TextOffsetFromConnection;
      if (HandleSize != dialog.HandleSize) Project.Current.IsDirty = true;
      HandleSize = dialog.HandleSize;
      if (SnapToElementSize != dialog.SnapToElementSize) Project.Current.IsDirty = true;
      SnapToElementSize = dialog.SnapToElementSize;
      if (ConnectionArrowSize != dialog.ConnectionArrowSize) Project.Current.IsDirty = true;
      ConnectionArrowSize = dialog.ConnectionArrowSize;
      if (DocumentSpecificMargins != dialog.DocumentSpecificMargins) Project.Current.IsDirty = true;
      DocumentSpecificMargins = dialog.DocumentSpecificMargins;
      if (DocHorizontalMargin != dialog.DocHorizontalMargin) Project.Current.IsDirty = true;
      DocHorizontalMargin = dialog.DocHorizontalMargin;
      if (DocVerticalMargin != dialog.DocVerticalMargin) Project.Current.IsDirty = true;
      DocVerticalMargin = dialog.DocVerticalMargin;
      if (WrapTextAtDashes != dialog.WrapTextAtDashes) Project.Current.IsDirty = true;
      WrapTextAtDashes = dialog.WrapTextAtDashes;
      if (DefaultRoomShape != dialog.DefaultRoomShape) Project.Current.IsDirty = true;
      DefaultRoomShape = dialog.DefaultRoomShape;
    }

    //Note this needs to be done outside of the "if OK button is clicked" loop for now.
    //Trizbort makes changes to regions immediately. So we can change a region and hit cancel.
    //We need to account for that.
    var newRegCount = Regions.Count;
    var newReg = Regions.ToArray();
    if (newRegCount != regCount)
      Project.Current.IsDirty = true;
    else
      for (var index = 0; index < newRegCount; index++)
      {
        if (regBkgdColorList[index] != newReg[index].RColor)
          Project.Current.IsDirty = true;
        if (regTextColorList[index] != newReg[index].TextColor)
          Project.Current.IsDirty = true;
        if (regNameList[index] != newReg[index].RegionName)
          Project.Current.IsDirty = true;
      }
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

  private static string ModifierKeysToString(Keys key)
  {
    var builder = new StringBuilder();
    if ((key & Keys.Shift) == Keys.Shift)
    {
      if (builder.Length != 0)
        builder.Append("|");
      builder.Append("shift");
    }

    if ((key & Keys.Control) == Keys.Control)
    {
      if (builder.Length != 0)
        builder.Append("|");
      builder.Append("control");
    }

    if ((key & Keys.Alt) == Keys.Alt)
    {
      if (builder.Length != 0)
        builder.Append("|");
      builder.Append("alt");
    }

    if (builder.Length == 0) builder.Append("none");
    return builder.ToString();
  }

  private static void RaiseChanged()
  {
    var changed = Changed;
    changed?.Invoke(null, EventArgs.Empty);
  }

  private static void SaveFont(XmlScribe scribe, Font font, string name)
  {
    scribe.StartElement(name);
    scribe.Attribute("size", font.Size);
    if ((font.Style & FontStyle.Bold) == FontStyle.Bold) scribe.Attribute("bold", true);
    if ((font.Style & FontStyle.Italic) == FontStyle.Italic) scribe.Attribute("italic", true);
    if ((font.Style & FontStyle.Underline) == FontStyle.Underline) scribe.Attribute("underline", true);
    if ((font.Style & FontStyle.Strikeout) == FontStyle.Strikeout) scribe.Attribute("strikeout", true);
    scribe.Value(Drawing.FontName(font));
    scribe.EndElement();
  }

  private static Keys StringToModifierKeys(string text, Keys defaultValue)
  {
    if (string.IsNullOrEmpty(text)) return defaultValue;

    var value = Keys.None;
    foreach (var part in text.Split('|'))
      if (StringComparer.InvariantCultureIgnoreCase.Compare(part, "shift") == 0)
        value |= Keys.Shift;
      else if (StringComparer.InvariantCultureIgnoreCase.Compare(part, "control") == 0)
        value |= Keys.Control;
      else if (StringComparer.InvariantCultureIgnoreCase.Compare(part, "alt") == 0) value |= Keys.Alt;
    // Note that "none" is also an allowed value.
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