using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Editor;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Automap;

public class AutomapCanvas : IAutomapCanvas
{
  private readonly Automap _automap;
  private readonly Project _project;
  private readonly EditorSession _session;

  /// <param name="session">
  ///   When given, the room being worked on is also selected in the session and scrolled into view.
  /// </param>
  public AutomapCanvas(Project project = null, Automap automap = null, EditorSession session = null)
  {
    _project = project;
    _session = session;
    _automap = automap ?? Automap.Instance;
  }

  private Project CurrentProject => _project ?? Project.Current;

  public void AddExitStub(Room room, MappableDirection direction)
  {
    if (!CurrentProject.Elements.Contains(room)) return;

    var sourceCompassPoint = CompassPointHelper.GetCompassDirection(direction);
    var connection = AddConnection(room, sourceCompassPoint, room, sourceCompassPoint);
    switch (direction)
    {
      case MappableDirection.Up:
        connection.StartText = Connection.Up;
        break;
      case MappableDirection.Down:
        connection.StartText = Connection.Down;
        break;
    }
  }

  public void Connect(
    Room source,
    MappableDirection directionFromSource,
    Room target,
    bool assumeTwoWayConnections)
  {
    if (!CurrentProject.Elements.Contains(source) || !CurrentProject.Elements.Contains(target)) return;

    var sourceCompassPoint = CompassPointHelper.GetCompassDirection(directionFromSource);
    CompassPoint? acceptableSourceCompassPoint;
    switch (directionFromSource)
    {
      case MappableDirection.Up:
      case MappableDirection.Down:
      case MappableDirection.In:
      case MappableDirection.Out:
        acceptableSourceCompassPoint = null;
        break;
      default:
        acceptableSourceCompassPoint = sourceCompassPoint;
        break;
    }

    var connection = FindConnection(source, target, acceptableSourceCompassPoint, out var wrongWay);

    if (connection == null)
    {
      var targetCompassPoint = CompassPointHelper.GetAutomapOpposite(sourceCompassPoint);
      TryMoveRoomsForTidyConnection(source, sourceCompassPoint, target, targetCompassPoint);
      connection = AddConnection(source, sourceCompassPoint, target, targetCompassPoint);

      if (_automap.UseDottedConnection)
      {
        connection.Style = ConnectionStyle.Dashed;
        _automap.UseDottedConnection = false;
      }
      else
      {
        connection.Style = ConnectionStyle.Solid;
      }

      connection.Flow = assumeTwoWayConnections ? ConnectionFlow.TwoWay : ConnectionFlow.OneWay;
    }
    else if (wrongWay)
    {
      connection.Flow = ConnectionFlow.TwoWay;
    }

    switch (directionFromSource)
    {
      case MappableDirection.Up:
      case MappableDirection.Down:
      case MappableDirection.In:
      case MappableDirection.Out:
        if (string.IsNullOrEmpty(connection.StartText) && string.IsNullOrEmpty(connection.EndText))
          switch (directionFromSource)
          {
            case MappableDirection.Up:
              connection.SetText(wrongWay ? ConnectionLabel.Down : ConnectionLabel.Up);
              break;
            case MappableDirection.Down:
              connection.SetText(wrongWay ? ConnectionLabel.Up : ConnectionLabel.Down);
              break;
            case MappableDirection.In:
              connection.SetText(wrongWay ? ConnectionLabel.Out : ConnectionLabel.In);
              break;
            case MappableDirection.Out:
              connection.SetText(wrongWay ? ConnectionLabel.In : ConnectionLabel.Out);
              break;
          }

        break;
    }
  }

  public void ConnectByCommand(Room source, Room target, string command)
  {
    if (source == null || target == null || source == target || string.IsNullOrWhiteSpace(command)) return;
    if (!CurrentProject.Elements.Contains(source) || !CurrentProject.Elements.Contains(target)) return;
    command = command.Trim();

    foreach (var connection in CurrentProject.Elements.OfType<Connection>())
    {
      if (!string.Equals(connection.MidText, command, StringComparison.OrdinalIgnoreCase)) continue;
      var from = connection.GetSourceRoom();
      var to = connection.GetTargetRoom();
      if (from == source && to == target) return;
      if (from == target && to == source)
      {
        connection.Flow = ConnectionFlow.TwoWay;
        return;
      }
    }

    var sourcePoint = FacingPoint(source, target);
    var added = AddConnection(source, sourcePoint, target, CompassPointHelper.GetOpposite(sourcePoint));
    added.Style = ConnectionStyle.Dashed;
    added.Flow = ConnectionFlow.OneWay;
    added.MidText = command;
  }

