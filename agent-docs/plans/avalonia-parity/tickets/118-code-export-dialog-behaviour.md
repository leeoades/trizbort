# 118 — Code export pickers: default name, extension, folder memory

Matrix rows: E005, E006, E007, E008, E010          Priority: P?          Size: M
Depends on: 100, 106, 011          Suggested tier: mid

## What
Each language's export remembers its last file and folder (`LastExport<Lang>FileName`), starts the picker there, preselects the filter of the last chosen extension (matters for Inform 7: `.inform` / `.ni` / `.txt`), suggests a default file name from the project plus the language's default extension, asks before overwriting,.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ExportCode<T>`-style helper used by `FileExportAlanMenuItem_Click` ... (grep -n `LastExport`), lines ~310–345 for filter/initial directory logic, and the enable logic for export items (lines ~990–1008)
- `src/Trizbort.Avalonia/Services/StorageService.cs` — `ExportCodeAsync` (line 156)
- `src/Trizbort.Core/Domain/AppSettings/ApplicationSettings.cs` lines 30–39 (`LastExport*` properties); `ApplicationSettingsController.cs` lines 89–95 (loader skips Alan/Adventuron/QuestRooms: add them to load and save)

## Implement
1. Core: `ExportPathMemory` in `src/Trizbort.Core/Export/ExportPathMemory.cs`: `Get(ExportLanguage)` returns `(folder, fileName, extension)`; `Remember(ExportLanguage, path)`. Uses 106's `ExportLanguage`. Fix the settings loader/saver so all nine `LastExport*` values round-trip.
2. Core: `ExportNaming.SuggestedFileName(Project, ExportLanguage)` = remembered name else sanitised project title/file stem + default extension (106).
3. Avalonia: `StorageService.ExportCodeAsync` sets `SuggestedStartLocation`, `SuggestedFileName`, `DefaultExtension`, and `SuggestedFileType` from the memory. Always ensure the chosen path has an extension (append the filter's if none).
4. Overwrite: after picking, if the file exists and the platform picker did not already confirm (cannot detect reliably — so always do an app-level `File.Exists` prompt via `IEditorPrompts.ConfirmAsync`, same pattern as `SaveMapAsync` lines 121–128) — except `.inform` bundles where the picker may pre-create an empty placeholder: delete a 0-byte placeholder before creating the directory (OQ-E3).

## Tests
- Core: `ExportPathMemoryTests.cs`, `ExportNamingTests.cs`, settings round-trip incl. Alan/Adventuron/QuestRooms.
- Avalonia: fake storage — overwrite prompt declined leaves file untouched; extension appended.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Rows updated (note E006/E010 manual check on Windows needed); committed as `parity(118): export dialog behaviour`.

## Out of scope
- Clipboard exports (057), empty-map enable rule and gestures (056), image/PDF pickers (120, 123).
