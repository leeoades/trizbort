# 133 Canvas fixes from the manual checklist (I-027..I-031)
Source: user's Linux run of `Docs/manual-test-checklist.md`, 2026-10-31.
- I-027 One Esc cancels an in-progress room drag or connection-end drag (restores, no undo step). Find why the first press is lost (focus/capture/tooltip/handled flag) and fix the cause.
- I-028 A selected connection keeps its dash style (dotted stays dotted in the highlight). Check legacy `Connection.Draw` for how the highlight is drawn.
- I-029 Ctrl+arrow / keypad add-or-connect must create connections with no Up/Down text unless the direction is up/down (compare legacy `Canvas` ~1600-1660, `AddConnection(room, compassPoint, two, opposite)`).
- I-030 After opening a map (and on start with a reopened map) the view zooms to fit, like legacy `Canvas.OnProjectChanged` → `ZoomToFit()`.
- I-031 Context menus opened from the canvas show the normal arrow cursor.
Acceptance: unit/headless tests for each where practical; all tests green.
