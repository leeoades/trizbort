using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

/// <summary>
///   Multi-level undo/redo. Each step is an <see cref="IUndoMemento" /> holding the state to go back to; undo and
///   redo swap the current state with the stored one. Edits are grouped with <see cref="Begin" /> transactions.
///   The stack belongs to one <see cref="EditorSession" />, which attaches itself on construction.
/// </summary>
public sealed class UndoStack
{
  private readonly int _capacity;
  private readonly List<Step> _undo = new();
  private readonly List<Step> _redo = new();

  private EditorSession _session;

  // Undo count at the last save; -1 when that position is no longer reachable (trimmed or redo-truncated).
  private int _savedIndex;

  // Open transaction state.
  private bool _isOpen;
  private int _depth;
  private int _serial;
  private string _openTitle;
  private int[] _openTouched;
  private SnapshotMemento _openBefore;

  public UndoStack(int capacity = 200)
  {
    if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
    _capacity = capacity;
  }

  public bool CanUndo => _undo.Count > 0;

  public bool CanRedo => _redo.Count > 0;

  public string UndoTitle => _undo.Count > 0 ? _undo[_undo.Count - 1].Title : null;

  public string RedoTitle => _redo.Count > 0 ? _redo[_redo.Count - 1].Title : null;

  /// <summary>True when the stack position differs from the position at the last <see cref="MarkSaved" />.</summary>
  public bool IsDirty => _savedIndex != _undo.Count;

  /// <summary>Raised when CanUndo, CanRedo, the titles or IsDirty may have changed.</summary>
  public event EventHandler Changed;

  public void MarkSaved()
  {
    _savedIndex = _undo.Count;
    RaiseChanged();
  }

  /// <summary>
  ///   Forget all history and treat the current state as saved (new or opened document). An open transaction is
  ///   abandoned without being recorded or rolled back: the document was replaced, so there is nothing to restore,
  ///   and the transaction's handle becomes a no-op.
  /// </summary>
  public void Clear()
  {
    if (_isOpen) CloseTransaction();

    _undo.Clear();
    _redo.Clear();
    _savedIndex = 0;
    RaiseChanged();
  }

  /// <summary>
  ///   Open a transaction: the map is snapshotted now, and disposing the returned handle records one step if the
  ///   map changed. Nested calls join the outer transaction.
  /// </summary>
  /// <param name="title">Shown as "Undo {title}".</param>
  /// <param name="touched">
  ///   Elements the edit will change. Stored on the step but ignored in v1; v2 element-level mementos (D1) will
  ///   use it.
  /// </param>
  public UndoTransaction Begin(string title, IEnumerable<Element> touched = null)
  {
    if (_session == null)
      throw new InvalidOperationException("The undo stack is not attached to an editor session.");

    if (_isOpen)
    {
      _depth++;
      return new UndoTransaction(this, _serial, false);
    }

    _openBefore = SnapshotMemento.Capture(_session);
    _openTitle = title ?? string.Empty;
    _openTouched = touched == null
      ? Array.Empty<int>()
      : touched.Where(element => element != null).Select(element => element.Id).ToArray();
    _isOpen = true;
    _depth = 1;
    _serial++;
    return new UndoTransaction(this, _serial, true);
  }

  public void Undo(EditorSession session)
  {
    if (session == null) throw new ArgumentNullException(nameof(session));
    EnsureNoTransaction();
    if (_undo.Count == 0) return;

    var step = _undo[_undo.Count - 1];
    var opposite = SnapshotMemento.Capture(session);
    step.Memento.Restore(session);

    _undo.RemoveAt(_undo.Count - 1);
    _redo.Add(new Step(step.Title, opposite, step.Touched));
    RaiseChanged();
  }

  public void Redo(EditorSession session)
  {
    if (session == null) throw new ArgumentNullException(nameof(session));
    EnsureNoTransaction();
    if (_redo.Count == 0) return;

    var step = _redo[_redo.Count - 1];
    var opposite = SnapshotMemento.Capture(session);
    step.Memento.Restore(session);

    _redo.RemoveAt(_redo.Count - 1);
    _undo.Add(new Step(step.Title, opposite, step.Touched));
    RaiseChanged();
  }

  internal void Attach(EditorSession session)
  {
    if (_session != null && !ReferenceEquals(_session, session))
      throw new InvalidOperationException("The undo stack already belongs to another editor session.");
    _session = session;
  }

  internal void EndTransaction(int serial, bool outermost)
  {
    if (!_isOpen || serial != _serial) return;

    if (!outermost)
    {
      _depth--;
      return;
    }

    var before = _openBefore;
    var title = _openTitle;
    var touched = _openTouched;
    CloseTransaction();

    // Hash the after-state instead of compressing it: only "did anything change" matters here.
    if (before.HasSameMap(SnapshotMemento.ComputeHash(_session))) return;

    // Recording a step discards the redo list; a saved position inside it is gone for good.
    var position = _undo.Count;
    if (_savedIndex > position) _savedIndex = -1;
    _redo.Clear();

    _undo.Add(new Step(title, before, touched));
    if (_undo.Count > _capacity)
    {
      _undo.RemoveAt(0);
      // Positions shift down by one; the state before the dropped step cannot be reached any more.
      if (_savedIndex >= 0) _savedIndex--;
    }

    RaiseChanged();
  }

  internal void CancelTransaction(int serial)
  {
    if (!_isOpen || serial != _serial) return;

    var before = _openBefore;
    CloseTransaction();
    before.Restore(_session);
    // The restore replaced the project object, so listeners must re-apply the dirty flag.
    RaiseChanged();
  }

  private void CloseTransaction()
  {
    _isOpen = false;
    _depth = 0;
    _openBefore = null;
    _openTitle = null;
    _openTouched = null;
  }

  private void EnsureNoTransaction()
  {
    if (_isOpen) throw new InvalidOperationException("Cannot undo or redo inside a transaction.");
  }

  private void RaiseChanged()
  {
    Changed?.Invoke(this, EventArgs.Empty);
  }

  private sealed class Step
  {
    public Step(string title, IUndoMemento memento, int[] touched)
    {
      Title = title;
      Memento = memento;
      Touched = touched;
    }

    public string Title { get; }

    public IUndoMemento Memento { get; }

    // Element ids (not elements, so old snapshots do not keep dead objects alive). Unused in v1.
    public int[] Touched { get; }
  }
}

/// <summary>
///   Handle for an open <see cref="UndoStack.Begin" /> transaction. Dispose to commit; call <see cref="Cancel" />
///   to roll back without recording a step. <see cref="Cancel" /> on a nested handle rolls back the whole
///   transaction.
/// </summary>
public sealed class UndoTransaction : IDisposable
{
  private readonly UndoStack _stack;
  private readonly int _serial;
  private readonly bool _outermost;
  private bool _finished;

  internal UndoTransaction(UndoStack stack, int serial, bool outermost)
  {
    _stack = stack;
    _serial = serial;
    _outermost = outermost;
  }

  /// <summary>Restore the state from when the transaction began and record nothing.</summary>
  public void Cancel()
  {
    if (_finished) return;
    _finished = true;
    _stack.CancelTransaction(_serial);
  }

  public void Dispose()
  {
    if (_finished) return;
    _finished = true;
    _stack.EndTransaction(_serial, _outermost);
  }
}
