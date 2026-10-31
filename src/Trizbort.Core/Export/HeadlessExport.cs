using System;
using System.Collections.Generic;
using System.IO;
using Trizbort.Domain.Application;

namespace Trizbort.Export;

/// <summary>The outcome of one export flag: Error is null on success.</summary>
public sealed record HeadlessExportResult(string Flag, ExportLanguage Language, string File, string Error);

/// <summary>Runs the code export command-line flags (--inform6, --inform7, --tads and so on) without any UI.</summary>
public static class HeadlessExport
{
  /// <summary>True when any export flag has a file name.</summary>
  public static bool HasExportFlags(CommandLineOptions options)
  {
    return options != null && Plan(options).Count > 0;
  }

  /// <summary>Exports <paramref name="project" /> once per non-empty flag. Never throws for export failures.</summary>
  public static IReadOnlyList<HeadlessExportResult> Run(CommandLineOptions options, Project project)
  {
    var results = new List<HeadlessExportResult>();
    if (options == null) return results;

    foreach (var (flag, language, file) in Plan(options))
    {
      string error = null;
      try
      {
        if (project == null) throw new InvalidOperationException("No map is loaded.");
        var directory = Path.GetDirectoryName(Path.GetFullPath(file));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          throw new DirectoryNotFoundException("Folder not found: " + directory);
        CodeExportService.ExportToFile(language, project, file);
      }
      catch (Exception ex)
      {
        error = ex.Message;
      }
      results.Add(new HeadlessExportResult(flag, language, file, error));
    }

    return results;
  }

  private static List<(string Flag, ExportLanguage Language, string File)> Plan(CommandLineOptions o)
  {
    var plan = new List<(string, ExportLanguage, string)>();
    Add(plan, "inform6", ExportLanguage.Inform6, o.I6);
    Add(plan, "inform7", ExportLanguage.Inform7, o.I7);
    Add(plan, "tads", ExportLanguage.Tads, o.Tads);
    Add(plan, "alan", ExportLanguage.Alan, o.Alan);
    Add(plan, "hugo", ExportLanguage.Hugo, o.Hugo);
    Add(plan, "zil", ExportLanguage.Zil, o.Zil);
    Add(plan, "quest", ExportLanguage.Quest, o.Quest);
    Add(plan, "questrooms", ExportLanguage.QuestRooms, o.QuestRooms);
    Add(plan, "adventuron", ExportLanguage.Adventuron, o.Adventuron);
    return plan;
  }

  private static void Add(List<(string, ExportLanguage, string)> plan, string flag, ExportLanguage language, string file)
  {
    if (!string.IsNullOrWhiteSpace(file)) plan.Add((flag, language, file));
  }
}
