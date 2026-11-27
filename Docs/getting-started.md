# Getting started

[Back to the guide](index.md)

## Download and install

Get the latest release from the [Releases page](https://github.com/leeoades/trizbort/releases). The downloads
include everything they need: there's no separate .NET to install. They aren't signed yet, so Windows and macOS warn
about them the first time: see [why, and how you can help](signing.md).

Each release's notes start with a **Download** table: the one file for your computer, with a link.

These keep themselves up to date (see [Updates](settings.md#updates)):

- **Windows:** run `TrizbortRecast-win-x64-Setup.exe`. It installs Recast for you alone (no administrator needed),
  adds it to the Start menu, and opens `.trizbort` files with it. The app isn't signed yet, so Windows SmartScreen may warn you: choose
  **More info**, then **Run anyway**.
- **Linux:** download `TrizbortRecast-linux-x64.AppImage`. It's one file that works on any 64-bit Linux
  distribution (Ubuntu, Debian, Mint, Fedora, Arch and so on), so there's nothing to install. Let it run (in its
  **Properties > Permissions**, or `chmod +x`), then double-click it. If it won't start, your system may need FUSE:
  on Ubuntu, `sudo apt install libfuse2t64`.
- **macOS:** download `TrizbortRecast-osx-arm64.dmg` (Apple Silicon) or `TrizbortRecast-osx-x64.dmg` (Intel), open
  it and drag Trizbort Recast to Applications. (The `-Portable.zip` of the same name is the same app, zipped.) The first time, macOS refuses to open it: go to
  **System Settings > Privacy & Security** and choose **Open Anyway**.

Or take a plain copy that you update by hand:

- **Windows:** extract `TrizbortRecast-<version>-win-x64.zip` into a folder you can write to, and run
  `TrizbortRecast.exe`. The app isn't signed yet, so Windows SmartScreen may warn you the first time: choose
  **More info**, then **Run anyway**.
- **Linux:** extract `TrizbortRecast-<version>-linux-x64.tar.gz` and run `trizbort-recast`. To open `.trizbort` files
  from your file manager, see [packaging/README.md](../packaging/README.md).
- **macOS:** download `…-osx-arm64.zip` (Apple Silicon) or `…-osx-x64.zip` (Intel), unzip it and drag
  `Trizbort Recast.app` to Applications. The first time, macOS refuses to open it: go to
  **System Settings > Privacy & Security** and choose **Open Anyway**.

The plain copies don't update themselves: **Help > Check for Updates...** points you to the Releases page.

## The Recast window

![Recast automapping The Lighthouse.](images/main-window.png)

- **The menus and toolbar** along the top. Most commands are in the menus, with their keys beside them.
- **The map** fills the window. North is up, east is to the right. Rooms are boxes with their names in them; joins
  are the lines between them, the ways from one room to another. A dotted join is a way you're not sure of, and an
  arrow marks a way you can only go one way. A room with a dark corner is dark: you'll need a light in the game.
- **Objects** are listed beside a room (below it, to start with), or inside it.
- **The minimap**, top right, shows the whole map, with a box round the part in the window.
- **The zoom** is in the bottom-left corner of the map, and the **status bar** along the bottom says what's happening.
- **Panels** open on the right while you need them: the automap panel (as here) and directions.

In this guide, **Edit > Map Settings...** means the **Map Settings...** command on the **Edit** menu, and **Ctrl+S**
means hold **Ctrl** and press **S**. On a Mac, use **Cmd** for **Ctrl** and **Option** for **Alt**.
In dialogs on Windows and Linux, **Alt** with an underlined letter goes to that box or button.

## Your first map

1. **Add a room.** Double-click an empty part of the map, or press **R**. Type its name and press **Enter**.
2. **Add another**, a little to the east of the first.
3. **Join them.** Hover over the first room: small dots (its *ports*) appear round its edge. Drag from the east dot
   to the second room. That's a join: a way from one room to the other.
4. **Save.** Press **Ctrl+S** (**Cmd+S** on a Mac).

Made a mistake? **Ctrl+Z** undoes it, and **Ctrl+Y** puts it back. Every change can be undone.

## Getting around the map

- **Pan:** drag with the right or middle mouse button, hold **Space** and drag, or scroll with two fingers on a
  trackpad.
- **Zoom:** the mouse wheel, **Ctrl+Plus** and **Ctrl+Minus**, or pinch on a trackpad. **Ctrl+Home** fits the whole
  map in the window.
- **The minimap** in the corner shows the whole map. Click or drag in it to move there.
- **Quick Find** (**Ctrl+F**) finds a room by name.

## Mapping a game for you

If your game's interpreter can save a transcript, Recast can draw the map while you play. See [Automap](automap.md).

Next: [A quick map, step by step](quick-map.md)
