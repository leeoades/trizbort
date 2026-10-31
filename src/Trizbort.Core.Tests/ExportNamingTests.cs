using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class ExportNamingTests
{
  private System.Collections.Generic.Dictionary<ExportLanguage, string> _saved;

  private static System.Collections.Generic.Dictionary<ExportLanguage, string> Snapshot()
  {
    var saved = new System.Collections.Generic.Dictionary<ExportLanguage, string>();
    foreach (var language in CodeExportService.AllLanguages)
    {
      saved[language] = ExportPathMemory.GetPath(ApplicationSettingsController.AppSettings, language);
      ExportPathMemory.SetPath(ApplicationSettingsController.AppSettings, language, null);
    }
    return saved;
  }

  private static void Restore(System.Collections.Generic.Dictionary<ExportLanguage, string> saved)
  {
    foreach (var pair in saved)
      ExportPathMemory.SetPath(ApplicationSettingsController.AppSettings, pair.Key, pair.Value);
  }
  private Project _project;

  [SetUp]
  public void SetUp()
  {
    _saved = Snapshot();
    _project = new Project();
  }

  [TearDown]
  public void TearDown()
  {
    Restore(_saved);
    _project.Dispose();
  }

  [Test]
  public void UsesTitleAndDefaultExtension()
  {
    _project.Title = "Cave Game";
    ExportNaming.SuggestedFileName(_project, ExportLanguage.Inform7).ShouldBe("Cave Game.inform");
    ExportNaming.SuggestedFileName(_project, ExportLanguage.Tads).ShouldBe("Cave Game.t");
  }

  [Test]
  public void FallsBackToFileStemThenMap()
  {
    _project.Title = string.Empty;
    _project.FileName = System.IO.Path.Combine("x", "castle.trizbort");
    ExportNaming.SuggestedFileName(_project, ExportLanguage.Hugo).ShouldBe("castle.hug");

    _project.FileName = string.Empty;
    ExportNaming.SuggestedFileName(_project, ExportLanguage.Hugo).ShouldBe("map.hug");
  }

  [Test]
  public void SanitisesInvalidCharacters()
  {
    _project.Title = "a/b";
    ExportNaming.SuggestedFileName(_project, ExportLanguage.Zil).ShouldBe("a_b.zil");
  }

  [Test]
  public void RememberedNameWins()
  {
    _project.Title = "Ignored";
    ExportPathMemory.Remember(ExportLanguage.Inform6, System.IO.Path.Combine("d", "last.inf"));
    ExportNaming.SuggestedFileName(_project, ExportLanguage.Inform6).ShouldBe("last.inf");
  }
}
