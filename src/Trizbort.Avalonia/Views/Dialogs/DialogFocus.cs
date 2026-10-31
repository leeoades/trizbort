using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>
///   Makes a modal dialog take keyboard focus when it opens. Some window managers (for example COSMIC on Linux)
///   do not give a freshly shown modal window the keyboard focus, so Enter / Esc went to the main window.
/// </summary>
public static class DialogFocus
{
    /// <summary>Activates <paramref name="dialog" /> and focuses its first input when it is opened.</summary>
    public static void Prepare(Window dialog)
    {
        dialog.Opened += (_, _) =>
        {
            dialog.Activate();
            Dispatcher.UIThread.Post(() => FocusFirstInput(dialog), DispatcherPriority.Loaded);
        };
    }

    /// <summary>
    ///   Activates the dialog and, unless focus is already inside it, focuses the first enabled input
    ///   (falling back to the default button).
    /// </summary>
    public static void FocusFirstInput(Window dialog)
    {
        dialog.Activate();

        var focused = dialog.FocusManager?.GetFocusedElement() as Visual;
        if (focused != null && focused.GetVisualAncestors().Contains(dialog))
        {
            return;
        }

        var descendants = dialog.GetVisualDescendants().OfType<Control>().ToList();
        var first = descendants.FirstOrDefault(IsInput)
                    ?? descendants.OfType<Button>().FirstOrDefault(b => b.IsDefault && CanFocus(b))
                    ?? descendants.OfType<Button>().FirstOrDefault(CanFocus);
        first?.Focus();
    }

    private static bool CanFocus(Control control) =>
        control.Focusable && control.IsTabStop && control.IsEffectivelyVisible && control.IsEffectivelyEnabled;

    private static bool IsInput(Control control) =>
        control is not Button and not TabItem and not ScrollViewer and not Expander && CanFocus(control);
}
