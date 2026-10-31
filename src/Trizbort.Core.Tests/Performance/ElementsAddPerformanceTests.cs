using System.Diagnostics;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

[TestFixture]
public class ElementsAddPerformanceTests
{
  [Test]
  [Category("Performance")]
  public void BuildingLargeGridViaElementsAdd_StaysFast()
  {
    const int side = 45; // 2,025 rooms
    var project = new Project();
    var grid = new Room[side, side];
    var stopwatch = Stopwatch.StartNew();

    for (var y = 0; y < side; y++)
      for (var x = 0; x < side; x++)
      {
        var room = new Room(project) { Name = $"Room {x},{y}", Position = new Vector(x * 160, y * 120) };
        project.Elements.Add(room);
        grid[x, y] = room;
      }

    for (var y = 0; y < side; y++)
      for (var x = 0; x < side; x++)
      {
        if (x + 1 < side)
          project.Elements.Add(new Connection(project,
            new Vertex(grid[x, y].PortAt(CompassPoint.East)),
            new Vertex(grid[x + 1, y].PortAt(CompassPoint.West))));
        if (y + 1 < side)
          project.Elements.Add(new Connection(project,
            new Vertex(grid[x, y].PortAt(CompassPoint.South)),
            new Vertex(grid[x, y + 1].PortAt(CompassPoint.North))));
      }

    stopwatch.Stop();
    project.Elements.Count.ShouldBe(side * side + 2 * side * (side - 1));
    // Guards against the O(n^3) id allocation fixed in I-009, which made this take minutes. Well under a second on a
    // desktop PC; GitHub runners with code coverage switched on take nearly 3 s, so allow 4x the 2 s target.
    TestContext.Out.WriteLine("Grid build: " + stopwatch.Elapsed.TotalMilliseconds + " ms");
    stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(2.0 * 4);
  }
}
