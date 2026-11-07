using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Automap;

/// <summary>
///   Moves rooms so the map reads clearly: each join pulls its far room one step away in its direction, rooms keep
///   clear of each other and of other rooms' joins, joins avoid crossing, and rooms prefer to stay where they are.
///   A small local search: it tries moving a room one step (pushing any room in the way along with it) and keeps the
///   move only if the map scores better. Used by automap after each move (<see cref="TidyAround" />).
/// </summary>
/// <remarks>
///   A join's direction comes from the side of the room it leaves. How it is drawn (style, labels) doesn't matter.
///   Teleport joins (<see cref="Connection.AutomapCommand" />) don't pull, and up, down, in and out pull gently, since
///   another floor can sit anywhere nearby.
///   A join that already reads correctly should keep reading correctly: rooms joined like that (a group) can move as
///   a whole, making space may stretch a group along its joins, and bending one of its joins the wrong way costs
///   extra, so it happens only to get a room off another room or off a join. Of several joins between the same
///   two rooms, only the one that fits best counts. The far end of a one-way join nobody has come back along says
///   nothing about where its room should be.
/// </remarks>
public static class LayoutTidy
{
  // weights of the score; lower is better
  private const double OverlapCost = 400;
  private const double RoomOnJoinCost = 60;
  private const double CrossingCost = 6;
  private const double CompassPull = 1.0;
  private const double SoftPull = 0.25;
  private const double StayPut = 0.2;

  /// <summary>A join counts as naturally placed when it points within this many degrees of its direction.</summary>
  private const double NaturalAngle = 22.6;

  /// <summary>Cost per step a join is longer than one; small, since a long join that points the right way reads fine.</summary>
  private const double LengthCost = 0.08;

  /// <summary>How many rooms one step may push along with it.</summary>
  private const int MaxPush = 24;

  /// <summary>Extra cost of bending a join that reads correctly the wrong way, on top of the join's own cost.</summary>
  private const double BendPenalty = 1;

  /// <summary>
  ///   Tidies the rooms within <paramref name="radius" /> joins of <paramref name="centre" />; rooms further away
  ///   stay put but still count (nothing may be moved on top of them). Returns how many rooms moved.
  /// </summary>
  /// <param name="onlyIfCluttered">
  ///   Tidy only if a room near <paramref name="centre" /> sits on another room or on a join; otherwise leave the
  ///   map alone (for a move that didn't change the map).
  /// </param>
  public static int TidyAround(
    IList<Element> elements, Room centre, int radius = 2, int timeLimitMs = 80, bool onlyIfCluttered = false)
  {
    if (elements == null || centre == null) return 0;
    var map = new LayoutMap(elements);
    if (!map.Contains(centre)) return 0;
    if (onlyIfCluttered && !map.Cluttered(map.Neighbourhood(centre, radius))) return 0;
    // A room reached for the first time (one join so far) was only just placed, so it has nowhere it "should"
    // stay; a room the player came back to keeps its place like the others.
    return map.Tidy(map.Neighbourhood(centre, radius), timeLimitMs, map.JoinCount(centre) <= 1 ? centre : null);
  }

  /// <summary>
  ///   How many joins are naturally placed: the far room lies in the direction the join's ends give (within 22.5
  ///   degrees), as read on the map. Joins without a direction (teleports, exit stubs) aren't counted.
  /// </summary>
  public static (int Natural, int Total) NaturalJoins(IList<Element> elements)
  {
    var map = new LayoutMap(elements);
    var joins = map.DirectedJoins.ToList();
    return (joins.Count(map.IsNatural), joins.Count);
  }

  /// <summary>Tidies the given rooms (all of them may move); the others stay put. Returns how many rooms moved.</summary>
  public static int Tidy(IList<Element> elements, IEnumerable<Room> rooms, int timeLimitMs = 400)
  {
    if (elements == null || rooms == null) return 0;
    var map = new LayoutMap(elements);
    return map.Tidy(rooms.Where(map.Contains).ToList(), timeLimitMs, null);
  }

  internal sealed class Join
  {
    public int A;
    public int B;

