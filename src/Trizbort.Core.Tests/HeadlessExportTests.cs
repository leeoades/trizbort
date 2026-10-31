using System.IO;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 105: the CLI code export flags.</summary>
[TestFixture]
[Category("Integration")]
public class HeadlessExportTests : IsolatedProjectTests
{
  private static void BuildMap()
  {
    var first = ProjectRegressionTests.AddRoom("Observatory");
    first.IsStartRoom = true;
    var second = ProjectRegressionTests.AddRoom("Gallery");
    ProjectRegressionTests.Connect(first, second);
  }

  private CommandLineOptions AllFlags()
  {
    return new CommandLineOptions
    {
      I6 = Files.File("a.inf"),
      I7 = Files.File("b.ni"),
      Tads = Files.File("c.t"),
      Alan = Files.File("d.ala"),
      Hugo = Files.File("e.hug"),
      Zil = Files.File("f.zil"),
      Quest = Files.File("g.aslx"),
      QuestRooms = Files.File("h.aslx"),
      Adventuron = Files.File("i.txt")
    };
  }

  [Test]
  public void Run_WritesEveryLanguage()
  {
    BuildMap();
    var options = AllFlags();

    var results = HeadlessExport.Run(options, Project.Current);

    results.Count.ShouldBe(9);
    foreach (var result in results)
    {
      result.Error.ShouldBeNull(result.Flag);
      File.Exists(result.File).ShouldBeTrue(result.Flag);
      new FileInfo(result.File).Length.ShouldBeGreaterThan(0, result.Flag);
    }
  }

  [Test]
  public void Run_NoFlags_DoesNothing()
  {
    BuildMap();
    HeadlessExport.HasExportFlags(new CommandLineOptions()).ShouldBeFalse();
    HeadlessExport.Run(new CommandLineOptions(), Project.Current).ShouldBeEmpty();
  }

  [Test]
  public void Run_InvalidFolder_ReportsError()
  {
    BuildMap();
    var options = new CommandLineOptions
    {
      Alan = Path.Combine(Files.File("missing-folder"), "x.ala"),
      Tads = Files.File("ok.t")
    };

    var results = HeadlessExport.Run(options, Project.Current);

    results.Count.ShouldBe(2);
    results.Single(r => r.Flag == "alan").Error.ShouldNotBeNullOrEmpty();
    results.Single(r => r.Flag == "tads").Error.ShouldBeNull();
  }

  [Test]
  public void QuestRoomsFlag_ParsesWithoutSpace()
  {
    var options = CommandLineParsing.Parse(new[] { "--questrooms", "out.aslx", "--adventuron", "adv.txt" });
    options.QuestRooms.ShouldBe("out.aslx");
    options.Adventuron.ShouldBe("adv.txt");
  }
}
