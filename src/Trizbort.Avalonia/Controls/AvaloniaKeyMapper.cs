using System;
using System.Collections.Generic;
using Avalonia.Input;
using Trizbort.Editor.Input;
using CoreKeyGesture = Trizbort.Editor.Commands.KeyGesture;
using CoreModifierKeys = Trizbort.Domain.Enums.ModifierKeys;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   Converts between Core's UI-neutral key types and Avalonia's. <c>Primary</c> is Ctrl on Windows/Linux and Cmd
///   (Meta) on macOS.
/// </summary>
public static class AvaloniaKeyMapper
{
    // Avalonia keys that are aliases of the same value (Prior/PageUp ...); the enum may print either name.
    private static readonly Dictionary<string, string> _aliasNames = new()
    {
        ["Prior"] = "PageUp",
        ["Next"] = "PageDown",
        ["Oem1"] = "OemSemicolon",
        ["Oem4"] = "OemOpenBrackets",
        ["Oem6"] = "OemCloseBrackets",
        ["OemPipe"] = "Oem5",
        ["Return"] = "Enter"
    };

    public static bool IsMacOS => OperatingSystem.IsMacOS();

    public static KeyModifiers ToKeyModifiers(CoreModifierKeys modifiers, bool macOS)
    {
        var result = KeyModifiers.None;
        if ((modifiers & CoreModifierKeys.Shift) != 0) result |= KeyModifiers.Shift;
        if ((modifiers & CoreModifierKeys.Alt) != 0) result |= KeyModifiers.Alt;
        if ((modifiers & CoreModifierKeys.Control) != 0) result |= KeyModifiers.Control;
        if ((modifiers & CoreModifierKeys.Primary) != 0) result |= macOS ? KeyModifiers.Meta : KeyModifiers.Control;
        return result;
    }

    public static CoreModifierKeys ToModifierKeys(KeyModifiers modifiers, bool macOS)
    {
        var result = CoreModifierKeys.None;
        if ((modifiers & KeyModifiers.Shift) != 0) result |= CoreModifierKeys.Shift;
        if ((modifiers & KeyModifiers.Alt) != 0) result |= CoreModifierKeys.Alt;
        if ((modifiers & KeyModifiers.Control) != 0) result |= macOS ? CoreModifierKeys.Control : CoreModifierKeys.Primary;
        if ((modifiers & KeyModifiers.Meta) != 0 && macOS) result |= CoreModifierKeys.Primary;
        return result;
    }

    public static bool TryToKey(InputKey key, out Key result)
    {
        result = Key.None;
        if (key == InputKey.None) return false;
        return Enum.TryParse(key.ToString(), out result) && result != Key.None;
    }

    public static InputKey ToInputKey(Key key)
    {
        var name = key.ToString();
        if (_aliasNames.TryGetValue(name, out var alias)) name = alias;
        return Enum.TryParse<InputKey>(name, out var result) && Enum.IsDefined(typeof(InputKey), result)
            ? result
            : InputKey.None;
    }

    public static KeyInput ToKeyInput(Key key, KeyModifiers modifiers, bool macOS)
    {
        return new KeyInput(ToInputKey(key), ToModifierKeys(modifiers, macOS));
    }

    public static bool TryToAvaloniaGesture(CoreKeyGesture gesture, bool macOS, out global::Avalonia.Input.KeyGesture result)
    {
        result = null!;
        if (!TryToKey(gesture.Key, out var key)) return false;
        result = new global::Avalonia.Input.KeyGesture(key, ToKeyModifiers(gesture.Modifiers, macOS));
        return true;
    }
}
