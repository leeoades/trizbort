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
    CompassPoint? acceptableSourceCompassPoint = IsCompass(directionFromSource) ? sourceCompassPoint : null;

    var connection = FindConnection(source, target, acceptableSourceCompassPoint, out var wrongWay);
    bool sourceIsStart;
    bool sourceEndIsGuess;

    if (connection == null)
    {
      var targetCompassPoint = CompassPointHelper.GetAutomapOpposite(sourceCompassPoint);
      TryMoveRoomsForTidyConnection(source, sourceCompassPoint, target, targetCompassPoint);
      // a one-way join's far end is only a guess until the way back is found, so it meets the far room on the side
      // facing the room it came from rather than looping round to the opposite side
      if (!assumeTwoWayConnections) targetCompassPoint = FacingPoint(target, source);
      connection = AddConnection(source, sourceCompassPoint, target, targetCompassPoint);

      if (_automap != null && _automap.UseDottedConnection)
      {
        connection.Style = ConnectionStyle.Dashed;
        _automap.UseDottedConnection = false;
      }
      else
      {
        connection.Style = ConnectionStyle.Solid;
      }

      connection.Flow = assumeTwoWayConnections ? ConnectionFlow.TwoWay : ConnectionFlow.OneWay;
      sourceIsStart = true;
      sourceEndIsGuess = false;
    }
    else
    {
      sourceIsStart = connection.GetSourceRoom() == source;
      // a guessed end is replaced when the player goes another way, and simply confirmed when they go that way
      sourceEndIsGuess = IsGuessedEnd(connection, sourceIsStart) &&
                         !ListsDirection(EndWords(connection, sourceIsStart), directionFromSource);
      if (wrongWay) connection.Flow = ConnectionFlow.TwoWay;
    }

    MarkTravelled(connection, sourceIsStart, directionFromSource);

    RecordDirection(connection, source, sourceIsStart, directionFromSource, sourceEndIsGuess, target,
      assumeTwoWayConnections);

    TakeOverCommandJoins(connection, source, target);
  }

  /// <summary>
  ///   A direction leading from <paramref name="source" /> to <paramref name="target" /> comes first: commands that
  ///   lead the same way ("light lamp") join its middle label, instead of having dashed joins of their own.
  /// </summary>
  private void TakeOverCommandJoins(Connection connection, Room source, Room target)
  {
    if (IsCommandJoin(connection))
    {
      // the command join turned out to be a direction too: its commands stay as its label
      connection.AutomapCommand = string.Empty;
      connection.Style = ConnectionStyle.Solid;
    }

    foreach (var other in CurrentProject.Elements.OfType<Connection>().ToList())
    {
      if (other == connection || !IsCommandJoin(other)) continue;
      if (other.GetSourceRoom() != source || other.GetTargetRoom() != target) continue;
      foreach (var command in CommandsOf(other)) connection.MidText = WithMethod(connection.MidText, command);
      CurrentProject.Elements.Remove(other);
    }
  }

  /// <summary>The commands a join records: a command join's commands, or the ones listed in a join's middle label.</summary>
  private static List<string> CommandsOf(Connection connection) =>
    SplitMethods(IsCommandJoin(connection) ? connection.AutomapCommand : connection.MidText);

  private static List<string> SplitMethods(string text) =>
    (text ?? string.Empty).Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

  /// <summary>A " / " list of the ways along a join, with <paramref name="method" /> added if it isn't there yet.</summary>
  private static string WithMethod(string text, string method)
  {
    var methods = SplitMethods(text);
    if (methods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase))) return text;
    methods.Add(method);
    return string.Join(" / ", methods);
  }

  /// <summary>Whether <paramref name="connection" /> is a way from <paramref name="from" /> to <paramref name="to" />.</summary>
  private static bool Leads(Connection connection, Room from, Room to)
  {
    var start = connection.GetSourceRoom();
    var end = connection.GetTargetRoom();
    return (start == from && end == to) || (connection.Flow == ConnectionFlow.TwoWay && start == to && end == from);
  }

  /// <summary>
  ///   Note that <paramref name="direction" /> leads from <paramref name="here" /> along <paramref name="connection" />.
  ///   Each end of a join lists every direction that leaves along it ("north / down"); the join leaves the room on the
  ///   side of the first compass direction, which beats up, down, in and out, and the far room moves to that side
  ///   when nothing else holds it in place. A guessed exit on a one-way join is replaced rather than added to.
  /// </summary>
  private void RecordDirection(
    Connection connection,
    Room here,
    bool hereIsStart,
    MappableDirection direction,
    bool hereIsGuess,
    Room far,
    bool assumeTwoWayConnections)
  {
    List<string> hereWords;
    if (hereIsGuess)
    {
      // the guessed directions go; anything the user typed there stays
      DockEnd(connection, hereIsStart, here, CompassPointHelper.GetCompassDirection(direction));
      hereWords = EndWords(connection, hereIsStart).Where(word => !DirectionWords.TryParse(word, out _)).ToList();
    }
    else
    {
      hereWords = EndWords(connection, hereIsStart);
    }

    var thereWords = EndWords(connection, !hereIsStart);
    var added = AddWord(hereWords, DirectionWord(direction));
    // assuming two-way joins, the way back is the opposite direction
    if (added && !hereIsGuess && assumeTwoWayConnections && connection.Flow == ConnectionFlow.TwoWay)
      AddWord(thereWords, DirectionWord(CompassPointHelper.GetOpposite(direction)));

    var hereCompass = FirstCompass(hereWords);
    if (hereCompass != null && !IsMainPoint(EndPoint(connection, hereIsStart)))
    {
      var point = CompassPointHelper.GetCompassDirection(hereCompass.Value);
      // move the join to the compass side only if the rooms are (or can be put) that way round, so it doesn't loop
      // back on itself
      var inPlace = ApproximateDirectionMatch(FacingPoint(here, far), point) ||
                    TryMoveRoomNextTo(far, here, point) ||
                    TryMoveRoomNextTo(here, far, CompassPointHelper.GetOpposite(point));
      if (inPlace)
      {
        DockEnd(connection, hereIsStart, here, point);
        if (!IsMainPoint(EndPoint(connection, !hereIsStart)))
        {
          var thereCompass = FirstCompass(thereWords);
          DockEnd(connection, !hereIsStart, far,
            thereCompass != null
              ? CompassPointHelper.GetCompassDirection(thereCompass.Value)
              : CompassPointHelper.GetOpposite(point));
        }
      }
    }

    SetEndText(connection, hereIsStart, hereWords);
    SetEndText(connection, !hereIsStart, thereWords);
  }

  /// <summary>
  ///   Record that the player set off from one end of a join in <paramref name="direction" />. Kept on the connection
  ///   (<see cref="Connection.StartTravelled" />), so it is saved with the map and undone with the move that made it.
  /// </summary>
  private static void MarkTravelled(Connection connection, bool atStart, MappableDirection direction)
  {
    var words = TravelledWords(connection, atStart);
    var word = DirectionWord(direction);
    if (words.Contains(word)) return;
    words.Add(word);
    var text = string.Join(" ", words);
    if (atStart) connection.StartTravelled = text;
    else connection.EndTravelled = text;
  }

  private static List<string> TravelledWords(Connection connection, bool atStart)
  {
    var text = atStart ? connection.StartTravelled : connection.EndTravelled;
    return string.IsNullOrWhiteSpace(text)
      ? new List<string>()
      : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
  }

  private static bool WasTravelled(Connection connection, bool atStart) => TravelledWords(connection, atStart).Count > 0;

  /// <summary>
  ///   Whether the exit drawn at one end of an existing join is only a guess: nobody has set off from that end, and it
  ///   is the far end of a one-way join or the assumed way back of a two-way join automap made. Travelling from a
  ///   guessed end another way replaces the guess; travelling from a known end adds a direction. Labels don't count:
  ///   automap writes the assumed way back ("down") itself.
  /// </summary>
  private static bool IsGuessedEnd(Connection connection, bool atStart)
  {
    if (WasTravelled(connection, atStart)) return false;
    return connection.Flow == ConnectionFlow.OneWay ? !atStart : WasTravelled(connection, !atStart);
  }

  private static bool IsCompass(MappableDirection direction)
  {
    return direction is not (MappableDirection.Up or MappableDirection.Down or MappableDirection.In
      or MappableDirection.Out or MappableDirection.None);
  }

  private static readonly CompassPoint[] _mainPoints =
  {
    CompassPoint.North, CompassPoint.NorthEast, CompassPoint.East, CompassPoint.SouthEast, CompassPoint.South,
    CompassPoint.SouthWest, CompassPoint.West, CompassPoint.NorthWest
  };

  private static bool IsMainPoint(CompassPoint point) => _mainPoints.Contains(point);

  private static MappableDirection DirectionOfMainPoint(CompassPoint point) => point switch
  {
    CompassPoint.North => MappableDirection.North,
    CompassPoint.NorthEast => MappableDirection.NorthEast,
    CompassPoint.East => MappableDirection.East,
    CompassPoint.SouthEast => MappableDirection.SouthEast,
    CompassPoint.South => MappableDirection.South,
    CompassPoint.SouthWest => MappableDirection.SouthWest,
    CompassPoint.West => MappableDirection.West,
    _ => MappableDirection.NorthWest
  };

  private static string DirectionWord(MappableDirection direction) => direction.ToString().ToLowerInvariant();

  private static bool IsCompassWord(string word) =>
    DirectionWords.TryParse(word, out var direction) && IsCompass(direction);

  private static MappableDirection? FirstCompass(IEnumerable<string> words)
  {
    foreach (var word in words)
      if (DirectionWords.TryParse(word, out var direction) && IsCompass(direction))
        return direction;
    return null;
  }

  private static bool ListsDirection(IEnumerable<string> words, MappableDirection direction) =>
    words.Any(word => DirectionWords.TryParse(word, out var listed) && listed == direction);

  private static bool AddWord(List<string> words, string word)
  {
    if (DirectionWords.TryParse(word, out var direction) &&
        words.Any(w => DirectionWords.TryParse(w, out var existing) && existing == direction))
      return false;
    if (words.Contains(word, StringComparer.OrdinalIgnoreCase)) return false;
    words.Add(word);
    return true;
  }

  /// <summary>
  ///   A join made by <see cref="ConnectByCommand" /> ("xyzzy"). Marked by <see cref="Connection.AutomapCommand" />,
  ///   not by its look: the user can give any connection a dotted line and a middle label.
  /// </summary>
  internal static bool IsCommandJoin(Connection connection) => !string.IsNullOrEmpty(connection.AutomapCommand);

  private static CompassPoint EndPoint(Connection connection, bool atStart)
  {
    CompassPoint point;
    if (atStart) connection.GetSourceRoom(out point);
    else connection.GetTargetRoom(out point);
    return point;
  }

  private static void DockEnd(Connection connection, bool atStart, Room room, CompassPoint point)
  {
    var vertex = atStart ? connection.VertexList[0] : connection.VertexList[connection.VertexList.Count - 1];
    vertex.Port = room.PortAt(point);
  }

  /// <summary>
  ///   The directions written at one end of a join, plus the compass direction of the side it leaves the room from
  ///   when no compass direction is written. Words that aren't directions (a label the user typed) are kept.
  /// </summary>
  private static List<string> EndWords(Connection connection, bool atStart)
  {
    var text = atStart ? connection.StartText : connection.EndText;
    var words = string.IsNullOrWhiteSpace(text)
      ? new List<string>()
      : text.Split('/').Select(word => word.Trim()).Where(word => word.Length > 0).ToList();
    var point = EndPoint(connection, atStart);
    if (IsMainPoint(point) && !words.Any(IsCompassWord)) words.Insert(0, DirectionWord(DirectionOfMainPoint(point)));
    return words;
  }

  /// <summary>
  ///   Write the directions at one end: none when the end's side already says it (a single compass direction),
  ///   otherwise every direction, compass directions first.
  /// </summary>
  private static void SetEndText(Connection connection, bool atStart, List<string> words)
  {
    var ordered = words.Where(IsCompassWord).Concat(words.Where(word => !IsCompassWord(word))).ToList();
    var point = EndPoint(connection, atStart);
    var sideSaysIt = ordered.Count == 1 && IsMainPoint(point) &&
                     DirectionWords.TryParse(ordered[0], out var only) && only == DirectionOfMainPoint(point);
    var text = sideSaysIt ? string.Empty : string.Join(" / ", ordered);

    if (atStart)
    {
      if (connection.StartText != text) connection.StartText = text;
    }
    else if (connection.EndText != text)
    {
      connection.EndText = text;
    }
  }

  /// <summary>
  ///   Move <paramref name="room" /> to sit <paramref name="fromAnchor" /> of <paramref name="anchor" />, if it is joined
  ///   to nothing else and the spot is clear of rooms and joins.
  /// </summary>
  private bool TryMoveRoomNextTo(Room room, Room anchor, CompassPoint fromAnchor)
  {
    var joinedElsewhere = CurrentProject.Elements.OfType<Connection>().Any(connection =>
    {
      var a = connection.GetSourceRoom();
      var b = connection.GetTargetRoom();
      if (a == b) return false;
      return (a == room && b != anchor) || (b == room && a != anchor);
    });
    if (joinedElsewhere) return false;

    var position = room.Position;
    PositionRelativeTo(room, anchor, fromAnchor, out _);
    if (AnyRoomsIntersect(room) || AnyJoinsIntersect(room.InnerBounds, room) || JoinWouldCrossRooms(anchor, room))
    {
      room.Position = position;
      return false;
    }

    return true;
  }

  /// <summary>Whether any join, other than exit stubs and joins to <paramref name="ignore" />, crosses <paramref name="bounds" />.</summary>
  private bool AnyJoinsIntersect(Rect bounds, Room ignore)
  {
    foreach (var connection in CurrentProject.Elements.OfType<Connection>())
    {
      var a = connection.GetSourceRoom();
      var b = connection.GetTargetRoom();
      if (a == b) continue;
      if (ignore != null && (a == ignore || b == ignore)) continue;
      if (connection.Intersects(bounds)) return true;
    }

    return false;
  }

  /// <summary>Whether a straight join between the two rooms would pass through another room.</summary>
  private bool JoinWouldCrossRooms(Room one, Room two)
  {
    var segment = new LineSegment(one.InnerBounds.Center, two.InnerBounds.Center);
    foreach (var other in CurrentProject.Elements.OfType<Room>())
    {
      if (other == one || other == two) continue;
      if (segment.IntersectsWith(other.InnerBounds)) return true;
    }

    return false;
  }

  /// <summary>
  ///   Put <paramref name="room" /> in the nearest clear spot <paramref name="point" /> of <paramref name="existing" />:
  ///   first the usual spot, then spots up to 45 degrees either side and further out. Clear means no room or join is
  ///   there and the join to it won't pass through another room.
  /// </summary>
  private bool TryPlaceInClearSpace(Room room, Room existing, CompassPoint point)
  {
    var direction = CompassPointHelper.GetAutomapDirectionVector(point);
    var wanted = Math.Atan2(direction.Y, direction.X);
    var stepX = Settings.PreferredDistanceBetweenRooms + room.Width;
    var stepY = Settings.PreferredDistanceBetweenRooms + room.Height;
    var centre = existing.InnerBounds.Center;

    var candidates = new List<(int X, int Y, int Ring, double Off)>();
    for (var ring = 1; ring <= 3; ring++)
      for (var x = -ring; x <= ring; x++)
        for (var y = -ring; y <= ring; y++)
        {
          if (Math.Max(Math.Abs(x), Math.Abs(y)) != ring) continue;
          var off = Math.Abs(Math.Atan2(y, x) - wanted);
          if (off > Math.PI) off = 2 * Math.PI - off;
          if (off <= Math.PI / 4 + 0.0001) candidates.Add((x, y, ring, off));
        }

    foreach (var candidate in candidates.OrderBy(c => c.Ring).ThenBy(c => c.Off))
    {
      var spot = new Vector(centre.X + candidate.X * stepX, centre.Y + candidate.Y * stepY);
      room.Position = Settings.Snap(new Vector(spot.X - room.Width / 2, spot.Y - room.Height / 2));
      if (!AnyRoomsIntersect(room) && !AnyJoinsIntersect(room.InnerBounds, null) &&
          !JoinWouldCrossRooms(existing, room))
        return true;
    }

    return false;
  }

  public void ConnectByCommand(Room source, Room target, string command)
  {
    if (source == null || target == null || source == target || string.IsNullOrWhiteSpace(command)) return;
    if (!CurrentProject.Elements.Contains(source) || !CurrentProject.Elements.Contains(target)) return;
    command = command.Trim();

    var between = CurrentProject.Elements.OfType<Connection>().Where(connection =>
    {
      var from = connection.GetSourceRoom();
      var to = connection.GetTargetRoom();
      return (from == source && to == target) || (from == target && to == source);
    }).ToList();

    // a command already known along a join between these rooms
    foreach (var connection in between)
    {
      if (!CommandsOf(connection).Any(c => string.Equals(c, command, StringComparison.OrdinalIgnoreCase))) continue;
      if (IsCommandJoin(connection) && !Leads(connection, source, target)) connection.Flow = ConnectionFlow.TwoWay;
      return;
    }

    // a direction that leads there comes first; the command joins its label
    var way = between.FirstOrDefault(connection => !IsCommandJoin(connection) && Leads(connection, source, target));
    if (way != null)
    {
      way.MidText = WithMethod(way.MidText, command);
      return;
    }

    // another command that leads there ("turn on lamp" as well as "light lamp"): one join lists both
    var other = between.FirstOrDefault(connection => IsCommandJoin(connection) && Leads(connection, source, target));
    if (other != null)
    {
      other.AutomapCommand = WithMethod(other.AutomapCommand, command);
      other.MidText = WithMethod(other.MidText, command);
      return;
    }

    var sourcePoint = FacingPoint(source, target);
    var added = AddConnection(source, sourcePoint, target, CompassPointHelper.GetOpposite(sourcePoint));
    added.Style = ConnectionStyle.Dashed;
    added.Flow = ConnectionFlow.OneWay;
    added.MidText = command;
    added.AutomapCommand = command;
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

  public bool MakeSpaceForNewRooms { get; set; }

  private string _tidiedMap;

  /// <summary>
  ///   Tidies the rooms around <paramref name="room" /> when the map has changed since the last tidy (a new room, a
  ///   new join, or a join that learned a direction). Walking around rooms automap already knows leaves the map, and
  ///   anything the user moved by hand, alone, unless a room nearby sits on another room or on a join.
  /// </summary>
  public void TidyAround(Room room)
  {
    room = ResolveRoom(room);
    if (room == null) return;
    var unchanged = MapShape() == _tidiedMap;
    var moved = LayoutTidy.TidyAround(CurrentProject.Elements.ToList(), room, onlyIfCluttered: unchanged);
    if (moved > 0)
    {
      FaceGuessedEnds();
      _session?.EnsureVisible(room);
    }

    _tidiedMap = MapShape();
  }

  /// <summary>What the map is made of, ignoring where the rooms are: its rooms, its joins and their directions.</summary>
  private string MapShape()
  {
    var text = new System.Text.StringBuilder();
    foreach (var element in CurrentProject.Elements)
      switch (element)
      {
        case Room r:
          text.Append('R').Append(r.Id).Append(';');
          break;
        case Connection c:
          text.Append('C').Append(c.Id).Append(':').Append(c.GetSourceRoom()?.Id).Append(EndPoint(c, true))
            .Append('>').Append(c.GetTargetRoom()?.Id).Append(EndPoint(c, false)).Append(c.Flow)
            .Append(c.StartText).Append('|').Append(c.EndText).Append(';');
          break;
      }

    return text.ToString();
  }

  /// <summary>
  ///   After rooms move, the far end of a one-way join nobody has come back along turns to face the room it came
  ///   from, until the way back is known.
  /// </summary>
  private void FaceGuessedEnds()
  {
    foreach (var connection in CurrentProject.Elements.OfType<Connection>().ToList())
    {
      if (connection.Flow != ConnectionFlow.OneWay || IsCommandJoin(connection)) continue;
      if (WasTravelled(connection, false) || EndWords(connection, false).Count(IsCompassWord) > 1) continue;
      if (!string.IsNullOrWhiteSpace(connection.EndText)) continue;
      var source = connection.GetSourceRoom();
      var target = connection.GetTargetRoom();
      if (source == null || target == null || source == target) continue;
      var facing = FacingPoint(target, source);
      if (EndPoint(connection, false) != facing) DockEnd(connection, false, target, facing);
    }
  }

  public Room CreateRoom(Room existing, string name)
  {
    var room = new Room(CurrentProject) { Name = name };

    if (existing != null) room.Position = existing.Position;
    else if (_session != null)
      // the first room goes in the middle of the view, so the map has space to grow every way
      room.Position = new Vector(
        _session.Viewport.VisibleWorldBounds.Center.X - room.Width / 2,
        _session.Viewport.VisibleWorldBounds.Center.Y - room.Height / 2);
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

    var point = CompassPointHelper.GetCompassDirection(directionFromExisting);
    if (MakeSpaceForNewRooms)
    {
      // next to the room it was reached from, even if that spot is taken: the tidy that follows moves the other
      // rooms aside, which reads better than putting the new room further away at the end of a long join
      PositionRelativeTo(room, existing, point, out _);
    }
    else if (!TryPlaceInClearSpace(room, existing, point))
    {
      // nowhere clear nearby: make room by moving the map apart, as Trizbort did
      PositionRelativeTo(room, existing, point, out var delta);
      if (AnyRoomsIntersect(room)) ShiftMap(room.InnerBounds, delta);
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
    // the user is part-way through a change of their own (a drag): joining it would put automap's work in the
    // user's undo step, and cancelling the drag would throw it away
    if (_session.Undo.IsRecording) return null;
    return _session.BeginTransaction(title);
  }

  public Room ResolveRoom(Room room)
  {
    if (room == null) return null;
    return CurrentProject.Elements.OfType<Room>().FirstOrDefault(element => element.StableId == room.StableId);
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
      if (element is Connection connection && !IsCommandJoin(connection))
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

        // Already connected, but the player went another way.
        // (Connect records the new direction: it adds to the join's directions, or replaces a guessed exit.)
        if (fromRoom == target && toRoom == source)
        {
          wrongWay = connection.Flow == ConnectionFlow.OneWay;
          return connection;
        }

        if (fromRoom == source && toRoom == target)
        {
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
