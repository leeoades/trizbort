using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Google.OrTools.Sat;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Automap;

/// <summary>
///   The solver tidy: lays the rooms out on the grid in one go with a constraint solver (Google OR-Tools CP-SAT), in
///   the manner of Nöllenburg and Wolff's metro-map layout, instead of improving the layout a step at a time.
/// </summary>
/// <remarks>
///   Each room takes whole grid squares (a tall room several), and no two rooms share one. Each join with a compass
///   direction is either "satisfied" or not: satisfied means the far room's centre lies within 22.5 degrees of that
///   direction, the same rule as the rest of the tidy, and the solver satisfies as many as it can. It starts from the
///   current layout and never leaves the map with fewer joins pointing their way than it found. After that it keeps
///   joins short and rooms near where they were. Joins running over rooms and joins crossing aren't modelled; the
///   step-by-step tidy polishes the result for those afterwards.
/// </remarks>
public static partial class LayoutTidy
{
  private const int SatisfiedWeight = 100;

  /// <summary>Bonus for a join exactly level, upright or diagonal, so rooms line up in rows and columns.</summary>
  private const int ExactWeight = 20;
  private const int LengthWeight = 3;
  private const int MoveWeight = 1;

  /// <summary>How far (in squares) beyond a join's rooms the solver looks for rooms that could end up on it.</summary>
  private const int LineReach = 4;

  /// <summary>
  ///   When <paramref name="satisfied" />, the slanting join from <paramref name="a" /> to <paramref name="b" />
  ///   (towards <paramref name="wx" />, <paramref name="wy" />) has no room within the box between them: every other
  ///   room nearby is in the same columns as one of them or beyond, or in the same rows or beyond.
  /// </summary>
  private static void KeepOutOfTheBox(
    CpModel model, BoolVar satisfied, int a, int b, int wx, int wy, IntVar[] x, IntVar[] y, long[] widthCells,
    long[] heightCells, long[] cellX, long[] cellY, int j)
  {
    var n = x.Length;
    var (leftRoom, rightRoom) = wx > 0 ? (a, b) : (b, a);
    var (topRoom, bottomRoom) = wy > 0 ? (a, b) : (b, a);
    var left = Math.Min(cellX[a], cellX[b]) - LineReach;
    var right = Math.Max(cellX[a], cellX[b]) + LineReach;
    var top = Math.Min(cellY[a], cellY[b]) - LineReach;
    var bottom = Math.Max(cellY[a], cellY[b]) + LineReach;
    for (var c = 0; c < n; c++)
    {
      if (c == a || c == b || cellX[c] < left || cellX[c] > right || cellY[c] < top || cellY[c] > bottom) continue;
      var west = model.NewBoolVar($"bw{j}_{c}");
      var east = model.NewBoolVar($"be{j}_{c}");
      var north = model.NewBoolVar($"bn{j}_{c}");
      var south = model.NewBoolVar($"bs{j}_{c}");
      model.Add(x[c] + widthCells[c] <= x[leftRoom] + widthCells[leftRoom]).OnlyEnforceIf(west);
      model.Add(x[c] >= x[rightRoom]).OnlyEnforceIf(east);
      model.Add(y[c] + heightCells[c] <= y[topRoom] + heightCells[topRoom]).OnlyEnforceIf(north);
      model.Add(y[c] >= y[bottomRoom]).OnlyEnforceIf(south);
      model.AddBoolOr(new ILiteral[] { west, east, north, south }).OnlyEnforceIf(satisfied);
    }
  }

