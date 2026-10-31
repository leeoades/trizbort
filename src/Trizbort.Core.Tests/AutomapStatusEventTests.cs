using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Parser = Trizbort.Automap.Automap;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 126: the engine raises StatusChanged for every status string instead of being polled.</summary>
[TestFixture]
[Category("Integration")]
public class AutomapStatusEventTests : IsolatedProjectTests
{
  private AutomapSettings SettingsFor(string text)
  {
    var settings = AutomapSettings.Default;
    settings.FileName = Files.File("transcript.txt");
    File.WriteAllText(settings.FileName, text);
    settings.VerboseTranscript = true;
    settings.GuessExits = false;
    return settings;
  }

  [Test]
  public async Task SampleRun_RaisesEachStatus()
  {
    var parser = new Parser();
    var statuses = new List<string>();
    parser.StatusChanged += statuses.Add;

    await parser.StartCl(
      new AutomapCanvas(null, parser),
      SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\n\nSecond Room\nAnother room.\n"));

    statuses.ShouldBe(new[]
    {
      "Automapping has started.",
      "Automapping is processing the transcript.",
      "Automapping has completed."
    });
    parser.Status.ShouldBe(statuses[statuses.Count - 1]);
  }

  [Test]
  public async Task Stop_RaisesNotRunning()
  {
    var parser = new Parser();
    var statuses = new List<string>();
    parser.StatusChanged += statuses.Add;

    var run = parser.Start(new AutomapCanvas(null, parser), SettingsFor(string.Empty));
    parser.Stop();
    await run.WaitAsync(System.TimeSpan.FromSeconds(5));

    statuses.ShouldContain("Automapping has started.");
    statuses.ShouldContain("Automap is not running.");
    statuses[statuses.Count - 1].ShouldBe("Automap is not running.");
  }

  [Test]
  public async Task WaitingForMoreText_RaisesStatus()
  {
    var parser = new Parser();
    var statuses = new List<string>();
    parser.StatusChanged += statuses.Add;

    var run = parser.Start(new AutomapCanvas(null, parser), SettingsFor(string.Empty));
    await Task.Delay(100);
    parser.Stop();
    await run.WaitAsync(System.TimeSpan.FromSeconds(5));

    statuses.ShouldContain("Automapping is waiting for more text.");
  }

  [Test]
  public async Task SingleStepWait_RaisesStepStatus()
  {
    var parser = new Parser();
    var statuses = new List<string>();
    parser.StatusChanged += statuses.Add;
    var settings = SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>e\n");
    settings.SingleStep = true;

    var run = parser.Start(new AutomapCanvas(null, parser), settings);
    var deadline = System.DateTime.UtcNow.AddSeconds(5);
    while (!statuses.Contains("Automapping is waiting for you to step through it (with F11.)") && System.DateTime.UtcNow < deadline)
      await Task.Delay(20);
    parser.SingleStep.ShouldBeTrue();
    parser.Stop();
    await run.WaitAsync(System.TimeSpan.FromSeconds(5));

    statuses.ShouldContain("Automapping is waiting for you to step through it (with F11.)");
  }

  [Test]
  public async Task Failure_RaisesHalted()
  {
    var parser = new Parser();
    var statuses = new List<string>();
    parser.StatusChanged += statuses.Add;

    await parser.Start(new AutomapCanvas(null, parser), SettingsFor("x") with { FileName = Files.File("missing.txt") });

    statuses.ShouldContain("Automapping halted.");
  }
}
