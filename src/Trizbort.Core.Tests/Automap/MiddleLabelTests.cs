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

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 19 Nov 2026: a join drawn south from one room and north from the other, labelled "random chance" in the
///   middle (one of Colossal Cave's). Going through it and back moved the words to an end label: "n / random chance".
///   Words the user wrote in the middle stay there.
/// </summary>
[TestFixture]
[Category("Unit")]
public class MiddleLabelTests : IsolatedProjectTests
{
  [Test]
  public async Task WordsWrittenInTheMiddle_StayThere_WhenThePlayerGoesThroughAndBack()
  {
    var project = Project.Current;
    var hall = new Room(project) { Name = "Hall", Position = new Vector(0, 0) };
    hall.AddDescription("A hall.");
    var cave = new Room(project) { Name = "Cave", Position = new Vector(0, 320) };
    cave.AddDescription("A cave.");
    project.Elements.Add(hall);
    project.Elements.Add(cave);
    var join = new Connection(project, new Vertex(hall.PortAt(CompassPoint.South)),
      new Vertex(cave.PortAt(CompassPoint.North))) { MidText = "random chance" };
    project.Elements.Add(join);
    var path = Files.File("game.txt");
    File.WriteAllText(path, ">look\n\nHall\nA hall.\n\n>s\n\nCave\nA cave.\n\n>n\n\nHall\nA hall.\n\n>s\n\nCave\nA cave.\n\n>");
    var settings = AutomapSettings.Default;
    settings.AutoTidy = false;
    settings.GuessExits = false;

    var result = await HeadlessAutomap.RunAsync(project, path, settings, null);

    result.Succeeded.ShouldBeTrue(result.Error);
    project.Elements.OfType<Connection>().ShouldBe(new[] { join });
    join.MidText.ShouldBe("random chance");
    (join.StartText ?? string.Empty).ShouldNotContain("random");
    (join.EndText ?? string.Empty).ShouldNotContain("random");
  }
}
