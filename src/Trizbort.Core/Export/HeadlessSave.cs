using System;
using System.IO;
using Trizbort.Domain.Application;

namespace Trizbort.Export;

/// <summary>The <c>--quicksave</c> command-line flag (ticket 107): save the map to a named file without any UI.</summary>
public static class HeadlessSave
{
  /// <summary>
  ///   Saves <paramref name="project" /> to <paramref name="path" />, makes that its file name and marks it not dirty.
  ///   Returns null on success, otherwise the error message. The original file name is restored on failure.
  /// </summary>
  public static string QuickSave(Project project, string path)
  {
    if (project == null) return "No map is loaded.";
    if (string.IsNullOrWhiteSpace(path)) return "No file name was given.";

    var original = project.FileName;
    try
    {
      var full = Path.GetFullPath(path);
      if (!string.Equals(Path.GetExtension(full), ".trizbort", StringComparison.Ordinal))
        full = Path.ChangeExtension(full, ".trizbort");
      var directory = Path.GetDirectoryName(full);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        throw new DirectoryNotFoundException("Folder not found: " + directory);

      project.FileName = full;
      if (project.Save()) return null;
      project.FileName = original;
      return "Failed to save the map to " + full;
    }
    catch (Exception ex)
    {
      project.FileName = original;
      return ex.Message;
    }
  }
}
