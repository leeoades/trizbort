# 079 — Map settings: region validation, rename/delete update rooms, swatch preview; OK as one undo step

Matrix rows: D004, D005          Priority: P?          Size: M
Depends on: 077, 002, 003           Suggested tier: mid

## What
The Regions tab validates names (not empty, no duplicate, `"` replaced by `'`, `_` and `:` rejected), keeps rooms in sync when a region is renamed or deleted (rooms of a deleted region move to the default region), disables Delete for the default region, and shows a swatch with sample "123" text per region. The Map Settings OK (all tabs) applies as one undo step.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/MapSettings.md` rows Region *, "RegionSettings (D005)", checklist "Rename/delete side effects", "Validation", "OK/Cancel/Apply".
- `$REF/src/Trizbort/UI/RegionSettings.cs` (43-80), `$REF/src/Trizbort/UI/SettingsDialog.cs` (230-233, 324-391, 463-474, 501-547).
- `src/Trizbort.Avalonia/Views/Dialogs/MapSettingsDialog.axaml:145-177`, `ViewModels/MapSettingsViewModel.cs` (276-321).
- `agent-docs/plans/avalonia-parity/issues.md` — I-007 (stale region names).

## Implement
1. Core: `RegionNaming.Validate(name, existing, current)` / `Sanitize(name)` pure helper in `src/Trizbort.Core/Setup/` (legacy rules); tests.
2. Rename/delete already keep rooms in step (I-007, fixed in commit `ebe0dfc`: `RegionItemViewModel.OriginalName` + `MapSettingsViewModel.UpdateRoomRegions`, tests in `DialogViewModelTests`). Keep that logic; just move it inside the session transaction so undo restores settings and rooms together.
3. Add Region default names `Region1`, `Region2`... and defaults per legacy (White fill, Subtitle colour text) — legacy behaviour; note change from `New Region`.
4. Delete button `IsEnabled` false for the default region; name box read-only for default. Inline Name error text via 070 `error` class; disable OK while any region name is invalid? use the 070 rule (focus offending field).
5. List item template: swatch rectangle in region fill with "123" in region text colour + name; the details panel gets a live sample line "Sample" in text colour on fill colour.
6. OK: `session.Execute("Edit map settings", ...)` wraps settings plus room region updates in one step; Cancel unchanged. (Theme-wide settings that are not map content still go through the session so undo works as decided.)

## Tests
- Core: `RegionNamingTests.cs`; `EditorSessionDialogTests.cs` — rename updates rooms, undo restores both settings and rooms; delete reassigns rooms.
- Avalonia: `DialogViewModelTests.cs` — validation messages; Delete disabled for default region.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D004/D005 updated.
- Committed as `parity(079): map settings regions`.

## Out of scope
- Moving to a separate Region modal (inline editing kept); the room-dialog region colour swatches (072).
