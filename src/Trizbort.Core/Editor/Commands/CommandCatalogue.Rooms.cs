using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Automap;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>Room commands: darkness, start/end, shapes, swaps, add room.</summary>
  private static void RegisterRoomCommands(CommandCatalogue catalogue)
  {
    var primary = ModifierKeys.Primary;

    // Lighting (025). Bare K needs no modifiers, so Primary+K never falls through to Toggle.
    catalogue.Register(new EditorCommand
    {
      Id = "room.toggleDark",
      ContextOrder = 50, ContextGroup = "5-room",
      Title = "Toggle _Darkness",
      MenuPath = "Edit/Rooms",
      Group = "1-lighting",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.K, ModifierKeys.None) },
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Selection.Rooms.Any(),
      IsCheckedFunc = s => s.PrimaryRoom != null && s.PrimaryRoom.IsDark,
      ExecuteAsyncFunc = s => Run(() => s.ToggleDarkness())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.toggleAutomapPause",
      ContextOrder = 62, ContextGroup = "5-room",
      Title = "Don't _Automap From Here",
      MenuPath = "Edit/Rooms",
      Group = "6-automap",
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Selection.Rooms.Any(),
      IsCheckedFunc = s => s.PrimaryRoom != null && s.PrimaryRoom.AutomapPause,
      ExecuteAsyncFunc = s => Run(() =>
      {
        var rooms = s.Selection.Rooms.ToList();
        var on = !(s.PrimaryRoom ?? rooms[0]).AutomapPause;
        s.Execute(on ? "Don't automap from here" : "Automap from here", () =>
        {
          foreach (var room in rooms) room.AutomapPause = on;
        });
      })
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.forceDark",
      Title = "_Force Darkness",
      MenuPath = "Edit/Rooms",
      Group = "1-lighting",
      Gestures = new[] { new KeyGesture(InputKey.K, primary) },
      CanExecuteFunc = s => s.Selection.Rooms.Any(),
      ExecuteAsyncFunc = s => Run(() => s.ForceDarkness())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.forceLit",
      Title = "Force _Lighted",
      MenuPath = "Edit/Rooms",
      Group = "1-lighting",
      Gestures = new[] { new KeyGesture(InputKey.K, primary | ModifierKeys.Shift) },
      CanExecuteFunc = s => s.Selection.Rooms.Any(),
      ExecuteAsyncFunc = s => Run(() => s.ForceLighted())
    });

    // Start / end room (026).
    catalogue.Register(new EditorCommand
    {
      Id = "room.setStart",
      ContextOrder = 51, ContextGroup = "5-room",
      Title = "_Start Room",
      MenuPath = "Edit/Rooms",
      Group = "2-markers",
      Gestures = new[] { new KeyGesture(InputKey.F5, primary) },
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.SelectedRooms.Count == 1,
      IsCheckedFunc = s => s.PrimaryRoom != null && s.PrimaryRoom.IsStartRoom,
      ExecuteAsyncFunc = s => Run(() => s.SetStartRoom())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "room.setEnd",
      ContextOrder = 52, ContextGroup = "5-room",
      Title = "_End Room",
      MenuPath = "Edit/Rooms",
      Group = "2-markers",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.F5, ModifierKeys.Shift) },
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Selection.Rooms.Any(),
      IsCheckedFunc = s => s.PrimaryRoom != null && s.PrimaryRoom.IsEndRoom,
      ExecuteAsyncFunc = s => Run(() => s.ToggleEndRoom())
    });

    // Shapes (027). Primary+E is Ellipse (the legacy canvas wrongly set square corners).
    RegisterShape(catalogue, "room.shape.square", "S_quare Corners", InputKey.H, RoomShape.SquareCorners, 53);
    RegisterShape(catalogue, "room.shape.ellipse", "_Ellipse", InputKey.E, RoomShape.Ellipse, 54);
    RegisterShape(catalogue, "room.shape.rounded", "_Rounded Edges", InputKey.R, RoomShape.RoundedCorners, 55);
    RegisterShape(catalogue, "room.shape.octagonal", "_Octagonal Edges", InputKey.D8, RoomShape.Octagonal, 56);

    // Swaps (029): exactly two rooms; distinguished by exact modifiers.
    RegisterSwap(catalogue, "room.swap.objects", "Swap _Objects", new KeyGesture(InputKey.W, ModifierKeys.None),
      CommandScope.Canvas, 57, s => s.SwapRoomObjects());
    RegisterSwap(catalogue, "room.swap.names", "Swap _Names", new KeyGesture(InputKey.W, primary),
      CommandScope.Window, 58, s => s.SwapRoomNames());
    RegisterSwap(catalogue, "room.swap.formats", "Swap _Formats", new KeyGesture(InputKey.W, ModifierKeys.Shift),
      CommandScope.Canvas, 59, s => s.SwapRoomFormats());
    RegisterSwap(catalogue, "room.swap.regions", "Swap _Regions", new KeyGesture(InputKey.W, ModifierKeys.Alt),
      CommandScope.Canvas, 60, s => s.SwapRoomRegions());

    // Tidy Map...: asks which rooms and which way, works it out in the background, then applies it as one undo step.
    catalogue.Register(new EditorCommand
    {
      Id = "map.tidy",
      ContextOrder = 61, ContextGroup = "5-room",
      Title = "_Tidy Map...",
      MenuPath = "Edit/Rooms",
      Group = "5-tidy",
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Project.Elements.OfType<Room>().Skip(1).Any(),
      ExecuteAsyncFunc = s => TidyAsync(s)
    });
  }

  /// <summary>The way Tidy Map tidied last time, offered first next time.</summary>
  private static TidyMethod _lastTidyMethod = TidyMethod.Quick;

  /// <summary>
  ///   Tidy Map...: asks which rooms (the whole map, or the selected rooms when more than one is selected) and which
  ///   way to tidy them, works the layout out off the UI thread with a progress bar, then moves the rooms as one undo
  ///   step. Tidied rooms lose their placed-by-hand mark. Cancelling at either step changes nothing.
  /// </summary>
  private static async Task TidyAsync(EditorSession session)
  {
    var selected = session.SelectedRooms.Count;
    var choice = await session.Prompts.ChooseTidyAsync(new TidyChoice(selected > 1, _lastTidyMethod), selected);
    if (choice == null) return;
    _lastTidyMethod = choice.Value.Method;

    var elements = session.Project.Elements.ToList();
    var rooms = choice.Value.SelectedOnly && selected > 1
      ? session.SelectedRooms.ToList()
      : elements.OfType<Room>().ToList();
    var plan = LayoutTidy.PlanTidy(elements, rooms, choice.Value.Method);
    var title = choice.Value.SelectedOnly && selected > 1 ? "Tidy selected rooms" : "Tidy map";
    var moves = await session.Prompts.RunWithProgressAsync(title, "Tidying the map...", plan.ExpectedMs, plan.Run);
    if (moves == null || moves.Count == 0) return;

    session.Execute(title, () =>
    {
      // asked to tidy them, so they're no longer where the user put them by hand
      foreach (var room in plan.Rooms) room.PlacedByHand = false;
      LayoutTidy.TidyPlan.Apply(moves);
      GuessedEnds.Face(session.Project.Elements, plan.Rooms.ToList());
    });
  }

  private static void RegisterShape(
    CommandCatalogue catalogue, string id, string title, InputKey key, RoomShape shape, int contextOrder)
  {
    catalogue.Register(new EditorCommand
    {
      Id = id,
      ContextOrder = contextOrder, ContextGroup = "5-room",
      Title = title,
      MenuPath = "Edit/Rooms/Room Shape",
      Group = "3-shape",
      Gestures = new[] { new KeyGesture(key, ModifierKeys.Primary) },
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Selection.Rooms.Any(),
      IsCheckedFunc = s => s.PrimaryRoom != null && s.PrimaryRoom.Shape == shape,
      ExecuteAsyncFunc = s => Run(() => s.SetRoomShape(shape))
    });
  }

  private static void RegisterSwap(
    CommandCatalogue catalogue,
    string id,
    string title,
    KeyGesture gesture,
    CommandScope scope,
    int contextOrder,
    System.Action<EditorSession> swap)
  {
    catalogue.Register(new EditorCommand
    {
      Id = id,
      ContextOrder = contextOrder, ContextGroup = "5-room",
      Title = title,
      MenuPath = "Edit/Rooms/Swap",
      Group = "4-swap",
      Scope = scope,
      Gestures = new[] { gesture },
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.SelectedRooms.Count == 2,
      ExecuteAsyncFunc = s => Run(() => swap(s))
    });
  }
}
