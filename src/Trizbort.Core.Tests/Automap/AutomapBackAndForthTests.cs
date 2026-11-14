using System;
using System.Collections.Generic;
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

/// <summary>Live, at the end of the transcript: going back several moves and forward again, as a player would.</summary>
[TestFixture]
[Category("Unit")]
public class AutomapBackAndForthTests : IsolatedProjectTests
{
  private static string[] Game { get; } =
  {
    ">look", "Hall", "A grand hall. A door leads north.", "",
    ">north", "Kitchen", "A small kitchen. The hall is south.", "",
    ">east", "Pantry", "A pantry. The kitchen is west.", "",
    ">west", "Kitchen", "",
    ">"
  };

  private static List<string> Rooms() =>
    Project.Current.Elements.OfType<Room>().Select(room => room.Name).OrderBy(name => name).ToList();

  private static async Task WaitFor(Func<bool> condition, string what)
  {
    var clock = Stopwatch.StartNew();
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 5000) Assert.Fail("Timed out waiting for automap: " + what);
      await Task.Delay(10);
    }
  }

  private static async Task Settle() => await Task.Delay(150);

  [Test]
  public async Task BackSeveralMoves_ThenForwardOneAtATime_EndsWhereItWas_AndCarriesOnLive()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, Game);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count == Game.Length, "the end");
      await Settle();
      var atTheEnd = engine.ScreenLines().ToList();

      // back three moves, one press at a time
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "west", "back to west");
      await Settle();
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "east", "back to east");
      await Settle();
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "north", "back to north");
      engine.ScreenLines().Last().ShouldBe(">north");
      engine.CurrentRoom?.Name.ShouldBe("Hall");

      // and forward one at a time
      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "east", "forward to east");
      engine.CurrentRoom?.Name.ShouldBe("Kitchen");
      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "west", "forward to west");
      engine.CurrentRoom?.Name.ShouldBe("Pantry");
      engine.PlayNextMove();
      await WaitFor(() => engine.ScreenLines().Count == Game.Length && engine.NextCommand == null, "forward to the end");
      await Settle();
      engine.ScreenLines().ShouldBe(atTheEnd);
      engine.CurrentRoom?.Name.ShouldBe("Kitchen");
      Rooms().ShouldBe(new[] { "Hall", "Kitchen", "Pantry" });

      // back one and forward one again
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "west", "back to west again");
      engine.PlayNextMove();
      await WaitFor(() => engine.ScreenLines().Count == Game.Length && engine.NextCommand == null, "the end again");
      await Settle();
      engine.ScreenLines().ShouldBe(atTheEnd);

      // the game goes on: the next move is mapped
      File.AppendAllText(path, "south\nHall\n\n>");
      await WaitFor(() => engine.NextCommand == "south" || engine.CurrentRoom?.Name == "Hall", "the game going on");
      if (engine.NextCommand == "south") engine.PlayToEnd();
      await WaitFor(() => engine.CurrentRoom?.Name == "Hall", "south mapped");
      Rooms().ShouldBe(new[] { "Hall", "Kitchen", "Pantry" });
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task BackTwo_ForwardOne_BackOne_ForwardToTheEnd()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, Game);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count == Game.Length, "the end");
      await Settle();
      var atTheEnd = engine.ScreenLines().ToList();

      engine.GoBack();
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "east", "back two");
      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "west", "forward one");
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "east", "back one");
      engine.ScreenLines().Last().ShouldBe(">east");
      engine.PlayToEnd();
      await WaitFor(() => engine.ScreenLines().Count == Game.Length && engine.NextCommand == null, "to the end");
      await Settle();
      engine.ScreenLines().ShouldBe(atTheEnd);
      engine.CurrentRoom?.Name.ShouldBe("Kitchen");
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }
}
