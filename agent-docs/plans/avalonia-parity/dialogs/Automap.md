# Automap dialogs (T1.4f)

Scope: `AutomapDialog`, `AutomapRoomSameDirectionDialog`, `DisambiguateRoomsDialog` (reference, `legacy/src/Trizbort/UI/`) vs
`AutomapSettingsDialog` (Avalonia, `src/Trizbort.Avalonia/Views/Dialogs/`). Evidence: code reading + screenshots under
`agent-docs/plans/avalonia-parity/screenshots/` (`winforms/AutomapDialog__*.png`, `winforms/AutomapRoomSameDirectionDialog__main__default.png`,
`winforms/DisambiguateRoomsDialog__main__default.png`, `avalonia/AutomapSettings__*.png`). No build or run. Status words per plan section 2.

Abbreviations: `AD` = `legacy/src/Trizbort/UI/AutomapDialog.cs`, `ADD` = `AutomapDialog.Designer.cs`, `AX` = `AutomapSettingsDialog.axaml`,
`AXC` = `AutomapSettingsDialog.axaml.cs`, `VM` = `src/Trizbort.Avalonia/ViewModels/AutomapSettingsViewModel.cs`.

---

## 1. AutomapDialog vs AutomapSettingsDialog (D007)

### 1.1 Field table

| Field | Reference (tab, control, label text) | Avalonia (control, binding, label text) | Status | Notes |
|---|---|---|---|---|
| Intro text | `_label1` "Trizbort can automatically generate a map from a transcript of a game, even while the game is in progress." (ADD:63) | none | Missing | Pure explanatory text; no equivalent. |
| Transcript file | `_label2` "&Transcript File" (ADD:73) + `_textBox` (ADD:80) -> `FileName` (AD:25,39) | `TextBlock` "Transcript File:" + `TextBox` `{Binding FileName}`, watermark "Path to transcript file..." (AX:17-19) | Implemented | Mnemonic (Alt+T) lost; watermark added (nice). |
| Browse | `_browseButton` "&Browse..." (ADD:92); OpenFileDialog, filter "Text and Log Files(*.txt, *.log)\|...", title "Open Transcript" (AD:60-69) | `Button` "Browse..." -> `BrowseTranscriptCommand` -> `IStorageService.OpenTranscriptFileAsync(topLevel, FileName)` (AX:20-22, VM:161-169) | Implemented | Filter/title live in the storage service (not checked here); cancel keeps old path (test `AutomapSettingsViewModel_BrowseTranscript_Cancelled_KeepsPreviousFileName`, `AutomapIntegrationTests.cs:171`). |
| Verbose | `_verboseTranscriptCheckBox` "Treat transcript as &VERBOSE; expect room descriptions." (ADD:148) -> `VerboseTranscript` | `CheckBox` "Transcript is verbose" `{Binding VerboseTranscript}` (AX:31-32) | Partial | Reworded; loses "expect room descriptions" hint. Default true in VM (VM:114). |
| Same-name rooms | `_roomsWithSameNameAreSameRoomCheckBox` "&Assume no two rooms have the same name." (ADD:138) -> `AssumeRoomsWithSameNameAreSameRoom` | `CheckBox` "Rooms with same name are same room" `{Binding AssumeRoomsWithSameNameAreSameRoom}`, indented 16px (AX:34-37) | Partial | Reworded (same meaning). Enable/check coupling differs, see 1.2. |
| Guess exits | `_guessExitsCheckBox` "&Guess possible exits from a room by reading its initial description." (ADD:161) | `CheckBox` "Guess exits from room descriptions" (AX:42-43) | Implemented | Reworded/shorter. |
| Continue from end | `_startFromEndCheckBox` "&Start processing from the end of the transcript file. " (ADD:201) -> `ContinueTranscript` | `CheckBox` "Start from end of file (for active transcripts)" (AX:25-26) | Implemented | Moved from Options group to directly under the file box; wording gains a useful hint. |
| Two-way | `_chkAssumeTwoWayConnections` "Always assume two-way connections." (ADD:189) | `CheckBox` "Assume two-way connections" (AX:39-40) | Implemented | "Always" dropped. Default true in VM (VM:123). |
| Add object command | `_addObjectCommandTextBox` (ADD:272), preceded by ">" (ADD:266), trailing label "followed by an object name adds it to the map." (ADD:256) | `TextBox` `{Binding AddObjectCommand}`, label "Add Object Command:" (AX:49-50) | Partial | Text field present; the explanatory sentence and ">" prompt cue are gone. Blank falls back to default (VM:148,179). |
| Add region command | `_addRegionCommandTextBox` (ADD:244), ">" (ADD:238), trailing label "followed by region name to add room to region." (ADD:229) | `TextBox` `{Binding AddRegionCommand}`, label "Add Region Command:" (AX:52-53) | Partial | As above. |
| Command heading | `_label3` "At the in-game prompt:" (ADD:284) | none | Missing | Explains that these are typed into the game. |
| Single step | `_singleStepCheckBox` "&Step with F11 (Debug)", visible only `#if DEBUG` (AD:14-18; ADD:126) -> `SingleStep` | none; `GetSettings` hard-codes `SingleStep = false` (VM:175) | Won't-port candidate (Lee decides) | Debug-only in reference; the screenshot is from a Debug build. Treat as intentional unless Lee wants it. |
| Group boxes | `_groupBox1` "&Options" (ADD:178), `_groupBox2` "&Commands" (ADD:220) | `Separator`s only (AX:28,45) | Different | See 1.2 grouping. |
| Start / Cancel | `_startButton` "Start Automapping" `DialogResult.OK` (ADD:104); `_cancelButton` "Cancel" (ADD:115) | "Start" `IsDefault`, enabled by `CanStart` (AX:60); "Cancel" `IsCancel` (AX:61) | Different | Start is disabled until a file name is entered (VM:108); legacy has no such gate. |
| Persist on OK | `OnClosed` writes `AppSettings.Automap = Data` on OK (AD:53-58) | `DialogService.ShowAutomapSettingsAsync` writes and saves settings when result true and file name non-blank (DialogService.cs:340-350) | Implemented | Avalonia additionally calls `SaveSettings()`. |

