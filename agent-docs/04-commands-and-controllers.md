# Commands & Controllers (and the absence of Undo/Redo)

See [`README.md`](README.md) for the doc map.

## There is no undo/redo in this codebase

This is the single most important fact in this document. A repo-wide search for `Undo`/`Redo`
turns up nothing — no command history, no inverse-command stack, no transaction/batch-edit
API. `Project.History` (`Domain\Application\Project.cs`) is a free-text changelog **document
property** (think "map changelog shown to players"), not an edit-history mechanism. If a task
asks you to "add undo support" or assumes undo exists, that's new, nontrivial infrastructure —
flag it rather than assuming a hook already exists somewhere you haven't found yet.

## `Domain\Commands\` — lightweight execution interfaces, not a command pattern with history

`Domain\Commands\ICommand.cs` defines plain, fire-and-forget execution contracts:

```csharp
ICommand<T>                         // Execute()
IParameterizedCommand<T, Value>     // Execute(Value) -> result
IParameterizedCommand<V>            // Execute(V)
ICanvasCommand<T, V>                // canvas-flavored, returns a value
ICanvasCommand<V>                   // canvas-flavored, void
```

There's no `Undo()`/`Redo()` on any of these. The only concrete implementation is
`SelectCommand.cs`, which wraps `Canvas` selection operations (select all / rooms / connections
/ unconnected rooms / dangling connections / self-looping connections / rooms with or without
objects / regions) behind this interface. Treat this folder as a thin **dispatch shape** for
selection, not a general edit-command framework — most editing logic lives directly in
controllers, dialogs, and `Canvas`, not behind `ICommand`.

## `Domain\Controllers\` — the façade between UI and domain

Five controllers sit between `MainForm`/`Canvas` and the domain model. None of them implement
undo; they're synchronous façades that mutate `Project.Current`/`Canvas` state directly.

- **`CommandController`** — the main menu/command façade `MainForm` talks to. Covers: bring/send
  element forward-back, selection, connection flow/style/label, start/end room assignment, room
  lighting/shape, validation settings, opening property dialogs. If you're adding a new menu
  item that mutates the map, this is usually where the handler method belongs.
- **`CanvasController`** — canvas-facing operations: ensure element visible, select elements,
  select start room, select the room nearest the viewport center, set connection
  flow/label/style.
- **`RoomController`** — room-specific bulk operations on the current selection:
  `ForceDarkness`, `ForceLighted`, `SetRoomShape`, `ToggleDarkness`.
- **`ElementController`** — tiny façade, just `ShowElementProperties(Element)` (opens the right
  properties dialog for a `Room` vs `Connection`).
- **`CopyController`** — copy/paste. Converts selected rooms/connections into JSON DTOs
  (`CopyRoomObj`/`CopyConnectionObj`/`CopyVertexObj`/`CopyColorsObj`) for the clipboard and
  reconstructs them on paste, preserving vertex owner IDs, port IDs, positions, door data, and
  visual properties. See [`03-storage-and-persistence.md`](03-storage-and-persistence.md) for
  how this relates to the JSON converters in `Domain\SerializeHelpers`.

`CanvasController` has an internal Canvas-injecting constructor; `CommandController` passes
its bound Canvas consistently rather than looking up the global main form. The public legacy
constructor remains for existing callers. This permits command/selection integration tests
with a real Canvas without constructing the application shell.

Continuous movement/resize behavior is shared through internal `Domain\Misc\MapEditing`
(see the Canvas reference), not through a new command-history/MVP framework.

## Practical implication for making changes

- Adding a new **menu command**: wire the menu item in `UI\MainForm.Designer.cs`/`MainForm.cs`,
  implement the actual logic in (or call out to) `CommandController` — see
  [`05-ui-and-canvas.md`](05-ui-and-canvas.md) for the UI side of this.
- Adding a new **selection mode**: extend `SelectTypes` (`Domain\Enums\SelectTypes.cs`) and
  `SelectCommand`/`CanvasController.SelectElements`.
- **Don't** assume an edit you make is automatically undoable, reversible, or transactional —
  if a feature needs that, it has to be built, not hooked into something existing.
- Direct mutation of `Project.Current.Elements`/`Room`/`Connection` properties *outside* the
  controllers is normal and expected in this codebase (e.g. `Canvas` drag handling and
  `Automap` both do it directly) — the controller layer is a convenience façade for
  menu/command-driven edits, not a mandatory gatekeeper.
