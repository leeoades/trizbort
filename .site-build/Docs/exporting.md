# Exporting

[Back to the guide](index.md)

Everything is under **File > Export**. An export writes a whole new file: exporting over a file you've changed since
replaces it, changes and all.

## Pictures

- **Image...** saves the map as a picture. PNG is best for maps: sharp, and small for the flat colours of a map. JPEG
  blurs the lines a little, and BMP files are large.
- **PDF...** saves the whole map as one page, as big as it needs to be. It's drawn with lines rather than dots, so it
  stays sharp however far you zoom in, and you can search its text. Rooms with descriptions carry a note you can open.

A little space is left round the map so nothing at its edge is cut off. To choose how much, set margins for this map
in **Map Settings > Rooms & Editing**, or for every map in **App Settings > General** (**Specify margins**).

**Smart Save** (**Ctrl+Alt+S**) saves the map, then a PDF and an image of it beside the map file, with the same name,
in one go. Choose which, and the image type, in [App Settings > General](settings.md#app-settings). A map that's never
been saved asks for a name first.

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

What goes in:

- **Rooms**, each with its description, whether it's dark, and which is the start room. Inform 7 groups them by
  region.
- **Objects**, each in its room, or inside the object above it when its line starts with **-**, with the properties
  in square brackets ([see Objects](map.md#objects)).
- **Ways between rooms.** A join's direction is read from the ports it uses, to the nearest compass point (a join from
  the east-northeast port goes east), or from its up, down, in or out label. Doors marked in Join Properties become
  doors in Inform 7 and Inform 6.
- **Names** are made safe for each language ("The Porch", "West of the House"), and rooms or objects with the same
  name each get a name of their own in the code.

Labels are left out: they're notes for you, not part of the game. So are joins that don't reach a room at both ends.

The code is a start, not a finished game: it should build as it is, but you may need to touch some of it up. Inform 7
is the most tried. If an export doesn't build, [let us know](https://github.com/leeoades/trizbort/issues) with the
map.

### Inform 7

Inform 7 keeps a project in a folder ending `.inform`. Export to a name ending `.inform` and Recast makes a new
project that Inform opens, with the code in `Source/Story.ni` inside it. To write just the code, choose the
**Inform 7 Source Files** file type and a name ending `.ni`.

### TADS

The export writes a `.t` file. Turn on **Save TADS to ADV3Lite** (App Settings > General) to write it for the adv3Lite
library, with regions.

## Statistics

**Project > Map Statistics...** counts the rooms, joins and objects. **Export Map Statistics...** saves them to a
file.

## Editing a map file by hand

A `.trizbort` file is XML, so you can open it in a text editor to change a lot at once: replace a word in every room,
or swap one colour for another. Keep a copy first, as Recast may not open a file it doesn't expect.

Next: [Keyboard shortcuts](keyboard.md)
