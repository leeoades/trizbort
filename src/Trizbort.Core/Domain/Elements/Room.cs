using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.Domain.Elements;

/// <summary>
///   A room in the project.
/// </summary>
public class Room : Element, ISizeable
{
  private const CompassPoint DefaultObjectsPosition = CompassPoint.South;
  private readonly List<string> _descriptions = new();
  private string _name = string.Empty;
  private string _objects = string.Empty;
  private string _subTitle = string.Empty;
  private bool _allCornersEqual = true;
  private BorderDashStyle _borderStyle = BorderDashStyle.Solid;
  private CornerRadii _corners;
  private bool _ellipse;
  private HandDrawnStyle _handDrawnStyle;
  private bool _isDark;
  private bool _isEndRoom;
  private bool _isStartRoom;
  private CompassPoint _objectsPosition = DefaultObjectsPosition;
  private bool _octagonal;
  private Vector _position;
  private Color _roomborder = Color.Transparent;
  private Color _roomfill = Color.Transparent;
  private Color _roomlargetext = Color.Transparent;
  private string _roomRegion;
  private Color _roomsmalltext = Color.Transparent;
  private Color _roomSubtitleColor = Color.Transparent;
  private bool _roundedCorners;
  private Color _secondfill = Color.Transparent;
  private string _secondfilllocation = "Bottom";
  private RoomShape _shape;
  private Vector _size;
  private bool _straightEdges;

  public event EventHandler NameLayoutInvalidated;
  public event EventHandler SubTitleLayoutInvalidated;
  public event EventHandler ObjectsLayoutInvalidated;

  public Room()
  {
    AddPortsToRoom();
  }

  public Room(Project project) : base(project)
  {
    Name = Settings.DefaultRoomName;
    Region = Misc.Region.DefaultRegion;
    Size = new Vector(3 * Settings.GridSize, 2 * Settings.GridSize);
    Position = new Vector(-Size.X / 2, -Size.Y / 2);
    Corners = new CornerRadii();
    Shape = Settings.DefaultRoomShape;

    // connections may connect to any of our "corners"
    AddPortsToRoom();
  }

  public Room(Project project, int totalIDs) : base(project, totalIDs)
  {
    Name = Settings.DefaultRoomName;
    Region = Misc.Region.DefaultRegion;
    Size = new Vector(3 * Settings.GridSize, 2 * Settings.GridSize);
    Position = new Vector(-Size.X / 2, -Size.Y / 2);
    Corners = new CornerRadii();
    Shape = RoomShape.SquareCorners;

    // connections may connect to any of our "corners"
    AddPortsToRoom();
  }

  public bool AllCornersEqual {
    get { return _allCornersEqual; }
    set {
      if (_allCornersEqual != value)
      {
        _allCornersEqual = value;
        RaiseChanged();
      }
    }
  }

  public bool ArbitraryAutomappedPosition { get; set; }

  public BorderDashStyle BorderStyle {
    get { return _borderStyle; }
    set {
      if (_borderStyle != value)
      {
        _borderStyle = value;
        RaiseChanged();
      }
    }
  }

  public CornerRadii Corners {
    get { return _corners; }
    set {
      _corners = value;
      RaiseChanged();
    }
  }

  public override Depth Depth => Depth.Medium;

  [JsonIgnore]
  public List<string> Descriptions => _descriptions;

  public bool Ellipse {
    get { return _ellipse; }
    set {
      if (_ellipse != value)
      {
        _ellipse = value;
        RaiseChanged();
      }
    }
  }

  public bool HasDescription => _descriptions.Count > 0;

  public HandDrawnStyle HandDrawnStyle {
    get { return _handDrawnStyle; }
    set {
      if (_handDrawnStyle != value)
      {
        _handDrawnStyle = value;
        RaiseChanged();
      }
    }
  }

  public override bool HasDialog => true;

  public float Height => Size.Y;

  public Rect InnerBounds => new(Position, Size);

  [JsonIgnore]
  public bool IsConnected {
    get {
      return Project.Current.Elements.OfType<Connection>()
                    .Any(element => element
                                    .VertexList.Select(vertex => vertex.Port)
                                    .Any(port => port != null && port.Owner == this));
    }
  }

  public bool IsDark {
    get { return _isDark; }
    set {
      if (_isDark != value)
      {
        _isDark = value;
        RaiseChanged();
      }
    }
  }

  public bool IsEndRoom {
    get { return _isEndRoom; }
    set {
      if (_isEndRoom != value)
      {
        _isEndRoom = value;
        RaiseChanged();
      }
    }
  }

