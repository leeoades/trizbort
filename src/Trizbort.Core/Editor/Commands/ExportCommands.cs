using System;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;
using Trizbort.Export;

namespace Trizbort.Editor.Commands;

/// <summary>
///   The code export commands (tickets 056, 057): the eight File > Export source commands with their gestures, and
///   the File > Export > To Clipboard commands. Both are disabled while the map has no rooms. Export to file is run by
///   the UI host; export to clipboard is done here through <see cref="EditorSession.Clipboard" />. None is an undo step.
/// </summary>
public static class ExportCommands
{
  public const string FileIdPrefix = "file.export.";
  public const string ClipboardIdPrefix = "export.clipboard.";

  /// <summary>True when the session's map has at least one room.</summary>
  public static bool HasRooms(EditorSession session)
  {
    return session != null && session.Project != null && session.Project.Elements.OfType<Room>().Any();
  }

  /// <summary>Command id of the export to file of a language, or null for languages without a file command.</summary>
  public static string FileId(ExportLanguage language)
  {
    switch (language)
    {
      case ExportLanguage.Alan: return FileIdPrefix + "alan";
      case ExportLanguage.Adventuron: return FileIdPrefix + "adventuron";
      case ExportLanguage.Hugo: return FileIdPrefix + "hugo";
      case ExportLanguage.Inform6: return FileIdPrefix + "inform6";
      case ExportLanguage.Inform7: return FileIdPrefix + "inform7";
      case ExportLanguage.Tads: return FileIdPrefix + "tads";
      case ExportLanguage.Quest: return FileIdPrefix + "quest";
      case ExportLanguage.Zil: return FileIdPrefix + "zil";
      default: return null;
    }
  }

  /// <summary>Command id of the export to clipboard of a language.</summary>
  public static string ClipboardId(ExportLanguage language)
  {
    switch (language)
    {
      case ExportLanguage.Alan: return ClipboardIdPrefix + "alan";
      case ExportLanguage.Adventuron: return ClipboardIdPrefix + "adventuron";
      case ExportLanguage.Hugo: return ClipboardIdPrefix + "hugo";
      case ExportLanguage.Inform6: return ClipboardIdPrefix + "inform6";
      case ExportLanguage.Inform7: return ClipboardIdPrefix + "inform7";
      case ExportLanguage.Tads: return ClipboardIdPrefix + "tads";
      case ExportLanguage.Quest: return ClipboardIdPrefix + "quest";
      case ExportLanguage.QuestRooms: return ClipboardIdPrefix + "questRooms";
      case ExportLanguage.Zil: return ClipboardIdPrefix + "zil";
      default: throw new ArgumentOutOfRangeException(nameof(language), language, null);
    }
  }

  /// <summary>
  ///   Generates the source of <paramref name="language" /> for the session's map and puts it on the session clipboard.
  ///   Returns false (and does nothing) when the map has no rooms.
  /// </summary>
  public static async Task<bool> ExportToClipboardAsync(EditorSession session, ExportLanguage language)
  {
    if (!HasRooms(session)) return false;
    var text = CodeExportService.ExportToText(language, session.Project);
    await session.Clipboard.SetTextAsync(text);
    return true;
  }

