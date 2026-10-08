using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Tests {
  [TestFixture, Category("Unit")]
  public class DocumentVersionTests {
    [TestCase("2.3.4.5", "None")]
    [TestCase("1.9.9.9", "None")]
    [TestCase("3.0.0.0", "Major")]
    [TestCase("2.2.99.99", "None")]
    [TestCase("2.4.0.0", "Minor")]
    [TestCase("2.3.3.99", "None")]
    [TestCase("2.3.5.0", "Build")]
    [TestCase("2.3.4.4", "None")]
    [TestCase("2.3.4.6", "Revision")]
    [TestCase("0.0.0.0", "None")]
    public void VersionPolicy_PreservesPrecedenceAndWarningSeverity(string document, string warning) {
      DocumentVersionPolicy.Compare(Version.Parse(document), new Version(2, 3, 4, 5)).ToString().ShouldBe(warning);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("invalid")]
    [TestCase("-1.0.0.0")]
    public void InvalidVersion_UsesExistingLegacyZeroVersion(string version) {
      using (var project = new Project()) {
        project.SetVersion(version);
        project.Version.ShouldBe(new Version(0, 0, 0, 0));
      }
    }

    [Test]
    public void PythonVersionScenarios_CheckEachComponentAroundActualApplicationVersion() {
      var current = typeof(Project).Assembly.GetName().Version;
      var parts = new[] {current.Major, current.Minor, current.Build, current.Revision};
      for (var index = 0; index < parts.Length; index++) {
        var changed = (int[]) parts.Clone();
        changed[index]++;
        DocumentVersionPolicy.Compare(new Version(changed[0], changed[1], changed[2], changed[3]), current)
          .ShouldBe((DocumentVersionWarning) (index + 1));
        if (parts[index] == 0) continue;
        changed[index] -= 2;
        DocumentVersionPolicy.Compare(new Version(changed[0], changed[1], changed[2], changed[3]), current)
          .ShouldBe(DocumentVersionWarning.None);
      }
    }
  }

  [TestFixture, Category("Integration")]
  public class PersistenceRegressionTests : IsolatedProjectTests {
    internal static string FixtureDirectory => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Manual");

    internal static void Load(Project project, string path) {
      Exception failure = null;
      new LegacyMapFileEngine(project, error => failure = error, _ => { },
        (message, title) => TestContext.Out.WriteLine(title + ": " + message)).Load(path).ShouldBeTrue(failure?.ToString());
    }

    private static IEnumerable<string> ManualMaps() => Directory.GetFiles(FixtureDirectory, "*.trizbort")
      .Select(Path.GetFileName).OrderBy(name => name);

    [TestCaseSource(nameof(ManualMaps))]
    public void LegacyManualMap_LoadSaveReloadPreservesCanonicalContent(string fixture) {
      Load(Project.Current, Path.Combine(FixtureDirectory, fixture));
      var engine = new LegacyMapFileEngine(Project.Current);
      engine.Save(Files.File("first.trizbort")).ShouldBeTrue();
      using (var reloaded = new Project()) {
        Load(reloaded, Files.File("first.trizbort"));
        new LegacyMapFileEngine(reloaded).Save(Files.File("second.trizbort")).ShouldBeTrue();
        XNode.DeepEquals(XDocument.Load(Files.File("first.trizbort")), XDocument.Load(Files.File("second.trizbort"))).ShouldBeTrue(fixture);
        reloaded.Elements.Count.ShouldBe(Project.Current.Elements.Count);
        foreach (var connection in reloaded.Elements.OfType<Connection>())
          foreach (var vertex in connection.VertexList.Where(vertex => vertex.Port != null)) {
            reloaded.Elements.ShouldContain(vertex.Port.Owner);
            vertex.Port.Owner.ShouldNotBeSameAs(Project.Current.Elements.Single(element => element.ID == vertex.Port.Owner.ID));
          }
      }
    }

    [TestCase("en-US")]
    [TestCase("fr-FR")]
    [TestCase("de-DE")]
    public void RichDocument_RoundTripsMetadataGeometryRegionsAndGraphAcrossCultures(string culture) {
      var previous = Thread.CurrentThread.CurrentCulture;
      try {
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        Project.Current.Title = "Title <&> \u00e9";
        Project.Current.Author = "Author";
        Project.Current.History = "History\nsecond line";
        Project.Current.Description = "Description";
        Settings.GridSize = 17.5f;
        Settings.Regions.Add(new Trizbort.Domain.Misc.Region {RegionName = "Ice & Snow", RColor = Color.LightBlue, TextColor = Color.Navy});
        var first = ProjectRegressionTests.AddRoom("First <&> \u03a9");
        first.Position = new Vector(-12.5f, 31.25f);
        first.Size = new Vector(123.5f, 61.25f);
        first.AddDescription("First description\nsecond line");
        first.Objects = "Chest [c]\n  Key";
        first.Region = "Ice & Snow";
        first.IsStartRoom = true;
        first.IsDark = true;
        var second = ProjectRegressionTests.AddRoom("Second");
        var connection = ProjectRegressionTests.Connect(first, second);
        connection.Name = "Door";
        connection.Door = new Door {Lockable = true, Locked = true, Openable = true};
        connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(45.5f, -13.25f));
        var label = new MapLabel(Project.Current) {Text = "Note\n<&>", Position = new Vector(250, 100)};
        Project.Current.Elements.Add(label);
        Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(second.PortAt(CompassPoint.East)), new Vertex(label.PortList[0])));
        new LegacyMapFileEngine(Project.Current).Save(Files.File("rich.trizbort")).ShouldBeTrue();
        using (var loaded = new Project()) {
          Load(loaded, Files.File("rich.trizbort"));
          loaded.Title.ShouldBe(Project.Current.Title);
          loaded.Author.ShouldBe("Author");
          loaded.History.Replace("\r\n", "\n").ShouldBe("History\nsecond line");
          loaded.Description.ShouldBe("Description");
          var room = loaded.Elements.OfType<Room>().Single(item => item.Name == first.Name);
          room.Position.ShouldBe(first.Position);
          room.Size.ShouldBe(first.Size);
          room.PrimaryDescription.ShouldBe(first.PrimaryDescription);
          room.Objects.Replace("\r\n", "\n").ShouldBe(first.Objects);
          room.Region.ShouldBe("Ice & Snow");
          room.IsStartRoom.ShouldBeTrue();
          room.IsDark.ShouldBeTrue();
          var line = loaded.Elements.OfType<Connection>().Single(item => item.Name == "Door");
          line.GetSourceRoom().ShouldBeSameAs(room);
          line.Door.Locked.ShouldBeTrue();
          line.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(45.5f, -13.25f));
          loaded.Elements.OfType<MapLabel>().Single().Text.ShouldBe("Note\n<&>");
          Settings.GridSize.ShouldBe(17.5f);
          Settings.Regions.Single(region => region.RegionName == "Ice & Snow").TextColor.ToArgb().ShouldBe(Color.Navy.ToArgb());
        }
      } finally {
        Thread.CurrentThread.CurrentCulture = previous;
      }
    }

    [Test]
    public void LinesBeforeRooms_ResolvePortsInSecondPass() {
      File.WriteAllText(Files.File("order.trizbort"),
        "<trizbort version=\"1.0\"><map><line id=\"3\"><dock index=\"0\" id=\"1\" port=\"e\"/><dock index=\"1\" id=\"2\" port=\"w\"/></line>" +
        "<room id=\"1\" name=\"First\"/><room id=\"2\" name=\"Second\"/></map></trizbort>");
      Load(Project.Current, Files.File("order.trizbort"));
      var line = Project.Current.Elements.OfType<Connection>().Single();
      line.GetSourceRoom().Name.ShouldBe("First");
      line.GetTargetRoom().Name.ShouldBe("Second");
      line.IsDangling.ShouldBeFalse();
    }

    [TestCase("<wrong/>", typeof(InvalidDataException))]
    [TestCase("<trizbort>", typeof(System.Xml.XmlException))]
    public void BadDocuments_ReportFailureWithoutBlockingDialog(string xml, Type expected) {
      File.WriteAllText(Files.File("bad.trizbort"), xml);
      Exception reported = null;
      new LegacyMapFileEngine(Project.Current, error => reported = error, _ => { }).Load(Files.File("bad.trizbort")).ShouldBeFalse();
      reported.ShouldBeOfType(expected);
    }

    [Test]
    public void EmptyFile_ResetsMapSettingsAndLoadsBlankDocument() {
      Settings.GridSize = 90;
      File.WriteAllText(Files.File("empty.trizbort"), "");
      Load(Project.Current, Files.File("empty.trizbort"));
      Project.Current.Elements.ShouldBeEmpty();
      Settings.GridSize.ShouldBe(32);
    }

    [TestCase("yes", "no", FontStyle.Bold)]
    [TestCase("no", "yes", FontStyle.Italic)]
    [TestCase("yes", "yes", FontStyle.Bold | FontStyle.Italic)]
    public void FontScriptCases_PreserveStylesAndFallbackForUnavailableFamily(string bold, string italic, FontStyle style) {
      File.WriteAllText(Files.File("font.trizbort"), $"<trizbort version=\"1.0\"><settings><fonts><room size=\"18\" bold=\"{bold}\" italic=\"{italic}\">Trizbort-Test-Font-Does-Not-Exist</room></fonts></settings></trizbort>");
      Load(Project.Current, Files.File("font.trizbort"));
      Settings.RoomNameFont.Style.ShouldBe(style);
      Settings.RoomNameFont.Size.ShouldBe(18);
      Settings.RoomNameFont.Name.ShouldNotBe("Trizbort-Test-Font-Does-Not-Exist");
    }

    [Test]
    public void PerlFixtureAudit_AllBatchReferencesResolve_AndKnownUnassignedMapsAreExplicit() {
      var references = Directory.GetFiles(FixtureDirectory, "*.bat").SelectMany(path => File.ReadLines(path))
        .Where(line => !line.TrimStart().StartsWith("::") && !line.TrimStart().StartsWith("rem ", StringComparison.OrdinalIgnoreCase))
        .Select(line => Regex.Match(line, @"([^\\\s]+\.trizbort)\s*$", RegexOptions.IgnoreCase))
        .Where(match => match.Success).Select(match => match.Groups[1].Value).ToArray();
      foreach (var reference in references) File.Exists(Path.Combine(FixtureDirectory, reference)).ShouldBeTrue(reference);
      var unassigned = ManualMaps().Except(references, StringComparer.OrdinalIgnoreCase).OrderBy(name => name).ToArray();
      unassigned.ShouldBe(new[] {
        "17-by-17.trizbort", "object-testing.trizbort", "region-accent-clash.trizbort", "region-accent-one.trizbort",
        "roomstats-case-insensitive-duplicate-testing.trizbort", "roomstats-duplicate-testing.trizbort"
      });
    }
  }
}