  public bool IsHandDrawn {
    get {
      switch (HandDrawnStyle)
      {
        case HandDrawnStyle.MapDefault:
          return Settings.HandDrawn;
        case HandDrawnStyle.HandDrawn:
          return true;
        case HandDrawnStyle.Straight:
          return false;
        default:
          return false;
      }
    }
  }

  public int OldId { get; set; }

  public bool IsReference => ReferenceRoom != null;

  public bool IsStartRoom {
    get { return _isStartRoom; }
    set {
      if (_isStartRoom != value)
      {
        _isStartRoom = value;
        RaiseChanged();
      }
    }
  }

  public override string Name {
    get { return _name; }
    set {
      value = value ?? string.Empty;
      if (_name == value) return;
      _name = value;
      MarkNameInvalid();
      RaiseChanged();
    }
  }

  public string Objects {
    get { return _objects; }
    set {
      value = value ?? string.Empty;
      if (_objects == value) return;
      _objects = value;
      InvalidateObjectList();
      RaiseChanged();
    }
  }

  public bool ObjectsCustomPosition { get; set; }
  public int ObjectsCustomPositionDown { get; set; }
  public int ObjectsCustomPositionRight { get; set; }

  public CompassPoint ObjectsPosition {
    get { return _objectsPosition; }
    set {
      if (_objectsPosition != value)
      {
        _objectsPosition = value;
        RaiseChanged();
      }
    }
  }

  public bool Octagonal {
    get { return _octagonal; }
    set {
      if (_octagonal != value)
      {
        _octagonal = value;
        RaiseChanged();
      }
    }
  }

  public override Vector Position {
    get { return _position; }
    set {
      if (_position != value)
      {
        _position = value;
        RaiseChanged();
      }
    }
  }

  public string PrimaryDescription => _descriptions.Count > 0 ? _descriptions[0] : null;

  public List<RoomValidationState> ValidationState { get; set; } = new();

  public void AdjustAllRoomConnections()
  {
    var somethingChanged = false;
    foreach (var element in GetConnections())
    {
      if (element.VertexList[0].Port.Owner == element.VertexList[1].Port.Owner) continue;

      var cp = CompassPoint.Min;

      var xDelta = element.VertexList[0].Port.Owner.Position.X - element.VertexList[1].Port.Owner.Position.X;
      var yDelta = element.VertexList[0].Port.Owner.Position.Y - element.VertexList[1].Port.Owner.Position.Y;

      if (xDelta == 0 && yDelta == 0) continue;
      if (xDelta == 0)
      {
        cp = CompassPoint.North;
      }
      else
      {
        var slope = yDelta / xDelta;
        var abSlope = Math.Abs(slope);
        var isNeg = slope > 0;
        var isLeft = xDelta > 0;
        switch (ApplicationSettingsController.AppSettings.PortAdjustDetail)
        {
          //These numbers are decided as follows: tangent of 45 degrees, then 22.5/67.5, then 11.25/33.75/56.25/78.75
          case 0: //fourths
            if (abSlope > 1)
            {
              cp = CompassPoint.North;
            }
            else
            {
              cp = CompassPoint.East;
              if (isLeft) cp = CompassPoint.West;
            }

            break;
          case 1: //eighths
            if (abSlope > 2.414)
            {
              cp = CompassPoint.North;
            } //incidentally tan (pi/8) = sqrt 2 - 1. Angle bisector theorem/trig identities prove it.
            else if (abSlope > 0.414)
            {
              cp = CompassPoint.NorthEast;
              if (isNeg) cp = CompassPoint.NorthWest;
            }
            else
            {
              cp = CompassPoint.East;
              if (isLeft) cp = CompassPoint.West;
            }

            break;
          case 2: //sixteenths
            if (abSlope > 5.03)
            {
              cp = CompassPoint.North;
            }
            else if (abSlope > 1.49)
            {
              cp = CompassPoint.NorthNorthEast;
              if (isNeg) cp = CompassPoint.NorthNorthWest;
            }
            else if (abSlope > 0.668)
            {
              cp = CompassPoint.NorthEast;
              if (isNeg) cp = CompassPoint.NorthWest;
            }
            else if (abSlope > 0.197)
            {
              cp = CompassPoint.EastNorthEast;
              if (isNeg) cp = CompassPoint.WestNorthWest;
            }
            else
            {
              cp = CompassPoint.East;
              if (isLeft) cp = CompassPoint.West;
            }

            break;
        }
      }

      //we need to check we're not totally backwards here. This code appears correct, but I'm defining the boolean in case there's
      //a special case I forgot.
      var backwards = yDelta < 0;
      if (backwards)
        cp = CompassPointHelper.GetOpposite(cp);
      var cpInt = (int)cp;
      if (element.VertexList[0].Port != element.VertexList[0].Port.Owner.PortList[cpInt])
      {
        somethingChanged = true;
        element.VertexList[0].Port = element.VertexList[0].Port.Owner.PortList[cpInt];
      }

      var cpIntOpposite = (int)CompassPointHelper.GetOpposite(cp);
      if (element.VertexList[1].Port != element.VertexList[1].Port.Owner.PortList[cpIntOpposite])
      {
        element.VertexList[1].Port = element.VertexList[1].Port.Owner.PortList[cpIntOpposite];
        somethingChanged = true;
      }
    }

    if (somethingChanged) RaiseChanged();
  }

