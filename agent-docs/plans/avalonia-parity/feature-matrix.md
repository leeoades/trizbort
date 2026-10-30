# Avalonia Feature Matrix

Seeded on 2026-10-30 from a static scan (menus, key handlers, dialog files). "WinForms" columns describe the
**reference app at tag `v2.3.1-beta.2`** (`$REF` = `legacy/`, restored by task T0.2). Every `Suspected-*` value
is a **hypothesis** to be confirmed by the Phase 1 tasks in `../avalonia-parity-review.md`. Fill in
`Verified status` using the vocabulary in that plan, and cite evidence. Do not delete rows; add new ones
with the next free ID in the section.

## M — Main menu and toolbar (task T1.1)

| ID | Area | WinForms item | WinForms shortcut | Avalonia equivalent | Seed status | Verified status | Evidence | Notes |
|---|---|---|---|---|---|---|---|---|
| M001 | File | New Map | Ctrl+N | File > New Map (NewMapCommand) | Suspected-Implemented |  |  |  |
| M002 | File | Open Map... | Ctrl+O | File > Open Map (OpenMapCommand) | Suspected-Implemented |  |  |  |
| M003 | File | Open Map from Web... | Ctrl+Shift+? | — | Suspected-Missing |  |  |  |
| M004 | File | Save Map | Ctrl+S | File > Save Map | Suspected-Implemented |  |  |  |
| M005 | File | Save Map As... | Ctrl+Alt+S | File > Save Map As (bound to Ctrl+Shift+S) | Suspected-Different (shortcut) |  |  |  |
| M006 | File | Smart Save | Ctrl+Shift+S | — | Suspected-Missing |  |  |  |
| M007 | File | Export > PDF... |  | — | Suspected-Missing |  |  |  |
| M008 | File | Export > Image... |  | Export > Image (PNG) | Suspected-Implemented |  |  |  |
| M009 | File | Export > Alan/Adventuron/Hugo/Inform7/Inform6/TADS/ZIL/Quest (file) | Ctrl+Shift+? | Export > * Source... | Suspected-Implemented (check shortcuts) |  |  |  |
| M010 | File | Export > Alan/Adventuron/Hugo/I7/I6/TADS/ZIL/Quest to Clipboard (9 items) | Ctrl+Alt+? | — | Suspected-Missing |  |  |  |
| M011 | File | Export > Quest to Clipboard (no header) | Ctrl+Alt+? | — | Suspected-Missing |  |  |  |
| M012 | File | Recent Maps |  | — | Suspected-Missing |  |  |  |
| M013 | File | Exit |  | File > Exit | Suspected-Implemented |  |  |  |
| M014 | Edit | Toggle Text | Ctrl+F4 | — | Suspected-Missing |  |  |  |
| M015 | Edit | Select All | Ctrl+A | Edit > Select All | Suspected-Implemented |  |  |  |
| M016 | Edit | Select None |  | — (Esc on canvas?) | Suspected-Partial |  |  |  |
| M017 | Edit | Select Special > All Rooms |  | — | Suspected-Missing |  |  |  |
| M018 | Edit | Select Special > Unconnected Rooms |  | — | Suspected-Missing |  |  |  |
| M019 | Edit | Select Special > Rooms w/ Objects |  | — | Suspected-Missing |  |  |  |
| M020 | Edit | Select Special > Rooms w/o Objects |  | — | Suspected-Missing |  |  |  |
| M021 | Edit | Select Special > All Connections |  | — | Suspected-Missing |  |  |  |
| M022 | Edit | Select Special > Dangling Connections |  | — | Suspected-Missing |  |  |  |
| M023 | Edit | Select Special > Self Looping Connections |  | — | Suspected-Missing |  |  |  |
| M024 | Edit | Copy | Ctrl+C | Edit > Copy | Suspected-Implemented |  |  |  |
| M025 | Edit | Copy Color | Ctrl+Alt+C? | — | Suspected-Missing |  |  |  |
| M026 | Edit | Paste | Ctrl+V | Edit > Paste | Suspected-Implemented |  |  |  |
| M027 | Edit | (none) | Ctrl+X | Edit > Cut | New-in-Avalonia |  |  |  |
| M028 | Edit | Delete | Delete | Edit > Delete | Suspected-Implemented |  |  |  |
| M029 | Edit | Properties | Enter (canvas) | Edit > Properties (Alt+Enter) | Suspected-Different (shortcut) |  |  |  |
| M030 | Edit > Rooms | Add Room | R (canvas) | Project > Add Room | Suspected-Partial (no R key) |  |  |  |
| M031 | Edit > Rooms | Rename | F2 | — | Suspected-Missing |  |  |  |
| M032 | Edit > Rooms | Change Region | Shift+F2 | — (Room Properties only?) | Suspected-Missing |  |  |  |
| M033 | Edit > Rooms | Toggle Darkness | K | — | Suspected-Missing |  |  |  |
| M034 | Edit > Rooms | Force Darkness | Ctrl+K | — | Suspected-Missing |  |  |  |
| M035 | Edit > Rooms | Force Lighted | Ctrl+Shift+K | — | Suspected-Missing |  |  |  |
| M036 | Edit > Rooms | Start Room | Ctrl+F5 | — (Room Properties only?) | Suspected-Missing |  |  |  |
| M037 | Edit > Rooms | End Room | Shift+F5 | — (Room Properties only?) | Suspected-Missing |  |  |  |
| M038 | Edit > Rooms | Room Shape > Square Corners | Ctrl+H | — | Suspected-Missing |  |  |  |
| M039 | Edit > Rooms | Room Shape > Ellipse | Ctrl+E | — | Suspected-Missing |  |  |  |
| M040 | Edit > Rooms | Room Shape > Rounded Edges | Ctrl+R | — | Suspected-Missing |  |  |  |
| M041 | Edit > Rooms | Room Shape > Octagonal Edges | Ctrl+8 | — | Suspected-Missing |  |  |  |
| M042 | Edit > Rooms | Join Rooms | J | — | Suspected-Missing |  |  |  |
| M043 | Edit > Rooms | Swap Objects | W | — | Suspected-Missing |  |  |  |
| M044 | Edit > Rooms | Swap Names | Ctrl+W | — | Suspected-Missing |  |  |  |
| M045 | Edit > Rooms | Swap Formats / Fills | Shift+W | — | Suspected-Missing |  |  |  |
| M046 | Edit > Rooms | Swap Regions | Alt+W | — | Suspected-Missing |  |  |  |
| M047 | Edit > Connections | Line Styles > Plain | P | — | Suspected-Missing |  |  |  |
| M048 | Edit > Connections | Line Styles > Dotted | T | — | Suspected-Missing |  |  |  |
| M049 | Edit > Connections | Line Styles > One Way Arrow | A | — | Suspected-Missing |  |  |  |
| M050 | Edit > Connections | Up / Down / In / Out labels (4 items) | U / D / I / O | — | Suspected-Missing |  |  |  |
| M051 | Edit > Connections | Reverse Line | V | — | Suspected-Missing |  |  |  |
| M052 | Edit > Validation | Rooms Must Have a Unique Name |  | — | Suspected-Missing |  |  |  |
| M053 | Edit > Validation | Rooms Must Have a Description |  | — | Suspected-Missing |  |  |  |
| M054 | Edit > Validation | Rooms Must Have a Subtitle |  | — | Suspected-Missing |  |  |  |
| M055 | Edit > Validation | Rooms Must Not Have a Dangling Connection |  | — | Suspected-Missing |  |  |  |
| M056 | View | Zoom In / Out | + / - (canvas) | View > Zoom In/Out (Ctrl++/Ctrl+-) | Suspected-Implemented |  |  |  |
| M057 | View | Zoom 50% / 100% / 200% |  | Reset Zoom = 100% only | Suspected-Partial |  |  |  |
| M058 | View | Zoom -1% / +1% |  | — | Suspected-Missing |  |  |  |
| M059 | View | Entire Map | Ctrl+Home | View > Fit to Map | Suspected-Implemented (check key) |  |  |  |
| M060 | View | Reset | Home | View > Reset Zoom (Ctrl+0) | Suspected-Different |  |  |  |
| M061 | View | Mini Map (toggle) |  | — (minimap always shown) | Suspected-Missing |  |  |  |
| M062 | View | Show Grid |  | View > Toggle Grid | Suspected-Implemented |  |  |  |
| M063 | Automap | Start... |  | Project > Toggle Automap | Suspected-Implemented |  |  |  |
| M064 | Automap | Stop |  | Toggle Automap / AutomapBar Stop | Suspected-Implemented |  |  |  |
| M065 | Tools | Map Settings... | Ctrl+, | Edit > Map Settings | Suspected-Implemented (no shortcut) |  |  |  |
| M066 | Tools | App Settings... | Alt+Shift+? | Edit > Application Settings | Suspected-Implemented (no shortcut) |  |  |  |
| M067 | Tools | Restore Default Map Settings |  | — | Suspected-Missing |  |  |  |
| M068 | Tools | Map Statistics... |  | — | Suspected-Missing |  |  |  |
| M069 | Tools | Map Statistic Export... |  | — | Suspected-Missing |  |  |  |
| M070 | Tools | (theme handling in MainForm.Themes.cs) |  | Edit > Map Theme... | Suspected-Implemented (verify vs MainForm.Themes.cs) |  |  |  |
| M071 | Help | Online Help | F1 | — (F1 opens About) | Suspected-Missing |  |  |  |
| M072 | Help | About |  | Help > About | Suspected-Implemented |  |  |  |
| M073 | Toolbar | Toggle Dotted Lines (T) |  | — | Suspected-Missing |  |  |  |
| M074 | Toolbar | Toggle One Way Arrow Lines (A) |  | — | Suspected-Missing |  |  |  |

