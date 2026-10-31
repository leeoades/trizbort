using System.Linq;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>
  ///   Applies the macOS gesture variants of shortcut-map.md section 4 to the commands registered by
  ///   <see cref="CreateDefault" />. Idempotent. Debug and export commands choose their macOS gestures themselves.
  /// </summary>
  public static void ApplyMacVariants(CommandCatalogue catalogue)
  {
    // Cmd+Y is History in many macOS apps: Redo is Cmd+Shift+Z only.
    SetGestures(catalogue, "edit.redo", new KeyGesture(InputKey.Z, ModifierKeys.Primary | ModifierKeys.Shift));
    // Cmd+H hides the app.
    SetGestures(catalogue, "room.shape.square", new KeyGesture(InputKey.H, ModifierKeys.Control));
    // Cmd+F5 toggles VoiceOver.
    SetGestures(catalogue, "room.setStart", new KeyGesture(InputKey.F5, ModifierKeys.Primary | ModifierKeys.Shift));
    // Cmd+W closes the window.
    SetGestures(catalogue, "room.swap.names", new KeyGesture(InputKey.W, ModifierKeys.Control));
  }

  private static void SetGestures(CommandCatalogue catalogue, string id, params KeyGesture[] gestures)
  {
    var command = catalogue.Find(id);
    if (command == null) return;
    var updated = command with { Gestures = gestures.ToList() };
    var index = catalogue._commands.IndexOf(command);
    catalogue._commands[index] = updated;
    catalogue._byId[id] = updated;
  }
}