  public void CheckValidation()
  {
    RoomValidationState state;
    ValidationState.Clear();

    if (Project.Current.MustHaveDescription && !HasDescription)
    {
      state = new RoomValidationState
      {
        Message = "There is no description for this room.",
        Status = RoomValidationStatus.Invalid,
        Type = ValidationType.RoomDescription
      };
      ValidationState.Add(state);
    }

    if (Project.Current.MustHaveUniqueNames)
    {
      if (Project.Current.Elements.OfType<Room>().Count(p => p.Name == Name) > 1)
      {
        state = new RoomValidationState
        {
          Message = "The room name is not unique.",
          Status = RoomValidationStatus.Invalid,
          Type = ValidationType.RoomUniqueName
        };
        ValidationState.Add(state);
      }
    }

    if (Project.Current.MustHaveNoDanglingConnectors)
    {
      if (Project.Current.Elements.OfType<Connection>().Count(p => p.GetSourceRoom() == this && p.IsDangling) > 0)
      {
        state = new RoomValidationState
        {
          Message = "Room has dangling connectors.",
          Status = RoomValidationStatus.Invalid,
          Type = ValidationType.RoomDanglingConnection
        };
        ValidationState.Add(state);
      }
    }

    if (Project.Current.MustHaveSubtitle && string.IsNullOrWhiteSpace(SubTitle))
    {
      state = new RoomValidationState
      {
        Message = "Room must have a subtitle.",
        Status = RoomValidationStatus.Invalid,
        Type = ValidationType.RoomSubTitle
      };
      ValidationState.Add(state);
    }
  }

  public Room ReferenceRoom {
    get {
      if (ReferenceRoomId > 0)
        return (Room)Project?.Elements?.FirstOrDefault(p => p.Id == ReferenceRoomId);
      return null;
    }
  }

  public int ReferenceRoomId { get; set; } = -1;

  public string Region {
    get { return _roomRegion; }
    set {
      _roomRegion = value;
      RaiseChanged();
    }
  }

  public Color RoomBorderColor {
    get { return _roomborder; }
    set {
      if (_roomborder != value)
      {
        _roomborder = value;
        RaiseChanged();
      }
    }
  }

  public Color RoomFillColor {
    get { return _roomfill; }
    set {
      if (_roomfill != value)
      {
        _roomfill = value;
        RaiseChanged();
      }
    }
  }

  public Color RoomNameColor {
    get { return _roomlargetext; }
    set {
      if (_roomlargetext != value)
      {
        _roomlargetext = value;
        RaiseChanged();
      }
    }
  }

  public Color RoomObjectTextColor {
    get { return _roomsmalltext; }
    set {
      if (_roomsmalltext != value)
      {
        _roomsmalltext = value;
        RaiseChanged();
      }
    }
  }

  public Color RoomSubtitleColor {
    get { return _roomSubtitleColor; }
    set {
      if (_roomSubtitleColor != value)
      {
        _roomSubtitleColor = value;
        RaiseChanged();
      }
    }
  }

  public bool RoundedCorners {
    get { return _roundedCorners; }
    set {
      if (_roundedCorners != value)
      {
        _roundedCorners = value;
        RaiseChanged();
      }
    }
  }

  public Color SecondFillColor {
    get { return _secondfill; }
    set {
      if (_secondfill != value)
      {
        _secondfill = value;
        RaiseChanged();
      }
    }
  }

  public string SecondFillLocation {
    get { return _secondfilllocation; }
    set {
      if (_secondfilllocation != value)
      {
        _secondfilllocation = value;
        RaiseChanged();
      }
    }
  }

