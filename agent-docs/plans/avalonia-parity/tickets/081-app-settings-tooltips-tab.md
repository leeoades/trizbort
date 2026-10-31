# 081 — App settings: tooltip tab dependencies and the Show Canvas Tooltips master toggle (I-005)

Matrix rows: D006          Priority: P?          Size: S
Depends on: 080           Suggested tier: mid

## What
The Tooltips tab behaves correctly: "Show Canvas Tooltips" truly saves (I-005), "Descriptions" wording covers rooms and connections, and the limit options are enabled only under the right conditions.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/AppSettings.md` rows Show Canvas Tooltips, Show Descriptions, Limit *; checklist "Enable/disable dependencies".
- `$REF/src/Trizbort/UI/AppSettingsDialog.Designer.cs` — grep `Text =` lines 410-500 (tooltip group).
- `src/Trizbort.Avalonia/Views/Dialogs/AppSettingsDialog.axaml:35-60`, `ViewModels/AppSettingsViewModel.cs:84-99` (`ApplyToSettings`), `src/Trizbort.Core/Setup/ApplicationSettings.cs:46-60` (`ShowTooltips` computed).
- `agent-docs/plans/avalonia-parity/issues.md` I-005.

## Implement
1. Decide per I-005: the master is a UI convenience. In the VM keep `ShowTooltips`; on OK, if unchecked write both `ShowDescriptionsInTooltips=false` and `ShowObjectsInTooltips=false`; when checked and both children are false, set both true. Loading: `ShowTooltips = Descriptions || Objects`. Remember the children's previous values in the VM while the dialog is open so toggling master off/on restores them.
2. Dependencies: Show Objects and Show Descriptions `IsEnabled = ShowTooltips`; the two limit checkboxes `IsEnabled = ShowTooltips && ShowDescriptions`; each count box `IsEnabled` as its limit checkbox. (Legacy bug where unchecking a limit disabled itself stays fixed.)
3. Wording: "Show Descriptions in Tooltip" (rooms and connections) with legacy limit labels "Limit Characters of Room Description to [n]" / "...Connection Description to [n]" with the number box inline and a "characters" unit label after it.
4. VM default char count aligned to Core default 50; limits 0–4096 (legacy).
5. Layout: group "Descriptions" box around the show-descriptions options (legacy used the checkbox as group caption); Objects option above.

## Tests
- Avalonia: `DialogViewModelTests.cs` — unchecking master and OK sets both flags false and the app tooltips are off (read `Settings` props); re-open shows master unchecked; re-ticking restores previous children; dependency matrix of `IsEnabled`.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D006: I-005 marked fixed with ticket reference in `issues.md`.
- Committed as `parity(081): app settings tooltips tab`.

## Out of scope
- Changing the tooltip rendering code.
