# 101 — Save settings on exit, project open and project save

Matrix rows: A013, A017          Priority: P?          Size: S
Depends on: 100                  Suggested tier: mid

## What
Settings are saved when the window closes and whenever a map is opened or saved, so `LastProjectFileName`, `RecentProjects`, `ShowMiniMap` and window geometry (117) survive restarts. Open/Save As pickers start in the folder of the last project.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `OnClosing` (settings save), `MainForm_Load`, `FileRecentProject_Click`; grep -n `OpenProject(`
- `$REF/src/Trizbort/Domain/AppSettings/ApplicationSettingsController.cs` — `OpenProject`, `SaveSettings`
- `src/Trizbort.Avalonia/Services/StorageService.cs` — `OpenMapAsync`, `SaveMapAsync` (no start location set)

## Implement
1. Avalonia: in `StorageService.OpenMapAsync` and `SaveMapAsync`, after success call `ApplicationSettingsController.OpenProject(fileName)` (this updates last project and recent list; recent list UI is in 102 and the menu item in range 020–069).
2. Avalonia: `MainWindow.OnWindowClosing`, on the confirmed-close path, call `ApplicationSettingsController.SaveSettings()` inside try/catch.
3. Set `FilePickerOpenOptions.SuggestedStartLocation` / `FilePickerSaveOptions.SuggestedStartLocation` from the folder of `Project.Current.FileName`, else of `LastProjectFileName`, via `TopLevel.StorageProvider.TryGetFolderFromPathAsync`.
4. Do not save when the settings file could not be loaded at startup and the user has not changed anything (avoid overwriting a `.bad` recovery).

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/SettingsPersistenceTests.cs` — with `AppPaths.OverrideConfigFolder` pointing at a temp dir, open a sample map through a fake storage, assert `LastProjectFileName` and file contents; closing saves.
- Core: none needed (`OpenProject` already exists).

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows A013, A017 updated with evidence.
- Committed as `parity(101): save settings on exit/open/save`.

## Out of scope
- Recent Maps menu (range 020–069, uses 102). Minimap toggle command (M061, range 020–069; it only reads/writes `ShowMiniMap`, persisted here via the same save).
