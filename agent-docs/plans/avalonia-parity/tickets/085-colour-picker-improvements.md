# 085 — Colour picker: visible "no colour" state, one-click clear, optional full colour view

Matrix rows: D016, D001, D002, D003          Priority: P?          Size: S
Depends on: 070           Suggested tier: mid

## What
`ColorPickerButton` shows a swatch with text ("No Color Set" / name / hex), has a direct clear (X) button beside it (as legacy red X), and a "More..." entry that opens the full Avalonia `ColorView` (RGB/HSV, custom colours) in the flyout.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/NoCounterpart.md` §D016 (colour part) and `ConnectionProperties.md` rows Colour *.
- `$REF/src/Trizbort/UI/ConnectionPropertiesDialog.cs:20-35,96-99,174-177`; `$REF/src/Trizbort/Domain/Misc/Colors.cs:60-66`.
- `src/Trizbort.Avalonia/Controls/ColorPickerButton.axaml(.cs)`.

## Implement
1. Button content: swatch + text; transparent/default shows hatch swatch + "No Color Set" (property `NoColorText`, default that string).
2. Clear X `Button` in the control (visible only when a colour is set; `ToolTip` "Clear colour"); sets Transparent (Default) as legacy.
3. Flyout: keep presets and hex; add a "More..." expander/button revealing `ColorView` bound to the same colour. Cancel/Esc closes without change.
4. Use theme brushes for borders so Light/Dark are legible.
5. Access: `AutomationProperties.Name` bound to the label.

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/ColorPickerButtonTests.cs` — clear sets Transparent; text shows "No Color Set"; hex input; ColorView change updates `SelectedColor`.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D016 colour part updated; Light/Dark checked.
- Committed as `parity(085): colour picker`.

## Out of scope
- Eyedropper.
