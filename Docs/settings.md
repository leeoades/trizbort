# Settings

[Back to the guide](index.md)

There are two kinds of settings:

- **App settings** (Edit > Application Settings..., **Ctrl+,**) are yours: they apply to every map you open.
- **Map settings** (Edit > Map Settings..., **Ctrl+Shift+,**) are saved in the map, so anyone who opens it sees it the
  same way.

## App settings

![Application Settings, General tab.](images/app-settings-general.png)

The **General** tab has your preferences: whether to open the last map when Recast starts, keep a recovery copy of
unsaved changes (on by default), and how the mouse wheel and trackpad zoom and scroll.

![Application Settings, Map tab: the token chooser.](images/app-settings-map.png)

The **Map** tab chooses the token that marks where you are while automapping: a wooden disc, a brass coin, a pawn, a
"you are here" sign, painted or pewter figures, or **your own** (a PNG with a transparent background, or an SVG).
Untick **Token on the room** to hide it. **Glow round the room** sets the colour of the glow.

**Apply style to new rooms** makes each new room copy the look of the last room you selected.

## Map settings

| Tab | What it sets |
|---|---|
| Map Details | The map's title, author, description and history |
| Lines & Grid | Line width and arrows, how joins are labelled, and the grid |
| Rooms & Editing | The default room shape, where object lists go, handle sizes |
| Colours | The colours of the canvas, rooms, joins and text |
| Fonts | The fonts for room names, subtitles, objects and join labels |
| Regions | Regions and their colours |
| Styles | The label styles |

![Map Settings, Lines & Grid: the join labels, with a preview.](images/map-settings-lines.png)

**Direction words** chooses how join labels write directions: "n" or "north", or as written. The preview shows the
result as you change it.

![Map Settings, Rooms & Editing: drag the object list to where it should go.](images/map-settings-rooms.png)

![Map Settings, Colours.](images/map-settings-colours.png)

## Label styles

![Map Settings, Styles: Subtitle is based on Title.](images/map-settings-styles.png)

The **Styles** tab lists the map's label styles. Every map has **Default**. **New Style** makes one **based on** the
style that's selected: it looks the same until you change something. What it doesn't set comes from the style it's
based on, and the tab says where ("from Title"). **Reset** takes a value from there again. Delete a style and its
labels take the style it was based on.

Styles are saved in the map. Older versions draw each label as it looks now.

## Themes

A theme changes the whole look of a map in one go: Classic, Parchment, Dark, High contrast or Sketch. Choose
**Edit > Map Theme...**.

![Map Theme: the built-in themes.](images/map-themes.png)

Tick **Reset individual room/line styles** to also clear colours you've set on single rooms and joins. You can share
a theme with **Export Selected Theme...** and **Import Theme...**. Applying a theme is one undo step.

Next: [Exporting](exporting.md)
