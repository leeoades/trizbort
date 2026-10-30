using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Trizbort.Domain.Application;

namespace Trizbort.Editor;

/// <summary>Handlers for the Map Statistics and Map Statistic Export commands (ticket 087).</summary>
public static class MapStatisticsCommands
{
  public const string StatisticsTitle = "Map Statistics";

  public const string ExportTitle = "Map Statistic Export";

  public const string WrittenTitle = "Log file written";

  /// <summary>
  ///   The suggested log path: the map's path with a <c>.log</c> extension, or <c>trizbort.log</c> in the user's
  ///   home folder for an unsaved map.
  /// </summary>
  public static string DefaultLogPath(Project project)
  {
    if (project != null && project.HasFileName) return Path.ChangeExtension(project.FileName, ".log");

    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "trizbort.log");
  }

  public static Task ShowAsync(EditorSession session, MapDimensions dimensions)
  {
    return session.Prompts.ShowTextReportAsync(StatisticsTitle, MapStatisticsReport.Build(dimensions));
  }

  public static async Task ExportAsync(EditorSession session, MapDimensions dimensions)
  {
    var text = MapStatisticsReport.Build(dimensions);
    var path = await session.Prompts.PickSaveFileAsync(ExportTitle, DefaultLogPath(session.Project), ".log");
    if (string.IsNullOrEmpty(path)) return;

    try
    {
      File.WriteAllText(path, text, new UTF8Encoding(false));
    }
    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
    {
      await session.Prompts.MessageAsync($"Unable to write the log file:\n\n{path}\n\n{ex.Message}", ExportTitle);
      return;
    }

    await session.Prompts.MessageAsync("Wrote log to " + path, WrittenTitle);
  }
}
