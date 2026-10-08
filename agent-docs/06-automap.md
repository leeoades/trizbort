# Automap: Transcript-Based Map Generation

See [`README.md`](README.md) for the doc map. For what the generated `Room`/`Connection`
objects are, see [`02-domain-model.md`](02-domain-model.md).

## What it does

Automap reads a raw IF game transcript (the text a player would see, including their typed
commands) and infers rooms, descriptions, and connections between them, populating
`Project.Current.Elements` automatically instead of the user drawing the map by hand.

## Files

- `Automap\Automap.cs` — the parser/state machine. All the actual heuristics live here.
- `Automap\AutomapSettings.cs` — configuration (see table below).
- `Automap\IAutomapCanvas.cs` — abstraction the parser uses to find/create/connect/select/
  remove rooms, implemented by `UI\Controls\Canvas.Automap.cs`. It keeps most map-mutation calls
  independent of WinForms. Production `Automap.cs` still shows
  `AutomapRoomSameDirectionDialog` and transcript I/O error message boxes, through callbacks
  supplied by its default constructor; an internal constructor accepts deterministic decisions/
  error reporting for tests. Same-name disambiguation still belongs to Canvas, so this is not a
  complete UI-independence boundary.
- `Automap\Utility\PeekingStreamReader.cs` — stream reader with lookahead, used so the parser
  can process a transcript that's still growing (live/continuing transcript mode).

## Entry points

```
UI\AutomapDialog.cs
  → Canvas.StartAutomapping(...)              UI\Controls\Canvas.Automap.cs
      justParseFile == false → Automap.Start(...)     // live/interactive transcript processing
      justParseFile == true  → Automap.StartCL(...)   // one-shot full-file parse (also used by CLI, see 08-cli-and-entry-point.md)
```

## Pipeline

