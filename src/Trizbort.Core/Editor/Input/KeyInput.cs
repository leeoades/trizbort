using Trizbort.Domain.Enums;

namespace Trizbort.Editor.Input;

/// <summary>
///   A key press as seen by Core: a neutral key plus the modifiers held. UI hosts convert Cmd (macOS) or Ctrl
///   (elsewhere) to <see cref="ModifierKeys.Primary" /> before building one.
/// </summary>
public readonly record struct KeyInput(InputKey Key, ModifierKeys Modifiers);
