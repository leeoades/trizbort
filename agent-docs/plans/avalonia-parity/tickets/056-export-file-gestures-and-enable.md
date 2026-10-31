# 056 — Add export-to-file gestures and enable-when-rooms rule

Matrix rows: M009          Priority: P3          Size: S
Depends on: 009, 010        Suggested tier: cheap

## What
The eight source exports get their shortcuts and are disabled when the map has no rooms. Gestures: Alan `Primary+Shift+L`, Hugo `Primary+Shift+H`, Inform 7 `Primary+Shift+7`, Inform 6 `Primary+Shift+6`, TADS `Primary+Shift+T`, ZIL `Primary+Shift+I` (not Z: Redo), Quest `Primary+Shift+Q`, Adventuron none.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — enable logic ~994-1008; `FileExportAlanMenuItem_Click` etc. (409-502)
- `$REF/src/Trizbort/UI/MainForm.Designer.cs` — accelerators at 337-398

## Implement
1. Core: only catalogue data for existing export command ids (`export.alan` ...): set gestures, `CanExecute = Project has rooms`. macOS variants Hugo/Quest per shortcut-map section 4.
2. Export logic itself is covered by the export tickets.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ExportGestureTests.cs` — gestures and CanExecute with empty/non-empty map.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(056): Add export-to-file gestures and enable-when-rooms rule`.

## Out of scope
- Export engines.
