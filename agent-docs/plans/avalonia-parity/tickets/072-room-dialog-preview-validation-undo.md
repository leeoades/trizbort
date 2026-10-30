# 072 — Room dialog: shape preview, validation, dependencies, last tab, OK as one undo step

Matrix rows: D001          Priority: P?          Size: M
Depends on: 070, 071, 002, 003           Suggested tier: mid

## What
Room Properties restores its live shape preview, region colour swatches, name validation, the "already the start room" confirmation, hides the Rounded Corners group unless that shape is chosen, remembers its last tab, and pressing OK applies every change as a single undo step.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/RoomProperties.md` §2 checklist (Enable/disable, Defaults, Validation, Previews) and Recommendation (5)-(8).
- `$REF/src/Trizbort/UI/RoomPropertiesDialog.cs` — `OkButtonClick` (validation, start-room prompt ~530-546), sample shape drawing 717-770, region owner-draw 809-831, `LastClosedTab`, corner visibility 462-478.
- `src/Trizbort.Avalonia/Views/Dialogs/RoomPropertiesDialog.axaml(.cs)`, `ViewModels/RoomPropertiesViewModel.cs` (`ApplyTo` ~line 203), `Services/DialogService.cs:224-239`.
- `src/Trizbort.Avalonia/Drawing/RoomDrawingAdapter.cs` — to draw the preview.

## Implement
1. Core/VM validation: `RoomPropertiesViewModel.Validate()` returns message or null — name empty -> "The room name can't be empty", no letter -> "The room name must contain at least one letter" (exact legacy text from `OkButtonClick`). Show in `TextBlock.error` under Name; OK click with an error focuses Name and does not close.
2. Start room: when `IsStartRoom` is ticked and another start room exists, OK asks via `IEditorPrompts.ConfirmAsync` the legacy question ("Room X is already the start room. Change?"); No unticks only the flag and keeps the dialog open (check legacy behaviour). Use prompts, not a message box.
3. `IsVisible` of the Corner Radii group bound to `Shape == RoundedCorners`; max radius per legacy (30) unless Core/renderer tolerates 100 (verify in `RoomDrawingAdapter`; if tolerated keep 100 and note it).
4. Shape preview: small `Control` (`RoomShapePreview`) in the Shapes tab drawing a sample room with current shape, corner radii, border style and hand-drawn setting via `RoomDrawingAdapter`; redraw on VM property changes. Region combo items show a colour swatch (ItemTemplate with Rectangle bound to region fill).
5. Remember last tab: `Settings` (UI-side `DialogState` in app settings is fine) key `RoomDialogLastTab`; `DialogService.ShowRoomPropertiesAsync(room, startTab?)` accepts an optional start tab (Regions / Shapes) as the reference `PropertiesStartType`.
6. OK through the session: replace the direct `ApplyTo(room)` call with `session.Execute("Edit room properties", () => vm.ApplyTo(room))` (one transaction = one undo step, see decisions D1 "per dialog OK"). Cancel makes no change and no undo entry. Via `IEditorPrompts.EditElement(room)` when 002 provides it.

## Tests
- Avalonia: `DialogViewModelTests.cs` — validation messages, start-room confirmation (fake prompts), corner group visibility, last-tab persistence.
- Core: `src/Trizbort.Core.Tests/EditorSessionDialogTests.cs` — executing an `ApplyTo` transaction changes N properties and one `Undo()` restores all; Cancel adds no step.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D001 -> Implemented (or Partial with remaining items listed).
- Committed as `parity(072): room dialog preview, validation, undo`.

## Out of scope
- Objects text editing (073). Fixing "ignored" settings.
