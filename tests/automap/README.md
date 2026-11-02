# Automap scenarios

Each folder here is one automap test: a game transcript and the map automap should make from it. The tests run the
automap engine with no UI (`AutomapScenarioTests` in `src/Trizbort.Core.Tests`), so they are quick and run on every
build.

## Adding a scenario

1. Make a folder with a short name, e.g. `tests/automap/garden-comma-names/`.
2. Put the transcript in it as `transcript.txt`: the whole transcript, or just the part that goes wrong. Keep the
   game's opening lines if the problem is near the start.
3. Run the tests: `dotnet test src/Trizbort.Core.Tests --filter Category=AutomapScenario`.
   With no `expected.txt` yet, the test is *inconclusive* and writes `actual.txt`: the map automap makes today.
4. Copy `actual.txt` to `expected.txt` and correct it to the map you expected.
5. If it doesn't match yet (a bug to fix), add `known-issue.txt` with a sentence saying what's wrong. The test is then
   skipped with that explanation instead of failing the build, and starts failing as soon as the map matches, so the
   marker gets removed when the bug is fixed.

`actual.txt` is written whenever a scenario doesn't match, so you can compare it with `expected.txt`. It is ignored by
git. To accept the current maps as correct, run the tests with `TRIZBORT_APPROVE_AUTOMAP=1`.

## Files

| File | |
|---|---|
| `transcript.txt` | The transcript, as the game wrote it. |
| `expected.txt` | The map you expect (format below). |
| `settings.txt` | Optional. Automap settings that differ from the defaults, one `Name = value` per line. |
| `known-issue.txt` | Optional. Marks a scenario that is known to fail, and says why. |
| `actual.txt` | Written by the test when the map differs or there is no `expected.txt`. |
| `trace.txt` | Written with `actual.txt`: automap's reasoning for every transcript line (Debug builds). |

Settings names (defaults in brackets): `VerboseTranscript` (true), `AssumeRoomsWithSameNameAreSameRoom` (false),
`GuessExits` (true: adds unexplored exits mentioned in room descriptions), `AssumeTwoWayConnections` (true),
`AddObjectCommand` (`tb see`), `AddRegionCommand` (`tb region`). `SameDirectionAnswer` (`KeepRoom1`) answers "there's
already a room that way": `KeepRoom1` (keep the existing room), `KeepRoom2` (replace it) or `KeepBoth`. Lines
starting with `#` are comments.

Questions automap would ask in the app get the non-interactive answers: the first match when several rooms could be
meant, and `SameDirectionAnswer` when a direction is already taken.

## The map format

```
current: Garden, West

rooms:
Garden, East (start)
Garden, North
Garden, West

connections:
Garden, East [north] <-> [south] Garden, North
Garden, East [west] <-> [east] Garden, West
Cellar [north "up"] <-> [south "down"] Hall
Pantry [east] -> [west] Kitchen
Hall [west] stub
```

- `current:` the room automap thinks you are in at the end of the transcript.
- `rooms:` every room, in alphabetical order. `(start)` marks the start room. Rooms with the same name are numbered in
  the order automap made them: `Maze #1`, `Maze #2`.
- `connections:` one line per connection, in alphabetical order: room, the compass point the line leaves from, `<->`
  for two-way or `->` for one-way, the compass point it arrives at, and the other room. Up, down, in and out are drawn
  from a compass point with a label, shown in quotes; when several directions lead along one connection, the label
  lists them all (`"north / down"`). A label in the middle, such as the command for a teleport, follows the second
  room (`"xyzzy"`). `(dotted)` marks a dotted line. `stub` is an unexplored exit.
- `overlaps:` only there when the layout has problems a player would notice: a room on top of another
  (`A overlaps B`), or a room sitting on a connection that isn't its own (`Patio is on the join A - B`).

Positions on the canvas are not compared otherwise: a scenario is about what is connected to what.