### 1.2 Context checklist

| Item | Verdict | Note |
|---|---|---|
| Grouping (group boxes / headings) | Lost | Reference has "Options" and "Commands" group boxes (ADD:178,220). Avalonia has two unlabelled separators and no headings; file/start-from-end sit together, options and commands unlabeled. |
| Label wording and units | Changed | All checkbox labels reworded (see table). "Assume no two rooms have the same name" -> "Rooms with same name are same room" is equivalent. "expect room descriptions" and "Always" hints dropped. |
| Tooltips / help text | Lost | Reference has intro sentence, "At the in-game prompt:" and the two "followed by ..." explanations; none are tooltips (no `ToolTip` in designer) but all are inline help. Avalonia has none, so a user cannot tell what "Add Object Command" does. |
| Enable/disable dependencies | Changed | Same control disables under same condition (same-name box disabled when not verbose: VM:152-158, AX:36), and forced checked when verbose is off (VM:145,157; screenshot `AutomapSettings__main__Transcript-is-verbose-off.png` matches `AutomapDialog__main__m-verboseTranscriptCheckBox-off.png`). Differences: legacy toggling verbose also resets same-name to `!verbose` (AD:73-75), so turning verbose ON unchecks it; Avalonia leaves the previous value (VM:152-158). Legacy setter at load takes `Enabled` from the verbose box's Enabled (always true) rather than Checked (AD:42), so Enabled at load is not reliable in legacy; Avalonia uses `settings.VerboseTranscript` (VM:144), which is the evidently intended behaviour. Net: Avalonia is more correct on load, legacy resets on re-enable. |
| Defaults | Pass | Verbose true, two-way true, guess exits true, "tb see"/"tb region" (VM:114,123,126,129,132) match the reference screenshot defaults. Start-from-end false, same-name false. |
| Order and tab order | Changed | Reference order: file, verbose, same-name, guess, start-from-end, two-way, then commands (TabIndex ADD). Avalonia: file, start-from-end, verbose, same-name, two-way, guess, commands. Start-from-end moved up; two-way/guess swapped. No tabs in either dialog. |
| Which tab a field lives on | Pass | Single page in both. |
| Previews | Pass (n/a) | None in either. |
| Validation messages | Changed | Reference has none. Avalonia disables Start on blank path (VM:108) and silently ignores a blank-path click (AXC:20-24); no message explains why Start is greyed. |
| OK / Cancel / Apply | Changed | OK is labelled "Start Automapping" in reference, "Start" in Avalonia. No Apply in either. |
| Enter / Esc | Pass | Reference `AcceptButton`/`CancelButton` (ADD:288,291); Avalonia `IsDefault`/`IsCancel` (AX:60-61). Enter is a no-op while Start is disabled. |
| Size / resizability | Changed | Reference is `FixedDialog` 486x402 and everything is visible (ADD:292,303). Avalonia is resizable 480x420 min 420x360 inside a `ScrollViewer` (AX:8-9,14); in the captured screenshot (`AutomapSettings__main__default.png`) the Commands rows are clipped under the buttons and the scrollbar is showing, so default height is too small. Needs a taller default (about 480x500) or `SizeToContent="Height"`. |
| Window title | Changed | "Automapping" (ADD:310) vs "Automap Settings" (AX:7). |
| Mnemonics / access keys | Lost | Reference uses `&` on Transcript, Browse, Options, VERBOSE, Assume, Guess, Start, Commands. Avalonia labels use none (`_` prefix not used). |

