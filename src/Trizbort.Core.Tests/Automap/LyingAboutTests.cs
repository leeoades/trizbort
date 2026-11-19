using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 19 Nov 2026, Colossal Cave in brief mode: back in the Shell Room the game showed only "There is an enormous
///   clam here with its shell tightly closed.", and automap took that for a new description and asked which room it
///   was. What's lying about isn't a description.
/// </summary>
[TestFixture]
[Category("Unit")]
public class LyingAboutTests : IsolatedProjectTests
{
  private const string Shell =
    "You're in a large room carved out of sedimentary rock. A shallow passage proceeds downward.\n" +
    "There is an enormous clam here with its shell tightly closed.";

  private sealed class Counted : IEditorPrompts
  {
    public int Asked { get; private set; }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default)
    {
      Asked++;
      return Task.FromResult(RoomDisambiguation.CreateNew());
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      Task.FromResult(true);
  }

  [TestCase(true)]
  [TestCase(false)]
  public async Task BackInARoom_WhatsLyingAbout_IsntANewDescription(bool verbose)
  {
    var path = Files.File("game.txt");
    File.WriteAllText(path,
      ">look\n\nArched Hall\nYou are in an arched hall.\n\n" +
      $">d\n\nShell Room\n{Shell}\n\n" +
      ">u\n\nArched Hall\n\n" +
      ">d\n\nShell Room\nThere is an enormous clam here with its shell tightly closed.\n\n>");
    var settings = AutomapSettings.Default;
    settings.VerboseTranscript = verbose;
    settings.AutoTidy = false;
    settings.GuessExits = false;
    var prompts = new Counted();

    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, prompts);

    prompts.Asked.ShouldBe(0);
    var rooms = Project.Current.Elements.OfType<Room>().ToList();
    rooms.Select(room => room.Name).ShouldBe(new[] { "Arched Hall", "Shell Room" }, ignoreOrder: true);
    result.CurrentRoom.Name.ShouldBe("Shell Room");
    // and it isn't kept as one of the room's descriptions
    rooms.Single(room => room.Name == "Shell Room").Descriptions.ShouldAllBe(description => description.StartsWith("You're"));
  }

  [TestCase("There is an enormous clam here with its shell tightly closed.", true)]
  [TestCase("You can see a lamp here.", true)]
  [TestCase("There is a shiny brass lamp nearby. There is a little bird in the cage.", true)]
  [TestCase("A three foot black rod with a rusty star on an end lies nearby.", true)]
  [TestCase("You are in a maze of twisty little passages, all alike.", false)]
  [TestCase("You're in a large room. There is an enormous clam here.", false)]
  [TestCase("The top of a 12-foot-tall beanstalk is poking out of the west pit.", false)]
  public void WhatsLyingAbout_IsToldFromADescription(string text, bool lyingAbout) =>
    Trizbort.Automap.Automap.OnlyWhatsLyingAbout(text).ShouldBe(lyingAbout);
}
