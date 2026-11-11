# Trizbort: Recast change log

Trizbort: Recast is a cross-platform rebuild of [Trizbort](https://github.com/JasonLautzenheiser/trizbort), the
interactive fiction map editor first written by [Genstein](https://github.com/genstein) and continued by
[Jason Lautzenheiser](https://github.com/JasonLautzenheiser) and contributors. It opens and saves the same `.trizbort` maps.

This file lists what Recast changes compared with Trizbort 1.7.4.1, the last release before this fork. The Windows-only
app's own history, from Trizbort 1.x through this fork's 2.x releases, is in [legacy/changelog.md](legacy/changelog.md).

Recast version numbers start again at 0.9 and follow [semantic versioning](https://semver.org/). Releases are tagged
`recast-vX.Y.Z`; headings here leave out the prefix.

## [Unreleased]

### Added

- **What Moved It?**: when a room that moves turns up somewhere new, automap asks which of the commands you typed in
  it moved it (a question and your answer listed as one, "pull peg → second"), with its best guess picked; or
  **Nothing I did** (a bus that moves by itself: the join is labelled "(by itself)" and never followed), or something
  else you type. It asks once for each new way between stops (down, then the first time back up), then follows the
  labelled joins as you play.
- **Go Live**: stepping through move by move to the end of the transcript, then playing on in the game, no longer
  leaves automap silently paused. A bar in the automap panel says the game has gone on, with a Go Live button to map
  each move as you play.
- **Automap: This Is a Different Room** (the room's right-click menu, or Project > Automap): when automap took you
  to be in a room already on the map but it's another with the same name (its exits go elsewhere), it adds the
  room ("Maze (2)") the way you went, moves the way you came in over to it, and carries on from there. It works for
  a way automap took for a loop back into the same room, too. One undo step.

- **This is a different 'X'** in the question automap asks when a known way out leads somewhere new: the room you
  left is another with its name, so automap adds it, moves the way you came in over to it, and joins the new room to
  it.
- **Split Room...** (Edit > Rooms, or the room menu): for a room that turns out to be two places with one name,
  even several moves later. Tick the joins that belong to the other place; they move to a new room ("Maze (2)") at
  the same sides. The newest join (automap's way in) is ticked to start with. When automap has you in that room,
  you also say where you are now (the new room, the original, or somewhere else). A way taken for a loop (up from
  "In the Chimney" to what automap took for the same chimney) arrives in the new room. One undo step.
- **Disambiguate Rooms** shows the room you pick on the map behind it, with the rooms around it, and puts the view
  back afterwards; **<Add a new room>** shows a faint dashed room where automap would put it. The dialog opens on the
  right of the window to leave the map in view.
- **The same place at different times** in the question automap asks when a known way out leads somewhere new (a dry
  riverbed that becomes a river once the dam is opened): both joins stay, and the two rooms go side by side.

### Changed

- **Previous Move**, **Next Move** and **Play to the End** are greyed out when there's nothing to go back to or
  play, and the automap panel shows where you are in the transcript: "Move 12 of 4,567" (moves are the commands in
  the transcript, not the game's own turn count).
- The question automap asks when a known way out leads somewhere new lists its answers on the left and explains the
  one picked on the right, with OK and Cancel. **'X' moves** and **Split** are now one answer: a copy of the room
  at each stop, joined by what moved it, with automap keeping track of which stop you're at. **Cancel** maps nothing for that move, where
  it used to stop automapping.
- While automapping, the map keeps your room near the middle of the view, rather than just inside the edge, so you
  can see its exits and it isn't hidden by the minimap.

### Removed

- The automap option **Assume no two rooms have the same name.** Automap now always tells rooms with the same name
  apart by the map, then by description, asking when it can't tell; with a transcript that isn't verbose it still
  takes a name to mean that room. The setting is ignored if it's in your settings file.

## [0.12.0-beta] - 2026-11-10

This beta is about **automap understanding the places games play tricks with**: rooms that share a name, rooms
that move (lifts, vehicles), ways that lead to different rooms at different times, darkness, and joins you draw
yourself. Joins now remember each way along them, so their labels say exactly what you did. A long join can be
**split** into two labelled stubs, and **Start a New Area Here** gives a part of the game (inside the house) room
of its own. While automapping, a **panel** beside the map shows where you are, the transcript and the mapping. This
is still a beta: keep backups and [report problems](https://github.com/leeoades/trizbort/issues).

### Added

- **Automap panel.** While automapping, its controls are in a panel docked beside the map, in three parts:
  **Where you are** (the room automap thinks you're in, with a green, amber or grey dot for mapping, not changing
  the map, or in the dark, and Centre on Current Room); **Transcript** (Previous Move, Next Move, Pause, and the
  game screen); and **Mapping** (Mapping or Tracking only, and why automap isn't mapping when it isn't). **Play to
  the End** is at the bottom, away from the step buttons, and asks first when there are 20 or more moves left.

- **Don't automap from here.** Mark a room (Room Properties, or the room's right-click menu) where automap has no
  chance, such as a maze or a cupboard that turns out to be a lift. While you're in it, automap maps nothing; when you
  come out it carries on from there, jumping to a room it knows or adding the new one joined by a dotted "?" join
  for the way through. The room shows a small amber "no entry" sign in its top-left corner; hover over it for what
  it means. Saved with the map; older versions ignore it.
- **Darkness.** A heading that says it's too dark to see ("Darkness", "It is pitch black") isn't mapped as a room.
  Automap remembers how you got there, and when you light the lamp maps the room you see as reached that way, with
  no "light lamp" join, and marks it Dark.
- **Merge Rooms** (Edit > Rooms, or the room menu, with two or more rooms selected) makes them one room: the one
  selected last is kept, and gets the others' joins, descriptions and objects. Joins between them, and joins and
  stubs left doubled, go. One undo step.
- **Joins you draw are followed.** Type a command that a join from your room is labelled with ("pull first peg"),
  and automap moves you along it once the game replies, unless the reply shows you somewhere else. A question and
  your answer count together ("pull peg", "Which peg?", "first"), words in any order. A heading naming a copy of
  a room ("Cupboard" in "Cupboard (-1)") means that copy when you're in it, or when your way there is known.
- **Split a room in two.** When a way out you've mapped leads somewhere new, the question automap asks now also
  offers **Split 'Cupboard' into separate rooms**: for a room that is really two places alike, such as a lift.
  Automap adds "Cupboard (2)", joined from the cupboard by the last thing you did there ("pull peg, first"), and
  joins the new room to it.
- **Rooms that move** (a lift, a car, a train). The same question offers **'Closet' moves (a lift, a vehicle)**:
  the room is marked as moving, and a copy of it with its name stands for it at the new stop, joined from it by what
  you did there ("pull peg"). Automap then works out which stop you're at from the way you go out, asking nothing
  more as you ride up and down, and adds a stop for a new place. A moving room and its stops show a blue sign with
  up and down arrows; Ctrl-click a stop to go to the room. Set it yourself with **Moves** in Room Properties or
  **Room Moves** on the room menu. Saved as an optional `moves` attribute; older versions ignore it.
- **Start a New Area Here** (the room menu, or Edit > Rooms) moves a room, such as the foyer on going into a house,
  to clear space beside the map, so the area beyond it can be mapped on its own, and splits its joins to the rooms
  it leaves behind. One undo step.
- **Split Join** (the join menu, or Edit) draws a long join as a short stub out of each room, labelled with the
  room at the other end ("to Building", "xyzzy: to Building", "from Debris Room" at the far end of a one-way join),
  instead of a line across the map. It's still one join, so automap goes along it as usual. Click a label ("to
  Debris Room") to move the map to that room, or Ctrl-click a stub. Saved as an optional `split` attribute; older
  versions draw the whole line.
- **Copies of rooms** (reference rooms, and a moving room's stops) are drawn in the look of the room they stand
  for, and their properties show only their name and that room. New on their menu: **Go to the Room It Stands
  For**, **Move Joins to the Room It Stands For**, and **Make It a Room of Its Own**. Darkness, start and end room
  and the automap settings are greyed out for them. Automap goes through a copy to the room it stands for, and
  never matches a copy by name.
- **One way, two rooms.** A way out (a direction, or a command such as "enter closet") that leads to more than one
  room, as after Keep Both, takes you to the one the game's heading names, then the one whose description starts
  the same way. With no heading, automap waits for the next one instead of guessing.
- **Where you are stays shown.** While automap runs, the room it thinks you're in has a blue dashed ring of its
  own, so clicking about the map (which changes the selection) no longer loses it.
- **Same name, different place?** Going a direction to a room with the same name as the one you left, when the
  game shows a description that starts unlike any of that room's, automap asks whether it's a new room (a second
  "In the Chimney" further up) or the same one. A way already mapped as looping back never asks, and once you say
  it's the same room, its new description is kept, so it won't ask again.
- **Automap: I'm Here** (the room's right-click menu, or Project > Automap > I'm Here, while automapping) tells automap which room you're in, when
  it has lost track or got it wrong. It maps on from there.
- **The map glides** when automap moves on to another room, and for Current Room, easing in and out so you can see
  which way it went. Panning or zooming yourself stops it.
- **Tracking only** (the automap panel, or Project > Automap > Tracking Only): automap follows you through rooms it
  knows but changes nothing, so you can step back and forward through the transcript to check it, and fix the map.
  Switch back to Mapping and it maps on from the next room it sees, joined to nothing.

### Changed

- A command used from one end alongside a direction ("in" and "enter closet" into the closet, with "north" seen
  back) is written on that end's label ("in / enter closet") instead of in the middle, since it works from that end.
  A command stays in the middle while the way back is only assumed.
- **Disambiguate Rooms** lists the rooms with that name and then **<Add a new room>** (showing the description the
  transcript gives), with **OK** and **Cancel**. Cancel maps nothing for it. **Auto-Choose the Rest**, which always
  took the first room, is now **Don't ask again while automapping: guess**: the room whose description starts the
  same way; if none does, a new room when you moved there, or the room you're in when you didn't.
- A join's side gives its direction only when its label names none: drawn on the south side and labelled "down",
  it's down, not south too. On the right of a room with no label, it's still east. **Join Properties** shows that
  direction ("east") in the end's label box, so you can see it and change it; left as it is, the map still leaves
  it off.
- **Joins remember their ways.** Each join keeps a list of the ways along it: every direction or command, from
  which end, and whether you were seen going that way or automap only assumed it. Its labels are drawn from that
  list ("north / up" at one end, "climb ladder" in the middle), so automap no longer has to guess from the label
  text or from the side a join leaves a room. Change a join's labels, or move an end whose side gave its
  direction, and automap goes by what you changed. Saved with the map; older versions keep the labels as before.
- Going a way the map already shows leading somewhere else now asks, wherever the join leaves the room: up from a
  room with a join drawn "n / up" on its north side.
- The start room is shown by a small green "play" sign in its top-left corner, instead of a green glow round the
  room. Hover over it for what it means.
- The automap items in the Project menu are now together in a **Project > Automap** submenu, named as in the
  automap panel: Start Automap..., Stop Automap, Play to the End, Pause, Next Move, Previous Move, Tracking Only, Centre on
  Current Room and I'm Here.

### Fixed

- Opening a map shows the whole map again: the view fitted to it, then jumped back to the map's origin, which can
  be well away from the rooms.
- The faint preview of a new room in Disambiguate Rooms is where automap would actually put it, the way you went.
- A moving room's join to its stop is labelled with what moved it (`pull first peg`), not the last thing typed in
  it (`open closet door`, typed after the closet stopped). What moves it back (`pull second peg`) gets a join the other
  way, so pulling either peg later moves your location to the other stop.
- Coming back north out of the closet no longer assumes the foyer leads south to it when south from the foyer is
  already the front porch, and the join's end doesn't move onto the side another join uses.
- A game's question in brackets (Infocom's "[Which peg do you mean, ...?]") counts as a question, so "pull peg"
  then "second" follows a join labelled "pull second peg".
- Going back a move while automap waited for a live game's next line no longer crashes when you carry on.
- **Room Properties** fits a small screen (Linux, laptops): everything but OK and Cancel scrolls, and the dialog opens
  no taller than the screen, so the buttons stay in reach.
- Going a way you've been seen to go somewhere else, into a room already on the map (out of the closet led to the
  foyer; after pulling the peg it leads to the cellar), now asks, as it does for a new room: keep the old way,
  the new one, both, or split the room you left ("Closet (2)", reached by "pull peg"). Keeping the new way takes
  it off the old join.
- A command's join ("enter closet") leaving a room's corner is no longer labelled with the corner ("se") when the
  map labels diagonal exits: its side only faces the other room.
- Several rooms with one name (Hollywood Hijinx has three "In the Chimney"): the map says which first. A join the
  way you went leads to its room, and going back the way a one-way join came (down, after coming up) leads back to
  the room it came from, until you find otherwise. Otherwise the room whose description starts the same way is
  the one; if none does, automap asks which, or whether it's a new room, offering them all. A name only one room
  has is that room, whatever the game shows on a later visit.
- A game that answers a move into the dark with a sentence ("It is pitch black.") rather than a heading is
  understood as dark: lighting the lamp maps the room as reached by the move, not by "turn on flashlight".
- A join's end moves to the side of a compass direction learnt later, when the side it was on only faced the
  other room (a command's join, or a guess): north from the Front Porch now leaves from its top.
- Dying no longer joins rooms: "yes" to "Do you want to be reincarnated?" isn't taken for a magic word, and a room
  shown after "You have died" or "*** The End ***" isn't joined to the room before.
- A command into a room and a direction back out are one join: "enter closet" from the foyer and "north" back
  from the closet make a single two-way join, labelled "enter closet" at the foyer and north at the closet, not two
  one-way joins. A command's join is no longer labelled with the corner it happens to leave from ("NW").
- Going up into a room that mentions a way down (or any way back the way you came) no longer adds a stub beside
  the join: automap assumes that way leads back, and labels the join's end with it. The join stays one-way until
  you go that way. A way out already drawn on a join gets no stub either, wherever the join leaves the room.
- Previous Move works after Play to the End: automap goes back to before the last move it played and pauses there.
- Going back a move keeps the end of the transcript at the bottom of the game screen, with the lines played since
  gone, instead of showing the top of the screen.
- Two rooms with the same name, one beyond the other ("In the Chimney", and up to another "In the Chimney"): when
  the map has a join that way to a room by that name, automap follows it, rather than taking you to be back in the
  room you left and adding a loop. A join counts by its label ("n / up") wherever it leaves the room.
- The label of a join leaving straight up or down from a room no longer overprints the room's objects listed on
  that side: it goes on the other side of the line, as was intended.
- Tracking only no longer changes the map at all: "see" (add object) and region commands in the transcript, and
  ways that lead nowhere removing their stubs, are left alone too.

## [0.11.0-beta] - 2026-11-08

This beta is about **Automap keeping your map tidy as you play**, and **playing a transcript a move at a time**.
Rooms now move aside to keep joins pointing the way their directions say, never across a room, and crossing as
little as possible. **Tidy Map...** tidies a whole map or the rooms you select, and rooms you place by hand stay
where you put them. The app is now called **Trizbort: Recast**. This is still a beta: keep backups and
[report problems](https://github.com/leeoades/trizbort/issues).

### Added

- **Rooms tidy themselves as you explore.** After each automap move, the rooms nearby shift so that joins point the
  way their directions say (north up the map, east to the right), lined up in rows and columns where they can be.
  Automap makes space for a new room by moving whole parts of the map, keeps groups of neatly joined rooms in shape,
  moves a room up or down when a join would run across it, and avoids joins crossing. It only tidies when the map
  changed.
- **Rooms placed by hand stay put.** A room you drag or nudge is left where you put it by the tidy as you play. This
  is saved with the map (older versions ignore it), and Tidy Map clears it from the rooms it tidies.
- **Tidy Map...** (Edit > Rooms, and the room right-click menu) tidies the whole map, or the selected rooms. Choose
  **step by step**, which keeps the map familiar and takes a few seconds, or the **solver** (Google OR-Tools), which
  lays everything out again and can untangle what step by step can't. It works in the background with a progress bar
  and Cancel, and is one undo step.
- **Play a transcript move by move.** Choose it when you start Automap, then use **Next Move** (F10), **Play to End**
  (F5), **Pause** and **Previous Move** (F9) on the automap bar. Previous Move goes back in the transcript without
  undoing the map, so you can step through a part again; the room you were in then is selected.
- **Game monitor.** A small monochrome screen in the corner of the map shows the game so far. Click it for a larger window you
  can copy from, which floats beside the map without blocking it (the map's keys and Ctrl+Z still work). Hide it from
  the automap bar.
- **Current Room** on the automap bar selects the room automap thinks you're in and centres the map on it.
- **Loops.** An exit that leads straight back into the same room is drawn as a small curl with an arrow, and counts
  as explored.
- **Map Settings > always label diagonal exits** (on by default) and **always label straight exits**.
- Automap labels several directions in short, "n / u / in" (a setting, on by default), and commands that lead to the
  same room share one label ("light lamp / turn on lamp"), with a compass direction first.

### Changed

- **Renamed Trizbort: Recast.** Download and file names stay as they were (`TrizbortRecast-...`,
  `trizbort-recast`, `Trizbort Recast.app`), as file names can't contain a colon.
- **One-way arrows** from a move automap hasn't seen the way back from arrive on the side of the room facing where
  they came from, keep off the room's known exits, and follow the room as you drag it. Going west into a new room
  arrives at its east side, taken to be the way back until you find otherwise.
- A join labelled with several directions ("n / e") moves to the side that faces the other room as you drag, and join
  ends move to wherever their line crosses fewest other joins.
- Up and down joins move round their rooms to keep from crossing other joins. Up rooms are placed above and down
  rooms below, but move aside when something's in the way.
- Double-clicking an empty part of the map always adds a room; the setting for it has gone (#28).
- Automap treats rooms with the same name as the same room when the first sentence of their descriptions matches, so
  text a game adds afterwards ("The door is open.") no longer makes it ask (#29).
- Dialogs scroll when a tiling window manager makes them smaller than they need, with OK and Cancel kept in view.
- The automap bar's Back button is now Previous Move, and the status bar no longer shows the pointer's coordinates.

### Fixed

- Joins and unexplored-exit stubs no longer touch or run into each other.
- Trying an exit that goes nowhere removes its stub as soon as the game replies, not when you next type a command.
- Carrying on from the end of a game that's still running now reads the command you type next, so automap finds the
  room you're in.
- Automap keeps headings such as `At "Y2"` and lowercase qualifiers such as `Garden, north` (#29).
- Inform 7 export declares a door once, however many directions lead through it (#30).
- F5, F9 and F10 work wherever the focus is, and Undo works while the transcript window is open.
- The automap bar's buttons no longer miss clicks when the status text changes.

## [0.10.0-beta] - 2026-11-02

This beta focuses on **more reliable Automap and recovery of unsaved work**. Automap now follows magic-word
journeys, remembers multiple directions between rooms, and records each move as its own undo step. Recovery copies
and clearer error reports help protect your maps and diagnose problems. This is still a beta: keep backups and
[report problems](https://github.com/leeoades/trizbort/issues).

### Added

- **Automap joins teleports.** When a command that isn't a direction takes you somewhere (a magic word such as
  `xyzzy`, or `enter boat`), automap joins the two rooms with a dotted one-way line labelled with what you typed. Using
  the same command back makes the join two-way. Undo, restore, restart and `look` don't create joins.
- **Recovery copies.** Recast keeps a recovery copy of the open map and offers to restore it after a crash or power
  cut. It is on by default; turn it off in Application Settings.
- **Error logs.** When something goes wrong, Recast writes a log you can send in, and shows a plain message saying
  where it is, instead of a technical error.
- Automap opens `.scr` transcripts (the Frotz default) as well as `.txt` and `.log`.
- **Several ways to the same room.** When more than one direction leads from one room to another (down to the beach,
  and north too), automap lists them all on one join ("north / down") instead of moving it. Compass directions decide
  where the join and the room go: a room first placed below by `down` moves above when `north` also leads there.

### Fixed

- Automap now maps the first room of a game correctly, including when the first command is `look`.
- Automap keeps room names with a comma or dash (`Garden, East`, `Hall - Upstairs`) as separate rooms.
- Each automap move is one undo step, and Undo while automap is running no longer crashes (GitHub #24).
- Automap no longer crashes with "Sequence contains no matching element" when it re-routes an exit.
- With "assume two-way connections" off, automap no longer asks about a room in the way, or moves exits it already
  knows.
- Automap carries on correctly when a transcript holds more than one play session.
- Automap no longer loses a move typed while you play live.
- Room names such as `Y2` are no longer mistaken for shouted text.
- Automap puts a new room in a clear spot nearby when its usual place is taken, instead of on top of an existing
  join.
- Automap no longer thinks the game started again when you type `version` (or `about`, `credits`...), which
  printed the opening banner and could lose the next room.
- Walking out of a room you teleported into no longer clashes with the teleport's join and drops the new room.
- Undoing an automap move also brings back the unexplored-exit stub it used.
- The error message only says your unsaved changes were kept when a copy was actually written; otherwise it asks you
  to save. A recovered map's copy is kept until the map is saved, even with recovery copies turned off.
- Show Grid is saved with the map, so turning it on or off now marks the map as changed and can be undone.
- Code export includes every direction of a join labelled with several ("north / down"), and doesn't turn
  teleport joins into compass exits.
- Inform 6 export writes each door only once, even when its join lists several directions.
- An automap move that arrives while you are dragging waits for the drag to finish, so cancelling the drag no
  longer loses it.
- Saving to a file name without a folder no longer reports a failure after saving, and Undo and Redo are no
  longer left greyed out after an automap step that changed nothing.
- After recovering unsaved changes, Recast watches the original file for changes made elsewhere; turning
  recovery copies on now copies a map that already has unsaved changes.
- All compiler warnings are fixed, including a broken binding in Map Settings.

### Changed

- The grid is drawn as lines again, and **View > Show Grid** and **Map Settings > Grid > Visible** now control
  the same setting.
- The New Map toolbar button uses a page icon rather than a plus sign.

## [0.9.0-beta.1] - 2026-10-31

This is the **first beta of Trizbort: Recast**, for testing. It has everything the Windows app has, and more, but it has
not been used much yet. Keep backups of your maps. The Windows-only Trizbort 2.x is still available from the
[releases page](https://github.com/leeoades/trizbort/releases) (tags `legacy-v*` and the older `v2.*`).

### Runs on Windows, Linux and macOS
- Rebuilt on .NET 10 and [Avalonia](https://avaloniaui.net/), so the same app runs on Windows, Linux and macOS.
  Downloads are self-contained, so no separate .NET install is needed: Windows x64 (`TrizbortRecast.exe`), Linux x64
  (`trizbort-recast`), and macOS for Apple Silicon and for Intel (`Trizbort Recast.app`).
- The macOS app opens `.trizbort` files from Finder. It is new and hasn't been tested on a Mac yet.
- A new app icon, based on the original's rooms and connections.
- Linux: a desktop entry and MIME type are included so `.trizbort` files open in Recast from your file manager. A
  Windows registry file for the file association is in the repository (`packaging/`).
- Settings are kept in your per-user folder: `%APPDATA%\Trizbort` on Windows, `~/.config/trizbort` on Linux and
  `~/Library/Application Support/Trizbort` on macOS. A damaged
  settings file no longer stops the app from starting; it is set aside and the defaults are used.

### New in Recast
- **Undo and redo** for every change: moving, resizing, adding and deleting, dialog edits, automap and more. Use
  Ctrl+Z and Ctrl+Y (or Ctrl+Shift+Z), up to 200 steps. The Edit menu names the step, and each dialog's OK is one step.
  The window title shows `*` for unsaved changes and clears it when you undo back to the saved map.
- **Esc cancels** a room drag, resize or connection drag on the first press and puts everything back.
- **Trackpads and touchscreens** (#21): two-finger scrolling pans in any direction; Ctrl + two-finger scroll zooms
  smoothly; on a touchscreen, one finger selects and drags, and two fingers pan and pinch-zoom. A new
  **App Settings > Scroll wheel / trackpad scroll** option chooses *Automatic*, *Always pan* or *Always zoom*.
- **More ways to pan:** right-drag or hold Space and drag, as well as middle-drag.
- **Duplicate** the selection with Ctrl+D.
- **Zoom shortcuts** Ctrl+=, Ctrl+- and Ctrl+0 (100%), as well as + and -.
- **Backspace** deletes, as well as Delete.
- **PDF export** draws the whole map as one vector page, so it stays sharp at any zoom and its text can be selected and
  searched. Rooms with descriptions still get a note you can open.
- **Image export** keeps the map's proportions when a very large map has to be scaled down, and the scale option says
  plainly that "current zoom" scales the whole map.
- **Code exports** keep accented and other non-English characters that the Windows app mangled.
- **Every save asks before overwriting** an existing file, including image, PDF, theme, statistics and log saves.
- **Room Properties:** an *Object syntax* button shows how to write object lists without moving the text box.
- **Dialogs** have been rebuilt to a consistent layout: Alt+letter access keys, Enter for OK, Esc for Cancel, a
  sensible Tab order, a font picker with a live sample, and a colour picker with a clear "no colour" choice.
- **Code export** remembers the last folder and file name for each language.

### Carried over from this fork's Trizbort 2.x releases
These were added to the Windows app after the fork (versions 2.0 to 2.3) and are all in Recast too.
- **Map labels:** free-standing notes with a choice of shapes, outlines, backgrounds and colours, which can be
  connected to rooms. Press L or use **Edit > Add Label**.
- **Curved connections:** drag a selected connection's midpoint handle to bend it, with up to three waypoints.
- **Themes:** built-in Classic, Parchment, Dark, High contrast and Sketch themes, plus import and export of
  `.trizbort-theme` files, and **Infer default room style from rooms**.
- **Hand-drawn style** as a map setting, with a per-room override, for every room shape.
- **Containers in object lists:** start a line with `-` to put an object inside the one above it (`- Pouch`, `-- Gem`).
  The map indents contained objects, and exporters treat them as contents.
- **Apply style to new rooms** and **Double click to add room** preferences.
- Room Properties remembers the last tab you used.
- Room tooltips appear after a normal hover delay and stay out of the way while you work.
- The automatic update checker has been removed; download new versions from GitHub Releases.

### Changed from Trizbort 2.x
- **Object lists:** Tab now moves to the next field instead of indenting. Type `-` at the start of a line to put an
  object inside the one above it; Enter still continues the current level.
- **Ctrl+arrow** (add or connect a room in that direction) makes plain connections: it no longer applies the Up, Down,
  In or Out label last chosen for new connections. That choice also resets when you start or open a map.
- **Shortcuts** that clashed or broke common conventions have changed:

  | Command | Trizbort 2.x | Recast |
  |---|---|---|
  | Save As | Ctrl+Alt+S | Ctrl+Shift+S |
  | Smart Save | Ctrl+Shift+S | Ctrl+Alt+S |
  | Export ZIL to file | Ctrl+Shift+Z | Ctrl+Shift+I (Ctrl+Shift+Z is Redo) |
  | Delete all connections of a room | Ctrl+D | Alt+Delete (Ctrl+D is Duplicate) |
  | Map Settings | Ctrl+, | Ctrl+Shift+, |
  | Application Settings | Alt+Shift+S | Ctrl+, |
  | Exit | Alt+F4 | Alt+F4 on Windows, Ctrl+Q on Linux |
  | Pan | Shift+right-drag or middle-drag | Right-drag, middle-drag or Space+drag |

- Debug tools (FPS counter, coordinates and rendering toggles) are only available when you start the app with
  `--debug`.

### Known limitations
- Pinch on a Linux touchpad does nothing, because the UI toolkit doesn't pass touchpad pinches through on Linux yet.
  Use Ctrl + two-finger scroll to zoom. Pinch works on touchscreens.
- The macOS app isn't signed with an Apple Developer ID or notarised, so the first time you open it macOS refuses: go
  to **System Settings > Privacy & Security** and choose **Open Anyway**.
- Downloads are unsigned, so Windows may show a SmartScreen warning.
- Recast keeps its own settings, so preferences from Trizbort 2.x may need setting again.
- Older versions of Trizbort open Recast maps but ignore labels and draw curved connections straight, as with 2.x.

### Beta testing
- Open, edit, save and reopen copies of your existing maps, and check they look and export as before.
- Try undo and redo after every kind of edit, and Esc during drags.
- Try a trackpad, a mouse and a touchscreen if you have them.
- Run Automap on a transcript, and stop and restart it.
- Export images, PDFs and code, and compile the code in your IF system.
- Report problems at https://github.com/leeoades/trizbort/issues with the version (Help > About), your operating
  system, what you did and, if you can, a small map or transcript that shows it.

### Under the hood
- The map model, editing, automap, exporters and file handling are a separate, UI-free library (`Trizbort.Core`), so
  the app's behaviour can be tested without a screen. About 1,700 automated tests run on Windows, Linux and macOS
  for every change, with code coverage recorded.
- Rendering and exports are checked side by side against the Windows app on 123 test maps.
