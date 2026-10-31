using System;
using System.Collections.Generic;
using System.Text;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

/// <summary>
///   A key plus modifiers, as written in the shortcut map, e.g. <c>Primary+Shift+Z</c>.
/// </summary>
public readonly record struct KeyGesture(InputKey Key, ModifierKeys Modifiers)
{
  private static readonly Dictionary<string, InputKey> _aliases = new(StringComparer.OrdinalIgnoreCase)
  {
    ["Plus"] = InputKey.OemPlus,
    ["Minus"] = InputKey.OemMinus,
    ["Comma"] = InputKey.OemComma,
    ["NumPadAdd"] = InputKey.Add,
    ["NumPadSubtract"] = InputKey.Subtract,
    ["Esc"] = InputKey.Escape,
    ["Backspace"] = InputKey.Back,
    ["Return"] = InputKey.Enter
  };

  public static bool TryParse(string text, out KeyGesture gesture)
  {
    gesture = default;
    if (string.IsNullOrWhiteSpace(text)) return false;

    var parts = text.Split('+');
    var modifiers = ModifierKeys.None;
    for (var i = 0; i < parts.Length - 1; i++)
    {
      var part = parts[i].Trim();
      ModifierKeys flag;
      if (part.Equals("Primary", StringComparison.OrdinalIgnoreCase)) flag = ModifierKeys.Primary;
      else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) flag = ModifierKeys.Shift;
      else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) flag = ModifierKeys.Alt;
      else if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("Control", StringComparison.OrdinalIgnoreCase)) flag = ModifierKeys.Control;
      else return false;

      if ((modifiers & flag) != 0) return false;
      modifiers |= flag;
    }

    if (!TryParseKey(parts[parts.Length - 1].Trim(), out var key)) return false;
    gesture = new KeyGesture(key, modifiers);
    return true;
  }

  private static bool TryParseKey(string name, out InputKey key)
  {
    key = InputKey.None;
    if (name.Length == 0) return false;
    if (name.Length == 1 && name[0] >= '0' && name[0] <= '9')
    {
      key = InputKey.D0 + (name[0] - '0');
      return true;
    }
    if (_aliases.TryGetValue(name, out key)) return true;
    if ((name[0] >= '0' && name[0] <= '9')) return false;
    if (Enum.TryParse(name, true, out key) && key != InputKey.None && Enum.IsDefined(typeof(InputKey), key)) return true;
    key = InputKey.None;
    return false;
  }

  /// <summary>Canonical text form; round-trips through <see cref="TryParse" />.</summary>
  public override string ToString()
  {
    var sb = new StringBuilder();
    if ((Modifiers & ModifierKeys.Primary) != 0) sb.Append("Primary+");
    if ((Modifiers & ModifierKeys.Control) != 0) sb.Append("Ctrl+");
    if ((Modifiers & ModifierKeys.Alt) != 0) sb.Append("Alt+");
    if ((Modifiers & ModifierKeys.Shift) != 0) sb.Append("Shift+");
    sb.Append(KeyName(Key, false));
    return sb.ToString();
  }

  /// <summary>Human-readable form: <c>Ctrl+Shift+Z</c>, or <c>⌘⇧Z</c> on macOS.</summary>
  public string ToDisplayString(bool macOS)
  {
    var sb = new StringBuilder();
    if (macOS)
    {
      if ((Modifiers & ModifierKeys.Control) != 0) sb.Append('⌃');
      if ((Modifiers & ModifierKeys.Alt) != 0) sb.Append('⌥');
      if ((Modifiers & ModifierKeys.Shift) != 0) sb.Append('⇧');
      if ((Modifiers & ModifierKeys.Primary) != 0) sb.Append('⌘');
      sb.Append(KeyName(Key, true));
      return sb.ToString();
    }

    if ((Modifiers & (ModifierKeys.Primary | ModifierKeys.Control)) != 0) sb.Append("Ctrl+");
    if ((Modifiers & ModifierKeys.Alt) != 0) sb.Append("Alt+");
    if ((Modifiers & ModifierKeys.Shift) != 0) sb.Append("Shift+");
    sb.Append(KeyName(Key, true));
    return sb.ToString();
  }

  /// <summary>
  ///   True when the input is exactly this gesture. The host maps Cmd (macOS) or Ctrl (elsewhere) to
  ///   <see cref="ModifierKeys.Primary" /> first, so this is plain equality.
  /// </summary>
  public bool Matches(KeyInput input)
  {
    return input.Key == Key && input.Modifiers == Modifiers;
  }

  private static string KeyName(InputKey key, bool display)
  {
    if (key >= InputKey.D0 && key <= InputKey.D9) return ((int)(key - InputKey.D0)).ToString();
    if (!display) return key.ToString();
    switch (key)
    {
      case InputKey.Add:
      case InputKey.OemPlus: return "+";
      case InputKey.Subtract:
      case InputKey.OemMinus: return "-";
      case InputKey.OemComma: return ",";
      case InputKey.OemOpenBrackets: return "[";
      case InputKey.OemCloseBrackets: return "]";
      case InputKey.OemSemicolon: return ";";
      case InputKey.Oem5: return "\\";
      case InputKey.Back: return "Backspace";
      case InputKey.Escape: return "Esc";
      default: return key.ToString();
    }
  }
}
