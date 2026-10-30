using System.Threading.Tasks;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

/// <summary>Delete, Copy, Cut, Paste and Add Map Label.</summary>
public static class EditCommands
{
  public static void Register(CommandCatalogue catalogue)
  {
    var primary = ModifierKeys.Primary;
    var elementTargets = ContextTargets.Room | ContextTargets.Connection | ContextTargets.Label;

    catalogue.Register(new EditorCommand
    {
      Id = "edit.delete",
      ToolbarOrder = 60, ToolbarGroup = "2-edit", ShowInToolbar = true, IconKey = "IconDelete",
      ContextOrder = 22, ContextGroup = "2-clipboard",
      Title = "_Delete",
      MenuPath = "Edit",
      Group = "1-select",
      // One Window-scope Delete binding; Backspace is canvas-only so it never deletes text in a text box.
      Gestures = new[] { new KeyGesture(InputKey.Delete, ModifierKeys.None) },
      ContextTargets = elementTargets,
      CanExecuteFunc = s => s.Selection.Count > 0,
      ExecuteAsyncFunc = s => Run(() => s.DeleteSelected())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.deleteBackspace",
      Title = "Delete (Backspace)",
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[]
      {
        new KeyGesture(InputKey.Back, ModifierKeys.None),
        new KeyGesture(InputKey.Back, primary)
      },
      CanExecuteFunc = s => s.Selection.Count > 0,
      ExecuteAsyncFunc = s => Run(() => s.DeleteSelected())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.copy",
      ContextOrder = 21, ContextGroup = "2-clipboard",
      Title = "_Copy",
      MenuPath = "Edit",
      Group = "2-clipboard",
      Gestures = new[] { new KeyGesture(InputKey.C, primary) },
      ContextTargets = elementTargets,
      CanExecuteFunc = s => s.Selection.Count > 0,
      ExecuteAsyncFunc = s => s.CopyAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.cut",
      ContextOrder = 20, ContextGroup = "2-clipboard",
      Title = "Cu_t",
      MenuPath = "Edit",
      Group = "2-clipboard",
      Gestures = new[] { new KeyGesture(InputKey.X, primary) },
      ContextTargets = elementTargets,
      CanExecuteFunc = s => s.Selection.Count > 0,
      ExecuteAsyncFunc = s => s.CutAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.paste",
      ContextOrder = 30, ContextGroup = "3-paste",
      Title = "_Paste",
      MenuPath = "Edit",
      Group = "2-clipboard",
      Gestures = new[] { new KeyGesture(InputKey.V, primary) },
      ContextTargets = ContextTargets.Any,
      CanExecuteFunc = s => s.CanPaste,
      ExecuteAsyncFunc = s => s.PasteAsync(null)
    });

    catalogue.Register(new EditorCommand
    {
      Id = "project.addLabel",
      ToolbarOrder = 50, ToolbarGroup = "2-edit", ShowInToolbar = true, IconKey = "IconAddLabel",
      ContextOrder = 41, ContextGroup = "4-add",
      Title = "Add _Map Label",
      MenuPath = "Project",
      Group = "1-add",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.L, ModifierKeys.None) },
      ContextTargets = ContextTargets.Any,
      ExecuteAsyncFunc = s => Run(() => s.AddLabel(s.PointerMapPosition))
    });
  }

  private static Task Run(System.Action action)
  {
    action();
    return Task.CompletedTask;
  }
}
