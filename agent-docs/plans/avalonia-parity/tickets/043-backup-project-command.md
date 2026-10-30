# 043 — Add Backup Project command

Matrix rows: K016          Priority: P3          Size: S
Depends on: 002, 009, 010        Suggested tier: cheap

## What
`Primary+B` writes a backup copy of the project and reports it.

## Reference (read only these)
- `legacy/src/Trizbort/Domain/Application/Project.cs` — `Backup` (line 102)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.B`

## Implement
1. Core: `file.backup` (`Primary+B`, Window; menu `File/Backup`) calls `Project.Backup()` (already in Core) and reports via `IEditorPrompts.ShowMessage`. CanExecute: project has a file name.
2. Not an undo step.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/BackupCommandTests.cs` — temp dir backup created, CanExecute false without file name.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(043): Add Backup Project command`.

## Out of scope
- Auto-backup scheduling.
