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

  /// <summary>
  ///   Lays out <paramref name="rooms" /> (others stay put) with the solver, then polishes with the step-by-step tidy.
  ///   Returns how many rooms moved, or -1 if the solver found no layout in time (nothing is changed then).
  /// </summary>
  public static int SolverTidy(IList<Element> elements, IEnumerable<Room> rooms, int timeLimitMs = 5000)
  {
    if (elements == null || rooms == null) return 0;
    var map = new LayoutMap(elements);
    var result = Solve(map, rooms.Where(map.Contains).ToList(), timeLimitMs, CancellationToken.None);
    return result < 0 ? -1 : TidyPlan.Apply(map.Moves());
  }

  /// <summary>
  ///   The solver tidy on <paramref name="map" />'s copy of the layout; the rooms themselves aren't moved. Returns 1
  ///   when it changed the layout, 0 when it kept it (nothing better, or cancelled) and -1 when it found no layout.
  /// </summary>
  private static int Solve(LayoutMap map, List<Room> rooms, int timeLimitMs, CancellationToken token)
  {
    var movable = rooms.Select(map.IndexOf).Where(i => i >= 0).ToHashSet();
    var n = map.Rooms.Count;
    if (movable.Count == 0 || n < 2) return 0;
    var commit = map.Commit;
    map.Commit = false;
    try
    {
      return Solve(map, rooms, movable, n, timeLimitMs, token);
    }
    finally
    {
      map.Commit = commit;
    }
  }

  private static int Solve(
    LayoutMap map, List<Room> rooms, HashSet<int> movable, int n, int timeLimitMs, CancellationToken token)
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

      // exactly that way: level, upright, or as many squares across as down
      var exact = model.NewBoolVar($"e{j}");
      objective.Add(ExactWeight * exact);
      model.AddImplication(exact, satisfied);
      if (wy == 0) model.Add(dy == 0).OnlyEnforceIf(exact);
      else if (wx == 0) model.Add(dx == 0).OnlyEnforceIf(exact);
      else model.Add(wx * dx == wy * dy).OnlyEnforceIf(exact);
    }

    model.Maximize(LinearExpr.Sum(objective));
    var solver = new CpSolver
    {
      StringParameters = $"max_time_in_seconds:{timeLimitMs / 1000.0:0.0###}, num_workers:8"
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
    map.Tidy(rooms, Math.Max(500, timeLimitMs / 5), null, null, StayPut / 20, token);
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
