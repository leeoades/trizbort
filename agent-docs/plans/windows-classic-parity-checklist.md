# Windows Classic Parity Checklist

> **Superseded (2026-10-30)** by [`avalonia-parity-review.md`](avalonia-parity-review.md). The refactored WinForms app this
> checklist covered has been removed; the original app is in `legacy/`. Kept for its fixture groups and capture workflow.

This plan tracks checks for the Windows Forms application against a known pre-refactor baseline.
It is intended to be updated incrementally: run a section, record differences, fix regressions,
then mark the section retested.

## Baseline

| Field | Value |
|---|---|
| Baseline revision | TBD. Suggested: last Windows-only release/tag before the core/Avalonia extraction. |
| Candidate baselines | `v2.3.1-beta.2`, `v2.2.0`, or an explicitly identified pre-refactor commit. |
| Current revision | Update before each run with `git rev-parse --short HEAD`. |
| Test OS | Windows. Record version, DPI scale, and installed fonts for visual runs. |
| Runner output | Use an ignored directory under `artifacts/parity/`. |

## Latest Capture Output

On 2026-10-19, capture-only test runs generated side-by-side WinForms and Avalonia screenshots
under `artifacts/parity/2026-10-19/captures/`.

- `index.html`: browser-friendly side-by-side review of every fixture/state.
- `comparison-summary.csv`: joined manifest for classic/Avalonia dimensions, byte sizes and hashes.
- `classic-winforms/manifest.csv`: 336 WinForms captures.
- `avalonia/manifest.csv`: 336 Avalonia captures.

The capture set covers all 112 `tests/manual/*.trizbort` fixtures in three states:
`export`, `edit-none`, and `edit-selected`. Fixed viewport edit states are `1200x900`;
export captures intentionally differ in dimensions between frontends because the classic harness
uses a fixed fitted viewport while Avalonia's export path captures content bounds where available.

## Status Values

Use these status labels in the tables below:

- `Not started`: no parity check has been run.
- `Partially automated`: model state or selected probes are covered, but rendered pixels and/or UI workflows still need coverage.
- `Automated capture`: screenshots/manifests are generated reproducibly, but differences still require review or pixel assertions.
- `Automated`: covered by an automated parity or regression test.
- `Manual pass`: human visual/interaction check passed.
- `Difference`: observed difference from baseline; add an entry to the differences log.
- `Fixed`: a difference was fixed but not yet retested.
- `Retested`: fixed and checked again against baseline.
- `Accepted`: intentional behavior difference; document why.

## Comparison Workflow

1. Create a baseline worktree outside this worktree, for example:

   ```powershell
   git worktree add ..\trizbort-baseline v2.3.1-beta.2
   ```

2. Build both revisions in isolated output folders:

   ```powershell
   dotnet build Trizbort.sln -c Debug -p:BaseOutputPath=artifacts\parity\current-bin\
   dotnet build ..\trizbort-baseline\Trizbort.sln -c Debug -p:BaseOutputPath=artifacts\parity\baseline-bin\
   ```

3. Run current automated tests first:

   ```powershell
   dotnet test Trizbort.sln -c Debug -p:BaseOutputPath=artifacts\parity\test-bin\
   ```

4. For parity runs, prefer deterministic comparisons before UI screenshots:

   - load/save canonical XML for every `tests/manual/*.trizbort`
   - map statistics and validation results
   - code export text output
   - PDF metadata/page bounds/annotation counts
   - rendered canvas bitmap dimensions and sampled pixels
   - full bitmap comparisons only with tolerance and stable DPI/font settings

5. For manual UI checks, open the same fixture in baseline and current builds, record pass/fail,
   and add screenshots to `artifacts/parity/<date>/` when reporting a difference.

6. To regenerate the current WinForms/Avalonia visual capture corpus:

   ```powershell
   $env:TRIZBORT_CAPTURE_OUTPUT="$PWD\artifacts\parity\<date>\captures"
   dotnet test src\Trizbort.Tests\Trizbort.Tests.csproj -c Debug --filter FullyQualifiedName~ClassicVisualCaptureTests
   dotnet test src\Trizbort.Avalonia.Tests\Trizbort.Avalonia.Tests.csproj -c Debug --filter FullyQualifiedName~AvaloniaVisualCaptureTests
   ```

