using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

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