### 1.3 Recommendation

Keep the Avalonia layout (flat, modern, resizable) but restore the context it lost: add "Options" and "Commands" headings (or `Expander`/`HeaderedContentControl`), keep the intro sentence at the top, restore the "At the in-game prompt:" line with the two "followed by ..." hints as helper text beside the command boxes, and restore "expect room descriptions" in the verbose label. Make the default window tall enough to avoid clipping the Commands rows, and add a short hint when Start is disabled (e.g. red "Choose a transcript file"). Use the Avalonia-style behaviour for the verbose/same-name coupling but also reset same-name to unchecked when verbose is turned on if Lee wants strict legacy parity. Treat the missing Single Step option as intentional (debug-only). Overall status: Partial.

---

## 2. AutomapRoomSameDirectionDialog (D008) - spec, no Avalonia counterpart

Reference screenshot: `screenshots/winforms/AutomapRoomSameDirectionDialog__main__default.png`. No Avalonia screenshot or code exists.

- **Purpose.** During automapping, the transcript moves the player in a direction where the current room already has a connection to a room, but the transcript's destination room name is a new one. The user decides which is correct.
- **When shown.** From Core `Automap.cs`: `FindRoom` returned null (new room), `_lastKnownRoom` and `_lastMoveDirection` are set, and `_lastKnownRoom.GetConnections(dir).FirstOrDefault()?.GetTargetRoom()` is non-null (`src/Trizbort.Core/Automap/Automap.cs:400-418`, call at line 414).
- **Inputs.** `Room existing` (the room already in that direction, `Room1`) and `string roomName` (the transcript's name, `Room2`) (legacy `AutomapRoomSameDirectionDialog.cs:22-23`). Message text: `Room '{Room1.Name} is already defined in the same direction as room '{Room2}'.  What would you like to do?` (cs:28-29; note the missing closing quote after `Room1.Name`; fix in the port). Button labels `Keep '{Room1.Name}'`, `Keep '{Room2}'`, fixed "Keep Both" (cs:30-31, Designer:76).
- **Outputs / return value.** `AutomapSameDirectionResult` (`src/Trizbort.Core/Automap/AutomapSameDirectionResult.cs`): `KeepRoom1`, `KeepRoom2`, `KeepBoth` (legacy cs:7-12). The result defaults to `KeepRoom1` (first enum value) if the window is closed with the X, since the buttons are the only code that sets it (cs:24,34-50).
- **Core callback served.** `Automap.ConflictingRoomChooser` (`Func<Room, string, AutomapSameDirectionResult>`, `Automap.cs:806`), wrapped by the `Automap()` constructor at 809-811 and stored as `_chooseConflictingRoom` (line 20). Core behaviour per result (lines 417-431): `KeepRoom1` -> reuse existing room; `KeepRoom2` -> create the new room, connect it, remove the existing room; `KeepBoth` -> create the new room and connect it (the old connection in that direction is not removed by Core here).
- **Legacy wiring.** `legacy/src/Trizbort/Automap/Automap.cs:810-815` (`ChooseRoom`, shows the dialog modally and returns `dialog.Result`).
- **Avalonia state.** `ConflictingRoomChooser` is never assigned in `src/Trizbort.Avalonia` (only `ErrorReporter` is wired, `App.axaml.cs:30,44-64`), so Core silently falls back to `KeepRoom1` (`Automap.cs:811`). Status: Missing; real behavioural gap, the user is never asked and the transcript's new room name is discarded.
- **Context to carry over.** Modal, centred on owner, no maximize/minimize (Designer:89-92); three equal-width stacked buttons on the right, message on the left; no Cancel; Esc should map to `KeepRoom1` (the legacy implicit default) or an explicit "Keep existing". Called from the automap background thread, so the UI service must marshal to the UI thread (compare `AvaloniaAutomapCanvas.FindRoom`, `AvaloniaAutomapCanvas.cs:19-23`) and block the Core thread for the answer.
- **Suggested port.** `IDialogService.ShowAutomapSameDirectionAsync(Window? owner, Room existing, string roomName) -> AutomapSameDirectionResult`, assigned to `AutomapEngine.ConflictingRoomChooser` in the same place as `WireAutomapErrorReporter`. Fix message quoting; show the existing room's name in the button, not just "Keep Room".

---

## 3. DisambiguateRoomsDialog (D009) - spec, no Avalonia counterpart

Reference screenshot: `screenshots/winforms/DisambiguateRoomsDialog__main__default.png`. Note this capture shows the intro label clipped (its third line is cut off), the 192-DPI designer scaling on a 96-DPI capture; give the port a properly sized message area.

- **Purpose.** When the transcript names a room that matches several existing rooms ambiguously (same name, descriptions differ or changed), ask the user which existing room is meant, or that it is a new room.
- **When shown.** From the `IAutomapCanvas.FindRoom(roomName, roomDescription, line, matcher)` implementation (`src/Trizbort.Core/Automap/IAutomapCanvas.cs:11`; called from `Automap.cs:315-318,400`). The matcher is `Automap.Match` (`Automap.cs:321-344`) returning true (certain), false (no), or null (maybe); a null match adds the room to the candidate list. Legacy `legacy/src/Trizbort/UI/Controls/Canvas.Automap.cs:196-225`: first true match returns immediately; no candidates -> null; if `_dontAskAboutAmbiguities` -> first candidate (209); one candidate and (no description or candidate has none) -> that candidate (210); otherwise show the dialog (212-216). Flag is reset per run at `Canvas.Automap.cs:258`. Never reached when the transcript is non-verbose or "same-name rooms are same room" is on (`Automap.cs:327-336`).
- **Inputs.** `roomName`, `roomDescription`, `line` (`SetTranscriptContext`, legacy `DisambiguateRoomsDialog.cs:77-81`; the box shows `line` then the description, CRLF normalised; `roomName` is unused); candidate list `IEnumerable<Room>` (`AddAmbiguousRooms`, cs:100-103; list items display `Room.Name`, cs:124-136). Selecting a candidate fills a read-only Description box with `Room.PrimaryDescription` (cs:105-118).
- **Controls.** Title "Disambiguate Rooms" (Designer:202); explanatory label (Designer:54); "When the transcript says:" + read-only multi-line context box; "Which room does it mean?"; "&Room" list and "&Description" box; buttons "&This Room" (Yes, default `AcceptButton`, Designer:149,178), "A &New Room" (No, Designer:161), "&Auto-Choose the Rest" (Abort, Designer:173). Min size 1002x779 (resizable) at 192 DPI (Designer:198).
- **Enable rules / defaults.** "This Room" disabled until a list item is selected (cs:64,107-117). A single candidate is auto-selected on load (cs:85-87). Focus starts in the context box with the caret at 0 (cs:89-90). No candidate is selected by default when there are several.
- **Outputs / return value.** Three outcomes, encoded in `DialogResult`: `Yes` -> `Disambiguation` returns the chosen `Room` (cs:67-73); `No` ("A New Room") -> `Disambiguation` is null so `FindRoom` returns null and Core creates a new room (`Automap.cs:402-`); `Abort` -> `UserDoesntCareAnyMore` is true (cs:75), the canvas sets `_dontAskAboutAmbiguities = true` for the rest of the run and returns the first candidate (`Canvas.Automap.cs:216-222`). Closing the window with X gives a non-Yes/Abort result and so behaves as "A New Room". Port as an enum or record: `{ Room? Chosen; bool NewRoom; bool AutoChooseRest }`.
- **Core callback served.** There is no dedicated callback; it is served by `IAutomapCanvas.FindRoom` inside the canvas implementation, not by a static hook like `ConflictingRoomChooser`. Hence the port belongs in `AvaloniaAutomapCanvas.FindRoom` (`src/Trizbort.Avalonia/Services/AvaloniaAutomapCanvas.cs:19-24`), which currently delegates to Core `AutomapCanvas.FindRoom` (`src/Trizbort.Core/Automap/AutomapCanvas.cs:169-181`); that version never asks the user and always returns `list[0]` for an ambiguous match. Status: Missing (and a silent behaviour change: ambiguous rooms are always merged into the first candidate and a genuinely new room with the same name is never created).
- **Port notes.** The ask must be marshalled to the UI thread and block the automap thread (already marshalled in `AvaloniaAutomapCanvas`, but the Dispatcher.Invoke blocks while the dialog is awaited: use a nested `ShowDialog` or `DispatcherFrame`/`Invoke(async)` pattern). Move the "should we ask" rules (legacy `Canvas.Automap.cs:209-210`) and the `_dontAskAboutAmbiguities` flag (reset at run start, `Canvas.Automap.cs:258`) into Core `AutomapCanvas` or a view-model-free service so they are testable and the dialog is only the final step. Suggested API: `IDialogService.DisambiguateRoomAsync(Window? owner, string line, string description, IReadOnlyList<Room> candidates) -> DisambiguationResult`.
