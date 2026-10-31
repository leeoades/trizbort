# 110 — Smart Save service: file naming, settings, messages, PDF/image output

Matrix rows: E030, E031, E032, E033, E034          Priority: P?          Size: M
Depends on: 059, 112 (watcher suspend), 120, 123 (PDF; image-only until then)          Suggested tier: mid

## What
Smart Save saves the project, then writes `<folder>/<name>.pdf` and/or `<folder>/<name>.<image ext>` according to the `SaveToPDF` / `SaveToImage` settings, overwriting silently. If the project is unsaved/dirty/URL-based it first asks "Your project needs to be saved before we can do a SmartSave. Would you like to save the project now?". If both settings are off: "Your settings are set to not save anything...". Summary message lists files written, per-file errors, or "No files have been saved during the SmartSave." The catalogue command and gesture (`Primary+Alt+S`) belong to ticket 059; this ticket supplies the service 059 calls (`ISmartSaveExporter`).

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `SmartSave(bool silent)` (line 1047), `SmartSaveToolStripMenuItemClick`, `ExportImage`, and the PDF save helper called from `SmartSave`
- `$REF/src/Trizbort/UI/AppSettingsDialog.cs` — `SaveToPDF`, `SaveToImage` properties
- `src/Trizbort.Core/Domain/AppSettings/ApplicationSettings.cs:47-48` (settings exist)

## Implement
1. Core: `SmartSaveService` in `src/Trizbort.Core/Export/SmartSaveService.cs`: `SmartSavePlan Plan(Project, AppSettings)` (target paths, or reason not possible) and `Task<SmartSaveResult> RunAsync(plan, IMapImageWriter image, IMapPdfWriter pdf)` returning per-file result; no UI. Abstractions `IMapImageWriter`/`IMapPdfWriter` are implemented in Avalonia by 120/123; if PDF is not yet available, the PDF writer reports "not supported" without failing the image output.
2. Implement 059's `ISmartSaveExporter` with `SmartSaveService`; add a `silent` mode (no prompts or message boxes) for the CLI (107). Do not register commands here.
3. Avalonia: dialog wiring through `IEditorPrompts` (011): confirm-save prompt, results message. Add the two checkboxes `Save PDF on Smart Save` / `Save image on Smart Save` to App Settings dialog if absent (E031; grep `SaveToPDF` in `AppSettingsViewModel`).
4. Suspend the file watcher around the map save (112) as the normal Save does.

## Tests
- Core: `src/Trizbort.Core.Tests/SmartSaveServiceTests.cs` — plan paths, both-off message, dirty flow, partial failure with fake writers.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Matrix rows updated; committed as `parity(110): smart save`.

## Out of scope
- CLI invocation (107). PDF/image writers themselves (120, 123).
