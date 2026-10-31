# Implementation tickets

Written 2026-10-30 (T3.1) from the feature matrix, dialog reviews, issues and the accepted decisions D1–D4.
Each ticket follows the template in `../../avalonia-parity-review.md` §8 and is sized for one agent session.

**Done so far (109 of 109):** 001, 002, 003, 004, 005, 006, 007, 008, 009, 010, 011, 012, 020, 021, 022, 023, 024, 025, 026, 027, 028, 029, 030, 031, 032, 033, 034, 035, 036, 037, 038, 039, 040, 041, 042, 043, 044, 045, 046, 047, 048, 049, 050, 051, 052, 053, 054, 055, 056, 057, 058, 059, 060, 061, 062, 063, 064, 065, 066, 067, 070, 071, 072, 073, 074, 075, 076, 077, 078, 079, 080, 081, 082, 083, 084, 085, 086, 087, 088, 089, 090, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 114, 115, 116, 117, 118, 120, 121, 122, 123, 124, 126, 127, 128, 129, 130 — all tickets done. Follow-ups are in `../issues.md`.

**How to pick one:** take the lowest wave whose dependencies are all done; within a wave, P1 first. Mark a ticket done by
adding `Status: Done (<commit>)` under its title and updating the matrix rows it lists.

**Priorities are suggestions — Lee to confirm (T3.2).** P1 = needed for a first usable release; P2 = parity; P3 = power-user/rare.

**Open decision:** ticket 123 asks Lee to confirm the PDF engine (recommended: Skia PDF now, PDFsharp-core annotations later).

**Critical path:** 001 → 002 → 009 → 010 unblocks most command tickets; 003 → 004 (undo) and 005 → 006 (canvas interaction) run alongside.