## Fixture Groups

Use `tests/manual/` as the primary regression fixture set. The `samples/` and `Docs/*.trizbort`
maps are useful smoke tests for real maps, but the manual maps are targeted to specific behavior.

| Group | Fixtures | Purpose | Status |
|---|---|---|---|
| Persistence inventory | all `tests/manual/*.trizbort` | Load, save, reload, canonical XML, dock-owner identity. | Automated |
| Basic visual rendering | `borders`, `connections`, `fills`, `object-placement`, `fill-different-room-shapes` | Room borders, connection styles, fills, object placement, room shape fills. | Automated capture |
| Room shapes and ports | `manual-room-shape-ports-handles`, `rounded-*`, `ellipse-*`, `dark-shapes` | Shape geometry, resize handles, ports, rounded/ellipse line attachment. | Automated capture |
| Region and fill colors | `fills-orange-region`, `fills-orange-region-dark`, `map-settings-colors`, `region-accent-*`, `start-room-purple` | Region colors, explicit room colors, dark rooms, start/end/reference highlighting. | Automated |
| Connection drawing | `connections`, `line-width-*`, `line-colors`, `stalk-length-*`, `one-room-lines`, `stray-line` | Solid/dashed lines, width, colors, stalks, self/stray connections. | Automated |
| Arrows and labels | `arrow-size-*`, `line-labeling*`, `text-offset-*`, `rounded-to-from*`, `ellipse-to-from*` | Direction chevrons, endpoint labels, mid labels, offsets, shape-specific adjustments. | Automated capture |
| Text and fonts | `font-*`, `long-subtitles`, `long-big-subtitles`, `dbcs-text`, `chars-gt-lt-underline-dquote`, `unavailable-font` | Font loading/fallback, style, size, wrapping, Unicode/special characters. | Automated capture |
| Object lists | `object-placement`, `object-testing`, `text-cut-paste` | Object list formatting, inside/outside placement, copy/paste text fidelity. | Automated capture |
| Map extent and zoom | `17-*`, `big-square`, `vertical-connector-only*`, `horizontal-connector-only*` | Zoom-to-fit, finite zoom, margins, connector-only maps. | Automated capture |
| Editing commands | `manual-edit-select-special`, `manual-room-swap`, `manual-control-arrow-connections`, `small-room-ctrl-arrow-test` | Select special, room swaps, control-arrow linking, edit command behavior. | Not started |
| Auto-adjust and navigation | `auto-adjust-*`, `shift-arrow-*`, `connector-choose-right-room`, `connector-rotate-full-ports` | Auto port adjustment, shift-arrow navigation, connector port selection/rotation. | Not started |
| View/menu workflows | `manual-view-menu`, `manual-help-menu` | View toggles, help/about/menu behavior. | Not started |
| Statistics | `map-stats-*`, `roomstats-*`, `two-start-rooms` | Room/dead-end/duplicate/start-room statistics. | Partially automated |
| PDF and image export | `pdf-save-annotation`, extent fixtures, representative visual fixtures | PDF page bounds, annotations, image dimensions and visual output. | Not started |
| Code export | `properties-in-brackets-basic`, `ext-chars-test`, representative sample maps | IF language export text and accent/property handling. | Not started |
| Real sample maps | `samples/*.trizbort`, `Docs/*.trizbort` | Smoke checks on real-world maps and docs examples. | Not started |

## Functionality Checklist

### File and Project Lifecycle

| Check | Evidence | Status | Notes |
|---|---|---|---|
| New project resets map, selection, viewport, defaults | Automated or manual UI workflow | Not started | |
| Open all manual fixtures without error | Automated inventory | Automated | See `tests/manual/README.md`. |
| Manual fixture model summaries preserve classic semantics | `ClassicManualFixtureModelSummaryTests` | Automated | Covers counts, bounds, settings, regions, room fields, connection vertices/segments, and labels for every `tests/manual/*.trizbort`. |
| Save and Save As preserve canonical map content | Automated parity XML comparison | Not started | |
| Recent projects and unsaved-change prompts | Manual desktop workflow | Not started | Dialog behavior is intentionally desktop-bound. |
| File watcher reload prompt | Manual desktop workflow | Not started | |
| Clipboard copy/paste of rooms, labels, connections, colors | Automated plus manual clipboard check | Not started | |

