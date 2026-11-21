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
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 20 Nov 2026: Previous Move (F9) did nothing on a transcript automap started at the end of (carrying on a
///   game still running): the moves before the end weren't noted, so there was nothing to go back to.
/// </summary>
[TestFixture]
[Category("Unit")]
public class BackFromTheEndTests : IsolatedProjectTests
{
  private Trizbort.Automap.Automap _engine;

  private async Task WaitFor(Func<bool> condition, string what)
  {
    var clock = Stopwatch.StartNew();
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 10000) Assert.Fail("Timed out waiting for automap: " + what + "; status " + _engine?.Status + "; next " +
                                                    _engine?.NextCommand + "; screen " +
                                                    string.Join(" | ", _engine?.ScreenLines() ?? Array.Empty<string>()));
      await Task.Delay(10);
    }
  }

  [Test]
  public async Task StartedAtTheEnd_PreviousMove_GoesBackIntoTheTranscript_AndFindsTheRoom()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall. A door leads north.", "",
      ">north", "Kitchen", "A small kitchen. The hall is south.", "",
      ">east", "Pantry", "A pantry. The kitchen is west.", "",
      ">west", "Kitchen", "",
      ">"
    });
    foreach (var name in new[] { "Hall", "Kitchen", "Pantry" })
      Project.Current.Elements.Add(new Room(Project.Current) { Name = name });
    var engine = _engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.ContinueTranscript = true;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      // the transcript so far is on the game monitor
      await WaitFor(() => engine.ScreenLines().Count == 16 || run.IsCompleted, "the transcript so far");
      TestContext.Out.WriteLine(engine.Status);
      TestContext.Out.WriteLine(string.Join(" | ", engine.ScreenLines()));
      run.IsCompleted.ShouldBeFalse(engine.Status);
      engine.ScreenLines().Count.ShouldBe(16, string.Join(" | ", engine.ScreenLines()));
      engine.CanGoBack.ShouldBeTrue();

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "west", "back to west");
      engine.ScreenLines().Last().ShouldBe(">west");
      engine.CurrentRoom?.Name.ShouldBe("Pantry");

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "east", "back to east");
      engine.CurrentRoom?.Name.ShouldBe("Kitchen");
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task PlayedLive_GoingBack_FindsTheRightPlaceInTheTranscript()
  {
    // Lee, 21 Nov 2026: playing live, then Previous Move twice and Next Move, automap said "Move 75 of 75" at the
    // Slab Room and wouldn't go on. The game writes its prompt (">") and then, when the player types it, the command:
    // one line of the transcript that automap read as two, so each move live put it a line further on.
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\nHall\n>");
    var engine = _engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count == 3, "the first prompt");
      File.AppendAllText(path, "north\nKitchen\n>");
      await WaitFor(() => engine.ScreenLines().Count == 6, "north");
      File.AppendAllText(path, "east\nPantry\n>");
      await WaitFor(() => engine.ScreenLines().Count == 9, "east");
      File.AppendAllText(path, "west\nKitchen\n>");
      await WaitFor(() => engine.ScreenLines().Count == 12, "west");
      engine.MoveCount().ShouldBe((4, 4));

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "west", "back to west");
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "east", "back to east");

      // two moves played (look, north), paused before east, four in the transcript
      engine.MoveCount().ShouldBe((2, 4));
      engine.HasNextMove.ShouldBeTrue();
      engine.MovesAhead().ShouldBe(2);

      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "west", "on to west");
      engine.MoveCount().ShouldBe((3, 4));
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }
}
