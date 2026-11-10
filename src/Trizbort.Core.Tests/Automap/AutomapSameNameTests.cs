using System.Collections.Generic;
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
///   Several rooms with one name: Hollywood Hijinx has three "In the Chimney", two in the west chimney one above the
///   other, and one down the east chimney from the roof. Joins one-way, as the user maps.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AutomapSameNameTests : IsolatedProjectTests
{
  private static readonly string[] _downIntoTheEastChimney =
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
    ">w",
    "Living Room",
    "This is the living room where Uncle Buddy and Aunt Hildegarde used to spend",
    "their evenings. Most of the west wall is covered by a grand stone fireplace with",
    "a mantle over it. A long sofa, several cushy chairs and a telephone fill the",
    "room. Most of the hardwood floor is covered by a monstrous Persian rug. A",
    "doorway leads north and an archway exits to the east.",
    "",
    "Sitting on the mantle is a red wax statuette, a white wax statuette and a blue",
    "wax statuette.",
    "",
    ">w",
    "If you want to go in the fireplace, say so.",
    "",
    ">enter fireplace",
    "Fireplace",
    "The fireplace is finely crafted masonry, but has long been neglected. The mortar",
    "is crumbling in a number of places. One brick, about waist high, seems",
    "particularly loose. The fireplace opens to the east.",
    "",
    ">e",
    "Living Room",
    "",
    "Sitting on the mantle is a red wax statuette, a white wax statuette and a blue",
    "wax statuette.",
    "",
    ">enter fireplace",
    "Fireplace",
    "",
    ">u",
    "You'll have to figure that out for yourself.",
    "",
    ">climb fireplace",
    "What a concept!",
    "",
    ">l",
    "Fireplace",
    "The fireplace is finely crafted masonry, but has long been neglected. The mortar",
    "is crumbling in a number of places. One brick, about waist high, seems",
    "particularly loose. The fireplace opens to the east.",
    "",
    ">take brick",
    "You pull the brick out of the hole, and along with it comes an indigo punch",
    "card, which drops in a spiral to the ground.",
    "",
    ">take card",
    "Taken.",
    "",
    ">up",
    "Using the hole where the brick was, you climb up the chimney several feet.",
    "",
    "In the Chimney",
    "You're in the chimney, above the fireplace. You can see a couple of holes here",
    "in the chimney where bricks used to be. The chimney continues upward and the",
    "fireplace is below.",
    "",
    ">up",
    "Using the holes you climb up the chimney several feet.",
    "",
    "In the Chimney",
    "You're inside the chimney. You can see a couple of holes here in the chimney",
    "where bricks used to be. The chimney continues upward and downward.",
    "",
    ">up",
    "Using the holes you climb up the chimney several feet onto the...",
    "",
    "West End of Roof",
    "You're doing your best to keep your balance here on the roof. During a fierce",
    "storm you can see Linda Ronstadt's house wash into the ocean from here. A",
    "chimney juts up from the roof here and there is another chimney to the east,",
    "near the end of the roof.",
    "",
    ">up",
    "You can't go that way.",
    "",
    ">e",
    "East End of Roof",
    "This is the east end of the roof. A chimney juts up from the roof here and there",
    "is another chimney off to the west, near the end of the roof.",
    "",
    ">up",
    "You can't go that way.",
    "",
    ">s",
    "You'd fall off the roof!",
    "",
    ">d",
    "You climb down into the chimney.",
    "",
    "In the Chimney",
    "You're standing in the chimney. There are a couple of holes here in the chimney",
    "where bricks used to be. This chimney doesn't lead any further down, only up.",
    "",
    "You can see a stuffed penguin here.",
    "",
    ">"
  };

  private static readonly string[] _downFromTheWestEndOfTheRoof =
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
    ">w",
    "Living Room",
    "This is the living room where Uncle Buddy and Aunt Hildegarde used to spend",
    "their evenings. Most of the west wall is covered by a grand stone fireplace with",
    "a mantle over it. A long sofa, several cushy chairs and a telephone fill the",
    "room. Most of the hardwood floor is covered by a monstrous Persian rug. A",
    "doorway leads north and an archway exits to the east.",
    "",
    "Sitting on the mantle is a red wax statuette, a white wax statuette and a blue",
    "wax statuette.",
    "",
    ">w",
    "If you want to go in the fireplace, say so.",
    "",
    ">enter fireplace",
    "Fireplace",
    "The fireplace is finely crafted masonry, but has long been neglected. The mortar",
    "is crumbling in a number of places. One brick, about waist high, seems",
    "particularly loose. The fireplace opens to the east.",
    "",
    ">e",
    "Living Room",
    "",
    "Sitting on the mantle is a red wax statuette, a white wax statuette and a blue",
    "wax statuette.",
    "",
    ">enter fireplace",
    "Fireplace",
    "",
    ">u",
    "You'll have to figure that out for yourself.",
    "",
    ">climb fireplace",
    "What a concept!",
    "",
    ">l",
    "Fireplace",
    "The fireplace is finely crafted masonry, but has long been neglected. The mortar",
    "is crumbling in a number of places. One brick, about waist high, seems",
    "particularly loose. The fireplace opens to the east.",
    "",
    ">take brick",
    "You pull the brick out of the hole, and along with it comes an indigo punch",
    "card, which drops in a spiral to the ground.",
    "",
    ">take card",
    "Taken.",
    "",
    ">up",
    "Using the hole where the brick was, you climb up the chimney several feet.",
    "",
    "In the Chimney",
    "You're in the chimney, above the fireplace. You can see a couple of holes here",
    "in the chimney where bricks used to be. The chimney continues upward and the",
    "fireplace is below.",
    "",
    ">up",
    "Using the holes you climb up the chimney several feet.",
    "",
    "In the Chimney",
    "You're inside the chimney. You can see a couple of holes here in the chimney",
    "where bricks used to be. The chimney continues upward and downward.",
    "",
    ">up",
    "Using the holes you climb up the chimney several feet onto the...",
    "",
    "West End of Roof",
    "You're doing your best to keep your balance here on the roof. During a fierce",
    "storm you can see Linda Ronstadt's house wash into the ocean from here. A",
    "chimney juts up from the roof here and there is another chimney to the east,",
    "near the end of the roof.",
    "",
    ">up",
    "You can't go that way.",
    "",
    ">e",
    "East End of Roof",
    "This is the east end of the roof. A chimney juts up from the roof here and there",
    "is another chimney off to the west, near the end of the roof.",
    "",
    ">up",
    "You can't go that way.",
    "",
    ">s",
    "You'd fall off the roof!",
    "",
    ">d",
    "You climb down into the chimney.",
    "",
    "In the Chimney",
    "You're standing in the chimney. There are a couple of holes here in the chimney",
    "where bricks used to be. This chimney doesn't lead any further down, only up.",
    "",
    "You can see a stuffed penguin here.",
    "",
    ">u",
    "Using the holes you climb up the chimney several feet onto the...",
    "",
    "East End of Roof",
    "",
    ">d",
    "You climb down into the chimney.",
    "",
    "In the Chimney",
    "",
    "You can see a stuffed penguin here.",
    "",
    ">u",
    "Using the holes you climb up the chimney several feet onto the...",
    "",
    "East End of Roof",
    "",
    ">s",
    "You'd fall off the roof!",
    "",
    ">u",
    "You can't go that way.",
    "",
    ">d",
    "You climb down into the chimney.",
    "",
    "In the Chimney",
    "",
    "You can see a stuffed penguin here.",
    "",
    ">u",
    "Using the holes you climb up the chimney several feet onto the...",
    "",
    "East End of Roof",
    "",
    ">w",
    "West End of Roof",
    "",
    ">d",
    "You climb down into the chimney.",
    "",
    "In the Chimney",
    "",
    ">"
  };

  private sealed class NewRoomPrompts : IEditorPrompts
  {
    public readonly List<IReadOnlyList<Room>> Asked = new();

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, System.Threading.CancellationToken token = default) =>
      Task.FromResult(AutomapSameDirectionResult.KeepBoth);

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, System.Threading.CancellationToken token = default)
    {
      Asked.Add(candidates);
      return Task.FromResult(RoomDisambiguation.CreateNew());
    }
  }

  private static async Task RunToEnd(string[] lines, IEditorPrompts prompts)
  {
    var path = System.IO.Path.GetTempFileName();
    System.IO.File.WriteAllText(path, string.Join("\n", lines));
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AssumeTwoWayConnections = false;
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

  private static Room Chimney(string startsWith) =>
    Project.Current.Elements.OfType<Room>().Single(room =>
      room.Name == "In the Chimney" && room.Descriptions.Any(description => description.StartsWith(startsWith)));

  private static Room RoomNamed(string name) => Project.Current.Elements.OfType<Room>().Single(room => room.Name == name);

  /// <summary>Where the map shows down leading from <paramref name="room" />, from either end of a join.</summary>
  private static List<Room> Below(Room room) =>
    Project.Current.Elements.OfType<Connection>()
      .Select(join => (join, ways: JoinWays.Of(join)))
      .Where(pair => pair.join.GetSourceRoom() != pair.join.GetTargetRoom())
      .Select(pair =>
        pair.join.GetSourceRoom() == room && JoinWays.Lists(pair.ways, true, Domain.Enums.MappableDirection.Down)
          ? pair.join.GetTargetRoom()
          : pair.join.GetTargetRoom() == room && JoinWays.Lists(pair.ways, false, Domain.Enums.MappableDirection.Down)
            ? pair.join.GetSourceRoom()
            : null)
      .Where(other => other != null).ToList();

  [Test]
  public async Task DownTheEastChimney_ADescriptionUnlikeEitherWestChimney_AsksAndMakesANewRoom()
  {
    var prompts = new NewRoomPrompts();

    await RunToEnd(_downIntoTheEastChimney, prompts);

    // up into the second west chimney (a description unlike the first's), then down the east chimney, offering both
    // west chimney rooms
    prompts.Asked.Count.ShouldBe(2);
    prompts.Asked[1].Count.ShouldBe(2);
    Project.Current.Elements.OfType<Room>().Count(room => room.Name == "In the Chimney").ShouldBe(3);
    Below(RoomNamed("East End of Roof")).ShouldBe(new[] { Chimney("You're standing in the chimney") });
  }

  [Test]
  public async Task DownFromTheWestEndOfTheRoof_GoesBackTheWayItCameUp_ToTheUpperChimney()
  {
    var prompts = new NewRoomPrompts();

    await RunToEnd(_downFromTheWestEndOfTheRoof, prompts);

    // only up into the second west chimney, and the first time down the east chimney: after that, the map says
    prompts.Asked.Count.ShouldBe(2);
    Below(RoomNamed("West End of Roof")).ShouldBe(new[] { Chimney("You're inside the chimney") });
    Below(RoomNamed("East End of Roof")).ShouldBe(new[] { Chimney("You're standing in the chimney") });
  }

  [Test]
  public async Task WithoutAsking_ADescriptionMatchingAnotherRoomOfThatName_PicksThatRoom()
  {
    // Auto-choose (as when the user said to stop asking) still goes by the description when one matches
    await RunToEnd(_downFromTheWestEndOfTheRoof, NonInteractiveEditorPrompts.Instance);

    Below(RoomNamed("West End of Roof")).ShouldBe(new[] { Chimney("You're inside the chimney") });
  }
}
