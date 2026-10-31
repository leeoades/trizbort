# 130 — Room validation indicators on the map and in tooltips

Matrix rows: A011          Priority: P1          Size: S
Depends on: 036, 065          Suggested tier: mid

## What
With project validation rules on (Edit > Validation, ticket 036: unique names, description, subtitle, no dangling connections), the reference marks every room that breaks a rule. Core already computes `Room.ValidationState` via `CheckValidation()`, but Avalonia never shows it, so the toggles have no visible effect. Found by the integrator's matrix review.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Elements/Room.cs`:
  - `Draw` calls `CheckValidation()` first (~line 680).
  - When invalid it draws a red 2-pt solid cross through `InnerBounds` (both diagonals), after the room (~lines 1024-1045).
  - `GetToolTipColor` returns red when invalid, else light blue (~1122).
  - `GetToolTipHeader` appends " - Room validation issues:" and the body lists each `ValidationState` message, one per line (~1132-1152).
- `src/Trizbort.Core/Domain/Elements/Room.cs` — the ported `CheckValidation`, `ValidationState`, `GetToolTipColor`, `GetToolTipHeader`.
- `src/Trizbort.Avalonia/Drawing/RoomDrawingAdapter.cs` and `Controls/ElementToolTipFactory.cs` (ticket 065).

## Implement
1. Make sure `CheckValidation()` runs before a room is drawn (screen, image and PDF export all use `RenderMap`) and before its tooltip is built. Cheapest correct place: the drawing adapter and the tooltip model, not every edit.
2. Draw the red cross exactly as the reference (InnerBounds diagonals, 2 px, solid) on top of the room.
3. Tooltip: red colour and the validation messages, matching the reference header/body.
4. Toggling a validation rule (ticket 036 commands) must redraw the map.

## Tests
- Core: `CheckValidation` produces the expected states for each rule on/off (if not already covered).
- Avalonia: render a duplicate-name pair with the unique-names rule on and check a red pixel on the room's diagonal; off → no red. Tooltip model for an invalid room has the red colour and lists the message.

## Done when
- Build + Core + Avalonia tests pass; matrix A011 updated; committed as `parity(130): room validation indicators`.
