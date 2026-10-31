# 006 — Move the pointer/key drag state machine to Core (CanvasInteraction)

Matrix rows: X037, X038, X039, X040, X041, X042, X043, X044, X046, X051 (part), G001–G007, G009–G012, G015, G016, G019–G021; fixes I-010 (foundation F5b; D1 point 4)          Priority: P1          Size: L (strong-tier review of the diff)
Depends on: 004, 005, 009        Suggested tier: mid (implementation), strong (diff review)

## What
`CanvasInteraction` in Core consumes neutral input events and runs the whole gesture machine (pan, marquee, move, resize, move port, move waypoint, hover, cursor, tooltip target) and outputs an `AdornerState`. The Avalonia control shrinks to "convert events → call session → draw". A drag is one undo transaction; Esc / capture-lost cancels it and puts everything back (fixes I-010 for room drags; Avalonia's existing cancel/restore for connection ends is kept).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D1 points 3 (grouping), 4, 6.
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs`: `enum CanvasDragMode` (top of file), `OnPointerPressed` (~698-848), `OnPointerMoved` (~850-1010), `OnPointerReleased` (~1011-1047), `OnPointerCaptureLost` (~1048), `IsDegenerateConnection` (~1062), `EndDrag(modifiers, cancelled)` (~1083-1202), `OnPointerWheelChanged` (~1204), `OnKeyDown/OnKeyUp` Esc & Space handling (~1216-1316), `DetermineCursorType` (~638), `UpdateToolTip/BuildToolTipText` (~660-696), the `_drag*`/`_hover*`/`_pending*`/`_gesture*` fields. Read by method, not whole file.
- `src/Trizbort.Core/Domain/Misc/MapEditing.cs` — `Move`, `Resize`, `MoveAttachedWaypoints`.
- `src/Trizbort.Core/Domain/Enums/ModifierKeys.cs` (`None/Shift/Control/Alt`; reuse).
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnMouseDown`, `BeginDragMove` (click-selection rules ~1740-1775), `EndDrag` (marquee Shift/Ctrl) — only to settle X043/X044 semantics.
- `src/Trizbort.Avalonia.Tests/MapCanvasControlGestureTests.cs` — behaviours to preserve.

## Implement
In `src/Trizbort.Core/Editor/` (namespace `Trizbort.Editor`):
1. Neutral input: `enum PointerButton { None, Left, Middle, Right }`; `readonly record struct PointerInput(PointerButton Button, ModifierKeys Modifiers, Vector Client, int ClickCount)`; `readonly record struct WheelInput(float DeltaY, ModifierKeys Modifiers, Vector Client)`; `KeyInput`/`InputKey` come from ticket 009 (`src/Trizbort.Core/Editor/Input/`) — do not redefine them.
2. `AdornerState` (immutable snapshot, rebuilt by `CanvasInteraction`): `Element? HoverElement`, `Port? HoverPort`, `ResizeHandle? HoverHandle`, `CurveWaypoint? HoverWaypoint`, `IReadOnlyList<ResizeHandle> Handles`, `IReadOnlyList<Port> Ports`, `Rect? Marquee` (world), `PendingLine? PendingConnection` (from-port + current end, null if none), `CursorKind Cursor`, `Element? TooltipTarget`. `enum CursorKind { Default, Hand, SizeAll, Pan, Cross, ResizeNWSE, ResizeNESW, ResizeNS, ResizeEW, DrawLine, NoMove }` (copy the mapping from `DetermineCursorType`, naming each Avalonia `StandardCursorType` it returns).
3. `CanvasInteraction.cs` — `public sealed class CanvasInteraction { CanvasInteraction(EditorSession, HitTester); AdornerState Adorners; CanvasDragMode Mode; bool OnPointerPressed(PointerInput); void OnPointerMoved(PointerInput); void OnPointerReleased(PointerInput); void Cancel(); /* Esc, capture lost */ void OnWheel(WheelInput); bool OnKey(KeyInput); void OnKeyUp(KeyInput); void SetSpaceHeld(bool); event Action<Element>? EditRequested; event Action<Vector>? ContextMenuRequested; }`.
   Move `CanvasDragMode` to Core (public enum, same values). Port, line by line:
   - Press dispatch with the order from `HitTester.HitAll` (ticket 005): pan (middle, right-drag, Space+left), waypoint, handle, port, element, empty → marquee. Click selection rules per legacy X044: plain click selects only the hit element; **Ctrl toggles, Shift adds**; clicking empty space clears unless a modifier is held (D1 point 6; this fixes Avalonia where both toggle — X043). Double-click: `EditRequested` for the hit element (G013); double-click on empty = no-op (G014 is a later ticket).
   - Right button: right-drag pans; right release with ≤ 2 px total movement raises `ContextMenuRequested(world)` (existing behaviour).
   - Snap with `Settings.Snap`; moves via `MapEditing.Move`; resize via `MapEditing.Resize`; waypoint drag via `Connection.SetCurveWaypoint`; port move via `MoveablePort.DockAt/SetPosition`. Keep today's `DragDistanceToInitiateNewConnection` behaviour for new connections as is (ticket 007 changes creation).
   - Transactions: on press that begins MoveElement/MoveResizeHandle/MovePort/MoveWaypoint call `session.BeginTransaction("Move"/"Resize"/"Move connection end"/"Move waypoint")` and keep the handle; `OnPointerReleased` (not cancelled) disposes it (records one step, or none if nothing moved); `Cancel()` calls `transaction.Cancel()`, which restores the whole map from the snapshot, so the hand-written restore code in `EndDrag` (original port/position, delete connection created by drag) is replaced by the snapshot restore. After cancel/undo restore, reselect by id happens in `SnapshotMemento`; `CanvasInteraction` must clear all hover/drag references on `EditorChange.Model`. This replaces the temporary transaction code added to the control in ticket 004.
   - `IsDegenerateConnection` discard logic moves unchanged (inside the transaction).
   - Arrow-key/Escape/Space handling from `OnKeyDown` goes to `OnKey`/`OnKeyUp` (arrows → `session.NudgeSelected`/`ResizeSelected` from 004; Esc → `Cancel()` if dragging else clear selection, as today).
   - `OnWheel`: `session.Viewport.ZoomWheel(…, invertWheel: Settings.InvertMouseWheel)` — fixes the ignored invert setting (X047 part).
   - Raise `session.Invalidate(EditorChange.Adorners)` whenever `Adorners` changes.
4. Avalonia `MapCanvasControl`: add `private CanvasInteraction? _interaction` (created when `CommandService.Session` is set); `OnPointer*`/`OnKey*`/`OnPointerCaptureLost`/`OnPointerWheelChanged` become: build neutral input (`e.GetCurrentPoint`, map `KeyModifiers`→`ModifierKeys`, `Key`→`InputKey` in a new `Controls/InputMapper.cs`), call `_interaction`, then `e.Handled = …`, capture/release the pointer based on `_interaction.Mode`, set `Cursor = InputMapper.ToCursor(adorners.Cursor)`, update tooltip from `TooltipTarget`. The render code reads `_interaction.Adorners` instead of the `_hover*`/`_drag*` fields. Delete the moved fields and methods.

## Split guidance
If this exceeds a day, split inside this ticket into two commits: 006a = input/adorner/cursor types + Pan, Marquee, MoveElement, MoveResizeHandle, wheel, keys, selection rules; 006b = MovePort, MoveWaypoint, pending connection, cancel via transaction. Both commits must keep the app working.

## Tests
`src/Trizbort.Core.Tests/CanvasInteractionTests.cs` (no Avalonia; build a project with 2–3 rooms; drive with `PointerInput`s; a client point is converted with `session.Viewport`):
- `ClickRoom_SelectsOnlyThatRoom`; `CtrlClick_TogglesSelection`; `ShiftClick_AddsToSelection`; `ClickEmpty_ClearsSelection`; `ClickEmpty_WithShift_KeepsSelection`.
- `Marquee_SelectsIntersectingElements`; `Marquee_ShiftAdds`.
- `DragRoom_MovesSnappedByGrid`; `DragRoom_ReleaseRecordsOneUndoStep`; `DragRoom_Cancel_RestoresPosition_NoUndoStep` (I-010); `DragRoom_MovesAllSelected`.
- `DragHandle_Resizes_AndCancelRestores`.
- `DragPort_MovesEnd_CancelRestoresDock`; `DragFromPort_BelowThreshold_CreatesNothing`; `DragNewConnection_Cancel_RemovesIt`; `DragNewConnection_DegenerateOnRelease_Discarded`.
- `RightDrag_Pans`; `RightClickWithin2px_RaisesContextMenuRequested`; `MiddleDrag_Pans`; `SpaceLeftDrag_Pans`.
- `Hover_SetsCursorAndAdorners` (Port → DrawLine, Handle → matching resize cursor, Pan → Pan).
- `Wheel_Zooms_AboutCursor_AndHonoursInvertSetting`.
- `ModelReplaced_DuringDrag_ClearsHoverAndDragState` (undo during a drag).
- `Escape_Idle_ClearsSelection`; `DoubleClickRoom_RaisesEditRequested`.
Existing `MapCanvasControlGestureTests.cs` stay green; trim only tests that become redundant and list them in the commit message.

## Done when
- Build + Core + Avalonia tests pass on Linux; `MapCanvasControl.cs` shrinks by roughly 600+ lines and holds no `_drag*` state.
- I-010 marked fixed in `issues.md` (Ticket column: 006) and G020 updated; matrix rows above updated with test names.
- Commit `parity(006): Move canvas drag state machine to Core`; strong-tier review of the diff.

## Out of scope
- New-connection defaults and the redock gesture (007); ghost-waypoint drag threshold (X040) and Ctrl+click reference jump (G003) — separate feature tickets; context-menu building (010); tooltips content; minimap; spatial index/smart segments (render concerns, stay UI-side).
