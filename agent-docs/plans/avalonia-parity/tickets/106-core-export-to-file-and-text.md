# 106 — Core export helpers: export to file and to string for every language

Matrix rows: E013, E014, E015 (Core half of each)          Priority: P?          Size: S
Depends on: none                                               Suggested tier: mid

## What
All code exporters can be driven from Core with one call each for "write to path" and "produce text" (what clipboard exports and CLI exports need). Also ensures the Quest "rooms only" variant and the Quest with-header variant are both reachable.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ExportCodeCl<T>`, and `AlanToTextToolStripMenuItemClick` and sibling `*ToText*Click` handlers (grep -n `ToText`)
- `src/Trizbort.Core/Export/CodeExporter.cs` — `Export()` (string overload, line ~63), `Export(string fileName)`
- `src/Trizbort.Core/Export/Languages/QuestExporter.cs` (class `QuestRoomsExporter`, lines 13–20), `Inform7Exporter.cs` lines 27–37 (`.inform` bundle path logic)

## Implement
1. Core: `CodeExportService` in `src/Trizbort.Core/Export/CodeExportService.cs`: `enum ExportLanguage { Alan, Adventuron, Hugo, Inform6, Inform7, Tads, Quest, QuestRooms, Zil }`; `CodeExporter Create(ExportLanguage)`; `string ExportToText(ExportLanguage, Project)`; `void ExportToFile(ExportLanguage, Project, string path)`. Table-driven (dictionary of factories) so 119/120/105 do not repeat switch statements.
2. Confirm the string overload for each exporter does not touch the file system or UI (grep `MessageBox`, `Application.`); fix if it does.
3. Expose a stable display name and default extension per language (`DefaultExtension`, `DisplayName`) used by 118.

## Tests
- Core: `src/Trizbort.Core.Tests/CodeExportServiceTests.cs` — for each language, text of a sample map equals the file output (ASCII-decoded); `.inform` path creates `Source/Story.ni`.

## Done when
- Build + Core tests pass; matrix rows updated; committed as `parity(106): core export service`.

## Out of scope
- Menu items and pickers (118). Clipboard commands are ticket 057 and the file-export gestures/enable rule are 056; they should call `CodeExportService` from this ticket (057 may be implemented before or after; whichever lands second adapts).