    /// <summary>Where B should be relative to A, in steps.</summary>
    public double WantX;

    public double WantY;
    public double Pull;
  }

  private sealed class LayoutMap
  {
    private readonly List<Room> _rooms;
    private readonly Dictionary<Room, int> _index = new(ReferenceEqualityComparer.Instance);
    private readonly List<Join> _joins = new();
    private readonly List<int>[] _joinsOf;

    // centres and half sizes; positions change as the search runs
    private readonly double[] _x;
    private readonly double[] _y;
    private readonly double[] _startX;
    private readonly double[] _startY;
    private readonly double[] _halfW;
    private readonly double[] _halfH;
    private readonly double _stepX;
    private readonly double _stepY;
    private readonly double _gap;

    public LayoutMap(IList<Element> elements)
    {
      _rooms = elements.OfType<Room>().ToList();
      for (var i = 0; i < _rooms.Count; i++) _index[_rooms[i]] = i;
      var n = _rooms.Count;
      _x = new double[n];
      _y = new double[n];
      _startX = new double[n];
      _startY = new double[n];
      _halfW = new double[n];
      _halfH = new double[n];
      _joinsOf = new List<int>[n];
      for (var i = 0; i < n; i++)
      {
        var bounds = _rooms[i].InnerBounds;
        _x[i] = _startX[i] = bounds.Center.X;
        _y[i] = _startY[i] = bounds.Center.Y;
        _halfW[i] = bounds.Width / 2;
        _halfH[i] = bounds.Height / 2;
        _joinsOf[i] = new List<int>();
      }

      _gap = Settings.PreferredDistanceBetweenRooms;
      var width = n > 0 ? _halfW.Average() * 2 : 96;
      var height = n > 0 ? _halfH.Average() * 2 : 64;
      _stepX = _gap + width;
      _stepY = _gap + height;

      var pairOf = new Dictionary<(int, int), int>();
      foreach (var connection in elements.OfType<Connection>())
      {
        var a = connection.GetSourceRoom(out var point);
        var b = connection.GetTargetRoom(out var targetPoint);
        if (a == null || b == null || a == b) continue;
        if (!_index.TryGetValue(a, out var ia) || !_index.TryGetValue(b, out var ib)) continue;
        var join = new Join { A = ia, B = ib };
        if (!string.IsNullOrEmpty(connection.AutomapCommand))
        {
          join.Pull = 0; // a teleport: it can go anywhere
        }
        else
        {
          // Both ends say where the other room is. "East from South Junction" and "south from Southeast Junction"
          // together put Southeast Junction to the north-east; when the ends agree (east and west) it is just east.
          // A compass end beats up, down, in and out, which don't say much about where a room is drawn.
          var fromA = CompassPointHelper.GetAutomapDirectionVector(point);
          var fromB = CompassPointHelper.GetAutomapDirectionVector(targetPoint);
          var aIsCompass = IsMainPoint(point);
          // the far end of a one-way join nobody has come back along only faces the room it came from: it says
          // nothing about where that room should be
          var bIsGuess = connection.Flow == ConnectionFlow.OneWay && string.IsNullOrWhiteSpace(connection.EndTravelled) &&
                         string.IsNullOrWhiteSpace(connection.EndText);
          var bIsCompass = IsMainPoint(targetPoint) && !bIsGuess;
          double x, y;
          if (bIsGuess && !aIsCompass)
          {
            x = fromA.X;
            y = fromA.Y;
          }
          else if (aIsCompass == bIsCompass)
          {
            x = fromA.X - fromB.X;
            y = fromA.Y - fromB.Y;
          }
          else if (aIsCompass)
          {
            x = fromA.X;
            y = fromA.Y;
          }
          else
          {
            x = -fromB.X;
            y = -fromB.Y;
          }

          if (Math.Sign(x) == 0 && Math.Sign(y) == 0)
          {
            // the ends contradict each other (east there, east back): go by the first
            x = fromA.X;
            y = fromA.Y;
          }

          join.WantX = Math.Sign(x);
          join.WantY = Math.Sign(y);
          join.Pull = aIsCompass || bIsCompass ? CompassPull : SoftPull;
        }

        _joins.Add(join);
        _joinsOf[ia].Add(_joins.Count - 1);
        _joinsOf[ib].Add(_joins.Count - 1);
        var key = ia < ib ? (ia, ib) : (ib, ia);
        if (!pairOf.TryGetValue(key, out var pair))
        {
          pair = _pairs.Count;
          pairOf[key] = pair;
          _pairs.Add(new List<int>());
        }

        _pairs[pair].Add(_joins.Count - 1);
        _pairIndex.Add(pair);
      }

      _group = Enumerable.Range(0, n).ToArray();
    }

