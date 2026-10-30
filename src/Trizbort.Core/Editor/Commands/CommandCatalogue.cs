using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

/// <summary>
///   The single registry of editor commands. Core registers the session-level commands in
///   <see cref="CreateDefault" />; UI projects register app-level ones (dialogs, files) through
///   <see cref="Register" />.
/// </summary>
public sealed class CommandCatalogue
{
  private readonly List<EditorCommand> _commands = new();
  private readonly Dictionary<string, EditorCommand> _byId = new(StringComparer.Ordinal);

  public IReadOnlyList<EditorCommand> All => _commands;

  public EditorCommand Find(string id)
  {
    if (id == null) return null;
    return _byId.TryGetValue(id, out var command) ? command : null;
  }

  public void Register(EditorCommand command)
  {
    if (command == null) throw new ArgumentNullException(nameof(command));
    if (string.IsNullOrEmpty(command.Id)) throw new ArgumentException("Command id is required.", nameof(command));
    if (_byId.ContainsKey(command.Id)) throw new InvalidOperationException($"Duplicate command id '{command.Id}'.");

    _byId[command.Id] = command;
    _commands.Add(command);
  }

  /// <summary>The command of the given scope bound to <paramref name="gesture" />, or null.</summary>
  public EditorCommand FindByGesture(KeyGesture gesture, CommandScope scope)
  {
    foreach (var command in _commands)
    {
      if (command.Scope != scope) continue;
      foreach (var candidate in command.Gestures)
      {
        if (candidate == gesture) return command;
      }
    }
    return null;
  }

  public EditorCommand FindByGesture(KeyInput input, CommandScope scope)
  {
    return FindByGesture(new KeyGesture(input.Key, input.Modifiers), scope);
  }

  public IEnumerable<EditorCommand> ForContext(ContextTargets target)
  {
    return _commands.Where(c => (c.ContextTargets & target) != 0);
  }

  /// <summary>Commands whose menu path starts at <paramref name="rootName" />, ordered by group then registration.</summary>
  public IEnumerable<EditorCommand> Menu(string rootName)
  {
    return _commands
      .Where(c => !c.Hidden && c.MenuPath != null && MenuRoot(c.MenuPath) == rootName)
      .Select((c, index) => (Command: c, Index: index))
      .OrderBy(x => x.Command.Group ?? string.Empty, StringComparer.Ordinal)
      .ThenBy(x => x.Index)
      .Select(x => x.Command);
  }

  private static string MenuRoot(string path)
  {
    var slash = path.IndexOf('/');
    return slash < 0 ? path : path.Substring(0, slash);
  }

