# 086 — Quick Find: search rooms and jump to the match

Matrix rows: D010          Priority: P?          Size: M
Depends on: 002, 009, 010, 070           Suggested tier: mid

## What
Ctrl+F (and a new Edit > Find menu item) opens Quick Find: type text, Enter/Find selects every room whose name, description, objects or subtitle contains it (case-insensitive) and centres the view on the first. Empty text does nothing. Esc/Cancel/focus loss closes it.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/NoCounterpart.md` §D010.
- `$REF/src/Trizbort/UI/QuickFind.cs` (whole, ~110 lines) and `QuickFind.Designer.cs` (grep `Text =`).
- `src/Trizbort.Core/Domain/Cache/Indexer.cs`, `FindCacheItem.cs`.
- `agent-docs/plans/avalonia-parity/shortcut-map.md` — gesture for Find (`Primary+F`).

## Implement
1. Core: `src/Trizbort.Core/Editor/QuickFind.cs` — `IReadOnlyList<Room> Search(Project, string text)` (distinct rooms in encounter order, culture-invariant case-insensitive substring over Name, primary Description, Objects, Subtitle; empty/whitespace -> empty) and `IEnumerable<string> Suggestions(Project)` (distinct, sorted) using `Indexer`.
2. Core: editor command `find.quick` in the catalogue (009), scope Window, gesture per shortcut-map, `ExecuteAsync` asks `IEditorPrompts.QuickFindAsync(suggestions)` which returns the text; then `session.Selection.Set(matches)` and `session.Viewport.CentreOn(matches[0].Bounds centre)` (no zoom change). No undo step (selection only).
3. Avalonia: `Views/Dialogs/QuickFindDialog.axaml` per legacy: title "Quick Find", label "Enter room name, description, or objects", `AutoCompleteBox` over suggestions, buttons "Find"/"Cancel", Enter = Find, Esc closes, closes on deactivate; centred on owner. Implement `IEditorPrompts.QuickFindAsync` with it. (Overlay-bar alternative is a later enhancement; keep the dialog for parity.)
4. Add Edit > "Find..." through the catalogue only (no hand-written menu).
5. No match: status bar message "No rooms match '<text>'" if a status service exists; otherwise just clear selection as legacy.

## Tests
- Core: `src/Trizbort.Core.Tests/QuickFindTests.cs` — each field matches, case-insensitive, de-duplicated, empty returns none; command selects matches and moves viewport origin; no undo entry.
- Avalonia: `DialogViewModelTests.cs` — dialog returns text; Enter/Find/Esc.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D010 -> Implemented.
- Committed as `parity(086): quick find`.

## Out of scope
- Next/previous match, live result list, connection search.