    // joins between the same two rooms ("east" and "north" both lead to the Hall): only the one that fits best counts
    private readonly List<List<int>> _pairs = new();
    private readonly List<int> _pairIndex = new();

    // groups of rooms whose joins between them read correctly; a group moves as one piece
    private readonly int[] _group;
    private Dictionary<int, List<int>> _members = new();

    private bool PairNatural(int pair) => _pairs[pair].Any(j => _joins[j].Pull > 0 && IsNatural(_joins[j]));

    /// <summary>
    ///   Rooms joined by naturally placed joins form a group, which keeps its shape: the tidy moves the group as a
    ///   whole, or opens it up only along its joins' directions. The room just arrived at belongs to none yet.
    /// </summary>
    private void BuildGroups()
    {
      var parent = Enumerable.Range(0, _rooms.Count).ToArray();

      int Find(int i)
      {
        while (parent[i] != i) i = parent[i] = parent[parent[i]];
        return i;
      }

      for (var pair = 0; pair < _pairs.Count; pair++)
      {
        var join = _joins[_pairs[pair][0]];
        if (join.A == _free || join.B == _free || !PairNatural(pair)) continue;
        parent[Find(join.A)] = Find(join.B);
      }

      _members = new Dictionary<int, List<int>>();
      for (var i = 0; i < _rooms.Count; i++)
      {
        _group[i] = Find(i);
        if (!_members.TryGetValue(_group[i], out var list)) _members[_group[i]] = list = new List<int>();
        list.Add(i);
      }
    }

    /// <summary>The naturally placed pairs of rooms that moving <paramref name="moving" /> pulls apart (the new room aside).</summary>
    private List<int> NaturalPairsAcross(IEnumerable<int> moving)
    {
      var set = moving.ToHashSet();
      var across = new List<int>();
      for (var pair = 0; pair < _pairs.Count; pair++)
      {
        var join = _joins[_pairs[pair][0]];
        if (join.A == _free || join.B == _free) continue;
        if (set.Contains(join.A) == set.Contains(join.B)) continue;
        if (PairNatural(pair)) across.Add(pair);
      }

      return across;
    }

    /// <summary>
    ///   What it costs to bend joins that read correctly the wrong way: about double a join left pointing the wrong
    ///   way, so a group keeps its shape unless that gets a room off another room or off a join.
    /// </summary>
    private double BendCost(List<int> pairs) => BendPenalty * pairs.Count(pair => !PairNatural(pair));

    public bool Contains(Room room) => room != null && _index.ContainsKey(room);

    /// <summary>Whether any of these rooms sits on another room or on a join, or has a join running over a room.</summary>
    public bool Cluttered(IEnumerable<Room> rooms)
    {
      var set = rooms.Select(room => _index[room]).ToHashSet();
      foreach (var a in set)
        for (var b = 0; b < _rooms.Count; b++)
          if (b != a && Overlaps(a, b))
            return true;

      for (var j = 0; j < _joins.Count; j++)
      {
        var join = _joins[j];
        var touches = set.Contains(join.A) || set.Contains(join.B);
        for (var room = 0; room < _rooms.Count; room++)
          if (room != join.A && room != join.B && (touches || set.Contains(room)) &&
              SegmentHitsRoom(join.A, join.B, room))
            return true;
      }

      return false;
    }

    public int JoinCount(Room room) => _joinsOf[_index[room]].Count;

