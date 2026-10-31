# 036 — Add the four project validation toggles

Matrix rows: M052, M053, M054, M055          Priority: P3          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
Edit > Validation has four checkable items: rooms must have a unique name, a description, a subtitle, and must not have a dangling connection. Each flips the project flag.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `SetValidation`
- `$REF/src/Trizbort/UI/MainForm.cs` — handlers 756-778, check state ~1233-1236

## Implement
1. Core: `project.validate.uniqueNames|description|subtitle|noDangling` in `ValidationCommands`, `IsChecked` from `Project.MustHaveUniqueNames` etc. Execute is an undoable step (project settings change). Menu `Edit/Validation/...`.
2. If Map Settings dialog already exposes the same flags, both use the same Core property; no duplicate state.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ValidationCommandTests.cs` — toggle, IsChecked, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(036): Add the four project validation toggles`.

## Out of scope
- Validation rendering.
