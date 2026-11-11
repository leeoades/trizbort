using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.AppSettings;
using Trizbort.Editor.Input;
using Trizbort.Domain.Enums;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>
  ///   Debug toggles (055) and the automap run-to-completion and single-step keys (061). The debug commands only
  ///   work, and only appear in menus, when the app was started with <c>--debug</c> (<see cref="DebugOptions" />).
  /// </summary>
  private static void RegisterDebugCommands(CommandCatalogue catalogue)
  {
    var none = ModifierKeys.None;
    var macOS = OperatingSystem.IsMacOS();
    var hidden = !DebugOptions.Enabled;
    KeyGesture? noGesture = null;
    // Ctrl+F1..F3 are taken by macOS (menu bar, Dock): there the toggles live in the Developer menu only.
    var menu = macOS ? "Developer" : "View/Debug";

    catalogue.Register(DebugToggle(
      "debug.fps", "Show _FPS", menu, hidden,
      macOS ? noGesture : new KeyGesture(InputKey.F1, ModifierKeys.Control),
      s => ApplicationSettingsController.AppSettings.DebugShowFps,
      s => s.ToggleDebugFps()));
    catalogue.Register(DebugToggle(
      "debug.mouseCoords", "Show _Mouse Coordinates", menu, hidden,
      macOS ? noGesture : new KeyGesture(InputKey.F1, ModifierKeys.Shift),
      s => ApplicationSettingsController.AppSettings.DebugShowMouseCoordinates,
      s => s.ToggleDebugMouseCoordinates()));
    catalogue.Register(DebugToggle(
      "debug.elementRendering", "Draw _Elements", menu, hidden,
      macOS ? noGesture : new KeyGesture(InputKey.F2, ModifierKeys.Control),
      s => !ApplicationSettingsController.AppSettings.DebugDisableElementRendering,
      s => s.ToggleDebugElementRendering()));
    catalogue.Register(DebugToggle(
      "debug.lineRendering", "Draw _Lines", menu, hidden,
      macOS ? noGesture : new KeyGesture(InputKey.F3, ModifierKeys.Control),
      s => !ApplicationSettingsController.AppSettings.DebugDisableLineRendering,
      s => s.ToggleDebugLineRendering()));
    // Ctrl+F5 is Start Room, so the grid polyline toggle has no key.
    catalogue.Register(DebugToggle(
      "debug.gridPolyline", "Draw Grid _Polyline", menu, hidden,
      noGesture,
      s => !ApplicationSettingsController.AppSettings.DebugDisableGridPolyline,
      s => s.ToggleDebugGridPolyline()));

    // Automap keys (061). Exact modifiers only; the reference's "any modifier except Ctrl" rule is not kept.
    catalogue.Register(new EditorCommand
    {
      Id = "automap.run",
      Title = "_Play to the End",
      MenuPath = "Project/Automap",
      Group = "2-automap/2-transcript",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.F5, none) },
      CanExecuteFunc = s => s.Automap.IsRunning,
      ExecuteAsyncFunc = PlayToEndAsync
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.pause",
      Title = "P_ause",
      MenuPath = "Project/Automap",
      Group = "2-automap/2-transcript",
      CanExecuteFunc = s => s.Automap.IsRunning && !s.Automap.MoveByMove,
      ExecuteAsyncFunc = s => Run(() => s.Automap.Pause())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.nextMove",
      Title = "_Next Move",
      MenuPath = "Project/Automap",
      Group = "2-automap/2-transcript",
      Scope = CommandScope.Canvas,
      // F10, as stepping over a line in a debugger; F5 plays to the end
      Gestures = new[] { new KeyGesture(InputKey.F10, none) },
      CanExecuteFunc = s => s.Automap.IsRunning,
      ExecuteAsyncFunc = s => Run(() => s.Automap.PlayNextMove())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.back",
      Title = "P_revious Move",
      MenuPath = "Project/Automap",
      Group = "2-automap/2-transcript",
      Scope = CommandScope.Canvas,
      // F9, beside F10 for the next move: read the transcript again from the move before, map left as it is
      Gestures = new[] { new KeyGesture(InputKey.F9, none) },
      CanExecuteFunc = s => s.Automap.IsRunning,
      ExecuteAsyncFunc = s => Run(() => s.Automap.GoBack())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.suspend",
      Title = "_Tracking Only",
      MenuPath = "Project/Automap",
      Group = "2-automap/3-mapping",
      CanExecuteFunc = s => s.Automap.IsRunning,
      IsCheckedFunc = s => s.Automap.Suspended,
      ExecuteAsyncFunc = s => Run(() =>
      {
        if (s.Automap.Suspended) s.Automap.Resume();
        else s.Automap.Suspend();
      })
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.imHere",
      ContextOrder = 64, ContextGroup = "5-room",
      Title = "_I'm Here",
      ContextTitle = "Automap: I'm _Here",
      MenuPath = "Project/Automap",
      Group = "2-automap/4-where",
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Automap.IsRunning && s.PrimaryRoom != null && s.Selection.Count == 1,
      ExecuteAsyncFunc = s => Run(() => s.Automap.SetCurrentRoom(s.PrimaryRoom))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.differentRoom",
      ContextOrder = 64, ContextGroup = "5-room",
      Title = "This Is a _Different Room",
      ContextTitle = "Automap: This Is a _Different Room",
      MenuPath = "Project/Automap",
      Group = "2-automap/4-where",
      ContextTargets = ContextTargets.Room,
      // automap took the player to be in a room on the map, but it's another with that name: split it off
      CanExecuteFunc = s => s.Automap.IsRunning && s.Automap.CanSplitCurrentRoom && s.Selection.Count == 1 &&
                            s.PrimaryRoom != null && s.PrimaryRoom == s.Automap.CurrentRoom,
      ExecuteAsyncFunc = s => s.Automap.SplitCurrentRoomAsync()
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.currentRoom",
      Title = "_Centre on Current Room",
      MenuPath = "Project/Automap",
      Group = "2-automap/4-where",
      Scope = CommandScope.Canvas,
      CanExecuteFunc = s => s.Automap.IsRunning && s.Automap.CurrentRoom != null,
      ExecuteAsyncFunc = s => Run(() => GoToAutomapRoom(s))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.step",
      Title = "Single _Step (debug)",
      MenuPath = "Project/Automap",
      Group = "2-automap/9-debug",
      Hidden = hidden,
      Scope = CommandScope.Canvas,
      // F11 is Show Desktop on macOS: use Primary+Shift+F11 there.
      Gestures = new[] { macOS ? new KeyGesture(InputKey.F11, ModifierKeys.Primary | ModifierKeys.Shift) : new KeyGesture(InputKey.F11, none) },
      CanExecuteFunc = s => s.DebugEnabled && s.Automap.IsRunning && s.Automap.SingleStep,
      ExecuteAsyncFunc = s => Run(() => s.Automap.Step())
    });
  }

  /// <summary>Playing to the end with at least this many moves left asks first: it can't be stopped part way back.</summary>
  public const int ConfirmPlayToEndMoves = 20;

  /// <summary>
  ///   Plays the rest of the transcript without pausing. When automap is paused with many moves left, asks first: a
  ///   slip of the finger shouldn't map the whole game in one go.
  /// </summary>
  public static async Task PlayToEndAsync(EditorSession session)
  {
    var automap = session.Automap;
    if (!automap.IsRunning) return;
    if (automap.MoveByMove)
    {
      var moves = automap.MovesAhead();
      if (moves >= ConfirmPlayToEndMoves &&
          !await session.Prompts.ConfirmAsync(
            $"There are {moves} moves left in the transcript. Play them all without pausing?\n\n" +
            "Automap maps each move as it goes, then keeps following the game. Pause stops it at any point.",
            "Play to the End"))
        return;
    }

    automap.RunToCompletion();
  }

  /// <summary>Selects the room automap thinks the player is in and centres the map on it.</summary>
  public static void GoToAutomapRoom(EditorSession session)
  {
    var room = session.Automap.CurrentRoom;
    if (room == null || !session.Project.Elements.Contains(room)) return;
    session.Selection.Set(new List<Element> { room });
    session.CenterOnElement(room);
  }

  private static EditorCommand DebugToggle(
    string id,
    string title,
    string menuPath,
    bool hidden,
    KeyGesture? gesture,
    Func<EditorSession, bool> isChecked,
    Action<EditorSession> toggle)
  {
    return new EditorCommand
    {
      Id = id,
      Title = title,
      MenuPath = menuPath,
      Group = "1-debug",
      Hidden = hidden,
      Scope = CommandScope.Canvas,
      Gestures = gesture.HasValue ? new[] { gesture.Value } : Array.Empty<KeyGesture>(),
      CanExecuteFunc = s => s.DebugEnabled,
      IsCheckedFunc = isChecked,
      ExecuteAsyncFunc = s => Run(() => toggle(s))
    };
  }
}
