# Exporting

[Back to the guide](index.md)

Everything is under **File > Export**.

## Pictures

- **Image...** saves the map as a PNG, JPEG or BMP picture.
- **PDF...** saves the whole map as one page. It's drawn with lines rather than dots, so it stays sharp however far
  you zoom in, and you can search its text. Rooms with descriptions carry a note you can open.

**Smart Save** (**Ctrl+Alt+S**) saves the map, then a PDF and an image of it beside the map file, in one go. Choose
which in [App Settings > General](settings.md#app-settings).

## Game source

Writing your own game? Recast can write the map out as a starting point, with the rooms, their descriptions, the
objects in them and the ways between them:

| Language | Save to a file | Copy to the clipboard |
|---|---|---|
| Inform 7 | Ctrl+Shift+7 | Ctrl+Alt+7 |
| Inform 6 | Ctrl+Shift+6 | Ctrl+Alt+6 |
| TADS | Ctrl+Shift+T | Ctrl+Alt+T |
| Hugo | Ctrl+Shift+H | Ctrl+Alt+H |
| ZIL | Ctrl+Shift+I | Ctrl+Alt+Z |
| Quest | Ctrl+Shift+Q | Ctrl+Alt+Q (without the header) |
| Alan | Ctrl+Shift+L | Ctrl+Alt+L |
| Adventuron | | Ctrl+Alt+A |

On a Mac, use Cmd for Ctrl and Option for Alt. Quest to a file is Cmd+Option+Shift+Q, and Hugo to the clipboard is
Cmd+Option+Shift+H, as macOS uses the shorter keys itself.

Labels are left out of game source: they're notes for you, not part of the game.

## Statistics

**Project > Map Statistics...** counts the rooms, joins and objects. **Export Map Statistics...** saves them to a
file.

Next: [Keyboard shortcuts](keyboard.md)
