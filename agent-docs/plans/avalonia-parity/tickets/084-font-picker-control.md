# 084 — Font picker control (family list, size, bold/italic, live sample)

Matrix rows: D016, D004, D006          Priority: P?          Size: M
Depends on: 070           Suggested tier: mid

## What
A reusable `FontPickerButton` lets users choose a font as the legacy FontDialog did: a flyout with searchable list of installed families (with each name drawn in its own face), size, Bold/Italic, and a live sample; used for the four map fonts (078) and the app default font (080 follow-up).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/NoCounterpart.md` §D016.
- `$REF/src/Trizbort/UI/SettingsDialog.cs:569-578` (`ShowFontDialog`) and Designer grep for the four font rows.
- `src/Trizbort.Avalonia/Controls/ColorPickerButton.axaml(.cs)` — pattern for a button + flyout control.
- `src/Trizbort.Core/Setup/Settings.cs:471, 638-661` — font = name + size + style.

## Implement
1. Avalonia: `Controls/FontPickerButton.axaml(.cs)` with `SelectedFont` (`FontSpec` record `{Family, Size, Bold, Italic}` defined in `ViewModels/`), button text "Family, 10 pt, Bold" rendered in that font; flyout: filter TextBox, ListBox of `FontManager.Current.SystemFonts` names (virtualised, each item `FontFamily` bound to itself), size `NumericUpDown` (6–72), Bold/Italic `ToggleButton`s, sample "AaBbYyZz 123" and room-name sample in the chosen font; "Done"/"Cancel" in flyout, Esc cancels.
2. A missing family (stored name not installed) is shown with "(not installed)" and keeps the stored value until changed.
3. Include a default "(Default)" entry only for the App default font.
4. Access name `AutomationProperties.Name` for each flyout control.

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/FontPickerButtonTests.cs` — selecting, toggling style updates `SelectedFont`; unknown family preserved; Cancel keeps old value.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D016 font part -> Implemented.
- Committed as `parity(084): font picker control`.

## Out of scope
- Using it in dialogs (078, 080 follow-up).
