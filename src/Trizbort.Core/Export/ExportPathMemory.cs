using System;
using System.IO;
using Trizbort.Domain.AppSettings;
using Trizbort.Export.Languages;

namespace Trizbort.Export;

/// <summary>
///   Remembers, per export language, the last file written (stored in the <c>LastExport*FileName</c> settings).
/// </summary>
public static class ExportPathMemory
{
  /// <summary>Returns the remembered folder, file name (without extension) and extension (with dot), or empty strings.</summary>
  public static (string Folder, string FileName, string Extension) Get(ExportLanguage language)
  {
    return Split(GetPath(ApplicationSettingsController.AppSettings, language));
  }

  public static void Remember(ExportLanguage language, string path)
  {
    if (string.IsNullOrEmpty(path)) return;
    SetPath(ApplicationSettingsController.AppSettings, language, path);
  }

  public static (string Folder, string FileName, string Extension) Split(string path)
  {
    if (string.IsNullOrEmpty(path)) return (string.Empty, string.Empty, string.Empty);
    return (
      Path.GetDirectoryName(path) ?? string.Empty,
      Path.GetFileNameWithoutExtension(path),
      Path.GetExtension(path));
  }

  public static string GetPath(ApplicationSettings settings, ExportLanguage language)
  {
    var value = language switch
    {
      ExportLanguage.Alan => settings.LastExportAlanFileName,
      ExportLanguage.Adventuron => settings.LastExportAdventuronFileName,
      ExportLanguage.Hugo => settings.LastExportHugoFileName,
      ExportLanguage.Inform6 => settings.LastExportInform6FileName,
      ExportLanguage.Inform7 => settings.LastExportInform7FileName,
      ExportLanguage.Tads => settings.LastExportTadsFileName,
      ExportLanguage.Quest => settings.LastExportQuestFileName,
      ExportLanguage.QuestRooms => settings.LastExportQuestRoomsFileName,
      ExportLanguage.Zil => settings.LastExportZilFileName,
      _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
    };
    return value ?? string.Empty;
  }

  public static void SetPath(ApplicationSettings settings, ExportLanguage language, string path)
  {
    switch (language)
    {
      case ExportLanguage.Alan: settings.LastExportAlanFileName = path; break;
      case ExportLanguage.Adventuron: settings.LastExportAdventuronFileName = path; break;
      case ExportLanguage.Hugo: settings.LastExportHugoFileName = path; break;
      case ExportLanguage.Inform6: settings.LastExportInform6FileName = path; break;
      case ExportLanguage.Inform7: settings.LastExportInform7FileName = path; break;
      case ExportLanguage.Tads: settings.LastExportTadsFileName = path; break;
      case ExportLanguage.Quest: settings.LastExportQuestFileName = path; break;
      case ExportLanguage.QuestRooms: settings.LastExportQuestRoomsFileName = path; break;
      case ExportLanguage.Zil: settings.LastExportZilFileName = path; break;
      default: throw new ArgumentOutOfRangeException(nameof(language), language, null);
    }
  }

  /// <summary>Maps an exporter instance to its language; false for an unknown exporter type.</summary>
  public static bool TryGetLanguage(CodeExporter exporter, out ExportLanguage language)
  {
    language = ExportLanguage.Inform7;
    switch (exporter)
    {
      case AlanExporter: language = ExportLanguage.Alan; return true;
      case AdventuronExporter: language = ExportLanguage.Adventuron; return true;
      case HugoExporter: language = ExportLanguage.Hugo; return true;
      case Inform6Exporter: language = ExportLanguage.Inform6; return true;
      case Inform7Exporter: language = ExportLanguage.Inform7; return true;
      case TadsExporter: language = ExportLanguage.Tads; return true;
      // QuestExporter derives from QuestRoomsExporter, so test it first.
      case QuestExporter: language = ExportLanguage.Quest; return true;
      case QuestRoomsExporter: language = ExportLanguage.QuestRooms; return true;
      case ZilExporter: language = ExportLanguage.Zil; return true;
      default: return false;
    }
  }
}
