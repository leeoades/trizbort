# Map settings dialog review (T1.4d)

Reference: `SettingsDialog` + `RegionSettings` (`legacy/src/Trizbort/UI/SettingsDialog.cs`, `SettingsDialog.Designer.cs`, `RegionSettings.cs`).
Avalonia: `src/Trizbort.Avalonia/Views/Dialogs/MapSettingsDialog.axaml` (AXAML), `ViewModels/MapSettingsViewModel.cs` (VM).
Screenshots: `screenshots/winforms/SettingsDialog__*.png`, `RegionSettings__main__default.png`; `screenshots/avalonia/MapSettings__*.png`, `MapTheme__main__default.png`.
Reviewed from code and screenshots only (no build).

## 1. Field table

Reference tab order: About, Colors and Fonts, Lines and Grid, Regions, Other. Avalonia: Map Details, Grid & Snap, Sizing & Defaults, Colours, Fonts, Regions (AXAML:16,37,57,94,110,145).

| Field | Reference (tab, control, label) | Avalonia (control, binding, label) | Status | Notes |
|---|---|---|---|---|
| Title | About, `_titleTextBox`, "&Title" (Designer:730) | TextBox `Title`, "Title:" (AXAML:20) | Implemented | |
| Author | About, `_authorTextBox`, "&Author" | TextBox `Author` (AXAML:23) | Implemented | |
| Description | About, multiline, "&Description" | TextBox `Description`, "Description:" (AXAML:27) | Implemented | |
| History | About, multiline, "&History" | TextBox `History`, "History / Notes:" (AXAML:30) | Different | Label reworded. Text is the same property. |
| About hint | About, label "These details help identify your map to others." (Designer:672); group box "&Map" | none | Missing | Hint and group heading dropped. |
| Colour list (11 items) | Colors and Fonts, `_colorListBox` owner-drawn, swatch plus friendly name: Canvas, Border, Connection, Connection (selected), Connection (hover), Subtitle Text, Object Text, Connection Text, Grid, Start Room, End Room (Designer:788-799) | ItemsControl of `ColorPickerButton` per colour; label is the raw key from `Colors.ToName` (AXAML:96-104; `Colors.cs` names: `canvas`, `selectedLine`, `hoverLine`, `subTitle`, `smallText`, `lineText`, `startRoom`...) | Partial | Same 11 colours, but labels are internal camelCase keys, not the friendly names. Reference uses single "Change..." button and a colour dialog. |
| Colour multi-select | `_colorListBox` is `MultiExtended`; Change applies one colour to all selected (SettingsDialog.cs:477-494) | none | Missing | Minor power feature. |
| Room Name font | Colors and Fonts, name + size read-only boxes + "Ch&ange..." (FontDialog) (Designer:991-1026) | TextBox `RoomFontName` + NumericUpDown `RoomFontSize`, header "Room Name Font" (AXAML:113-118) | Different | Free-text family instead of a font picker. See Notes below the table. |
| Objects font | "&Objects:" | `ObjectFontName`/`ObjectFontSize`, "Objects Font" | Different | as above |
| Line font | "&Line:" | `LineFontName`/`LineFontSize`, "Line Text Font" | Different | as above |
| Subtitle font | "&Subtitle:" | `SubtitleFontName`/`SubtitleFontSize`, "Subtitle Font" | Different | as above |
| Font style (bold/italic) | Preserved from FontDialog (SettingsDialog.cs:569-577 copies `font.Style`) | not editable; VM only writes `Name` and `Size` (VM:250-269) | Missing | Style cannot be changed or seen. |
| Hand-drawn style | Lines and Grid, Lines group, "&Hand-drawn style" | CheckBox "Hand-Drawn Line Style" on Grid & Snap (AXAML:50) | Partial | Moved to a different tab, away from the other line settings. |
| Line width | Lines and Grid, "&Width", 1 decimal | NumericUpDown `LineWidth`, "Line Width:", Min 1 Max 20 (AXAML:68) | Partial | Screenshot shows integer "2"; check fractional values survive (legacy `DecimalPlaces=1`). Tab moved to Sizing & Defaults. |
| Arrow size | Lines and Grid, "&Arrow Size" | NumericUpDown `ConnectionArrowSize`, "Arrow Size:" (AXAML:71) | Partial | Same tab/decimal note. |
| Room arrow stalk length | "&Room Arrow Stalk Length" | `ConnectionStalkLength`, "Connection Stalk Length:" (AXAML:74) | Different | Label reworded, "Room Arrow Stalk" became "Connection Stalk". |
| Preferred distance between rooms | "&Preferred Distance Between Rooms" | `PreferredDistanceBetweenRooms` (AXAML:86) | Partial | Label truncated in screenshot (`Sizing-Defaults`): "Preferred Distance Between R" is cut off by the 180px column. |
| Text offset from line | "&Text Offset From Line" | `TextOffsetFromConnection`, "Text Offset from Line:" (AXAML:83) | Implemented | |
| Grid: visible | Lines and Grid, Grid group, "&Visible" | CheckBox "Show Grid Lines" (AXAML:47) | Implemented | Reworded. |
| Grid: show origin | "Show &Origin" | CheckBox "Show Coordinate Origin (0,0)" (AXAML:49) | Implemented | |
| Grid: snap to | "S&nap To" | CheckBox "Snap Elements to Grid" (AXAML:48) | Implemented | |
| Grid size | "&Size", 1 decimal | NumericUpDown `GridSize`, "Grid Size:", Min 4 Max 256 (AXAML:42) | Implemented | Screenshot shows "32". |
| Default room shape | Other, Room group, DropDownList: Straight Edges, Rounded Corners, Ellipse, Octagonal (Designer:496-500) | ComboBox bound to enum values (AXAML:65; VM:158-163) | Partial | Shows raw enum names ("SquareCorners") instead of "Straight Edges"; order is Octagonal before Ellipse. |
| Darkness stripe size | Other, Room, "&Darkness Stripe Size" | `DarknessStripeSize` (AXAML:77) | Implemented | |
| Object list offset | Other, Room, "&Object List Offset" | `ObjectListOffsetFromRoom` (AXAML:80) | Implemented | |
| Default room text | Other, Room, `_txtDefaultRoomName`, "Default Room &Text" | TextBox `DefaultRoomName`, "Default Room Name:" (AXAML:61) | Partial | Reworded. No validation (see checklist). |
| Resize/Drag handle size | Other, Advanced group, "Resize/Drag &Handle Size" (Designer:1310) | none; no VM property (`Settings.HandleSize` exists, `Settings.cs:162`) | Missing | |
| Snap to element distance | Other, Advanced, "&Snap to Element Distance" (Designer:1278) | none; no VM property (`Settings.SnapToElementSize`, `Settings.cs:298`) | Missing | |
| Advanced hint | "These settings adjust the ease of clicking and dragging." (Designer:1267) | none | Missing | |
| Wrap text at dashes | Other, Margins group, "Wrap Text at Dashes" | CheckBox "Wrap Room Name Text at Dashes" on Grid & Snap (AXAML:51) | Partial | Moved out of Margins group; wording more specific (arguably clearer). |
| Document-specific margins | Other, Margins, "Document-Specific Margins" | VM `DocumentSpecificMargins` (VM:118) loaded and saved (VM:191, 246) but no control | Missing | Setting round-trips silently, user cannot change it. |
| Horizontal margins | Margins, "&Horizontal margins", enabled only when the checkbox is ticked | VM `DocHorizontalMargin` (VM:121), no control | Missing | |
| Vertical margins | Margins, "&Vertical margins", same dependency | VM `DocVerticalMargin` (VM:124), no control | Missing | |
| Region list | Regions, `_regionListing` owner-drawn: fill swatch with "123" in text colour, plus name (SettingsDialog.cs:512-547; screenshot) | ListBox of names only (AXAML:153-159) | Partial | Swatch preview lost. |
| Add Region | "&Add Region"; next name `Region1`, `Region2`... (SettingsDialog.cs:463-474); new region fill White, text colour = Subtitle colour (:231) | Button "Add Region"; names `New Region`, `New Region 2`; fill LightYellow, text MidnightBlue (VM:290-310) | Different | Different default name and colours. |
| Change... (region) | "C&hange..." opens `RegionSettings`; double-click also opens it | none; details panel is always visible at right (AXAML:163-177) | Different | Intentional change (inline editing). See RegionSettings section. |
| Delete Region | "&Delete Region"; disabled for NoRegion; Delete key; rooms in that region are moved to NoRegion (SettingsDialog.cs:353-366, 447) | Button "Delete Region", no-op for NoRegion but not greyed out (VM:312-321, AXAML:151) | Partial | No room reassignment. Button never disabled. |
| Region rename | F2 / edit box in the list, validates, renames rooms (SettingsDialog.cs:324-350, 549+) | Name TextBox in details panel, read-only for NoRegion (AXAML:169) | Partial | Rename does not update `Room.Region` on rooms, no validation. |
| Region help note | "Note:  F2 to edit Region name" (Designer:1066) | none | Missing | Not needed if rename is inline. |
| OK / Cancel | OK/Cancel, `AcceptButton`/`CancelButton` set (Designer:1335-1338) | OK (`IsDefault`) / Cancel (`IsCancel`) (AXAML:184-185) | Implemented | |

