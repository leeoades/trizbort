# Getting started

[Back to the guide](index.md)

## Download and install

Get the latest release from the [Releases page](https://github.com/leeoades/trizbort/releases). The downloads
include everything they need: there's no separate .NET to install.

- **Windows:** extract `TrizbortRecast-<version>-win-x64.zip` into a folder you can write to, and run
  `TrizbortRecast.exe`. The app isn't signed yet, so Windows SmartScreen may warn you the first time: choose
  **More info**, then **Run anyway**.
- **Linux:** extract `TrizbortRecast-<version>-linux-x64.tar.gz` and run `trizbort-recast`. To open `.trizbort` files
  from your file manager, see [packaging/README.md](https://github.com/leeoades/trizbort/blob/master/packaging/README.md).
- **macOS:** download `…-osx-arm64.zip` (Apple Silicon) or `…-osx-x64.zip` (Intel), unzip it and drag
  `Trizbort Recast.app` to Applications. The first time, macOS refuses to open it: go to
  **System Settings > Privacy & Security** and choose **Open Anyway**.

Recast doesn't update itself. Check the Releases page for new versions.

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
