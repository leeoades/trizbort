using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Copy colour, duplicate, backup and unexplored connection (tickets 041, 042, 043, 049).
public sealed partial class EditorSession
{
  // ---- Copy colour (041) ----

  /// <summary>
  ///   Copies the colours of the single selected room to <see cref="Clipboard" /> so Paste applies them to the
  ///   selected rooms. Not an undo step. Returns false when exactly one room is not selected.
  /// </summary>
  public async Task<bool> CopyColorAsync()
  {
    if (!HasSingleSelectedRoom) return false;

    // CopyController writes through a static handler; capture the text so it reaches this session's clipboard.
    string text = null;
    var previous = CopyController.SetClipboardTextHandler;
    CopyController.SetClipboardTextHandler = captured => text = captured;
    try
    {
      new CopyController().CopyColors((Room)Selection.Primary);
    }
    finally
    {
      CopyController.SetClipboardTextHandler = previous;
    }

    if (string.IsNullOrEmpty(text)) return false;

    await _clipboard.SetTextAsync(text);
    _clipboardHasData = true;
    _lastPasteText = null;
    PostStatusMessage("Copied colours");
    return true;
  }

  // ---- Duplicate (042) ----

  /// <summary>True when there is a selection (or active element) to duplicate.</summary>
  public bool CanDuplicate => GetSelectedOrActive().Count > 0;

  /// <summary>
  ///   Duplicates the selection one grid step down and right, copying connections only when they were selected, and
  ///   selects the copies. The clipboard and the repeated-paste offset are untouched. One undo step. Returns false
  ///   when there is nothing to duplicate.
  /// </summary>
  public bool DuplicateSelected()
  {
    var text = BuildCopyText();
    if (text == null) return false;

    var copyObj = JsonConvert.DeserializeObject<CopyController.CopyObject>(text);
    var controller = new CopyController();
    var step = Settings.GridSize;
    Execute("Duplicate", () => PasteElements(copyObj, controller, null, step));
    PostStatusMessage("Duplicated " + ((copyObj.Rooms?.Count ?? 0) + (copyObj.Labels?.Count ?? 0) + (copyObj.Connections?.Count ?? 0)) + " element(s)");
    return true;
  }

  // ---- Backup (043) ----

  /// <summary>True when the project has a file name to back up.</summary>
  public bool CanBackup => Project.HasFileName;

  /// <summary>
  ///   Copies the project file to the next free backup name and tells the user through <see cref="Prompts" />.
  ///   Returns the backup path, or null when nothing was backed up. Not an undo step.
  /// </summary>
  public async Task<string> BackupProjectAsync()
  {
    var project = Project;
    if (!project.HasFileName) return null;

    if (!File.Exists(project.FileName))
    {
      await Prompts.ShowMessageAsync("Backup", "The project has not been saved yet, so there is nothing to back up.");
      return null;
    }

    string path;
    try
    {
      path = project.Backup();
    }
    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
    {
      await Prompts.ShowMessageAsync("Backup failed", ex.Message);
      return null;
    }

    if (path == null) return null;

    PostStatusMessage("Backup saved to " + path);
    await Prompts.ShowMessageAsync("Backup", "A backup of the project was saved to:" + Environment.NewLine + path);
    return path;
  }

  // ---- Unexplored connection (049) ----

  /// <summary>
  ///   Alt+keypad: with a room already reachable in the direction it is selected (as the plain keypad does);
  ///   otherwise a loopback ("unexplored") connection is added leaving and re-entering the selected room on that
  ///   compass point, as one undo step. Returns false when exactly one room is not selected.
  /// </summary>
  public bool AddUnexploredConnection(CompassPoint direction)
  {
    if (!HasSingleSelectedRoom) return false;
    if (SelectRoomInDirection(direction)) return true;

    var room = (Room)Selection.Primary;
    Connection connection = null;
    Execute("Add unexplored connection", () => connection = AddRoomConnection(room, direction, room, direction), new Element[] { room });
    if (connection != null) Selection.Set(new Element[] { room });
    RaiseChanged(EditorChange.Model);
    return true;
  }
}
