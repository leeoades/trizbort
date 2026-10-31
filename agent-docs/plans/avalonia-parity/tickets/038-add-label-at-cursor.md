# 038 — Add Map Label command (L)

Matrix rows: M075, K037          Priority: P3          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
`L` (or Project > Add Map Label) adds a map label at the cursor or viewport centre, selects it and opens its properties.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `AddLabel` (line ~342)
- `$REF/src/Trizbort/UI/MainForm.cs` — constructor near line 49 (menu item built in code), `OnKeyDown` `case Keys.L`

## Implement
1. Core: `label.add` (`L`, Canvas) via `EditorSession.AddLabel(Point?)`, then `prompts.EditElement`. Centre on the point like rooms (037).
2. Avalonia: remove `CommandService.AddMapLabel` placement code.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/AddLabelTests.cs` — position, selection, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(038): Add Map Label command (L)`.

## Out of scope
- Label dialog.
