using System;

namespace Trizbort.Editor;

/// <summary>
///   What part of the editor state changed. Raised through <see cref="EditorSession.Changed" />.
/// </summary>
[Flags]
public enum EditorChange
{
  None = 0,
  Model = 1,
  Selection = 2,
  Viewport = 4,
  Adorners = 8,
  Dirty = 16,

  /// <summary>Only the pointer position changed (status-bar readout); nothing needs redrawing.</summary>
  Pointer = 32
}