    public List<Room> Neighbourhood(Room centre, int radius)
    {
      var seen = new HashSet<int> { _index[centre] };
      var frontier = new List<int> { _index[centre] };
      for (var depth = 0; depth < radius; depth++)
      {
        var next = new List<int>();
        foreach (var room in frontier)
          foreach (var j in _joinsOf[room])
          {
            var other = _joins[j].A == room ? _joins[j].B : _joins[j].A;
            if (seen.Add(other)) next.Add(other);
          }

        frontier = next;
      }

      // and any room close by, which may be in the way of the centre room
      var c = _index[centre];
      for (var i = 0; i < _rooms.Count; i++)
        if (Math.Abs(_x[i] - _x[c]) < _stepX * 1.5 && Math.Abs(_y[i] - _y[c]) < _stepY * 1.5)
          seen.Add(i);

      return seen.Select(i => _rooms[i]).ToList();
    }

    private int _free = -1;
    private HashSet<int> _movable = new();

    public int Tidy(List<Room> movableRooms, int timeLimitMs, Room free)
    {
      if (movableRooms.Count == 0) return 0;
      _free = free != null && _index.TryGetValue(free, out var freeIndex) ? freeIndex : -1;
      _movable = movableRooms.Select(room => _index[room]).ToHashSet();
      var movable = movableRooms.Select(room => _index[room]).ToHashSet();
      BuildGroups();
      var clock = Stopwatch.StartNew();
      var score = Score(movable);

      for (var pass = 0; pass < 12; pass++)
      {
        var improved = false;

        // The room just arrived at first, trying every move and taking the best, not the first that helps: sliding
        // it aside is found before making space for it, but making space is usually better.
        if (_free >= 0 && clock.ElapsedMilliseconds <= timeLimitMs)
        {
          var best = BestMoveForFree(movable, score);
          if (best != null)
          {
            Shift(best.Value.Rooms, best.Value.Dx, best.Value.Dy);
            score = Score(movable);
            improved = true;
          }
        }
        // move a room, or a whole group, one step; a group with a room that has to stay put stays put
        var units = movable.Select(room => new List<int> { room }).Concat(movable.Select(room => _group[room])
            .Distinct().Select(group => _members[group]).Where(unit => unit.Count > 1 && unit.All(movable.Contains)))
          .ToList();
        foreach (var unit in units)
          for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
              if (dx == 0 && dy == 0) continue;
              if (clock.ElapsedMilliseconds > timeLimitMs) return Apply();
              var moved = TryMove(unit, dx, dy, movable, out var across);
              if (moved == null) continue;
              var after = Score(movable);
              // bending a join that reads correctly costs extra, so groups keep their shape
              if (after + BendCost(across) < score - 0.001)
              {
                score = after;
                improved = true;
              }
              else
              {
                foreach (var i in moved)
                {
                  _x[i] -= dx * _stepX;
                  _y[i] -= dy * _stepY;
                }
              }
            }

        // Make space: move a room and every movable room beyond it, in that direction, one step further out,
        // so a group keeps its shape (like moving the map apart, but only near the change).
        foreach (var room in movable)
          for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
              if ((dx == 0 && dy == 0) || room == _free) continue;
              if (clock.ElapsedMilliseconds > timeLimitMs) return Apply();
              var side = movable.Where(other => other != _free &&
                                                (_x[other] - _x[room]) * dx / _stepX +
                                                (_y[other] - _y[room]) * dy / _stepY > -0.5).ToList();
              if (side.Count < 2) continue;
              var sideAcross = NaturalPairsAcross(side);
              foreach (var i in side)
              {
                _x[i] += dx * _stepX;
                _y[i] += dy * _stepY;
              }

              var after = Score(movable);
              if (after + BendCost(sideAcross) < score - 0.001)
              {
                score = after;
                improved = true;
              }
              else
              {
                foreach (var i in side)
                {
                  _x[i] -= dx * _stepX;
                  _y[i] -= dy * _stepY;
                }
              }
            }