### Canvas Rendering

| Check | Evidence | Status | Notes |
|---|---|---|---|
| Canvas background/grid/origin draw correctly | Pixel or screenshot comparison | Not started | |
| Room fills, second fills, dark tabs, start/end/reference halos | Targeted fixtures, model summaries, and pixel probes | Automated | `ClassicBehaviorCharacterizationTests` covers region and second-fill colors; `ClassicManualFixtureModelSummaryTests` covers loaded style/color state for every manual fixture. Dark/start/reference still need more rendered probes. |
| Room borders and border styles | `borders.trizbort` screenshot/pixel probes | Not started | |
| Room shapes: square, rounded, ellipse, octagonal | Shape fixtures and screenshot comparison | Not started | |
| Text layout: names, subtitles, objects, multiline text | Font/text fixtures | Not started | |
| Connection geometry, line widths, colors, labels, doors | Connection fixtures and model summaries | Automated | `ClassicBehaviorCharacterizationTests` covers semantic connection flags/text/colors and rendered line colors; `ClassicManualFixtureModelSummaryTests` covers vertices/segments/door state across all manual fixtures. Widths/doors still need more rendered probes. |
| Smart-line gaps only at connection crossings, not room borders | Regression tests and visual fixtures | Automated | Covered by current WinForms regression test. |
| One-way arrows/chevrons render at expected position and direction | Arrow-size fixtures and pixel probes | Not started | |
| Selection/hover adorners, handles, ports, waypoint handles | Manual interaction and screenshots | Not started | |
| Minimap overview and viewport rectangle | Manual UI and/or screenshot | Not started | |
| Export final-render mode excludes selection/hover adorners | Image/PDF parity check | Not started | |

### Canvas Interaction

| Check | Evidence | Status | Notes |
|---|---|---|---|
| Add room/label/connection from keyboard/menu/context menu | Manual or UI integration tests | Not started | |
| Drag rooms/labels/connections and preserve docked endpoints | Automated geometry tests plus manual feel | Not started | |
| Resize rooms and labels using handles | Automated plus manual handles fixture | Not started | |
| Draw new connections between ports and empty canvas | Manual workflow | Not started | |
| Move connection ports, rotate ports, auto-adjust ports | Manual fixtures | Not started | |
| Pan, zoom, zoom-to-fit, scrollbars | Extent fixtures and manual view workflow | Not started | |
| Keyboard movement and shift/control-arrow shortcuts | Manual fixtures and integration tests | Not started | |
| Selection commands and select-special behavior | Manual fixture | Not started | |

### Dialogs and Settings

| Check | Evidence | Status | Notes |
|---|---|---|---|
| Room properties dialog round-trips all fields | Automated dialog tests plus manual | Not started | |
| Connection properties dialog round-trips all fields | Automated dialog tests plus manual | Not started | |
| Label properties dialog round-trips outline/background/text | Automated dialog tests plus manual | Not started | |
| Map settings: colors, fonts, line widths, grid, regions | Settings fixtures and manual dialog | Not started | |
| Application settings persist and restore | Automated settings tests/manual restart | Not started | |
| Theme apply/import/export and style preservation prompts | Manual or targeted tests | Not started | |
| Room style inference preview/apply | Manual or targeted tests | Not started | |

### Persistence and Compatibility

| Check | Evidence | Status | Notes |
|---|---|---|---|
| Load legacy fixtures from `tests/manual` | Automated inventory and model snapshot | Automated | `ClassicManualFixtureModelSummaryTests` characterizes loaded model state for every manual fixture. |
| Load real sample maps and docs maps | Automated smoke check | Not started | |
| Save format remains compatible with baseline where expected | XML parity comparison | Not started | Normalize volatile fields. |
| Unknown/future fields do not crash loader | Targeted tests | Not started | |
| Path handling accepts spaces, relative paths, and missing files gracefully | Automated/manual | Not started | |

