using System;

namespace Trizbort.Domain.Enums;

[Flags]
public enum ModifierKeys
{
  None = 0,
  Shift = 1,
  Control = 2,
  Alt = 4,

  /// <summary>
  ///   Ctrl on Windows/Linux, Cmd on macOS. Only used by catalogue gestures; never persisted.
  /// </summary>
  Primary = 8
}
