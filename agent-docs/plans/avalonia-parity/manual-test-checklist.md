# Manual test checklist — Avalonia app (2026-10-31)

Things the headless tests cannot prove. Run on Windows and on the Linux laptop. Note OS, input device and what went
wrong next to any item that fails; failures go into `issues.md`.

Start the app: `dotnet run --project src/Trizbort.Avalonia` (add `--debug` for the View > Debug menu).

## 1. Trackpad, mouse and touch (issue #21)
- [ ] Two-finger scroll pans in all four directions; speed feels right (too slow / too fast?).
- [ ] Pinch zooms smoothly about the fingers; no jumps from fully out to fully in.
- [ ] Ctrl + two-finger scroll also zooms smoothly.
- [ ] Mouse wheel still zooms one step per notch; Ctrl + wheel does a small step.
- [ ] App Settings > "Scroll wheel / trackpad scroll": *Always pan* and *Always zoom* both work; *Automatic* restored.
- [ ] Space + left-drag pans; middle-drag pans; right-drag pans; arrow keys pan when nothing is selected.
- [ ] Touchscreen: one finger selects and drags rooms; two fingers pan and pinch-zoom and never select or move anything.
- [ ] Home resets the view; Ctrl+Home zooms to the whole map.

## 2. Cursors
- [ ] Over a room's port (the little dots): cross-with-pencil "draw line" cursor.
- [ ] Over the end of a selected connection: four-way "move line" cursor.
- [ ] Over a waypoint, and over a selected room: four-way arrow. Over resize handles: the matching resize arrows.
- [ ] Ctrl held over a reference room: hand.
- [ ] Change the canvas to a dark colour (Map Settings > Colours): line cursors switch to the outlined versions.

## 3. Editing on the map
- [ ] R adds a room at the pointer and opens its properties; L adds a label and opens its properties.
- [ ] Draw a connection from port to port; drag a connection end to another room; drag it off and press Esc (goes back).
- [ ] Drag a room and press Esc mid-drag: it goes back, and nothing appears in Undo.
- [ ] Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z: undo and redo every kind of edit (move, resize, add, delete, dialogs, automap). The Edit menu names the step.
- [ ] Window title shows `*` when there are unsaved changes and clears when you undo back to the saved state.
- [ ] Marquee: plain replaces the selection, Ctrl adds, Shift toggles.
- [ ] Double-click empty space adds a room only when the App Setting is on.
- [ ] Single-letter keys on the map: P plain, T dotted, A one-way, V reverse, [ and ] rotate, J join rooms, F2 rename, Enter properties.
- [ ] Ctrl+arrow / keypad adds or connects a room in that direction; Shift+arrow selects in that direction; F3 cycles.
- [ ] Copy, cut, paste within the app, and paste a map fragment copied from the original app.
- [ ] Right-click menus differ for room, connection, label and empty space; Set Region submenu ticks the current region.
- [ ] Tooltips: room tooltip coloured; hides while dragging; invalid rooms show a red tooltip listing the problems.
- [ ] Edit > Validation: turn on "unique names", make two rooms with the same name: both get a red cross.

## 4. Dialogs (look and feel)
- [ ] Room, Connection, Label, Map Settings, App Settings, Automap Settings, Map Theme, About: nothing clipped, access keys (Alt+letter) work, Tab order sensible, OK on Enter, Cancel on Esc.
- [ ] Room dialog: shape preview, remembers last tab, invalid name shows an error, Objects box Tab/Shift+Tab/Enter.
- [ ] Connection dialog: preset radios follow what you type in Start/End.
- [ ] Map Settings: region rename/delete moves rooms correctly; colours show friendly names; fonts pick style.
- [ ] Each dialog's OK is one undo step.

## 5. Files
- [ ] Open, Save, Save As, Recent Maps (max 4), reopen last map on start (setting on).
- [ ] Edit the open map in another program and save: the app asks to reload.
- [ ] Open Map from Web (Ctrl+Shift+O) with a real URL to a .trizbort file; Save then goes to Save As.
- [ ] Close with unsaved changes: prompt; settings and window size/position remembered next start.
- [ ] Double-click a .trizbort file in the file manager (after registering with `packaging/`): opens in the app.

## 6. Export
- [ ] File > Export > Image: PNG, JPEG and BMP by extension; scale options (100%, current zoom, custom); selection only.
- [ ] File > Export > PDF: opens in a PDF viewer, text selectable, rooms with descriptions show orange notes.
- [ ] Each code export (Inform 7, Inform 6, TADS, Hugo, ZIL, Quest, Alan, Adventuron) to file and to clipboard.
- [ ] Smart Save (Ctrl+Alt+S) writes the formats ticked in App Settings next to the map.
- [ ] Command line: `--inform7 out` etc., `--quicksave`, `--smartsave`, `--exit` (check the exit code).

## 7. Automap
- [ ] Start automap on a transcript; rooms appear and the current room scrolls into view.
- [ ] Ambiguous room names ask which room; two rooms in the same direction ask what to do.
- [ ] Stop, restart, F5 runs to completion; an unreadable transcript shows one clear error.

## 8. Platform
- [ ] Linux: menus, fonts, dialogs and file pickers look right; Ctrl shortcuts work.
- [ ] Windows: same, plus Alt+F4 exits.
- [ ] Light and dark OS theme: everything readable.
