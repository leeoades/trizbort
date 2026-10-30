# Avalonia manual smoke test (T2.4)

About 30 minutes, 64 steps, run on Windows and again on Linux against the Avalonia app.
Launch from the repo root: `dotnet run --project src/Trizbort.Avalonia`

Sources: Partial, Broken, Different and Implemented rows in `feature-matrix.md`, and `issues.md`.
Fixtures are in `tests/manual/` and `samples/`.

How to read each step:
- **Expected** says what the Avalonia app should do. Where the matrix marks a row Partial or Broken, the line says what the reference (legacy WinForms app) does and carries `(known gap: ID)`. Those steps are expected to fail today; see "How to report".
- `Different` rows say how the Avalonia behaviour differs from the reference, `(differs: ID)`.

| | Windows | Linux |
|---|---|---|
| OS version | | |
| Date | | |
| Commit (`git rev-parse --short HEAD`) | | |

---

## 1. Start-up & File (9 steps)

Fixture: `tests/manual/basics-1570.trizbort`

1. Launch the app with `dotnet run --project src/Trizbort.Avalonia`.
   Expected: window opens on an empty map; title starts with `Trizbort`.
   `[ ] pass  [ ] fail  notes:`
2. Press Ctrl+N.
   Expected: a new empty map opens with no prompt when nothing is unsaved.
   `[ ] pass  [ ] fail  notes:`
3. Press Ctrl+O and open `tests/manual/basics-1570.trizbort`.
   Expected: map loads. Title reads `Trizbort - basics-1570`. Reference title is `<name> - Trizbort - <version>`, so the version suffix is missing (known gap: A004).
   `[ ] pass  [ ] fail  notes:`
4. Drag one room to a new position.
   Expected: title gains a `*` dirty marker.
   `[ ] pass  [ ] fail  notes:`
5. Press Ctrl+S.
   Expected: saves without a dialog; the `*` disappears.
   `[ ] pass  [ ] fail  notes:`
6. Press Ctrl+Shift+S.
   Expected: Save Map As dialog opens. Differs from reference: reference uses Ctrl+Alt+S for Save As and Ctrl+Shift+S for Smart Save (differs: M005).
   `[ ] pass  [ ] fail  notes:`
7. Move a room again (map now dirty), then press Ctrl+O.
   Expected: Yes / No / Cancel prompt. Choose Cancel: the map and your move are kept. Prompt text says "current map" and the title is "Trizbort" (wording differs, not a gap).
   `[ ] pass  [ ] fail  notes:`
8. Edit and save `basics-1570.trizbort` in another program (or a text editor), then return to the app.
   Expected: reference shows "map modified by another program, reload?". Avalonia does nothing (known gap: A005 / I-003).
   `[ ] pass  [ ] fail  notes:`
9. Tick "Load Last Opened Project on Startup" in App Settings, OK. Open a map, close the app, relaunch.
   Expected: reference reopens the last map. Avalonia starts empty (known gap: A014 / I-001).
   `[ ] pass  [ ] fail  notes:`

## 2. Edit & selection (6 steps)

Fixture: `tests/manual/manual-edit-select-special.trizbort`

10. Press Ctrl+A.
    Expected: all elements are selected.
    `[ ] pass  [ ] fail  notes:`
11. Click an empty part of the canvas.
    Expected: selection clears.
    `[ ] pass  [ ] fail  notes:`
12. Select one room, press Ctrl+C, then Ctrl+V.
    Expected: reference pastes a copy of the room. Avalonia paste format and ID handling are unverified (known gap: K043).
    `[ ] pass  [ ] fail  notes:`
13. Copy some plain text in another app, click the canvas, and choose Edit > Paste.
    Expected: reference disables Paste unless Trizbort's own clipboard holds data. Avalonia reads the system clipboard, so the text is handled differently (differs: M026). Nothing should crash.
    `[ ] pass  [ ] fail  notes:`