  public RoomShape Shape {
    get { return _shape; }
    set {
      if (_shape != value)
      {
        _shape = value;
        SetRoomShape(value);
        RaiseChanged();
      }
    }
  }

  public Vector Size {
    get { return _size; }
    set {
      if (_size != value)
      {
        _size = value;
        RaiseChanged();
      }
    }
  }

  public bool StraightEdges {
    get { return _straightEdges; }
    set {
      if (_straightEdges != value)
      {
        _straightEdges = value;
        RaiseChanged();
      }
    }
  }

  public string SubTitle {
    get { return _subTitle; }
    set {
      value = value ?? string.Empty;
      if (_subTitle == value) return;
      _subTitle = value;
      InvalidateSubTitle();
      RaiseChanged();
    }
  }

  public float Width => Size.X;

  public float X => Position.X;

  public float Y => Position.Y;

  public void AddDescription(string description)
  {
    if (string.IsNullOrEmpty(description)) return;

    if (!_descriptions.Any(existing => string.Compare(existing, description, StringComparison.OrdinalIgnoreCase) == 0))
    {
      _descriptions.Add(description);
      RaiseChanged();
    }
  }

  public void AddReferenceRoom(int roomId)
  {
    ReferenceRoomId = roomId;
    Name = $"{ReferenceRoom.Name} (ref)";
    _descriptions.Clear();
    _descriptions.AddRange(ReferenceRoom.Descriptions);
    SubTitle = ReferenceRoom.SubTitle;
    IsDark = ReferenceRoom.IsDark;
    Objects = ReferenceRoom.Objects;
    ObjectsPosition = ReferenceRoom.ObjectsPosition;
    BorderStyle = ReferenceRoom.BorderStyle;
    RoomFillColor = ReferenceRoom.RoomFillColor;
    SecondFillColor = ReferenceRoom.SecondFillColor;
    SecondFillLocation = ReferenceRoom.SecondFillLocation;
    RoomBorderColor = ReferenceRoom.RoomBorderColor;
    RoomNameColor = ReferenceRoom.RoomNameColor;
    RoomSubtitleColor = ReferenceRoom.RoomSubtitleColor;
    RoomObjectTextColor = ReferenceRoom.RoomObjectTextColor;
    Shape = ReferenceRoom.Shape;
    Corners = ReferenceRoom.Corners;
    AllCornersEqual = ReferenceRoom.AllCornersEqual;
    Region = ReferenceRoom.Region;
  }

  public void ChangeRoomShape(RoomShape shape)
  {
    Shape = shape;
    SetRoomShape(shape);
  }

  public void CheckCornerRadii()
  {
    if (Corners.TopLeft > Width / 2 || Corners.TopLeft > Height / 2)
      Corners.TopLeft = (int)Math.Min(Width / 2, Height / 2);
    if (Corners.TopRight > Width / 2 || Corners.TopRight > Height / 2)
      Corners.TopRight = (int)Math.Min(Width / 2, Height / 2);
    if (Corners.BottomRight > Width / 2 || Corners.BottomRight > Height / 2)
      Corners.BottomRight = (int)Math.Min(Width / 2, Height / 2);
    if (Corners.BottomLeft > Width / 2 || Corners.BottomLeft > Height / 2)
      Corners.BottomLeft = (int)Math.Min(Width / 2, Height / 2);
  }

  public void ClearDescriptions()
  {
    if (_descriptions.Count > 0)
    {
      _descriptions.Clear();
      RaiseChanged();
    }
  }

  public void DeleteAllRoomConnections()
  {
    var zappedOne = false;
    foreach (var element in GetConnections())
    {
      Project.Current.Elements.Remove(element);
      zappedOne = true;
    }

    if (zappedOne) RaiseChanged();
  }

  public override float Distance(Vector pos, bool includeMargins)
  {
    var bounds = UnionBoundsWith(Rect.Empty, includeMargins);
    return pos.DistanceFromRect(bounds);
  }

  public List<Connection> GetConnections()
  {
    var connections = new List<Connection>();
    foreach (var element in Project.Current.Elements.OfType<Connection>())
    {
      var hasSource = element.GetSourceRoom() == this;
      var hasTarget = element.GetTargetRoom() == this;
      if (hasSource || hasTarget)
        connections.Add(element);
    }

    return connections;
  }

