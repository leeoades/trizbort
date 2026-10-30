using System;
using System.IO;
using System.Linq;
using Trizbort.Domain.Application;

namespace Trizbort.Export;

public static class ExportNaming
{
  /// <summary>Remembered file name for the language, else a sanitised project title / file stem, plus the default extension.</summary>
  public static string SuggestedFileName(Project project, ExportLanguage language)
  {
    var extension = CodeExportService.DefaultExtension(language);
    var remembered = ExportPathMemory.Get(language);
    if (!string.IsNullOrEmpty(remembered.FileName))
      return remembered.FileName + (string.IsNullOrEmpty(remembered.Extension) ? extension : remembered.Extension);

    var stem = project?.Title;
    if (string.IsNullOrWhiteSpace(stem) && project != null && !string.IsNullOrEmpty(project.FileName))
      stem = Path.GetFileNameWithoutExtension(project.FileName);
    stem = Sanitise(stem);
    if (stem.Length == 0) stem = "map";
    return stem + extension;
  }

  public static string Sanitise(string name)
  {
    if (string.IsNullOrWhiteSpace(name)) return string.Empty;
    var invalid = Path.GetInvalidFileNameChars();
    var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    return cleaned.Trim().TrimEnd('.');
  }
}
