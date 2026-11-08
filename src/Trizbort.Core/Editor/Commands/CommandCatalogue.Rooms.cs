using System.Collections.Generic;
using System.Linq;
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

    // Tidy: the auto-tidy, run over the whole map or the selected rooms, as one undo step.
    catalogue.Register(new EditorCommand
    {
      Id = "map.tidy",
      Title = "_Tidy Map",
      MenuPath = "Edit/Rooms",
      Group = "5-tidy",
      CanExecuteFunc = s => s.Project.Elements.OfType<Room>().Skip(1).Any(),
      ExecuteAsyncFunc = s => Run(() => Tidy(s, null))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "map.tidySelection",
      ContextOrder = 61, ContextGroup = "5-room",
      Title = "Tidy Selected Roo_ms",
      MenuPath = "Edit/Rooms",
      Group = "5-tidy",
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.SelectedRooms.Count > 1,
      ExecuteAsyncFunc = s => Run(() => Tidy(s, s.SelectedRooms.ToList()))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "map.tidySolver",
      Title = "Tidy Map with Sol_ver",
      MenuPath = "Edit/Rooms",
      Group = "5-tidy",
      CanExecuteFunc = s => s.Project.Elements.OfType<Room>().Skip(1).Any(),
      ExecuteAsyncFunc = s => Run(() => Tidy(s, null, true))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "map.tidySelectionSolver",
      Title = "Tidy Selected Rooms with Solve_r",
      MenuPath = "Edit/Rooms",
      Group = "5-tidy",
      CanExecuteFunc = s => s.SelectedRooms.Count > 1,
      ExecuteAsyncFunc = s => Run(() => Tidy(s, s.SelectedRooms.ToList(), true))
    });
  }

  /// <summary>How long Tidy Map and Tidy Selected Rooms may think: longer than the tidy after each automap move.</summary>
  private const int TidyTimeLimitMs = 3000;

  /// <summary>
  ///   Rearranges <paramref name="rooms" /> (every room when null) so joins point the way their directions say,
  ///   rooms keep off each other and off joins, and joins cross as little as possible; other rooms stay put.
  /// </summary>
  /// <param name="solver">
  ///   Lay the rooms out in one go with the constraint solver (LayoutTidy.SolverTidy) rather than improving the
  ///   layout a step at a time.
  /// </param>
  private static void Tidy(EditorSession session, IReadOnlyList<Room> rooms, bool solver = false)
  {
    var elements = session.Project.Elements.ToList();
    var targets = rooms ?? elements.OfType<Room>().ToList();
    session.Execute(rooms == null ? "Tidy map" : "Tidy selected rooms", () =>
    {
      // asked to tidy them, so they're no longer where the user put them by hand
      foreach (var room in targets) room.PlacedByHand = false;
      var moved = solver
        ? LayoutTidy.SolverTidy(elements, targets, SolverTimeLimitMs)
        : LayoutTidy.Tidy(elements, targets, TidyTimeLimitMs, rearrange: true);
      if (moved > 0) GuessedEnds.Face(session.Project.Elements, targets.ToList());
    });
  }

  /// <summary>How long the solver may search; it stops early when it knows its layout is the best.</summary>
  private const int SolverTimeLimitMs = 5000;

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