Related pairs (don't duplicate): 044 Quick Find command ↔ 086 Quick Find dialog · 060 Open from Web command ↔ 089 URL dialog · 058 Recent Maps menu ↔ 102 recent-files service · 055 debug keys ↔ 108 `--debug` flag · 059 Smart Save command ↔ 110 Smart Save service · 087 Map Statistics window ↔ E-section export naming.

| # | Ticket | Wave | Pri | Size | Tier | Depends | Matrix rows |
|---|---|---|---|---|---|---|---|
| 001 ✅ | [Add Selection and Viewport types to Core](001-selection-and-viewport-types-in-core.md) | 1 | P1 | S | mid | — | X010, X047–X049 |
| 002 ✅ | [Add EditorSession to Core; CommandService becomes a thin adapter](002-editor-session-in-core.md) | 2 | P1 | M | mid | 001 | X010, X048, X052, X053 |
| 003 ✅ | [Add UndoStack with gzip snapshot mementos, transactions and dirty flag](003-undostack-snapshot-mementos.md) | 3 | P1 | L | mid + strong review | 002 | undo/redo, X052 |
| 004 ✅ | [Route existing mutations through the EditorSession; remove hand-set IsDirty](004-route-mutations-through-session.md) | 4 | P1 | M | mid | 003 | X012–X015, X021, X022, X052, M024, M026–M030 |
| 005 ✅ | [Move canvas hit-testing to Core (ZOrder-aware)](005-hit-testing-to-core.md) | 3 | P1 | M | mid | 002 | X034–X036, X046, G001, G013, G016 |
| 006 ✅ | [Move the pointer/key drag state machine to Core (CanvasInteraction)](006-drag-state-machine-to-core.md) | 5 | P1 | L | mid + strong review | 004, 005, 009 | X037–X044, X046, X051, G001–G021, I-010 |
| 007 ✅ | [New-connection gesture uses new-connection defaults; redock gesture in Core](007-connection-gestures-defaults-redock.md) | 6 | P1 | M | mid | 006 | G008–G010, X004, X011, X041 |
| 008 ✅ | [Add Undo and Redo commands and UI](008-undo-redo-commands-and-ui.md) | 5 | P1 | S | mid | 004, 010 | undo/redo, X052 |
| 009 ✅ | [Command catalogue types (incl. neutral key input types) and tests in Core](009-command-catalogue-types.md) | 3 | P1 | M | mid | 002 | X050, X051 |
| 010 ✅ | [Avalonia generates main menu, context menu, toolbar and key bindings from the catalogue](010-avalonia-generates-menus-from-catalogue.md) | 4 | P1 | L | mid + strong review | 009 | X050, X051, C001, C002, M001–M013 |
| 011 ✅ | [Add async IEditorPrompts to Core; the automap engine awaits them](011-async-editor-prompts-and-automap-engine.md) | 1 | P1 | M | mid | — | E044, E045 |
| 012 ✅ | [Avalonia IEditorPrompts: same-direction and disambiguation dialogs](012-avalonia-automap-prompt-dialogs.md) | 2 | P1 | M | mid | 011 | E044, E045, D008, D009, I-008 |
| 020 ✅ | [Add Viewport.CenterOn(element) and select-nearest-centre helpers](020-viewport-centre-on-element.md) | 3 | P2 | S | mid | 002 | (supports K001, K003, K004, K011, K013, K014, M030) |
| 021 ✅ | [Add Select All/None and the Select Special commands](021-select-special-commands.md) | 5 | P1 | M | mid | 002, 003, 009, 010 | M015–M023, K002, K027 |
| 022 ✅ | [Add Select Regions, Select Start Room and select-nearest-to-centre](022-select-regions-start-room-nearest.md) | 5 | P1 | S | mid | 002, 009, 010, 020 | K001 (selection half), K003, K004 |
| 023 ✅ | [Add Properties/Rename commands with Enter, F2 and Alt+Enter](023-properties-rename-enter.md) | 6 | P1 | S | mid | 002, 009, 010, 022 | M029, M031, K001, K026, C007 |
| 024 ✅ | [Add Change Region (Shift+F2) and Set Region context submenu](024-change-region-and-set-region-menu.md) | 5 | P2 | M | mid | 002, 003, 009, 010 | M032, C011 |
| 025 ✅ | [Add Toggle Darkness, Force Darkness and Force Lighted commands](025-room-lighting-commands.md) | 5 | P1 | S | mid | 002, 003, 009, 010 | M033–M035, K023, K060, K061, C008 |
| 026 ✅ | [Add Start Room and End Room commands](026-start-end-room-commands.md) | 5 | P1 | S | mid | 002, 003, 009, 010 | M036, M037, C009, C010 |
| 027 ✅ | [Add the four Room Shape commands](027-room-shape-commands.md) | 5 | P1 | S | mid | 002, 003, 009, 010 | M038–M041, K009, K035, K036, C012 |
| 028 ✅ | [Add Join Rooms command](028-join-rooms-command.md) | 6 | P2 | M | mid | 002, 003, 009, 010, 030 | M042, K020, C013 |
| 029 ✅ | [Add Swap Objects/Names/Formats/Regions commands](029-swap-family-commands.md) | 5 | P1 | M | mid | 002, 003, 009, 010 | M043–M046, K021, K057–K059, C014 |
| 030 ✅ | [Add new-connection defaults and the Plain/Dotted/One Way commands](030-new-connection-defaults-and-line-styles.md) | 5 | P1 | M | mid | 002, 003, 009, 010 | M047–M049, M073, M074, K005, K041, K042, C004 |
| 031 ✅ | [Add connection label commands Up/Down/In/Out](031-connection-label-commands.md) | 6 | P1 | S | mid | 002, 003, 009, 010, 030 | M050, K006, K038–K040, C005 |
| 032 ✅ | [Add Reverse Line and Rotate Connector commands](032-reverse-line-and-rotate-connector.md) | 5 | P1 | S | mid | 002, 003, 009, 010 | M051, K007, C006, K019, K054–K056 |
| 033 ✅ | [Add Adjust Ports, Cycle Port Detail and Delete All Connections](033-port-adjust-and-delete-all-connections.md) | 5 | P2 | S | mid | 002, 003, 009, 010 | K017, K018, K053 |
| 034 ✅ | [Make drawn connections use new-connection defaults](034-new-connections-use-defaults.md) | 6 | P1 | S | mid | 006, 030 | G008 |
| 035 ✅ | [Add Bring to Front and Send to Back commands](035-z-order-commands.md) | 5 | P2 | S | mid | 002, 003, 009, 010 | C002, C003 |
| 036 ✅ | [Add the four project validation toggles](036-validation-toggles.md) | 5 | P2 | S | mid | 002, 003, 009, 010 | M052–M055 |
| 037 ✅ | [Add Room command with new-room defaults and insert-into-connection](037-add-room-at-cursor.md) | 5 | P1 | M | mid | 002, 003, 005, 006, 009, 010 | M030, K008, C001 |
| 038 ✅ | [Add Map Label command (L)](038-add-label-at-cursor.md) | 5 | P1 | S | mid | 002, 003, 009, 010 | M075, K037 |
| 039 ✅ | [Route Delete through one command and add Backspace alias](039-delete-and-backspace-aliases.md) | 5 | P1 | S | mid | 002, 003, 009, 010 | M028, K046, K047 |
| 040 ✅ | [Move Copy/Cut/Paste into Core commands over IClipboard](040-clipboard-copy-cut-paste.md) | 5 | P1 | M | mid | 002, 003, 009, 010 | K043–K045, M026 |
| 041 ✅ | [Add Copy Color command](041-copy-color-command.md) | 6 | P3 | S | cheap | 002, 009, 010, 040 | M025 |
| 042 ✅ | [Add Duplicate command (Primary+D)](042-duplicate-selection-command.md) | 6 | P2 | S | mid | 002, 003, 009, 010, 040 | (new, shortcut-map Q5) |
| 043 ✅ | [Add Backup Project command](043-backup-project-command.md) | 5 | P3 | S | cheap | 002, 009, 010 | K016 |
| 044 ✅ | [Add Quick Find command](044-quick-find-command.md) | 5 | P2 | S | cheap | 002, 009, 010 | K022 |
| 045 ✅ | [Add F3 / Shift+F3 active-element cycling](045-active-element-cycling.md) | 5 | P3 | S | cheap | 002, 009, 010, 020 | K065, K066 |
| 046 ✅ | [Make plain arrows move the selection or pan the view](046-arrow-keys-move-and-pan.md) | 5 | P1 | S | mid | 002, 003, 009, 010 | K012, K015 |
| 047 ✅ | [Add Shift+Arrow / Shift+Home/End/PgUp/PgDn / keypad navigation](047-shift-arrow-navigation.md) | 5 | P2 | M | strong | 002, 005, 006, 009, 010, 020 | K011, K014, K032–K034, K049, K050 |
| 048 ✅ | [Add Primary+Arrow / Primary+NumPad add-or-connect room](048-ctrl-arrow-add-or-connect.md) | 6 | P2 | M | strong | 002, 003, 009, 010, 020, 030, 037, 047 | K013, K051 |
| 049 ✅ | [Add Alt+NumPad unexplored connection](049-alt-numpad-unexplored-connection.md) | 6 | P3 | S | mid | 002, 003, 009, 010, 030 | K052 |
| 050 ✅ | [Add zoom presets, micro zoom and unify zoom gestures](050-zoom-commands-presets-micro.md) | 5 | P1 | M | mid | 002, 009, 010 | M056–M058, K028–K030 |
| 051 ✅ | [Honour InvertMouseWheel and Ctrl micro-zoom on the wheel](051-wheel-zoom-invert-and-micro.md) | 6 | P2 | S | mid | 006, 050 | G017 |
| 052 ✅ | [Add Reset View, Zoom to Entire Map, grid check state, minimap toggle, zoom label](052-view-origin-fit-grid-minimap.md) | 6 | P1 | M | mid | 002, 009, 010, 050 | M059–M062, K010, K031, C018 |
| 053 ✅ | [Apply approved gestures for Save As, Settings, Help, About and Exit](053-settings-help-about-exit-save-as-gestures.md) | 5 | P1 | S | cheap | 009, 010 | M005, M013, M065, M066, M071, M072, K024 |
| 054 ✅ | [Add Toggle Text command](054-toggle-text-command.md) | 5 | P2 | S | cheap | 002, 009, 010 | M014 |
| 055 ✅ | [Add debug toggles enabled only with --debug](055-debug-toggles-behind-debug-flag.md) | 6 | P3 | S | mid | 009, 010, 054 | K062–K064, K067, K069 |
| 056 ✅ | [Add export-to-file gestures and enable-when-rooms rule](056-export-file-gestures-and-enable.md) | 5 | P2 | S | cheap | 009, 010 | M009 |
| 057 ✅ | [Add Export to Clipboard commands](057-export-to-clipboard-commands.md) | 5 | P2 | M | mid | 002, 009, 010 | M010, M011 |
| 058 ✅ | [Add Recent Maps submenu](058-recent-maps-menu.md) | 5 | P2 | M | mid | 002, 009, 010 | M012 |
| 059 ✅ | [Add Smart Save command](059-smart-save-command.md) | 6 | P2 | M | mid | 002, 009, 010, 053 | M006 |
| 060 ✅ | [Add Open Map from Web](060-open-map-from-web.md) | 5 | P2 | M | mid | 002, 009, 010 | M003 |
| 061 ✅ | [Add automap run-to-completion and single-step keys](061-automap-run-and-step-keys.md) | 5 | P3 | S | mid | 009, 010 | K068, K071 |
| 062 ✅ | [Add Ctrl-click to jump to the original of a reference room](062-ctrl-click-reference-room.md) | 6 | P3 | S | mid | 005, 006, 020 | G003, G019 (hand cursor part) |
| 063 ✅ | [Align marquee modifiers and double-click empty-space rule with legacy](063-click-marquee-doubleclick-rules.md) | 6 | P2 | S | mid | 005, 006 | G005, G014 |
| 064 ✅ | [Add waypoint drag threshold, selected waypoint and delete](064-waypoint-drag-threshold-and-delete.md) | 6 | P2 | M | strong | 005, 006, 039 | G011, G012 |
| 065 ✅ | [Restore tooltip colouring and hide-on-drag](065-tooltip-styling-and-hiding.md) | 6 | P3 | S | cheap | 006 | G018 |
| 066 ✅ | [Generate context menu per target with legacy visibility rules](066-context-menu-target-rules.md) | 7 | P1 | M | mid | 005, 010, 023, 024, 025, 026, 027, 028, 029, 030, 031, 032, 035, 037 | C015, G016, C019, C001 |
| 067 ✅ | [Add shortcut-map conformance and modifier-exactness tests](067-shortcut-map-conformance-test.md) | 7 | P2 | M | mid | 010, 021, 025, 027, 029, 030, 031, 032, 047, 048, 050 | (all K and M gesture rows) |
| 070 ✅ | [Define and apply shared dialog style guidelines](070-dialog-style-guidelines.md) | 1 | P1 | M | mid | — | D001-D007, D015, D016 |
| 071 ✅ | [Room dialog: persistent header, missing fields and friendly option names](071-room-dialog-header-and-missing-fields.md) | 2 | P1 | M | mid | 070 | D001 |
| 072 ✅ | [Room dialog: shape preview, validation, dependencies, last tab, OK as one undo step](072-room-dialog-preview-validation-undo.md) | 4 | P1 | M | mid | 002, 003, 070, 071 | D001 |
| 073 ✅ | [Port ObjectListEditor to Core and wire it to the Objects box; restore Object Syntax help](073-object-list-editor.md) | 3 | P2 | S | mid | 071 | D001, D013 |
| 074 ✅ | [Connection dialog: restore Style group, colour swatch/clear, multiline description, no clipping](074-connection-dialog-layout-and-style.md) | 3 | P1 | M | mid | 070, 085 | D002, D016 |
| 075 ✅ | [Connection dialog: preset radios that track the text, access keys, OK as one undo step](075-connection-dialog-text-presets-and-undo.md) | 4 | P1 | M | mid | 002, 003, 074 | D002 |
| 076 ✅ | [Label dialog: Shape enable rule, initial focus, groups, size, OK as one undo step](076-label-dialog.md) | 4 | P1 | S | mid | 002, 003, 070 | D003 |
| 077 ✅ | [Map settings: add missing fields and restore group headings, hints and tab layout](077-map-settings-missing-fields-and-groups.md) | 2 | P1 | M | mid | 070 | D004 |
| 078 ✅ | [Map settings: friendly colour names and fonts with style via font picker](078-map-settings-colours-and-fonts.md) | 3 | P2 | M | mid | 077, 084, 085 | D004, D016 |
| 079 ✅ | [Map settings: region validation, swatch preview; OK as one undo step (rename/delete already fixed, I-007)](079-map-settings-regions-and-undo.md) | 4 | P1 | M | mid | 002, 003, 077 | D004, D005 |
| 080 ✅ | [App settings: restore General tab groups, Port Adjust Detail, hover tips, honest labels](080-app-settings-general-tab.md) | 2 | P1 | M | mid | 070 | D006 |
| 081 ✅ | [App settings: tooltip tab dependencies and Show Canvas Tooltips master toggle (I-005)](081-app-settings-tooltips-tab.md) | 3 | P1 | S | mid | 080 | D006 |
| 082 ✅ | [Automap settings dialog: groups, intro and hint text, access keys, no clipping](082-automap-settings-dialog.md) | 2 | P1 | M | mid | 070 | D007 |
| 083 ✅ | [About dialog: version, credits, repository links, licence, third-party notices](083-about-dialog.md) | 2 | P2 | S | cheap | 070 | D015 |
| 084 ✅ | [Font picker control (family list, size, bold/italic, live sample)](084-font-picker-control.md) | 2 | P2 | M | mid | 070 | D016, D004, D006 |
| 085 ✅ | [Colour picker: visible no-colour state, one-click clear, optional full colour view](085-colour-picker-improvements.md) | 2 | P1 | S | mid | 070 | D016, D001, D002, D003 |
| 086 ✅ | [Quick Find: search rooms and jump to the match](086-quick-find.md) | 5 | P2 | M | mid | 002, 009, 010, 070 | D010 |
| 087 ✅ | [Map Statistics window and Export](087-map-statistics-and-export.md) | 5 | P2 | M | mid | 009, 010, 070 | D011 |
| 088 ✅ | [Online Help: Help menu links and F1](088-online-help.md) | 5 | P2 | S | cheap | 009, 010 | D014 |
| 089 ✅ | [Open Map from URL dialog](089-open-map-from-url-dialog.md) | 5 | P2 | S | mid | 002, 009, 010, 070 | D012 |
| 090 ✅ | [Map Theme dialog: export current map theme, infer default room style, no-undo wording, current indicator](090-map-theme-dialog-gaps.md) | 4 | P2 | M | mid | 002, 003, 070 | D017 |
| 100 ✅ | [Store appsettings.json in a per-user config folder](100-per-user-settings-folder.md) | 1 | P1 | S | mid | — | A012, I-011 |
| 101 ✅ | [Save settings on exit, project open and project save](101-save-settings-on-exit-and-open.md) | 2 | P1 | S | mid | 100 | A013, A017 |
| 102 ✅ | [Recent files service (storage, pruning, activation)](102-recent-files-service.md) | 3 | P2 | S | mid | 100, 101 | A015, M012 |
| 103 ✅ | [Honour "Load last project on startup"](103-load-last-project-on-startup.md) | 3 | P2 | S | mid | 100, 101, 104 | A014, I-001 |
| 104 ✅ | [Parse command-line arguments and open the named map on launch](104-command-line-arguments-open-file.md) | 2 | P1 | M | mid | 100 | A001, A019 |
| 105 ✅ | [CLI code export flags](105-cli-headless-code-export.md) | 3 | P3 | M | mid | 104, 106 | A002, E014, E015 |
| 106 ✅ | [Core export helpers: export to file and to string for every language](106-core-export-to-file-and-text.md) | 1 | P2 | S | mid | — | E013–E015 |
| 107 ✅ | [CLI automap, quick save, smart save and exit flags](107-cli-automap-quicksave-smartsave-exit.md) | 8 | P3 | M | mid | 011, 012, 104, 105, 110 | A018, A019, E015 |
| 108 ✅ | [Wire the --debug startup flag and the debug rendering hooks](108-debug-flag-wiring.md) | 7 | P3 | S | mid | 055, 104 | K062–K064, K067, K069 |
| 109 ✅ | [File association metadata and OS "open file" activation](109-file-association-and-os-open-events.md) | 3 | P3 | S | mid | 104 | A001 |
| 110 ✅ | [Smart Save service: file naming, settings, messages, PDF/image output](110-smart-save.md) | 7 | P2 | M | mid | 059, 112, 120, 123 | E030–E034 |
| 111 ✅ | [Status bar cursor position follows the pointer](111-status-bar-cursor-position.md) | 3 | P1 | S | mid | 002 | A020, I-002 |
| 112 ✅ | [Reload prompt when the open map changes on disk](112-file-watcher-reload-prompt.md) | 2 | P1 | S | mid | 011 | A005, I-003 |
| 114 ✅ | [Window title: dirty marker, version and "show full path" setting](114-window-title-format-and-full-path.md) | 4 | P1 | S | mid | 003 | A004, I-006 |
| 115 ✅ | [Honour "Apply style to new rooms"](115-apply-style-to-new-rooms.md) | 3 | P1 | S | mid | 002 | G008, I-006 |
| 116 ✅ | [Persist window size, position and maximised state](116-window-geometry-persistence.md) | 3 | P2 | S | mid | 100, 101 | A009 |
| 117 ✅ | [Profile and fix slow Elements.Add on large maps](117-profile-and-fix-elements-add-performance.md) | 1 | P1 | M | mid | — | I-009 |
| 118 ✅ | [Code export pickers: default name, extension, folder memory, overwrite prompt](118-code-export-dialog-behaviour.md) | 2 | P1 | M | mid | 011, 100, 106 | E005–E008, E010 |
| 120 ✅ | [Image export formats (PNG, JPEG, BMP) and default image type](120-image-export-formats.md) | 3 | P2 | M | mid | 100, 118 | E016, E017, E024 |
| 121 ✅ | [Image export scale and selection-only option](121-image-export-scale-and-selection.md) | 4 | P2 | M | mid | 002, 120 | E018, E019, E022 |
| 122 ✅ | [Verify and fix image export background and margins](122-image-export-fidelity-background-margins.md) | 4 | P2 | S | mid | 120 | E020, E021 |
| 123 ✅ | [Export PDF (single page, vector, metadata), includes engine decision](123-pdf-export.md) | 5 | P2 | L | mid | 011, 120, 121, 122 | M007, E027, E029 |
| 124 ✅ | [PDF room-description annotations](124-pdf-room-description-annotations.md) | 6 | P3 | M | mid | 123 | E028 |
| 126 ✅ | [Automap start/stop/status UX gaps and single-step](126-automap-start-stop-ux-and-single-step.md) | 7 | P2 | M | mid | 009, 010, 011, 012, 055 | E037, E038, E040, E041, M063 |
| 127 ✅ | [Automap error reporting and recovery](127-automap-error-handling-and-recovery.md) | 4 | P2 | S | mid | 003, 011 | E046–E049 |
| 128 ✅ | [Automap canvas ops parity: SelectRoom scrolls into view, dotted connections](128-automap-select-room-visibility.md) | 3 | P2 | S | mid | 002 | E049 |
| 129 ✅ | [Restore Default Map Settings](129-restore-default-map-settings.md) | 5 | P2 | S | mid | 003 | M067 |
| 130 ✅ | [Room validation indicators on the map and in tooltips](130-room-validation-indicators.md) | 5 | P1 | S | mid | 036, 065 | A011 |
| 131 ✅ | [Trackpad and touchscreen navigation (GitHub issue #21)](131-trackpad-and-touch-navigation.md) | 6 | P1 | M | mid + strong review | 006, 050, 051 | issue #21 |
| 132 ✅ | [Rendering parity fixes from the export comparison (GitHub #22) + three UI bugs](132-rendering-parity-fixes.md) | 6 | P1 | L | mid + strong review | — | I-022..I-025 |
| 133 ✅ | [Canvas fixes from the manual checklist](133-checklist-canvas-fixes.md) | 6 | P1 | M | mid | — | I-027..I-031 |
| 134 ✅ | [Dialog fixes from the manual checklist](134-checklist-dialog-fixes.md) | 6 | P1 | M | mid | — | I-033..I-039 |

Totals: 107 tickets — P1 53, P2 41, P3 13. Waves: 1: 6, 2: 13, 3: 17, 4: 11, 5: 36, 6: 18, 7: 5, 8: 1.