14. Select one room and press Delete.
    Expected: reference removes the selected room and its connections. Avalonia delete logic is not compared against the reference (known gap: K046).
    `[ ] pass  [ ] fail  notes:`
15. Open the Edit menu and look for a Rooms item; press R.
    Expected: reference has an R shortcut for it. Avalonia has the menu item but no R key (known gap: M030).
    `[ ] pass  [ ] fail  notes:`

## 3. Canvas mouse (14 steps)

Fixture: `tests/manual/connections.trizbort`

16. Click one room.
    Expected: room is selected and shows resize handles.
    `[ ] pass  [ ] fail  notes:`
17. Ctrl-click a second room, then Shift-click a third.
    Expected: each click adds or removes that room from the selection.
    `[ ] pass  [ ] fail  notes:`
18. Drag a marquee across three rooms on empty canvas.
    Expected: all three are selected. Ctrl adds only in reference; Avalonia toggles for both Ctrl and Shift (differs: G005).
    `[ ] pass  [ ] fail  notes:`
19. Drag one room.
    Expected: room moves and snaps to the grid.
    `[ ] pass  [ ] fail  notes:`
20. Drag from a room's port onto empty canvas.
    Expected: a connection is drawn from the port. Reference uses the toolbar's line style, flow and label for the new connection. Avalonia ignores those defaults (known gap: G008).
    `[ ] pass  [ ] fail  notes:`
21. Drag the movable end of an existing connection.
    Expected: end moves and reattaches to the new position.
    `[ ] pass  [ ] fail  notes:`
22. Single-click (no drag) on a curved connection's ghost handle.
    Expected: reference adds no waypoint. Avalonia can add one (known gap: G011).
    `[ ] pass  [ ] fail  notes:`
23. Drag a curved connection's ghost handle to a new position.
    Expected: a waypoint is added. Whether a waypoint can be deleted in Avalonia is unverified (known gap: G012).
    `[ ] pass  [ ] fail  notes:`
24. Double-click a room.
    Expected: Room Properties dialog opens.
    `[ ] pass  [ ] fail  notes:`
25. Right-click empty canvas.
    Expected: menu shows Add Room, Map Settings and App Settings. Properties... is absent with no active element. Reference shows Properties... only for Room, Connection and label; Avalonia shows it for any active element (known gap: C015).
    `[ ] pass  [ ] fail  notes:`
26. Right-drag the canvas.
    Expected: canvas pans. Intentional change: reference needs Shift for this (differs: G015).
    `[ ] pass  [ ] fail  notes:`
27. Scroll the mouse wheel over the canvas.
    Expected: canvas zooms. Reference also has Ctrl micro-zoom and honours Invert Mouse Wheel; Avalonia does not (known gap: G017 / I-006).
    `[ ] pass  [ ] fail  notes:`
28. Hover a room with canvas tooltips on.
    Expected: tooltip appears with the room text. Reference uses coloured title, body and footer; Avalonia loses that styling (known gap: G018).
    `[ ] pass  [ ] fail  notes:`
29. Move the pointer over the canvas and watch the status-bar X/Y readout.
    Expected: readout follows the pointer. Avalonia stays at `X: 0, Y: 0` (known gap: A020 / I-002).
    `[ ] pass  [ ] fail  notes:`

## 4. Canvas keyboard (10 steps)

Fixture: `tests/manual/shift-arrow-basic-directions.trizbort`

30. Select one room and press an arrow key. Then click empty canvas and press an arrow key again.
    Expected: with selection the room moves one grid step. With no selection the canvas should pan. Avalonia does not pan (known gap: K012).
    `[ ] pass  [ ] fail  notes:`
31. Select one room and press Ctrl+Arrow.
    Expected: Avalonia nudges the selection one step (differs: K013).
    `[ ] pass  [ ] fail  notes:`
