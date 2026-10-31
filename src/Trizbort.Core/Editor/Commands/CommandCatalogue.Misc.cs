using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>Copy colour, duplicate and backup (tickets 041, 042, 043).</summary>
  private static void RegisterMiscCommands(CommandCatalogue catalogue)
  {
    var primary = ModifierKeys.Primary;

    catalogue.Register(new EditorCommand
    {
      Id = "edit.copyColor",
      Title = "Copy Co_lour",
      MenuPath = "Edit",
      Group = "2-clipboard",
      Gestures = new[] { new KeyGesture(InputKey.C, primary | ModifierKeys.Alt) },
      ContextTargets = ContextTargets.Room,
      ContextOrder = 23, ContextGroup = "2-clipboard",
      CanExecuteFunc = s => s.HasSingleSelectedRoom,
      ExecuteAsyncFunc = s => s.CopyColorAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.duplicate",
      Title = "D_uplicate",
      MenuPath = "Edit",
      Group = "2-clipboard",
      // Matched exactly: bare D is connection label Down (Canvas scope).
      Gestures = new[] { new KeyGesture(InputKey.D, primary) },
      CanExecuteFunc = s => s.CanDuplicate,
      ExecuteAsyncFunc = s => Run(() => s.DuplicateSelected())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "file.backup",
      Title = "_Backup",
      MenuPath = "File",
      Group = "1-d-backup",
      Gestures = new[] { new KeyGesture(InputKey.B, primary) },
      CanExecuteFunc = s => s.CanBackup,
      ExecuteAsyncFunc = s => s.BackupProjectAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "project.restoreDefaultSettings",
      Title = "_Restore Default Map Settings",
      MenuPath = "Edit",
      Group = "4-settings",
      ExecuteAsyncFunc = s => s.RestoreDefaultMapSettingsAsync()
    });
  }
}
