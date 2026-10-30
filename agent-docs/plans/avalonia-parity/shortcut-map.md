# Shortcut map (decision D4, draft for Lee)

Status: Draft 1 (2026-10-30). Inputs: plan `avalonia-parity-review.md` section 1 (L4, L5), section 2 and the D4 card; `feature-matrix.md` sections M and K (including open questions); `Window.KeyBindings` and `InputGesture` in `src/Trizbort.Avalonia/Views/MainWindow.axaml`; `OnKeyDown` in `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs`. Last column is for Lee.

## 1. Policy, notation, single-letter keys

**Policy (Lee, L4).** Port every shortcut and feature. Keep the reference (WinForms) gesture unless an obvious modern convention exists, then use the convention. Right-drag pan is the model of a good change. Undo/redo is coming (L3).

**Notation.** `Primary` = Ctrl on Windows/Linux, Cmd on macOS. `Alt` = Option on macOS. `Shift` is Shift. `Ctrl` written alone on a macOS variant means the physical Control key. "Reference" gestures that say `Ctrl+X` in the matrix are written `Primary+X` here. Avalonia implements `Primary` through the platform hotkey configuration (`CommandModifiers`), not by hard-coding `KeyModifiers.Control` as `OnKeyDown` does today (AVC:1228; matrix K open question 2).

