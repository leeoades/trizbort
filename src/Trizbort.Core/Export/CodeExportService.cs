using System;
using System.Collections.Generic;
using Trizbort.Domain.Application;
using Trizbort.Export.Languages;

namespace Trizbort.Export;

public enum ExportLanguage
{
  Alan,
  Adventuron,
  Hugo,
  Inform6,
  Inform7,
  Tads,
  Quest,
  QuestRooms,
  Zil
}

/// <summary>
///   Single entry point for driving any code exporter: create one, write it to a file or produce its text.
/// </summary>
public static class CodeExportService
{
  private sealed class LanguageInfo
  {
    public LanguageInfo(Func<CodeExporter> factory, string displayName, string defaultExtension)
    {
      Factory = factory;
      DisplayName = displayName;
      DefaultExtension = defaultExtension;
    }

    public Func<CodeExporter> Factory { get; }
    public string DisplayName { get; }
    public string DefaultExtension { get; }
  }

  private static readonly Dictionary<ExportLanguage, LanguageInfo> _languages = new() {
    [ExportLanguage.Alan] = new LanguageInfo(() => new AlanExporter(), "Alan", ".i"),
    [ExportLanguage.Adventuron] = new LanguageInfo(() => new AdventuronExporter(), "Adventuron", ".adv"),
    [ExportLanguage.Hugo] = new LanguageInfo(() => new HugoExporter(), "Hugo", ".hug"),
    [ExportLanguage.Inform6] = new LanguageInfo(() => new Inform6Exporter(), "Inform 6", ".inf"),
    [ExportLanguage.Inform7] = new LanguageInfo(() => new Inform7Exporter(), "Inform 7", ".inform"),
    [ExportLanguage.Tads] = new LanguageInfo(() => new TadsExporter(), "TADS", ".t"),
    [ExportLanguage.Quest] = new LanguageInfo(() => new QuestExporter(), "Quest", ".aslx"),
    [ExportLanguage.QuestRooms] = new LanguageInfo(() => new QuestRoomsExporter(), "Quest (rooms only)", ".aslx"),
    [ExportLanguage.Zil] = new LanguageInfo(() => new ZilExporter(), "ZIL", ".zil")
  };

  public static IEnumerable<ExportLanguage> AllLanguages => _languages.Keys;

  public static CodeExporter Create(ExportLanguage language)
  {
    return Get(language).Factory();
  }

  public static string DisplayName(ExportLanguage language)
  {
    return Get(language).DisplayName;
  }

  public static string DefaultExtension(ExportLanguage language)
  {
    return Get(language).DefaultExtension;
  }

  /// <summary>
  ///   Exports the project to a string. Exporters read <see cref="Project.Current" />, so it is swapped to
  ///   <paramref name="project" /> for the duration of the call and then restored.
  /// </summary>
  public static string ExportToText(ExportLanguage language, Project project)
  {
    if (project == null) throw new ArgumentNullException(nameof(project));
    var previous = Project.Current;
    try
    {
      Project.Current = project;
      using var exporter = Create(language);
      return exporter.Export();
    }
    finally
    {
      Project.Current = previous;
    }
  }

  /// <summary>
  ///   Exports the project to a file. For Inform 7, a path ending in <c>.inform</c> writes
  ///   <c>Source/Story.ni</c> inside that bundle.
  /// </summary>
  public static void ExportToFile(ExportLanguage language, Project project, string path)
  {
    if (project == null) throw new ArgumentNullException(nameof(project));
    if (string.IsNullOrEmpty(path)) throw new ArgumentException("A file path is required.", nameof(path));
    var previous = Project.Current;
    try
    {
      Project.Current = project;
      using var exporter = Create(language);
      exporter.Export(path);
    }
    finally
    {
      Project.Current = previous;
    }
  }

  private static LanguageInfo Get(ExportLanguage language)
  {
    if (!_languages.TryGetValue(language, out var info))
      throw new ArgumentOutOfRangeException(nameof(language), language, null);
    return info;
  }
}