Notes on fonts: legacy shows read-only name and size with a Change... button that opens the system font dialog (name, size, style). Avalonia has a free-text family field with no list or validation and a size spinner. A typo gives a silent fallback font. In the screenshot the size spinner is clipped (`MapSettings__Fonts__default.png`, the 80px column is too narrow).

## 2. Context checklist

| Item | Verdict | Note |
|---|---|---|
| Grouping (group boxes/headings) | Lost | Legacy groups: Map, Colors, Fonts, Lines, Grid, Room, Advanced, Margins. Avalonia has none except bold "Region Properties". Line settings are split across three tabs (Grid & Snap holds hand-drawn and wrap; Sizing & Defaults holds widths). |
| Label wording and units | Changed | Several reworded (see table). Friendly colour names replaced by raw keys (`selectedLine`, `subTitle`, `smallText`). Enum names for shapes. No units anywhere in either app. |
| Tooltips / help text | Lost | Legacy has two hint labels (About, Advanced) and the region F2 note. Avalonia has none. Legacy has no real `ToolTip`s on this dialog (grep found none). |
| Enable/disable dependencies | Lost | Margins numeric boxes enable only with "Document-Specific Margins" (SettingsDialog.cs:58, 407-410); controls missing entirely in Avalonia. NoRegion: legacy disables Delete and name edit; Avalonia disables only the name (read-only). |
| Defaults | Changed | New-region defaults differ (above). Default room name "Cave" in VM, legacy screenshot shows "Test Room"; both come from `Settings`, so not a dialog difference. Font defaults come from `Settings`, not checked. |
| Order and tab order | Changed | Legacy: About first, Regions fourth, Other last. Avalonia: Map Details first, Regions last. Legacy remembers the last-used tab (`SettingsLastTabIndex`, SettingsDialog.cs:549-565); Avalonia always opens on the first tab. Tabs wrap onto two rows at 580px (screenshots). Legacy keyboard accelerators (&Title etc.) are gone. |
| Which tab a field lives on | Changed | Everything line-related moved. Hand-drawn is on Grid & Snap; wrap-at-dashes is on Grid & Snap; default room name, shape and sizes are merged into one Sizing & Defaults page of 9 rows. |
| Previews (colour/font samples) | Lost | Legacy: colour swatch per list item, region swatch with "123", RegionSettings live "Region Color:" sample. Avalonia colour buttons do show a swatch and name (good); font has no sample; region list has none. |
| Validation messages | Lost | Legacy blocks OK for an empty default room name or one without a letter (SettingsDialog.cs:412-433). Regions: duplicate name, empty name, `"` replaced by `'`, `_` and `:` blocked (RegionSettings.cs:45-80; SettingsDialog.cs:374-391, 501-509). Avalonia has none; `ApplyToSettings` writes whatever is typed (VM:233, 276-280). |
| OK/Cancel/Apply | Pass | Both have OK and Cancel only. Difference: legacy applies region edits and deletions immediately on the live `Settings.Regions`/rooms, so Cancel does not undo them (SettingsDialog.cs:230-233, 359-360). Avalonia edits a copy and applies on OK, which is better but is a behaviour change to note. |
| Enter/Esc | Pass | `IsDefault`/`IsCancel` match legacy Accept/Cancel. |
| Size/resizability | Changed | Legacy `FixedDialog`. Avalonia is resizable, 580x520, min 500x420 (AXAML:8-9). Fine, but several columns are clipped at default size (preferred-distance label, font size spinner). |
| Rename/delete side effects | Lost | Legacy renames and reassigns rooms (`UpdateExistingRoomRegions`, SettingsDialog.cs:~370-380 / `DeleteRegion`). Avalonia only rewrites `Settings.Regions` (VM:276), so rooms keep a region name that no longer exists. Likely data-integrity bug: verify in T2 and log in `issues.md` if confirmed. |

