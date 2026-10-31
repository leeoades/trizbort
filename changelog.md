# Trizbort Recast change log

Trizbort Recast is a cross-platform rebuild of [Trizbort](https://github.com/JasonLautzenheiser/trizbort), the
interactive fiction map editor first written by [Genstein](https://github.com/genstein) and continued by
[Jason Lautzenheiser](https://github.com/JasonLautzenheiser) and contributors. It opens and saves the same `.trizbort` maps.

This file lists what Recast changes compared with Trizbort 1.7.4.1, the last release before this fork. The Windows-only
app's own history, from Trizbort 1.x through this fork's 2.x releases, is in [legacy/changelog.md](legacy/changelog.md).

Recast version numbers start again at 0.9 and follow [semantic versioning](https://semver.org/). Releases are tagged
`recast-vX.Y.Z`; headings here leave out the prefix.

## [Unreleased]

## [0.9.0-beta.1]

This is the **first beta of Trizbort Recast**, for testing. It has everything the Windows app has, and more, but it has
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
  the app's behaviour can be tested without a screen. About 1,700 automated tests run on Windows and Linux.
- Rendering and exports are checked side by side against the Windows app on 123 test maps.
