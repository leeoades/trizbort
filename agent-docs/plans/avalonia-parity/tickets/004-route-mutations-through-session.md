# 004 — Route existing mutations through the EditorSession; remove hand-set IsDirty

Matrix rows: X012, X013, X014, X015, X021, X022, X052; M024, M026, M027, M028, M029, M030 (foundation F4; D1 point 2)          Priority: P1          Size: M
Depends on: 003        Suggested tier: mid

## What
Every edit that exists today (add room/label, delete, cut/paste, arrow-key nudge and resize, properties-dialog OK, map settings OK, automap batch) runs inside an undo transaction, and the dirty flag comes from the undo stack. No visible change except that these edits become undoable (the Undo command itself arrives in 008, but tests can call `session.Undo()`).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D1 points 2 and 3 ("Grouping").
- `src/Trizbort.Core/Editor/EditorSession.cs`, `UndoStack.cs` (tickets 002, 003).
- Hand-set `IsDirty` sites, found with `grep -rn "IsDirty = true" src --include=*.cs | grep -v Tests`: `Services/CommandService.cs` (AddRoom, AddMapLabel, DeleteSelected, NudgeSelected, Paste, PasteRooms), `Services/DialogService.cs` (~237, 253, 269: Room/Connection/MapLabel properties OK), `ViewModels/MapSettingsViewModel.cs` (~230-233, 297, 324), `Controls/MapCanvasControl.cs` (~906, 917, 952, 963 = drags; ~1161 = marquee/EndDrag; ~1343 = Ctrl+Alt resize). Also `StorageService.cs:67` sets `IsDirty = false` after save.
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — automap start/stop (`ToggleAutomapCommand`, `_automapTask`).
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — `HandleArrowKey` (~1318-1364).

## Implement
1. Core `EditorSession`: wrap in `Execute("<title>", …)`: `AddRoom` ("Add room"), `AddMapLabel` ("Add label"), `DeleteSelected` ("Delete"), `NudgeSelected` ("Move"), `Paste` ("Paste"), `CutSelected` ("Cut"). Pass the affected elements as `touched`. Delete a room undone must bring back its cascaded connections (snapshot does this).
2. Add `EditorSession.ResizeSelected(Vector delta)` (move the Ctrl+Alt+arrow logic out of `HandleArrowKey`; keep the `GridSize` minimum) in a "Resize" transaction; `HandleArrowKey` calls it. Consecutive nudges/resizes from key repeat each make a step (acceptable; note it).
3. Dialogs: add `EditorSession.Apply(string title, Element element, Action apply)` = `Execute(title, apply, new[]{element})`. In `DialogService.Show{Room,Connection,MapLabel}PropertiesAsync` replace `viewModel.ApplyTo(x); Project.Current.IsDirty = true;` with `session.Apply("Edit room"/"Edit connection"/"Edit label", x, () => viewModel.ApplyTo(x))`. Inject `EditorSession` (or `ICommandService.Session`) into `DialogService`. `MapSettingsViewModel` and map-theme OK paths: wrap the whole apply in one `Execute("Map settings", …)` and delete their `IsDirty = true` lines. App-level settings (`ShowAppSettingsAsync`) are not map state: leave alone.
4. Automap: in `MainWindowViewModel`, open one transaction for the whole automap run: `var tx = session.BeginTransaction("Automap")` before `AutomapEngine.Start…`, dispose it when the run ends or is stopped/cancelled (finally block). Decision recorded: one undo step per automap run (plan §7). Because automap edits run on the UI thread via `AvaloniaAutomapCanvas`, no further marshalling changes.
5. Drags and marquee in `MapCanvasControl`: delete the `Project.Current.IsDirty = true` lines at ~906/917/952/963/1161 and instead wrap the *whole drag* in a transaction — begin in `OnPointerPressed` when a MoveElement/Resize/MovePort/MoveWaypoint mode starts, dispose in `EndDrag(…, cancelled:false)`, `Cancel()` in `EndDrag(…, cancelled:true)`. This is a stop-gap; ticket 006 moves it into Core. Keep Avalonia's existing manual restore code working (Cancel also restores — whichever runs second must be harmless; add a test).
6. Dirty flag: `StorageService` after a successful save calls `session.MarkSaved()` instead of `project.IsDirty = false`; after New/Open call `session.Undo.Clear()` (add `UndoStack.Clear()` + `MarkSaved()`; document it). `Project.IsDirty` is driven by the session; remove every remaining `IsDirty = true` in non-test code (re-run the grep; the result must be empty except the session itself).

## Tests
- Core `src/Trizbort.Core.Tests/EditorSessionUndoTests.cs`: `AddRoom_IsUndoable`, `Delete_IsUndoable_WithConnections`, `Paste_IsOneStep_AndUndoRemovesAllPastedElements`, `Nudge_IsUndoable`, `ResizeSelected_RespectsGridMinimum_AndIsUndoable`, `Apply_Dialog_IsOneStep`, `Apply_NoChange_RecordsNoStep`, `AutomapBatch_OneStep` (open transaction, run `AutomapCanvas.CreateRoom`/`Connect` three times from a test — see `AutomapRegressionTests.cs` for setup — dispose, one `Undo()` removes all), `Dirty_ClearAfterUndoToSaved`.
- Avalonia `CommandServiceViewTests.cs`: `AddRoom_MarksProjectDirty_ViaUndoStack` (adapt the existing dirty assertions; they must now pass through the stack). `MapCanvasControlGestureTests.cs`: `DragRoom_ThenEscape_...` is ticket 006; here only add `DragRoom_Release_IsOneUndoStep`.
- Grep guard test in Core.Tests is NOT needed; the ticket's done-when has the grep.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- `grep -rn "IsDirty = " src --include=*.cs | grep -v Tests` shows only `EditorSession`/`Project` internals.
- Matrix rows X052, X014, X015 updated with test evidence; commit `parity(004): Route existing mutations through EditorSession`.

## Out of scope
- Undo/Redo commands and UI (008), moving drag logic to Core (006), new edit commands (feature tickets), clipboard abstraction, settings (non-map) undo.
