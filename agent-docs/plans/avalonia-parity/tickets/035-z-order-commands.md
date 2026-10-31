# 035 — Add Bring to Front and Send to Back commands

Matrix rows: C002, C003          Priority: P3          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
Room and label context menus get Send to Back and Bring to Front, which set the selected elements' `ZOrder` to min-1 / max+1. Hidden for connections and empty canvas.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `BringToFront`, `SendToBack`
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `GetHighestZOrderIndex`/`GetLowestZOrderIndex` (~658-670), `BringToFrontToolStripMenuItemClick` (1817), `SendToBackToolStripMenuItemClick` (3078)

## Implement
1. Core: `element.bringToFront`, `element.sendToBack` in `ZOrderCommands`, ContextTargets Room|Label, menu `Edit/Arrange/...`, no gesture. Hit-testing (005) must already honour `ZOrder`; add a test that front/back changes which element is hit.
2. CanExecute: >= 1 selected room or label.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ZOrderCommandTests.cs` — order values, hit-test result changes, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(035): Add Bring to Front and Send to Back commands`.

## Out of scope
- Rendering order (UI reads `ZOrder` already).
