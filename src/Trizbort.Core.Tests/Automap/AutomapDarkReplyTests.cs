using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   A game that answers a move into the dark with a sentence ("It is pitch black.") rather than a heading, and the
///   join's end moving to the side of a direction learnt later.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AutomapDarkReplyTests : IsolatedProjectTests
{
  // Hollywood Hijinx: north from the porch into the dark house, turn on the flashlight, then south and north again
  private static readonly string[] _hijinx =
  {
      "Here begins a transcript of interaction with",
      "",
      "HOLLYWOOD HIJINX",
      "Infocom interactive fiction -- a zany treasure hunt",
      "Copyright (C) 1986 Infocom, Inc. All rights reserved.",
      "HOLLYWOOD HIJINX is a trademark of Infocom, Inc.",
      "Release 37 / Serial Number 861215",
      "",
      ">look",
      "South Junction",
      "You're standing in front of the house where you spent many of your summers as a",
      "youngster. The old place is not as big as it seemed to you then, but it is still",
      "quite large. Stone pathways wind east and west around the house, and a larger",
      "main walkway leads north.",
      "",
      "A life-size statue of Buck Palace, one of the stars of Uncle Buddy's talent",
      "stables, stands here. He's holding a bazooka pointing north.",
      "",
      ">look",
      "South Junction",
      "You're standing in front of the house where you spent many of your summers as a",
      "youngster. The old place is not as big as it seemed to you then, but it is still",
      "quite large. Stone pathways wind east and west around the house, and a larger",
      "main walkway leads north.",
      "",
      "A life-size statue of Buck Palace, one of the stars of Uncle Buddy's talent",
      "stables, stands here. He's holding a bazooka pointing north.",
      "",
      ">n",
      "As you walk toward the house, a large black cat scurries across the path,",
      "heading toward Johnny Carson's house.",
      "",
      "Front Porch",
      "You're standing on the front porch of the house. Next to the front door is a",
      "regulation mailbox endorsed by Buck Palace. Beneath the mailbox is a doorbell",
      "once rung by Sonny Tufts. To the south is a walkway.",
      "",
      ">s",
      "South Junction",
      "",
      "A life-size statue of Buck Palace, one of the stars of Uncle Buddy's talent",
      "stables, stands here. He's holding a bazooka pointing south. Hmmm. It looks as",
      "if the statue of Buck Palace, the fighting letter carrier, has changed position.",
      "",
      ">turn statue west",
      "You turn the statue of Buck Palace, the fighting letter carrier, to the west.",
      "",
      ">turn statue east",
      "You turn the statue of Buck Palace, the fighting letter carrier, to the east.",
      "",
      ">turn status north",
      "You can't see any status north here!",
      "",
      ">turn statue north",
      "You turn the statue of Buck Palace, the fighting letter carrier, to the north,",
      "and hear a click from off to the north.",
      "",
      ">n",
      "Front Porch",
      "",
      ">n",
      "The oak door is closed.",
      "",
      ">open door",
      "The oak door opens with a long squeal, and you hear hurried footsteps several",
      "rooms away.",
      "",
      ">n",
      "It is pitch black.",
      "",
      "Somewhere in the house you hear the thud of something falling on the floor.",
      "",
      ">turn on flashlight",
      "Okay, the flashlight is now on.",
      "",
      "Foyer",
      "This is the elegant foyer, covered in rich mahogany paneling. Your footsteps",
      "echo sharply in this large, hollow space. A broad, graceful staircase sweeps",
      "upstairs. Pillared archways beckon you east, west and north. The front door to",
      "the south is open. There is a coat closet by the front door. The door to the",
      "closet is closed.",
      "",
      ">s",
      "Front Porch",
      "",
      ">n",
      "Foyer",
      "",
      ">"
  };

  private static async Task RunToEnd(string[] lines)
  {
    var path = System.IO.Path.GetTempFileName();
    System.IO.File.WriteAllText(path, string.Join("\n", lines));
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    var run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
    try
    {
      var clock = System.Diagnostics.Stopwatch.StartNew();
      while (engine.ScreenLines().Count < lines.Length - 1 && clock.ElapsedMilliseconds < 8000) await Task.Delay(20);
      await Task.Delay(300);
    }
    finally
    {
      engine.Stop();
      await run;
      System.IO.File.Delete(path);
    }
  }

  [Test]
  public async Task NorthIntoThePitchBlackHouse_ThenTheFlashlight_MapsTheFoyerNorthOfThePorch()
  {
    await RunToEnd(_hijinx);

    var rooms = Project.Current.Elements.OfType<Room>().ToList();
    var porch = rooms.Single(room => room.Name == "Front Porch");
    var foyer = rooms.Single(room => room.Name == "Foyer");
    var join = Project.Current.Elements.OfType<Connection>().Single(connection =>
      (connection.GetSourceRoom() == porch && connection.GetTargetRoom() == foyer) ||
      (connection.GetSourceRoom() == foyer && connection.GetTargetRoom() == porch));

    // "turn on flashlight" isn't the way there: north is
    join.MidText.ShouldNotContain("flashlight");
    (join.StartText + join.EndText).ShouldNotContain("flashlight");
    var porchIsStart = join.GetSourceRoom() == porch;
    CompassPoint porchSide;
    if (porchIsStart) join.GetSourceRoom(out porchSide);
    else join.GetTargetRoom(out porchSide);
    porchSide.ShouldBe(CompassPoint.North);
    foyer.InnerBounds.Center.Y.ShouldBeLessThan(porch.InnerBounds.Center.Y);
  }

  [TestCase("It is pitch black.", true)]
  [TestCase("It is pitch black. You are likely to be eaten by a grue.", true)]
  [TestCase("It's too dark to see.", true)]
  [TestCase("Darkness", true)]
  [TestCase("It's too dark to go that way.", false)]
  [TestCase("You can't see anything in the darkness beyond the door.", false)]
  [TestCase("The oak door is closed.", false)]
  public void DarkReplies(string line, bool dark)
  {
    Trizbort.Automap.Automap.IsDarkReply(line).ShouldBe(dark);
  }

  [Test]
  public void ADirectionLearntLater_MovesTheEndOffASideThatOnlyFacedTheOtherRoom()
  {
    var porch = new Room(Project.Current) { Name = "Front Porch", Position = new Vector(0, 0) };
    var foyer = new Room(Project.Current) { Name = "Foyer", Position = new Vector(-160, -128) };
    Project.Current.Elements.Add(porch);
    Project.Current.Elements.Add(foyer);
    var canvas = new AutomapCanvas(Project.Current);

    // reached by a command, the join leaves the porch on whichever side faces the foyer
    canvas.ConnectByCommand(porch, foyer, "turn on flashlight");
    canvas.Connect(foyer, MappableDirection.South, porch, true);
    canvas.Connect(porch, MappableDirection.North, foyer, true);

    var join = Project.Current.Elements.OfType<Connection>().ShouldHaveSingleItem();
    join.GetSourceRoom(out var porchSide).ShouldBeSameAs(porch);
    porchSide.ShouldBe(CompassPoint.North);
  }
}