  /// <summary>
  ///   When <paramref name="exact" />, the straight join from <paramref name="a" /> to <paramref name="b" /> (going
  ///   <paramref name="wx" />, <paramref name="wy" />) has no room on it: every other room nearby is wholly to one side
  ///   of the line, or no further along it than the rooms at its ends.
  /// </summary>
  private static void KeepOffTheLine(
    CpModel model, BoolVar exact, int a, int b, int wx, int wy, IntVar[] x, IntVar[] y, long[] widthCells,
    long[] heightCells, long[] cellX, long[] cellY, int j)
  {
    var n = x.Length;
    // the room the join starts from in its direction, and the one it ends at
    var (first, last) = (wx < 0 || wy < 0) ? (b, a) : (a, b);
    var left = Math.Min(cellX[a], cellX[b]) - LineReach;
    var right = Math.Max(cellX[a], cellX[b]) + LineReach;
    var top = Math.Min(cellY[a], cellY[b]) - LineReach;
    var bottom = Math.Max(cellY[a], cellY[b]) + LineReach;
    for (var c = 0; c < n; c++)
    {
      if (c == a || c == b || cellX[c] < left || cellX[c] > right || cellY[c] < top || cellY[c] > bottom) continue;
      var before = model.NewBoolVar($"lb{j}_{c}");
      var after = model.NewBoolVar($"la{j}_{c}");
      var oneSide = model.NewBoolVar($"l1{j}_{c}");
      var otherSide = model.NewBoolVar($"l2{j}_{c}");
      if (wx == 0)
      {
        // upright: the line is down the middle of a's column, between first's bottom and last's top
        var line = 2 * x[a] + widthCells[a];
        model.Add(2 * x[c] + 2 * widthCells[c] <= line).OnlyEnforceIf(oneSide);
        model.Add(2 * x[c] >= line).OnlyEnforceIf(otherSide);
        model.Add(y[c] + heightCells[c] <= y[first] + heightCells[first]).OnlyEnforceIf(before);
        model.Add(y[c] >= y[last]).OnlyEnforceIf(after);
      }
      else
      {
        // level: the line is along the middle of a's row, between first's right and last's left
        var line = 2 * y[a] + heightCells[a];
        model.Add(2 * y[c] + 2 * heightCells[c] <= line).OnlyEnforceIf(oneSide);
        model.Add(2 * y[c] >= line).OnlyEnforceIf(otherSide);
        model.Add(x[c] + widthCells[c] <= x[first] + widthCells[first]).OnlyEnforceIf(before);
        model.Add(x[c] >= x[last]).OnlyEnforceIf(after);
      }

      model.AddBoolOr(new ILiteral[] { oneSide, otherSide, before, after }).OnlyEnforceIf(exact);
    }
  }

  /// <summary>Bonus for two rooms near each other sharing a column (or a row), joined or not.</summary>
  private const int AlignWeight = 4;

  /// <summary>How many squares apart (across a column, or down a row) rooms count as near, for lining up.</summary>
  private const int AlignReach = 3;

  /// <summary>
  ///   Lays out <paramref name="rooms" /> (others stay put) with the solver, then polishes with the step-by-step tidy.
  ///   Returns how many rooms moved, or -1 if the solver found no layout in time (nothing is changed then).
  /// </summary>
  /// <param name="deterministic">
  ///   Always give the same layout for the same map (one search thread, and a limit on the work done rather than on
  ///   the time taken), as automap needs: the same transcript must make the same map.
  /// </param>
  public static int SolverTidy(
    IList<Element> elements, IEnumerable<Room> rooms, int timeLimitMs = 5000, bool deterministic = false)
  {
    if (elements == null || rooms == null) return 0;
    var map = new LayoutMap(elements);
    var result = Solve(map, rooms.Where(map.Contains).ToList(), timeLimitMs, CancellationToken.None, deterministic);
    return result < 0 ? -1 : TidyPlan.Apply(map.Moves());
  }

  /// <summary>
  ///   The solver tidy on <paramref name="map" />'s copy of the layout; the rooms themselves aren't moved. Returns 1
  ///   when it changed the layout, 0 when it kept it (nothing better, or cancelled) and -1 when it found no layout.
  /// </summary>
  private static int Solve(
    LayoutMap map, List<Room> rooms, int timeLimitMs, CancellationToken token, bool deterministic = false)
  {
    var movable = rooms.Select(map.IndexOf).Where(i => i >= 0).ToHashSet();
    var n = map.Rooms.Count;
    if (movable.Count == 0 || n < 2) return 0;
    var commit = map.Commit;
    map.Commit = false;
    try
    {
      return Solve(map, rooms, movable, n, timeLimitMs, token, deterministic);
    }
    finally
    {
      map.Commit = commit;
    }
  }

