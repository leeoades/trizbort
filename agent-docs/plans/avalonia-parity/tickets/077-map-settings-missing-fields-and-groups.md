# 077 — Map settings: add missing fields and restore group headings, hints and tab layout

Matrix rows: D004          Priority: P?          Size: M
Depends on: 070           Suggested tier: mid

## What
Map Settings gains Resize/Drag Handle Size, Snap to Element Distance, and Document-Specific Margins (checkbox + Horizontal/Vertical enabled only when ticked), and every tab is organised in headed groups with the legacy hint texts, so lines/grid/rooms/editing settings are no longer scattered.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/MapSettings.md` §1 field table, §2 checklist, §3 Recommendation.
- `$REF/src/Trizbort/UI/SettingsDialog.Designer.cs` — grep `Text =` and `GroupBox` only (labels, hints "These details help identify...", "These settings adjust the ease of clicking and dragging.").
- `$REF/src/Trizbort/UI/SettingsDialog.cs:58, 407-410` (margins enabling).
- `src/Trizbort.Avalonia/Views/Dialogs/MapSettingsDialog.axaml`, `ViewModels/MapSettingsViewModel.cs` (`DocumentSpecificMargins` etc. lines ~118-124, 191, 246).
- `src/Trizbort.Core/Setup/Settings.cs:162` (`HandleSize`), `:298` (`SnapToElementSize`).

## Implement
1. VM: add `HandleSize`, `SnapToElementDistance`; load/save to `Settings`. Wire existing `DocumentSpecificMargins`, `DocHorizontalMargin`, `DocVerticalMargin` into controls (margin boxes `IsEnabled={Binding DocumentSpecificMargins}`).
2. Axaml tabs (keep Avalonia tab-per-topic; Map Details first):
   - Map Details: group "Map" with hint "These details help identify your map to others." above Title/Author/Description/History (History label "History:" — legacy; keep "/ Notes" only if Lee prefers: use "History:").
   - Lines & Grid: group "Lines" (Hand-drawn style, Width, Arrow Size, Room Arrow Stalk Length, Text Offset From Line, Preferred Distance Between Rooms), group "Grid" (Visible, Show Origin, Snap To, Size).
   - Rooms & Editing: group "Room" (Default Room Text, Default Shape [friendly names, 071's `EnumDisplay`], Darkness Stripe Size, Object List Offset), group "Advanced" with hint (Handle Size, Snap to Element Distance), group "Margins" (Wrap Text at Dashes, Document-Specific Margins + H/V).
   - Colours, Fonts, Regions as they are (078, 079).
   Restore legacy labels ("Room Arrow Stalk Length", "Default Room Text", etc.); fix 180 px label column clipping with `SharedSizeGroup` (070). Numeric formats: Width/Arrow/Grid Size show 1 decimal (`FormatString="0.0"`, `Increment 0.5`) — verify fractional round-trip.
3. Access keys per legacy; default size about 640x560, tabs must not wrap onto two rows.
4. Remember last tab in UI settings (`MapSettingsLastTab`), like legacy `SettingsLastTabIndex`.
5. Validation: empty default room text or no letter blocks OK with the legacy message (`SettingsDialog.cs:412-433`), focusing the field.

## Tests
- Avalonia: `DialogViewModelTests.cs` — new fields round-trip via `ApplyToSettings`; margin boxes disabled until checkbox; validation; fractional line width; no-clipping on every tab (iterate tabs).

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D004 notes: fields missing 0; Light/Dark checked.
- Committed as `parity(077): map settings fields and groups`.

## Out of scope
- Colour/font editing (078), regions (079), OK through session (079).
