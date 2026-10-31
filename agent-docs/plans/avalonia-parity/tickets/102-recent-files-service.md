# 102 — Recent files service (storage, pruning, activation)

Matrix rows: A015, M012 (storage half)          Priority: P?          Size: S
Depends on: 100, 101                            Suggested tier: mid

## What
An app-level service that owns the most-recently-used list (max 4, dedupe, newest first, persisted) and exposes display labels, pruning of missing files, and an "open recent" entry point. The menu item itself is a catalogue entry in range 020–069 that calls this service.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/AppSettings/ApplicationSettingsController.cs` — `OpenProject`, `RecentProjectsMaxCount`
- `$REF/src/Trizbort/UI/MainForm.cs` — `FileRecentProject_Click`, and the method that rebuilds the recent menu (grep -n `RecentProjects`)

## Implement
1. Core: `RecentFiles` class in `src/Trizbort.Core/Domain/AppSettings/RecentFiles.cs` wrapping `AppSettings.RecentProjects`: `IReadOnlyList<RecentFileEntry> GetEntries(Func<string,bool> exists)` returning `(Path, Label)` where Label is `"&{n} {path}"` style number plus path (middle-ellipsised when > 60 chars); URLs (`StringExtensions.IsUrl`) are kept without an existence check; `Remove(path)`; `Clear()`.
2. Make `OpenProject` order/dedupe case-insensitive on Windows and macOS (ordinal on Linux).
3. Avalonia: `IRecentFilesService` registered in `App.ConfigureServices`, `OpenRecentAsync(path)` = unsaved-changes prompt (`CanLoseProjectAsync`) then load; on missing file show a message and remove the entry.
4. Catalogue wiring (`file.recent`, dynamic submenu) is done in range 020–069; this ticket only provides data and the open call.

## Tests
- Core: `src/Trizbort.Core.Tests/RecentFilesTests.cs` — cap 4, dedupe, move-to-top, missing files hidden, URL kept, label truncation.
- Avalonia: `RecentFilesServiceTests.cs` — missing file removed and message shown via fake `IDialogService`.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Matrix rows A015 (and the storage part of M012) updated.
- Committed as `parity(102): recent files service`.

## Out of scope
- The menu item and its rendering. Clear-recent command (new; not in reference).
