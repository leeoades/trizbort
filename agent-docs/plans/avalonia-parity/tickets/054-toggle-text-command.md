# 054 — Add Toggle Text command

Matrix rows: M014          Priority: P3          Size: S
Depends on: 002, 009, 010        Suggested tier: cheap

## What
`Primary+F4` toggles text rendering (`DebugDisableTextRendering`) and redraws.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ToggleText` (line 946)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ToggleTextToolStripMenuItem` handler (~1177)

## Implement
1. Core: `view.toggleText` (`Primary+F4`, Window; menu `View/Toggle Text`), IsChecked from the setting; Avalonia drawing already reads the setting or add the read in the renderer.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ToggleTextTests.cs` — flips the flag.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(054): Add Toggle Text command`.

## Out of scope
- Other debug toggles (055).
