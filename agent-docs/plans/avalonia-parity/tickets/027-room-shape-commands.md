# 027 — Add the four Room Shape commands

Matrix rows: M038, M039, M040, M041, K009, K035, K036, C012          Priority: P2          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
Square Corners `Primary+H`, Ellipse `Primary+E`, Rounded Edges `Primary+R`, Octagonal Edges `Primary+8` apply the shape to all selected rooms; context menu has a Room Shape submenu with the current shape ticked.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/RoomController.cs` — `SetRoomShape`
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `SetRoomShape`
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` cases `Keys.H`, `Keys.E`, `Keys.R` with Control (~1161-1177) — NOTE the Ctrl+E case wrongly sets SquareCorners
- `$REF/src/Trizbort/UI/MainForm.Designer.cs` — menu accelerators at 818-845

## Implement
1. Core: `room.shape.square|ellipse|rounded|octagonal` in `RoomShapeCommands`. **Do not copy the reference Ctrl+E bug (K035):** Ctrl+E must set Ellipse, per the menu label and shortcut-map. macOS: Square Corners is `Ctrl+H`.
2. Bare `R` (add room, 037) must not fire when Primary is held; `Primary+R` is Rounded Edges.
3. CanExecute: >= 1 selected room. IsChecked: primary room's shape.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/RoomShapeCommandTests.cs` — each shape, Ctrl+E regression test asserts Ellipse, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(027): Add the four Room Shape commands`.

## Out of scope
- Shape rendering changes.