  public static CommandCatalogue CreateDefault()
  {
    var catalogue = new CommandCatalogue();
    var primary = ModifierKeys.Primary;

    catalogue.Register(new EditorCommand
    {
      Id = "edit.undo",
      Title = "_Undo",
      DynamicTitle = s => s.Undo.CanUndo ? "_Undo " + s.Undo.UndoTitle : "_Undo",
      MenuPath = "Edit",
      Group = "0-undo",
      Gestures = new[] { new KeyGesture(InputKey.Z, primary) },
      CanExecuteFunc = s => s.Undo.CanUndo,
      ExecuteAsyncFunc = s => Run(() => s.PerformUndo())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.redo",
      Title = "_Redo",
      DynamicTitle = s => s.Undo.CanRedo ? "_Redo " + s.Undo.RedoTitle : "_Redo",
      MenuPath = "Edit",
      Group = "0-undo",
      Gestures = new[] { new KeyGesture(InputKey.Y, primary), new KeyGesture(InputKey.Z, primary | ModifierKeys.Shift) },
      CanExecuteFunc = s => s.Undo.CanRedo,
      ExecuteAsyncFunc = s => Run(() => s.PerformRedo())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.selectAll",
      Title = "Select _All",
      MenuPath = "Edit",
      Group = "1-select",
      Gestures = new[] { new KeyGesture(InputKey.A, primary) },
      CanExecuteFunc = s => s.Project.Elements.Any(),
      ExecuteAsyncFunc = s => Run(() => s.SelectAll())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.delete",
      ToolbarOrder = 60, ToolbarGroup = "2-edit", ShowInToolbar = true, IconKey = "IconDelete",
      ContextOrder = 22, ContextGroup = "2-clipboard",
      Title = "_Delete",
      MenuPath = "Edit",
      Group = "1-select",
      Gestures = new[] { new KeyGesture(InputKey.Delete, ModifierKeys.None) },
      ContextTargets = ContextTargets.Room | ContextTargets.Connection | ContextTargets.Label,
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
      ContextTargets = ContextTargets.Room | ContextTargets.Connection | ContextTargets.Label,
      CanExecuteFunc = s => s.Selection.Count > 0,
      ExecuteAsyncFunc = s => Run(() => s.CopySelected())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.cut",
      ContextOrder = 20, ContextGroup = "2-clipboard",
      Title = "Cu_t",
      MenuPath = "Edit",
      Group = "2-clipboard",
      Gestures = new[] { new KeyGesture(InputKey.X, primary) },
      ContextTargets = ContextTargets.Room | ContextTargets.Connection | ContextTargets.Label,
      CanExecuteFunc = s => s.Selection.Count > 0,
      ExecuteAsyncFunc = s => Run(() => s.CutSelected())
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
      ExecuteAsyncFunc = s => Run(() => s.Paste(null, null))
    });

    catalogue.Register(new EditorCommand
    {
      Id = "view.zoomIn",
      ToolbarOrder = 70, ToolbarGroup = "3-view", ShowInToolbar = true, IconKey = "IconZoomIn",
      Title = "Zoom _In",
      MenuPath = "View",
      Group = "1-zoom",
      Gestures = new[]
      {
        new KeyGesture(InputKey.OemPlus, primary),
        new KeyGesture(InputKey.Add, primary)
      },
      ExecuteAsyncFunc = s => Run(() => s.Viewport.ZoomIn())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.zoomOut",
      ToolbarOrder = 80, ToolbarGroup = "3-view", ShowInToolbar = true, IconKey = "IconZoomOut",
      Title = "Zoom _Out",
      MenuPath = "View",
      Group = "1-zoom",
      Gestures = new[]
      {
        new KeyGesture(InputKey.OemMinus, primary),
        new KeyGesture(InputKey.Subtract, primary)
      },
      ExecuteAsyncFunc = s => Run(() => s.Viewport.ZoomOut())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.resetZoom",
      ToolbarOrder = 90, ToolbarGroup = "3-view", ShowInToolbar = true, IconKey = "IconZoomReset",
      Title = "_Reset Zoom",
      MenuPath = "View",
      Group = "1-zoom",
      Gestures = new[]
      {
        new KeyGesture(InputKey.D0, primary),
        new KeyGesture(InputKey.NumPad0, primary)
      },
      ExecuteAsyncFunc = s => Run(() => s.Viewport.ResetZoom())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.fitToMap",
      Title = "_Fit to Map",
      MenuPath = "View",
      Group = "1-zoom",
      ExecuteAsyncFunc = s => Run(() => s.Viewport.FitToMap(s.Project.Elements))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.toggleGrid",
      ToolbarOrder = 100, ToolbarGroup = "3-view", ShowInToolbar = true, IconKey = "IconGrid",
      Title = "Toggle _Grid",
      MenuPath = "View",
      Group = "2-display",
      IsCheckedFunc = s => s.ShowGrid,
      ExecuteAsyncFunc = s => Run(() => s.ShowGrid = !s.ShowGrid)
    });

    catalogue.Register(new EditorCommand
    {
      Id = "project.addRoom",
      ToolbarOrder = 40, ToolbarGroup = "2-edit", ShowInToolbar = true, IconKey = "IconAddRoom",
      ContextOrder = 40, ContextGroup = "4-add",
      Title = "Add _Room",
      MenuPath = "Project",
      Group = "1-add",
      ContextTargets = ContextTargets.Any,
      ExecuteAsyncFunc = s => Run(() => s.AddRoom(null))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "project.addLabel",
      ToolbarOrder = 50, ToolbarGroup = "2-edit", ShowInToolbar = true, IconKey = "IconAddLabel",
      ContextOrder = 41, ContextGroup = "4-add",
      Title = "Add _Map Label",
      MenuPath = "Project",
      Group = "1-add",
      ContextTargets = ContextTargets.Any,
      ExecuteAsyncFunc = s => Run(() => s.AddMapLabel(null))
    });

    return catalogue;
  }

  private static Task Run(Action action)
  {
    action();
    return Task.CompletedTask;
  }
}
