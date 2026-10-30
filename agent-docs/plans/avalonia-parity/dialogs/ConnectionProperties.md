# ConnectionPropertiesDialog review (T1.4b)

Reference: `legacy/src/Trizbort/UI/ConnectionPropertiesDialog.cs` + `.Designer.cs` (abbrev. `Ref.cs` / `Ref.D`).
Avalonia: `src/Trizbort.Avalonia/Views/Dialogs/ConnectionPropertiesDialog.axaml` (`.axaml`), `.axaml.cs`, `ViewModels/ConnectionPropertiesViewModel.cs` (`VM`).
Screenshots: `screenshots/winforms/ConnectionPropertiesDialog__main__default.png`, `screenshots/avalonia/ConnectionProperties__main__{default,Has-Door-on}.png`
(`Has-Door-off` is the same as `default`; no WinForms Door-on screenshot, so the legacy enable behaviour below is from code).
Not statically checked: ColorPickerButton popup internals (only skimmed) and runtime layout beyond the screenshots.

## 1. Field table

| Field | Reference (tab, control, label) | Avalonia (control, binding, label) | Status | Notes |
|---|---|---|---|---|
| Name | Top of form, `_txtName` TextBox, "Name:" (Ref.D:332-346) | TextBox `ConnectionName`, "Name:" in General group (.axaml:19-20) | Implemented | Same field. Avalonia adds a "General" heading. |
| Description | Bottom of form, multiline `_txtDescription` (78px high), "Description:" label above (Ref.D:394-409) | Single-line TextBox `ConnectionDescription`, "Description:" beside Name (.axaml:22-23) | Partial | Lost multi-line editing: no `AcceptsReturn`/`TextWrapping`/height, so long or multi-line descriptions are cramped. Moved up next to Name. |
| Dotted | Style group, `&Dotted` CheckBox (Ref.D:103-109) | CheckBox `IsDotted`, "Dotted Line" (.axaml:31) | Implemented | Maps to `ConnectionStyle.Dashed` (VM:56). Label reworded; mnemonic lost. |
| One Way Arrow | Style group, `One Way &Arrow` CheckBox (Ref.D:92-98) | CheckBox `IsDirectional`, "One-Way (Directional)" (.axaml:30) | Implemented | Wording changed ("Arrow" dropped; "Directional" is jargon). Maps to `ConnectionFlow.OneWay` (VM:55). |
| Colour swatch | Style group, read-only `connectionColorBox`, shows colour or "No Color Set"; double-click opens picker (Ref.D:287-293; Ref.cs:20-35, 96-99) | `controls:ColorPickerButton SelectedColor=ConnectionColor` (.axaml:82) | Changed | Picker is a button + popup; whether it shows a swatch/"none" text was not verified. Control is off-screen in the default-size screenshot. |
| Colour change ("...") | `connectionColorChange` button opens colour dialog (Ref.D:300-303) | Same ColorPickerButton popup ("Choose Colour", hex "Set"; ColorPickerButton.axaml:15,55) | Changed | New-in-Avalonia hex entry. |
| Colour clear (red X) | `connectionColorClear` image button -> Transparent (Ref.D:270-276; Ref.cs:174-177) | "Default / Transparent" button inside picker popup (ColorPickerButton.axaml:18-22) | Changed | Still reachable but one extra click and hidden in a popup; no direct clear. |
| Text preset radios: Up/Down, Down/Up, In/Out, Out/In, Custom | Text group, 5 RadioButtons `&Up/Down` `&Down/Up` `&In/Out` `&Out/In` `&Custom` (Ref.D:190-246) | 4 buttons "Up / Down" "Down / Up" "In / Out" "Out / In" + "Clear" under "Presets:" (.axaml:55-60; .axaml.cs:15-39) | Changed | Radios (persistent state) replaced by one-shot buttons. "Custom" radio gone. Radios auto-reflect current text (Ref.cs:156-168); buttons never show what is selected. "Clear" is new (empties all three texts incl. Middle, .axaml.cs:33-41). |
| Start text | Text group, `_startTextBox`, "&Start" (Ref.D:164-186) | TextBox `StartText`, "Start:" small label above (.axaml:40-41) | Implemented | Label position and font size changed (11px). |
| Middle text | Text group, `_middleTextBox`, "&Middle" | TextBox `MidText`, "Middle:" (.axaml:44-45) | Implemented | Same. |
| End text | Text group, `_endTextBox`, "&End" | TextBox `EndText`, "End:" (.axaml:48-49) | Implemented | Same. |
| Door | `_chkDoor` CheckBox "Door" (Ref.D:320-326) | CheckBox `HasDoor`, "Has Door" (.axaml:68) | Implemented | Reworded. |
| Openable | `_chkOpenable` "Openable" | CheckBox `Openable` (.axaml:70) | Implemented | |
| Open | `_chkOpen` "Open" | CheckBox `Open` (.axaml:71) | Implemented | |
| Lockable | `_chkLockable` "Lockable" | CheckBox `Lockable` (.axaml:72) | Implemented | |
| Locked | `_chkLocked` "Locked" | CheckBox `Locked` (.axaml:73) | Implemented | |
| OK / Cancel | `_okButton` / `_cancelButton`, AcceptButton/CancelButton (Ref.D:69-87, 413-416) | OK `IsDefault`, Cancel `IsCancel` (.axaml:90-91; .axaml.cs:43-51) | Implemented | No Apply in either. |

