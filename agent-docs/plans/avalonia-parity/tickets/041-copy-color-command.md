# 041 — Add Copy Color command

Matrix rows: M025          Priority: P3          Size: S
Depends on: 002, 009, 010, 040        Suggested tier: cheap

## What
`Primary+Alt+C` copies the colours of the single selected room so a later Paste applies them (Avalonia already has the paste half).

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `CopySelectedColor` (line 532)
- `src/Trizbort.Core/Domain/Controllers/CopyController.cs` — `CopyColors`, `SetRoomColors`

## Implement
1. Core: `edit.copyColor` (`Primary+Alt+C`) via `CopyController.CopyColors` through `IClipboard`. CanExecute: exactly one room selected. Not an undo step.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/CopyColorTests.cs` — copy then paste applies colours.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(041): Add Copy Color command`.

## Out of scope
- Pasting colours to several rooms.
