using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export;
using Trizbort.Export.Domain;
using Trizbort.Export.Languages;
using Trizbort.Setup;

namespace Trizbort.Tests {
  internal class PreparationExporter : CodeExporter {
    public IReadOnlyList<Location> Locations => LocationsInExportOrder;
    public IReadOnlyList<ExportRegion> Regions => RegionsInExportOrder;
    public override List<KeyValuePair<string, string>> FileDialogFilters => new List<KeyValuePair<string, string>>();
    public override string FileDialogTitle => "";
    protected override IEnumerable<string> ReservedWords => new[] {"reserved"};
    protected override string GetExportName(Room room, int? suffix) => GetExportName(room.Name, suffix);
    protected override string GetExportName(string name, int? suffix) => name + suffix;
    protected override void ExportContent(TextWriter writer) { }
    protected override void ExportHeader(TextWriter writer, string title, string author, string description, string history) { }
  }

  [TestFixture, Category("Integration")]
  public class ExporterRegressionTests : IsolatedProjectTests {
    private static IEnumerable<TestCaseData> Exporters() {
      yield return new TestCaseData(typeof(Inform6Exporter), "Include \"Parser\";", "e_to");
      yield return new TestCaseData(typeof(Inform7Exporter), "room", "east");
      yield return new TestCaseData(typeof(TadsExporter), "gameMain: GameMainDef", "east");
      yield return new TestCaseData(typeof(AlanExporter), "isa location", "Exit east");
      yield return new TestCaseData(typeof(HugoExporter), "routine init", "e_to");
      yield return new TestCaseData(typeof(ZilExporter), "<VERSION ZIP>", "(EAST TO");
      yield return new TestCaseData(typeof(QuestExporter), "<asl", "alias=\"east\"");
      yield return new TestCaseData(typeof(QuestRoomsExporter), "<object", "alias=\"east\"");
      yield return new TestCaseData(typeof(AdventuronExporter), "locations {", ", east_oneway,");
    }

