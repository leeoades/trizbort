# 007 — New-connection gesture uses the "new connection" defaults; redock gesture in Core

Matrix rows: G008, G009, G010, X004 (state only), X041, X011 (connection half); M047–M051 depend on the defaults (foundation F5c; D1 point 6)          Priority: P1          Size: M
Depends on: 006        Suggested tier: mid

## What
Drawing a connection from a port creates it with the editor's current "new connection" style, flow and label (legacy behaviour; Avalonia hard-codes `Solid`/`TwoWay`). Selecting one connection makes it the template for new ones. Moving an existing connection end (redock) and drawing both live in `CanvasInteraction`, with correct docking: hover a fixed port to dock, otherwise snap to the grid.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/feature-matrix.md` — rows G008–G010 and X004, X011, X041 (read those rows only); open question 4 for G008.
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `NewConnectionFlow/Label/Style` properties and `Raise…Changed` (~2811-2828), `SetConnectionDefaultsFrom` / `UpdateSelection` (~3083-3095, 2638-2652), `BeginDragDrawLine`, `BeginDrawConnection`, `DoDragMovePort` (~1701, 1788, 2011).
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — look for `LineStyle.Solid` / `ConnectionFlow.TwoWay` literals near the pending-connection code (~766-800); after ticket 006 this logic lives in `CanvasInteraction`.
- `src/Trizbort.Core/Editor/CanvasInteraction.cs` (ticket 006).
- `src/Trizbort.Core/Domain/Elements/Connection.cs` — `Style`, `Flow`, `StartText/MidText/EndText` (find the label property used for "label" in legacy: grep `NewConnectionLabel` in `$REF`), `MoveablePort`.

## Implement
1. Core `src/Trizbort.Core/Editor/NewElementDefaults.cs`: `public sealed class NewElementDefaults { ConnectionLabel Label; ConnectionStyle Style; ConnectionFlow Flow; event EventHandler? Changed; void CopyFrom(Connection c); }` (property types as found by the grep below; if the label is not an enum, use that type) — use the real enum names found in Core (`grep -rn "enum ConnectionStyle\|enum ConnectionFlow\|enum ConnectionLabel" src/Trizbort.Core`). Default values: Solid, TwoWay, None (the app defaults).
2. `EditorSession.NewDefaults`; when the selection becomes exactly one connection, call `NewDefaults.CopyFrom(thatConnection)` (legacy `SetConnectionDefaultsFrom`); a multi-selection or non-connection leaves them unchanged. Do NOT add commands to set the defaults from the toolbar here (that is the "connection flow/style" feature tickets, X004–X006); only the state and consumption.
3. In `CanvasInteraction` (pending connection creation, after the 32-unit threshold `DragDistanceToInitiateNewConnection`): create the connection with `Style`, `Flow` (and label via the label property) from `session.NewDefaults` instead of literals.
4. Redock clean-up in `CanvasInteraction`: while moving a connection end, hovering a fixed `Port` docks (`DockAt(port)`), otherwise `DockAt(null)` then `SetPosition(Settings.Snap(world))`. Avalonia today un-docks on every move event; keep dock stable while the pointer stays within the same hovered port (only call `DockAt` when the hovered port changes) so the docked end does not flicker or lose its `PortId`. Moving the end of a connection that has `StartText/EndText` must keep them.
5. Release in empty space (G009): connection ends undocked at the snapped position (existing behaviour); zero-length connection discarded (existing `IsDegenerateConnection`).

## Tests
`src/Trizbort.Core.Tests/ConnectionGestureTests.cs` (drive `CanvasInteraction` as in ticket 006 tests):
- `DrawConnection_UsesNewDefaults_StyleFlowLabel` (set defaults Dashed/OneWay, draw, assert on the created connection).
- `DrawConnection_NoSelection_UsesAppDefaults_SolidTwoWay`.
- `SelectingConnection_CopiesItsStyleFlowIntoDefaults`; `SelectingTwoConnections_DoesNotChangeDefaults`; `SelectingRoom_DoesNotChangeDefaults`.
- `Redock_HoverFixedPort_Docks`; `Redock_EmptySpace_UndocksAndSnaps`; `Redock_StayingOverSamePort_DoesNotRedock` (count `DockAt` effects via the port's connection list or a changed-event counter); `Redock_KeepsTextOnEnds`.
- `Redock_IsOneUndoStep_AndCancelRestoresDock` (uses 006 transaction).
- `NewConnection_FromPort_CreatedConnection_IsOneUndoStep`.

## Done when
- Build + Core + Avalonia tests pass on Linux; no `Solid`/`TwoWay` literals remain in gesture code.
- Matrix rows G008–G010, X041 updated; X004 note "state in `EditorSession.NewDefaults`, commands: later"; commit `parity(007): New-connection defaults and redock in Core`.

## Out of scope
- Toolbar/menu commands that change flow/style/label and "plain connection" (X004–X006); new-room defaults (X011 room half, matrix G001/C001 tickets); ghost-waypoint threshold (X040); reference rooms.
