# 059 — Add Smart Save command

Matrix rows: M006          Priority: P3          Size: M
Depends on: 002, 009, 010, 053        Suggested tier: mid

## What
`Primary+Alt+S` silently saves the map and then exports PDF and/or image according to the SaveToPDF/SaveToImage app settings; if both are off it explains and does nothing.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `SmartSave(bool silent)` (1047), `SmartSaveToolStripMenuItemClick` (1132)
- `$REF/src/Trizbort/UI/MainForm.Designer.cs` — Smart Save item 276-277

## Implement
1. Core: `file.smartSave` in `FileCommands`: save map if URL/unnamed/dirty (via `IEditorPrompts.PickFile` only when no file name), then call the PDF/image export services. Menu `File/Smart Save`.
2. Depends on PDF and image export tickets from the export owner; until they exist, implement the save half and leave the exports behind an interface `ISmartSaveExporter`.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/SmartSaveTests.cs` — both flags off message, save then export calls order, with fakes.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(059): Add Smart Save command`.

## Out of scope
- Export engines.