        // Make space across the map: move a room one step and shift every room at or beyond the spot it moves to,
        // anywhere on the map, one step further the same way. Going south into a garden where the Patio is moves
        // the hedge and everything above it up, instead of pushing the garden off to one side.
        foreach (var room in movable)
          for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
              if (dx == 0 && dy == 0) continue;
              if (clock.ElapsedMilliseconds > timeLimitMs) return Apply();
              var beyond = Beyond(room, dx, dy);
              if (beyond.Count == 0) continue;
              var counted = new HashSet<int>(movable);
              counted.UnionWith(beyond);
              var before = Score(counted);
              var across = NaturalPairsAcross(beyond.Append(room));
              foreach (var i in beyond.Append(room))
              {
                _x[i] += dx * _stepX;
                _y[i] += dy * _stepY;
              }

              // making space may stretch a group along its joins; bending one of them the wrong way costs extra
              if (Score(counted) + BendCost(across) < before - 0.001)
              {
                score = Score(movable);
                improved = true;
              }
              else
              {
                foreach (var i in beyond.Append(room))
                {
                  _x[i] -= dx * _stepX;
                  _y[i] -= dy * _stepY;
                }
              }
            }

        if (!improved) break;
      }

      return Apply();
    }

    private void Shift(IEnumerable<int> rooms, int dx, int dy)
    {
      foreach (var i in rooms)
      {
        _x[i] += dx * _stepX;
        _y[i] += dy * _stepY;
      }
    }

    /// <summary>The rooms at or beyond the spot one step from <paramref name="room" />, anywhere on the map.</summary>
    private List<int> Beyond(int room, int dx, int dy)
    {
      var targetX = _x[room] + dx * _stepX;
      var targetY = _y[room] + dy * _stepY;
      return Enumerable.Range(0, _rooms.Count)
        .Where(other => other != room && other != _free &&
                        (_x[other] - targetX) * dx / _stepX + (_y[other] - targetY) * dy / _stepY > -0.5)
        .ToList();
    }

    /// <summary>
    ///   The best single step for the room just arrived at: on its own (pushing nearby rooms), or with every room
    ///   beyond the spot moving out of its way. Null when nothing improves the map.
    /// </summary>
    private (List<int> Rooms, int Dx, int Dy)? BestMoveForFree(HashSet<int> movable, double score)
    {
      (List<int> Rooms, int Dx, int Dy)? best = null;
      var bestGain = 0.001;
      for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
          if (dx == 0 && dy == 0) continue;

          var pushed = TryMove(new[] { _free }, dx, dy, movable, out var pushedAcross);
          if (pushed != null)
          {
            var gain = score - Score(movable) - BendCost(pushedAcross);
            Shift(pushed, -dx, -dy);
            if (gain > bestGain)
            {
              bestGain = gain;
              best = (pushed, dx, dy);
            }
          }

          var beyond = Beyond(_free, dx, dy);
          if (beyond.Count == 0) continue;
          var counted = new HashSet<int>(movable);
          counted.UnionWith(beyond);
          var before = Score(counted);
          var all = beyond.Append(_free).ToList();
          var across = NaturalPairsAcross(all);
          Shift(all, dx, dy);
          var spaceGain = before - Score(counted) - BendCost(across);
          Shift(all, -dx, -dy);
          if (spaceGain > bestGain)
          {
            bestGain = spaceGain;
            best = (all, dx, dy);
          }
        }

      return best;
    }

    /// <summary>
    ///   Moves some rooms one step, pushing along any movable room they would land on (and so on, up to a few rooms).
    ///   Returns the rooms moved, or null if a room that can't move is in the way; <paramref name="across" /> gets
    ///   the naturally placed joins the move pulls apart, as they were before it.
    /// </summary>
    private List<int> TryMove(IEnumerable<int> rooms, int dx, int dy, HashSet<int> movable, out List<int> across)
    {
      across = null;
      var moved = new List<int>();
      var queue = new Queue<int>(rooms);
      var inMove = new HashSet<int>(queue);
      var limit = inMove.Count + MaxPush;
      while (queue.Count > 0)
      {
        var current = queue.Dequeue();
        moved.Add(current);
        _x[current] += dx * _stepX;
        _y[current] += dy * _stepY;
        for (var other = 0; other < _rooms.Count; other++)
        {
          if (inMove.Contains(other) || !Overlaps(current, other)) continue;
          if (!movable.Contains(other) || inMove.Count >= limit)
          {
            foreach (var i in moved)
            {
              _x[i] -= dx * _stepX;
              _y[i] -= dy * _stepY;
            }

            return null;
          }

          inMove.Add(other);
          queue.Enqueue(other);
        }
      }

      // which joins read correctly is judged where the rooms were
      Shift(moved, -dx, -dy);
      across = NaturalPairsAcross(moved);
      Shift(moved, dx, dy);
      return moved;
    }

    private bool Overlaps(int a, int b) =>
      Math.Abs(_x[a] - _x[b]) < _halfW[a] + _halfW[b] + _gap / 4 &&
      Math.Abs(_y[a] - _y[b]) < _halfH[a] + _halfH[b] + _gap / 4;

    /// <summary>The score of everything that involves a movable room; lower is better.</summary>
    private double Score(HashSet<int> movable)
    {
      double score = 0;

      // rooms keep clear of each other
      foreach (var a in movable)
        for (var b = 0; b < _rooms.Count; b++)
          if (b != a && (!movable.Contains(b) || b > a) && Overlaps(a, b))
            score += OverlapCost;

      // the joins touching a movable room
      var joins = movable.SelectMany(room => _joinsOf[room]).Distinct().ToList();
      var pairCost = new Dictionary<int, double>();
      foreach (var j in joins)
      {
        var join = _joins[j];
        if (join.Pull > 0)
        {
          // of several joins between the same two rooms, the one that fits best counts
          var cost = JoinCost(join);
          var pair = _pairIndex[j];
          pairCost[pair] = pairCost.TryGetValue(pair, out var other) ? Math.Min(other, cost) : cost;
        }

        // no room sits on a join that isn't its own
        for (var room = 0; room < _rooms.Count; room++)
          if (room != join.A && room != join.B && SegmentHitsRoom(join.A, join.B, room))
            score += RoomOnJoinCost;
      }

      score += pairCost.Values.Sum();

      // a movable room doesn't sit on other joins
      foreach (var room in movable)
        for (var j = 0; j < _joins.Count; j++)
        {
          var join = _joins[j];
          if (join.A == room || join.B == room || movable.Contains(join.A) || movable.Contains(join.B)) continue;
          if (SegmentHitsRoom(join.A, join.B, room)) score += RoomOnJoinCost;
        }

      // joins don't cross
      foreach (var j in joins)
        for (var k = 0; k < _joins.Count; k++)
        {
          if (k == j || (joins.Contains(k) && k < j)) continue;
          if (Cross(_joins[j], _joins[k])) score += CrossingCost;
        }

      // and rooms prefer to stay where they were (rooms moved along with a whole side of the map don't count:
      // they keep their places relative to each other)
      foreach (var room in _movable)
      {
        if (!movable.Contains(room)) continue;
        if (room == _free) continue;
        var mx = (_x[room] - _startX[room]) / _stepX;
        var my = (_y[room] - _startY[room]) / _stepY;
        score += StayPut * (mx * mx + my * my);
      }

      return score;
    }

    /// <summary>
    ///   What a join costs: nothing when it is naturally placed (the far room lies within 22.5 degrees of the
    ///   direction its ends give, as you would read it on the map), more the further off it points, plus a little for
    ///   each step longer than one, so that rooms stay close when nothing else matters.
    /// </summary>
    private double JoinCost(Join join)
    {
      var ax = (_x[join.B] - _x[join.A]) / _stepX;
      var ay = (_y[join.B] - _y[join.A]) / _stepY;
      var want = Math.Sqrt(join.WantX * join.WantX + join.WantY * join.WantY);
      if (want < 1e-9) return 0;
      var distance = Math.Sqrt(ax * ax + ay * ay);
      if (distance < 1e-6) return join.Pull * 2;
      var off = AngleBetween(ax, ay, join.WantX, join.WantY);
      var cost = off <= NaturalAngle ? 0 : 1 + (off - NaturalAngle) / 45;
      cost += LengthCost * Math.Max(0, distance - want);
      return join.Pull * cost;
    }

    /// <summary>Whether a join is naturally placed (see <see cref="JoinCost" />).</summary>
    public bool IsNatural(Join join)
    {
      var ax = (_x[join.B] - _x[join.A]) / _stepX;
      var ay = (_y[join.B] - _y[join.A]) / _stepY;
      if (Math.Sqrt(ax * ax + ay * ay) < 1e-6) return false;
      return AngleBetween(ax, ay, join.WantX, join.WantY) <= NaturalAngle;
    }

    public IEnumerable<Join> DirectedJoins => _joins.Where(join => join.Pull > 0);

    private static double AngleBetween(double ax, double ay, double bx, double by)
    {
      var difference = Math.Abs(Math.Atan2(ay, ax) - Math.Atan2(by, bx)) * 180 / Math.PI;
      return difference > 180 ? 360 - difference : difference;
    }

    private bool SegmentHitsRoom(int a, int b, int room)
    {
      // the room's box, slightly shrunk, against the line between the two room centres
      var left = _x[room] - _halfW[room] * 0.9;
      var right = _x[room] + _halfW[room] * 0.9;
      var top = _y[room] - _halfH[room] * 0.9;
      var bottom = _y[room] + _halfH[room] * 0.9;
      return SegmentHitsBox(_x[a], _y[a], _x[b], _y[b], left, top, right, bottom);
    }

    private static bool SegmentHitsBox(
      double x1, double y1, double x2, double y2, double left, double top, double right, double bottom)
    {
      // Liang-Barsky clipping
      var t0 = 0.0;
      var t1 = 1.0;
      var dx = x2 - x1;
      var dy = y2 - y1;
      var p = new[] { -dx, dx, -dy, dy };
      var q = new[] { x1 - left, right - x1, y1 - top, bottom - y1 };
      for (var i = 0; i < 4; i++)
      {
        if (Math.Abs(p[i]) < 1e-9)
        {
          if (q[i] < 0) return false;
          continue;
        }

        var t = q[i] / p[i];
        if (p[i] < 0) t0 = Math.Max(t0, t);
        else t1 = Math.Min(t1, t);
        if (t0 > t1) return false;
      }

      return true;
    }

    private bool Cross(Join one, Join two)
    {
      if (one.A == two.A || one.A == two.B || one.B == two.A || one.B == two.B) return false;
      return SegmentsCross(_x[one.A], _y[one.A], _x[one.B], _y[one.B], _x[two.A], _y[two.A], _x[two.B], _y[two.B]);
    }

    private static bool SegmentsCross(
      double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
    {
      static double Side(double px, double py, double qx, double qy, double rx, double ry) =>
        (qx - px) * (ry - py) - (qy - py) * (rx - px);

      var d1 = Side(cx, cy, dx, dy, ax, ay);
      var d2 = Side(cx, cy, dx, dy, bx, by);
      var d3 = Side(ax, ay, bx, by, cx, cy);
      var d4 = Side(ax, ay, bx, by, dx, dy);
      return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    /// <summary>Puts the rooms where the search left them; returns how many moved.</summary>
    private int Apply()
    {
      var moved = 0;
      for (var i = 0; i < _rooms.Count; i++)
      {
        if (Math.Abs(_x[i] - _startX[i]) < 0.5 && Math.Abs(_y[i] - _startY[i]) < 0.5) continue;
        var room = _rooms[i];
        room.Position = Settings.Snap(new Vector((float)(_x[i] - _halfW[i]), (float)(_y[i] - _halfH[i])));
        moved++;
      }

      return moved;
    }

    private static bool IsMainPoint(CompassPoint point) => point is CompassPoint.North or CompassPoint.NorthEast
      or CompassPoint.East or CompassPoint.SouthEast or CompassPoint.South or CompassPoint.SouthWest
      or CompassPoint.West or CompassPoint.NorthWest;
  }
}
