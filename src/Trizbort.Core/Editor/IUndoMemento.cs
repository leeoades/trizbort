namespace Trizbort.Editor;

/// <summary>
///   One stored editor state that <see cref="UndoStack" /> can put back. The storage strategy (whole-map snapshot
///   in v1) is hidden behind this interface so it can change without touching commands.
/// </summary>
public interface IUndoMemento
{
  /// <summary>Approximate memory held by this memento, in bytes.</summary>
  long SizeBytes { get; }

  /// <summary>
  ///   Put the stored state back. Restoring recreates element objects, so the session raises
  ///   <see cref="EditorChange.Model" />, <see cref="EditorChange.Selection" /> and <see cref="EditorChange.Adorners" />.
  /// </summary>
  void Restore(EditorSession session);
}
