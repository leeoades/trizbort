using System;
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
      Title = "_Run Automap to Completion",
      MenuPath = "Project",
      Group = "2-automap-run",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.F5, none) },
      CanExecuteFunc = s => s.Automap.IsRunning,
      ExecuteAsyncFunc = s => Run(() => s.Automap.RunToCompletion())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "automap.step",
      Title = "Automap Single _Step (debug)",
      MenuPath = "Project",
      Group = "2-automap-run",
      Hidden = hidden,
      Scope = CommandScope.Canvas,
      // F11 is Show Desktop on macOS: use Primary+Shift+F11 there.
      Gestures = new[] { macOS ? new KeyGesture(InputKey.F11, ModifierKeys.Primary | ModifierKeys.Shift) : new KeyGesture(InputKey.F11, none) },
      CanExecuteFunc = s => s.DebugEnabled && s.Automap.IsRunning && s.Automap.SingleStep,
      ExecuteAsyncFunc = s => Run(() => s.Automap.Step())
    });
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