### Export

| Check | Evidence | Status | Notes |
|---|---|---|---|
| Image export dimensions and content match baseline | Bitmap capture corpus | Automated capture | `ClassicVisualCaptureTests` and `AvaloniaVisualCaptureTests` produce manifests and PNGs for all manual fixtures; tolerant pixel assertions are still future work. |
| PDF page bounds, visual content, annotations match baseline | PDF metadata/pixel/manual | Not started | |
| Inform 6 export | Text diff | Not started | |
| Inform 7 export | Text diff | Not started | |
| TADS export | Text diff | Not started | |
| Hugo export | Text diff | Not started | |
| ZIL export | Text diff | Not started | |
| Alan export | Text diff | Not started | |
| Quest export | Text diff | Not started | |
| Adventuron export | Text diff | Not started | |
| Clipboard exports preserve expected text | Manual/automated clipboard boundary | Not started | |

### Automap

| Check | Evidence | Status | Notes |
|---|---|---|---|
| Transcript parsing creates same rooms/exits as baseline | Automated transcript comparison | Not started | |
| Ambiguous room disambiguation prompts behave as before | Manual UI workflow | Not started | |
| Same-direction conflict handling behaves as before | Manual/automated callback test | Not started | |
| Automap start/stop/status/error handling | Manual UI workflow | Not started | |
| Automap file reading handles encodings and large transcripts | Automated regression | Not started | Recent file-handling changes touched this. |

### Desktop Integration

| Check | Evidence | Status | Notes |
|---|---|---|---|
| File dialogs, color dialogs, font dialogs | Manual desktop workflow | Not started | |
| Clipboard interoperability with external apps | Manual desktop workflow | Not started | |
| File association launching | Manual desktop workflow | Not started | |
| Help/about links open expected targets | Manual fixture | Not started | |
| DPI scaling, multiple monitors, Windows theme differences | Manual screenshot notes | Not started | |

## Differences Log

Add one row per observed difference. Keep fixed issues in the table so they can be retested.

| ID | Date | Fixture/Workflow | Baseline | Current | Severity | Status | Fix commit | Retest notes |
|---|---|---|---|---|---|---|---|---|
| D001 | 2026-10-19 | Classic room fills and connection gaps | Room region fills visible; connection lines continuous near rooms | `NoRegion` rooms used canvas color; some lines had gaps near room borders | High | Retested | `3d92b99` | Full solution tests passed after fix. |

## Next Automation Targets

1. Add a parity runner that builds a baseline worktree and current tree into separate output folders.
2. Add deterministic export text comparisons for all language exporters.
3. Add tolerant image comparison for selected fixtures after normalizing DPI, size, fonts, and settings.
4. Store parity output under `artifacts/parity/<baseline>--<current>/` with a machine-readable summary.

## Forward Characterization Tests

The preferred long-term protection is to convert baseline observations into ordinary tests that
run on every future change, rather than repeatedly launching an old binary. Current examples:

| Test class | Coverage |
|---|---|
| `ClassicVisualCaptureTests` | Capture-only WinForms screenshot harness. With `TRIZBORT_CAPTURE_OUTPUT` set, writes `export`, `edit-none`, and `edit-selected` PNGs plus a manifest for every manual fixture. Skips when the env var is absent. |
| `AvaloniaVisualCaptureTests` | Capture-only Avalonia screenshot harness mirroring the WinForms capture states for every manual fixture. Skips when `TRIZBORT_CAPTURE_OUTPUT` is absent. |
| `ClassicManualFixtureModelSummaryTests` | Every `tests/manual/*.trizbort` fixture. Captures deterministic model summaries for loaded settings, regions, room fields, connection endpoints/segments/doors/text, labels, counts, and bounds. Regenerate intentionally with `TRIZBORT_UPDATE_CHARACTERIZATION=1` and `TRIZBORT_CHARACTERIZATION_OUTPUT=<repo snapshot path>`. |
| `ClassicBehaviorCharacterizationTests` | Manual `connections`, `fills-orange-region`, and `line-colors` fixtures. Captures classic connection semantics, region/second-fill rendering, and rendered custom line colors using off-screen WinForms rendering. |
