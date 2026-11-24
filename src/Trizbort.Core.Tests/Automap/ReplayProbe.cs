using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

[Explicit]
public class ReplayProbe : IsolatedProjectTests
{
  [Test]
  public async Task Replay()
  {
    var path = Environment.GetEnvironmentVariable("TRIZBORT_REPLAY")!;
    var lines = File.ReadAllLines(path).Length;
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    var run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
    var clock = Stopwatch.StartNew();
    var last = -1;
    while (clock.ElapsedMilliseconds < 60000)
    {
      await Task.Delay(1500);
      var now = engine.ScreenLines().Count;
      if (now == last) break;
      last = now;
    }
    TestContext.Out.WriteLine($"PROBE lines={last} of {lines} rooms={Project.Current.Elements.OfType<Room>().Count()} joins={Project.Current.Elements.OfType<Connection>().Count()} startFromEnd={settings.ContinueTranscript}");
    foreach (var c in Project.Current.Elements.OfType<Connection>().Where(c => !string.IsNullOrEmpty(c.AutomapCommand) || (c.AutomapWays ?? "").Contains(",c,")))
      TestContext.Out.WriteLine($"PROBE {c.GetSourceRoom()?.Name} -> {c.GetTargetRoom()?.Name}: flow={c.Flow} style={c.Style} ways={c.AutomapWays} mid={c.MidText}");
    engine.Stop();
    await run;
  }
}