  private static int Solve(
    LayoutMap map, List<Room> rooms, HashSet<int> movable, int n, int timeLimitMs, CancellationToken token,
    bool deterministic)
  {
    var stepX = map.StepX;
    var stepY = map.StepY;
    var left = new double[n];
    var top = new double[n];
    for (var i = 0; i < n; i++)
    {
      left[i] = map.CentreX(i) - map.HalfWidth(i);
      top[i] = map.CentreY(i) - map.HalfHeight(i);
    }

    var originX = left.Min();
    var originY = top.Min();
    var cellX = new long[n];
    var cellY = new long[n];
    var widthCells = new long[n];
    var heightCells = new long[n];
    for (var i = 0; i < n; i++)
    {
      cellX[i] = (long)Math.Round((left[i] - originX) / stepX);
      cellY[i] = (long)Math.Round((top[i] - originY) / stepY);
      widthCells[i] = Math.Max(1, (long)Math.Ceiling((map.HalfWidth(i) * 2 + map.Gap) / stepX - 0.01));
      heightCells[i] = Math.Max(1, (long)Math.Ceiling((map.HalfHeight(i) * 2 + map.Gap) / stepY - 0.01));
    }

    // room to spread out around the current map
    var reach = Math.Min(n + 2, 30);
    var minX = cellX.Min() - reach;
    var maxX = cellX.Max() + reach;
    var minY = cellY.Min() - reach;
    var maxY = cellY.Max() + reach;

    var naturalBefore = map.NaturalCount();
    var before = map.Snapshot();

    var model = new CpModel();
    var x = new IntVar[n];
    var y = new IntVar[n];
    var noOverlap = model.AddNoOverlap2D();
    var objective = new List<LinearExpr>();
    for (var i = 0; i < n; i++)
    {
      if (movable.Contains(i))
      {
        x[i] = model.NewIntVar(minX, maxX, $"x{i}");
        y[i] = model.NewIntVar(minY, maxY, $"y{i}");
        model.AddHint(x[i], cellX[i]);
        model.AddHint(y[i], cellY[i]);
        // stay near where it was
        var dx = model.NewIntVar(0, maxX - minX, $"mx{i}");
        var dy = model.NewIntVar(0, maxY - minY, $"my{i}");
        model.AddAbsEquality(dx, x[i] - cellX[i]);
        model.AddAbsEquality(dy, y[i] - cellY[i]);
        objective.Add(-MoveWeight * (dx + dy));
      }
      else
      {
        x[i] = model.NewConstant(cellX[i]);
        y[i] = model.NewConstant(cellY[i]);
      }

      noOverlap.AddRectangle(
        model.NewFixedSizeIntervalVar(x[i], widthCells[i], $"ix{i}"),
        model.NewFixedSizeIntervalVar(y[i], heightCells[i], $"iy{i}"));
    }

    var j = 0;
    foreach (var join in map.Joins)
    {
      j++;
      int a = join.A, b = join.B;
      if (a == b || (!movable.Contains(a) && !movable.Contains(b)) || join.Pull <= 0) continue;

      // between the rooms' centres, in half squares so a room two squares tall has a whole-number centre
      var dx = 2 * (x[b] - x[a]) + (widthCells[b] - widthCells[a]);
      var dy = 2 * (y[b] - y[a]) + (heightCells[b] - heightCells[a]);

      // keep joins short (up, down, in and out included)
      var lx = model.NewIntVar(0, 2 * (maxX - minX + n), $"lx{j}");
      var ly = model.NewIntVar(0, 2 * (maxY - minY + n), $"ly{j}");
      model.AddAbsEquality(lx, dx);
      model.AddAbsEquality(ly, dy);
      objective.Add(-LengthWeight * (lx + ly));

      var wx = Math.Sign((int)Math.Round(join.WantX));
      var wy = Math.Sign((int)Math.Round(join.WantY));
      if (join.Pull < 1 || (wx == 0 && wy == 0)) continue;

      // within 22.5 degrees of the direction: tan 22.5 = 0.414, tan 67.5 = 2.414
      var satisfied = model.NewBoolVar($"s{j}");
      objective.Add(SatisfiedWeight * satisfied);
      if (wy == 0)
      {
        model.Add(wx * dx >= 1).OnlyEnforceIf(satisfied);
        model.Add(1000 * dy <= 414 * wx * dx).OnlyEnforceIf(satisfied);
        model.Add(-1000 * dy <= 414 * wx * dx).OnlyEnforceIf(satisfied);
      }
      else if (wx == 0)
      {
        model.Add(wy * dy >= 1).OnlyEnforceIf(satisfied);
        model.Add(1000 * dx <= 414 * wy * dy).OnlyEnforceIf(satisfied);
        model.Add(-1000 * dx <= 414 * wy * dy).OnlyEnforceIf(satisfied);
      }
      else
      {
        model.Add(wx * dx >= 1).OnlyEnforceIf(satisfied);
        model.Add(wy * dy >= 1).OnlyEnforceIf(satisfied);
        model.Add(1000 * wy * dy >= 414 * wx * dx).OnlyEnforceIf(satisfied);
        model.Add(1000 * wy * dy <= 2414 * wx * dx).OnlyEnforceIf(satisfied);
      }

      // a slanting join (diagonal, or a bend) runs across no room: while it points its way, no room sits within
      // the box its two rooms span, where the line runs
      if (wx != 0 && wy != 0) KeepOutOfTheBox(model, satisfied, a, b, wx, wy, x, y, widthCells, heightCells, cellX, cellY, j);

      // a bend is drawn slanting: it needn't be exactly diagonal (straight joins come first)
      if (join.Bent) continue;

      // exactly that way: level, upright, or as many squares across as down
      var exact = model.NewBoolVar($"e{j}");
      objective.Add(ExactWeight * exact);
      model.AddImplication(exact, satisfied);
      if (wy == 0) model.Add(dy == 0).OnlyEnforceIf(exact);
      else if (wx == 0) model.Add(dx == 0).OnlyEnforceIf(exact);
      else model.Add(wx * dx == wy * dy).OnlyEnforceIf(exact);

      // a straight join (exact or not) runs across no room: each room near it is to one side of the line, or
      // beyond its ends
      if (wx == 0 || wy == 0)
      {
        var straight = model.NewBoolVar($"st{j}");
        var across = wx == 0 ? dx : dy;
        model.Add(across == 0).OnlyEnforceIf(straight);
        model.Add(across != 0).OnlyEnforceIf(straight.Not());
        model.AddImplication(exact, straight);
        KeepOffTheLine(model, straight, a, b, wx, wy, x, y, widthCells, heightCells, cellX, cellY, j);
      }
    }

    // rooms near each other line up in rows and columns, joined or not, so the map reads as a grid
    for (var a = 0; a < n; a++)
      for (var b = a + 1; b < n; b++)
      {
        if (!movable.Contains(a) && !movable.Contains(b)) continue;
        var apartX = Math.Abs(cellX[a] - cellX[b]);
        var apartY = Math.Abs(cellY[a] - cellY[b]);
        if (apartX <= AlignReach && apartY <= AlignReach * 2)
        {
          var column = model.NewBoolVar($"c{a}_{b}");
          model.Add(2 * x[a] + widthCells[a] == 2 * x[b] + widthCells[b]).OnlyEnforceIf(column);
          objective.Add(AlignWeight * column);
        }

        if (apartY <= AlignReach && apartX <= AlignReach * 2)
        {
          var row = model.NewBoolVar($"r{a}_{b}");
          model.Add(2 * y[a] + heightCells[a] == 2 * y[b] + heightCells[b]).OnlyEnforceIf(row);
          objective.Add(AlignWeight * row);
        }
      }

    model.Maximize(LinearExpr.Sum(objective));
    var solver = new CpSolver
    {
      StringParameters = deterministic
        ? $"max_deterministic_time:{timeLimitMs / 1000.0:0.0###}, num_workers:1, random_seed:1"
        : $"max_time_in_seconds:{timeLimitMs / 1000.0:0.0###}, num_workers:8"
    };
    CpSolverStatus status;
    using (token.Register(() => solver.StopSearch()))
      status = solver.Solve(model);
    if (token.IsCancellationRequested) return 0;
    if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible) return -1;

    foreach (var i in movable)
      map.PlaceTopLeft(i, originX + solver.Value(x[i]) * stepX, originY + solver.Value(y[i]) * stepY);

    var solved = map.NaturalCount();
    var solvedAt = map.Snapshot();
    // polish: rooms off joins, fewer crossings, lined up; kept only if it doesn't cost a join pointing its way
    map.Rebase();
    // (the polish stops when nothing improves; deterministically, it isn't cut short by the clock)
    map.Tidy(rooms, deterministic ? 5000 : Math.Max(500, timeLimitMs / 5), null, null, StayPut / 20, token);
    if (token.IsCancellationRequested) return 0;
    if (map.NaturalCount() < solved) map.Restore(solvedAt);

    // never worse than it was
    if (map.NaturalCount() < naturalBefore)
    {
      map.Restore(before);
      return 0;
    }

    return 1;
  }
}
