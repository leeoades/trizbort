# 002 — Add EditorSession to Core; CommandService becomes a thin adapter

Matrix rows: X053, X010, X048, X052 (foundation F2; D1)          Priority: P1          Size: M
Depends on: 001        Suggested tier: mid

## What
No user-visible change. `EditorSession` in Core owns the project, `Selection`, `Viewport` and a single `Changed(EditorChange)` event. Avalonia's `CommandService` keeps implementing `ICommandService` but only delegates to the session, so the app and all existing Avalonia tests keep working.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D1 points 1, 2, 5.
- `src/Trizbort.Avalonia/Services/ICommandService.cs` (whole, 49 lines) and `CommandService.cs` — constructor (~82-103: it wires `Project.SelectedElementsProvider`, listens to `Project.ProjectChanged`), `NewMap`, `AddRoom`, `AddMapLabel`, `DeleteSelected`, `NudgeSelected`, `CopySelected/CutSelected/Paste/PasteRooms` (~273-410), `ToggleGrid`.
- `src/Trizbort.Core/Domain/Application/Project.cs` — `Current`, `ProjectChanged`, `ActiveSelectedElement`, `SelectedElementsProvider`, `IsDirty`.
- `src/Trizbort.Core/Domain/Controllers/CopyController.cs` — `SetClipboardTextHandler`/`GetClipboardTextHandler`.
- `src/Trizbort.Avalonia.Tests/CommandServiceViewTests.cs` and `MapCanvasControlSyncTests.cs` — must stay green unchanged.

## Implement
1. Core `src/Trizbort.Core/Editor/EditorChange.cs`: `[Flags] public enum EditorChange { None=0, Model=1, Selection=2, Viewport=4, Adorners=8, Dirty=16 }`.
2. Core `src/Trizbort.Core/Editor/EditorSession.cs` — `public sealed class EditorSession : IDisposable`:
   - `Project Project => Project.Current` (re-read each time; `NewProject()` replaces `Project.Current` like `CommandService.NewMap` does, incl. `Settings.Reset()` — copy that code).
   - `Selection Selection`, `Viewport Viewport`, `bool ShowGrid` (view state, raises `Viewport`).
   - `event Action<EditorChange>? Changed` (a single `EventHandler<EditorChangedEventArgs>` with `.Change` is fine). It is raised from: `Selection.Changed` → `Selection`, `Viewport.Changed` → `Viewport`, `Project.ProjectChanged` → `Model`, `Project.IsDirty` changes → `Dirty` (poll after Execute; see 004), and `Invalidate(EditorChange)`.
   - Keep `Project.ActiveSelectedElement` == `Selection.Primary` and set `Project.SelectedElementsProvider` to return `Selection.Items` (what `CommandService` does today).
   - Move these bodies out of `CommandService` into session methods with the same behaviour, taking explicit world positions (no UI types): `Room AddRoom(Vector? position)`, `MapLabel AddMapLabel(Vector? position)`, `int DeleteSelected()`, `void NudgeSelected(Vector delta)` (uses `MapEditing.Move`), `string? CopySelected()`, `string? CutSelected()`, `void Paste(string? clipboardText, Vector? target)` (+ private `PasteRooms`), `SelectAll()`, `ClearSelection()`. Clipboard text stays passed in/out as a string; the `_internalClipboard` field moves into the session as an internal fallback until an `IClipboard` exists (ticket 004/Phase-4 clipboard work).
   - These methods do NOT set `IsDirty` differently from today (ticket 004 does that).
3. Avalonia `CommandService`: constructor creates (or receives via optional ctor parameter) an `EditorSession`; expose `public EditorSession Session { get; }`. Every `ICommandService` member delegates to it. `SelectedElements` returns a read-only set view over `Session.Selection.Items` (`IReadOnlySet<Element>` — wrap in a small private class, ordering of the set is not relied on). Map the old events: `SelectionChanged` ← `EditorChange.Selection`, `ZoomChanged` ← `Viewport` (pass `Viewport.ZoomFactor`), `ProjectChanged` ← `Model`, `CanvasInvalidated` ← `Invalidate…`/`Adorners`. `ICommandService` itself does not change in this ticket.
4. Register nothing new in DI unless `CommandService` is already registered (keep its registration, grep `AddSingleton<ICommandService`).

## Tests
- Core: `src/Trizbort.Core.Tests/EditorSessionTests.cs` — `AddRoom_AtPosition_AddsAndSelects`, `DeleteSelected_RemovesRoomAndItsConnections` (confirms cascade, answers matrix open question 6), `NudgeSelected_MovesAllSelected`, `CopyPaste_RoundTrip_CreatesRemappedCopies` (ids differ, connection between pasted rooms re-docked), `Selection_Change_RaisesChangedWithSelectionFlag`, `Viewport_Change_RaisesChangedWithViewportFlag`, `ProjectActiveSelectedElement_TracksPrimary`, `NewProject_ReplacesProjectCurrent_AndRaisesModel`. Reset `Project.Current` in each test.
- Avalonia: no new tests; run `CommandServiceViewTests`, `MapCanvasControlGestureTests`, `MapCanvasControlSyncTests`, `AutomapIntegrationTests` unchanged.

## Done when
- Build + Core tests + Avalonia tests pass on Linux, with no existing test edited (except mechanical ctor changes, listed in the commit message).
- `CommandService.cs` contains no selection/zoom/paste logic (delegation only; < ~150 lines).
- Matrix rows X010, X048, X053 updated with evidence; commit `parity(002): Add EditorSession to Core`.

## Out of scope
- Undo (003), removing hand-set `IsDirty` (004), moving hit-testing/drag code (005–007), `IClipboard`/`IEditorPrompts`, removing `Project.Current`, changing `MapCanvasControl` (it keeps using `ICommandService`).
