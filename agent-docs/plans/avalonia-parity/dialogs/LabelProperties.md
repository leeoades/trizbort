# Label properties dialog review (T1.4c)

Reference: `legacy/src/Trizbort/UI/LabelPropertiesDialog.cs` (code-built form, no Designer). Avalonia: `src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml`, `src/Trizbort.Avalonia/ViewModels/MapLabelPropertiesViewModel.cs`, opened from `src/Trizbort.Avalonia/Services/DialogService.cs:259-272`.
Screenshots: `screenshots/winforms/LabelPropertiesDialog__main__{default,outlineEnabled-on,backgroundEnabled-on}.png`, `screenshots/avalonia/MapLabelProperties__main__*.png`.
Caveat: all five Avalonia captures show the same 440x420 window with the Background group clipped below the scroll fold (the dialog is too short), so the Background controls were verified from XAML only. The "-on/-off" captures are visually identical to default.

## 1. Field table

| Field | Reference (tab, control, label text) | Avalonia (control, binding, label text) | Status | Notes |
|---|---|---|---|---|
| Window title | "Label Properties" (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:14) | "Map Label Properties" (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:7) | Different | Wording only; reference name matches menu/UI term "Label". |
| Text | Multiline TextBox, caption "&Text" (left caption column), vertical scrollbar, fills spare height (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:30-35) | TextBox in group headed "Text", `{Binding Text}`, AcceptsReturn, wrap, fixed 60px (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:17-19) | Implemented | Avalonia has no mnemonic; box is smaller (about 3 lines) and does not grow. |
| Text colour | Button "Choose..." filled with colour, ColorDialog FullOpen (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:36-37,129-143) | ColorPickerButton, label "Text Colour:" (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:21-22) | Implemented | Different picker widget; shows swatch + name ("Black"). Same property (TextColor). |
| Outline enabled | CheckBox "&Enabled" in group "Outline" (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:40-44) | CheckBox "Draw Outline", `HasOutline` (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:30) | Implemented | Wording changed; mnemonic lost. Same mapping to BorderStyle None (src/Trizbort.Avalonia/ViewModels/MapLabelPropertiesViewModel.cs:75,88). |
| Outline shape | ComboBox "&Shape", all RoomShape except NotARoom (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:45-50) | ComboBox "Shape:", 4 explicit values (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:32-34, src/Trizbort.Avalonia/ViewModels/MapLabelPropertiesViewModel.cs:56-61) | Implemented | Same 4 values; order differs (Square, Rounded, Octagonal, Ellipse vs enum order Square, Rounded, Ellipse, Octagonal). |
| Outline line style | ComboBox "Line &style", all except None/Custom (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:51-57) | ComboBox "Line Style:" (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:36-38, src/Trizbort.Avalonia/ViewModels/MapLabelPropertiesViewModel.cs:63-69) | Implemented | Same 5 values. |
| Outline colour | Button, caption "Colour" (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:58-60) | ColorPickerButton, "Border Colour:" (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:40-41) | Implemented | Wording changed. |
| Background enabled | CheckBox "E&nabled" in group "Background" (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:66-69) | CheckBox "Fill Background", `HasBackground` (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:49) | Implemented | Wording changed. |
| Background colour | Button, caption "Colour" (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:70-72) | ColorPickerButton, "Fill Colour:" (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:51-52) | Implemented | Wording changed. |
| OK / Cancel | Buttons, AcceptButton/CancelButton (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:87-95) | Buttons OK (IsDefault) / Cancel (IsCancel) (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:60-63) | Implemented | Same order (OK left of Cancel). |
| Apply on OK | Writes all 7 properties (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:96-104) | `ApplyTo` writes the same 7 (src/Trizbort.Avalonia/ViewModels/MapLabelPropertiesViewModel.cs:83-92) | Implemented | Reference only writes colours via button BackColor; equivalent. |

Missing fields: 0. Reference controls not present in Avalonia: none (all 8 data controls map).

## 2. Context checklist

| Item | Verdict | Note |
|---|---|---|
| Grouping (group boxes/headings) | Changed | Reference: Text and Text colour ungrouped, GroupBoxes "Outline" and "Background" (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:39,65). Avalonia: three `HeaderedContentControl` headings incl. "Text" (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:17,28,47); no border, so headings look like plain text in the screenshot. |
| Label wording and units | Changed | "Enabled" -> "Draw Outline" / "Fill Background"; "Colour" -> "Border Colour" / "Fill Colour"; trailing colons added; "Text colour" -> "Text Colour:". Arguably clearer; no units involved. |
| Tooltips / help text | Pass | Neither has any. |
| Enable/disable dependencies | Changed | Outline style and colour grey out unless outline is on (Pass, src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:31 vs legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:78-79); fill colour greys unless background on (Pass, src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:50 vs legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:80). **Lost:** Shape is enabled when outline OR background is on in the reference (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:81) because shape also affects the fill; Avalonia disables Shape unless Outline is on (shape is inside the outline grid, src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:31-34), so a background-only label cannot change shape. |
| Defaults | Pass | Same defaults from label; None border maps to Solid in the combo (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:56, src/Trizbort.Avalonia/ViewModels/MapLabelPropertiesViewModel.cs:77). Avalonia also coerces NotARoom shape to SquareCorners (src/Trizbort.Avalonia/ViewModels/MapLabelPropertiesViewModel.cs:76). |
| Order and tab order | Pass | Text, text colour, outline, background, buttons in both. |
| Which tab a field lives on | Pass | Single page in both. |
| Previews (colour/font samples) | Changed | Reference colour is the button face (large swatch). Avalonia shows a small swatch + colour name. No font control in either. |
| Validation messages | Pass | None in either. |
| OK/Cancel/Apply | Pass | No Apply in either; Avalonia cancel returns false and discards (:264-271). |
| Enter/Esc | Pass | Enter = OK (IsDefault) in both; multiline text accepts Enter in both (AcceptsReturn). Esc cancels. |
| Initial focus / selection | Lost | Reference focuses Text and selects all on show (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:105-108, visible in screenshot); Avalonia has no focus/select-all, so retyping a label needs a manual select. |
| Mnemonics / access keys | Lost | Reference uses &Text, &Enabled, E&nabled, &Shape, Line &style; Avalonia none. |
| Size / resizability | Changed | Reference fixed 430x500 client, no resize (legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:15-21); Avalonia 440x420, resizable, min 380x350 (src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml:8-9). **Height is too small**: Background group is clipped behind a scrollbar in every capture, so the Background checkbox is not visible without scrolling. |

Counts for matrix: 0 fields missing; context lost/changed: 3 Lost (shape enable rule, initial focus/select-all, mnemonics) plus 5 Changed.

## 3. Recommendation

Keep the Avalonia layout (modern wording like "Draw Outline"/"Fill Background" is fine) but fix four things: raise default height to about 520 (or SizeToContent=Height) so the Background group is visible without scrolling; move Shape out of the outline-only grid and enable it when Outline or Background is checked (matches legacy/src/Trizbort/UI/LabelPropertiesDialog.cs:81); focus and select all text in the Text box on open; and consider letting the Text box grow with the window. Mnemonics are optional and low priority.