  public List<Connection> GetConnections(CompassPoint? compassPoint)
  {
    var connections = new List<Connection>();
    foreach (var element in Project.Current.Elements.OfType<Connection>())
    {
      var hasSource = element.GetSourceRoom(out var sourceCompassPoint) == this &&
                      (compassPoint == null || sourceCompassPoint == compassPoint.Value);
      var hasTarget = element.GetTargetRoom(out var targetCompassPoint) == this &&
                      (compassPoint == null || targetCompassPoint == compassPoint.Value);
      if (hasSource || hasTarget)
        connections.Add(element);
    }

    return connections;
  }

  public override Vector GetPortPosition(Port port)
  {
    var compassPort = (CompassPort)port;
    return InnerBounds.GetCorner(compassPort.CompassPoint, Shape, Corners);
  }

  public override Vector GetPortStalkPosition(Port port)
  {
    var compassPort = (CompassPort)port;
    var bounds = InnerBounds;
    bounds.Inflate(Settings.ConnectionStalkLength);
    return bounds.GetCorner(compassPort.CompassPoint);
  }

  public override Color GetToolTipColor()
  {
    return Color.LightBlue;
  }

  public override string GetToolTipFooter()
  {
    return Region;
  }

  public override string GetToolTipHeader()
  {
    return $"{Name}{(IsDark ? " (Dark)" : string.Empty)}";
  }

  public override string GetToolTipText()
  {
    var desc = string.Empty;
    var objects = string.Empty;
    var charsToShow = ApplicationSettingsController.AppSettings.ToolTipRoomDescriptionCharactersToShow;
    if (ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips)
    {
      desc = $"{PrimaryDescription}";
      if (ApplicationSettingsController.AppSettings.LimitRoomDescriptionCharactersInTooltip &&
          desc.Length >= charsToShow)
      {
        desc = desc.Substring(0, charsToShow);
        desc = desc + "...";
      }
    }

    if (ApplicationSettingsController.AppSettings.ShowObjectsInTooltips) objects = $"{Objects}";

    if (desc != string.Empty && objects != string.Empty)
      return $"{desc}\r\n-----\r\n{objects}";
    if (desc != string.Empty)
      return $"{desc}";
    if (objects != string.Empty) return $"{objects}";
    return string.Empty;
  }


  public override bool HasTooltip()
  {
    return true;
  }

  public override bool Intersects(Rect rect)
  {
    return InnerBounds.IntersectsWith(rect);
  }

  public IList<string> ListOfObjects()
  {
    var tObjects = Objects.Replace("\r", string.Empty).Replace("|", "\\|").Replace("\n", "|");
    var objects = tObjects.Split('|').Where(p => p != string.Empty).ToList();
    return objects;
  }

  public void Load(XmlElementReader element)
  {
    Load(element, null);
  }

