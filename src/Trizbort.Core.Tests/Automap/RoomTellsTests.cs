using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 18 Nov 2026: "which room does it mean?" listed "Maze", "Maze", "Maze". Each room now shows what tells it
///   apart: its subtitle, its description, its objects, or the ways out of it.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RoomTellsTests : IsolatedProjectTests
{
  private const string Alike = "You are in a maze of twisty little passages, all alike.";

  private static Room AddRoom(string name, string description, string objects = "", string subtitle = "")
  {
    var room = new Room(Project.Current) { Name = name, Objects = objects, SubTitle = subtitle };
    if (description != null) room.AddDescription(description);
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void RoomsAllAlike_AreToldApartByTheirObjects()
  {
    var rod = AddRoom("Maze", Alike, "black rod");
    var bird = AddRoom("Maze", Alike, "bird");
    var bare = AddRoom("Maze", Alike);

    var tells = RoomTells.Apart(new[] { rod, bird, bare });

    tells[rod].ShouldBe("objects: black rod");
    tells[bird].ShouldBe("objects: bird");
    tells[bare].ShouldBe("no objects");
  }

  [Test]
  public void RoomsWithNothingDown_AreToldApartByTheirWaysOut()
  {
    var a = AddRoom("Maze", Alike);
    var b = AddRoom("Maze", Alike);
    var dead = AddRoom("Dead End", "You have reached a dead end.");
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(a.PortAt(CompassPoint.East)),
      new Vertex(b.PortAt(CompassPoint.West))));
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(b.PortAt(CompassPoint.South)),
      new Vertex(dead.PortAt(CompassPoint.North))));

    var tells = RoomTells.Apart(new[] { a, b });

    tells[a].ShouldBe("ways: e to Maze");
    tells[b].ShouldBe("ways: s to Dead End, w to Maze");
  }

  [Test]
  public void DifferentDescriptions_ShowTheSentenceThatDiffers()
  {
    var one = AddRoom("Maze", "You are in a maze of twisty little passages, all different.");
    var two = AddRoom("Maze", "You are in a little maze of twisting passages, all different.");

    var tells = RoomTells.Apart(new[] { one, two });

    tells[one].ShouldBe("You are in a maze of twisty little passages, all different.");
    tells[two].ShouldBe("You are in a little maze of twisting passages, all different.");
  }

  [Test]
  public void ASubtitle_IsEnough()
  {
    var north = AddRoom("Garden", "A garden.", subtitle: "North");
    var south = AddRoom("Garden", "A garden.", subtitle: "South");

    var tells = RoomTells.Apart(new[] { north, south });

    tells[north].ShouldBe("North");
    tells[south].ShouldBe("South");
  }
}