## RegionSettings (D005)

Legacy: separate modal "Region Settings" dialog (`RegionSettings.cs`) with Name box (disabled for NoRegion, RegionSettings.cs:23-24), "Region Color:" button and "Text Color:" button, and a live preview label "Region Color:" drawn in the chosen text colour (:25-26, :70-74). OK validates (:43-68); `_` and `:` keypresses are swallowed (:76-80).

Avalonia: no separate dialog. Its function is the "Region Properties" panel on the Regions tab (AXAML:163-177) with Name, "Background Fill:" and "Text Colour:" `ColorPickerButton`s. Verdict: Different (intentional, an improvement for discoverability and fewer clicks), but it loses the live text-on-fill preview, all name validation, and the room-reference updates. Cancel semantics are better.

## MapTheme dialog vs the reference theme menu

Reference: no dialog. A "Themes" menu inserted after Project Settings (`MainForm.Themes.cs:14-27`) lists built-in themes (one click applies), then "Import theme...", "Export current theme...", and "Infer default room style from rooms...".
Avalonia: `MapThemeDialog` (New-in-Avalonia): list of built-ins, details (name, default shape, room font), "Reset individual room/line styles" checkbox with tooltip, orange warning when `HasIndividualStyles`, Import/Export buttons, "Apply Theme"/"Cancel".

