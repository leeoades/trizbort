using System;

namespace Trizbort.Editor.Commands;

public enum CommandScope
{
  /// <summary>Fires wherever focus is inside the window.</summary>
  Window,

  /// <summary>Fires only when the map canvas has focus and no text box is editing.</summary>
  Canvas
}

[Flags]
public enum ContextTargets
{
  None = 0,
  Room = 1,
  Connection = 2,
  Label = 4,
  Empty = 8,
  Any = 15
}
