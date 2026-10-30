# 049 — Add Alt+NumPad unexplored connection

Matrix rows: K052          Priority: P3          Size: S
Depends on: 002, 003, 009, 010, 030        Suggested tier: mid

## What
`Alt+NumPad1-9` adds an unexplored (loopback) connection leaving the selected room in that direction.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `AddUnexploredConnectionToSelectedRoom` (line 1687)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` keypad block, `KeypadNavigationUnexploredModifier` use (~1672-1675)

## Implement
1. Core: `room.addUnexplored.*` (8 directions) in `NavigationCommands`, Canvas scope, gesture `Alt+NumPadN`. CanExecute: exactly one room selected. One undo step.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/UnexploredConnectionTests.cs` — loopback created on correct compass port, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(049): Add Alt+NumPad unexplored connection`.

## Out of scope
- Modifier setting.
