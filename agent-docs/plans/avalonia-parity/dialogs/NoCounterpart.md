# Dialogs with no Avalonia counterpart (T1.4g)

Reference = `legacy/` (`$REF`). Screenshots = `agent-docs/plans/avalonia-parity/screenshots/winforms/`. Core = `src/Trizbort.Core`.
Avalonia has none of these dialogs (grep of `src/Trizbort.Avalonia` for QuickFind, Statistic, OnlineHelp, InputDialog, ObjectListEditor, FontDialog returned nothing).
"Suggestion" lines are not requirements; Lee decides.

## D010 QuickFind

- **Purpose**: jump to rooms by text. Selects every room whose name, primary description, objects or subtitle contains the typed text, and centres the view on the first.
- **Opened by**: Ctrl+F only (no menu item), in the canvas key handler: `legacy/src/Trizbort/UI/Controls/Canvas.cs:1309-1313` (`new QuickFind()` then `UserInteraction.ShowDialog`). Only the canvas has this handler, so the shortcut works when the canvas has focus. Only caller (grep `new QuickFind`).
- **UI** (screenshot `QuickFind__main__default.png`; `QuickFind.Designer.cs:45-103`): title "Quick Find"; label "Enter room name, description, or objects"; one full-width text box (`txtFind`) with AutoComplete; buttons "Find" and "Cancel". Fixed dialog, centred on screen, no resize.
- **Inputs**: `Project.Current` rooms, via `Indexer.Index()` (`QuickFind.cs:38-60`). Cache holds one entry per non-empty Name (always added, even if empty), Description, Objects, Subtitle (trimmed), sorted by text (`:21`). Autocomplete list = those strings (`:23-25`).
- **Behaviour**:
  - Enter in the box or Find button runs the search (`:102-105`, `:33-36`).
  - Match = case-insensitive substring, current culture (`:79-84`). A room can match via several fields; the result list is not de-duplicated.
  - Selects all matches (`controller.SelectElements`), sets `Project.Current.ActiveSelectedElement` to the first match, scrolls so that element is centred (`EnsureVisible` sets `Origin` to the element bounds centre; it does not change zoom) (`:62-77`; `CanvasController.cs:24-35`).
  - Esc, Cancel, or losing focus (Deactivate) closes it with no action (`:28-31`, `:91-100`). Dialog focuses the text box on activate (`:86-89`).
  - Quirk: empty/whitespace text calls `Close()` but then still runs the search (`:65`, missing `return`); with `""` every room matches. Do not copy this; empty input should do nothing.
  - No-match: selection is emptied, nothing shown to the user.
- **Outputs**: new canvas selection, active element, view origin. No data changed.
- **Core API**: `Trizbort.Domain.Cache.Indexer.Index()` and `FindCacheItem` exist (`src/Trizbort.Core/Domain/Cache/Indexer.cs:8-22`, `FindCacheItem.cs`). Fields are Name, Description (primary), Objects, Subtitle, Element. Exists. `CanvasController.SelectElements/EnsureVisible` is legacy-UI only (not in Core); Avalonia needs its own select + centre-on-element in `MapCanvasControl` (not Core). `Project.ActiveSelectedElement` exists (`Project.cs:31`).
- **Suggestion**: inline search box (overlay at top of canvas, opened by Ctrl+F, Esc closes) filtering live with a result list; Enter/click selects and centres; also add an Edit-menu entry (legacy had none) and allow next/previous match. Search should also cover room region and optionally connection text.

## D011 MapStatisticsView

