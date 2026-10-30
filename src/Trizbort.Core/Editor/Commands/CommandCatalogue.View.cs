using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>Zoom presets and keys, and the plain/Primary+Alt arrow keys. (Undo/redo live in CreateDefault.)</summary>
  private static void RegisterViewCommands(CommandCatalogue catalogue)
  {
    var primary = ModifierKeys.Primary;
    var shift = ModifierKeys.Shift;
    var none = ModifierKeys.None;

    catalogue.Register(new EditorCommand
    {
      Id = "view.zoom50",
      Title = "Zoom _50%",
      MenuPath = "View",
      Group = "1-zoom-presets",
      ExecuteAsyncFunc = s => Run(() => s.Viewport.SetZoom(0.5))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.zoom200",
      Title = "Zoom _200%",
      MenuPath = "View",
      Group = "1-zoom-presets",
      ExecuteAsyncFunc = s => Run(() => s.Viewport.SetZoom(2.0))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.zoomMicroIn",
      Title = "Zoom In 1%",
      MenuPath = "View",
      Group = "1-zoom-presets",
      ExecuteAsyncFunc = s => Run(() => s.Viewport.ZoomMicroIn())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.zoomMicroOut",
      Title = "Zoom Out 1%",
      MenuPath = "View",
      Group = "1-zoom-presets",
      ExecuteAsyncFunc = s => Run(() => s.Viewport.ZoomMicroOut())
    });

    // The bare +/- keys zoom whatever modifiers are held (as the reference does); canvas scope so typing in a text
    // box is unaffected. Primary+= / Primary+- stay on the window-scope view.zoomIn / view.zoomOut.
    catalogue.Register(new EditorCommand
    {
      Id = "view.zoomIn.key",
      Title = "Zoom In (key)",
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[]
      {
        new KeyGesture(InputKey.OemPlus, none),
        new KeyGesture(InputKey.Add, none),
        new KeyGesture(InputKey.OemPlus, shift),
        new KeyGesture(InputKey.Add, shift)
      },
      ExecuteAsyncFunc = s => Run(() => s.Viewport.ZoomIn())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "view.zoomOut.key",
      Title = "Zoom Out (key)",
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[]
      {
        new KeyGesture(InputKey.OemMinus, none),
        new KeyGesture(InputKey.Subtract, none),
        new KeyGesture(InputKey.OemMinus, shift),
        new KeyGesture(InputKey.Subtract, shift)
      },
      ExecuteAsyncFunc = s => Run(() => s.Viewport.ZoomOut())
    });

    RegisterArrow(catalogue, "left", InputKey.Left, new Vector(-1, 0));
    RegisterArrow(catalogue, "right", InputKey.Right, new Vector(1, 0));
    RegisterArrow(catalogue, "up", InputKey.Up, new Vector(0, -1));
    RegisterArrow(catalogue, "down", InputKey.Down, new Vector(0, 1));
  }

  private static void RegisterArrow(CommandCatalogue catalogue, string name, InputKey key, Vector unit)
  {
    // Shift+arrow is left to the navigation commands (ticket 047); the session offers a fast pan for it.
    catalogue.Register(new EditorCommand
    {
      Id = "selection.move." + name,
      Title = "Move or Pan " + name,
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(key, ModifierKeys.None) },
      ExecuteAsyncFunc = s => Run(() => s.ArrowMoveOrPan(unit))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "selection.resize." + name,
      Title = "Resize " + name,
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(key, ModifierKeys.Primary | ModifierKeys.Alt) },
      CanExecuteFunc = s => s.CanResizeSelection,
      ExecuteAsyncFunc = s => Run(() => s.ArrowResize(unit))
    });
  }
}
