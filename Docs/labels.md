# Labels

[Back to the guide](index.md)

Labels are notes on the map: a title, a list of treasures, a reminder of a puzzle. Press **L** to add one where the
mouse is, or right-click and choose **Add Label**. Double-click a label to edit it.

## Formatting with Markdown

Tick **Markdown** to format the text. You don't need to remember the marks: the toolbar puts them in for you.

![The label editor, with a table of treasures and the Add a Link panel open.](images/label-editor.png)

- **B**, **I** and **<>** make the selected words bold, italic or code (**Ctrl+B**, **Ctrl+I**).
- **H** makes a heading, the list buttons make a bulleted or numbered list, and **—** draws a line across.
- **Insert Table** puts in a small table to type over.
- **Add Link...** (**Ctrl+K**) lists the map's rooms or objects. Search, pick one and press **Insert Link**, or click
  a room on the map. Click a room link on the map to go to that room.

On the map, the table above looks like this:

![The treasure label on the map, with links to the rooms.](images/main-window.png)

If you'd rather type the marks:

- **Headings:** `# Treasures`, `## Above ground`.
- **Bold, italic and code:** `**bold**`, `*italic*`, `` `code` ``.
- **Lists:** `- lamp`, or `1. first`. Put two spaces before a line for a list inside a list.
- **A line across:** `---`.
- **A table:** a row a line, with the cells between `|`s, and `|---|---|` under the first row to make it the heading.
- **A link to a room:** `[[Giant Room]]`, or `[[Giant Room|the giant's room]]` to show other words.
- **A web link:** `[the walkthrough](https://...)`.

Rooms with the same name ("Maze") have their number in the link: `[[Maze#42]]`. Right-click a room and choose **Copy
Room Link** to get its link, then paste it into the label.

A Markdown label's text starts at its top left. Older versions of Trizbort show the text as written.

## Style

![The label's Style tab: text colour, outline, background and shape.](images/label-style.png)

Choose the text colour, and turn the outline and background on or off. The shape (straight edges, rounded,
ellipse or octagon) applies to both.

## Size

A label grows as you type, and shrinks back as you delete, but never smaller than you made it. Drag its handles to
resize it yourself.

Labels can be joined to rooms: with nothing selected, drag from one of the label's ports to a room. They appear in
image and PDF exports, but aren't exported to game source. Select, move, copy and delete them as you would rooms.

Older versions of Trizbort ignore labels. Opening a map with labels in them is safe, but saving it there loses the
labels and their joins.

Next: [Automap](automap.md)