1. Read transcript text and split into chunks between **prompt** lines
   (`IsPrompt` — recognizes a `>` marker within the first 42 characters of a line and extracts
   the player's typed command).
2. `ProcessPromptCommand` interprets the typed command: is it a recognized movement direction,
   or a Trizbort-specific control command (see "custom commands" below)?
3. `ProcessTranscriptText` scans the game's output text (between prompts) for a room name,
   optionally followed by a description paragraph.
4. `FindRoom` asks `IAutomapCanvas.FindRoom` (→ `Canvas.Automap.cs`) whether a matching room
   already exists.
5. Depending on match result + last movement direction: create a new room and connect it to the
   previous room, connect to an existing room, or (if ambiguous) ask the user via a dialog.
6. The current room (`m_lastKnownRoom`) and last movement direction (`m_lastMoveDirection`)
   are tracked across iterations to know what to connect to what.

   Each run initializes room, direction, game-title and stepping state. One-shot parsing processes
   the final chunk at EOF (a trailing prompt is not required). I/O/access errors report a halted
   status rather than subsequently claiming completion. Both entry points own cancellation tokens;
   cancellation propagates out of single-step waits and buffered transcript processing before any
   following prompt command is applied. Stopping during ambiguity callbacks also aborts before
   applying the returned decision. Cancellation leaves the status "Automap is not running." and
   clears the owned cancellation source when finished. Keeping the
   existing room in a same-direction conflict makes that room the current source for subsequent
   travel. Regression tests exercise all three conflict decisions with real Canvas graph mutations,
   sequential runs, reader lookahead, cancellation and save/load/export workflows. Replacement
   runs cancel their predecessor in both entry points; canceled cleanup changes status and clears
   the token only if it still owns the current run. Tokens are installed before opening files so
   even a failed replacement cannot leave its predecessor owning the run. Regression tests cover
   replacement waiting, completion and file-open failures, with and without an explicit Stop.
   Arbitrary concurrent processing and native dialog interaction are not covered.

## Room/description detection heuristics (no regex — rule-based)

`ExtractRoomName` treats a line as a room name if, among other checks:

- the previous line was blank or a prompt,
- the line itself is non-blank, non-indented,
- it starts and ends with a letter/digit,
- a leading letter must be capitalized,
- each space-separated word starts with a letter, digit, or `#`,
- longer words must generally be capitalized,
- an all-uppercase line is rejected (treated as a banner/game-over text, not a room name).

Decorative suffixes (starting at `,`, `(`, `[`, `{`, or `" - "`) are stripped from the detected
name, e.g. `Bedroom, on the bed` → room name `Bedroom`.

`ExtractParagraph` then greedily collects the following non-blank lines (stopping at a blank
line, a prompt, another detected room name, or a short line under 65 chars) and joins them with
spaces as the room description. The literal string `[Previous turn undone.]` is explicitly
ignored so transcript "undo" noise doesn't get treated as content.

## Direction/exit inference

Recognized movement words (with common abbreviations), handled by `ProcessPromptCommand`:

| Direction | Words |
|---|---|
| North/South/East/West | `north`/`n`, `south`/`s`, `east`/`e`, `west`/`w` (nautical: `fore`/`f`, `aft`/`a`, `starboard`/`sb`, `port`/`p`) |
| Diagonals | `northeast`/`ne`, `southeast`/`se`, `southwest`/`sw`, `northwest`/`nw` |
| Vertical/interior | `up`/`u`, `down`/`d`, `in`/`inside`, `out`/`outside` |

Filler words (`the`, `a`, `to`, `on`) and leading verbs (`go`, `walk`, `move`) are stripped
before matching.

When a new room description follows a recognized movement:
- **new room** → create it positioned in that direction from `m_lastKnownRoom` and connect them;
- **existing room** (per `FindRoom`/`Match`) → connect the existing room in that direction
  instead of creating a duplicate.

If no movement direction was known (e.g. transcript starts mid-document, or the player teleported), a newly
encountered room is treated as disconnected (optionally marked as the start room if it's the
first one seen). **Known limitation**: compound commands in one line (e.g. `go east. n. s then
w.`) are explicitly not handled — only a single movement per prompt line is parsed.

Independently of movement, if `GuessExits` is enabled, `DeduceExitsFromDescription` scans room
*description* text for direction words (`north`, `southward`, `port`, `starboard`, `upward`,
`below`, `all directions`, `every direction`, etc.) and adds exit stubs even without an actual
observed movement — useful for a single room description that says "exits lead north and east."

## Ambiguity resolution (user-in-the-loop)

Two distinct ambiguous situations, each with its own dialog:

1. **Same room name, different description** (`Match` returns `null` = "possibly the same
   room") → `UI\DisambiguateRoomsDialog.cs` shows transcript context and candidate rooms. User
   picks an existing room, chooses "new room", or asks to stop being prompted (after which the
   first candidate is used automatically for the rest of the run).
2. **A room already exists in the direction just moved** → `UI\AutomapRoomSameDirectionDialog.cs`
   asks whether to keep the existing room, replace it with the newly detected one, or keep both.

If `AssumeRoomsWithSameNameAreSameRoom` is enabled, case 1 never prompts — same-named rooms are
always treated as the same room.

## Settings (`Automap\AutomapSettings.cs`)

| Setting | Effect |
|---|---|
| `VerboseTranscript` | Whether the transcript includes full room descriptions (affects whether description text is used for same-name room matching). |
| `AssumeRoomsWithSameNameAreSameRoom` | Skip the disambiguation dialog for same-named rooms; auto-enabled by the dialog when `VerboseTranscript` is turned off. |
| `GuessExits` | Enable/disable `DeduceExitsFromDescription` exit-stub inference. |
| `AssumeTwoWayConnections` | Whether moving from A→B also creates the implicit B→A connection. |
| `ContinueTranscript` | Live mode: start at the end of an existing transcript file and wait for new text to be appended, rather than parsing the whole file once. |
| `SingleStep` | Diagnostic mode: pause between each processing step. |
| `AddObjectCommand` | Custom in-transcript command (default `tb see`) that adds an object to the current room. |
| `AddRegionCommand` | Custom in-transcript command (default `tb region`) that assigns/creates a region for the current room. |

These (transcript filename, verbosity, same-name matching, exit guessing, the two custom
commands, **and** `SingleStep`/`ContinueTranscript`/`AssumeTwoWayConnections`) are all persisted:
`ApplicationSettings.Automap` is a plain `AutomapSettings` struct field, and
`ApplicationSettingsController.SaveSettings()` serializes the whole `ApplicationSettings` object
to `appsettings.json` with `JsonConvert.SerializeObject` — every public field of the struct goes
in, not just the subset the legacy-XML migration path (`loadLegacyAppSettings()`) happens to map.
`UI\AutomapDialog.cs`'s `Data` property round-trips all nine fields to/from its controls
(`SingleStep` ↔ `m_singleStepCheckBox`, `ContinueTranscript` ↔ `m_startFromEndCheckBox`,
`AssumeTwoWayConnections` ↔ `chkAssumeTwoWayConnections`, etc.) — see
[`03-storage-and-persistence.md`](03-storage-and-persistence.md) for the broader settings picture.

## Debugging a bad automap result

- If a room wasn't detected at all: check `ExtractRoomName`'s rules against the exact transcript
  line (capitalization, leading whitespace, trailing punctuation are the most common causes of a
  rejected match). Debug builds can surface a failure reason for skipped lines.
- If two rooms were merged that shouldn't have been, or vice versa: check `Match`'s
  name/description comparison and whether `AssumeRoomsWithSameNameAreSameRoom` is set.
- If an exit/connection is missing or wrong: check whether the move command was recognized by
  `ProcessPromptCommand` (compound commands on one line are a known gap) and whether
  `AssumeTwoWayConnections`/`GuessExits` are set as expected.
