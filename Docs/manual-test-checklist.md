# Manual test checklist — Avalonia app (2026-10-31)

Things the headless tests cannot prove. Run on Windows and on the Linux laptop. Note OS, input device and what went
wrong next to any item that fails; failures go into `issues.md`.

Start the app: `dotnet run --project src/Trizbort.Avalonia` (add `--debug` for the View > Debug menu).

## 1. Trackpad, mouse and touch (issue #21)
- [x] Two-finger scroll pans in all four directions; speed feels right (too slow / too fast?).
- [ ] Pinch zooms smoothly about the fingers; no jumps from fully out to fully in.
    - The pinch gesture has no effect on the map. I've tested pinch to zoom functionality in a browser test app and it is supported on this laptop.
- [x] Ctrl + two-finger scroll also zooms smoothly.
- [ ] Mouse wheel still zooms one step per notch; Ctrl + wheel does a small step.
    - I don't have a mouse on this laptop
- [x] App Settings > "Scroll wheel / trackpad scroll": *Always pan* and *Always zoom* both work; *Automatic* restored.
- [ ] Space + left-drag pans; middle-drag pans; right-drag pans; arrow keys pan when nothing is selected.
    - On my laptop, holding space disables the touchpad. Need to check this with a mouse.
    - arrow keys work!
- [x] Touchscreen: one finger selects and drags rooms; two fingers pan and pinch-zoom and never select or move anything.
- [x] Home resets the view; Ctrl+Home zooms to the whole map.

## 2. Cursors
- [x] Over a room's port (the little dots): cross-with-pencil "draw line" cursor.
- [x] Over the end of a selected connection: four-way "move line" cursor.
- [x] Over a waypoint, and over a selected room: four-way arrow. Over resize handles: the matching resize arrows.
- [ ] Ctrl held over a reference room: hand.
- [x] Change the canvas to a dark colour (Map Settings > Colours): line cursors switch to the outlined versions.

## 3. Editing on the map
- [x] R adds a room at the pointer and opens its properties; L adds a label and opens its properties.
    - Did the original app open the properties straight away? If not, let's leave it to the user to double click on it.
- [ ] Draw a connection from port to port; drag a connection end to another room; drag it off and press Esc (goes back).
    - It seems to require esc pressing twice. The first press doesn't do anything.
- [ ] Drag a room and press Esc mid-drag: it goes back, and nothing appears in Undo.
    - It seems to require esc pressing twice. The first press doesn't do anything.
- [x] Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z: undo and redo every kind of edit (move, resize, add, delete, dialogs, automap). The Edit menu names the step.
- [x] Window title shows `*` when there are unsaved changes and clears when you undo back to the saved state.
- [ ] Marquee: plain replaces the selection, Ctrl adds, Shift toggles.
    - Please can you describe this feature to me in more detail? I'm not sure what a Marquee is.
- [x] Double-click empty space adds a room only when the App Setting is on.
- [x] Single-letter keys on the map: P plain, T dotted, A one-way, V reverse, [ and ] rotate, J join rooms, F2 rename, Enter properties.
    - Everything works however a highlighted dotted line still appears as solid so it isn't obvious that it has changed.