- **Purpose**: read-only text report about the map; a second command writes the same report to a log file.
- **Opened by**: Tools menu > "Map Statistics..." (no shortcut) -> `MainForm.cs:689-693` shows the dialog; Tools > "Map Statistic E&xport..." -> `MainForm.cs:683-687` calls `MapStatisticsView_Export` without showing the dialog (`MainForm.Designer.cs:1250-1262`). Both create a throw-away view. Only callers.
- **UI**: `MapStatisticsView.Designer.cs:39-59`: title "Map Statistics", 850x666 client, Segoe UI 8pt, one multiline read-only text box with vertical scrollbar, no buttons (close with the title-bar X). `MapStatisticsView__main__default.png` is an EMPTY capture (no project open when captured); needs a running MainForm with a map to see real output. Text is set on Load (`MapStatisticsView.cs:206-211`).
- **Report content** (`MapStatisticsString`, `MapStatisticsView.cs:19-174`), in order. If there are 0 rooms the whole text is "No rooms to take stats of." (`:23`).
  1. `# of Rooms`, `# of Dark Rooms`, `# of Unconnected Rooms`.
  2. Start room: exactly 1 -> "Start room = {name}"; >1 -> "More than one start room."; else "No start room.". End rooms: 1 -> "End room = {name}"; >1 -> "End rooms = ({names})"; else "No end room.".
  3. "Dimensions with margins: height H, width W" and "without margins" (from canvas bounds, include-padding true/false).
  4. `# of Connections: N total, N unlabeled, N one-way, N dashed/dotted, N up/down, N in/out.`; `# of Dangling Connections`; `# of Self Looping Connections`; `# of Dead Ends`.
  5. Door line: "N door(s), N locked, N lockable, N open, N openable".
  6. `# of Regions`, `# of Rooms without a region`, and if regions exist "Regions:" then one line "Name (N room/rooms)" per non-default region, sorted by name.
  7. `Total # Objects in All Rooms`; `Total # Rooms with Objects: N, N with 1, N with 2, N with 3+`.
  8. `Total # of Rooms with subtitles`, `... with descriptions`.
  9. Separator line of 30 "=" followed by "Odd stuff below here"; `Duplicate rooms: ...`; room shape counts (rectangular, elliptical, round cornered, octagonal).
  10. If regions exist: per non-default region "has no rooms." / "Only room in X: name" / "List of rooms in X: a, b". Else-if branch "Region Links" (`:127-148`) is unreachable because `NumberOfRegions > 0` is tested first; do not port as is.
  11. Counts with pluralisation (`Plur`): up-down, in-out, custom, diagonal (2-way), diagonal (1-way), "with middle text", bent (with text / "with no text"); then `DupConnectionList` for "in", "out", "up", "down".
