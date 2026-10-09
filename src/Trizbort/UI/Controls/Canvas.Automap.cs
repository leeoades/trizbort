using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.UI.Controls;

public partial class Canvas {
  private readonly Automap.Automap _automap = Automap.Automap.Instance;
  private readonly MultithreadedAutomapCanvas _threadSafeAutomapCanvas;
  private bool _dontAskAboutAmbiguities;
  public string AutomappingStatus => _automap.Status;


  public bool IsAutomapping => _automap.Running;

  void IAutomapCanvas.AddExitStub(Room room, MappableDirection direction)
  {
    if (!Project.Current.Elements.Contains(room)) return;

    var sourceCompassPoint = CompassPointHelper.GetCompassDirection(direction);
    var connection = AddConnection(room, sourceCompassPoint, room, sourceCompassPoint);
    switch (direction) {
      case MappableDirection.Up:
        connection.StartText = Connection.Up;
        break;
      case MappableDirection.Down:
        connection.StartText = Connection.Down;
        break;
    }
  }

  void IAutomapCanvas.Connect(
    Room source,
    MappableDirection directionFromSource,
    Room target,
    bool assumeTwoWayConnections)
  {
    if (!Project.Current.Elements.Contains(source) || !Project.Current.Elements.Contains(target)) return;

    // work out the correct compass point to use for the given direction

    // look for existing connections:
    // if the given direction is up/down/in/out, any existing connection will suffice;
    // otherwise, only match an existing connection if it's pretty close to the one we want.
    var sourceCompassPoint = CompassPointHelper.GetCompassDirection(directionFromSource);
    CompassPoint? acceptableSourceCompassPoint;
    switch (directionFromSource) {
      case MappableDirection.Up:
      case MappableDirection.Down:
      case MappableDirection.In:
      case MappableDirection.Out:
        acceptableSourceCompassPoint = null; // any existing connection will do
        break;
      default:
        acceptableSourceCompassPoint = sourceCompassPoint;
        break;
    }

    var connection = FindConnection(source, target, acceptableSourceCompassPoint, out var wrongWay);

    if (connection == null) {
      // there is no suitable connection between these rooms:
      var targetCompassPoint = CompassPointHelper.GetAutomapOpposite(sourceCompassPoint);

      // check whether we can move one of the rooms to make the connection tidier;
      // we won't need this very often, but it can be useful especially if the user teleports into a room
      // and then steps out into an existing one (this can appear to happen if the user moves into a 
      // dark room, turns on the light, then leaves).
      TryMoveRoomsForTidyConnection(source, sourceCompassPoint, target, targetCompassPoint);

      // add a new connection
      connection = AddConnection(source, sourceCompassPoint, target, targetCompassPoint);

      if (_automap.UseDottedConnection) {
        connection.Style = ConnectionStyle.Dashed;
        _automap.UseDottedConnection = false;
      }
      else {
        connection.Style = ConnectionStyle.Solid;
      }

      connection.Flow = assumeTwoWayConnections ? ConnectionFlow.TwoWay : ConnectionFlow.OneWay;
    }
    else if (wrongWay) {
      // there is a suitable connection between these rooms, but it goes the wrong way;
      // make it bidirectional since we can now go both ways.
      connection.Flow = ConnectionFlow.TwoWay;
    }

    // if this is an up/down/in/out connection, mark it as such;
    // but don't override any existing text.
    switch (directionFromSource) {
      case MappableDirection.Up:
      case MappableDirection.Down:
      case MappableDirection.In:
      case MappableDirection.Out:
        if (string.IsNullOrEmpty(connection.StartText) && string.IsNullOrEmpty(connection.EndText))
          switch (directionFromSource) {
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

  Room IAutomapCanvas.CreateRoom(Room existing, string name)
  {
    // start by placing the room at the origin
    var room = new Room(Project.Current) { Name = name };

    if (existing != null) room.Position = existing.Position;
    room.Position = Settings.Snap(room.Position);

    // while we can't place the room, try to the left, then the right,
    // expanding our distance as we go, until we find a blank space.
    var tryOtherSideNext = false;
    var tryLeft = true;
    var distance = 0;
    var initialPosition = room.Position;
    while (AnyRoomsIntersect(room)) {
      if (tryOtherSideNext) {
        tryLeft = !tryLeft;
        tryOtherSideNext = false;
      }
      else {
        tryOtherSideNext = true;
        ++distance;
      }

      var f = distance * (Settings.PreferredDistanceBetweenRooms + room.Width);
      var vectorX = tryLeft ? initialPosition.X - f : initialPosition.X + f;
      room.Position = Settings.Snap(new Vector(vectorX, room.Position.Y));

      Debug.WriteLine($"Try again, more to the {(tryLeft ? "left" : "right")}.");
    }

    // after we set the position, set this flag,
    // since setting the position clears this flag if set.
    room.ArbitraryAutomappedPosition = true;

    Project.Current.Elements.Add(room);
    return room;
  }

  Room IAutomapCanvas.CreateRoom(Room existing, MappableDirection directionFromExisting, string roomName, string line)
  {
    if (!Project.Current.Elements.Contains(existing)) return null;

    var room = new Room(Project.Current) { Name = roomName };
    if (line != roomName) room.SubTitle = line.Replace(roomName, "");

    PositionRelativeTo(room, existing, CompassPointHelper.GetCompassDirection(directionFromExisting), out var delta);

    if (AnyRoomsIntersect(room)) {
      ShiftMap(room.InnerBounds, delta);
      Debug.WriteLine("Shift map.");
    }

    Project.Current.Elements.Add(room);

    return room;
  }

  Room IAutomapCanvas.FindRoom(string roomName, string roomDescription, string line, RoomMatcher matcher)
  {
    var list = new List<Room>();
    foreach (var element in Project.Current.Elements)
      if (element is Room room1) {
        var room = room1;
        var matches = matcher(roomName, roomDescription, room);
        if (matches.HasValue && matches.Value) return room;
        if (!matches.HasValue) list.Add(room);
      }

    if (list.Count == 0) return null;
    if (_dontAskAboutAmbiguities) return list[0];
    if ((string.IsNullOrEmpty(roomDescription) || !list[0].HasDescription) && list.Count == 1) return list[0];

    using var dialog = new DisambiguateRoomsDialog();
    dialog.SetTranscriptContext(roomName, roomDescription, line);
    dialog.AddAmbiguousRooms(list);
    UserInteraction.ShowDialog(dialog);
    if (dialog.UserDoesntCareAnyMore) {
      // The user has given up on this process! Can't say I blame them.
      // Use the first ambiguous room on the list, as above.
      _dontAskAboutAmbiguities = true;
      return list[0];
    }

    // Either the user picked a room, or they said "New Room" in which case we don't match, returning null.
    return dialog.Disambiguation;
  }

  void IAutomapCanvas.RemoveExitStub(Room room, MappableDirection direction)
  {
    if (!Project.Current.Elements.Contains(room)) return;

    var compassPoint = CompassPointHelper.GetCompassDirection(direction);
    foreach (var connection in room.GetConnections(compassPoint)) {
      var source = connection.GetSourceRoom(out var sourceCompassPoint);
      var target = connection.GetTargetRoom(out var targetCompassPoint);
      if (source == room && target == room && sourceCompassPoint == compassPoint && targetCompassPoint == compassPoint)
        Project.Current.Elements.Remove(connection);
    }
  }

  void IAutomapCanvas.SelectRoom(Room room)
  {
    if (!Project.Current.Elements.Contains(room)) return;

    SelectedElement = room;
    _commandController.MakeVisible(room);
  }

  public Task StartAutomapping(AutomapSettings settings, bool justParseFile = false)
  {
    StopAutomapping();

    var task = justParseFile
      ? _automap.StartCl(_threadSafeAutomapCanvas, settings)
      : _automap.Start(_threadSafeAutomapCanvas, settings);

    _dontAskAboutAmbiguities = false;

    return task;
  }

  public void StopAutomapping()
  {
    _automap.Stop();
  }

  private static bool AnyRoomsIntersect(Room room)
  {
    var bounds = room.InnerBounds;
    foreach (var element in Project.Current.Elements) {
      if (!(element is Room) || element == room || !element.Intersects(bounds)) continue;
      Debug.WriteLine($"{(element as Room).Name} is blocking {room.Name}.");
      return true;
    }

    return false;
  }

  /// <summary>
  ///   Approximately match two directions, allowing for aesthetic rearrangement by the user.
  /// </summary>
  /// <remarks>
  ///   Two compass points match if they are on the same side of a box representing the room.
  /// </remarks>
  private static bool ApproximateDirectionMatch(CompassPoint one, CompassPoint two)
  {
    return CompassPointHelper.GetAutomapDirectionVector(one) == CompassPointHelper.GetAutomapDirectionVector(two);
  }

  private static Connection FindConnection(
    Room source,
    Room target,
    CompassPoint? directionFromSource,
    out bool wrongWay)
  {
    foreach (var element in Project.Current.Elements)
      if (element is Connection connection) {
        var fromRoom = connection.GetSourceRoom(out var fromDirection);
        var toRoom = connection.GetTargetRoom(out var toDirection);
        if (fromRoom == source && toRoom == target && (directionFromSource == null ||
                                                       ApproximateDirectionMatch(
                                                         directionFromSource.Value,
                                                         fromDirection))) {
          // the two rooms are connected already in the given direction, A to B or both ways.
          wrongWay = false;
          return connection;
        }

        if (fromRoom == target && toRoom == source && (directionFromSource == null ||
                                                       ApproximateDirectionMatch(
                                                         directionFromSource.Value,
                                                         toDirection))) {
          // the two rooms are connected already in the given direction, B to A or both ways.
          wrongWay = connection.Flow == ConnectionFlow.OneWay;
          return connection;
        }

        if (fromRoom == target && toRoom == source) {
          var r1 = (Room.CompassPort)connection.VertexList[1].Port;
          if (directionFromSource != null) r1.CompassPoint = (CompassPoint)directionFromSource;
          wrongWay = connection.Flow == ConnectionFlow.OneWay;
          return connection;
        }

        if (fromRoom == source && toRoom == target) {
          var r1 = (Room.CompassPort)connection.VertexList[0].Port;
          if (directionFromSource != null) r1.CompassPoint = (CompassPoint)directionFromSource;
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
    // move all elements to the left/right of the origin left/right by the given delta
    foreach (var element in Project.Current.Elements)
      if (element is Room room) {
        var bounds = room.InnerBounds;
        if (delta.X < 0) {
          if (bounds.Center.X < deltaOrigin.Right)
            room.Position = new Vector(room.Position.X + delta.X, room.Position.Y);
        }
        else if (delta.X > 0) {
          if (bounds.Center.X > deltaOrigin.Left)
            room.Position = new Vector(room.Position.X + delta.X, room.Position.Y);
        }
      }

    // move all elements above/below the origin up/down by the given delta
    foreach (var element in Project.Current.Elements)
      if (element is Room room) {
        var bounds = room.InnerBounds;
        if (delta.Y < 0) {
          if (bounds.Center.Y < deltaOrigin.Bottom)
            room.Position = new Vector(room.Position.X, room.Position.Y + delta.Y);
        }
        else if (bounds.Center.Y > deltaOrigin.Top) {
          if (bounds.Bottom > deltaOrigin.Y) room.Position = new Vector(room.Position.X, room.Position.Y + delta.Y);
        }
      }
  }

  private static bool TryMoveRoomForTidyConnection(Room source, CompassPoint targetCompassPoint, Room target)
  {
    var sourceArbitrary = source.ArbitraryAutomappedPosition;
    var sourcePosition = source.Position;

    Vector delta;
    PositionRelativeTo(source, target, targetCompassPoint, out delta);
    if (AnyRoomsIntersect(source)) {
      // didn't work; restore previous position
      source.Position = sourcePosition;
      source.ArbitraryAutomappedPosition = sourceArbitrary;
      return false;
    }

    // that's better
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

  /// <summary>
  ///   A proxy class which implements IAutomapCanvas, marshalling calls to the real canvas on the main thread.
  /// </summary>
  private class MultithreadedAutomapCanvas : IAutomapCanvas {
    private readonly IAutomapCanvas _canvas;
    private readonly Control _control;

    public MultithreadedAutomapCanvas(Canvas canvas)
    {
      _control = canvas;
      _canvas = canvas;
    }

    public void AddExitStub(Room room, MappableDirection direction)
    {
      try {
        _control.Invoke((MethodInvoker)delegate { _canvas.AddExitStub(room, direction); });
      }
      catch (Exception) {
        // ignored
      }
    }

    public void Connect(Room source, MappableDirection directionFromSource, Room target, bool assumeTwoWayConnections)
    {
      try {
        _control.Invoke(
          (MethodInvoker)delegate { _canvas.Connect(source, directionFromSource, target, assumeTwoWayConnections); });
      }
      catch (Exception) {
        // ignored
      }
    }

    public Room CreateRoom(Room existing, string name)
    {
      Room room = null;
      try {
        _control.Invoke((MethodInvoker)delegate { room = _canvas.CreateRoom(existing, name); });
      }
      catch (Exception) {
        // ignored
      }

      return room;
    }

    public Room CreateRoom(Room existing, MappableDirection directionFromExisting, string roomName, string line)
    {
      Room room = null;
      try {
        _control.Invoke(
          (MethodInvoker)delegate { room = _canvas.CreateRoom(existing, directionFromExisting, roomName, line); });
      }
      catch (Exception) {
        // ignored
      }

      return room;
    }

    public Room FindRoom(string roomName, string roomDescription, string line, RoomMatcher matcher)
    {
      Room room = null;
      try {
        _control.Invoke((MethodInvoker)delegate { room = _canvas.FindRoom(roomName, roomDescription, line, matcher); });
      }
      catch (Exception) {
        // ignored
      }

      return room;
    }

    public void RemoveExitStub(Room room, MappableDirection direction)
    {
      try {
        _control.Invoke((MethodInvoker)delegate { _canvas.RemoveExitStub(room, direction); });
      }
      catch (Exception) {
        // ignored
      }
    }

    public void RemoveRoom(Room otherRoom)
    {
      try {
        _control.Invoke((MethodInvoker)delegate { _canvas.RemoveRoom(otherRoom); });
      }
      catch (Exception) {
        // ignored
      }
    }

    public void SelectRoom(Room room)
    {
      try {
        _control.Invoke((MethodInvoker)delegate { _canvas.SelectRoom(room); });
      }
      catch (Exception) {
        // ignored
      }
    }
  }
}