**Single-letter and bare keys on the canvas** (R, L, J, W, K, P, T, A, U, D, I, O, V, 0, `[`, `]`, `;`, `\`, Enter, F2, F3, F5, arrows, Home/End/PageUp/PageDown, NumPad):
- They act only when the map canvas has keyboard focus and no text box or in-place editor is active. A text box always gets the key first.
- They fire only with exactly the modifiers the row lists. In particular a bare-letter row must not fire when Primary or Alt is held (the reference mostly ignores modifiers, which is why Ctrl+V pastes and V reverses; the port checks modifiers explicitly). The two reference exceptions kept on purpose are `+`/`-` (any modifier) and `F11`.
- Primary-modified gestures are window-level `KeyBinding`s with menu items (so they are discoverable and work with focus in the toolbar). Bare keys are canvas-only and are listed in the menu with `InputGesture` text only where the reference menu showed them.
- Ctrl+Alt+letter is AltGr on some Windows layouts, so those gestures must not fire when a text box has focus.

Mouse-only gestures (wheel zoom, double-click, drag) belong to matrix section G and are not mapped here, except the two pan rows for continuity.

## 2. Table

Change values: keep = reference gesture unchanged; modernise = convention replaces the reference gesture; new = no reference gesture; conflict-fix = the gesture changes or is corrected to resolve a clash or an Avalonia bug.

| Command | Matrix rows | Reference gesture | Current Avalonia gesture | Proposed gesture | Change? | Reason | Lee: approve/change |
|---|---|---|---|---|---|---|---|
| New Map | M001 | Primary+N | Ctrl+N | Primary+N | keep | Universal. | |
| Open Map | M002 | Primary+O | Ctrl+O | Primary+O | keep | Universal. | |
| Open Map from Web | M003 | Primary+Shift+O | none | Primary+Shift+O | keep | No convention; reference is free of clashes. | |
| Save Map | M004 | Primary+S | Ctrl+S | Primary+S | keep | Universal. | |
| Save Map As | M005 | Primary+Alt+S | Ctrl+Shift+S | Primary+Shift+S | modernise | Primary+Shift+S is the near-universal Save As. Avalonia already uses it. | |
| Smart Save | M006 | Primary+Shift+S | none | Primary+Alt+S | conflict-fix | Takes the gesture Save As leaves behind (a swap), so users of the old Save As chord get the other save command. | |
| Export Alan (file) | M009 | Primary+Shift+L | none | Primary+Shift+L | keep | Mnemonic; no clash. | |
| Export Adventuron (file) | M009 | none | none | none | keep | Menu only. | |
| Export Hugo (file) | M009 | Primary+Shift+H | none | Primary+Shift+H (macOS: Primary+Alt+Shift+H) | keep | Cmd+Shift+H is Finder-only, fine; see macOS notes for the clipboard sibling. | |
| Export Inform 7 (file) | M009 | Primary+Shift+7 | none | Primary+Shift+7 | keep | No clash. | |
| Export Inform 6 (file) | M009 | Primary+Shift+6 | none | Primary+Shift+6 | keep | No clash on Win/Linux. Cmd+Shift+6 is Touch Bar screenshot on older MacBooks. | |
| Export TADS (file) | M009 | Primary+Shift+T | none | Primary+Shift+T | keep | No clash. | |
| Export ZIL (file) | M009 | Primary+Shift+Z | none | Primary+Shift+I | conflict-fix | Primary+Shift+Z must be Redo (below). I for Infocom; free. | |
| Export Quest (file) | M009 | Primary+Shift+Q | none | Primary+Shift+Q (macOS: Primary+Alt+Shift+Q) | keep | Cmd+Shift+Q is log out on macOS. | |
| Export Alan to clipboard | M010 | Primary+Alt+L | none | Primary+Alt+L | keep | No clash. | |
| Export Adventuron to clipboard | M010 | Primary+Alt+A | none | Primary+Alt+A | keep | No clash. | |
| Export Hugo to clipboard | M010 | Primary+Alt+H | none | Primary+Alt+H (macOS: Primary+Alt+Shift+H) | keep | Cmd+Option+H is Hide Others on macOS. | |
| Export Inform 7 to clipboard | M010 | Primary+Alt+7 | none | Primary+Alt+7 | keep | No clash. | |
| Export Inform 6 to clipboard | M010 | Primary+Alt+6 | none | Primary+Alt+6 | keep | No clash. | |
| Export TADS to clipboard | M010 | Primary+Alt+T | none | Primary+Alt+T | keep | No clash. | |
| Export ZIL to clipboard | M010 | Primary+Alt+Z | none | Primary+Alt+Z | keep | No clash (Redo uses Shift, not Alt). | |
| Export Quest (no header) to clipboard | M011 | Primary+Alt+Q | none | Primary+Alt+Q | keep | No clash. | |
| Exit | M013 | none | Alt+F4 (displayed only; OS handles it) | Windows: Alt+F4. Linux: Primary+Q. macOS: Cmd+Q | modernise | Each platform's standard quit gesture. | |
| Undo | (new, L3) | none | none | Primary+Z | new | Universal. | |
| Redo | (new, L3) | none | none | Primary+Y and Primary+Shift+Z (macOS: Cmd+Shift+Z only) | new | Per Lee. Cmd+Y is History in many mac apps, so not bound there. | |
| Toggle Text | M014 | Primary+F4 (Ctrl+F4) | none | Primary+F4 | keep | Ctrl+F4 means "close document" in MDI apps, but Trizbort is single-document, so keep and watch for confusion. | |
| Select All | M015, K027 | Primary+A (canvas; no menu key) | Ctrl+A (window and canvas) | Primary+A | keep | Universal. | |
| Select None | M016, K002 | Esc | Esc (cancels drag first, then clears) | Esc | keep | Avalonia's cancel-drag-first is a good extra. | |
| Select rooms in the selected rooms' regions | K004 | Primary+Shift+A | Ctrl+Shift+A runs Select All (Shift ignored) | Primary+Shift+A = select regions | conflict-fix | Avalonia bug (K004). Reference wins; Shift must be checked. | |
| Select start room | K003 | 0 or NumPad0 (bare) | none (Ctrl+0 is Reset Zoom) | 0 or NumPad0 (bare, canvas only) | keep | Bare 0 and Primary+0 no longer overlap once modifiers are checked. | |
| Select room nearest viewport centre / open properties | K001, M029 | Enter | none | Enter | keep | Enter on one selected element opens its properties; with nothing selected it selects the room nearest the centre. | |
| Properties (alias) | K026, M029 | none (Enter, see above) | Alt+Enter | Alt+Enter | keep | Windows "properties" convention; already present, so retained as an alias. Option+Return on macOS. | |
| Copy | M024, K044 | Primary+C | Ctrl+C | Primary+C | keep | Universal. | |
| Cut | M027, K045 | none | Ctrl+X | Primary+X | keep | Universal. Already in Avalonia. | |
| Paste | M026, K043 | Primary+V | Ctrl+V | Primary+V | keep | Universal. | |
| Copy Color | M025 | Primary+Alt+C | none | Primary+Alt+C | keep | No clash. | |
| Delete | M028, K046 | Delete | Delete (window and canvas) | Delete | keep | Convention. | |
| Delete (Backspace alias) | K047 | none | Backspace (canvas) | Backspace (macOS also Cmd+Backspace) | new | macOS users expect Backspace; harmless on Windows. Canvas only, never in a text box. | |
| Add Room at cursor | M030, K008 | R | none (menu and toolbar only) | R | keep | Reference. | |
| Add Label at cursor | M075, K037 | L | none | L | keep | Reference. | |
| Rename room | M031 | F2 | none | F2 | keep | Matches the Explorer/Excel convention. | |
| Change Region | M032 | Shift+F2 | none | Shift+F2 | keep | Reference. | |
| Toggle Darkness | M033, K023 | K | none | K | keep | Reference. | |
| Force Darkness | M034, K060 | Primary+K | none | Primary+K | keep | Reference. | |
| Force Lighted | M035, K061 | Primary+Shift+K | none | Primary+Shift+K | keep | Reference. | |
| Start Room | M036 | Primary+F5 | none | Primary+F5 (macOS: Primary+Shift+F5) | keep | Cmd+F5 is VoiceOver on macOS. | |
| End Room | M037 | Shift+F5 | none | Shift+F5 | keep | Reference. | |
| Room shape: Square Corners | M038, K009 | Primary+H | none | Primary+H (macOS: Ctrl+H) | keep | Cmd+H is Hide on macOS. | |
| Room shape: Ellipse | M039, K035 | Menu Primary+E; canvas code wrongly sets Square Corners | none | Primary+E = Ellipse | conflict-fix | Menu label is authoritative; canvas case is a copy error (K035). | |
| Room shape: Rounded Edges | M040, K036 | Primary+R | none | Primary+R | keep | Reference. | |
| Room shape: Octagonal Edges | M041 | Primary+8 | none | Primary+8 | keep | Reference. | |
| Join Rooms | M042, K020 | J | none | J | keep | Reference. Needs exactly two rooms. | |
| Swap Objects | M043, K021 | W | none | W | keep | Reference. | |
| Swap Names | M044, K059 | Primary+W | none | Win/Linux: Primary+W. macOS: Ctrl+W | keep (flag) | Conventionally Primary+W is Close. See Questions. | |
| Swap Formats / Fills | M045, K057 | Shift+W | none | Shift+W | keep | Reference. | |
| Swap Regions | M046, K058 | Alt+W | none | Alt+W | keep | Reference. | |
| Connection: Plain | M047, K042 | P | none | P | keep | Reference. | |
| Connection: Dotted | M048, M073, K041 | T | none | T | keep | Reference. Toolbar button M073 shares the command. | |
| Connection: One Way Arrow / toggle flow | M049, M074, K005 | A | none | A | keep | Reference. Toolbar button M074 shares the command. | |
| Connection label: Up | M050, K006 | U | none | U | keep | Reference. | |
| Connection label: Down | M050, K038 | D | none | D | keep | Reference. Distinct from Primary+D (below). | |
| Connection label: In | M050, K039 | I | none | I | keep | Reference. | |
| Connection label: Out | M050, K040 | O | none | O | keep | Reference. | |
| Reverse Line | M051, K007 | V | none | V | keep | Reference. Distinct from Primary+V paste. | |
| Rotate connection (one way) | K019 | [ | none | [ | keep | Reference. Match by physical key so non-US layouts work. | |
| Rotate connection (other way) | K054 | ] | none | ] | keep | Reference. | |
| Rotate connection (Ctrl flag) | K055 | Primary+[ | none | Primary+[ | keep | Reference. | |
| Rotate connection (Ctrl flag, other way) | K056 | Primary+] | none | Primary+] | keep | Reference. | |
| Adjust all connection ports of room | K018 | ; or \ | none | ; or \ | keep | Reference. | |
| Cycle port-adjust detail | K053 | Primary+; or Primary+\ | none | Primary+; or Primary+\ | keep | Reference; fix the dead Shift branch in the ticket (matrix K open question 4). | |
| Delete all connections of room | K017 | Primary+D | none | Alt+Delete | modernise | Lee (Q5): Primary+D is Duplicate by convention. | approved |
| Duplicate selection | (new) | none | none | Primary+D | new | Lee (Q5): reserved for a future Duplicate command. | approved |
| Backup project | K016 | Primary+B | none | Primary+B | keep | Reference. | |
| Quick Find | K022 | Primary+F | none | Primary+F | keep | Same as convention. | |
| Make next selected element active | K065 | F3 | none | F3 | keep | Matches Find Next convention. | |
| Make previous selected element active | K066 | Shift+F3 | none | Shift+F3 | keep | Matches Find Previous convention. | |
| Move selection / pan with no selection | K012 | Arrows: move selection by grid or 2 units; with no selection pan by 1/10 viewport (1/5 with Shift) | Arrows move selection only (nothing happens without a selection) | Same as reference | keep | Reference behaviour; the no-selection pan is the missing half. | |
| Select in direction (room, else add room) | K013 | Primary+Arrows | Nudges selection (wrong) | Primary+Arrows | conflict-fix | Avalonia currently reuses the plain move path. | |
| Select connection/room in direction | K014 | Shift+Arrows | Moves selection by 2x grid (wrong) | Shift+Arrows | conflict-fix | Reference and the usual "Shift extends selection" idea agree. | |
| Select room NW, SW, NE, SE | K011, K032, K033, K034 | Shift+Home, Shift+End, Shift+PageUp, Shift+PageDown | none | Same as reference | keep | Four commands in one row. Mac laptops need Fn; NumPad rows give the same result. | |
| Resize selected rooms | K015 | Primary+Alt+Arrows | Ctrl+Alt+Arrows | Primary+Alt+Arrows | keep | Already matches. | |
| Keypad: select room in direction | K049 | NumPad1-9 (not 5) | none | NumPad1-9 (not 5) | keep | Reference. | |
| Keypad: select connection/room in direction | K050 | Shift+NumPad1-9 | none | Shift+NumPad1-9 | keep | Reference. | |
| Keypad: select, else add/connect room | K051 | Ctrl+NumPad1-9 (modifier is a setting) | none | Primary+NumPad1-9 | keep | See Question about the setting in the macOS notes. | |
| Keypad: add unexplored connection | K052 | Alt+NumPad1-9 (modifier is a setting) | none | Alt+NumPad1-9 | keep | Reference. | |
| Zoom In | M056, K028 | + / NumPad+ (any modifier) | Ctrl+= and Ctrl+Add (window), + (canvas) | + / NumPad+ and Primary+= / Primary++ | modernise | Primary+= is the browser/editor convention. Bare + stays. | |
| Zoom Out | M056, K029 | - / NumPad- (any modifier) | Ctrl+- and Ctrl+Subtract (window), - (canvas) | - / NumPad- and Primary+- | modernise | As above. | |
| Reset Zoom to 100% | M057, M060, K030 | none found in reference | Ctrl+0 | Primary+0 | modernise | Browser/editor convention; already in Avalonia. | |
| Zoom to Entire Map | M059, K031 | Primary+Home | none | Primary+Home (macOS: also Cmd+Up) | keep | Reference. Mac laptops lack Home. | |
| Reset view origin | M060, K010 | Home | none | Home | keep | Reference "Reset". Not the same as Reset Zoom. | |
| Pan with right-drag | K025 | Shift+right-drag or middle-drag | Plain right-drag, middle-drag | Plain right-drag, middle-drag | modernise | The model change Lee cited. | |
| Pan with Space + drag | K048 | none | Space held + left drag | Space held + left drag | new | Design-tool convention; keep. | |
| Automap run to completion | K068 | F5 (while automapping) | none | F5 (while automapping) | keep | Reference. | |
| Automap single step | K071 | F11 (any modifier, while automapping) | none | F11 (macOS: menu or Primary+Shift+F11) | keep | F11 is Show Desktop on macOS. | |
| Map Settings | M065 | Primary+, | none | Primary+Shift+, | modernise | Frees Primary+, for Application Settings (convention). See Questions. | |
| Application Settings | M066 | Alt+Shift+S | none | Primary+, | modernise | Primary+, is Settings in VS Code, browsers and macOS. See Questions. | |
| Online Help | M071, K024 | F1 | F1 opens About | F1 (macOS: Cmd+? / Primary+Shift+/) | conflict-fix | F1 is help everywhere; About must give it up. | |
| About | M072 | none | F1 | none (Help menu; macOS app menu) | conflict-fix | Frees F1. | |
| Debug: toggle FPS | K062 | Ctrl+F1 | none | Ctrl+F1 (only when started with `--debug`) | keep | Lee (Q6): keep the reference keys for advanced users; enable debug features with a startup flag. | approved |
| Debug: toggle mouse coordinates | K063 | Shift+F1 | none | Shift+F1 (only when started with `--debug`) | keep | Lee (Q6): keep the reference keys for advanced users; enable debug features with a startup flag. | approved |
| Debug: toggle element rendering | K064 | Ctrl+F2 | none | Ctrl+F2 (only when started with `--debug`) | keep | Lee (Q6): keep the reference keys for advanced users; enable debug features with a startup flag. | approved |
| Debug: toggle line rendering | K067 | Ctrl+F3 | none | Ctrl+F3 (only when started with `--debug`) | keep | Lee (Q6): keep the reference keys for advanced users; enable debug features with a startup flag. | approved |
| Debug: toggle grid polyline | K069 | Ctrl+F5 (unreachable, Start Room wins) | none | none; menu item under `--debug` only | conflict-fix | Reference clash with Start Room. | approved |

Commands that have a matrix row but **no gesture in the reference or the proposal** (menu or toolbar only): Export PDF (M007), Export Image (M008), Recent Maps (M012), Select Special submenu (M017 to M023), Validation toggles (M052 to M055), Zoom 50/100/200 presets (M057), Zoom -1%/+1% (M058), Mini Map toggle (M061), Show Grid (M062), Automap Start/Stop (M063, M064), Restore Default Map Settings (M067), Map Statistics and Export (M068, M069), Themes, Import, Export, Infer (M070, M076 to M078), Quest to clipboard with header (M010).

## 3. Conflicts resolved

| Clash | Resolution |
|---|---|
| Save As vs Smart Save on Primary+Shift+S | Save As takes Primary+Shift+S (convention, already in Avalonia). Smart Save moves to the reference Save As chord, Primary+Alt+S. |
| Primary+Shift+Z: Redo vs Export ZIL (file) | Redo keeps Primary+Shift+Z (Lee's call; it is also the only Redo gesture on macOS). Export ZIL (file) moves to Primary+Shift+I. The clipboard ZIL export on Primary+Alt+Z is unaffected. |
| Primary+W: conventional Close vs reference Swap Names | Win/Linux keep Swap Names on Primary+W (keeps the whole W family together, no tabs to close, Exit has Alt+F4/Primary+Q). macOS: Cmd+W stays Close Window; Swap Names becomes Ctrl+W. Flagged in Questions. |
| Primary+E: Ellipse (menu) vs canvas code that sets Square Corners (K035) | Ellipse. The menu label and accelerator are authoritative; the canvas case is a copy error. |
| Primary+0: Reset Zoom vs bare 0 select-start-room | No clash once modifiers are checked: bare 0/NumPad0 selects the start room; Primary+0 resets zoom to 100%. Home keeps the reference "reset view origin". |
| F1: Online Help vs About | F1 is Online Help. About loses its gesture. |
| Primary+Shift+A: Select Regions vs Avalonia Select All | The canvas must check Shift. Primary+Shift+A selects regions, Primary+A selects all. |
| Bare letters vs Primary+letter (V and Primary+V, D and Primary+D, A and Primary+A, R and Primary+R, W and Primary+W, K and Primary+K) | Bare-letter handlers require no Primary or Alt (rule in section 1). The reference ignores modifiers here; the port does not. |
| Primary+Arrows and Shift+Arrows: reference selects in a direction, Avalonia nudges | Reference behaviour restored. Only plain arrows move or pan. Primary+Alt+Arrows resizes. |
| Primary+F5 Start Room vs debug "grid polyline" Ctrl+F5 | Start Room wins (it already wins in the reference). Debug toggle gets no key. |
| Primary+, Map Settings (reference) vs Settings convention | Primary+, becomes Application Settings; Map Settings moves to Primary+Shift+,. Flagged in Questions. |
| Primary+D delete-all-connections vs bare D label Down | Resolved by Q5: delete-all-connections moves to Alt+Delete; Primary+D is reserved for Duplicate. |
| Delete and Backspace | Both delete on the canvas only. Window-level binding on Delete duplicates the canvas case; remove one when implementing (matrix K open question 1). |
| Primary+= / Primary++ / bare + and - | All zoom. Both `OemPlus` and `Add` keys are bound so no Shift is needed on a US keyboard. |

## 4. macOS notes

macOS code-level support is in scope (L5); packaging is not. Rules: `Primary` is Cmd via `PlatformHotkeyConfiguration`; the native app menu supplies About, Settings (Cmd+,), Hide (Cmd+H), Hide Others, Quit (Cmd+Q) and Close Window (Cmd+W); F-keys need Fn on laptops, so every F-key command must also be in a menu.

| Gesture | Clash on macOS | Proposed macOS variant |
|---|---|---|
| Primary+H Square Corners | Cmd+H hides the app | Ctrl+H |
| Primary+Alt+H Hugo to clipboard | Cmd+Option+H hides other apps | Primary+Alt+Shift+H |
| Primary+Shift+H Hugo file export | Not an app-level clash (Finder Home) | Unchanged. Listed with its sibling so the two Hugo exports stay near each other if Lee prefers; low risk to leave it. |
| Primary+Shift+Q Quest file export | Cmd+Shift+Q logs out | Primary+Alt+Shift+Q |
| Primary+W Swap Names | Cmd+W closes the window | Ctrl+W; Cmd+W = Close Window |
| Primary+, Application Settings | None; this is the macOS Settings gesture | Native app menu Settings item, Cmd+, |
| Primary+Shift+, Map Settings | None known | Unchanged |
| Exit | Cmd+Q quits | Cmd+Q from the native app menu |
| Primary+M | Cmd+M minimises; Trizbort uses no Primary+M | No action needed |
| Primary+F5 Start Room | Cmd+F5 toggles VoiceOver | Primary+Shift+F5 |
| Primary+Alt+F5 (not used) | Cmd+Option+F5 opens Accessibility shortcuts | Avoided |
| F11 automap step | F11 is Show Desktop | Menu item, or Primary+Shift+F11 |
| Ctrl+F1, Ctrl+F2, Ctrl+F3 debug toggles | Ctrl+F2 and Ctrl+F3 focus the menu bar and Dock | Developer menu only |
| F1 Online Help | F1 is brightness on laptops | Cmd+? (Primary+Shift+/) in the Help menu, F1 with Fn |
| Primary+Shift+Z Redo | Matches the macOS convention | Cmd+Shift+Z only (no Cmd+Y) |
| Primary+Shift+6 Inform 6 export | Cmd+Shift+6 is Touch Bar screenshot on older Macs | Keep; revisit only if Lee cares about those models |
| Home, End, PageUp, PageDown (Home, Shift+Home and so on) | Not on laptop keyboards (Fn+arrows) | Primary+Home also on Cmd+Up; others via NumPad or Fn |
| Alt+Shift+S (old App Settings) | Option+Shift+S types a character | Dropped in the proposal |
| Backspace | The Mac "Delete" key | Backspace and Cmd+Backspace delete |
| Alt+Enter Properties | Option+Return works | Unchanged |
| Primary+NumPad keypad modifier | Ctrl+NumPad is a settings value in the reference (SET:465-468) | Primary+NumPad, i.e. Cmd; add the setting only if Lee wants it |

## 5. Lee's decisions (2026-10-30)

D4 is **approved** with these answers. Rows Lee did not comment on are accepted as proposed.

1. Primary+W: keep Swap Names on Windows/Linux; Ctrl+W on macOS (Cmd+W stays Close Window).
2. Settings: Primary+, = Application Settings; Primary+Shift+, = Map Settings.
3. Redo: Primary+Y (conventional) **and** Primary+Shift+Z (also a common convention, the macOS standard); so Export ZIL (file) moves to Primary+Shift+I.
4. Smart Save: Primary+Alt+S is OK.
5. Primary+D is Duplicate (reserved for a future command); Delete all connections of room moves to Alt+Delete.
6. Debug toggles keep their reference keys but only work when the app is started with a `--debug` flag (no menu otherwise).

## 6. Questions for Lee (answered — see section 5)

1. **Primary+W.** Keep the reference (Swap Names) on Windows/Linux and use Ctrl+W on macOS, or give Primary+W the conventional Close and move Swap Names to a free chord such as Primary+Shift+W?
- Yes

2. **Settings gestures.** Make Primary+, open Application Settings (VS Code, browsers, macOS) and move Map Settings to Primary+Shift+,, which drops the reference Ctrl+, and Alt+Shift+S? Or keep Ctrl+, for Map Settings and Alt+Shift+S for App Settings on Win/Linux only?

- Yes

3. **Redo vs Export ZIL.** Move the ZIL file export from Primary+Shift+Z to Primary+Shift+I so Redo can use Primary+Shift+Z. Is that acceptable, or should Redo be Primary+Y only on Windows/Linux?

- Primary+Y is conventional for redo so let's use that.
- Is Primary+Shift+Z convention for redo also? If so then change it to avoid the conflict.

4. **Smart Save.** Is Primary+Alt+S (the old Save As chord) acceptable, given Ctrl+Alt is AltGr on some layouts?

- OK

5. **Primary+D and duplicate.** Keep Primary+D = delete all connections of the room (reference), or reserve Primary+D for a future Duplicate and give delete-all-connections a different chord? Undo makes the destructive command safer.

- Primary + D feels like duplicate to me. Could alt+delete be an option?

6. **Debug toggles** (K062 to K064, K067, K069). Port them behind a hidden Developer menu, port them only in Debug builds, or won't-port?

- Anyone that is advanced enough to be using these probably wouldn't want them to change. We could add a startup flag to enable debug features.