  public void Load(XmlElementReader element, Action<string, string> reportWarning)
  {
    Name = element.Attribute("name").Text;
    SubTitle = element.Attribute("subtitle").Text;
    ClearDescriptions();
    AddDescription(element.Attribute("description").Text);
    Position = new Vector(element.Attribute("x").ToFloat(), element.Attribute("y").ToFloat());
    Size = new Vector(element.Attribute("w").ToFloat(), element.Attribute("h").ToFloat());
    Region = element.Attribute("region").Text;
    ReferenceRoomId = element.Attribute("referenceRoom").ToInt();
    IsDark = element.Attribute("isDark").ToBool();
    IsStartRoom = element.Attribute("isStartRoom").ToBool();
    IsEndRoom = element.Attribute("isEndRoom").ToBool();
    ZOrder = element.Attribute("ZOrder").ToInt();
    if (IsStartRoom)
      if (Settings.StartRoomLoaded)
        reportWarning?.Invoke(
          $"{Name} is a duplicate start room. You may need to erase \"isStartRoom=YES\" from the XML.",
          "Duplicate start room warning");
      else
        Settings.StartRoomLoaded = true;
    if (IsEndRoom)
      if (Settings.EndRoomLoaded)
        reportWarning?.Invoke(
          $"{Name} is a duplicate end room. You may need to erase \"isEndRoom=YES\" from the XML.",
          "Duplicate end room warning");
      else
        Settings.EndRoomLoaded = true;

    if (element.Attribute("roundedCorners").ToBool())
      Shape = RoomShape.RoundedCorners;
    else if (element.Attribute("octagonal").ToBool())
      Shape = RoomShape.Octagonal;
    else if (element.Attribute("ellipse").ToBool())
      Shape = RoomShape.Ellipse;
    else
      Shape = RoomShape.SquareCorners;

    if (Enum.TryParse(element.Attribute("handDrawnStyle").Text, out HandDrawnStyle handDrawnStyle))
      HandDrawnStyle = handDrawnStyle;
    else
      HandDrawnStyle = element.Attribute("handDrawn").ToBool() ? HandDrawnStyle.HandDrawn : HandDrawnStyle.MapDefault;
    AllCornersEqual = element.Attribute("allcornersequal").ToBool();

    Corners = new CornerRadii();
    Corners.TopLeft = element.Attribute("cornerTopLeft").ToFloat();
    Corners.TopRight = element.Attribute("cornerTopRight").ToFloat();
    Corners.BottomLeft = element.Attribute("cornerBottomLeft").ToFloat();
    Corners.BottomRight = element.Attribute("cornerBottomRight").ToFloat();

    if (element.Attribute("borderstyle").Text != "")
      BorderStyle = (BorderDashStyle)Enum.Parse(typeof(BorderDashStyle), element.Attribute("borderstyle").Text);

    if (Project.Version.CompareTo(new Version(1, 5, 8, 3)) < 0)
    {
      if (element.Attribute("roomFill").Text != "" && element.Attribute("roomFill").Text != "#FFFFFF")
        RoomFillColor = ColorTranslator.FromHtml(element.Attribute("roomFill").Text);
      if (element.Attribute("secondFill").Text != "" && element.Attribute("roomFill").Text != "#FFFFFF")
        SecondFillColor = ColorTranslator.FromHtml(element.Attribute("secondFill").Text);
      if (element.Attribute("secondFillLocation").Text != "" && element.Attribute("roomFill").Text != "#FFFFFF")
        SecondFillLocation = element.Attribute("secondFillLocation").Text;
      if (element.Attribute("roomBorder").Text != "" && element.Attribute("roomFill").Text != "#FFFFFF")
        RoomBorderColor = ColorTranslator.FromHtml(element.Attribute("roomBorder").Text);
      if (element.Attribute("roomLargeText").Text != "" && element.Attribute("roomFill").Text != "#FFFFFF")
        RoomNameColor = ColorTranslator.FromHtml(element.Attribute("roomLargeText").Text);
      if (element.Attribute("roomSmallText").Text != "" && element.Attribute("roomFill").Text != "#FFFFFF")
        RoomObjectTextColor = ColorTranslator.FromHtml(element.Attribute("roomSmallText").Text);
    }
    else
    {
      if (element.Attribute("roomFill").Text != "")
        RoomFillColor = ColorTranslator.FromHtml(element.Attribute("roomFill").Text);
      if (element.Attribute("secondFill").Text != "")
        SecondFillColor = ColorTranslator.FromHtml(element.Attribute("secondFill").Text);
      if (element.Attribute("secondFillLocation").Text != "")
        SecondFillLocation = element.Attribute("secondFillLocation").Text;
      if (element.Attribute("roomBorder").Text != "")
        RoomBorderColor = ColorTranslator.FromHtml(element.Attribute("roomBorder").Text);
      if (element.Attribute("roomLargeText").Text != "")
        RoomNameColor = ColorTranslator.FromHtml(element.Attribute("roomLargeText").Text);
      if (element.Attribute("roomSmallText").Text != "")
        RoomObjectTextColor = ColorTranslator.FromHtml(element.Attribute("roomSmallText").Text);
      if (element.Attribute("roomSubtitleColor").Text != "")
        RoomSubtitleColor = ColorTranslator.FromHtml(element.Attribute("roomSubtitleColor").Text);
    }

    Objects = element["objects"].Text.Replace("|", "\r\n").Replace("\\\r\n", "|");
    ObjectsPosition = element["objects"].Attribute("at").ToCompassPoint(ObjectsPosition);
    ObjectsCustomPosition = element["objects"].Attribute("custom").ToBool();
    ObjectsCustomPositionRight = element["objects"].Attribute("customRight").ToInt();
    ObjectsCustomPositionDown = element["objects"].Attribute("customDown").ToInt();
  }

  public bool MatchDescription(string description)
  {
    return MatchDescription(description, out _);
  }

  public bool MatchDescription(string description, out float matchStrength)
  {
    var matchingLength = 0;
    var totalLength = 0;
    foreach (var ourDescription in _descriptions)
    {
      var index = 0;
      while (index < description.Length &&
             index < ourDescription.Length &&
             char.ToUpperInvariant(description[index]) == char.ToUpperInvariant(ourDescription[index]))
        ++index;

      matchingLength = Math.Max(matchingLength, index);
      totalLength = Math.Max(totalLength, Math.Max(description.Length, ourDescription.Length));
    }

    if (totalLength > 0)
      matchStrength = (float)matchingLength / totalLength;
    else
      matchStrength = 0;

    return matchingLength > 0;
  }

