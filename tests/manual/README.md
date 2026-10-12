# Manual test maps

This directory contains `.trizbort` fixtures and batch/script runners for manual and visual
regression checks. Automated tests live in `src/Trizbort.Tests/`; the test project copies the
map files and batch runners to its output so NUnit can discover them independently of the
working directory.

The batch runners resolve their fixture paths from the runner's own directory, so they can be
started from another working directory. Opening the map files still requires Trizbort to be
installed or associated with the `.trizbort` extension.

## Automated equivalents

Every map currently in this directory (112 fixtures) receives a load/save/reload/save check
comparing canonical XML and verifying dock-owner identity. Selected fixtures also have
explicit dead-end statistics and tall/wide/connector-only rendering/PDF assertions.
Round-trip success alone does **not** verify the original manual visual scenario.

| Legacy runner | C# coverage |
|---|---|
| `vtest.py` | Version-policy classification for equal/newer/older components, precedence, zero boundaries and malformed/missing versions; no file-association launching. |
| `testsync.pl` | Batch/map reference integrity and an explicit inventory of intentional unassigned maps; no clipboard or zip side effects. |
| `font-all.pl` | Deterministic font size/style loading and unavailable-font fallback; no enumeration of every installed font. |
| `.bat` map launchers | Referenced fixture existence plus per-map round-trips; launcher execution and visual judgments remain manual. |

The scripts are retained for optional manual use. Run automated checks from the repository root:

```
dotnet test Trizbort.sln -c Debug
dotnet test Trizbort.sln -c Debug --filter "FullyQualifiedName~PersistenceRegressionTests|FullyQualifiedName~CommandIntegrationTests|FullyQualifiedName~RenderingIntegrationTests"
```

## Remaining manual matrix

| Area | Human checks |
|---|---|
| Mouse interaction | Real cursor/capture, dragging outside the control, handle usability and drag feel. Automated tests cover resulting geometry and event routing, not OS input. |
| Appearance | High-DPI/multiple monitors, font fallback readability, wrapping/clipping, minimap and dense-map legibility. Off-screen pixel assertions are not screenshot baselines. |
| Desktop integration | Clipboard ownership/transfer, file/save dialogs, file associations and watcher prompts. |
| Dialogs and exports | Automap disambiguation/diagnostic controls and full exported-source compilation in the relevant IF engine. |