32. Select one room and press Shift+Arrow.
    Expected: Avalonia moves the selection by twice the grid. Reference navigates the selection (differs: K014).
    `[ ] pass  [ ] fail  notes:`
33. Start dragging a room, then press Esc before releasing.
    Expected: the drag is cancelled and the room returns to its start position.
    `[ ] pass  [ ] fail  notes:`
34. Press `+` then `-` (and NumPad+ / NumPad-).
    Expected: canvas zooms in, then out.
    `[ ] pass  [ ] fail  notes:`
35. Press Ctrl+0.
    Expected: zoom resets to 100%. Reference reset location not found (differs: K030).
    `[ ] pass  [ ] fail  notes:`
36. Press Ctrl+Shift+A.
    Expected: reference selects the rooms in the selected rooms' regions. Avalonia selects everything (known gap: K004 / I-004).
    `[ ] pass  [ ] fail  notes:`
37. Select one room and press Alt+Enter, then press Enter alone.
    Expected: Alt+Enter opens Properties. Plain Enter does nothing. Reference uses Enter for Properties at canvas level (known gap: K026 / M029).
    `[ ] pass  [ ] fail  notes:`
38. Press F1.
    Expected: About dialog opens. Reference F1 opens help (differs: K024).
    `[ ] pass  [ ] fail  notes:`
39. Press Ctrl+, (comma).
    Expected: nothing happens in Avalonia. Reference binds Ctrl+, to a Tools item (known gap: M065).
    `[ ] pass  [ ] fail  notes:`

## 5. Dialogs (9 steps)

Fixture: `tests/manual/region-accent-one.trizbort` (use it for all steps; any map works for the others).

40. Select a room and press Alt+Enter to open Room Properties.
    Expected: reference has Position, Down and Right fields. Avalonia does not (known gap: D001).
    `[ ] pass  [ ] fail  notes:`
41. Open Connection Properties on a connection.
    Expected: reference Description is multiline. Avalonia's is single-line, and Colour is buried (known gap: D002).
    `[ ] pass  [ ] fail  notes:`
42. Open Label Properties on a label.
    Expected: reference enables Shape with Outline or Background. Avalonia enables it only with Outline (known gap: D003).
    `[ ] pass  [ ] fail  notes:`
43. Open Map Settings and find handle size, snap-to-element distance and document margins.
    Expected: reference has all three. Avalonia has no UI for them (known gap: D004).
    `[ ] pass  [ ] fail  notes:`
44. Map Settings > Regions: rename a region that rooms use, then OK.
    Expected: reference moves those rooms to the new name. Avalonia leaves them with the old name and loses their colour (known gap: I-007, high severity).
    `[ ] pass  [ ] fail  notes:`
45. App Settings: untick "Show Canvas Tooltips", OK, hover a room.
    Expected: no tooltip. Avalonia still shows tooltips and the setting reverts (known gap: I-005).
    `[ ] pass  [ ] fail  notes:`
46. App Settings: tick "Show Full Path in Title Bar", OK.
    Expected: reference shows the full path in the title. Avalonia title does not change (known gap: I-006 / A004).
    `[ ] pass  [ ] fail  notes:`
47. Open Help > About.
    Expected: reference shows version, credits, licence and repo links. Avalonia shows only a Genstein line (known gap: D015).
    `[ ] pass  [ ] fail  notes:`
48. Tools > Themes: apply a built-in theme.
    Expected: Yes / No / Cancel prompt appears. Avalonia has no "Infer default room style" option (known gap: A025).
    `[ ] pass  [ ] fail  notes:`

## 6. Export (9 steps)

Fixture: `samples/Zork_770614.trizbort`

49. Open File > Export.
    Expected: one item per language, eight in total (Alan, Adventuron and the rest).
    `[ ] pass  [ ] fail  notes:`
50. Export to Inform 7 and enter a name ending in `.inform`.
    Expected: reference creates a `<name>.inform` folder. Avalonia behaviour with the file picker is untested (known gap: E010).
    `[ ] pass  [ ] fail  notes:`
