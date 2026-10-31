# 082 — Automap settings dialog: groups, intro and hint text, access keys, no clipping

Matrix rows: D007          Priority: P?          Size: M
Depends on: 070           Suggested tier: mid

## What
The Automap dialog regains its intro sentence, "Options" and "Commands" group boxes, the "At the in-game prompt:" line with the two "followed by ..." hints, legacy checkbox wording, access keys and legacy field order, and a visible reason when Start is disabled. The same-direction and disambiguation prompts are NOT part of this ticket (012).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/Automap.md` §1 (field table, checklist, recommendation); ignore §2-3 (ticket 012).
- `$REF/src/Trizbort/UI/AutomapDialog.Designer.cs` — grep `Text =`, `TabIndex`.
- `$REF/src/Trizbort/UI/AutomapDialog.cs` (40-76: load, verbose/same-name coupling).
- `src/Trizbort.Avalonia/Views/Dialogs/AutomapSettingsDialog.axaml(.cs)`, `ViewModels/AutomapSettingsViewModel.cs` (108, 144-158, 175), `Services/DialogService.cs:340-350`.

## Implement
1. Axaml (070 styles): window title "Automapping"; intro `hint` text "Trizbort can automatically generate a map from a transcript of a game, even while the game is in progress."; row "_Transcript File" + box + "_Browse..."; group "_Options": `Treat transcript as _VERBOSE; expect room descriptions.`, `_Assume no two rooms have the same name.` (indented under verbose), `_Guess possible exits from a room by reading its initial description.`, `_Start processing from the end of the transcript file.`, `Always assume two-way connections.` — legacy order; group "_Commands": "At the in-game prompt:" then `>` + [box] + "followed by an object name adds it to the map." and `>` + [box] + "followed by region name to add room to region."; buttons "Start Automapping" / "Cancel".
2. Same-name rule: keep Avalonia load behaviour; additionally when verbose is turned ON by the user, reset same-name to unchecked (legacy `AD:73-75`) — decide: implement (parity), note it.
3. Start disabled reason: red `error` text under the file box "Choose a transcript file" shown while blank; hide once set.
4. Default size so Commands rows are not clipped (about 520x520; check `AssertNoClipping`); `SizeToContent=Height` is acceptable.
5. Tooltip on the two command boxes with an example ("tb see lamp"). Single Step: Won't-port (debug only) — leave out.

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/AutomapIntegrationTests.cs` or `DialogViewModelTests.cs` — coupling rules; Start disabled message; no-clipping.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D007 -> Implemented; Light/Dark checked.
- Committed as `parity(082): automap settings dialog`.

## Out of scope
- Prompt dialogs (012); automap undo grouping (decided elsewhere).