    [TestCaseSource(nameof(Exporters))]
    public void AllLanguages_ExportRoomsDirectionsAndUpdatedContentWithoutMutatingMap(Type type, string structure, string direction) {
      var first = ProjectRegressionTests.AddRoom("Observatory");
      first.AddDescription("A bright dome.");
      first.Objects = "Chest [c]\n  Key";
      first.IsStartRoom = true;
      var second = ProjectRegressionTests.AddRoom("Gallery");
      second.AddDescription("A marble hall.");
      ProjectRegressionTests.Connect(first, second);
      var count = Project.Current.Elements.Count;
      using (var exporter = (CodeExporter) Activator.CreateInstance(type)) {
        var before = exporter.Export();
        before.ShouldContain(structure);
        before.ToLowerInvariant().ShouldContain(direction.ToLowerInvariant());
        before.ToLowerInvariant().ShouldContain("observatory");
        before.ToLowerInvariant().ShouldContain("gallery");
        if (type != typeof(AdventuronExporter)) {
          before.ToLowerInvariant().ShouldContain("chest");
          before.ToLowerInvariant().ShouldContain("key");
        }
        Normalize(type, exporter.Export()).ShouldBe(Normalize(type, before));
        if (type == typeof(QuestExporter)) XDocument.Parse(before).Root.Name.LocalName.ShouldBe("asl");
        if (type == typeof(QuestRoomsExporter)) XDocument.Parse("<rooms>" + before + "</rooms>").Root.Elements("object").Count().ShouldBe(2);
        var label = new MapLabel(Project.Current) {Text = "AnnotationMustNotLeak"};
        Project.Current.Elements.Add(label);
        Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(first.PortAt(CompassPoint.North)), new Vertex(label.PortList[0])));
        Normalize(type, exporter.Export()).ShouldBe(Normalize(type, before));
        first.Name = "Planetarium";
        exporter.Export().ToLowerInvariant().ShouldContain("planetarium");
        new LegacyMapFileEngine(Project.Current).Save(Files.File("map.trizbort")).ShouldBeTrue();
        var loaded = new Project();
        PersistenceRegressionTests.Load(loaded, Files.File("map.trizbort"));
        Project.Current = loaded;
        exporter.Export().ToLowerInvariant().ShouldContain("planetarium");
        exporter.Export(Files.File("output.txt"));
        File.ReadAllText(Files.File("output.txt")).ToLowerInvariant().ShouldContain("planetarium");
      }
      count.ShouldBe(3);
      Project.Current.Elements.Count.ShouldBe(5);
    }

    private static string Normalize(Type type, string output) {
      if (type != typeof(QuestExporter)) return output;
      var document = XDocument.Parse(output);
      var id = document.Root.Element("game").Element("gameid");
      Guid.TryParse(id.Value, out _).ShouldBeTrue();
      id.Value = "generated-game-id";
      return document.ToString();
    }

    [Test]
    public void Preparation_UsesUniqueNamesAcrossReservedWordsRegionsRoomsAndNestedObjects() {
      Settings.Regions.Add(new Region {RegionName = "Forest"});
      var room = ProjectRegressionTests.AddRoom("reserved");
      room.Region = "Forest";
      room.Objects = "Chest [c]\n  Key\nreserved\nForest";
      ProjectRegressionTests.AddRoom("reserved");
      ProjectRegressionTests.AddRoom("Forest");
      using var exporter = new PreparationExporter();
      exporter.Export();
      exporter.Locations.Select(location => location.ExportName).ShouldBe(new[] {"reserved2", "reserved3", "Forest2"});
      exporter.Regions.Single().ExportName.ShouldBe("Forest");
      var things = exporter.Locations[0].Things;
      things.Select(thing => thing.ExportName).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(4);
      things[0].IsContainer.ShouldBeTrue();
      things[1].Container.ShouldBeSameAs(things[0]);
      things[0].Contents.ShouldBe(new[] {things[1]});
      things[2].ExportName.ShouldBe("reserved4");
      things[3].ExportName.ShouldBe("Forest3");
      exporter.Export();
      exporter.Locations.Count.ShouldBe(3);
      exporter.Locations[0].Things.Count.ShouldBe(4);
    }

    [TestCase(ConnectionFlow.OneWay, false)]
    [TestCase(ConnectionFlow.TwoWay, true)]
    public void Preparation_ResolvesDirectedExitsAndDoorMetadata(ConnectionFlow flow, bool reciprocal) {
      var first = ProjectRegressionTests.AddRoom("First");
      var second = ProjectRegressionTests.AddRoom("Second");
      var connection = ProjectRegressionTests.Connect(first, second);
      connection.Flow = flow;
      connection.Style = ConnectionStyle.Dashed;
      connection.Door = new Door {Locked = true};
      using var exporter = new PreparationExporter();
      exporter.Export();
      var source = exporter.Locations[0];
      var target = exporter.Locations[1];
      var exit = source.GetBestExit(MappableDirection.East);
      exit.Target.ShouldBeSameAs(target);
      exit.Door.Locked.ShouldBeTrue();
      exit.Conditional.ShouldBeTrue();
      Exit.IsReciprocated(source, MappableDirection.East, target).ShouldBe(reciprocal);
      (target.GetBestExit(MappableDirection.West) != null).ShouldBe(reciprocal);
    }

    [Test]
    public void ExitChoice_PrefersExactCompassPortOverNearbyPort() {
      var first = ProjectRegressionTests.AddRoom("First");
      var exact = ProjectRegressionTests.AddRoom("Exact");
      var near = ProjectRegressionTests.AddRoom("Near");
      var source = new Location(first, "first");
      var exactTarget = new Location(exact, "exact");
      var nearTarget = new Location(near, "near");
      source.AddExit(new Exit(source, nearTarget, CompassPoint.EastNorthEast, "", new Connection(Project.Current)));
      source.AddExit(new Exit(source, exactTarget, CompassPoint.East, "", new Connection(Project.Current)));
      source.PickBestExits();
      source.GetBestExit(MappableDirection.East).Target.ShouldBeSameAs(exactTarget);
      source.GetBestExit(MappableDirection.West).ShouldBeNull();
    }

    [TestCase("f", Thing.ThingGender.Female)]
    [TestCase("m", Thing.ThingGender.Male)]
    [TestCase("p", Thing.ThingGender.Neuter)]
    public void ObjectFlags_DefinePeopleAndReportInvalidCombinations(string flag, Thing.ThingGender gender) {
      var person = new Thing("Person", "person", null, null, 0, flag + "!");
      person.IsPerson.ShouldBeTrue();
      person.Gender.ShouldBe(gender);
      person.ProperNamed.ShouldBeTrue();
      person.WarningText.ShouldBeEmpty();
      new Thing("Invalid", "invalid", null, null, 0, "fm12q").WarningText.ShouldContain("invalid character");
    }
  }
}
