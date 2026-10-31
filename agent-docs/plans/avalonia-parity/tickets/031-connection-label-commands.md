# 031 — Add connection label commands Up/Down/In/Out

Matrix rows: M050, K006, K038, K039, K040, C005          Priority: P2          Size: S
Depends on: 002, 003, 009, 010, 030        Suggested tier: mid

## What
`U`, `D`, `I`, `O` set the label on selected connections (and the new-connection default). Menu items show a check mark for the current label; they sit in the connection context menu's Line Styles submenu.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `SetConnectionLabel`
- `$REF/src/Trizbort/UI/MainForm.cs` — handlers at 250 (`DownLinesMenuItem_Click`), 601 (`InLinesMenuItem_Click`), 716 (`OutLinesMenuItem_Click`), up handler ~1274; check state 1198-1201
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.U/D/I/O` (~1196-1251)

## Implement
1. Core: `connection.label.up|down|in|out` in `ConnectionLabelCommands`; stores the label in `NewConnectionDefaults` (030).
2. Bare `D` must not fire with Primary (Primary+D is Duplicate, ticket 042).
3. CanExecute: >= 1 connection selected.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ConnectionLabelCommandTests.cs` — each label, IsChecked, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(031): Add connection label commands Up/Down/In/Out`.

## Out of scope
- Connection properties dialog.