Fields missing (no equivalent control): 0 outright; 2 degraded (Custom radio, direct colour-clear + "No Color Set" indicator) are counted as context loss below.
Round-trip logic (Door null when unchecked, flow/style mapping) matches Ref.cs:63-87 (VM:48-104). Note VM:73-77 assigns `Door = null` and unchecked sub-flags are discarded exactly as legacy.

## 2. Context checklist

| Item | Verdict | Note |
|---|---|---|
| Grouping (group boxes/headings) | Changed | Legacy: two group boxes, "Style" (Dotted, One Way Arrow, **Color**) and "Text" (radios + Start/Middle/End); Name/Description/Door row ungrouped (Ref.D:127-132, 260-265). Avalonia: five headed sections (General, Line Style & Direction, Text Labels, Door & Lock Status, Colour) with no box borders (.axaml:17,28,36,66,79). Colour moved out of Style into its own section at the bottom; Description moved out of the bottom into General. Headings are clearer, but the flat, borderless layout gives weaker visual grouping. |
| Label wording and units | Changed | "One Way Arrow" -> "One-Way (Directional)"; "Dotted" -> "Dotted Line"; "Door" -> "Has Door"; "Color" -> "Line Colour" (British spelling, inconsistent with the rest of the app's "Color" naming, e.g. ColorPickerButton); Start/Middle/End get colons. No units involved. |
| Tooltips / help text | Pass | None in either (no `ToolTip` in Ref.D or .axaml). Opportunity only. |
| Enable/disable dependencies | Changed | Legacy: Openable/Open/Lockable/Locked enable only when Door checked (Ref.cs:127-133) - but only on a change event, so on first open with no door they all start enabled (visible in the WinForms screenshot). Avalonia: sub-options greyed from the start and re-enabled by `HasDoor` (.axaml:69). Avalonia is more correct. Clipped in screenshot, see Size. Radios <-> text boxes relationship (typing text selects the matching radio, choosing a radio fills Start/End) is **Lost**: no radio state, and presets do not clear Middle (VM:106-111; legacy `SetText` also leaves Middle, Ref.cs:169-174). |
| Defaults | Pass | New connection: all unchecked, empty texts, Transparent colour (VM:11-26); legacy defaults to Custom radio and "No Color Set". Door-sub-flags are reset when no door (VM:68-74). |
| Order and tab order | Changed | Legacy tab order: Style group (Dotted, One Way, Color) -> Text group (radios, Start, Middle, End) -> Door... -> Description -> OK/Cancel by `TabIndex` (Ref.D); Name was first visually. Avalonia: visual order Name, Description, Dotted/One-Way (reversed relative to legacy Dotted then One-Way), texts, presets, Door, Colour. Reasonable, but One-Way/Dotted order swapped. No explicit TabIndex (natural order). |
| Which tab a field lives on | Pass | Single page in both. |
| Previews (colour/font samples) | Changed | Legacy: always-visible colour swatch in the Style group, with "No Color Set" text. Avalonia: only the picker button, which is off-screen at default window size (see Size); no verified "no colour" text. |
| Validation messages | Pass | None in either. |
| OK / Cancel / Apply | Pass | OK + Cancel only in both; OK is default, Cancel is Esc (.axaml:90-91). Enter in a multi-line Description would trigger OK in Avalonia (single-line there) vs. insert newline in legacy (`AcceptButton` + multiline TextBox consumes Enter): **Changed**, linked to Description losing multiline. |
| Enter / Esc | Changed | See above; Esc = Cancel in both. |
| Mnemonics / accelerators | Lost | Legacy `&Dotted`, `One Way &Arrow`, `&Style`, `&Text`, `&Custom`, `&Start`, `&Middle`, `&End`, `&Up/Down` etc. (Ref.D:98,109,132,142,160,170,198-246,265). None in Avalonia (no `_` access keys). |
| Size / resizability | Lost | Legacy 544x485 client, fixed (Ref.D:417; MaximizeBox/MinimizeBox false), everything visible. Avalonia 480x480, resizable (min 420x400) in a ScrollViewer (.axaml:8-9,14). The Avalonia default screenshot shows a scrollbar, the Colour section completely hidden, and the Door sub-options clipped at the bottom edge: **the OK-visible default view hides the Colour control and half the door checkboxes**. Height must be raised (~ 600) or content tightened. |
| Show in taskbar / centring | Pass | `CenterOwner`, `ShowInTaskbar=False` (.axaml:10-11), same as legacy `CenterParent`. |

## 3. Recommendation

Keep the Avalonia sections-and-headings layout and the Door sub-options greying (it is more correct than legacy), but fix the lost context: (1) raise the default height / remove the scroll need so Colour and all four door checkboxes are visible; (2) move Colour back beside Dotted / One-Way (a "Style" section) and show a visible swatch with a "No colour" label plus a one-click clear, rather than burying clear inside the popup; (3) make Description a multi-line TextBox (`AcceptsReturn`, `TextWrapping`, ~4 lines) and put it last, as in legacy; (4) replace the one-shot preset buttons with the legacy radio set (Up/Down, Down/Up, In/Out, Out/In, Custom) that auto-selects from the current Start/Mid/End text, or at least highlight the matching preset (legacy `MatchText` logic, Ref.cs:156-168, could move into the VM); (5) restore the original labels ("Dotted", "One Way Arrow", "Door", "Color") for continuity, and add `_` access keys. Overall status: Partial.
