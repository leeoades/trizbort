using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Trizbort.Domain.Elements;
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

  private const int MaxPush = 6;

  /// <summary>
  ///   Tidies the rooms within <paramref name="radius" /> joins of <paramref name="centre" />; rooms further away
  ///   stay put but still count (nothing may be moved on top of them). Returns how many rooms moved.
  /// </summary>
  public static int TidyAround(IList<Element> elements, Room centre, int radius = 2, int timeLimitMs = 40)
  {
    if (elements == null || centre == null) return 0;
    var map = new LayoutMap(elements);
    if (!map.Contains(centre)) return 0;
    // the room just arrived at was only just placed, so it has nowhere it "should" stay
    return map.Tidy(map.Neighbourhood(centre, radius), timeLimitMs, centre);
  }

  /// <summary>Tidies the given rooms (all of them may move); the others stay put. Returns how many rooms moved.</summary>
  public static int Tidy(IList<Element> elements, IEnumerable<Room> rooms, int timeLimitMs = 400)
  {
    if (elements == null || rooms == null) return 0;
    var map = new LayoutMap(elements);
    return map.Tidy(rooms.Where(map.Contains).ToList(), timeLimitMs, null);
  }

  private sealed class Join
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

      foreach (var connection in elements.OfType<Connection>())
      {
        var a = connection.GetSourceRoom(out var point);
        var b = connection.GetTargetRoom();
        if (a == null || b == null || a == b) continue;
        if (!_index.TryGetValue(a, out var ia) || !_index.TryGetValue(b, out var ib)) continue;
        var join = new Join { A = ia, B = ib };
        if (!string.IsNullOrEmpty(connection.AutomapCommand))
        {
          join.Pull = 0; // a teleport: it can go anywhere
        }
        else
        {
          var direction = CompassPointHelper.GetAutomapDirectionVector(point);
          join.WantX = Math.Sign(direction.X);
          join.WantY = Math.Sign(direction.Y);
          join.Pull = IsMainPoint(point) ? CompassPull : SoftPull;
        }

        _joins.Add(join);
        _joinsOf[ia].Add(_joins.Count - 1);
        _joinsOf[ib].Add(_joins.Count - 1);
      }
    }

    public bool Contains(Room room) => room != null && _index.ContainsKey(room);

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

      return seen.Select(i => _rooms[i]).ToList();
    }

    private int _free = -1;

    public int Tidy(List<Room> movableRooms, int timeLimitMs, Room free)
    {
      if (movableRooms.Count == 0) return 0;
      _free = free != null && _index.TryGetValue(free, out var freeIndex) ? freeIndex : -1;
      var movable = movableRooms.Select(room => _index[room]).ToHashSet();
      var clock = Stopwatch.StartNew();
      var score = Score(movable);

      for (var pass = 0; pass < 12; pass++)
      {
        var improved = false;
        foreach (var room in movable)
          for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
              if (dx == 0 && dy == 0) continue;
              if (clock.ElapsedMilliseconds > timeLimitMs) return Apply();
              var moved = TryMove(room, dx, dy, movable);
              if (moved == null) continue;
              var after = Score(movable);
              if (after < score - 0.001)
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

        if (!improved) break;
      }

      return Apply();
    }

    /// <summary>
    ///   Moves a room one step, pushing along any movable room it would land on (and so on, up to a few rooms).
    ///   Returns the rooms moved, or null if a room that can't move is in the way.
    /// </summary>
    private List<int> TryMove(int room, int dx, int dy, HashSet<int> movable)
    {
      var moved = new List<int>();
      var queue = new Queue<int>();
      queue.Enqueue(room);
      var inMove = new HashSet<int> { room };
      while (queue.Count > 0)
      {
        var current = queue.Dequeue();
        moved.Add(current);
        _x[current] += dx * _stepX;
        _y[current] += dy * _stepY;
        for (var other = 0; other < _rooms.Count; other++)
        {
          if (inMove.Contains(other) || !Overlaps(current, other)) continue;
          if (!movable.Contains(other) || inMove.Count >= MaxPush)
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
      foreach (var j in joins)
      {
        var join = _joins[j];
        if (join.Pull > 0)
        {
          var offX = (_x[join.B] - _x[join.A]) / _stepX - join.WantX;
          var offY = (_y[join.B] - _y[join.A]) / _stepY - join.WantY;
          score += join.Pull * (offX * offX + offY * offY);
        }

        // no room sits on a join that isn't its own
        for (var room = 0; room < _rooms.Count; room++)
          if (room != join.A && room != join.B && SegmentHitsRoom(join.A, join.B, room))
            score += RoomOnJoinCost;
      }

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

      // and rooms prefer to stay where they were
      foreach (var room in movable)
      {
        if (room == _free) continue;
        var mx = (_x[room] - _startX[room]) / _stepX;
        var my = (_y[room] - _startY[room]) / _stepY;
        score += StayPut * (mx * mx + my * my);
      }

      return score;
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