  public Port PortAt(CompassPoint point)
  {
    return PortList.Cast<CompassPort>().First(port => port.CompassPoint == point);
  }

  public void RemoveDescription(string description)
  {
    for (var index = 0; index < _descriptions.Count; ++index)
      if (string.Compare(_descriptions[index], description, StringComparison.OrdinalIgnoreCase) == 0)
      {
        _descriptions.RemoveAt(index);
        --index;
        RaiseChanged();
      }
  }

  public void Save(XmlScribe scribe)
  {
    scribe.Attribute("name", Name);
    scribe.Attribute("subtitle", SubTitle);
    scribe.Attribute("x", Position.X);
    scribe.Attribute("y", Position.Y);
    scribe.Attribute("w", Size.X);
    scribe.Attribute("h", Size.Y);
    scribe.Attribute("region", string.IsNullOrEmpty(Region) ? Misc.Region.DefaultRegion : Region);
    if (ReferenceRoom != null)
      scribe.Attribute("referenceRoom", ReferenceRoom.Id);

    scribe.Attribute("handDrawn", IsHandDrawn);
    scribe.Attribute("handDrawnStyle", HandDrawnStyle.ToString());
    scribe.Attribute("allcornersequal", AllCornersEqual);
    scribe.Attribute("ellipse", Ellipse);
    scribe.Attribute("roundedCorners", RoundedCorners);
    scribe.Attribute("octagonal", Octagonal);
    scribe.Attribute("cornerTopLeft", (float)Corners.TopLeft);
    scribe.Attribute("cornerTopRight", (float)Corners.TopRight);
    scribe.Attribute("cornerBottomLeft", (float)Corners.BottomLeft);
    scribe.Attribute("cornerBottomRight", (float)Corners.BottomRight);

    scribe.Attribute("borderstyle", BorderStyle.ToString());

    if (IsDark)
      scribe.Attribute("isDark", IsDark);

    if (IsStartRoom)
      scribe.Attribute("isStartRoom", IsStartRoom);

    if (IsEndRoom)
      scribe.Attribute("isEndRoom", IsEndRoom);

    scribe.Attribute("description", PrimaryDescription);

    var colorValue = Colors.SaveColor(RoomFillColor);
    scribe.Attribute("roomFill", colorValue);

    colorValue = Colors.SaveColor(SecondFillColor);
    scribe.Attribute("secondFill", colorValue);
    scribe.Attribute("secondFillLocation", SecondFillLocation);

    colorValue = Colors.SaveColor(RoomBorderColor);
    scribe.Attribute("roomBorder", colorValue);

    colorValue = Colors.SaveColor(RoomNameColor);
    scribe.Attribute("roomLargeText", colorValue);

    colorValue = Colors.SaveColor(RoomSubtitleColor);
    scribe.Attribute("roomSubtitleColor", colorValue);

    colorValue = Colors.SaveColor(RoomObjectTextColor);
    scribe.Attribute("roomSmallText", colorValue);

    scribe.Attribute("ZOrder", ZOrder);

    if (!string.IsNullOrEmpty(Objects) || ObjectsPosition != DefaultObjectsPosition)
    {
      scribe.StartElement("objects");

      if (ObjectsPosition != DefaultObjectsPosition)
        scribe.Attribute("at", ObjectsPosition);

      if (ObjectsCustomPosition)
      {
        scribe.Attribute("custom", ObjectsCustomPosition);
        scribe.Attribute("customRight", ObjectsCustomPositionRight);
        scribe.Attribute("customDown", ObjectsCustomPositionDown);
      }

      if (!string.IsNullOrEmpty(Objects))
        scribe.Value(Objects.Replace("\r", string.Empty).Replace("|", "\\|").Replace("\n", "|"));
      scribe.EndElement();
    }
  }

  public void SetCorner(CompassPoint corner, float value)
  {
    var changed = false;
    if (AllCornersEqual)
    {
      changed = _corners.SetCorner(CompassPoint.NorthWest, value) || changed;
      changed = _corners.SetCorner(CompassPoint.NorthEast, value) || changed;
      changed = _corners.SetCorner(CompassPoint.SouthWest, value) || changed;
      changed = _corners.SetCorner(CompassPoint.SouthEast, value) || changed;
    }
    else
    {
      changed = _corners.SetCorner(corner, value);
    }

    if (changed) RaiseChanged();
  }

  public override string ToString()
  {
    var text = $"Room: {Name}";
    return text;
  }

