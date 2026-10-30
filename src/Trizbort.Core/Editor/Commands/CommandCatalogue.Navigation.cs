using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>Select regions/start room, properties, connection labels and join rooms (tickets 022, 023, 028, 031).</summary>
  private static void RegisterNavigationCommands(CommandCatalogue catalogue)
  {
    var none = ModifierKeys.None;

    // Selection (022). Canvas scope so the focused canvas runs it; the reference only handles it there.
    catalogue.Register(new EditorCommand
    {
      Id = "select.regions",
      Title = "Select Rooms in _Regions",
      MenuPath = "Edit",
      Group = "1-select",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.A, ModifierKeys.Primary | ModifierKeys.Shift) },
      CanExecuteFunc = s => s.Selection.Rooms.Any(),
      ExecuteAsyncFunc = s => Run(() => s.SelectRegions())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "select.startRoom",
      Title = "Select _Start Room",
      MenuPath = "Edit",
      Group = "1-select",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.D0, none), new KeyGesture(InputKey.NumPad0, none) },
      CanExecuteFunc = s => s.HasStartRoom,
      ExecuteAsyncFunc = s => Run(() => s.SelectStartRoom())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "select.nearestToCentre",
      Title = "Select Room Nearest _Centre",
      Hidden = true,
      CanExecuteFunc = s => s.Project.Elements.OfType<Room>().Any(),
      ExecuteAsyncFunc = s => Run(() => s.SelectRoomNearestViewportCentre())
    });

    // Properties (023).
    catalogue.Register(new EditorCommand
    {
      Id = "edit.properties",
      Title = "_Properties...",
      MenuPath = "Edit",
      Group = "3-properties",
      Gestures = new[] { new KeyGesture(InputKey.Enter, ModifierKeys.Alt) },
      ContextTargets = ContextTargets.Room | ContextTargets.Connection | ContextTargets.Label,
      ContextOrder = 10, ContextGroup = "1-properties",
      CanExecuteFunc = s => s.CanShowProperties,
      ExecuteAsyncFunc = s => s.ShowSelectedPropertiesAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.enter",
      Title = "Enter",
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.Enter, none) },
      CanExecuteFunc = s => s.CanEnter,
      ExecuteAsyncFunc = s => s.EnterAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.rename",
      Title = "_Rename",
      MenuPath = "Edit",
      Group = "3-properties",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.F2, none) },
      ContextTargets = ContextTargets.Room,
      ContextOrder = 11, ContextGroup = "1-properties",
      CanExecuteFunc = s => s.Selection.Count == 1 && s.Selection.Primary is Room,
      ExecuteAsyncFunc = s => s.ShowSelectedPropertiesAsync()
    });

    // Connection labels (031).
    RegisterLabel(catalogue, "connection.label.up", "_Up", InputKey.U, ConnectionLabel.Up, 54);
    RegisterLabel(catalogue, "connection.label.down", "_Down", InputKey.D, ConnectionLabel.Down, 55);
    RegisterLabel(catalogue, "connection.label.in", "_In", InputKey.I, ConnectionLabel.In, 56);
    RegisterLabel(catalogue, "connection.label.out", "_Out", InputKey.O, ConnectionLabel.Out, 57);

    // Join rooms (028).
    catalogue.Register(new EditorCommand
    {
      Id = "room.join",
      Title = "_Join Rooms",
      MenuPath = "Edit/Rooms",
      Group = "7-join",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.J, none) },
      ContextTargets = ContextTargets.Room,
      ContextOrder = 60, ContextGroup = "6-join",
      CanExecuteFunc = s => s.CanJoinRooms,
      ExecuteAsyncFunc = s => Run(() => s.JoinSelectedRooms())
    });

    RegisterDirectionCommands(catalogue);
    RegisterActiveElementCommands(catalogue);
  }

  // Direction navigation (047, 048): one row per compass direction. Plain arrows move or pan (View group), so the
  // plain select commands are keypad only; the diagonals on the main keyboard are Home/End/PageUp/PageDown.
  private sealed record DirectionKeys(string Name, CompassPoint Compass, InputKey? Arrow, InputKey? Block, InputKey Keypad);

  private static readonly DirectionKeys[] _directions =
  {
    new("north", CompassPoint.North, InputKey.Up, null, InputKey.NumPad8),
    new("south", CompassPoint.South, InputKey.Down, null, InputKey.NumPad2),
    new("east", CompassPoint.East, InputKey.Right, null, InputKey.NumPad6),
    new("west", CompassPoint.West, InputKey.Left, null, InputKey.NumPad4),
    new("northWest", CompassPoint.NorthWest, null, InputKey.Home, InputKey.NumPad7),
    new("southWest", CompassPoint.SouthWest, null, InputKey.End, InputKey.NumPad1),
    new("northEast", CompassPoint.NorthEast, null, InputKey.PageUp, InputKey.NumPad9),
    new("southEast", CompassPoint.SouthEast, null, InputKey.PageDown, InputKey.NumPad3)
  };

  private static void RegisterDirectionCommands(CommandCatalogue catalogue)
  {
    var shift = ModifierKeys.Shift;
    var creation = KeypadModifier(Settings.KeypadNavigationCreationModifier);
    foreach (var direction in _directions)
    {
      var compass = direction.Compass;

      // Plain keypad: select the room in that direction.
      catalogue.Register(new EditorCommand
      {
        Id = "nav.select." + direction.Name,
        Title = "Select Room " + direction.Name,
        Hidden = true,
        Scope = CommandScope.Canvas,
        Gestures = new[] { new KeyGesture(direction.Keypad, ModifierKeys.None) },
        CanExecuteFunc = s => s.HasSingleSelectedRoom,
        ExecuteAsyncFunc = s => Run(() => s.SelectRoomInDirection(compass))
      });

      // Shift+arrow, Shift+Home/End/PageUp/PageDown, Shift+keypad: the connection, else the room.
      var shiftGestures = new List<KeyGesture> { new KeyGesture(direction.Keypad, shift) };
      if (direction.Arrow.HasValue) shiftGestures.Add(new KeyGesture(direction.Arrow.Value, shift));
      if (direction.Block.HasValue) shiftGestures.Add(new KeyGesture(direction.Block.Value, shift));
      catalogue.Register(new EditorCommand
      {
        Id = "nav.selectConnection." + direction.Name,
        Title = "Select Connection or Room " + direction.Name,
        Hidden = true,
        Scope = CommandScope.Canvas,
        Gestures = shiftGestures,
        CanExecuteFunc = s => s.CanNavigateFromSelection,
        ExecuteAsyncFunc = s => Run(() => s.SelectConnectionOrRoomInDirection(compass))
      });

      // Primary+arrow and (setting-dependent modifier)+keypad: select, else connect to or add a room.
      var addGestures = new List<KeyGesture> { new KeyGesture(direction.Keypad, creation) };
      if (direction.Arrow.HasValue) addGestures.Add(new KeyGesture(direction.Arrow.Value, ModifierKeys.Primary));
      catalogue.Register(new EditorCommand
      {
        Id = "nav.addOrConnect." + direction.Name,
        Title = "Select or Add Room " + direction.Name,
        Hidden = true,
        Scope = CommandScope.Canvas,
        Gestures = addGestures,
        CanExecuteFunc = s => s.CanNavigateFromSelection,
        ExecuteAsyncFunc = s => s.NavigateOrAddRoomAsync(compass)
      });
    }
  }

  /// <summary>
  ///   Maps the persisted keypad modifier setting (reference <c>Keys</c> value, Ctrl by default) to a catalogue
  ///   modifier: Ctrl becomes Primary. Values that would clash with the plain or Shift keypad rows, or with the
  ///   "unexplored" modifier, fall back to Primary. Read when the catalogue is built.
  /// </summary>
  private static ModifierKeys KeypadModifier(ModifierKeys setting)
  {
    var modifiers = ModifierKeys.None;
    if ((setting & (ModifierKeys.Control | ModifierKeys.Primary)) != 0) modifiers |= ModifierKeys.Primary;
    if ((setting & ModifierKeys.Alt) != 0) modifiers |= ModifierKeys.Alt;
    if ((setting & ModifierKeys.Shift) != 0) modifiers |= ModifierKeys.Shift;

    var unexplored = ModifierKeys.None;
    var other = Settings.KeypadNavigationUnexploredModifier;
    if ((other & (ModifierKeys.Control | ModifierKeys.Primary)) != 0) unexplored |= ModifierKeys.Primary;
    if ((other & ModifierKeys.Alt) != 0) unexplored |= ModifierKeys.Alt;
    if ((other & ModifierKeys.Shift) != 0) unexplored |= ModifierKeys.Shift;

    if (modifiers == ModifierKeys.None || modifiers == ModifierKeys.Shift || modifiers == unexplored)
      return ModifierKeys.Primary;
    return modifiers;
  }

  // Active element cycling (045).
  private static void RegisterActiveElementCommands(CommandCatalogue catalogue)
  {
    catalogue.Register(new EditorCommand
    {
      Id = "selection.nextActive",
      Title = "Make Next Selected Element Active",
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.F3, ModifierKeys.None) },
      CanExecuteFunc = s => s.CanCycleActiveElement,
      ExecuteAsyncFunc = s => Run(() => s.CycleActiveElement(true))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "selection.previousActive",
      Title = "Make Previous Selected Element Active",
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.F3, ModifierKeys.Shift) },
      CanExecuteFunc = s => s.CanCycleActiveElement,
      ExecuteAsyncFunc = s => Run(() => s.CycleActiveElement(false))
    });
  }

  private static void RegisterLabel(
    CommandCatalogue catalogue, string id, string title, InputKey key, ConnectionLabel label, int contextOrder)
  {
    catalogue.Register(new EditorCommand
    {
      Id = id,
      Title = title,
      MenuPath = "Edit/Line Styles",
      Group = "3-label",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(key, ModifierKeys.None) },
      ContextTargets = ContextTargets.Connection,
      ContextOrder = contextOrder, ContextGroup = "5-connection",
      CanExecuteFunc = s => s.Selection.Connections.Any(),
      IsCheckedFunc = s => s.CurrentConnectionLabel == label,
      ExecuteAsyncFunc = s => Run(() => s.SetConnectionLabel(label))
    });
  }
}
