using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Export;
using Trizbort.Export.Languages;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class ExportPathMemoryTests
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

  [SetUp]
  public void SetUp()
  {
    _saved = Snapshot();
  }

  [TearDown]
  public void TearDown()
  {
    Restore(_saved);
  }

  [Test]
  public void Get_NothingRemembered_ReturnsEmpty()
  {
    var result = ExportPathMemory.Get(ExportLanguage.Hugo);
    result.Folder.ShouldBe(string.Empty);
    result.FileName.ShouldBe(string.Empty);
    result.Extension.ShouldBe(string.Empty);
  }

  [Test]
  public void Remember_ThenGet_SplitsPath()
  {
    var path = Path.Combine(Path.GetTempPath(), "games", "cave.ni");
    ExportPathMemory.Remember(ExportLanguage.Inform7, path);

    var result = ExportPathMemory.Get(ExportLanguage.Inform7);
    result.Folder.ShouldBe(Path.Combine(Path.GetTempPath(), "games"));
    result.FileName.ShouldBe("cave");
    result.Extension.ShouldBe(".ni");
  }

  [Test]
  public void Remember_IsPerLanguage()
  {
    ExportPathMemory.Remember(ExportLanguage.Quest, Path.Combine("a", "q.aslx"));
    ExportPathMemory.Get(ExportLanguage.QuestRooms).FileName.ShouldBe(string.Empty);
    ExportPathMemory.Get(ExportLanguage.Quest).FileName.ShouldBe("q");
  }

  [Test]
  public void AllNineLanguages_RoundTripThroughJson()
  {
    foreach (var language in CodeExportService.AllLanguages)
      ExportPathMemory.Remember(language, Path.Combine("dir", language + ".x"));

    var json = JsonConvert.SerializeObject(ApplicationSettingsController.AppSettings);
    var loaded = JsonConvert.DeserializeObject<ApplicationSettings>(json);

    var count = 0;
    foreach (var language in CodeExportService.AllLanguages)
    {
      ExportPathMemory.GetPath(loaded, language).ShouldBe(Path.Combine("dir", language + ".x"));
      count++;
    }
    count.ShouldBe(9);
  }

  [Test]
  public void TryGetLanguage_QuestExporterIsNotQuestRooms()
  {
    using var quest = new QuestExporter();
    using var rooms = new QuestRoomsExporter();
    ExportPathMemory.TryGetLanguage(quest, out var a).ShouldBeTrue();
    ExportPathMemory.TryGetLanguage(rooms, out var b).ShouldBeTrue();
    a.ShouldBe(ExportLanguage.Quest);
    b.ShouldBe(ExportLanguage.QuestRooms);
  }
}
