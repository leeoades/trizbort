# 114 — Window title: dirty marker, version and "show full path" setting

Matrix rows: A004, I-006 (ShowFullPathInTitleBar)          Priority: P?          Size: S
Depends on: 003 (dirty flag from UndoStack)          Suggested tier: mid

## What
Window title honours `ShowFullPathInTitleBar` and shows the unsaved marker, and refreshes whenever the dirty flag or file name changes (including after edits and after undo back to the saved state). Format (Lee to confirm, open question A-7): keep the Avalonia order `Trizbort - <name>*`? This ticket implements the reference order `<name or full path>[*] - Trizbort - <version>` because L4 says port unless an obvious modern convention exists; changing it is a one-line format string in `TitleFormatter`.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `UpdateCommandUI` (line ~1182–1190, title assignment)
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — `WindowTitle` (lines 101–110)

## Implement
1. Core: `TitleFormatter.Format(projectName, fileName, isDirty, showFullPath, appName, version)` in `src/Trizbort.Core/Editor/TitleFormatter.cs`; unsaved project name = "Untitled". Version from the assembly informational version without build metadata.
2. Avalonia: `WindowTitle` calls it and is raised from `EditorSession.Changed(Dirty | Model)` and from settings changes (after App Settings OK).
3. Dirty marker is `*` after the name.

## Tests
- Core: `src/Trizbort.Core.Tests/TitleFormatterTests.cs` — all combinations incl. URL file names.
- Avalonia: title updates after an edit and after save.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- A004 -> Implemented; I-006 notes ticket 114 (wheel is 051, new-room style is 115); committed as `parity(114): window title`.

## Out of scope
- Undo stack itself (003).
