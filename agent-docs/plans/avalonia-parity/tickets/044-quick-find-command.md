# 044 — Add Quick Find command

Matrix rows: K022          Priority: P3          Size: S
Depends on: 002, 009, 010        Suggested tier: cheap

## What
`Primary+F` opens Quick Find. Command wiring only; the dialog is owned by the dialogs tickets.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.F` with Control
- `$REF/src/Trizbort/UI/QuickFind.cs` — `QuickFind` form (read only the constructor/entry points)

## Implement
1. Core: `edit.quickFind` (`Primary+F`, Window; menu `Edit/Find...`) calls `IEditorPrompts.QuickFindAsync(session)`. Selecting a result goes through `Viewport.CenterOn` (020).
2. Avalonia: provide the prompt implementation as a stub that shows the dialog once the dialog ticket exists; until then the command is marked hidden.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/QuickFindCommandTests.cs` — prompt called, gesture registered.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(044): Add Quick Find command`.

## Out of scope
- The Quick Find dialog and search algorithm.
