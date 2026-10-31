using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Editor;

/// <summary>The output formats Smart Save can write next to the saved map.</summary>
public enum SmartSaveKind
{
  Pdf,
  Image
}

/// <summary>
///   Writes one Smart Save output next to the map file. Implementations for PDF and further image formats (ticket 110)
///   are added by registering them; Smart Save itself does not change.
/// </summary>
public interface ISmartSaveExporter
{
  SmartSaveKind Kind { get; }

  /// <summary>Writes the output for the map saved at <paramref name="mapFileName" /> and returns the path written. Throws on failure.</summary>
  Task<string> ExportAsync(EditorSession session, string mapFileName, CancellationToken token = default);
}

/// <summary>File &gt; Smart Save (ticket 059): save the map, then write the PDF and/or image that the settings ask for.</summary>
public static class SmartSave
{
  public const string Title = "Smart Save";

  /// <summary>
  ///   Runs Smart Save. <paramref name="save" /> saves the map (its argument asks for Save As) and is only called after
  ///   the user agreed; success is judged from the project afterwards. Returns true when every enabled output was written.
  /// </summary>
  public static async Task<bool> RunAsync(
    EditorSession session, ApplicationSettings settings, IReadOnlyList<ISmartSaveExporter> exporters,
    Func<bool, Task> save, CancellationToken token = default)
  {
    var prompts = session.Prompts;
    if (!settings.SaveToPDF && !settings.SaveToImage)
    {
      await prompts.ShowMessageAsync(
        Title,
        "Your settings are set to not save anything. Please check your App Settings if this is not what you want.");
      return false;
    }

    var project = session.Project;
    var saved = true;
    if (RecentFiles.IsUrl(project.FileName ?? string.Empty) || !project.HasFileName || project.IsDirty)
    {
      saved = false;
      var yes = await prompts.ConfirmAsync(
        "Your project needs to be saved before we can do a SmartSave.  Would you like to save the project now?",
        "Save Project?");
      if (yes)
      {
        await save(RecentFiles.IsUrl(project.FileName ?? string.Empty));
        project = session.Project;
        saved = project.HasFileName && !RecentFiles.IsUrl(project.FileName) && !project.IsDirty;
      }
    }

    if (!saved)
    {
      await prompts.ShowMessageAsync(Title, "No files have been saved during the SmartSave.");
      return false;
    }

    var lines = new List<string>();
    var failed = false;
    var wanted = new List<SmartSaveKind>();
    if (settings.SaveToPDF) wanted.Add(SmartSaveKind.Pdf);
    if (settings.SaveToImage) wanted.Add(SmartSaveKind.Image);

    foreach (var kind in wanted)
    {
      var name = kind == SmartSaveKind.Pdf ? "PDF" : "Image";
      var exporter = exporters == null ? null : exporters.FirstOrDefault(e => e.Kind == kind);
      if (exporter == null)
      {
        await prompts.ShowMessageAsync(Title, $"The {name} file could not be saved during the SmartSave: {name} export is not available yet.");
        failed = true;
        continue;
      }

      try
      {
        var path = await exporter.ExportAsync(session, project.FileName, token);
        lines.Add($"{name} file has been saved to {path}");
      }
      catch (Exception ex)
      {
        await prompts.ShowMessageAsync(
          Title,
          $"There was an error saving the {name} file during the SmartSave.  Please make sure the {name} is not already opened.\n{ex.Message}");
        failed = true;
      }
    }

    if (!failed) await prompts.ShowMessageAsync(Title, string.Join(Environment.NewLine, lines));
    return !failed;
  }
}
