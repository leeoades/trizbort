# 136 Automap: one undo step per move, and Undo while automapping (fixes GitHub #24, I-043)
GitHub #24: pressing Undo while automapping crashed the app with
"InvalidOperationException: Cannot undo or redo inside a transaction" (UndoStack.EnsureNoTransaction), because
`MainWindowViewModel.StartAutomapAsync` opens one "Automap" transaction for the whole run. That also folds the user's
own edits made during the run into the same open step.

## Behaviour
- No long-running transaction. Each batch of map changes automap makes (one transcript block between prompts: a move,
  a new room, connections, exit stubs, a region/object command) is its own undo step, titled e.g.
  "Automap: Garden, North" (the room it moved to or created) or "Automap" when there is no room.
- The user can edit, undo and redo while automap runs; their edits are their own steps.
- Undo/redo restore a whole-map snapshot (new Room objects), and the user may delete rooms. Before using its remembered
  current room, the engine must look it up again by element id in the current project; if gone, behave as if there is
  no known current room (next room is placed unconnected, like a teleport).
- Changes must be applied on the UI thread as today (AvaloniaAutomapCanvas marshals) and steps recorded there.
