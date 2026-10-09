using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using PdfSharp.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Extensions;
using Trizbort.Setup;
using Trizbort.UI;
using Trizbort.Util;

namespace Trizbort.Domain.Elements;

/// <summary>
///   A room in the project.
/// </summary>
public class Room : Element, ISizeable
{
  private const CompassPoint DefaultObjectsPosition = CompassPoint.South;
  private readonly List<string> _descriptions = new();
  private readonly TextBlock _name = new();
  private readonly TextBlock _objects = new();
  private readonly TextBlock _objectsDisplay = new();
  private readonly TextBlock _subTitle = new();
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

  // Added for linking connections when pasting

  private Vector _position;

  // Added for Room specific colors (White shows global color)
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

  // Added this second constructor to be used when loading a room
  // This constructor is significantly faster as it doesn't look for gap in the element IDs
  public Room(Project project, int totalIDs) : base(project, totalIDs)
  {
    Name = Settings.DefaultRoomName;
    Region = Misc.Region.DefaultRegion;
    Size = new Vector(3 * Settings.GridSize, 2 * Settings.GridSize);
    Position = new Vector(-Size.X / 2, -Size.Y / 2);
    Corners = new CornerRadii();
    Shape = RoomShape.SquareCorners; //would be nice to make an app default later: issue #93

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
      if (_borderStyle == value) return;
      _borderStyle = value;
      RaiseChanged();
    }
  }

  public CornerRadii Corners {
    get { return _corners; }
    set {
      if (_corners == null)
      {
        _corners = value;
        return;
      }

      if (_corners.BottomLeft != value.BottomLeft)
      {
        _corners.BottomLeft = value.BottomLeft;
        RaiseChanged();
      } //mCorners never equals value...

      if (_corners.BottomRight != value.BottomRight)
      {
        _corners.BottomRight = value.BottomRight;
        RaiseChanged();
      }

      if (_corners.TopLeft != value.TopLeft)
      {
        _corners.TopLeft = value.TopLeft;
        RaiseChanged();
      }

      if (_corners.TopRight != value.TopRight)
      {
        _corners.TopRight = value.TopRight;
        RaiseChanged();
      }
    }
  }

  public override Depth Depth => Depth.Medium;

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

  /// <summary>Per-room override of the map's hand-drawn setting.</summary>
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

  /// <summary>Whether this room is actually drawn hand-drawn, taking the map setting into account.</summary>
  public bool IsHandDrawn => _handDrawnStyle == HandDrawnStyle.HandDrawn ||
                             _handDrawnStyle == HandDrawnStyle.MapDefault && Settings.HandDrawn;

  public bool HasDescription => _descriptions.Count > 0;
  public override bool HasDialog => true;

  [JsonIgnore]
  public bool IsConnected {
    get {
      return Project.Current.Elements.OfType<Connection>()
                    .Any(element => element
                                    .VertexList.Select(vertex => vertex.Port)
                                    .Any(port => port != null && port.Owner == this));
    }
  }

  /// <summary>
  ///   Get/set whether the room is dark or lit.
  /// </summary>
  public bool IsDark {
    get { return _isDark; }
    set {
      if (_isDark == value) return;
      _isDark = value;
      RaiseChanged();
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

  /// <summary>
  ///   Get/set the name of the room.
  /// </summary>
  public override string Name {
    get { return _name.Text; }
    set {
      value = value ?? string.Empty;
      if (_name.Text == value) return;
      _name.Text = value;
      RaiseChanged();
    }
  }

  /// <summary>
  ///   Get/set the list of objects in the room.
  /// </summary>
  public string Objects {
    get { return _objects.Text; }
    set {
      value = value ?? string.Empty;
      if (_objects.Text == value) return;
      _objects.Text = value;
      RaiseChanged();
    }
  }

  public bool ObjectsCustomPosition { get; set; }

  public int ObjectsCustomPositionDown { get; set; }
  public int ObjectsCustomPositionRight { get; set; }

  /// <summary>
  ///   Get/set the position, relative to the room,
  ///   at which the object list is drawn on the map.
  /// </summary>
  public CompassPoint ObjectsPosition {
    get { return _objectsPosition; }
    set {
      if (_objectsPosition == value) return;
      _objectsPosition = value;
      RaiseChanged();
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

  // Added for linking connections when pasting
  public int OldId { get; set; }

  public string PrimaryDescription {
    get {
      if (_descriptions.Count > 0)
        return _descriptions[0];
      return null;
    }
  }

  public Room ReferenceRoom => Project.Current.Elements.OfType<Room>().FirstOrDefault(p => p.Id == ReferenceRoomId);
  public int ReferenceRoomId { get; set; } = -1;

  public string Region {
    get { return _roomRegion; }
    set {
      if (_roomRegion != value)
      {
        _roomRegion = value;
        RaiseChanged();
      }
    }
  }

  // Added for Room specific colors
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

  // Added for Room specific colors
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

  // Added for Room specific colors
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

  // Added for Room specific colors
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

  // Added for Room specific colors
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

  // Added for Room specific colors
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

  public bool StraightEdges {
    get { return _straightEdges; }
    set {
      if (_straightEdges != value)
        _straightEdges = value; //RaiseChanged(); This is disabled because it gives false positives
    }
  }

  /// <summary>
  ///   Get/set the subtitle of the room.
  /// </summary>
  public string SubTitle {
    get { return _subTitle.Text; }
    set {
      value = value ?? string.Empty;
      if (_subTitle.Text == value) return;
      _subTitle.Text = value;

      RaiseChanged();
    }
  }

  public List<RoomValidationState> ValidationState { get; set; } = new();

  public sealed override Vector Position {
    get { return _position; }
    set {
      if (_position != value)
      {
        _position = value;
        ArbitraryAutomappedPosition = false;
        RaiseChanged();
      }
    }
  }

  public float X => _position.X;
  public float Y => _position.Y;
  public float Height => _size.Y;

  public Rect InnerBounds => new(Position, Size);

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

  public float Width => _size.X;

  public void AddDescription(string description)
  {
    if (string.IsNullOrEmpty(description))
      return;

    if (_descriptions.Any(existing => existing == description))
      return;

    // we don't have this (non-empty) description already; add it
    _descriptions.Add(description);
    RaiseChanged();
  }

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
      state = new RoomValidationState {
        Message = "There is no description for this room.",
        Status = RoomValidationStatus.Invalid,
        Type = ValidationType.RoomDescription
      };
      ValidationState.Add(state);
    }

    if (Project.Current.MustHaveUniqueNames)
      if (Project.Current.Elements.OfType<Room>().Count(p => p.Name == Name) > 1)
      {
        state = new RoomValidationState {
          Message = "The room name is not unique.",
          Status = RoomValidationStatus.Invalid,
          Type = ValidationType.RoomUniqueName
        };
        ValidationState.Add(state);
      }

    if (Project.Current.MustHaveNoDanglingConnectors)
      if (Project.Current.Elements.OfType<Connection>().Count(p => p.GetSourceRoom() == this && p.IsDangling) > 0)
      {
        state = new RoomValidationState {
          Message = "Room has dangling connectors.",
          Status = RoomValidationStatus.Invalid,
          Type = ValidationType.RoomDanglingConnection
        };
        ValidationState.Add(state);
      }

    if (Project.Current.MustHaveSubtitle)
      if (string.IsNullOrWhiteSpace(SubTitle))
      {
        state = new RoomValidationState {
          Message = "Room must have a subtitle.",
          Status = RoomValidationStatus.Invalid,
          Type = ValidationType.RoomSubTitle
        };
        ValidationState.Add(state);
      }
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
    else UserInteraction.ShowMessage("No connections were deleted.", "Nothing to delete");

    /*      foreach (var b in this.L)
          {
            Project.Current.Elements.Remove(b.);
          }*/
  }


  public override float Distance(Vector pos, bool includeMargins)
  {
    var bounds = UnionBoundsWith(Rect.Empty, includeMargins);
    return pos.DistanceFromRect(bounds);
  }

  public override void Draw(XGraphics graphics, Palette palette, DrawingContext context)
  {
    CheckValidation();
    //if we are drawing a new room, we need to edit the code at three points:
    //1. if (IsStartRoom) => start room outline
    //2. if (context.Selected) => selected room outline
    //3. if (!Settings.DebugDisableLineRendering => main default outline

    var handDrawn = IsHandDrawn;
    StraightEdges = !handDrawn;

    var random = Sketch.Seeded(Id + 1000);

    var topLeft = InnerBounds.GetCorner(CompassPoint.NorthWest);
    var topRight = InnerBounds.GetCorner(CompassPoint.NorthEast);
    var bottomLeft = InnerBounds.GetCorner(CompassPoint.SouthWest);
    var bottomRight = InnerBounds.GetCorner(CompassPoint.SouthEast);

    var topCenter = InnerBounds.GetCorner(CompassPoint.North);
    var rightCenter = InnerBounds.GetCorner(CompassPoint.East);
    var bottomCenter = InnerBounds.GetCorner(CompassPoint.South);
    var leftCenter = InnerBounds.GetCorner(CompassPoint.West);

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

    // if starting room: this is the code to draw a yellow-green boundary around the start room
    if (IsStartRoom || IsEndRoom || IsReference)
    {
      var tBounds = InnerBounds;
      tBounds.Inflate(5);

      var q = tBounds.Left;

      var topLeftSelect = tBounds.GetCorner(CompassPoint.NorthWest);
      var topRightSelect = tBounds.GetCorner(CompassPoint.NorthEast);
      var bottomLeftSelect = tBounds.GetCorner(CompassPoint.SouthWest);
      var bottomRightSelect = tBounds.GetCorner(CompassPoint.SouthEast);

      var topSelect = new LineSegment(topLeftSelect, topRightSelect);
      var rightSelect = new LineSegment(topRightSelect, bottomRightSelect);
      var bottomSelect = new LineSegment(bottomRightSelect, bottomLeftSelect);
      var leftSelect = new LineSegment(bottomLeftSelect, topLeftSelect);

      var pathSelected = palette.Path();
      AddOutline(pathSelected, tBounds, handDrawn);

      SolidBrush brushSelected;
      if (IsReference)
        brushSelected = new SolidBrush(Color.LightBlue);
      else
        brushSelected = new SolidBrush(Settings.Color[IsStartRoom ? Colors.StartRoom : Colors.EndRoom]);
      graphics.DrawPath(brushSelected, pathSelected);
    }

    //this is the code to draw the yellow boundary around a selected room
    if (context.Selected)
    {
      var tBounds = InnerBounds;
      tBounds.Inflate(Project.Current.ActiveSelectedElement?.Id == Id ? 10 : 5);

      var topLeftSelect = tBounds.GetCorner(CompassPoint.NorthWest);
      var topRightSelect = tBounds.GetCorner(CompassPoint.NorthEast);
      var bottomLeftSelect = tBounds.GetCorner(CompassPoint.SouthWest);
      var bottomRightSelect = tBounds.GetCorner(CompassPoint.SouthEast);

      var topSelect = new LineSegment(topLeftSelect, topRightSelect);
      var rightSelect = new LineSegment(topRightSelect, bottomRightSelect);
      var bottomSelect = new LineSegment(bottomRightSelect, bottomLeftSelect);
      var leftSelect = new LineSegment(bottomLeftSelect, topLeftSelect);

      var pathSelected = palette.Path();
      AddOutline(pathSelected, tBounds, handDrawn);

      var brushSelected = Project.Current.ActiveSelectedElement?.Id == Id
        ? new SolidBrush(Color.Gold)
        : new SolidBrush(Color.Gold);
      graphics.DrawPath(brushSelected, pathSelected);
    }

    // get region color
    var regionColor =
      Settings.Regions.FirstOrDefault(p => p.RegionName.Equals(Region, StringComparison.OrdinalIgnoreCase)) ??
      Settings.Regions.FirstOrDefault(p => p.RegionName.Equals(
        Misc.Region.DefaultRegion,
        StringComparison.OrdinalIgnoreCase));
    Brush brush = new SolidBrush(regionColor.RColor);

    // Room specific fill brush (White shows global color)
    if (RoomFillColor != Color.Transparent) brush = new SolidBrush(RoomFillColor);

    // this is the main drawing routine for the actual room borders
    if (!ApplicationSettingsController.AppSettings.DebugDisableLineRendering && BorderStyle != BorderDashStyle.None)
    {
      var path = palette.Path();
      AddOutline(path, InnerBounds, handDrawn);

      graphics.DrawPath(brush, path);

      // Second fill for room specific colors with a split option
      if (SecondFillColor != Color.Transparent)
      {
        var state = graphics.Save();
        graphics.IntersectClip(path);

        // Set the second fill color
        brush = new SolidBrush(SecondFillColor);

        // Define the second path based on the second fill location
        var secondPath = palette.Path();
        switch (SecondFillLocation)
        {
          case "Bottom":
            Drawing.AddLine(secondPath, centerHorizontal, random, StraightEdges);
            Drawing.AddLine(secondPath, halfRightBottom, random, StraightEdges);
            Drawing.AddLine(secondPath, bottom, random, StraightEdges);
            Drawing.AddLine(secondPath, halfLeftBottom, random, StraightEdges);
            break;
          case "BottomRight":
            Drawing.AddLine(secondPath, slantUp, random, StraightEdges);
            Drawing.AddLine(secondPath, right, random, StraightEdges);
            Drawing.AddLine(secondPath, bottom, random, StraightEdges);
            break;
          case "BottomLeft":
            Drawing.AddLine(secondPath, slantDown, random, StraightEdges);
            Drawing.AddLine(secondPath, bottom, random, StraightEdges);
            Drawing.AddLine(secondPath, left, random, StraightEdges);
            break;
          case "Left":
            Drawing.AddLine(secondPath, halfTopLeft, random, StraightEdges);
            Drawing.AddLine(secondPath, left, random, StraightEdges);
            Drawing.AddLine(secondPath, halfBottomLeft, random, StraightEdges);
            Drawing.AddLine(secondPath, centerVertical, random, StraightEdges);
            break;
          case "Right":
            Drawing.AddLine(secondPath, halfTopRight, random, StraightEdges);
            Drawing.AddLine(secondPath, right, random, StraightEdges);
            Drawing.AddLine(secondPath, halfBottomRight, random, StraightEdges);
            Drawing.AddLine(secondPath, centerVertical, random, StraightEdges);
            break;
          case "TopRight":
            Drawing.AddLine(secondPath, top, random, StraightEdges);
            Drawing.AddLine(secondPath, right, random, StraightEdges);
            Drawing.AddLine(secondPath, slantDown, random, StraightEdges);
            break;
          case "TopLeft":
            Drawing.AddLine(secondPath, top, random, StraightEdges);
            Drawing.AddLine(secondPath, slantUp, random, StraightEdges);
            Drawing.AddLine(secondPath, left, random, StraightEdges);
            break;
          case "Top":
            Drawing.AddLine(secondPath, centerHorizontal, random, StraightEdges);
            Drawing.AddLine(secondPath, halfRightTop, random, StraightEdges);
            Drawing.AddLine(secondPath, top, random, StraightEdges);
            Drawing.AddLine(secondPath, halfLeftTop, random, StraightEdges);
            break;
        }

        // Draw the second fill over the first
        graphics.DrawPath(brush, secondPath);
        graphics.Restore(state);
      }

      if (IsDark)
      {
        var state = graphics.Save();
        var solidbrush = (SolidBrush)palette.BorderBrush;
        var darknessXDistance = Settings.DarknessStripeSize;
        var darknessYDistance = Settings.DarknessStripeSize;
        graphics.IntersectClip(path);
        if (Ellipse)
        {
          darknessYDistance = 2 * Height / 5;
          darknessXDistance = 2 * Width / 5;
        }
        else if (RoundedCorners)
        {
          if (Corners.TopRight > 2 * Settings.DarknessStripeSize)
            darknessYDistance = darknessXDistance = (float)Corners.TopRight / 2;
        }
        else if (Octagonal)
        {
          darknessXDistance = Width * 7 / 20;
          darknessYDistance = Height * 7 / 20;
        }

        graphics.DrawPolygon(
          solidbrush,
          new[] {
            topRight.ToPointF(), new PointF(topRight.X - darknessXDistance, topRight.Y),
            new PointF(topRight.X, topRight.Y + darknessYDistance)
          },
          XFillMode.Alternate);
        graphics.Restore(state);
      }

      if (RoomBorderColor == Color.Transparent)
      {
        var pen = palette.BorderPen;
        pen.DashStyle = IsReference ? BorderDashStyle.Dot.ConvertToDashStyle() : BorderStyle.ConvertToDashStyle();
        graphics.DrawPath(pen, path);
      }
      else
      {
        var roomBorderPen = new Pen(RoomBorderColor, Settings.LineWidth) {
          StartCap = LineCap.Round, EndCap = LineCap.Round,
          DashStyle = IsReference ? BorderDashStyle.Dot.ConvertToDashStyle() : BorderStyle.ConvertToDashStyle()
        };
        graphics.DrawPath(roomBorderPen, path);
      }
    }

    var font = Settings.RoomNameFont;
    var roombrush = new SolidBrush(regionColor.TextColor);
    // Room specific fill brush (White shows global color)

    if (RoomNameColor != Color.Transparent) roombrush = new SolidBrush(RoomNameColor);

    var textBounds = InnerBounds;
    if (Ellipse)
      textBounds.Inflate(-11.5f);
    else
      textBounds.Inflate(-5, -5);

    if (textBounds.Width > 0 && textBounds.Height > 0)
      if (!ApplicationSettingsController.AppSettings.DebugDisableTextRendering)
      {
        var tName = IsReference ? new TextBlock { Text = "To" } : _name;
        var tSubtitle = IsReference ? new TextBlock { Text = ReferenceRoom.Name } : _subTitle;
        var roomTextRect = tName.Draw(
          graphics,
          font,
          roombrush,
          textBounds.Position,
          textBounds.Size,
          XStringFormats.Center);

        // draw subtitle text
        var subTitleBrush = IsReference ? roombrush :
          RoomSubtitleColor != Color.Transparent ? new SolidBrush(RoomSubtitleColor) : palette.SubtitleTextBrush;
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

    var expandedBounds = InnerBounds;
    expandedBounds.Inflate(Settings.ObjectListOffsetFromRoom, Settings.ObjectListOffsetFromRoom);
    var drawnObjectList = false;

    font = Settings.ObjectFont;
    brush = palette.SmallTextBrush;
    // Room specific fill brush (White shows global color)
    var bUseObjectRoomBrush = false;
    if (RoomObjectTextColor != Color.Transparent)
    {
      bUseObjectRoomBrush = true;
      brush = new SolidBrush(RoomObjectTextColor);
    }

    if (!string.IsNullOrEmpty(Objects))
    {
      var format = new XStringFormat();
      var pos = expandedBounds.GetCorner(_objectsPosition);

      var displayText = ObjectList.FormatForDisplay(_objects.Text);
      if (_objectsDisplay.Text != displayText) _objectsDisplay.Text = displayText;

      if (!Drawing.SetAlignmentFromCardinalOrOrdinalDirection(format, _objectsPosition))
      {
        // object list appears inside the room below its name
        format.LineAlignment = XLineAlignment.Far;
        format.Alignment = XStringAlignment.Near;
        var height = InnerBounds.Height / 2 - font.Height / 2;
        var bounds = new Rect(
          InnerBounds.Left + Settings.ObjectListOffsetFromRoom,
          InnerBounds.Bottom - height,
          InnerBounds.Width - Settings.ObjectListOffsetFromRoom,
          height - Settings.ObjectListOffsetFromRoom);
        if (bUseObjectRoomBrush)
          brush = new SolidBrush(RoomObjectTextColor);
        pos = bounds.Position;
        pos.X += ObjectsCustomPosition ? ObjectsCustomPositionRight : 0;
        pos.Y += ObjectsCustomPosition ? ObjectsCustomPositionDown : 0;
        if (bounds.Width > 0 && bounds.Height > 0)
          _objectsDisplay.Draw(graphics, font, brush, pos, bounds.Size, format);
        drawnObjectList = true;
      }
      else if (_objectsPosition == CompassPoint.North || _objectsPosition == CompassPoint.South)
      {
        pos.X += Settings.ObjectListOffsetFromRoom + (ObjectsCustomPosition ? ObjectsCustomPositionRight : 0);
        pos.Y += ObjectsCustomPosition ? ObjectsCustomPositionDown : 0;
      }
      else
      {
        pos.X += ObjectsCustomPosition ? ObjectsCustomPositionRight : 0;
        pos.Y += ObjectsCustomPosition ? ObjectsCustomPositionDown : 0;
      }

      if (!drawnObjectList)
        if (!ApplicationSettingsController.AppSettings.DebugDisableTextRendering)
        {
          if (format.Alignment != XStringAlignment.Near && displayText.Contains(ObjectList.DisplayBullet))
          {
            // right-aligning each line would lose the indentation of contained objects,
            // so instead left-align the lines within a block whose right edge is at pos
            var width = displayText.Replace("\r", string.Empty).Split('\n')
                                   .Max(line => graphics.MeasureString(line, font).Width);
            pos.X -= (float)(format.Alignment == XStringAlignment.Center ? width / 2 : width);
            format.Alignment = XStringAlignment.Near;
          }

          _objectsDisplay.Draw(graphics, font, brush, pos, Vector.Zero, format);
        }
    }

    if (!Valid())
    {
      var path = palette.Path();
      var pen = new Pen(Color.Red, 2.0f);
      pen.DashStyle = DashStyle.Solid;

      var xx = new PointF[2];
      xx[0] = new PointF(InnerBounds.Left, InnerBounds.Top);
      xx[1] = new PointF(InnerBounds.Right, InnerBounds.Bottom);

      path.AddLines(xx);
      graphics.DrawPath(pen, path);

      var yy = new PointF[2];
      yy[0] = new PointF(InnerBounds.Right, InnerBounds.Top);
      yy[1] = new PointF(InnerBounds.Left, InnerBounds.Bottom);

      path = palette.Path();
      path.AddLines(yy);

      graphics.DrawPath(pen, path);
    }
  }

  public List<Connection> GetConnections()
  {
    return GetConnections(null);
  }

  public List<Connection> GetConnections(CompassPoint? compassPoint)
  {
    var connections = new List<Connection>();

    // TODO: This is needlessly expensive, traversing as it does the entire project's element list.
    foreach (var element in Project.Current.Elements.OfType<Connection>())
    {
      var connection = element;
      foreach (var vertex in connection.VertexList)
      {
        var port = vertex.Port;
        if (port == null || port.Owner != this || !(port is CompassPort))
          continue;

        var compassPort = (CompassPort)vertex.Port;

        if (compassPoint == null)
          connections.Add(connection);
        else if (compassPort.CompassPoint == compassPoint)
          connections.Add(connection);
      }
    }

    return connections;
  }

  public override Vector GetPortPosition(Port port)
  {
    // map the compass points onto our bounding rectangle
    var compass = (CompassPort)port;

    if (Ellipse)
      return InnerBounds.GetCorner(compass.CompassPoint, RoomShape.Ellipse);

    if (RoundedCorners)
      return InnerBounds.GetCorner(compass.CompassPoint, RoomShape.RoundedCorners, Corners);

    if (Octagonal)
      return InnerBounds.GetCorner(compass.CompassPoint, RoomShape.Octagonal, Corners);

    return InnerBounds.GetCorner(compass.CompassPoint, RoomShape.SquareCorners, Corners);
  }


  public override Vector GetPortStalkPosition(Port port)
  {
    var outerBounds = InnerBounds;
    outerBounds.Inflate(Settings.ConnectionStalkLength);
    var compass = (CompassPort)port;
    var inner = InnerBounds.GetCorner(compass.CompassPoint);
    var outer = outerBounds.GetCorner(compass.CompassPoint);
    switch (compass.CompassPoint)
    {
      case CompassPoint.EastNorthEast:
      case CompassPoint.EastSouthEast:
      case CompassPoint.WestNorthWest:
      case CompassPoint.WestSouthWest:
        return new Vector(outer.X, inner.Y);
      case CompassPoint.NorthNorthEast:
      case CompassPoint.NorthNorthWest:
      case CompassPoint.SouthSouthEast:
      case CompassPoint.SouthSouthWest:
        return new Vector(inner.X, outer.Y);
      default:
        return outer;
    }
  }


  public override Color GetToolTipColor()
  {
    return !Valid() ? Color.Red : Color.LightBlue;
  }

  public override string GetToolTipFooter()
  {
    return ApplicationSettingsController.AppSettings.ShowObjectsInTooltips ? Objects : string.Empty;
  }

  public override string GetToolTipHeader()
  {
    var text = $"{Name}{(!IsDefaultRegion() ? $" ({Region})" : string.Empty)}";
    if (!Valid())
    {
      if (text.Length > 0) text += " - ";
      text += "Room validation issues:";
    }

    return text;
  }

  public override string GetToolTipText()
  {
    if (!Valid())
    {
      var text = "";
      text = ValidationState.Aggregate(
        text,
        (current, roomValidationState) => current + roomValidationState.Message + Environment.NewLine);
      return text;
    }

    var desc = string.Empty;
    if (ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips)
    {
      desc = $"{PrimaryDescription}";
      var charsToShow = ApplicationSettingsController.AppSettings.ToolTipRoomDescriptionCharactersToShow;
      if (ApplicationSettingsController.AppSettings.LimitRoomDescriptionCharactersInTooltip &
          desc.Length >= charsToShow)
      {
        desc = desc.Substring(0, charsToShow);
        desc = desc + "...";
      }
    }


    return desc;
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
    Load(element, (message, title) => UserInteraction.ShowMessage(message, title));
  }

  internal void Load(XmlElementReader element, Action<string, string> reportWarning)
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
        reportWarning(
          $"{Name} is a duplicate start room. You may need to erase \"isStartRoom=YES\" from the XML.",
          "Duplicate start room warning");
      else
        Settings.StartRoomLoaded = true;
    if (IsEndRoom)
      if (Settings.EndRoomLoaded)
        reportWarning(
          $"{Name} is a duplicate end room. You may need to erase \"isEndRoom=YES\" from the XML.",
          "Duplicate end room warning");
      else
        Settings.EndRoomLoaded = true;
    //Note: long term, we probably want an app default for this, but for now, let's force a room shape. #93 should fix this code along with #149.
    //We also should not have two of these at once.
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
    if (string.IsNullOrEmpty(description))
      return _descriptions.Count == 0;

    return _descriptions.Any(existing => existing == description);

    // no match
  }

  public void MarkNameInvalid()
  {
    _name.InvalidateLayout();
  }

  public Port PortAt(CompassPoint compassPoint)
  {
    return PortList.Cast<CompassPort>().FirstOrDefault(port => port.CompassPoint == compassPoint);
  }

  public override void PreDraw(DrawingContext context)
  {
    var topLeft = InnerBounds.GetCorner(CompassPoint.NorthWest);
    var topRight = InnerBounds.GetCorner(CompassPoint.NorthEast);
    var bottomLeft = InnerBounds.GetCorner(CompassPoint.SouthWest);
    var bottomRight = InnerBounds.GetCorner(CompassPoint.SouthEast);

    var topCenter = InnerBounds.GetCorner(CompassPoint.North);
    var rightCenter = InnerBounds.GetCorner(CompassPoint.East);
    var bottomCenter = InnerBounds.GetCorner(CompassPoint.South);
    var leftCenter = InnerBounds.GetCorner(CompassPoint.West);

    var top = new LineSegment(topLeft, topRight);
    var right = new LineSegment(topRight, bottomRight);
    var bottom = new LineSegment(bottomRight, bottomLeft);
    var left = new LineSegment(bottomLeft, topLeft);

    var halfTopRight = new LineSegment(topCenter, topRight);
    var halfBottomRight = new LineSegment(bottomRight, bottomCenter);
    var centerVertical = new LineSegment(bottomCenter, topCenter);

    var centerHorizontal = new LineSegment(leftCenter, rightCenter);
    var halfRightBottom = new LineSegment(rightCenter, bottomRight);
    var halfLeftBottom = new LineSegment(bottomLeft, leftCenter);

    var slantUp = new LineSegment(bottomLeft, topRight);
    var slantDown = new LineSegment(bottomRight, topLeft);

    context.LinesDrawn.Add(top);
    context.LinesDrawn.Add(right);
    context.LinesDrawn.Add(bottom);
    context.LinesDrawn.Add(left);
  }

  public Vector QuarterPoint(Vector frompoint, Vector topoint)
  {
    var retVector = new Vector();
    retVector.X = (frompoint.X * 3 + topoint.X) / 4;
    retVector.Y = (frompoint.Y * 3 + topoint.Y) / 4;
    return retVector;
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

    // Up to this point was added to turn colors to Hex code for xmpl saving/loading

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

  public void ShowDialog(PropertiesStartType startPoint)
  {
    ShowRoomDialog(startPoint);
  }


  public override void ShowDialog()
  {
    ShowRoomDialog(PropertiesStartType.Objects);
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

  private void CreateRoomPath(XGraphicsPath path, LineSegment top, LineSegment left)
  {
    path.AddArc(
      top.Start.X + top.Length - Corners.TopRight * 2,
      top.Start.Y,
      Corners.TopRight * 2,
      Corners.TopRight * 2,
      270,
      90);
    path.AddArc(
      top.Start.X + top.Length - Corners.BottomRight * 2,
      top.Start.Y + left.Length - Corners.BottomRight * 2,
      Corners.BottomRight * 2,
      Corners.BottomRight * 2,
      0,
      90);
    path.AddArc(
      top.Start.X,
      top.Start.Y + left.Length - Corners.BottomLeft * 2,
      Corners.BottomLeft * 2,
      Corners.BottomLeft * 2,
      90,
      90);
    path.AddArc(top.Start.X, top.Start.Y, Corners.TopLeft * 2, Corners.TopLeft * 2, 180, 90);
    path.CloseFigure();
  }

  /// <summary>
  ///   Adds this room's outline (in its shape) for the given bounds. Each call uses a fresh
  ///   generator seeded from the room ID so a hand-drawn outline is stable between redraws
  ///   and unaffected by selection or start/end-room highlighting.
  /// </summary>
  private void AddOutline(XGraphicsPath path, Rect bounds, bool handDrawn)
  {
    var rect = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    var random = Sketch.Seeded(Id);
    if (RoundedCorners)
    {
      if (handDrawn)
        path.AddPolygon(
          Sketch.ClosedCurve(
            Sketch.RoundedRectangle(
              rect,
              (float)Corners.TopLeft,
              (float)Corners.TopRight,
              (float)Corners.BottomRight,
              (float)Corners.BottomLeft),
            random));
      else
        CreateRoomPath(
          path,
          new LineSegment(bounds.GetCorner(CompassPoint.NorthWest), bounds.GetCorner(CompassPoint.NorthEast)),
          new LineSegment(bounds.GetCorner(CompassPoint.SouthWest), bounds.GetCorner(CompassPoint.NorthWest)));
      return;
    }

    if (Ellipse)
    {
      if (handDrawn)
        path.AddPolygon(Sketch.ClosedCurve(Sketch.Ellipse(rect), random));
      else
        path.AddEllipse(rect);
      return;
    }

    PointF[] vertices;
    if (Octagonal)
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

  private bool IsDefaultRegion()
  {
    return Region == Misc.Region.DefaultRegion || string.IsNullOrEmpty(Region);
  }

  /// <summary>
  ///   Copy the visual styling (shape, corners, border, colours, region, darkness and
  ///   objects position) of another room onto this one. Content such as name,
  ///   descriptions and objects is not copied.
  /// </summary>
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

  private void ShowRoomDialog(PropertiesStartType start)
  {
    using var dialog = new RoomPropertiesDialog(start, Id);
    dialog.RoomName = Name;
    dialog.Description = PrimaryDescription;
    dialog.RoomSubTitle = SubTitle;
    dialog.IsDark = IsDark;
    dialog.IsStartRoom = IsStartRoom;
    dialog.IsEndRoom = IsEndRoom;
    dialog.HandDrawnStyle = HandDrawnStyle;
    dialog.Objects = Objects;
    dialog.ObjectsPosition = ObjectsPosition;
    dialog.ObjectsCustomPosition = ObjectsCustomPosition;
    dialog.ObjectsCustomPositionDown = ObjectsCustomPositionDown;
    dialog.ObjectsCustomPositionRight = ObjectsCustomPositionRight;
    dialog.BorderStyle = BorderStyle;

    dialog.RoomFillColor = RoomFillColor;
    dialog.SecondFillColor = SecondFillColor;
    dialog.SecondFillLocation = SecondFillLocation;
    dialog.RoomBorderColor = RoomBorderColor;
    dialog.RoomNameColor = RoomNameColor;
    dialog.ObjectTextColor = RoomObjectTextColor;
    dialog.RoomSubtitleColor = RoomSubtitleColor;
    dialog.RoomRegion = Region;
    dialog.ReferenceRoom = ReferenceRoom;
    dialog.Corners = Corners;
    dialog.RoundedCorners = RoundedCorners;
    //dialog.Octagonal = Octagonal;
    dialog.Ellipse = Ellipse;
    dialog.StraightEdges = StraightEdges;
    dialog.AllCornersEqual = AllCornersEqual;
    dialog.Shape = Shape;

    if (UserInteraction.ShowDialog(dialog, TrizbortApplication.MainForm?.Canvas) == DialogResult.OK)
    {
      Name = dialog.RoomName;
      SubTitle = dialog.RoomSubTitle;
      if (PrimaryDescription != dialog.Description)
      {
        ClearDescriptions();
        AddDescription(dialog.Description);
      }

      IsDark = dialog.IsDark;
      IsStartRoom = dialog.IsStartRoom;
      IsEndRoom = dialog.IsEndRoom;
      HandDrawnStyle = dialog.HandDrawnStyle;
      Objects = dialog.Objects;
      BorderStyle = dialog.BorderStyle;
      ObjectsPosition = dialog.ObjectsPosition;
      ObjectsCustomPosition = dialog.ObjectsCustomPosition;
      ObjectsCustomPositionDown = dialog.ObjectsCustomPositionDown;
      ObjectsCustomPositionRight = dialog.ObjectsCustomPositionRight;
      // Added for Room specific colors
      RoomFillColor = dialog.RoomFillColor;
      RoomSubtitleColor = dialog.RoomSubtitleColor;
      SecondFillColor = dialog.SecondFillColor;
      SecondFillLocation = dialog.SecondFillLocation;
      RoomBorderColor = dialog.RoomBorderColor;
      RoomNameColor = dialog.RoomNameColor;
      RoomObjectTextColor = dialog.ObjectTextColor;


      Region = dialog.RoomRegion;

      ReferenceRoomId = dialog.ReferenceRoom?.Id ?? -1;
      Corners = dialog.Corners;
      RoundedCorners = dialog.RoundedCorners;
      Shape = dialog.Shape;
      //Octagonal = dialog.Octagonal;
      Ellipse = dialog.Ellipse;
      StraightEdges = dialog.StraightEdges;
      AllCornersEqual = dialog.AllCornersEqual;
    }
  }

  private bool Valid()
  {
    return ValidationState == null || ValidationState.Count == 0;
  }

  internal class CompassPort : Port
  {
    public CompassPort(CompassPoint compassPoint, Element owner) : base(owner)
    {
      CompassPoint = compassPoint;
      Room = owner as Room;
    }

    public CompassPoint CompassPoint { get; set; }

    public override string Id {
      get {
        string name;
        return CompassPointHelper.ToName(CompassPoint, out name) ? name : string.Empty;
      }
    }

    public Room Room { get; }
  }
}

public class CornerRadii
{
  private double _bottomLeft = 15.0;
  private double _bottomRight = 15.0;
  private double _topLeft = 15.0;
  private double _topRight = 15.0;

  public double BottomLeft {
    get { return _bottomLeft; }
    set {
      if (value < 1) _bottomLeft = 1;
      else if (value > 30) _bottomLeft = 30;
      else _bottomLeft = value;
    }
  }

  public double BottomRight {
    get { return _bottomRight; }
    set {
      if (value < 1) _bottomRight = 1;
      else if (value > 30) _bottomRight = 30;
      else _bottomRight = value;
    }
  }

  public double TopLeft {
    get { return _topLeft; }
    set {
      if (value < 1) _topLeft = 1;
      else if (value > 30) _topLeft = 30;
      else _topLeft = value;
    }
  }

  public double TopRight {
    get { return _topRight; }
    set {
      if (value < 1) _topRight = 1;
      else if (value > 30) _topRight = 30;
      else _topRight = value;
    }
  }
}

public enum RoomShape
{
  SquareCorners,
  RoundedCorners,
  Ellipse,
  Octagonal,
  NotARoom
}

public enum PropertiesStartType
{
  RoomName,
  Region,
  Objects
}