  /// <summary>
  ///   Registers the file export commands (ids <c>file.export.*</c>, run through <paramref name="fileExport" />) and the
  ///   clipboard export commands. <paramref name="status" /> is told when a clipboard export has finished or failed.
  /// </summary>
  public static void Register(
    CommandCatalogue catalogue, Func<ExportLanguage, Task> fileExport, bool isMacOS, Action<string> status = null)
  {
    var primary = ModifierKeys.Primary;
    var shift = ModifierKeys.Shift;
    var alt = ModifierKeys.Alt;

    // Same order as the Export menu has always had. The PNG image export is registered by the host before this.
    RegisterFile(catalogue, ExportLanguage.Inform7, "Inform _7 Source...", fileExport, Gesture(InputKey.D7, primary | shift));
    RegisterFile(catalogue, ExportLanguage.Inform6, "Inform _6 Source...", fileExport, Gesture(InputKey.D6, primary | shift));
    RegisterFile(catalogue, ExportLanguage.Tads, "_TADS Source...", fileExport, Gesture(InputKey.T, primary | shift));
    RegisterFile(catalogue, ExportLanguage.Hugo, "_Hugo Source...", fileExport, Gesture(InputKey.H, primary | shift));
    // Primary+Shift+Z is Redo, so ZIL uses I (for Infocom).
    RegisterFile(catalogue, ExportLanguage.Zil, "_ZIL Source...", fileExport, Gesture(InputKey.I, primary | shift));
    // Cmd+Shift+Q logs out on macOS.
    RegisterFile(catalogue, ExportLanguage.Quest, "_Quest Source...", fileExport,
      Gesture(InputKey.Q, isMacOS ? primary | alt | shift : primary | shift));
    RegisterFile(catalogue, ExportLanguage.Alan, "_Alan Source...", fileExport, Gesture(InputKey.L, primary | shift));
    RegisterFile(catalogue, ExportLanguage.Adventuron, "A_dventuron Source...", fileExport);

    RegisterClipboard(catalogue, ExportLanguage.Alan, "_Alan", status, Gesture(InputKey.L, primary | alt));
    RegisterClipboard(catalogue, ExportLanguage.Adventuron, "A_dventuron", status, Gesture(InputKey.A, primary | alt));
    // Cmd+Option+H hides other apps on macOS.
    RegisterClipboard(catalogue, ExportLanguage.Hugo, "_Hugo", status,
      Gesture(InputKey.H, isMacOS ? primary | alt | shift : primary | alt));
    RegisterClipboard(catalogue, ExportLanguage.Inform7, "Inform _7", status, Gesture(InputKey.D7, primary | alt));
    RegisterClipboard(catalogue, ExportLanguage.Inform6, "Inform _6", status, Gesture(InputKey.D6, primary | alt));
    RegisterClipboard(catalogue, ExportLanguage.Tads, "_TADS", status, Gesture(InputKey.T, primary | alt));
    RegisterClipboard(catalogue, ExportLanguage.Zil, "_ZIL", status, Gesture(InputKey.Z, primary | alt));
    RegisterClipboard(catalogue, ExportLanguage.Quest, "_Quest (with header)", status);
    RegisterClipboard(catalogue, ExportLanguage.QuestRooms, "Q_uest (no header)", status, Gesture(InputKey.Q, primary | alt));
  }

  private static KeyGesture[] Gesture(InputKey key, ModifierKeys modifiers)
  {
    return new[] { new KeyGesture(key, modifiers) };
  }

  private static void RegisterFile(
    CommandCatalogue catalogue, ExportLanguage language, string title, Func<ExportLanguage, Task> fileExport,
    KeyGesture[] gestures = null)
  {
    catalogue.Register(new EditorCommand
    {
      Id = FileId(language),
      Title = title,
      MenuPath = "File/Export",
      // "<group in File menu>/<group in Export submenu>"
      Group = "2-export/2-code",
      Gestures = gestures ?? Array.Empty<KeyGesture>(),
      CanExecuteFunc = HasRooms,
      ExecuteAsyncFunc = _ => fileExport == null ? Task.CompletedTask : fileExport(language)
    });
  }

  private static void RegisterClipboard(
    CommandCatalogue catalogue, ExportLanguage language, string title, Action<string> status,
    KeyGesture[] gestures = null)
  {
    catalogue.Register(new EditorCommand
    {
      Id = ClipboardId(language),
      Title = title,
      MenuPath = "File/Export/To _Clipboard",
      Group = "2-export/3-clipboard",
      Gestures = gestures ?? Array.Empty<KeyGesture>(),
      CanExecuteFunc = HasRooms,
      ExecuteAsyncFunc = session => RunClipboardAsync(session, language, status)
    });
  }

  private static async Task RunClipboardAsync(EditorSession session, ExportLanguage language, Action<string> status)
  {
    try
    {
      if (await ExportToClipboardAsync(session, language))
        status?.Invoke($"Copied {CodeExportService.DisplayName(language)} source code to the clipboard");
    }
    catch (Exception ex)
    {
      status?.Invoke($"Could not copy {CodeExportService.DisplayName(language)} source code: {ex.Message}");
    }
  }
}
