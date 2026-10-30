using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  private static IReadOnlyList<DynamicMenuItem> RegionMenuItems(EditorSession session)
  {
    var current = session.PrimaryRoom?.Region;
    return session.RegionNames()
      .Select(name => new DynamicMenuItem
      {
        Title = name,
        IsChecked = string.Equals(name, current, StringComparison.OrdinalIgnoreCase),
        Execute = s => s.SetSelectedRoomsRegion(name)
      })
      .ToList();
  }

  /// <summary>Regions, port adjust, z-order, validation toggles and Toggle Text (tickets 024, 033, 035, 036, 054).</summary>
  private static void RegisterMoreCommands(CommandCatalogue catalogue)
  {
    var primary = ModifierKeys.Primary;
    var none = ModifierKeys.None;

    // Regions (024). The context menu has a Set Region submenu listing the regions, the current one ticked.
    catalogue.Register(new EditorCommand
    {
      Id = "room.changeRegion",
      Title = "Change _Region...",
      MenuPath = "Edit/Rooms",
      Group = "5-region",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.F2, ModifierKeys.Shift) },
      CanExecuteFunc = s => s.Selection.Rooms.Any() && s.HasMultipleRegions,
      ExecuteAsyncFunc = s => s.ChangeRegionAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.setRegion",
      Hidden = true, // context menu only
      ContextOrder = 70, ContextGroup = "7-region",
      Title = "Set _Region",
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Selection.Rooms.Any() && s.HasMultipleRegions,
      DynamicChildren = RegionMenuItems
    });

    // Ports (033).
    catalogue.Register(new EditorCommand
    {
      Id = "room.adjustPorts",
      Title = "Adjust _Ports",
      MenuPath = "Edit/Rooms",
      Group = "6-ports",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.OemSemicolon, none), new KeyGesture(InputKey.Oem5, none) },
      CanExecuteFunc = s => s.SelectedRooms.Count == 1,
      ExecuteAsyncFunc = s => Run(() => s.AdjustPorts())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.cyclePortDetail",
      Title = "Cycle Port Adjust _Detail",
      MenuPath = "Edit/Rooms",
      Group = "6-ports",
      Gestures = new[] { new KeyGesture(InputKey.OemSemicolon, primary), new KeyGesture(InputKey.Oem5, primary) },
      CanExecuteFunc = s => s.SelectedRooms.Count == 1,
      ExecuteAsyncFunc = s => Run(() => s.CyclePortDetail())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.deleteAllConnections",
      Title = "Delete All Co_nnections",
      MenuPath = "Edit/Rooms",
      Group = "6-ports",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.Delete, ModifierKeys.Alt) },
      CanExecuteFunc = s => s.SelectedRooms.Count == 1,
      ExecuteAsyncFunc = s => Run(() => s.DeleteAllConnectionsOfRoom())
    });

    // Z-order (035).
    catalogue.Register(new EditorCommand
    {
      Id = "element.sendToBack",
      ContextOrder = 71, ContextGroup = "7-arrange",
      Title = "Send to _Back",
      MenuPath = "Edit/Arrange",
      Group = "1-zorder",
      ContextTargets = ContextTargets.Room | ContextTargets.Label,
      CanExecuteFunc = s => s.HasRoomOrLabelSelected,
      ExecuteAsyncFunc = s => Run(() => s.SendSelectionToBack())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "element.bringToFront",
      ContextOrder = 72, ContextGroup = "7-arrange",
      Title = "Bring to _Front",
      MenuPath = "Edit/Arrange",
      Group = "1-zorder",
      ContextTargets = ContextTargets.Room | ContextTargets.Label,
      CanExecuteFunc = s => s.HasRoomOrLabelSelected,
      ExecuteAsyncFunc = s => Run(() => s.BringSelectionToFront())
    });

    // Validation toggles (036).
    RegisterValidation(catalogue, "project.validate.uniqueNames", "Rooms must have a _unique name", ValidationFlag.UniqueNames);
    RegisterValidation(catalogue, "project.validate.description", "Rooms must have a _description", ValidationFlag.Description);
    RegisterValidation(catalogue, "project.validate.subtitle", "Rooms must have a _subtitle", ValidationFlag.Subtitle);
    RegisterValidation(catalogue, "project.validate.noDangling", "Rooms must not have _dangling connections", ValidationFlag.NoDangling);

    // Toggle Text (054).
    catalogue.Register(new EditorCommand
    {
      Id = "view.toggleText",
      Title = "Toggle _Text",
      MenuPath = "View",
      Group = "9-debug",
      Gestures = new[] { new KeyGesture(InputKey.F4, primary) },
      IsCheckedFunc = s => ApplicationSettingsController.AppSettings.DebugDisableTextRendering,
      ExecuteAsyncFunc = s => Run(() => s.ToggleTextRendering())
    });
  }

  private static void RegisterValidation(CommandCatalogue catalogue, string id, string title, ValidationFlag flag)
  {
    catalogue.Register(new EditorCommand
    {
      Id = id,
      Title = title,
      MenuPath = "Edit/Validation",
      Group = "1-validation",
      IsCheckedFunc = s => s.IsValidationEnabled(flag),
      ExecuteAsyncFunc = s => Run(() => s.ToggleValidation(flag))
    });
  }
}
