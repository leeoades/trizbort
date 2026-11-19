using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Navigation;

namespace Trizbort.Core.Tests.Navigation;

/// <summary>
///   Lee, 19 Nov 2026: in Colossal Cave, back to the far side of the Plover Room without saying PLOVER. Avoid This
///   Route on a step: the directions go round it, until it's allowed again.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AvoidRouteTests : IsolatedProjectTests
{
  // Hall -east- Kitchen -north- Pantry; "xyzzy" from the hall to the vault, one way; the pantry's east door also
  // leads to the vault
  private const string Game =
    ">look\n\nHall\nA grand hall.\n\n" +
    ">east\n\nKitchen\nA small kitchen.\n\n" +
    ">north\n\nPantry\nShelves of jars.\n\n" +
    ">south\n\nKitchen\nA small kitchen.\n\n" +
    ">west\n\nHall\nA grand hall.\n\n" +
    ">xyzzy\n\nVault\nA locked vault.\n\n>";

  private static Room Room(string name) => Project.Current.Elements.OfType<Room>().Single(room => room.Name == name);

  private async Task Map()
  {
    var path = Files.File("game.txt");
    File.WriteAllText(path, Game);
    var settings = AutomapSettings.Default;
    settings.AutoTidy = false;
    settings.GuessExits = false;
    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, null);
    result.Succeeded.ShouldBeTrue(result.Error);
    var project = Project.Current;
    project.Elements.Add(new Connection(project, new Vertex(Room("Pantry").PortAt(CompassPoint.East)),
      new Vertex(Room("Vault").PortAt(CompassPoint.West))));
  }

  [Test]
  public async Task AvoidingAStep_RoutesRoundIt_UntilItsAllowedAgain()
  {
    await Map();
    var directions = new Directions();
    directions.NavigateFrom(Room("Hall"));
    directions.NavigateTo(Room("Vault"), null);
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "xyzzy" });

    var xyzzy = directions.Steps[0];
    directions.Avoid(xyzzy);

    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "east", "north", "east" });
    directions.Avoiding.ShouldHaveSingleItem().Command.ShouldBe("xyzzy");
    directions.Status.ShouldContain("Avoiding");

    // still avoided going there again
    directions.Stop();
    directions.Go();
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "east", "north", "east" });

    directions.Allow(xyzzy);
    directions.Avoiding.ShouldBeEmpty();
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "xyzzy" });
  }

  [Test]
  public async Task AvoidingTheOnlyWay_SaysThereIsNone_WithoutIt()
  {
    await Map();
    var directions = new Directions();
    // the vault's only way out is the pantry door
    directions.NavigateFrom(Room("Vault"));
    directions.NavigateTo(Room("Hall"), null);
    var west = directions.Steps[0];

    directions.Avoid(west);

    directions.Steps.ShouldBeEmpty();
    directions.Status.ShouldContain("without the ways avoided");
    directions.Allow(west);
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "west", "south", "west" });
  }
}
