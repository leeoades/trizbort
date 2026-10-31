# 030 — Add new-connection defaults and the Plain/Dotted/One Way commands

Matrix rows: M047, M048, M049, M073, M074, K005, K041, K042, C004          Priority: P1          Size: M
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
`P` Plain, `T` toggles Dotted, `A` toggles One Way Arrow. Each applies to every selected connection AND becomes the default for newly drawn connections; toolbar toggle buttons for T and A show a checked state; context menu has a Line Styles submenu on connections.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `NewConnectionFlow`/`NewConnectionStyle`/`NewConnectionLabel` properties + `Raise…Changed` (grep -n 'NewConnection'), `ApplyNewPlainConnectionSettings` (line 436), `ClearMidText`, `SetDefaultConnectionColor`
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `ToggleConnectionFlow`, `ToggleConnectionStyle`, `SetConnectionFlow`, `SetConnectionStyle`
- `$REF/src/Trizbort/UI/MainForm.cs` — `PlainLinesMenuItem_Click` (721), dotted/one-way handlers ~1167-1175, check state ~1192-1197
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.P/T/A`

## Implement
1. Core: add `NewConnectionDefaults` (flow, style, label) to `EditorSession` with change event. Commands `connection.plain` (`P`), `connection.toggleDotted` (`T`), `connection.toggleOneWay` (`A`) in `ConnectionStyleCommands`. Plain resets style Solid, flow TwoWay, label None, clears MidText and resets colour to Transparent for selection and defaults (order matters per comment in `ApplyNewPlainConnectionSettings`).
2. IsChecked from defaults or selection; toolbar items come free from 010 (mark the two toggles `ToolbarGroup`).
3. Bare letters canvas-only, exact modifiers.
4. Avalonia: remove hard-coded `Solid`/`TwoWay`; ticket 034 consumes the defaults.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ConnectionStyleCommandTests.cs` — toggle on/off, plain resets everything, defaults updated, undo restores selection state (defaults are not undoable).

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(030): Add new-connection defaults and the Plain/Dotted/One Way commands`.

## Out of scope
- Using defaults for drawn connections (034).
