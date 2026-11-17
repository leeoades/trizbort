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

/// <summary>Directions (satnav): the commands that lead from one room to another along the map's joins.</summary>
[TestFixture]
[Category("Unit")]
public class DirectionsTests : IsolatedProjectTests
{
  // Hall -east- Kitchen -north- Pantry -up- Attic; "xyzzy" from the hall to the vault, one way; the cellar is reached
  // down from the kitchen, and nobody has come back up
  private const string Game =
    ">look\n\nHall\nA grand hall.\n\n" +
    ">east\n\nKitchen\nA small kitchen.\n\n" +
    ">north\n\nPantry\nShelves of jars.\n\n" +
    ">up\n\nAttic\nA dusty attic.\n\n" +
    ">down\n\nPantry\nShelves of jars.\n\n" +
    ">south\n\nKitchen\nA small kitchen.\n\n" +
    ">west\n\nHall\nA grand hall.\n\n" +
    ">xyzzy\n\nVault\nA locked vault.\n\n" +
    ">east\n\nTreasury\nGold everywhere.\n\n>";

  private async Task Map(string transcript)
  {
    var path = Files.File("game.txt");
    File.WriteAllText(path, transcript);
    var settings = AutomapSettings.Default;
    settings.AutoTidy = false;
    settings.GuessExits = false;
    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, null);
    result.Succeeded.ShouldBeTrue(result.Error);
  }

  private static Room Room(string name) => Project.Current.Elements.OfType<Room>().Single(room => room.Name == name);

  private static string[] Commands(Room from, Room to) =>
    RouteFinder.Find(Project.Current.Elements, from, to)?.Select(step => step.Command).ToArray();

  [Test]
  public async Task TheWayThere_FollowsTheJoinsAutomapMade()
  {
    await Map(Game);
    Commands(Room("Attic"), Room("Hall")).ShouldBe(new[] { "down", "south", "west" });
    Commands(Room("Hall"), Room("Attic")).ShouldBe(new[] { "east", "north", "up" });
    Commands(Room("Hall"), Room("Treasury")).ShouldBe(new[] { "xyzzy", "east" });
    Commands(Room("Hall"), Room("Hall")).ShouldBeEmpty();
  }

  [Test]
  public async Task AOneWayCommand_IsntTakenBackwards()
  {
    await Map(Game);
    // "xyzzy" leads from the hall to the vault, not back; the way back from the treasury is assumed (west)
    Commands(Room("Treasury"), Room("Vault")).ShouldBe(new[] { "west" });
    Commands(Room("Vault"), Room("Hall")).ShouldBeNull();
  }

  [Test]
  public async Task AWayOnlyAssumed_IsSaidSo()
  {
    await Map(Game);
    var route = RouteFinder.Find(Project.Current.Elements, Room("Treasury"), Room("Vault"));
    route.ShouldHaveSingleItem().Assumed.ShouldBeTrue();
    RouteFinder.Find(Project.Current.Elements, Room("Hall"), Room("Kitchen")).ShouldHaveSingleItem().Assumed.ShouldBeFalse();
  }

  [Test]
  public void JoinsDrawnByHand_LeadBothWays_ByTheirSidesAndLabels()
  {
    var project = Project.Current;
    var hall = new Room(project) { Name = "Hall", Position = new Vector(0, 0) };
    var garden = new Room(project) { Name = "Garden", Position = new Vector(0, 200) };
    var shed = new Room(project) { Name = "Shed", Position = new Vector(200, 200) };
    project.Elements.Add(hall);
    project.Elements.Add(garden);
    project.Elements.Add(shed);
    project.Elements.Add(new Connection(project, new Vertex(hall.PortAt(CompassPoint.South)),
      new Vertex(garden.PortAt(CompassPoint.North))));
    project.Elements.Add(new Connection(project, new Vertex(garden.PortAt(CompassPoint.East)),
      new Vertex(shed.PortAt(CompassPoint.West))) { StartText = "in", EndText = "out" });

    Commands(hall, shed).ShouldBe(new[] { "south", "in" });
    Commands(shed, hall).ShouldBe(new[] { "out", "north" });
    RouteFinder.Find(project.Elements, hall, shed)!.Any(step => step.Assumed).ShouldBeFalse();
  }

  [Test]
  public async Task FollowingThePlayer_DropsStepsTaken_ReroutesOffTheRoute_AndArrives()
  {
    await Map(Game);
    var directions = new Directions();
    directions.NavigateTo(Room("Attic"), Room("Hall"));
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "east", "north", "up" });
    directions.Status.ShouldStartWith("3 moves to Attic");

    directions.Follow(Room("Kitchen"));
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "north", "up" });

    // went back west instead: rerouted from the hall
    directions.Follow(Room("Hall"));
    directions.Status.ShouldStartWith("Rerouting from Hall.");
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "east", "north", "up" });

    // played on fast, straight to the pantry: the steps up to it go
    directions.Follow(Room("Pantry"));
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "up" });

    directions.Follow(Room("Attic"));
    directions.Steps.ShouldBeEmpty();
    directions.Status.ShouldBe("You've arrived at Attic.");
    // arrived: the directions stop, as at Stop, so the button says Go again
    directions.HasRoute.ShouldBeFalse();
    directions.CanGo.ShouldBeTrue();

    // and moving on from there doesn't start them again
    directions.Follow(Room("Pantry"));
    directions.HasRoute.ShouldBeFalse();
    directions.Steps.ShouldBeEmpty();
    directions.From.ShouldBe(Room("Pantry"));
  }

  [Test]
  public async Task AlreadyThere_NothingToStop()
  {
    await Map(Game);
    var directions = new Directions();
    directions.NavigateTo(Room("Hall"), Room("Hall"));
    directions.Status.ShouldBe("You're at Hall.");
    directions.HasRoute.ShouldBeFalse();
  }

  [Test]
  public async Task NoWayOnTheMap_SaysSo()
  {
    await Map(Game);
    var directions = new Directions();
    directions.NavigateFrom(Room("Vault"));
    directions.Steps.ShouldBeEmpty();
    directions.NavigateTo(Room("Hall"), null);
    directions.HasRoute.ShouldBeTrue();
    directions.Steps.ShouldBeEmpty();
    directions.Status.ShouldBe("The map doesn't show a way from Vault to Hall.");
  }

  [Test]
  public async Task WithoutAutomap_ChoosingWhereFromThenWhereTo_GivesDirections_ThatDontFollowAnyone()
  {
    await Map(Game);
    var directions = new Directions();
    directions.NavigateFrom(Room("Attic"));
    directions.Status.ShouldBe("Where to? Right-click a room and choose Navigate to Here.");
    directions.NavigateTo(Room("Hall"), null);
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "down", "south", "west" });
    directions.FromWhereYouAre.ShouldBeFalse();
    directions.Follow(Room("Kitchen"));
    directions.Steps.Count.ShouldBe(3);
  }

  [Test]
  public async Task Stop_ClearsTheDirections_KeepingFromAndTo()
  {
    await Map(Game);
    var directions = new Directions();
    directions.NavigateFrom(Room("Hall"));
    directions.NavigateTo(Room("Attic"), null);
    directions.Steps.Count.ShouldBe(3);

    directions.Stop();

    directions.HasRoute.ShouldBeFalse();
    directions.Steps.ShouldBeEmpty();
    directions.From.ShouldBe(Room("Hall"));
    directions.To.ShouldBe(Room("Attic"));
    directions.Status.ShouldBe("Press Go for directions.");

    directions.Go();
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "east", "north", "up" });
  }

  [Test]
  public async Task Stopped_FollowingWhereYouAre_KeepsFromUpToDate_WithoutDirections()
  {
    await Map(Game);
    var directions = new Directions();
    directions.NavigateTo(Room("Attic"), Room("Hall"));
    directions.Stop();

    directions.Follow(Room("Kitchen"));

    directions.Steps.ShouldBeEmpty();
    directions.From.ShouldBe(Room("Kitchen"));
    directions.Go();
    directions.Steps.Select(step => step.Command).ShouldBe(new[] { "north", "up" });
  }
}
