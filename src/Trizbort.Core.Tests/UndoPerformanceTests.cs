using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>
///   Performance gate from decisions D1 point 3: on a 1,000-room map, recording a step must stay under 50 ms and
///   restoring under 250 ms (median of 5).
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Performance")]
public class UndoPerformanceTests
{
  private const int RoomTotal = 1000;
  private const int Runs = 5;

  private Project _previousProject;
  private Project _project;
  private EditorSession _session;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = BuildSyntheticMap(RoomTotal);
    Project.Current = _project;
    _session = new EditorSession();
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  // Same generator as scripts/bench/undo-snapshot-bench.cs.
  private static Project BuildSyntheticMap(int rooms)
  {
    var project = new Project();
    Project.Current = project;
    var side = (int)Math.Ceiling(Math.Sqrt(rooms));
    var grid = new Room[side, side];
    var made = 0;
    for (var y = 0; y < side && made < rooms; y++)
      for (var x = 0; x < side && made < rooms; x++, made++)
      {
        var room = new Room(project) { Name = $"Room {made}", Position = new Vector(x * 160, y * 120) };
        room.ClearDescriptions();
        room.AddDescription($"A description for room {made}, long enough to look like a real one.");
        room.Objects = "lamp\nsword\nkey";
        project.Elements.Add(room);
        grid[x, y] = room;
      }

    for (var y = 0; y < side; y++)
      for (var x = 0; x < side; x++)
      {
        var room = grid[x, y];
        if (room == null) continue;
        if (x + 1 < side && grid[x + 1, y] != null)
          project.Elements.Add(new Connection(project,
            new Vertex(room.PortAt(CompassPoint.East)),
            new Vertex(grid[x + 1, y].PortAt(CompassPoint.West))));
        if (y + 1 < side && grid[x, y + 1] != null)
          project.Elements.Add(new Connection(project,
            new Vertex(room.PortAt(CompassPoint.South)),
            new Vertex(grid[x, y + 1].PortAt(CompassPoint.North))));
      }

    return project;
  }

  private static double Median(double[] values)
  {
    var sorted = values.OrderBy(value => value).ToArray();
    return sorted[sorted.Length / 2];
  }

  private void RecordMove(int i)
  {
    using (_session.BeginTransaction("Move"))
    {
      Project.Current.Elements.OfType<Room>().First().Position = new Vector(10 + i * 20, 10);
    }
  }

  [Test]
  public void SyntheticMap_HasExpectedSize()
  {
    RoomCount().ShouldBe(RoomTotal);
    Project.Current.Elements.OfType<Connection>().Count().ShouldBeGreaterThan(1800);
  }

  [Test]
  public void RecordStep_Under50ms()
  {
    RecordMove(0); // warm-up (JIT, caches)
    var times = new double[Runs];
    for (var i = 0; i < Runs; i++)
    {
      var stopwatch = Stopwatch.StartNew();
      RecordMove(i + 1);
      stopwatch.Stop();
      times[i] = stopwatch.Elapsed.TotalMilliseconds;
    }

    TestContext.Out.WriteLine("RecordStep median: " + Median(times) + " ms");
    Median(times).ShouldBeLessThan(50);
  }

  [Test]
  public void RestoreStep_Under250ms()
  {
    for (var i = 0; i <= Runs; i++) RecordMove(i);
    _session.PerformUndo(); // warm-up
    var times = new double[Runs];
    for (var i = 0; i < Runs; i++)
    {
      var stopwatch = Stopwatch.StartNew();
      _session.PerformUndo();
      stopwatch.Stop();
      times[i] = stopwatch.Elapsed.TotalMilliseconds;
    }

    TestContext.Out.WriteLine("RestoreStep median: " + Median(times) + " ms");
    Median(times).ShouldBeLessThan(250);
    RoomCount().ShouldBe(RoomTotal);
  }

  [Test]
  public void Snapshot_1000Rooms_GzipSmallerThan20PercentOfXml()
  {
    var memento = SnapshotMemento.Capture(_session);

    memento.XmlLength.ShouldBeGreaterThan(0);
    ((double)memento.CompressedLength).ShouldBeLessThan(memento.XmlLength * 0.2);
    memento.SizeBytes.ShouldBe(memento.CompressedLength);
  }

  private static int RoomCount()
  {
    return Project.Current.Elements.OfType<Room>().Count();
  }
}
