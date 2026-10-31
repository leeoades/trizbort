using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Integration")]
public class CodeExportServiceTests : IsolatedProjectTests
{
  private static IEnumerable<ExportLanguage> Languages => Enum.GetValues(typeof(ExportLanguage)).Cast<ExportLanguage>();

  private static string Normalize(string text)
  {
    // Quest writes a fresh game id on every export.
    return Regex.Replace(text, "<gameid>[^<]*</gameid>", "<gameid>x</gameid>");
  }

  private static void BuildMap()
  {
    var first = ProjectRegressionTests.AddRoom("Observatory");
    first.IsStartRoom = true;
    var second = ProjectRegressionTests.AddRoom("Gallery");
    ProjectRegressionTests.Connect(first, second);
  }

  [TestCaseSource(nameof(Languages))]
  public void ExportToFile_MatchesExportToText(ExportLanguage language)
  {
    BuildMap();
    var text = CodeExportService.ExportToText(language, Project.Current);
    text.ShouldNotBeNullOrEmpty();
    text.ToLowerInvariant().ShouldContain("observatory");

    var path = Files.File("out" + (language == ExportLanguage.Inform7 ? ".ni" : CodeExportService.DefaultExtension(language)));
    CodeExportService.ExportToFile(language, Project.Current, path);
    var written = Encoding.ASCII.GetString(File.ReadAllBytes(path));
    Normalize(written).ShouldBe(Normalize(text));
  }

  [TestCaseSource(nameof(Languages))]
  public void EveryLanguage_HasCreatorAndMetadata(ExportLanguage language)
  {
    using var exporter = CodeExportService.Create(language);
    exporter.ShouldNotBeNull();
    CodeExportService.DisplayName(language).ShouldNotBeNullOrEmpty();
    CodeExportService.DefaultExtension(language).ShouldStartWith(".");
  }

  [Test]
  public void QuestVariants_AreDistinct()
  {
    BuildMap();
    var full = CodeExportService.ExportToText(ExportLanguage.Quest, Project.Current);
    var rooms = CodeExportService.ExportToText(ExportLanguage.QuestRooms, Project.Current);
    full.ShouldContain("<asl");
    rooms.ShouldNotContain("<asl");
  }

  [Test]
  public void ExportToText_RestoresCurrentProject()
  {
    BuildMap();
    var current = Project.Current;
    var other = new Project();
    CodeExportService.ExportToText(ExportLanguage.Inform6, other);
    Project.Current.ShouldBeSameAs(current);
  }

  [Test]
  public void Inform7_InformBundlePath_CreatesStoryNi()
  {
    BuildMap();
    var bundle = Files.File("Game.inform");
    CodeExportService.ExportToFile(ExportLanguage.Inform7, Project.Current, bundle);
    File.Exists(Path.Combine(bundle, "Source", "Story.ni")).ShouldBeTrue();
  }
}