- [ ] Ctrl+arrow / keypad adds or connects a room in that direction; Shift+arrow selects in that direction; F3 cycles.
    - Please could you explain the F3 cycles function?
    - the other controls worked (although I don't have a keypad to test that ). It did show up an issue. Ctrl+Up Arrow created a room and opened the room dialog immediately which was somewhat confusing. Also, pressing enter (which I assumed would OK the dialog) opened another dialog. Pressing esc did not close the dialog.
        - Actually, the dialog behaviour might be weird because I'm using Cosmic desktop with a layout manager? I'd still like it to work though.
    - All automatic joins created this way seemed to be labled up/down.
- [ ] Copy, cut, paste within the app, and paste a map fragment copied from the original app.
    - I can't test this on a Linux laptop
- [x] Right-click menus differ for room, connection, label and empty space; Set Region submenu ticks the current region.
    - The cursor over the right click menus is weird. Should be a standard arrow.
- [x] Tooltips: room tooltip coloured; hides while dragging; invalid rooms show a red tooltip listing the problems.
    - tooltips show NoRegion
- [x] Edit > Validation: turn on "unique names", make two rooms with the same name: both get a red cross.

## 4. Dialogs (look and feel)
- [x] Room, Connection, Label, Map Settings, App Settings, Automap Settings, Map Theme, About: nothing clipped, access keys (Alt+letter) work, Tab order sensible, OK on Enter, Cancel on Esc.
    - Application Settings:
        - I can't select the tabs using the keyboard. I would expect to be able to tab up to them so I can change the tab
        - On General tab - tab from Specify margin goes to OK, then Cancel, then back to Specify margin instead of back to the top. Shift Tab from Specify margin does go up to Default Font Name.
        - Tooltip tab: looks like tab from Cancel also goes to last element in panel here too.
        - 
- [ ] Room dialog: shape preview, remembers last tab, invalid name shows an error, Objects box Tab/Shift+Tab/Enter
    - The object syntax when it opens makes the object text box move. Pressing esc to close the object syntax closes the whole dialog
    - pressing tab to indicate a contained object works but of course, you can't tab out of the text box, which is a bit of a usability issue tbh. Probably best if we remove the auto tab and rely on the user to enter manual dashes.
- [x] Connection dialog: preset radios follow what you type in Start/End.
- [x] Map Settings: region rename/delete moves rooms correctly; colours show friendly names; fonts pick style.
    - Setting the region text colour isn't affecting the rooms on the map. Background colour works.
    - Choose Colour dialog doesn't render correctly. Text is cut off. Choosing the More.. expanded dialog > then the HSV options - is too narrow to show the controls.
        - there's only a cancel button too  
- [x] Each dialog's OK is one undo step.

## 5. Files
- [x] Open, Save, Save As, Recent Maps (max 4), reopen last map on start (setting on).
    - the setting still says that it is "not active yet" in the UI. There are other settings also labelled such.
- [x] Edit the open map in another program and save: the app asks to reload.
- [x] Open Map from Web (Ctrl+Shift+O) with a real URL to a .trizbort file; Save then goes to Save As.
- [x] Close with unsaved changes: prompt; settings and window size/position remembered next start.
    - On load, Map view always starts top left corner
- [ ] Double-click a .trizbort file in the file manager (after registering with `packaging/`): opens in the app.

## 6. Export
- [x] File > Export > Image: PNG, JPEG and BMP by extension; scale options (100%, current zoom, custom); selection only.
    - current zooms seems to have exported the whole map
- [x] File > Export > PDF: opens in a PDF viewer, text selectable, rooms with descriptions show orange notes.
    - The application allowed me to export over an existing PDF file without confirming I wanted to overwrite it
- [ ] Each code export (Inform 7, Inform 6, TADS, Hugo, ZIL, Quest, Alan, Adventuron) to file and to clipboard.
- [x] Smart Save (Ctrl+Alt+S) writes the formats ticked in App Settings next to the map.
- [ ] Command line: `--inform7 out` etc., `--quicksave`, `--smartsave`, `--exit` (check the exit code).

## 7. Automap
- [ ] Start automap on a transcript; rooms appear and the current room scrolls into view.
- [ ] Ambiguous room names ask which room; two rooms in the same direction ask what to do.
- [ ] Stop, restart, F5 runs to completion; an unreadable transcript shows one clear error.

## 8. Platform
- [x] Linux: menus, fonts, dialogs and file pickers look right; Ctrl shortcuts work.
- [ ] Windows: same, plus Alt+F4 exits.
- [ ] Light and dark OS theme: everything readable.