## K — Canvas keyboard (task T1.2)

| ID | Key | WinForms behaviour | Seed status | Verified status | Evidence | Notes |
|---|---|---|---|---|---|---|
| K001 | `Enter` | Select room nearest viewport centre / open properties | Suspected-Missing |  |  |  |
| K002 | `Esc` | Select none | Suspected-Implemented |  |  |  |
| K003 | `0 / NumPad0` | Select start room | Suspected-Missing (Ctrl+0 is reset zoom) |  |  |  |
| K004 | `Ctrl+Shift+A` | Select all rooms in selected rooms' regions | Suspected-Missing |  |  |  |
| K005 | `A / T / P` | New-connection flow / style / plain toggles | Suspected-Missing |  |  |  |
| K006 | `U / D / I / O` | Set connection label | Suspected-Missing |  |  |  |
| K007 | `V` | Reverse line | Suspected-Missing |  |  |  |
| K008 | `R / L` | Add room / add label at cursor | Suspected-Missing |  |  |  |
| K009 | `Ctrl+H / Ctrl+E / Ctrl+R` | Room shape (note: Ctrl+E sets SquareCorners in source — likely a WinForms bug) | Suspected-Missing |  |  |  |
| K010 | `Home / Ctrl+Home` | Reset origin / zoom to fit | Suspected-Missing |  |  |  |
| K011 | `Shift+Home/End/PgUp/PgDn` | Select room in direction (up/down/in/out) | Suspected-Missing |  |  |  |
| K012 | `Arrows / NumPad1-9` | Move selection | Suspected-Implemented (check numpad diagonals) |  |  |  |
| K013 | `Ctrl+Arrows / NumPad` | Add-or-connect room in direction | Suspected-Missing |  |  |  |
| K014 | `Shift+Arrows / NumPad` | Select room in direction | Suspected-Missing |  |  |  |
| K015 | `Ctrl+Alt+Arrows` | Resize room | Suspected-Missing |  |  |  |
| K016 | `Ctrl+B` | Backup project | Suspected-Missing |  |  |  |
| K017 | `Ctrl+D` | Delete all connections of selected room | Suspected-Missing |  |  |  |
| K018 | `; or \` | Auto-adjust room's connection ports (Ctrl / Ctrl+Shift variants) | Suspected-Missing |  |  |  |
| K019 | `[ / ] (+Ctrl)` | Rotate connector | Suspected-Missing |  |  |  |
| K020 | `J` | Join two selected rooms | Suspected-Missing |  |  |  |
| K021 | `W / Shift+W / Alt+W / Ctrl+W` | Swap rooms / fills / regions / names | Suspected-Missing |  |  |  |
| K022 | `Ctrl+F` | Quick Find dialog | Suspected-Missing |  |  |  |
| K023 | `K / Ctrl+K / Ctrl+Shift+K` | Lighting toggle / force dark / force light | Suspected-Missing |  |  |  |
| K024 | `F1/F2/F3/F5/F11 (+modifiers)` | Various (see $REF Canvas.cs ~1337–1428) | Suspected-Missing — inventory T1.2 |  |  |  |
| K025 | `Space+drag / right-drag` | Pan | New/Implemented in Avalonia (verify vs WinForms middle-drag) |  |  |  |

## C — Canvas context menu (task T1.3)

| ID | WinForms item | Avalonia equivalent | Seed status | Verified status | Evidence | Notes |
|---|---|---|---|---|---|---|
| C001 | Add Room | Add Room | Suspected-Implemented |  |  |  |
| C002 | Send to Back | — | Suspected-Missing |  |  |  |
| C003 | Bring to Front | — | Suspected-Missing |  |  |  |
| C004 | Line Styles (Plain/Dotted/One Way) | — | Suspected-Missing |  |  |  |
| C005 | Up/Down/In/Out | — | Suspected-Missing |  |  |  |
| C006 | Reverse Line | — | Suspected-Missing |  |  |  |
| C007 | Rename | — | Suspected-Missing |  |  |  |
| C008 | Toggle Darkness | — | Suspected-Missing |  |  |  |
| C009 | Start Room | — | Suspected-Missing |  |  |  |
| C010 | End Room | — | Suspected-Missing |  |  |  |
| C011 | Set Region (submenu of regions) | — | Suspected-Missing |  |  |  |
| C012 | Room Shape (4) | — | Suspected-Missing |  |  |  |
| C013 | Join Rooms | — | Suspected-Missing |  |  |  |
| C014 | Swap (Objects/Names/Formats/Regions) | — | Suspected-Missing |  |  |  |
| C015 | Properties... | Properties... | Suspected-Implemented |  |  |  |
| C016 | Map Settings... | — | Suspected-Missing |  |  |  |
| C017 | Application Settings... | — | Suspected-Missing |  |  |  |
| C018 | Zoom | — | Suspected-Missing |  |  |  |
| C019 | (none) | Cut / Copy / Paste / Delete / Add Map Label | New-in-Avalonia |  |  |  |

Mouse gestures (drag room, resize, draw connection, move port/waypoint, marquee, double-click) are added as rows `G001…` by T1.3.

## G — Canvas mouse gestures (task T1.3)

| ID | Gesture | WinForms behaviour | Avalonia behaviour | Verified status | Evidence | Notes |
|---|---|---|---|---|---|---|

## D — Dialogs (tasks T1.4a–g)

| ID | WinForms dialog | Avalonia dialog | Seed status | Verified status | Field gaps (see dialogs/<name>.md) | Notes |
|---|---|---|---|---|---|---|
| D001 | RoomPropertiesDialog | RoomPropertiesDialog.axaml | Suspected-Partial (77KB designer vs 11KB axaml — field-by-field check) |  |  |  |
| D002 | ConnectionPropertiesDialog | ConnectionPropertiesDialog.axaml | Suspected-Implemented |  |  |  |
| D003 | LabelPropertiesDialog | MapLabelPropertiesDialog.axaml | Suspected-Implemented |  |  |  |
| D004 | SettingsDialog (map settings) | MapSettingsDialog.axaml | Suspected-Partial (87KB designer vs 12KB axaml) |  |  |  |
| D005 | RegionSettings | — (inside MapSettings?) | Suspected-Missing |  |  |  |
| D006 | AppSettingsDialog | AppSettingsDialog.axaml | Suspected-Partial |  |  |  |
| D007 | AutomapDialog | AutomapSettingsDialog.axaml | Suspected-Implemented |  |  |  |
| D008 | AutomapRoomSameDirectionDialog | — | Known gap (documented in 05-ui-and-canvas.md) |  |  |  |
| D009 | DisambiguateRoomsDialog | — | Suspected-Missing |  |  |  |
| D010 | QuickFind | — | Suspected-Missing |  |  |  |
| D011 | MapStatisticsView | — | Suspected-Missing |  |  |  |
| D012 | InputDialog (rename etc.) | — | Suspected-Missing |  |  |  |
| D013 | ObjectListEditor (control) | ? (check RoomPropertiesDialog objects tab) | Unknown |  |  |  |
| D014 | OnlineHelpDialog | — | Suspected-Missing |  |  |  |
| D015 | AboutDialog | AboutDialog.axaml | Suspected-Implemented |  |  |  |
| D016 | Colour/font pickers | ColorPickerButton; fonts? | Unknown |  |  |  |

## A — Application behaviour (task T1.5)

| ID | Behaviour | Avalonia evidence | Seed status | Verified status | Evidence | Notes |
|---|---|---|---|---|---|---|
| A001 | Open .trizbort passed on command line / file association | Program.cs/App.axaml.cs ignore args | Suspected-Missing |  |  |  |
| A002 | CLI export/automap options (CommandLineOptions in Core) | — | Suspected-Missing |  |  |  |
| A003 | Unsaved-changes prompt on New/Open/Close | CanLoseProjectAsync | Suspected-Implemented (verify on window close) |  |  |  |
| A004 | Window title shows file name + dirty marker | WindowTitle binding | Suspected-Implemented |  |  |  |
| A005 | File watcher: reload when file changes on disk (TrizbortFileWatcher) | — | Suspected-Missing |  |  |  |
| A006 | Drag-and-drop a .trizbort onto window | — | Unknown |  |  |  |
| A007 | Status bar: Caps/Num lock, zoom widget | Zoom slider + text; cursor position | Suspected-Different |  |  |  |
| A008 | Tooltips over elements on canvas | UpdateToolTip/BuildToolTipText | Suspected-Implemented |  |  |  |
| A009 | Window size/position persisted between runs | — | Unknown |  |  |  |
| A010 | Minimap click/drag navigation | MinimapControl | Suspected-Implemented |  |  |  |
| A011 | Room validation indicators drawn on canvas | — | Unknown |  |  |  |

## X — Editing/interaction logic outside Core (task T1.6)

| ID | Member (file; `$REF` or Avalonia) | What it does | Core equivalent? | Needed by matrix rows | Extraction effort (S/M/L) | Notes |
|---|---|---|---|---|---|---|

## Summary (task T1.8 fills this in)

| Section | Implemented | Partial | Missing | Broken | Different | New-in-Avalonia |
|---|---|---|---|---|---|---|
| M |  |  |  |  |  |  |
| K |  |  |  |  |  |  |
| C |  |  |  |  |  |  |
| G |  |  |  |  |  |  |
| D |  |  |  |  |  |  |
| A |  |  |  |  |  |  |
| X |  |  |  |  |  |  |
