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
    // a way out already drawn, wherever it leaves the room, needs no stub
    if (ListsExit(room, direction)) return;
    // the natural way back along a one-way join that brought the player here (up from the cellar, and the kitchen
    // has a way down): assume it leads back, on the join, rather than as a stub of its own. The join stays one-way
    // until the player goes that way.
    if (TakeAsWayBack(room, direction)) return;

    var sourceCompassPoint = CompassPointHelper.GetCompassDirection(direction);
    // a join already leaves or arrives there: no stub on top of it. Arriving by going west, the room's east exit
    // is taken to be the way back until the player finds otherwise.
    foreach (var other in CurrentProject.Elements.OfType<Connection>())
    {
      // (a teleport or command join arriving there says nothing about the exit)
      if (!string.IsNullOrEmpty(other.AutomapCommand)) continue;
      if (other.GetSourceRoom(out var start) == room && start == sourceCompassPoint) return;
      if (other.GetTargetRoom(out var end) == room && end == sourceCompassPoint) return;
    }

    var connection = AddConnection(room, sourceCompassPoint, room, sourceCompassPoint);
    // an exit not yet taken: up and down are written in full, compass exits go unlabelled (the side says it)
    var text = direction switch
    {
      MappableDirection.Up => Connection.Up,
      MappableDirection.Down => Connection.Down,
      _ => null
    };
    JoinWays.Write(connection, new[] { JoinWay.Go(true, direction, false, text) }, LabelWord);
  }

  /// <summary>Whether a join (or stub) already leaves <paramref name="room" /> in <paramref name="direction" />.</summary>
  private bool ListsExit(Room room, MappableDirection direction)
  {
    foreach (var connection in CurrentProject.Elements.OfType<Connection>())
    {
      if (IsCommandJoin(connection)) continue;
      var source = connection.GetSourceRoom();
      var target = connection.GetTargetRoom();
      if (source != room && target != room) continue;
      var ways = JoinWays.Of(connection);
      if (source == room && JoinWays.Lists(ways, true, direction)) return true;
      if (target == room && JoinWays.Lists(ways, false, direction)) return true;
    }

    return false;
  }

  /// <summary>
  ///   A one-way join into <paramref name="room" />, made by going the opposite of <paramref name="direction" />, whose
  ///   end here is still only a guess: label that end with <paramref name="direction" /> as the assumed way back.
  /// </summary>
  private bool TakeAsWayBack(Room room, MappableDirection direction)
  {
    var cameBy = CompassPointHelper.GetOpposite(direction);
    var join = CurrentProject.Elements.OfType<Connection>().FirstOrDefault(connection =>
    {
      if (connection.Flow != ConnectionFlow.OneWay || IsCommandJoin(connection) ||
          connection.GetTargetRoom() != room || connection.GetSourceRoom() == room ||
          !string.IsNullOrWhiteSpace(connection.EndText))
        return false;
      var ways = JoinWays.Of(connection);
      return ways.Any(way => way.AtStart && way.IsDirection && way.Seen && way.Direction == cameBy) &&
             !JoinWays.SeenFrom(ways, false);
    });
    if (join == null) return false;

    var from = join.GetSourceRoom();
    if (IsCompass(direction))
    {
      var point = CompassPointHelper.GetCompassDirection(direction);
      if (ApproximateDirectionMatch(FacingPoint(room, from), point)) DockEnd(join, false, room, point);
    }

    // the guess this end made (the side it faced) gives way to the assumed way back
    var joinWays = JoinWays.Of(join);
    joinWays.RemoveAll(way => !way.AtStart && way.IsDirection && !way.Seen);
    joinWays.Add(JoinWay.Go(false, direction, false));
    JoinWays.Write(join, joinWays, LabelWord);
    return true;
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
    // a command led from there to here ("enter closet") and this direction leads back: the two are one join
    var commandedBack = connection == null ? CommandJoinFrom(target, source) : null;

    if (commandedBack != null)
    {
      connection = commandedBack;
      TurnCommandJoinIntoWayBack(connection, source, sourceCompassPoint);
      sourceIsStart = false;
      sourceEndIsGuess = false;
    }
    else if (connection == null)
    {
      var targetCompassPoint = CompassPointHelper.GetAutomapOpposite(sourceCompassPoint);
      TryMoveRoomsForTidyConnection(source, sourceCompassPoint, target, targetCompassPoint);
      // a one-way join's far end is only a guess until the way back is found, so it meets the far room on the side
      // facing the room it came from rather than looping round to the opposite side
      if (!assumeTwoWayConnections) targetCompassPoint = FreeFacingPoint(target, source, null);
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
                         !JoinWays.Lists(JoinWays.Of(connection), sourceIsStart, directionFromSource);
      if (wrongWay) connection.Flow = ConnectionFlow.TwoWay;
    }

    RecordDirection(connection, source, sourceIsStart, directionFromSource, sourceEndIsGuess, target,
      assumeTwoWayConnections);
    // two-way only now, so the far end keeps its command rather than gaining the opposite direction
    if (commandedBack != null) connection.Flow = ConnectionFlow.TwoWay;

    TakeOverCommandJoins(connection, source, target);

    // a newly known exit may take the point a guessed arrow was using
    FaceGuessedEnds();
  }

  /// <summary>
  ///   A direction leading from <paramref name="source" /> to <paramref name="target" /> comes first: commands that
  ///   lead the same way ("light lamp") join its middle label, instead of having dashed joins of their own.
  /// </summary>
  private void TakeOverCommandJoins(Connection connection, Room source, Room target)
  {
    List<JoinWay> ways = null;
    var sourceIsStart = connection.GetSourceRoom() == source;
    foreach (var other in CurrentProject.Elements.OfType<Connection>().ToList())
    {
      if (other == connection || !IsCommandJoin(other)) continue;
      if (other.GetSourceRoom() != source || other.GetTargetRoom() != target) continue;
      ways ??= JoinWays.Of(connection);
      foreach (var command in JoinWays.Commands(JoinWays.Of(other)))
        JoinWays.AddCommand(ways, sourceIsStart, command);
      CurrentProject.Elements.Remove(other);
    }

    if (ways != null) JoinWays.Write(connection, ways, LabelWord);
  }

  /// <summary>A one-way command join ("enter closet") from <paramref name="from" /> to <paramref name="to" />, if any.</summary>
  private Connection CommandJoinFrom(Room from, Room to) =>
    CurrentProject.Elements.OfType<Connection>().FirstOrDefault(connection =>
      IsCommandJoin(connection) && connection.Flow == ConnectionFlow.OneWay &&
      connection.GetSourceRoom() == from && connection.GetTargetRoom() == to);

  /// <summary>
  ///   A command join is found to lead back by a direction from its far room: it becomes an ordinary join, solid,
  ///   with the command written at its start (how you leave that room along it) instead of in the middle, and its far
  ///   end on the side of the direction when the rooms are that way round.
  /// </summary>
  private void TurnCommandJoinIntoWayBack(Connection connection, Room here, CompassPoint side)
  {
    // its commands stay ways from the far room; once the direction back is recorded they're written on that end
    // (JoinWays), and the side there says nothing
    var ways = JoinWays.Of(connection);
    connection.Style = ConnectionStyle.Solid;
    // here, the join leaves on the direction's side, as a new join would
    DockEnd(connection, false, here, side);
    JoinWays.Write(connection, ways, LabelWord);
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
    var ways = JoinWays.Of(connection);
    if (hereIsGuess)
    {
      // the guessed directions go; anything the user typed there stays
      DockEnd(connection, hereIsStart, here, CompassPointHelper.GetCompassDirection(direction));
      ways.RemoveAll(way => way.AtStart == hereIsStart && way.IsDirection);
    }

    var added = JoinWays.AddDirection(ways, hereIsStart, direction, seen: true);
    // assuming two-way joins, the way back is the opposite direction, unless that way from the far room is known
    // to lead somewhere else (north out of the closet: the foyer's south is the front porch)
    var back = CompassPointHelper.GetOpposite(direction);
    if (added && !hereIsGuess && assumeTwoWayConnections && connection.Flow == ConnectionFlow.TwoWay &&
        !RoomsAlong(far, back).Any(other => other != null && other != here))
      JoinWays.AddDirection(ways, !hereIsStart, back, seen: false);

    var hereCompass = JoinWays.FirstCompass(ways, hereIsStart);
    // the end moves to the compass side, unless the side it's on is one of its ways already. A side that only faced
    // the other room (a command's join, "turn on lamp", a guess) isn't a way, and gives way to the direction.
    if (hereCompass != null && !SideIsOneOfItsWays(connection, ways, hereIsStart))
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
          // the far end goes to the side of its own direction; with none ("in", "enter closet"), opposite this
          // end, unless another join already leaves that side (the foyer's south, to the front porch)
          var thereCompass = JoinWays.FirstCompass(ways, !hereIsStart);
          if (thereCompass != null)
            DockEnd(connection, !hereIsStart, far, CompassPointHelper.GetCompassDirection(thereCompass.Value));
          else if (!SideInUse(far, CompassPointHelper.GetOpposite(point), connection))
            DockEnd(connection, !hereIsStart, far, CompassPointHelper.GetOpposite(point));
        }
      }
    }

    JoinWays.Write(connection, ways, LabelWord);
  }

  /// <summary>
  ///   Whether the exit drawn at one end of an existing join is only a guess: nobody has set off from that end, and it
  ///   is the far end of a one-way join or the assumed way back of a two-way join automap made. Travelling from a
  ///   guessed end another way replaces the guess; travelling from a known end adds a direction. Labels don't count:
  ///   automap writes the assumed way back ("down") itself.
  /// </summary>
  /// <summary>Whether the side an end leaves from is a direction one of its ways lists.</summary>
  private static bool SideIsOneOfItsWays(Connection connection, List<JoinWay> ways, bool atStart)
  {
    var point = EndPoint(connection, atStart);
    return IsMainPoint(point) && JoinWays.Lists(ways, atStart, DirectionOfMainPoint(point));
  }

  private static bool IsGuessedEnd(Connection connection, bool atStart)
  {
    var ways = JoinWays.Of(connection);
    if (JoinWays.SeenFrom(ways, atStart)) return false;
    return connection.Flow == ConnectionFlow.OneWay ? !atStart : JoinWays.SeenFrom(ways, !atStart);
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

  /// <summary>Whether a join other than <paramref name="except" /> leaves <paramref name="room" /> at <paramref name="point" />.</summary>
  private bool SideInUse(Room room, CompassPoint point, Connection except) =>
    CurrentProject.Elements.OfType<Connection>().Any(connection =>
    {
      if (connection == except) return false;
      if (connection.GetSourceRoom(out var start) == room && start == point) return true;
      return connection.GetTargetRoom(out var end) == room && end == point;
    });

  private static void DockEnd(Connection connection, bool atStart, Room room, CompassPoint point)
  {
    var vertex = atStart ? connection.VertexList[0] : connection.VertexList[connection.VertexList.Count - 1];
    vertex.Port = room.PortAt(point);
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

  /// <summary>
  ///   A way out of <paramref name="room" /> that leads straight back in ("north" in the forest returns to the forest):
  ///   a one-way loop leaving the room on the side of the direction and coming back in on the next side round, labelled
  ///   with the direction. Further directions that loop back are added to the same loop's label.
  /// </summary>
  public void ConnectLoop(Room room, MappableDirection direction)
  {
    room = ResolveRoom(room);
    if (room == null || direction == MappableDirection.None) return;

    var loop = CurrentProject.Elements.OfType<Connection>().FirstOrDefault(connection =>
      connection.GetSourceRoom(out var from) == room && connection.GetTargetRoom(out var to) == room &&
      from != to && !IsCommandJoin(connection));
    List<JoinWay> ways;
    if (loop == null)
    {
      var side = CompassPointHelper.GetCompassDirection(direction);
      // the next side round, two of the sixteen points on: a stub uses the same side at both ends
      var back = (CompassPoint)(((int)side + 2) % 16);
      loop = AddConnection(room, side, room, back);
      loop.Flow = ConnectionFlow.OneWay;
      loop.Style = ConnectionStyle.Solid;
      ways = new List<JoinWay>();
    }
    else
    {
      ways = JoinWays.Of(loop);
      if (JoinWays.Lists(ways, true, direction)) return;
    }

    JoinWays.AddDirection(ways, true, direction, seen: true);
    JoinWays.Write(loop, ways, LabelWord);

    // a guessed arrow on the side the loop now uses moves off it (north into the forest, south loops back)
    FaceGuessedEnds();
  }

  public void ConnectStops(Room source, Room target, string command)
  {
    if (source == null || target == null || source == target || string.IsNullOrWhiteSpace(command)) return;
    command = command.Trim();
    var back = CurrentProject.Elements.OfType<Connection>().FirstOrDefault(connection =>
      IsCommandJoin(connection) && connection.Flow == ConnectionFlow.OneWay &&
      connection.GetSourceRoom() == target && connection.GetTargetRoom() == source &&
      !JoinWays.Commands(JoinWays.Of(connection)).Contains(command, StringComparer.OrdinalIgnoreCase));
    if (back == null)
    {
      ConnectByCommand(source, target, command);
      return;
    }

    var ways = JoinWays.Of(back);
    JoinWays.AddCommand(ways, false, command);
    back.Flow = ConnectionFlow.TwoWay;
    JoinWays.Write(back, ways, LabelWord);
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
      if (!JoinWays.Commands(JoinWays.Of(connection))
            .Any(c => string.Equals(c, command, StringComparison.OrdinalIgnoreCase)))
        continue;
      if (IsCommandJoin(connection) && !Leads(connection, source, target)) connection.Flow = ConnectionFlow.TwoWay;
      return;
    }

    // a direction that leads there comes first; the command is another way along it. Or another command that leads
    // there ("turn on lamp" as well as "light lamp"): one join lists both.
    var way = between.FirstOrDefault(connection => !IsCommandJoin(connection) && Leads(connection, source, target)) ??
              between.FirstOrDefault(connection => IsCommandJoin(connection) && Leads(connection, source, target));
    if (way != null)
    {
      var ways = JoinWays.Of(way);
      JoinWays.AddCommand(ways, way.GetSourceRoom() == source, command);
      JoinWays.Write(way, ways, LabelWord);
      return;
    }

    // the nearest free points facing each other, so the dashed line doesn't share a point with another join or an
    // unexplored exit
    var sourcePoint = FreeFacingPoint(source, target, null);
    var added = AddConnection(source, sourcePoint, target, FreeFacingPoint(target, source, null));
    added.Style = ConnectionStyle.Dashed;
    added.Flow = ConnectionFlow.OneWay;
    JoinWays.Write(added, new[] { JoinWay.Typed(true, command) }, LabelWord);
  }

  /// <summary>The side of <paramref name="from" /> (one of the eight main points) that faces <paramref name="to" />.</summary>
  /// <summary>
  ///   The side of <paramref name="from" /> facing <paramref name="to" />, using all sixteen points round the room,
  ///   and the nearest one that no other join (or exit stub) uses already, so arrows arriving from the same side
  ///   don't run into one point and look joined up. <paramref name="self" /> is the join being docked, if it exists.
  /// </summary>
  private CompassPoint FreeFacingPoint(Room from, Room to, Connection self) =>
    GuessedEnds.FreeFacingPoint(CurrentProject.Elements, from, to, self);

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

  /// <summary>
  ///   Write directions on join labels in short ("n / u / in") rather than in full ("north / up / in"). Automap's
  ///   settings turn it on by default; labels already written in full are still understood, and not doubled up.
  /// </summary>
  public bool AbbreviateDirections { get; set; }

  /// <summary>The word for <paramref name="direction" /> on a join's label: short or in full, as asked for.</summary>
  private string LabelWord(MappableDirection direction) =>
    AbbreviateDirections ? ShortWord(direction) : DirectionWord(direction);

  private static string ShortWord(MappableDirection direction) => direction switch
  {
    MappableDirection.North => "n",
    MappableDirection.NorthEast => "ne",
    MappableDirection.East => "e",
    MappableDirection.SouthEast => "se",
    MappableDirection.South => "s",
    MappableDirection.SouthWest => "sw",
    MappableDirection.West => "w",
    MappableDirection.NorthWest => "nw",
    MappableDirection.Up => "u",
    MappableDirection.Down => "d",
    _ => DirectionWord(direction)
  };

  private string _tidiedMap;

  /// <summary>
  ///   Tidies the map after a move. The step-by-step tidy only clears up rooms sitting on each other or on joins near
  ///   <paramref name="room" />: making the map neat (lined up, balanced) is the solver's job, which it does when the
  ///   map has changed (see <see cref="PlanRethink" />), and the step-by-step tidy would undo it. Walking around rooms
  ///   automap already knows leaves the map alone.
  /// </summary>
  public void TidyAround(Room room)
  {
    room = ResolveRoom(room);
    if (room == null) return;
    var unchanged = MapShape() == _tidiedMap;
    var before = RoomGlide.Positions(CurrentProject.Elements);
    // only where rooms clash: the step-by-step tidy knows nothing of lining up or balancing
    var moved = LayoutTidy.TidyAround(CurrentProject.Elements.ToList(), room, onlyIfCluttered: true);
    if (moved > 0)
    {
      // the rooms slide to their new places on screen rather than jumping
      RoomGlide.Start(before);
      FaceGuessedEnds();
      _session?.EnsureVisible(room);
    }

    _tidiedMap = MapShape();
    if (unchanged) return;
    var rethink = PlanRethink();
    if (rethink == null) return;
    if (RethinkElsewhere)
    {
      _rethink = rethink;
      _planned = rethink;
      _plannedShape = _tidiedMap;
    }
    else FinishRethink(rethink, rethink.Run());
  }

  /// <summary>
  ///   How long (in the app, in milliseconds) or how much work (without it) the map may spend rethinking its layout
  ///   after a move (see <see cref="PlanRethink" />).
  /// </summary>
  private const int RethinkMs = 1000;

  /// <summary>
  ///   How much work a repeatable rethink may do (see <see cref="RepeatableLayout" />): less than the app's search, as
  ///   it runs on one thread and is only for tests and the command line.
  /// </summary>
  private const int RepeatableRethinkWork = 300;

  // what was wrong with the layout after the last tidy (see LayoutTidy.Problems)
  private int _problems;

  /// <summary>
  ///   How many new rooms and joins the map may gain before it is rethought anyway (see <see cref="PlanRethink" />).
  /// </summary>
  private const int RethinkEvery = 3;

  // how many rooms and joins the map had at the last rethink
  private int _sizeAtRethink;

  // a rethink waiting to be worked out (see RethinkElsewhere)
  private LayoutTidy.TidyPlan _rethink;

  // the latest rethink worked out elsewhere, and what the map was made of when it was planned
  private LayoutTidy.TidyPlan _planned;
  private string _plannedShape;

  /// <summary>
  ///   True when the caller works out rethinks itself, off the map's thread (see <see cref="TakeRethink" />), so the
  ///   window stays responsive while the solver thinks. Otherwise <see cref="TidyAround" /> does it there and then.
  /// </summary>
  public bool RethinkElsewhere { get; set; }

  /// <summary>
  ///   Whether a rethink gives the same layout every time for the same map (one search thread, limited by work), or
  ///   searches on every thread for up to <see cref="RethinkMs" /> milliseconds, finding better layouts sooner but not
  ///   always the same one. Unset: the faster search when rethinking elsewhere (the app), the repeatable one otherwise
  ///   (tests, the command line), so a transcript always makes the same map there.
  /// </summary>
  public bool? RepeatableLayout { get; set; }

  /// <summary>The rethink <see cref="TidyAround" /> left to be worked out, if any (see <see cref="RethinkElsewhere" />).</summary>
  public LayoutTidy.TidyPlan TakeRethink()
  {
    var plan = _rethink;
    _rethink = null;
    return plan;
  }

  /// <summary>
  ///   When the map has just learnt something that leaves its layout worse than before (more joins pointing the wrong
  ///   way, or running across rooms), as when South Junction turns out to be south of the
  ///   junctions automap had put below it: nudging rooms a step at a time can't put that right, so the solver lays out
  ///   every room not placed by hand again, and the result is kept only if no fewer joins point their way. It also
  ///   does so every few new rooms or joins, to line rooms up and balance them as the map grows. Plans that on a copy
  ///   of the layout; null when the map doesn't need it.
  /// </summary>
  private LayoutTidy.TidyPlan PlanRethink()
  {
    var elements = CurrentProject.Elements.ToList();
    var problems = LayoutTidy.Problems(elements);
    var worse = problems > _problems;
    _problems = problems;
    var rooms = elements.OfType<Room>().Where(r => !r.PlacedByHand).ToList();
    // and every few new rooms or joins anyway: a layout that has nothing wrong can still be made neater (lined up,
    // balanced), and a join that closes a loop often shows how
    var size = elements.Count(element => element is Room || element is Connection);
    var grown = size >= _sizeAtRethink + RethinkEvery;
    if (!worse && !grown) return null;
    _sizeAtRethink = size;
    var repeatable = RepeatableLayout ?? !RethinkElsewhere;
    return rooms.Count == 0
      ? null
      : LayoutTidy.PlanRethink(elements, rooms, repeatable ? RepeatableRethinkWork : RethinkMs, repeatable);
  }

  /// <summary>
  ///   Puts the rooms where a rethink worked out in the background said (on the map's thread), as its own undo step,
  ///   while automap carries on. Nothing happens if a later rethink has been planned since, or if the map has changed
  ///   since (rooms or joins added): the layout would be out of date, so the next move plans another. Returns how many
  ///   rooms moved, or -1 if the user is part-way through a change of their own (a drag) and it should be tried again
  ///   shortly.
  /// </summary>
  public int FinishRethinkInTheBackground(LayoutTidy.TidyPlan plan, IReadOnlyDictionary<Room, Vector> moves)
  {
    if (plan == null || plan != _planned) return 0;
    if (MapShape() != _plannedShape)
    {
      _planned = null;
      // plan again at the next move
      _sizeAtRethink = int.MinValue / 2;
      return 0;
    }

    var batch = BeginBatch("Tidy the map");
    if (batch == null) return -1;
    using (batch)
    {
      _planned = null;
      return FinishRethink(plan, moves);
    }
  }

  /// <summary>
  ///   Puts the rooms where a rethink said (on the map's thread), sliding them there. Rooms deleted meanwhile are
  ///   left out. Returns how many rooms moved.
  /// </summary>
  public int FinishRethink(LayoutTidy.TidyPlan plan, IReadOnlyDictionary<Room, Vector> moves)
  {
    if (plan == null || moves == null || moves.Count == 0) return 0;
    var present = CurrentProject.Elements.OfType<Room>().ToHashSet();
    var before = RoomGlide.Positions(CurrentProject.Elements);
    var moved = LayoutTidy.TidyPlan.Apply(moves.Where(move => present.Contains(move.Key))
      .ToDictionary(move => move.Key, move => move.Value));
    if (moved > 0)
    {
      RoomGlide.Start(before);
      FaceGuessedEnds();
    }

    _problems = LayoutTidy.Problems(CurrentProject.Elements.ToList());
    _tidiedMap = MapShape();
    return moved;
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
  private void FaceGuessedEnds() => GuessedEnds.Face(CurrentProject.Elements.ToList());

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
    PlaceClear(room);

    room.ArbitraryAutomappedPosition = true;

    CurrentProject.Elements.Add(room);
    return room;
  }

  /// <summary>Moves <paramref name="room" /> left or right, further each time, until it's clear of every other room.</summary>
  private void PlaceClear(Room room)
  {
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
  }

  /// <summary>
  ///   Where <see cref="CreateRoom(Room, MappableDirection, string, string)" /> would put a new room reached from
  ///   <paramref name="existing" /> going <paramref name="direction" /> (or <see cref="CreateRoom(Room, string)" />, by
  ///   a command), without adding it: for a preview. Null when <paramref name="existing" /> isn't on the map.
  /// </summary>
  public Rect? NewRoomBounds(Room existing, MappableDirection? direction)
  {
    if (existing == null || !CurrentProject.Elements.Contains(existing)) return null;
    var room = new Room(CurrentProject);
    if (direction is { } way && way != MappableDirection.None)
    {
      var point = CompassPointHelper.GetCompassDirection(way);
      if (MakeSpaceForNewRooms || !TryPlaceInClearSpace(room, existing, point))
        PositionRelativeTo(room, existing, point, out _);
    }
    else
    {
      room.Position = Settings.Snap(existing.Position);
      PlaceClear(room);
    }

    return new Rect(room.Position.X, room.Position.Y, room.Width, room.Height);
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

  /// <summary>
  ///   The rooms the map shows <paramref name="direction" /> leading to from <paramref name="room" />: joins whose end
  ///   there is labelled with it ("n / up") or leaves from its side, one-way joins only from where they start.
  /// </summary>
  public IReadOnlyList<Room> RoomsAlong(Room room, MappableDirection direction)
  {
    var rooms = new List<Room>();
    if (room == null || direction == MappableDirection.None) return rooms;
    var point = CompassPointHelper.GetCompassDirection(direction);
    foreach (var connection in CurrentProject.Elements.OfType<Connection>())
    {
      if (IsCommandJoin(connection)) continue;
      var source = connection.GetSourceRoom(out var sourcePoint);
      var target = connection.GetTargetRoom(out var targetPoint);
      if (source == null || target == null || source == target) continue;
      if (source != room && target != room) continue;
      var ways = JoinWays.Of(connection);
      if (source == room && (sourcePoint == point || JoinWays.Lists(ways, true, direction)))
        rooms.Add(target);
      else if (target == room && connection.Flow == ConnectionFlow.TwoWay &&
               (targetPoint == point || JoinWays.Lists(ways, false, direction)))
        rooms.Add(source);
    }

    return rooms;
  }

  public void ForgetWay(Room from, MappableDirection direction, Room to)
  {
    from = ResolveRoom(from);
    to = ResolveRoom(to);
    if (from == null || to == null) return;
    foreach (var connection in CurrentProject.Elements.OfType<Connection>().ToList())
    {
      var source = connection.GetSourceRoom();
      var target = connection.GetTargetRoom();
      bool fromIsStart;
      if (source == from && target == to) fromIsStart = true;
      else if (source == to && target == from) fromIsStart = false;
      else continue;

      var ways = JoinWays.Of(connection);
      if (ways.RemoveAll(way => way.AtStart == fromIsStart && way.IsDirection && way.Direction == direction) == 0)
        continue;
      if (!ways.Any(way => way.Kind != JoinWayKind.Label))
      {
        CurrentProject.Elements.Remove(connection);
        continue;
      }

      // nothing leads from this end any more: the join only goes the other way, one-way from its start
      if (!ways.Any(way => way.AtStart == fromIsStart && way.Kind != JoinWayKind.Label))
      {
        connection.Flow = ConnectionFlow.OneWay;
        if (fromIsStart)
        {
          connection.Reverse();
          ways = ways.Select(way => way with { AtStart = !way.AtStart }).ToList();
        }
      }

      JoinWays.Write(connection, ways, LabelWord);
    }
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
    _session.KeepInMiddle(room);
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
