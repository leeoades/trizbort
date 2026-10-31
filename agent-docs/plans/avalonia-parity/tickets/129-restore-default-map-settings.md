# 129 — Restore Default Map Settings

Matrix rows: M067          Priority: P2          Size: S
Depends on: 003          Suggested tier: mid

## What
The reference has Tools > Restore Default Map Settings: after a Yes/No warning it resets the project's map settings to the defaults. Avalonia has no equivalent. Found by the integrator's matrix review (no earlier ticket listed M067).

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ProjectResetToDefaultSettingsMenuItem_Click` (~line 726): message "Restore default settings?\n\nThis will revert any changes to settings in this project.", Yes/No, warning icon; Yes calls `Settings.Reset()`.
- `src/Trizbort.Core/Setup/Settings.cs` — `Reset()`.

## Implement
1. Core: `EditorSession.RestoreDefaultMapSettingsAsync()` asks via `IEditorPrompts.ConfirmAsync` (reference wording) and, on Yes, runs `Settings.Reset()` inside `session.Execute("Restore default map settings", ...)` so it is one undo step (map settings are part of the snapshot).
2. Catalogue: `project.restoreDefaultSettings` in the Project menu next to Map Settings (no gesture), in the Misc partial (`CommandCatalogue.Misc.cs`).

## Tests
- Core: confirm No → nothing changes, no undo step; Yes → defaults restored (e.g. canvas colour, a font) and one undo step that brings the old values back.

## Done when
- Build + Core + Avalonia tests pass; matrix M067 updated; committed as `parity(129): restore default map settings`.