- **Export** (`:176-199`): text written UTF-8 to a file named: no file name -> `<UserProfile>\trizbort.log`; file name containing ".trizbort" -> same path with `.log`; otherwise `<file>.trizbort` (this looks like a bug: probably meant `.log`). Silently overwrites. Then message box "Wrote log to <path>" titled "Log file written". Path separators are hard-coded `\`.
- **Core API**: `Trizbort.Domain.Application.MapStatistics` is a static class in Core with every number used (`src/Trizbort.Core/Domain/Application/MapStatistics.cs:9-290`, incl. `BentConnections`, `DiagonalConnections`, `DupConnectionList`, `NumberOfRoomsInRegion`, `RegionsLinked`). Exists. It reads `Project.Current` (static). The report string builder itself lives in the WinForms view and is NOT in Core; it should be moved to Core (e.g. `MapStatisticsReport.Build(Project, Settings)`) so it can be unit tested. `Canvas.ComputeCanvasBounds(bool)` (`Canvas.cs:501`) is WinForms; Avalonia has an equivalent `MapCanvasControl.ComputeCanvasBounds(project, includePadding)` (`MapCanvasControl.cs:1780`) and `MinimapTransform.ComputeCanvasBounds`.
- **Suggestion**: a Tools > "Map Statistics..." window showing grouped sections (or the same monospace text) with a Copy button and a "Save as..." (file picker instead of the auto-named log).

## D012 InputDialog

- **Purpose**: generic modal prompt with one or more labelled text boxes and a button set. A reusable helper, not a feature.
- **Opened by**: the only production caller is File > "Open Map from Web..." (Ctrl+Shift+O): `MainForm.cs:1349-1358` (`InputDialog.Show("Load from Web", items, OkCancel)` with one item label "URL", empty text); on OK `OpenUrl(input.Items["URL"])`. The screenshot titled "Rename room" is only a test capture (`legacy/src/Trizbort.Tests/DialogCaptureTests.cs:145-148`); legacy rename is not an InputDialog (Canvas F2 "Rename" opens the properties dialog: `Canvas.cs:2890-2893`).
- **UI** (`InputDialog.cs:67-423`, screenshot `InputDialog__main__default.png`): title = caller's; per item a label above a text box (label text, initial text, optional password mask); buttons by `InputBoxButtons`: Ok; OkCancel (Cancel, OK; Enter = OK); YesNo; YesNoCancel; Save; SaveCancel. Fixed height, width resizable (min width = widest label + 40); no icon/taskbar entry. Initial text is pre-selected (see screenshot). Esc has no cancel mapping (no CancelButton set), only the Cancel button/close box.
- **Inputs**: title, label(s), initial text(s), button set. **Outputs**: `Result` (Cancel/Ok/Yes/No/Save) and `Items[label]` text.
- **Follow-on behaviour**: `OpenUrl` asks to save the current project if dirty (`CheckLoseProject`), builds `new Uri(url)` (throws on an invalid string; unhandled in legacy), then `Project.Load(uri)` and `ApplicationSettingsController.OpenProject(uri.AbsoluteUri)` (`MainForm.cs:1360-1388`). File name is the last URL path segment.
- **Core API**: no dialog logic needed in Core. Loading from a URL exists: `Project.Load(Uri)` is `internal` in Core (`src/Trizbort.Core/Domain/Application/Project.cs:226-232`, calls `MapLoader.LoadMap(uri)`); Core only exposes it to `Trizbort` and test assemblies via `InternalsVisibleTo` (`Properties/AssemblyInfo.cs:5-7`), so Avalonia cannot call it until made public. Avalonia has no "Open from web" menu item (`MainWindow.axaml:42` only has Open Map).
- **Suggestion**: do not port InputDialog; add one small purpose-built "Open Map from URL" dialog (URL box, validation message for bad URLs, OK/Cancel with Enter/Esc), and use inline editors elsewhere.

## D013 ObjectListEditor (objects text box in Room properties)

- **Purpose**: not a dialog: a static text-editing helper (`legacy/src/Trizbort/UI/ObjectListEditor.cs:14-173`, no WinForms dependency) behind the Objects text box in Room properties. The list is free text where leading spaces and "-" bullets mean containment (see `ObjectList` in Core, `TabIndent = 4`, `src/Trizbort.Core/Domain/Misc/ObjectList.cs:46`).
- **Caller**: `RoomPropertiesDialog.cs:915-939` (`TxtObjectsKeyDown`), also Select-All handling first. Only caller.
- **Behaviour**:
  - Tab (no Ctrl/Alt) = `ChangeIndent(text, selStart, selLen, outdent:false)`; Shift+Tab = outdent: indents/outdents every line touched by the selection by one level, adjusts selection (`ObjectListEditor.cs:16-39`; outdent removes up to `TabIndent` leading spaces `:64`).
  - Enter (no modifiers) = `NewLine(...)`: starts a new line at the same nesting as the current line, inserting a "- " bullet (an already bulleted line gets another bullet, "- key" -> "-- key"); Enter on a line that holds only a bullet outdents it instead; returns null (default Enter behaviour) when there is no indentation to continue (`:48-95`, doc comment `:46-52`).
  - Result is written back to the text box and scrolled to caret; key suppressed.
  - Screenshot to compare: `RoomPropertiesDialog__Objects__default.png`.
- **Avalonia equivalent**: partial. `RoomPropertiesDialog.axaml:50-68` has an "Objects in Room:" label, a Position combo and a plain multi-line `TextBox` (`Text="{Binding Objects}"`, `AcceptsReturn`, fixed `Height=70`) bound at `RoomPropertiesViewModel.cs:154,210`. No Tab/Shift+Tab indent, no bullet continuation (Tab moves focus or inserts nothing special), no `ObjectListEditor` port anywhere in `src/` (grep). Spec for T1.4a to record as Missing.
- **Core API**: `ObjectListEditor` is in the legacy WinForms project only; it must be moved to `Trizbort.Core` (it is pure string logic; legacy has unit tests for it: look in `legacy/src/Trizbort.Tests`). `ObjectList`/`ObjectListItem` parsing already exists in Core (`ObjectList.cs`).
- **Suggestion**: move `ObjectListEditor` to Core, attach it to the Avalonia TextBox via a `KeyDown` handler (Tab/Shift+Tab/Enter, same rules), make the box taller/resizable, and show an optional tree preview of the parsed `ObjectList`.

## D014 OnlineHelpDialog

- **Purpose**: tell the user where the online docs are and open them in the browser.
- **Opened by**: Help > "Online Help" (F1): `MainForm.cs:580-584`; menu defined at `MainForm.Designer.cs:1274-1278`. Only caller. In Avalonia, F1 is bound to About (`MainWindow.axaml:32`, Help menu has only "About Trizbort...", `:93-94`), so Online Help is missing and F1 behaves differently (flag as Different).
- **UI** (built in code, `OnlineHelpDialog.cs:13-60`; screenshot `OnlineHelpDialog__main__default.png`): title "Online Help"; text "The Trizbort v2 user guide is available online:"; link `https://github.com/leeoades/trizbort/blob/master/Docs/index.md` (`UserGuideUrl`, `:10`); text "The original Trizbort v1 help, on which the guide is based, can be found here:"; link `https://trizbort.genstein.net/help/` (`OriginalHelpUrl`, `:11`); button "Close" (OK; both Accept and Cancel button, so Enter/Esc close). Fixed dialog, centred on parent, auto-sized, no taskbar entry.
- **Behaviour**: clicking a link opens it with the shell (`Process.Start` UseShellExecute, `:62-83`) and marks it visited; on failure a message box "Unable to open the link:\n\n<url>\n\n<error>" (error icon, title = dialog title).
- **Outputs**: none (launches the default browser).
- **Core API**: none needed. Avalonia needs a cross-platform launcher (`TopLevel.Launcher.LaunchUriAsync`, Avalonia 11.1+) in the UI layer. Nothing like it exists in `src/Trizbort.Avalonia` (grep of Process.Start/Launcher: none).
- **Suggestion**: replace the dialog by Help menu items that open the two URLs directly (e.g. "User Guide", "Original Trizbort v1 Help"), bind F1 to the user guide as in legacy, and keep About on its own item; show an error toast if the launch fails.

