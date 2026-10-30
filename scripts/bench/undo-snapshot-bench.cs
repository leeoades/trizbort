#:project ../../src/Trizbort.Core/Trizbort.Core.csproj
// Measures the cost of whole-map snapshot undo: serialise + deserialise a map via the real .trizbort engine.
using System.Diagnostics;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

var tmp = Path.Combine(Path.GetTempPath(), "trizbort-undo-bench.trizbort");

Project Synthetic(int rooms)
{
  var p = new Project(); Project.Current = p;
  var side = (int)Math.Ceiling(Math.Sqrt(rooms));
  var grid = new Room[side, side]; var made = 0;
  for (var y = 0; y < side && made < rooms; y++)
  for (var x = 0; x < side && made < rooms; x++, made++)
  {
    var r = new Room(p) { Name = $"Room {made}", Position = new Vector(x * 160, y * 120) };
    r.ClearDescriptions(); r.AddDescription($"A description for room {made}, long enough to look like a real one.");
    r.Objects = "lamp\nsword\nkey";
    p.Elements.Add(r); grid[x, y] = r;
  }
  for (var y = 0; y < side; y++)
  for (var x = 0; x < side; x++)
  {
    var r = grid[x, y]; if (r == null) continue;
    if (x + 1 < side && grid[x + 1, y] != null)
      p.Elements.Add(new Connection(p, new Vertex(r.PortAt(CompassPoint.East)), new Vertex(grid[x + 1, y].PortAt(CompassPoint.West))));
    if (y + 1 < side && grid[x, y + 1] != null)
      p.Elements.Add(new Connection(p, new Vertex(r.PortAt(CompassPoint.South)), new Vertex(grid[x, y + 1].PortAt(CompassPoint.North))));
  }
  return p;
}

void Measure(string label, Project p)
{
  Console.WriteLine($"[{DateTime.Now:T}] measuring {label}");
  Project.Current = p;
  new LegacyMapFileEngine(p).Save(tmp);           // warm up
  const int n = 5; var save = Stopwatch.StartNew();
  for (var i = 0; i < n; i++) new LegacyMapFileEngine(p).Save(tmp);
  save.Stop();
  var load = Stopwatch.StartNew();
  for (var i = 0; i < n; i++) { var q = new Project(); Project.Current = q; new LegacyMapFileEngine(q).Load(tmp); }
  load.Stop(); Project.Current = p;
  var rooms = p.Elements.OfType<Room>().Count(); var conns = p.Elements.OfType<Connection>().Count();
  Console.WriteLine($"{label,-28} rooms={rooms,5} conns={conns,5} size={new FileInfo(tmp).Length / 1024.0,8:F1} KB  save={save.Elapsed.TotalMilliseconds / n,7:F2} ms  load={load.Elapsed.TotalMilliseconds / n,7:F2} ms");
}

Console.WriteLine($"[{DateTime.Now:T}] start");
var sample = new Project(); Project.Current = sample;
new LegacyMapFileEngine(sample).Load(Path.GetFullPath("../../samples/space-quest-lost-chapter-surface.trizbort"));
Measure("sample: space-quest", sample);
foreach (var n in new[] { 250, 1000 }) { var t = Stopwatch.StartNew(); var p = Synthetic(n); Console.WriteLine($"[{DateTime.Now:T}] built {n} rooms in {t.ElapsedMilliseconds} ms"); Measure($"synthetic {n}", p); }
File.Delete(tmp);