| Aspect | Reference | Avalonia | Verdict |
|---|---|---|---|
| Choose and apply a built-in | Menu click, then Yes/No/Cancel prompt if individual styles exist (`MainForm.Themes.cs:31-42`) | Select, tick checkbox, Apply (`MapThemeViewModel.cs:34-40`) | Changed, arguably better (preview of details). Warning only appears when styles exist (good). |
| Wording of consequences | Prompt says what is replaced, what is preserved, and "There is no undo" | Warning says only "will reset them"; no mention of no-undo | Lost: add the preserved/no-undo text. |
| Import | Loads file and applies it immediately (`:58-84`) | Adds to the list and selects it; user must still Apply (`MapThemeDialog.axaml.cs:17-41`) | Changed, fine. |
| Export | "Export current theme": `MapTheme.Capture(...)` of the live map settings (`:86-109`) | Saves `SelectedTheme` (a built-in or imported one) (`MapThemeDialog.axaml.cs:43-66`) | Lost: cannot export the map's current look, which is the main way to create a custom theme. |
| Infer default room style from rooms | Menu item with confirmation summary (`:111-142`) | Not reachable. `RoomStyleInference` has no Avalonia caller (grep) | Missing. Add to matrix as a separate row or fold into this dialog. |
| Error text | "The theme could not be used." with exception message | "Failed to import theme" with message | Pass |
| Current theme indicator | none | none; selection always starts on first built-in (`MapThemeViewModel.cs:29`) | Changed: dialog suggests "Classic" is current. |

Recommendation: keep the dialog (it is a better home for theme options than a menu), but add "Export current map theme" (Capture), an "Infer default room style" button, and restore the no-undo wording.

## 3. Recommendation

Keep the Avalonia tab-per-topic layout and the inline region editor, but fix the losses. Add the missing fields (handle size, snap-to-element distance, document-specific margins checkbox with horizontal and vertical boxes that enable only when it is ticked) as an "Editing" group and a "Margins" group; the VM already carries the margin properties. Regroup with headings: put hand-drawn, line width, arrow, stalk, text offset, preferred distance in one "Lines" section, grid in a "Grid" section, defaults in a "Rooms" section, and use the friendly colour names and shape names from the reference. Restore validation (empty default name, no letter, empty/duplicate/illegal region names) and make region rename and delete update `Room.Region`. Replace the font text box with a font picker (family list, size, style) with a sample line, and restore the About hint text. Remember the last tab, put Map Details first (fine), and widen the label columns so nothing is clipped. Keep the Avalonia "apply on OK" semantics for regions.