## D016 Colour and font pickers

- **Legacy colour picker** = WinForms `ColorDialog` (full common dialog), reached through `Colors.ShowColorDialog(color, parent)` (`legacy/src/Trizbort/Domain/Misc/Colors.cs:60-66`: Transparent is shown as White; Cancel keeps the old colour). Callers: `SettingsDialog.cs:481,487` (colour list edit / add new), `RegionSettings.cs:33,39` (region colour, region text colour), `ConnectionPropertiesDialog.cs:123`, and directly `LabelPropertiesDialog.cs:135` (`new ColorDialog { Color = button.BackColor, FullOpen = true }`).
- **Legacy font picker** = WinForms `FontDialog`: `SettingsDialog.ShowFontDialog` (`SettingsDialog.cs:569-578`) for the four fonts Large (`:255`), Line (`:260`), Small (`:287`), Subtitle (`:292`); starts from the current font, returns a new Font (name, size, style; GraphicsUnit.World) or the old one on Cancel. Only user of `FontDialog` (grep).
- **Avalonia colour**: exists. `Controls/ColorPickerButton.axaml:1-56+`: a button showing the colour that opens a flyout with title "Choose Colour", a "Default / Transparent" button, a palette of preset swatches (Black, White, Gray, ... Gold, ForestGreen, MidnightBlue, ...), and a "#RRGGBB" hex box with "Set" button (`:52-55`). Compared with `ColorDialog`: no RGB/HSV sliders or custom-colour slots, no eyedropper; adds a transparent/default option. Used in Room, Connection, Label, Region and theme dialogs. Status: Partial/Different (acceptable; a full picker is optional).
- **Avalonia font**: no font picker exists. `MapSettingsDialog.axaml:109-134` (Fonts tab) uses a free-text "Font Family" `TextBox` plus a size `NumericUpDown` (6-72) for Room Name, Objects, Subtitle and Line Text fonts; `AppSettingsDialog.axaml:27-28` has a "Default Font Name" free-text box. No style (bold/italic/etc.), no list of installed fonts, no sample/preview, typos in the name are only found when text renders. Status: Missing (picker) / Partial (fields). Core stores a font as name + size + style (`src/Trizbort.Core/Setup/Settings.cs:471,638-661`, `MapTheme.cs:427`), so style can be edited without Core changes.
- **Core API**: no pickers in Core (UI-only). Fonts/colours are modelled in Core (`Settings.cs`, `MapTheme.cs`); installed font list must come from Avalonia (`FontManager.Current.SystemFonts`).
- **Suggestion**: add a `FontPickerButton` control (ComboBox of `SystemFonts` + size + bold/italic toggles + live sample text) used in Map settings and App settings; optionally add a full colour view (Avalonia `ColorView`) behind a "More..." button in `ColorPickerButton`.
