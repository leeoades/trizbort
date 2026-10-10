using System;
using System.IO;
using System.Xml;
using CommandLine;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Util;

namespace Trizbort.Tests;

[TestFixture]
[Category("Unit")]
public class BoundaryRegressionTests : IsolatedProjectTests
{
  [Test]
  public void CommandLine_ParsesCombinedMapAutomapSaveAndExportOptions()
  {
    using var parser = new Parser(settings => settings.HelpWriter = null);
    var result = parser.ParseArguments<CommandLineOptions>(
      new[] {
        "Trizbort.exe", "map.trizbort", "--automap", "transcript.txt",
        "--quicksave", "saved.trizbort", "--inform7", "story.ni", "--exit"
      });
    result.Tag.ShouldBe(ParserResultType.Parsed);
    var options = ((Parsed<CommandLineOptions>)result).Value;
    options.Executable.ShouldBe("Trizbort.exe");
    options.FileName.ShouldBe("map.trizbort");
    options.Transcript.ShouldBe("transcript.txt");
    options.QuickSave.ShouldBe("saved.trizbort");
    options.I7.ShouldBe("story.ni");
    options.Exit.ShouldBeTrue();
  }

  [TestCase("--not-an-option")]
  public void CommandLine_RejectsUnknownOptions(string argument)
  {
    using var parser = new Parser(settings => settings.HelpWriter = null);
    parser.ParseArguments<CommandLineOptions>(new[] { "Trizbort.exe", argument }).Tag
          .ShouldBe(ParserResultType.NotParsed);
  }

  [Test]
  public void CommandLine_BareAutomapOptionDoesNotSpecifyTranscript()
  {
    using var parser = new Parser(settings => settings.HelpWriter = null);
    var result = parser.ParseArguments<CommandLineOptions>(new[] { "Trizbort.exe", "--automap" });
    result.Tag.ShouldBe(ParserResultType.Parsed);
    string.IsNullOrEmpty(((Parsed<CommandLineOptions>)result).Value.Transcript).ShouldBeTrue();
  }

  [TestCase(".trizbort", true)]
  [TestCase(".txt", false)]
  [TestCase(".png", false)]
  public void MapSaver_WritesOnlySupportedMapExtension(string extension, bool supported)
  {
    var path = Files.File("map" + extension);
    new MapSaver(Project.Current).SaveMap(path).ShouldBe(supported);
    File.Exists(path).ShouldBe(supported);
  }

  [Test]
  public void MapLoader_RejectsUnknownExtensionWithExplicitNotification()
  {
    var path = Files.File("map.txt");
    File.WriteAllText(path, "<trizbort version=\"1.0\"><map/></trizbort>");
    string reported = null;
    var versionChecks = 0;
    var engine = new LegacyMapFileEngine(
      Project.Current,
      error => throw new AssertionException(error.ToString()),
      loaded => {
        loaded.Version.ShouldBe(new Version(1, 0));
        versionChecks++;
      },
      (message, title) => throw new AssertionException(title + ": " + message));
    var loader = new MapLoader(Project.Current, message => reported = message, engine);
    loader.LoadMap(path).ShouldBeFalse();
    reported.ShouldContain("not a known Trizbort file");
    Project.Current.Elements.ShouldBeEmpty();
    versionChecks.ShouldBe(0);
    var supported = Files.File("map.trizbort");
    File.Copy(path, supported);
    reported = null;
    loader.LoadMap(supported).ShouldBeTrue();
    reported.ShouldBeNull();
    versionChecks.ShouldBe(1);
  }

  [Test]
  public void MapLoader_SupportedMalformedDocumentReportsEngineErrorWithoutDialog()
  {
    var path = Files.File("malformed.trizbort");
    File.WriteAllText(path, "<trizbort>");
    Exception reported = null;
    var engine = new LegacyMapFileEngine(
      Project.Current,
      error => reported = error,
      _ => throw new AssertionException("Malformed XML must not reach version checking"),
      (message, title) => throw new AssertionException(title + ": " + message));
    new MapLoader(Project.Current, message => throw new AssertionException(message), engine)
      .LoadMap(path).ShouldBeFalse();
    reported.ShouldBeOfType<XmlException>();
  }

  [Test]
  public void XmlReaders_ApplyLegacyDefaultsAndParseInvariantValues()
  {
    var document = new XmlDocument();
    document.LoadXml(
      "<root valid=\"12.5\" invalid=\"not-a-number\" yes=\"YeS\" no=\"NO\"><child>Text &amp; content</child></root>");
    var reader = new XmlElementReader(document.DocumentElement);
    reader.Attribute("valid").ToFloat().ShouldBe(12.5f);
    reader.Attribute("invalid").ToFloat(7).ShouldBe(7);
    reader.Attribute("missing").ToInt(42).ShouldBe(42);
    reader.Attribute("yes").ToBool().ShouldBeTrue();
    reader.Attribute("no").ToBool(true).ShouldBeFalse();
    reader["child"].Text.ShouldBe("Text & content");
    reader["absent"]["nested"].Text.ShouldBeEmpty();
    reader["absent"].Children.ShouldBeEmpty();
    new XmlAttributeReader("SW").ToCompassPoint(CompassPoint.North).ShouldBe(CompassPoint.SouthWest);
  }

  [Test]
  public void ConnectionMoveablePorts_CanDockUndockAndNotifyGeometryChanges()
  {
    var room = ProjectRegressionTests.AddRoom("Room");
    var line = new Connection(Project.Current, new Vertex(new Vector(-200, -100)), new Vertex(new Vector(200, 100)));
    var port = (MoveablePort)line.PortList[0];
    var changes = 0;
    line.Changed += (_, __) => changes++;
    port.DockAt(room.PortAt(CompassPoint.North));
    port.DockedAt.ShouldBeSameAs(room.PortAt(CompassPoint.North));
    line.VertexList[0].Position.ShouldBe(room.PortAt(CompassPoint.North).Position);
    port.SetPosition(new Vector(75, -90));
    port.DockedAt.ShouldBeNull();
    line.VertexList[0].Position.ShouldBe(new Vector(75, -90));
    changes.ShouldBeGreaterThanOrEqualTo(2);
  }
}