  /// <summary>The side of <paramref name="from" /> (one of the eight main points) that faces <paramref name="to" />.</summary>
  private static CompassPoint FacingPoint(Room from, Room to)
  {
    var a = from.InnerBounds.Center;
    var b = to.InnerBounds.Center;
    var dx = b.X - a.X;
    var dy = b.Y - a.Y;
    if (dx == 0 && dy == 0) return CompassPoint.East;
    // screen y grows downwards, so flip it to get a compass angle: 0 = east, 90 = north
    var angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;
    var sector = (int)Math.Round(angle / 45.0);
    return sector switch
    {
      0 => CompassPoint.East,
      1 => CompassPoint.NorthEast,
      2 => CompassPoint.North,
      3 => CompassPoint.NorthWest,
      4 or -4 => CompassPoint.West,
      -3 => CompassPoint.SouthWest,
      -2 => CompassPoint.South,
      _ => CompassPoint.SouthEast
    };
  }

  public Room CreateRoom(Room existing, string name)
  {
    var room = new Room(CurrentProject) { Name = name };

    if (existing != null) room.Position = existing.Position;
    room.Position = Settings.Snap(room.Position);

    var tryOtherSideNext = false;
    var tryLeft = true;
    var distance = 0;
    var initialPosition = room.Position;
    while (AnyRoomsIntersect(room))
    {
      if (tryOtherSideNext)
      {
        tryLeft = !tryLeft;
        tryOtherSideNext = false;
      }
      else
      {
        tryOtherSideNext = true;
        ++distance;
      }

      var f = distance * (Settings.PreferredDistanceBetweenRooms + room.Width);
      var vectorX = tryLeft ? initialPosition.X - f : initialPosition.X + f;
      room.Position = Settings.Snap(new Vector(vectorX, room.Position.Y));
    }

    room.ArbitraryAutomappedPosition = true;

    CurrentProject.Elements.Add(room);
    return room;
  }

  public Room CreateRoom(Room existing, MappableDirection directionFromExisting, string roomName, string line)
  {
    if (!CurrentProject.Elements.Contains(existing)) return null;

    var room = new Room(CurrentProject) { Name = roomName };
    if (line != roomName) room.SubTitle = line.Replace(roomName, "");

    PositionRelativeTo(room, existing, CompassPointHelper.GetCompassDirection(directionFromExisting), out var delta);

    if (AnyRoomsIntersect(room))
    {
      ShiftMap(room.InnerBounds, delta);
    }

    CurrentProject.Elements.Add(room);

    return room;
  }

  public RoomSearchResult FindRooms(string roomName, string roomDescription, RoomMatcher matcher)
  {
    var list = new List<Room>();
    foreach (var element in CurrentProject.Elements)
      if (element is Room room)
      {
        var matches = matcher(roomName, roomDescription, room);
        if (matches.HasValue && matches.Value) return new RoomSearchResult(room, list);
        if (!matches.HasValue) list.Add(room);
      }

    return new RoomSearchResult(null, list);
  }

  public void RemoveExitStub(Room room, MappableDirection direction)
  {
    if (!CurrentProject.Elements.Contains(room)) return;

    var compassPoint = CompassPointHelper.GetCompassDirection(direction);
    foreach (var connection in room.GetConnections(compassPoint))
    {
      var source = connection.GetSourceRoom(out var sourceCompassPoint);
      var target = connection.GetTargetRoom(out var targetCompassPoint);
      if (source == room && target == room && sourceCompassPoint == compassPoint && targetCompassPoint == compassPoint)
        CurrentProject.Elements.Remove(connection);
    }
  }

  public void RemoveRoom(Room otherRoom)
  {
    CurrentProject.Elements.Remove(otherRoom);
  }

  public void SelectRoom(Room room)
  {
    if (!CurrentProject.Elements.Contains(room)) return;
    CurrentProject.ActiveSelectedElement = room;
    if (_session == null) return;

    _session.Selection.Set(new List<Element> { room });
    _session.EnsureVisible(room);
  }

  public IDisposable BeginBatch(string title)
  {
    if (_session == null) return NoBatch.Instance;
    return _session.BeginTransaction(title);
  }

  public Room ResolveRoom(Room room)
  {
    if (room == null) return null;
    return CurrentProject.Elements.OfType<Room>().FirstOrDefault(candidate => candidate.Id == room.Id);
  }

  private sealed class NoBatch : IDisposable
  {
    public static readonly NoBatch Instance = new();

    public void Dispose()
    {
    }
  }

  private Connection AddConnection(
    Room source,
    CompassPoint sourceCompassPoint,
    Room target,
    CompassPoint targetCompassPoint)
  {
    var a = new Vertex(source.PortAt(sourceCompassPoint));
    var b = new Vertex(target.PortAt(targetCompassPoint));
    var connection = new Connection(CurrentProject, a, b);
    CurrentProject.Elements.Add(connection);
    return connection;
  }

