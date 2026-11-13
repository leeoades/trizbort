using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Core.Tests;

/// <summary>
///   Lee's hand-tidied layouts (tests/automap/neat-layouts): what a neat map looks like, and rough versions of the
///   same maps. See the "What a neat map looks like" notes.
/// </summary>
[TestFixture]
[Category("Unit")]
public class NeatLayoutTests : IsolatedProjectTests
{
  private static string RepositoryFile(params string[] parts)
  {
    var directory = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "Trizbort.sln")))
      directory = directory.Parent;
    directory.ShouldNotBeNull();
    return System.IO.Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
  }

  [Test]
  public void ASplitJoin_DoesNotPullItsRoomsTogether()
  {
    // the Front Porch's split join leads into the house, a separate area well to the east: tidying the outside
    // must leave the porch where it is, under the start room
    new MapLoader(Project.Current).LoadMap(RepositoryFile("tests", "automap", "neat-layouts",
      "hollywood-hijinx-nov-13-neat.trizbort")).ShouldBeTrue();
    var elements = Project.Current.Elements.ToList();
    var rooms = elements.OfType<Room>().ToList();
    foreach (var room in rooms) room.PlacedByHand = false;
    var porch = rooms.Single(room => room.Name == "Front Porch");
    var foyer = rooms.Single(room => room.Name == "Foyer");
    var porchAt = porch.Position;

    var outside = rooms.Where(room => room.Position.X < 0).ToList();
    LayoutTidy.TidyPlan.Apply(LayoutTidy.PlanTidy(elements, outside, TidyMethod.Quick).Run());

    porch.Position.ShouldBe(porchAt);
    foyer.Position.X.ShouldBeGreaterThan(porch.Position.X + 1000);
  }

  /// <summary>Prints what each tidy does to each layout, for tuning (run by hand).</summary>
  [Explicit]
  [Category("Probe")]
  [TestCase("A-same-spot-neat")]
  [TestCase("B-busy-hub-neat")]
  [TestCase("C-long-corridor-neat")]
  [TestCase("D-twisty-forest-neat")]
  [TestCase("E-diagonals-neat")]
  [TestCase("hollywood-hijinx-nov-13-neat")]
  [TestCase("A-same-spot-rough")]
  [TestCase("B-busy-hub-rough")]
  [TestCase("C-long-corridor-rough")]
  [TestCase("D-twisty-forest-rough")]
  [TestCase("E-diagonals-rough")]
  public void Probe(string name)
  {
    foreach (var method in new[] { TidyMethod.Quick, TidyMethod.Thorough })
    {
      Project.Current = new Project();
      new MapLoader(Project.Current).LoadMap(RepositoryFile("tests", "automap", "neat-layouts", name + ".trizbort"));
      var elements = Project.Current.Elements.ToList();
      foreach (var r in elements.OfType<Room>()) r.PlacedByHand = false;
      var rooms = elements.OfType<Room>().ToList();
      var before = rooms.ToDictionary(r => r, r => r.Position);
      var plan = LayoutTidy.PlanTidy(elements, rooms, method);
      var moves = plan.Run();
      LayoutTidy.TidyPlan.Apply(moves);
      TestContext.Out.WriteLine($"== {name} {method}: moved {moves?.Count ?? 0}, natural {LayoutTidy.NaturalJoins(elements)}, exact {LayoutTidy.ExactJoins(elements)}");
      foreach (var r in rooms.Where(r => r.Position != before[r]))
        TestContext.Out.WriteLine($"   {r.Name}{(r.SubTitle != "" ? " (" + r.SubTitle + ")" : "")}: {before[r].X},{before[r].Y} -> {r.Position.X},{r.Position.Y}");
    }
  }
}
