using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Navigation;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 24 Nov 2026: xyzzy from the debris room to the building, then xyzzy back. The join went two-way, rightly,
///   but only the first way was written down, so directions from the building didn't know xyzzy. A command join is
///   one-way until the command (or another way) is seen from the other end, and then that's recorded.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CommandBackTests : IsolatedProjectTests
{
  private static async Task<Trizbort.Automap.Automap> Play(params string[] transcript)
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, transcript);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AutoTidy = false;
    _ = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
    var clock = Stopwatch.StartNew();
    while (engine.ScreenLines().Count < transcript.Length && clock.ElapsedMilliseconds < 5000) await Task.Delay(10);
    await Task.Delay(200);
    return engine;
  }

  private static readonly string[] _thereByMagic =
  {
    ">look", "Inside Building", "You are inside a building.", "",
    ">east", "End of Road", "You are at the end of a road.", "",
    ">down", "In Debris Room", "You are in a debris room.", "",
    ">xyzzy", "Inside Building", "You are inside a building.", ""
  };

  private static Connection Magic() =>
    Project.Current.Elements.OfType<Connection>().Single(c => (c.AutomapWays ?? "").Contains("xyzzy"));

  private static Room Called(string name) => Project.Current.Elements.OfType<Room>().Single(r => r.Name == name);

  [Test]
  public async Task SeenOneWay_ItIsOneWay_AndDirectionsDontTakeItBack()
  {
    var engine = await Play(_thereByMagic.Append(">").ToArray());
    try
    {
      Magic().Flow.ShouldBe(ConnectionFlow.OneWay);
      // "pull first peg" might only go down: nothing assumed back
      RouteFinder.Find(Project.Current.Elements, Called("Inside Building"), Called("In Debris Room"))!
        .Select(step => step.Command).ShouldBe(new[] { "east", "down" });
    }
    finally
    {
      engine.Stop();
    }
  }

  [Test]
  public async Task SeenBothWays_ItIsTwoWay_AndDirectionsTakeItBack()
  {
    var engine = await Play(_thereByMagic.Concat(new[] { ">xyzzy", "In Debris Room", "You are in a debris room.", "", ">" }).ToArray());
    try
    {
      var magic = Magic();
      magic.Flow.ShouldBe(ConnectionFlow.TwoWay);
      JoinWays.Of(magic).Count(way => way.IsCommand && way.Text == "xyzzy").ShouldBe(2);
      // written once, in the middle, as before
      magic.MidText.ShouldBe("xyzzy");
      magic.StartText.ShouldBeEmpty();
      magic.EndText.ShouldBeEmpty();

      var route = RouteFinder.Find(Project.Current.Elements, Called("Inside Building"), Called("In Debris Room"))!;
      route.Select(step => step.Command).ShouldBe(new[] { "xyzzy" });
      route.Single().Assumed.ShouldBeFalse();
    }
    finally
    {
      engine.Stop();
    }
  }
}
