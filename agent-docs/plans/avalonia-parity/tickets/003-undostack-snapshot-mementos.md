# 003 — Add UndoStack with gzip snapshot mementos, transactions and dirty flag

Matrix rows: New-in-Avalonia (undo/redo is P1, L3); X052 (foundation F3; D1 point 3)          Priority: P1          Size: L (strong-tier review of the diff)
Depends on: 002        Suggested tier: mid (implementation), strong (diff review)

## What
A multi-level undo/redo stack in Core. Each step stores a gzip-compressed whole-map snapshot behind an `IUndoMemento` interface; transactions group edits into one step; the project dirty flag becomes "stack position ≠ position at last save". No UI yet (008) and existing mutations are not routed yet (004).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D1 point 3 (storage, grouping, performance gate, v2 hint). Source of truth.
- `src/Trizbort.Core/Domain/Application/LegacyMapFileEngine.cs` — `Load(string fileName)` (line ~42) and `Save(string fileName)` (~133); find how they serialise (grep `XmlWriter`/`XmlTextWriter`, `Project.Current`) so you can add stream/string overloads without changing file format.
- `src/Trizbort.Core/Domain/Application/Project.cs` — `IsDirty` (line 60, set false at ~165 after save), `Elements`, `Current`.
- `scripts/bench/undo-snapshot-bench.cs` — Lee's benchmark; copy its approach for the synthetic 1,000-room map.
- `src/Trizbort.Core.Tests/PersistenceRegressionTests.cs` — how tests round-trip maps.

## Implement
Files in `src/Trizbort.Core/Editor/` (namespace `Trizbort.Editor`):
1. Persistence helpers (prerequisite, keep small): in `LegacyMapFileEngine` add `public void Save(Stream stream)` and `public bool Load(Stream stream)` (or `SaveToString`/`LoadFromString`) that share the code with the file versions. File output must stay byte-identical (existing persistence tests prove it). If this grows beyond a small refactor, stop and report instead of rewriting.
2. `IUndoMemento.cs`: `public interface IUndoMemento { void Restore(EditorSession session); long SizeBytes { get; } }`.
3. `SnapshotMemento.cs`: `internal sealed class SnapshotMemento : IUndoMemento` — `static SnapshotMemento Capture(EditorSession s)` serialises the current project via `LegacyMapFileEngine` to a `MemoryStream` → `GZipStream(CompressionLevel.Fastest)`; stores `byte[]` plus the selection as element ids (`IReadOnlyList<int>`; check the id property name on `Element`). `Restore`: decompress, load into a **fresh** `Project`, swap it in as `Project.Current` (reuse `EditorSession.ReplaceProject(Project)` — add it in this ticket; it must raise `Model`), then reselect by id and set `Selection`. Restoring recreates element objects: the session must raise `EditorChange.Model | Selection | Adorners` so the UI drops stale references (see ticket 006 for hover/drag state).
4. `UndoStack.cs`: `public sealed class UndoStack`:
   - `UndoStack(int capacity = 200)`.
   - `bool CanUndo`, `bool CanRedo`, `string? UndoTitle`, `string? RedoTitle`, `bool IsDirty` (= current index ≠ saved index; a saved index discarded by trimming/redo-truncation makes it permanently dirty until the next `MarkSaved`), `void MarkSaved()`.
   - `IDisposable Begin(string title, IEnumerable<Element>? touched = null)` — opens a transaction: captures the *before* memento now; nested `Begin` calls join the outer transaction (depth counter). Disposing the outermost transaction records the step *only if* the map actually changed (compare the after-snapshot bytes to the before bytes; if equal, discard). The `touched` hint is stored on the step and ignored in v1 (comment: v2 element-level mementos, D1).
   - `void Cancel()` on the transaction handle (`UndoTransaction : IDisposable { void Cancel(); }`) — restore the before-state **without** recording a step (used by Esc during a drag, ticket 006).
   - `void Undo(EditorSession)`, `void Redo(EditorSession)` — swap: capture the current state as the opposite memento, then `Restore` the other. Recording a new step clears the redo list. Exceeding capacity drops the oldest step.
   - `event EventHandler? Changed` (CanUndo/CanRedo/Dirty/titles changed).
5. `EditorSession`: add `UndoStack Undo { get; }`, `IDisposable Execute(string title, Action edit, IEnumerable<Element>? touched = null)` helper (= `Begin` + run + dispose; rolls back via `Cancel()` and rethrows if `edit` throws), `IDisposable BeginTransaction(string title, IEnumerable<Element>? touched = null)`, `void Undo()`, `void Redo()`. After undo/redo/execute the session sets `Project.IsDirty = Undo.IsDirty` and raises `EditorChange.Dirty`. Make `Project.IsDirty` still settable (legacy code) but the session overrides it after each step; ticket 004 removes the hand-set sites. Hook `StorageService` save success to call `session.MarkSaved()` in ticket 004, not here.

## Tests
`src/Trizbort.Core.Tests/UndoStackTests.cs` (build maps with `Project.Current` reset per test):
- `Undo_RestoresPreviousMap` / `Redo_ReappliesEdit` (add a room, undo → 0 rooms, redo → 1).
- `Undo_DeleteRoom_RestoresCascadedConnections` (delete a room with 2 connections; undo → both connections back, docked to the room's ports).
- `Undo_RestoresSelectionById`.
- `NewEditAfterUndo_ClearsRedo`.
- `Capacity_DropsOldestStep` (capacity 3, 5 edits → 3 undos available).
- `Transaction_Nested_RecordsOneStep`; `Transaction_NoChange_RecordsNothing`; `Transaction_Cancel_RestoresAndRecordsNothing`; `Execute_Throws_RollsBackAndRethrows`.
- `Dirty_FalseAfterMarkSaved`, `Dirty_TrueAfterEdit`, `Dirty_FalseAgainAfterUndoToSavedPoint`, `Dirty_StaysTrueWhenSavedStepWasTrimmedOrRedoTruncated`.
- `Touched_Hint_IsAccepted_AndIgnored` (stored on the step, no behaviour change).
- `UndoTitle_ReflectsLastStep`.
- Perf gate `UndoPerformanceTests.cs`: synthetic 1,000-room / ~1,900-connection map (port the bench generator) — `RecordStep_Under50ms` and `RestoreStep_Under250ms` (take the median of 5; assert with `Stopwatch`; mark the test `[Trait("Category","Perf")]` if the repo uses traits, otherwise no skip).
- Memory sanity: `Snapshot_1000Rooms_GzipSmallerThan20PercentOfXml`.

## Done when
- Build + Core + Avalonia tests pass on Linux; legacy file output unchanged (persistence tests green).
- Perf gate test green with the thresholds above.
- `AGENTS.md` line "No undo/redo exists" is NOT changed yet (changes in ticket 008).
- Commit `parity(003): Add UndoStack with snapshot mementos`; reviewer is a strong-tier session reading only the diff and D1.
- If > 1 day: split as 003a (stream overloads + `SnapshotMemento` + tests) and 003b (`UndoStack` transactions/dirty + perf gate) inside the same ticket, committed separately.

## Out of scope
- Routing existing mutations (004), UI/menu/shortcuts (008), drag transactions (006), element-level mementos (D1 v2), persisting undo history, undo of app/map-theme settings outside the map file.