  private bool AnyRoomsIntersect(Room room)
  {
    var bounds = room.InnerBounds;
    foreach (var element in CurrentProject.Elements)
    {
      if (!(element is Room) || element == room || !element.Intersects(bounds)) continue;
      return true;
    }

    return false;
  }

  private static bool ApproximateDirectionMatch(CompassPoint one, CompassPoint two)
  {
    return CompassPointHelper.GetAutomapDirectionVector(one) == CompassPointHelper.GetAutomapDirectionVector(two);
  }

  private Connection FindConnection(
    Room source,
    Room target,
    CompassPoint? directionFromSource,
    out bool wrongWay)
  {
    foreach (var element in CurrentProject.Elements)
      if (element is Connection connection)
      {
        var fromRoom = connection.GetSourceRoom(out var fromDirection);
        var toRoom = connection.GetTargetRoom(out var toDirection);
        if (fromRoom == source && toRoom == target && (directionFromSource == null ||
                                                       ApproximateDirectionMatch(
                                                         directionFromSource.Value,
                                                         fromDirection)))
        {
          wrongWay = false;
          return connection;
        }

        if (fromRoom == target && toRoom == source && (directionFromSource == null ||
                                                       ApproximateDirectionMatch(
                                                         directionFromSource.Value,
                                                         toDirection)))
        {
          wrongWay = connection.Flow == ConnectionFlow.OneWay;
          return connection;
        }

        // Already connected, but the player went another way: move this connection's end on the source room to
        // the port for that direction. (The reference renamed the room's shared port instead, which left the room
        // with two ports for one direction and none for another, and crashed the next connection that needed it.)
        if (fromRoom == target && toRoom == source)
        {
          if (directionFromSource != null) connection.VertexList[1].Port = source.PortAt(directionFromSource.Value);
          wrongWay = connection.Flow == ConnectionFlow.OneWay;
          return connection;
        }

        if (fromRoom == source && toRoom == target)
        {
          if (directionFromSource != null) connection.VertexList[0].Port = source.PortAt(directionFromSource.Value);
          wrongWay = false;
          return connection;
        }
      }

    wrongWay = false;
    return null;
  }

  private static void PositionRelativeTo(Room room, Room existing, CompassPoint existingCompassPoint, out Vector delta)
  {
    delta = CompassPointHelper.GetAutomapDirectionVector(existingCompassPoint);
    delta.X *= Settings.PreferredDistanceBetweenRooms + room.Width;
    delta.Y *= Settings.PreferredDistanceBetweenRooms + room.Height;

    var newRoomCenter = existing.InnerBounds.Center + delta;
    room.Position = Settings.Snap(new Vector(newRoomCenter.X - room.Width / 2, newRoomCenter.Y - room.Height / 2));
  }

  private void ShiftMap(Rect deltaOrigin, Vector delta)
  {
    foreach (var element in CurrentProject.Elements)
      if (element is Room room)
      {
        var bounds = room.InnerBounds;
        if (delta.X < 0)
        {
          if (bounds.Center.X < deltaOrigin.Right)
            room.Position = new Vector(room.Position.X + delta.X, room.Position.Y);
        }
        else if (delta.X > 0)
        {
          if (bounds.Center.X > deltaOrigin.Left)
            room.Position = new Vector(room.Position.X + delta.X, room.Position.Y);
        }
      }

    foreach (var element in CurrentProject.Elements)
      if (element is Room room)
      {
        var bounds = room.InnerBounds;
        if (delta.Y < 0)
        {
          if (bounds.Center.Y < deltaOrigin.Bottom)
            room.Position = new Vector(room.Position.X, room.Position.Y + delta.Y);
        }
        else if (bounds.Center.Y > deltaOrigin.Top)
        {
          if (bounds.Bottom > deltaOrigin.Y) room.Position = new Vector(room.Position.X, room.Position.Y + delta.Y);
        }
      }
  }

  private bool TryMoveRoomForTidyConnection(Room source, CompassPoint targetCompassPoint, Room target)
  {
    var sourceArbitrary = source.ArbitraryAutomappedPosition;
    var sourcePosition = source.Position;

    PositionRelativeTo(source, target, targetCompassPoint, out _);
    if (AnyRoomsIntersect(source))
    {
      source.Position = sourcePosition;
      source.ArbitraryAutomappedPosition = sourceArbitrary;
      return false;
    }

    return true;
  }

  private void TryMoveRoomsForTidyConnection(
    Room source,
    CompassPoint sourceCompassPoint,
    Room target,
    CompassPoint targetCompassPoint)
  {
    if (source.ArbitraryAutomappedPosition && !source.IsConnected)
      if (TryMoveRoomForTidyConnection(source, targetCompassPoint, target))
        return;
    if (target.ArbitraryAutomappedPosition && !target.IsConnected)
      TryMoveRoomForTidyConnection(target, sourceCompassPoint, source);
  }
}
