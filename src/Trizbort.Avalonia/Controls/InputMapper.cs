using Avalonia.Input;
using Trizbort.Editor;
using CoreModifierKeys = Trizbort.Domain.Enums.ModifierKeys;

namespace Trizbort.Avalonia.Controls;

/// <summary>Converts Avalonia pointer types to the neutral input types of <see cref="CanvasInteraction" /> and back.</summary>
internal static class InputMapper
{
    /// <summary>Pointer modifiers: Ctrl (and Cmd on macOS) is <c>Control</c>.</summary>
    public static CoreModifierKeys ToModifiers(KeyModifiers modifiers)
    {
        var result = CoreModifierKeys.None;
        if ((modifiers & KeyModifiers.Shift) != 0) result |= CoreModifierKeys.Shift;
        if ((modifiers & KeyModifiers.Alt) != 0) result |= CoreModifierKeys.Alt;
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0) result |= CoreModifierKeys.Control;
        return result;
    }

    public static PointerButton ToButton(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => PointerButton.Left,
            MouseButton.Middle => PointerButton.Middle,
            MouseButton.Right => PointerButton.Right,
            _ => PointerButton.None
        };
    }

    public static StandardCursorType ToCursor(CursorKind kind)
    {
        return kind switch
        {
            CursorKind.Hand => StandardCursorType.Hand,
            CursorKind.SizeAll => StandardCursorType.SizeAll,
            CursorKind.Pan => StandardCursorType.Hand,
            CursorKind.Cross => StandardCursorType.Cross,
            CursorKind.ResizeNWSE => StandardCursorType.TopLeftCorner,
            CursorKind.ResizeNESW => StandardCursorType.TopRightCorner,
            CursorKind.ResizeNS => StandardCursorType.SizeNorthSouth,
            CursorKind.ResizeEW => StandardCursorType.SizeWestEast,
            CursorKind.DrawLine => StandardCursorType.Cross,
            CursorKind.NoMove => StandardCursorType.No,
            _ => StandardCursorType.Arrow
        };
    }
}
