using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.UI;

namespace Trizbort.Domain.Controllers;

public class CopyController {
  private static readonly (string Name, Func<Room, Color> Get, Action<Room, Color> Set)[] _roomColors = {
    (nameof(Room.RoomBorderColor), room => room.RoomBorderColor, (room, color) => room.RoomBorderColor = color),
    (nameof(Room.RoomFillColor), room => room.RoomFillColor, (room, color) => room.RoomFillColor = color),
    (nameof(Room.RoomNameColor), room => room.RoomNameColor, (room, color) => room.RoomNameColor = color),
    (nameof(Room.RoomObjectTextColor), room => room.RoomObjectTextColor, (room, color) => room.RoomObjectTextColor = color),
    (nameof(Room.RoomSubtitleColor), room => room.RoomSubtitleColor, (room, color) => room.RoomSubtitleColor = color),
    (nameof(Room.SecondFillColor), room => room.SecondFillColor, (room, color) => room.SecondFillColor = color)
  };

  public enum CopyType {
    Rooms,
    Colors
  }

  public enum VertexType {
    Dock,
    Point
  }

  public void CopyColors(Room selectedElement)
  {
    var obj = new CopyColorsObj {
      Colors = _roomColors.Select(color => new CopyColorObj {
        Name = color.Name, Color = color.Get(selectedElement)
      }).ToList(),
      SecondFillLocation = selectedElement.SecondFillLocation
    };

    var clipboardText = JsonConvert.SerializeObject(
      obj,
      Formatting.Indented,
      new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
    UserInteraction.SetClipboardText(clipboardText);
  }

  public void SetRoomColors(Room room, CopyColorsObj copy)
  {
    foreach (var color in copy.Colors) {
      // Ignore unknown clipboard keys for compatibility with other Trizbort versions.
      var property = _roomColors.FirstOrDefault(property => property.Name == color.Name);
      property.Set?.Invoke(room, color.Color);
    }
    room.SecondFillLocation = copy.SecondFillLocation;
  }

  public void CopyElements(List<Element> selectedElements)
  {
    var xx = CreateCopyObject(selectedElements);
    var clipboardText = JsonConvert.SerializeObject(
      xx,
      Formatting.Indented,
      new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
    UserInteraction.SetClipboardText(clipboardText);
  }

  public CopyObject CreateCopyObject(IEnumerable<Element> elements)
  {
    var xx = new CopyObject { Rooms = new List<CopyRoomObj>(), Connections = new List<CopyConnectionObj>() };
    foreach (var element in elements)
      if (element is Room) {
        var copy = CreateCopyObj(element as Room);
        xx.Rooms.Add(copy);
      }
      else if (element is MapLabel label) {
        xx.Labels.Add(
          new CopyLabelObj {
            OldId = label.Id, Text = label.Text, Position = label.Position, Size = label.Size,
            Shape = label.Shape, BorderStyle = label.BorderStyle, HasBackground = label.HasBackground,
            TextColor = label.TextColor, BorderColor = label.BorderColor, BackgroundColor = label.BackgroundColor,
            ZOrder = label.ZOrder
          });
      }
      else if (element is Connection) {
        var copy = CreateCopyObj(element as Connection);
        xx.Connections.Add(copy);
      }

    return xx;
  }

  public void SetLabel(MapLabel label, CopyLabelObj copy)
  {
    label.Text = copy.Text;
    label.Size = copy.Size;
    label.Shape = copy.Shape;
    label.BorderStyle = copy.BorderStyle;
    label.HasBackground = copy.HasBackground;
    label.TextColor = copy.TextColor;
    label.BorderColor = copy.BorderColor;
    label.BackgroundColor = copy.BackgroundColor;
    label.ZOrder = copy.ZOrder;
  }

  public ICopyObj PasteElements()
  {
    ICopyObj xx;
    try {
      var clipboardText = UserInteraction.GetClipboardText();

      if (clipboardText.Contains("\"CopyType\": 1"))
        xx = JsonConvert.DeserializeObject<CopyColorsObj>(clipboardText);
      else
        xx = JsonConvert.DeserializeObject<CopyObject>(clipboardText);
    }
    catch {
      xx = null;
    }

    return xx;
  }

  public void SetConnection(Connection newConnection, CopyConnectionObj connection)
  {
    newConnection.ConnectionColor = connection.ConnectionColor;
    newConnection.Description = connection.Description;
    newConnection.Door = connection.Door == null
      ? null
      : new Door {
        Lockable = connection.Door.Lockable, Locked = connection.Door.Locked,
        Open = connection.Door.Open, Openable = connection.Door.Openable
      };
    newConnection.EndText = connection.EndText;
    newConnection.Flow = connection.Flow;
    newConnection.MidText = connection.MidText;
    newConnection.Name = connection.Name;
    newConnection.StartText = connection.StartText;
    newConnection.Style = connection.Style;
  }

  public void SetCurveWaypoints(Connection newConnection, CopyConnectionObj connection, Vector offset)
  {
    if (connection.CurveWaypoints == null) return;
    for (var i = 0; i < connection.CurveWaypoints.Length && i <= (int)CurveWaypoint.ThreeQuarter; ++i)
      if (connection.CurveWaypoints[i].HasValue)
        newConnection.SetCurveWaypoint((CurveWaypoint)i, connection.CurveWaypoints[i].Value - offset);
  }

  public void SetRoom(Room newRoom, CopyRoomObj room)
  {
    newRoom.OldId = room.Id;
    newRoom.AddDescription(room.PrimaryDescription);
    newRoom.Shape = room.Shape;
    newRoom.Name = room.Name;
    newRoom.SubTitle = room.SubTitle;
    newRoom.IsDark = room.IsDark;
    newRoom.Objects = room.Objects;
    newRoom.ObjectsPosition = room.ObjectsPosition;
    newRoom.BorderStyle = room.BorderStyle;
    newRoom.Region = room.Region;
    newRoom.Corners = new CornerRadii {
      TopLeft = room.Corners.TopLeft, TopRight = room.Corners.TopRight,
      BottomLeft = room.Corners.BottomLeft, BottomRight = room.Corners.BottomRight
    };
    newRoom.RoundedCorners = room.RoundedCorners;
    newRoom.Octagonal = room.Octagonal;
    newRoom.Ellipse = room.Ellipse;
    newRoom.StraightEdges = room.StraightEdges;
    newRoom.HandDrawnStyle = room.HandDrawnStyle;
    newRoom.AllCornersEqual = room.AllCornersEqual;
    newRoom.ArbitraryAutomappedPosition = room.ArbitraryAutomappedPosition;
    newRoom.RoomFillColor = room.RoomFillColor;
    newRoom.SecondFillColor = room.SecondFillColor;
    newRoom.SecondFillLocation = room.SecondFillLocation;
    newRoom.RoomBorderColor = room.RoomBorderColor;
    newRoom.RoomNameColor = room.RoomNameColor;
    newRoom.RoomSubtitleColor = room.RoomSubtitleColor;
    newRoom.RoomObjectTextColor = room.RoomObjectColor;
    newRoom.OldId = room.OldId;
    newRoom.ObjectsCustomPositionDown = room.ObjectsCustomPositionDown;
    newRoom.ObjectsCustomPositionRight = room.ObjectsCustomPositionRight;
    newRoom.ObjectsCustomPosition = room.ObjectsCustomPosition;
    newRoom.Size = room.Size;
    newRoom.ReferenceRoomId = room.ReferenceRoomId;
  }

  private CopyRoomObj CreateCopyObj(Room room)
  {
    var xx = new CopyRoomObj {
      Name = room.Name,
      Shape = room.Shape,
      SubTitle = room.SubTitle,
      IsDark = room.IsDark,
      Objects = room.Objects,
      ObjectsPosition = room.ObjectsPosition,
      BorderStyle = room.BorderStyle,
      Region = room.Region,
      Corners = room.Corners,
      RoundedCorners = room.RoundedCorners,
      Octagonal = room.Octagonal,
      Ellipse = room.Ellipse,
      StraightEdges = room.StraightEdges,
      HandDrawnStyle = room.HandDrawnStyle,
      AllCornersEqual = room.AllCornersEqual,
      IsStartRoom = room.IsStartRoom,
      IsEndRoom = room.IsEndRoom,
      ArbitraryAutomappedPosition = room.ArbitraryAutomappedPosition,
      PrimaryDescription = room.PrimaryDescription,
      RoomFillColor = room.RoomFillColor,
      SecondFillColor = room.SecondFillColor,
      SecondFillLocation = room.SecondFillLocation,
      RoomBorderColor = room.RoomBorderColor,
      RoomNameColor = room.RoomNameColor,
      RoomSubtitleColor = room.RoomSubtitleColor,
      RoomObjectColor = room.RoomObjectTextColor,
      OldId = room.Id,
      ObjectsCustomPositionDown = room.ObjectsCustomPositionDown,
      ObjectsCustomPositionRight = room.ObjectsCustomPositionRight,
      ObjectsCustomPosition = room.ObjectsCustomPosition,
      Position = room.Position,
      Size = room.Size,
      ReferenceRoomId = room.ReferenceRoomId
    };


    return xx;
  }

  private CopyConnectionObj CreateCopyObj(Connection conn)
  {
    var xx = new CopyConnectionObj {
      ConnectionColor = conn.ConnectionColor,
      Description = conn.Description,
      Door = conn.Door,
      EndText = conn.EndText,
      Flow = conn.Flow,
      MidText = conn.MidText,
      Name = conn.Name,
      StartText = conn.StartText,
      Style = conn.Style,
      CurveWaypoints = new[] {
        conn.GetCurveWaypoint(CurveWaypoint.Quarter), conn.GetCurveWaypoint(CurveWaypoint.Middle),
        conn.GetCurveWaypoint(CurveWaypoint.ThreeQuarter)
      },
      VertextList = new List<CopyVertexObj>()
    };

    var ii = 0;
    foreach (var vertex in conn.VertexList) {
      var yy = new CopyVertexObj { Index = ii, Position = vertex.Position };
      if (vertex.Port != null) {
        yy.Type = VertexType.Dock;
        yy.OwnerId = vertex.Port.Owner.Id;
        yy.PortId = vertex.Port.Id;
      }
      else {
        yy.Type = VertexType.Point;
        yy.Position = vertex.Position;
      }

      xx.VertextList.Add(yy);

      ii++;
    }

    return xx;
  }

  internal List<Connection> PasteConnections(
    Project project,
    IEnumerable<CopyConnectionObj> connections,
    IReadOnlyDictionary<int, Element> copiedNodes,
    Vector offset)
  {
    var result = new List<Connection>();
    foreach (var copy in connections) {
      var connection = new Connection(project);
      project.Elements.Add(connection);
      SetConnection(connection, copy);
      foreach (var vertex in copy.VertextList)
        if (vertex.Type == VertexType.Dock && copiedNodes.TryGetValue(vertex.OwnerId, out var node))
          connection.VertexList.Add(new Vertex(node.PortList.First(port => port.Id == vertex.PortId)));
        else
          connection.VertexList.Add(new Vertex(vertex.Position - offset));
      SetCurveWaypoints(connection, copy, offset);
      result.Add(connection);
    }

    return result;
  }

  public class CopyObject : ICopyObj {
    public List<CopyConnectionObj> Connections;
    public List<CopyRoomObj> Rooms;
    public List<CopyLabelObj> Labels { get; set; } = new();
  }

  public class CopyLabelObj {
    public int OldId { get; set; }
    public string Text { get; set; }
    public Vector Position { get; set; }
    public Vector Size { get; set; }
    public RoomShape Shape { get; set; }
    public BorderDashStyle BorderStyle { get; set; }
    public bool HasBackground { get; set; }
    public Color TextColor { get; set; }
    public Color BorderColor { get; set; }
    public Color BackgroundColor { get; set; }
    public int ZOrder { get; set; }
  }

  public class CopyConnectionObj {
    public Color ConnectionColor { get; set; }
    public string Description { get; set; }
    public Door Door { get; set; }
    public string EndText { get; set; }
    public ConnectionFlow Flow { get; set; }
    public string MidText { get; set; }
    public string Name { get; set; }
    public string StartText { get; set; }
    public ConnectionStyle Style { get; set; }
    public Vector?[] CurveWaypoints { get; set; }
    public List<CopyVertexObj> VertextList { get; set; }
  }

  public class CopyVertexObj {
    public int Index { get; set; }
    public int OwnerId { get; set; }
    public string PortId { get; set; }
    public Vector Position { get; set; }
    public VertexType Type { get; set; }
  }

  public class CopyRoomObj {
    public bool AllCornersEqual { get; set; }
    public bool ArbitraryAutomappedPosition { get; set; }
    public BorderDashStyle BorderStyle { get; set; }
    public CornerRadii Corners { get; set; }
    public bool Ellipse { get; set; }
    public int Id { get; set; }
    public bool IsDark { get; set; }
    public bool IsEndRoom { get; set; }
    public bool IsStartRoom { get; set; }
    public string Name { get; set; }
    public string Objects { get; set; }
    public bool ObjectsCustomPosition { get; set; }
    public int ObjectsCustomPositionDown { get; set; }
    public int ObjectsCustomPositionRight { get; set; }
    public CompassPoint ObjectsPosition { get; set; }
    public bool Octagonal { get; set; }
    public int OldId { get; set; }
    public Vector Position { get; set; }
    public string PrimaryDescription { get; set; }
    public string Region { get; set; }
    public Color RoomBorderColor { get; set; }
    public Color RoomFillColor { get; set; }
    public Color RoomNameColor { get; set; }
    public Color RoomObjectColor { get; set; }
    public Color RoomSubtitleColor { get; set; }
    public bool RoundedCorners { get; set; }
    public Color SecondFillColor { get; set; }
    public string SecondFillLocation { get; set; }
    public RoomShape Shape { get; set; }
    public Vector Size { get; set; }
    public bool StraightEdges { get; set; }
    public HandDrawnStyle HandDrawnStyle { get; set; }
    public string SubTitle { get; set; }
    public int ReferenceRoomId { get; set; }
  }


  public class CopyColorsObj : ICopyObj {
    public List<CopyColorObj> Colors { get; set; }
    public CopyType CopyType => CopyType.Colors;
    public string SecondFillLocation { get; set; }
  }

  public class CopyColorObj {
    public Color Color { get; set; }
    public string Name { get; set; }
  }

  public interface ICopyObj {
  }
}