51. Open any export save dialog and look at the default file name.
    Expected: Avalonia suggests the project Title, then the map file name, then "map". No extension is added (differs: E005).
    `[ ] pass  [ ] fail  notes:`
52. Export code to a file that already exists.
    Expected: overwrite confirmation appears. Behaviour depends on the platform picker (known gap: E008; check Linux explicitly).
    `[ ] pass  [ ] fail  notes:`
53. Export code to a read-only folder.
    Expected: "Export Error" message box shows the failure.
    `[ ] pass  [ ] fail  notes:`
54. File > Export Image and open the file-type filter.
    Expected: reference offers PNG, JPEG, BMP and EMF. Avalonia offers PNG only (known gap: E016).
    `[ ] pass  [ ] fail  notes:`
55. Save an image with the name `test.jpg`, then open it in an image viewer.
    Expected: reference writes JPEG. Avalonia writes PNG bytes under the `.jpg` name (known gap: E016).
    `[ ] pass  [ ] fail  notes:`
56. Zoom the canvas to about 200%, then export an image.
    Expected: reference uses the current zoom unless the SaveAt100 setting is set. Avalonia always exports at 100% (known gap: E018).
    `[ ] pass  [ ] fail  notes:`
57. Export an image, then open Export Image again.
    Expected: reference starts in the last image-export folder. Avalonia does not use that folder as the start location (known gap: E024).
    `[ ] pass  [ ] fail  notes:`

## 7. Automap (7 steps)

Fixture: `tests/manual/map-stats-general.trizbort`, plus a plain `.txt` transcript you write for the test.

58. Choose Toggle Automap from the menu.
    Expected: automap settings dialog opens. Start is blocked until a transcript file name is set.
    `[ ] pass  [ ] fail  notes:`
59. Set a transcript name, Start, and watch the status bar.
    Expected: status bar shows the running state and a green "Automap Running" indicator. Automap bar appears.
    `[ ] pass  [ ] fail  notes:`
60. Click Stop on the Automap bar.
    Expected: automap stops and the bar hides.
    `[ ] pass  [ ] fail  notes:`
61. While automap is running, open the Automap menu item and read its label.
    Expected: reference label changes to "Stop Automap". Avalonia keeps one static label (differs: E038).
    `[ ] pass  [ ] fail  notes:`
62. Run a transcript that names a room ambiguously.
    Expected: reference asks which room was meant. Avalonia silently picks the first candidate (known gap: I-008).
    `[ ] pass  [ ] fail  notes:`
63. Run a transcript where two rooms claim the same exit.
    Expected: reference asks what to do. Avalonia silently keeps room 1 (known gap: I-008).
    `[ ] pass  [ ] fail  notes:`
64. With automap running, close the window.
    Expected: automap stops and the app closes without a hang.
    `[ ] pass  [ ] fail  notes:`

---

## How to report

1. Note the OS, date and commit in the table at the top before you start.
2. Mark each step pass or fail. Put the actual result in `notes:`.
3. A fail on a step that carries `(known gap: ID)` is already logged. Add a note to the step only; do not log it again.
4. Any other fail, or any step where the app is worse than its Expected line, gets a new entry in `issues.md`, using the format in `../avalonia-parity-review.md` §9 and the existing entries:

```
## I-009 <short imperative or noun title>
- Reported: <date> by <name> (smoke test, <Windows|Linux>)   OS: <Windows|Linux>   Category: behaviour
- Steps: 1. ... 2. ...
- Expected: ...   Actual: ...
- Cause: not yet known (T2.3)
- Severity: low|medium|high     Flags: —
- Ticket: —
```

5. Use the next free I-NNN from the `<!-- Next ID -->` comment at the bottom of `issues.md`, and update that comment.
6. If a step fails on one OS only, say so in the Steps or Actual line.
