# 103 — Honour "Load last project on startup"

Matrix rows: A014          Priority: P?          Size: S
Depends on: 100, 101, 104          Suggested tier: mid

## What
When the setting is on (or `-a/--loadlastproject` is given) and no file is named on the command line, the app opens `LastProjectFileName` at startup if it is a URL or an existing file. Errors are swallowed to the status bar, never block startup. Fixes I-001.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `MainForm_Load` (grep -n `LoadLastProject`) and the helper that loads the last project (~lines 652–664)
- `src/Trizbort.Avalonia/ViewModels/AppSettingsViewModel.cs` — `LoadLastProjectOnStart` (lines 64, 86)

## Implement
1. Avalonia: in the startup routine added by 104 (`StartupActions.RunAsync(options)`), precedence: explicit file name argument, else (`options.LoadLastProject || AppSettings.LoadLastProjectOnStart`) and the file exists. Load via the same path as `OpenMapAsync` without a picker (add `IStorageService.OpenMapFromPathAsync(path)` if 104 has not already).
2. A failed load shows "Could not reopen <path>" in the status bar only.

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/StartupTests.cs` — setting on + existing file loads; setting on + missing file leaves empty map; explicit file argument wins; setting off does nothing.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Matrix row A014 -> Implemented; I-001 gets `Ticket: 103`.
- Committed as `parity(103): load last project on startup`.

## Out of scope
- Restoring zoom/scroll position of the last session.
