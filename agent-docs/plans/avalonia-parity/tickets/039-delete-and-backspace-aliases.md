# 039 — Route Delete through one command and add Backspace alias

Matrix rows: M028, K046, K047          Priority: P2          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
`Delete` removes the selection (a selected curve waypoint first, ticket 064). `Backspace` (canvas only; macOS also `Primary+Backspace`) does the same. Delete room cascades to its connections and undo restores them.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `DeleteSelection` (line ~544)
- `$REF/src/Trizbort/UI/MainForm.cs` — `EditDeleteMenuItem_Click` (260)

## Implement
1. Core: `edit.delete` gestures `Delete` (Window) and `Backspace` (Canvas, never in a text box). Remove the duplicate window-level Delete binding vs canvas handling (matrix K open question 1) — the catalogue emits one binding.
2. Execute via `session.Execute`; rely on `Project.OnElementRemoved` cascade and snapshot undo (D1).

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/DeleteCommandTests.cs` — delete room with connections, undo restores both, Backspace gesture present in catalogue exactly once.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(039): Route Delete through one command and add Backspace alias`.

## Out of scope
- Cut (040).