  public override Rect UnionBoundsWith(Rect rect, bool includeMargins)
  {
    var bounds = InnerBounds;
    if (includeMargins)
      bounds.Inflate(Settings.LineWidth + Settings.ConnectionStalkLength);
    return rect.Union(bounds);
  }

  private void AddPortsToRoom()
  {
    PortList.Add(new CompassPort(CompassPoint.North, this));
    PortList.Add(new CompassPort(CompassPoint.NorthNorthEast, this));
    PortList.Add(new CompassPort(CompassPoint.NorthEast, this));
    PortList.Add(new CompassPort(CompassPoint.EastNorthEast, this));
    PortList.Add(new CompassPort(CompassPoint.East, this));
    PortList.Add(new CompassPort(CompassPoint.EastSouthEast, this));
    PortList.Add(new CompassPort(CompassPoint.SouthEast, this));
    PortList.Add(new CompassPort(CompassPoint.SouthSouthEast, this));
    PortList.Add(new CompassPort(CompassPoint.South, this));
    PortList.Add(new CompassPort(CompassPoint.SouthSouthWest, this));
    PortList.Add(new CompassPort(CompassPoint.SouthWest, this));
    PortList.Add(new CompassPort(CompassPoint.WestSouthWest, this));
    PortList.Add(new CompassPort(CompassPoint.West, this));
    PortList.Add(new CompassPort(CompassPoint.WestNorthWest, this));
    PortList.Add(new CompassPort(CompassPoint.NorthWest, this));
    PortList.Add(new CompassPort(CompassPoint.NorthNorthWest, this));
  }

  public void CopyStyleFrom(Room source)
  {
    if (source == null || ReferenceEquals(source, this)) return;

    Region = source.Region;
    Shape = source.Shape;
    Ellipse = source.Ellipse;
    RoundedCorners = source.RoundedCorners;
    Octagonal = source.Octagonal;
    StraightEdges = source.StraightEdges;
    AllCornersEqual = source.AllCornersEqual;
    Corners = new CornerRadii {
      TopLeft = source.Corners.TopLeft,
      TopRight = source.Corners.TopRight,
      BottomLeft = source.Corners.BottomLeft,
      BottomRight = source.Corners.BottomRight
    };
    HandDrawnStyle = source.HandDrawnStyle;
    BorderStyle = source.BorderStyle;
    RoomBorderColor = source.RoomBorderColor;
    RoomFillColor = source.RoomFillColor;
    SecondFillColor = source.SecondFillColor;
    SecondFillLocation = source.SecondFillLocation;
    RoomNameColor = source.RoomNameColor;
    RoomSubtitleColor = source.RoomSubtitleColor;
    RoomObjectTextColor = source.RoomObjectTextColor;
    IsDark = source.IsDark;
    ObjectsPosition = source.ObjectsPosition;
  }

  private void SetRoomShape(RoomShape pShape)
  {
    switch (pShape)
    {
      case RoomShape.SquareCorners:
        StraightEdges = !StraightEdges;
        Ellipse = false;
        RoundedCorners = false;
        Octagonal = false;
        break;
      case RoomShape.RoundedCorners:
        RoundedCorners = true;
        Ellipse = false;
        StraightEdges = false;
        if (Corners.TopRight == 0.0 && Corners.TopLeft == 0.0 && Corners.BottomRight == 0.0 &&
            Corners.BottomLeft == 0.0)
          Corners = new CornerRadii();
        Octagonal = false;
        break;
      case RoomShape.Ellipse:
        Ellipse = true;
        RoundedCorners = false;
        StraightEdges = false;
        Octagonal = false;
        break;
      case RoomShape.Octagonal:
        Octagonal = true;
        Ellipse = false;
        StraightEdges = false;
        RoundedCorners = false;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(pShape), pShape, null);
    }
  }

  public void MarkNameInvalid()
  {
    NameLayoutInvalidated?.Invoke(this, EventArgs.Empty);
  }

  public void InvalidateObjectList()
  {
    ObjectsLayoutInvalidated?.Invoke(this, EventArgs.Empty);
  }

  public void InvalidateSubTitle()
  {
    SubTitleLayoutInvalidated?.Invoke(this, EventArgs.Empty);
  }

  public class CompassPort : Port
  {
    public CompassPort(CompassPoint compassPoint, Element owner) : base(owner)
    {
      CompassPoint = compassPoint;
    }

    public CompassPoint CompassPoint { get; set; }

    public override string Id {
      get {
        CompassPointHelper.ToName(CompassPoint, out var name);
        return name;
      }
    }
  }
}
