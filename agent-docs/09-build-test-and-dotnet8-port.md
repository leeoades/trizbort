# Build, Test, and Modernization (.NET 10 LTS & .NET 8 Port Notes)

> **Layout change (2026-10-30):** the refactored WinForms projects have been removed. The original WinForms app
> (tag `v2.3.1-beta.2`) now lives in `legacy/` with its own `legacy/Trizbort.Legacy.sln` — see `legacy/README.md`.
> WinForms sections of these docs describe the refactored app; for the original, paths are under `legacy/src/Trizbort/`
> and its Domain/Automap/Export code lives inside that project rather than in `Trizbort.Core`.

See [`README.md`](README.md) for the doc map. Read this before touching
`src/Trizbort/Trizbort.csproj`, `src/Trizbort.Tests/Trizbort.Tests.csproj`,
`src/Trizbort.Avalonia/Trizbort.Avalonia.csproj`,
`src/Trizbort/app.config`, or `src/Trizbort/app.manifest`.

## Current state (.NET 10 LTS)

The entire solution targets **.NET 10 LTS** (active LTS, supported through November 2028). Building the solution requires the **.NET 10 SDK** (10.0.x).

- `src/Trizbort.Core/Trizbort.Core.csproj` — SDK-style, `net10.0`, pure class library without Windows Forms dependencies. Contains domain elements, automap logic, exporters, math/geometry primitives, and serialization engines. Cross-platform.
- `src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj` — SDK-style, `net10.0`, headless test suite running on any OS.
- `src/Trizbort/Trizbort.csproj` — SDK-style, `net10.0-windows`, `UseWindowsForms=true`, `PackageReference`s
  (no more `packages.config`). **No `PlatformTarget=x86`** (removed — see below); builds
  AnyCPU/x64 by default.
- `src/Trizbort.Tests/Trizbort.Tests.csproj` — SDK-style, `net10.0-windows`, `Microsoft.NET.Test.Sdk`
  + NUnit 3.14.0 + NUnit3TestAdapter 4.6.0 + NUnit.Analyzers + Shouldly + FluentAssertions.
- `src/Trizbort.Avalonia/Trizbort.Avalonia.csproj` — SDK-style, `net10.0`, Avalonia 11.3.x cross-platform UI client (Linux/macOS/Windows).
- `src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj` — SDK-style, `net10.0`, headless Avalonia NUnit tests (`Avalonia.Headless.NUnit`).

## Building and Testing

### Cross-Platform / Linux
On Linux/macOS, all projects (including Windows Forms targets) compile with
`EnableWindowsTargeting=true`; the current build has 0 errors and 2 CA2024 warnings in
`Automap.cs` (see Gotcha #13):
```bash
# Build the entire solution
dotnet build Trizbort.sln -c Debug

# Run headless cross-platform test suites
dotnet test src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj -c Debug
dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj -c Debug

# Or run the core test script (generates TRX under TestResults/)
./scripts/run-core-tests.sh
```
*Note on Linux testing*: `Trizbort.Tests` contains WinForms STA and GDI+ integration tests that require a Windows runtime host. On Linux, validate that `Trizbort.Tests` compiles without errors during `dotnet build`, but do not execute its runtime test runner. Additionally, note that Windows-style path literals (such as `@".\appsettings.json"`) resolve literally on Linux file systems rather than as relative subpaths.

### Windows
On Windows, build and test the complete solution with:
```bash
dotnet build Trizbort.sln -c Debug
dotnet test Trizbort.sln -c Debug
```

### Individual project commands
```bash
dotnet build src/Trizbort.Core/Trizbort.Core.csproj -c Debug
dotnet test src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj -c Debug
dotnet build src/Trizbort.Avalonia/Trizbort.Avalonia.csproj -c Debug
dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj -c Debug
dotnet build src/Trizbort/Trizbort.csproj -c Debug
# Windows only: this runs WinForms runtime tests
dotnet test src/Trizbort.Tests/Trizbort.Tests.csproj -c Debug
```

- Immediate Commit Rule:
  - When making a change, once it is fixed up, building, and tests pass, **commit those changes immediately**.
  - **Do NOT** accumulate a working directory of unrelated changes across different components or domains.
  - Exclude `.junie/` and user-specific files (`*.DotSettings.user`) from git commits.
- Manual map fixtures and runners are in `tests/manual/`. Map files and batch runners are copied
  into the test output for automated round-trip/reference checks; visual scenarios still need
  inspection. Sample maps/transcripts remain in the root `samples` directory.
- The `Trizbort` WinForms GUI remains a **Windows Forms, Windows-only application**. The extracted `Trizbort.Core` library and `Trizbort.Avalonia` client, however, are fully cross-platform (Linux/macOS/Windows).

## Historical Gotchas: .NET Framework 4.8 → .NET 8 Port & .NET 10 Modernization

These are generic lessons for porting and modernizing any legacy WinForms app to modern .NET, not just
Trizbort-specific trivia — useful if a future task touches another legacy project, or if more
latent issues surface here.

### 1. `ContextMenu`/`MenuItem`/`Menu`/`MainMenu` were removed from WinForms on .NET 5+

Only `ContextMenuStrip`/`ToolStripMenuItem`/`MenuStrip` remain. Fixed in
`Domain\StatusBar\ZoomStatusWidget.cs` (swapped `ContextMenu`/`MenuItem` for
`ContextMenuStrip`/`ToolStripMenuItem`). Grep for `System.Windows.Forms.ContextMenu\b` or
`MenuItem\b` (not `ToolStripMenuItem`) if more of these turn up.

### 2. PDFsharp package rename

`PDFsharp-gdi` 1.50.5147 (old) had no modern .NET support. Replaced with **`PDFsharp-GDI` 6.2.4**
(note the capitalized "GDI" in the new package ID) which supports modern .NET (`net8.0-windows` and `net10.0-windows`). API is
compatible — `XGraphics`/`PdfDocument`/etc. usage in `UI\MainForm.cs`, `Domain\Elements\Room.cs`,
`Domain\Elements\Connection.cs`, `Domain\Misc\Drawing.cs` did not need code changes, only the
package reference.

### 3. `AutoUpdater.NET`

The port initially migrated from a vendored DLL to the `AutoUpdater.NET.Official` NuGet
package (1.9.3). The update checker and its dependency have since been removed; releases are
downloaded manually from the repository.

### 4. Keep project boundaries explicit

The codebase is split into distinct project roots:
- `src/Trizbort.Core/`: Portable domain, automap, geometry, settings, and exporters (`net10.0`).
- `src/Trizbort.Core.Tests/`: Cross-platform unit tests (`net10.0`).
- `src/Trizbort/`: Windows Forms UI host (`net10.0-windows`).
- `src/Trizbort.Tests/`: WinForms UI and integration tests (`net10.0-windows`).
- `src/Trizbort.Avalonia/`: Cross-platform Avalonia UI client (`net10.0`).
- `src/Trizbort.Avalonia.Tests/`: Avalonia headless tests (`net10.0`).

SDK-style implicit globs are relative to each project directory, so manual fixtures/scripts in
`tests/manual/` and root-level assets are not compiled into the projects. Keep future
project-specific source/resources under that project's directory and add intentional
cross-project dependencies with `ProjectReference`.

### 5. `GenerateAssemblyInfo` vs. a committed `AssemblyInfo.cs`

SDK-style projects default `GenerateAssemblyInfo=true`, which **conflicts** with a committed
hand-authored `Properties\AssemblyInfo.cs` (duplicate-attribute build errors). Resolved by
setting `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` in `Trizbort.csproj` to preserve the
original file's hand-set version/copyright/GUID. The test project's boilerplate
`AssemblyInfo.cs` carried no meaningful values, so it was simply deleted instead.

### 6. `Settings.settings`/`Resources.resx` code-gen needs explicit metadata in SDK-style

Non-SDK projects infer the old Generator/LastGenOutput/DependentUpon wiring automatically; SDK-
style does not for files that already exist on disk. Explicit `<Compile Update>`/
`<EmbeddedResource Update>`/`<None Update>` blocks were added to preserve:
- `Settings.settings` → `Settings.Designer.cs` (`SettingsSingleFileGenerator`)
- `Resources.resx` → `Resources.Designer.cs` (`ResXFileCodeGenerator`, kept non-public to match
  the original `internal sealed` `Resources` class visibility)

### 7. `CA1416` and `WFO1000` analyzer suppressions in `Trizbort.csproj`

Targeting Windows TFMs (`net8.0-windows` / `net10.0-windows`) produced ~2100 `CA1416` warnings for Windows-only API calls. In the historical .NET 8 port, trying the `net8.0-windows7.0` TFM did not reduce the count. Modern .NET Windows Forms SDK analyzers also emit `WFO1000` warnings for custom properties not explicitly configured for designer serialization. Since this app is intentionally Windows-only and runtime Form dialog properties do not require designer serialization, warnings are suppressed via:
```xml
<NoWarn>$(NoWarn);CA1416;WFO1000</NoWarn>
```
in `Trizbort.csproj`. This is a deliberate, justified suppression — don't "fix" it by removing the `NoWarn` without expecting ~2100 CA1416 warnings and WFO1000 warnings on dialog forms to reappear. Custom controls under `UI/Controls/` configure explicit `DesignerSerializationVisibility` attributes (see Gotcha #10 below).

### 8. `PlatformTarget=x86` bitness mismatch broke `dotnet test`

The original legacy `.csproj` built x86-only. Keeping `PlatformTarget=x86` on the ported SDK-
style project caused `dotnet test` to throw `FileNotFoundException` (not the more typical
`BadImageFormatException`) because the AnyCPU/x64 `testhost.exe` couldn't load the x86-only
`Trizbort.dll`. **Removed `PlatformTarget=x86`**; the app now builds AnyCPU/x64 by default, and
tests pass. **This is flagged as an open decision for maintainers**: if 32-bit-only
distribution is a hard requirement (e.g. for compatibility with some IF tool/environment),
further work is needed — e.g. configure the test host to run under x86, or keep the app x86 and
find another way to run tests out-of-process.

### 9. `app.config`/`app.manifest` sections that .NET Core's `ConfigurationManager` doesn't recognize

This is the most subtle gotcha and caused a real post-port runtime bug (crash on closing
`SettingsDialog`). **Any single unrecognized top-level `<configuration>` section causes the
*entire* config system to throw on first use** — not just when that specific section is read.
.NET Framework was lenient about sections it didn't specially handle; .NET Core's
`System.Configuration.ConfigurationManager` package is not.

Two sections were removed from `app.config` for this reason:
1. `<system.web>` — unused `ClientApplicationServices` membership/roleManager VS-template
   boilerplate (no code referenced it; safe to delete outright).
2. `<System.Windows.Forms.ApplicationConfigurationSection>` (the `DpiAwareness=PerMonitorV2`
   setting) — a .NET-Framework-only mechanism. Its *replacement* on modern .NET is the
   `ApplicationHighDpiMode` MSBuild property:
   ```xml
   <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
   ```
   in `src/Trizbort/Trizbort.csproj`. The redundant `dpiAware` entry was also removed from `src/Trizbort/app.manifest` (it
   predates `ApplicationHighDpiMode` and generates the `WFAC010` build warning otherwise).

Harmless things that were **confirmed not to be the problem** (and can stay):
- `<startup><supportedRuntime .../></startup>` — ignored, not a recognized/parsed section that
  throws.
- Stray `<appSettings>` keys like `ClientSettingsProvider.ServiceUri` — inert key/value pairs,
  not a custom section type.

**If you ever see `ConfigurationErrorsException: Unrecognized configuration section <X>`**,
look for exactly this class of problem: a custom/Framework-only top-level section in
`app.config` that isn't declared under `<configSections>` with a type .NET Core's
`ConfigurationManager` understands. The fix is almost always either (a) delete the section if
unused, or (b) move its behavior to the modern equivalent mechanism (as with DPI mode above) —
not to try to "fix" the section's XML syntax.

**Verification approach used** (no UI automation / screenshots needed): a standalone console
harness with a `Settings`-like `ApplicationSettingsBase` subclass, pointed at the real generated
`Trizbort.dll.config`, reproduced the exact failing call stack
(`ApplicationSettingsBase.set_Item` → `ConfigurationManager.RefreshSection`) and confirmed the
fix. This pattern (a tiny throwaway console app loading the real generated `.config`) is a good
template for verifying any future `app.config`-related fix without needing to drive the WinForms
UI.

### 10. `WFO1000` WinForms designer serialization attributes

Modern .NET Windows Forms analyzers emit `WFO1000` ("Property does not configure code serialization for its property content") on control properties that lack explicit serialization attributes. To support the WinForms designer cleanly:
- Custom controls (`UI/Controls/`: `Canvas`, `Minimap`, `TrizbortTextBox`, `TrizbortToolTip`, `AutomapBar`) have explicit `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` for internal runtime properties and `[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]` / `[DefaultValue(...)]` for configurable designer properties.
- Top-level dialog and form classes with runtime-only helper properties rely on the project-level `WFO1000` suppression.

### 11. Modern Form lifecycle overrides (`OnFormClosing` / `OnFormClosed`)

The legacy `Form.OnClosing(CancelEventArgs)` and `Form.OnClosed(EventArgs)` methods are obsolete in modern .NET Windows Forms. Overrides in `MainForm` and `AutomapDialog` were migrated to `protected override void OnFormClosing(FormClosingEventArgs e)` and `protected override void OnFormClosed(FormClosedEventArgs e)` to adhere to modern lifecycle patterns.

### 12. Avalonia 11.3 clipboard API migration (`CS0618`)

Avalonia 11.3 deprecated `IClipboard.GetTextAsync()` (`CS0618`). Its replacement,
`ClipboardExtensions.TryGetTextAsync`, is an extension method on `IClipboard` in
`Avalonia.Input.Platform`; import that namespace to use it. Both methods return
`Task<string?>` with equivalent null-fallback semantics when clipboard text is unavailable or
format conversions fail. Call sites in `MainWindowViewModel`, `MapCanvasControl`, and Avalonia
gesture tests were updated.

### 13. Modern compiler analyzers (`CA2024`)

The .NET 10 analyzer reports 2 `CA2024` warnings ("Do not use 'reader.EndOfStream' in an async method") in `src/Trizbort.Core/Automap/Automap.cs` where synchronous `StreamReader.EndOfStream` property checks are still used inside live-transcript asynchronous Automap parsing. These are pre-existing logic patterns that compile with 0 errors across the solution. One-shot transcript parsing reads the whole file through a shared-read `FileStream` so Windows tests cover transcripts that remain open for writing by an interpreter.

### 14. Avalonia headless text rendering pixel probes

In headless UI testing (`DrawingAdaptersParityTests`), when probing canvas pixel colors rendered via the Avalonia headless drawing adapter against expected colors, Room elements require `Name = string.Empty` to prevent default text label rendering from conflicting with fill pixel probes at central coordinates.

### 15. Intermittent Automap stop/single-step test

`AutomapRegressionTests.StopDuringSingleStep_PreservesBufferedGraphAndSkipsFollowingCommands`
can intermittently fail with `Collection was modified` when `LegacyMapFileEngine.Save`
enumerates `Project.Elements` while Automap is changing it. This race was reproduced on the
unchanged .NET 8 baseline as well, so it is pre-existing and outside the .NET 10 migration.

## Safety note for anyone testing this app interactively in a shared/non-sandboxed environment

**Do not take full-screen/desktop screenshots to verify UI changes in a shared environment** —
there is no guarantee the capture is scoped to an isolated sandbox; it can capture another
user's live desktop. Prefer process status checks (`HasExited`, `MainWindowTitle`), log output,
and scoped/headless repro harnesses (as described above) instead.

## Release process

The legacy ClickOnce distribution (`Trizbort.application` pointing at `trizbort.com`) is dead
and is **not** produced any more. Pushing a tag starts a release; the prefix picks the app:

| Tag | Workflow | Runner | Assets | Changelog |
|---|---|---|---|---|
| `recast-vX.Y.Z[.R][-beta.N]` | `.github/workflows/release.yml` | ubuntu-latest | `TrizbortRecast-<v>-win-x64.zip` (`TrizbortRecast.exe`), `TrizbortRecast-<v>-linux-x64.tar.gz` (`trizbort-recast` plus the `packaging/linux` files) | root `changelog.md` |
| `legacy-vX.Y.Z[.R][-beta.N]` | `.github/workflows/release-legacy.yml` | windows-latest | `Trizbort-<v>-win-x64.zip` (`Trizbort.exe`) | `legacy/changelog.md` |

The older `v2.*` tags are Trizbort 2.x (WinForms) releases from before the prefixes. Both workflows:

1. Check the tag format (`-beta.N` needs a positive integer; other prerelease suffixes are rejected).
2. Stamp the version from the tag without committing it (Recast passes `-p:Version` /
   `-p:InformationalVersion` to `dotnet publish`; legacy rewrites `AssemblyInfo.cs`).
3. Run the tests, then publish self-contained single-file builds (no .NET install required).
4. Create a GitHub Release ("Trizbort Recast 0.9.0-beta.1" / "Trizbort (legacy) legacy-v2.3.2").
   Betas are marked prerelease, never replace the latest stable release, and use their changelog
   section (heading without the prefix, e.g. `## [0.9.0-beta.1]`) as the notes; the build fails if
   it is missing. A beta can be tagged from an unmerged branch for testing.

There is no in-app update checker or update manifest. Help → Online Help (F1) links to the
v2 user guide (`Docs/index.md` on GitHub) and the original v1 help; users download releases
manually from GitHub Releases. User `appsettings.json` is not in
the zip, so it survives extracting a new release over the install folder. If the repo moves,
update `OnlineHelpDialog.UserGuideUrl` and the message in `Project.CheckDocVersion()`.

Extract releases into a user-writable folder, not Program Files. Releases are
unsigned; SmartScreen warnings are possible. No ClickOnce migration is provided: users of the
old builds need to download this fork's release manually first. The workflow uses its built-in
token with `contents: write`, not a personal access token.

## Regression safety net

The automated suite includes geometry/document unit tests; real Canvas event, controller and
selection integration; culture-independent persistence; Automap parsing/graph/error/cancellation
workflows; all nine language exporter variants; and bounded off-screen rendering/PDF checks.
It runs actual production behavior rather than implementing a second editor/parser for tests.
All manual maps receive canonical save/load/save checks, with additional specific assertions
for selected statistics, graph, version, font and extent scenarios. See `tests\manual\README.md`
for script equivalents and the remaining visual matrix.

New fixtures use `Unit`, `Integration`, or `Rendering` categories (older tests may be
uncategorized). Examples on Windows (or see Building and Testing for Linux headless test commands):

```bash
dotnet test Trizbort.sln -c Debug
dotnet test Trizbort.sln -c Release
dotnet test Trizbort.sln -c Debug --filter "TestCategory=Integration|TestCategory=Rendering"
dotnet test Trizbort.sln -c Debug --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
```

`coverlet.collector` produces Cobertura XML under the results directory. `coverage.runsettings`
includes only application code and excludes generated designer/compiler code. Coverage is a
guide to missing behavior, not a promise of completeness; there is no percentage gate.
`.github\workflows\tests.yml` runs Release tests on Windows for PRs and branch pushes and uploads
TRX/coverage artifacts even on failure (14-day retention). The release workflow also supports
beta prereleases as described above.

GitHub branch protection on `master` requires the `tests` status check and an up-to-date branch
before merging, including for administrators. Force pushes and branch deletion are disabled.
This protection is repository-side configuration, not enforced by the workflow YAML alone;
keep the job name `tests` stable or update the required check when renaming it.

`TestEnvironment` changes the test process working directory to a unique temporary directory
and precreates `appsettings.json` to prevent reading/migrating the user's settings.
It also installs `UnexpectedUserInteraction`, which fails immediately on unexpected message
boxes, modal dialogs or clipboard access. All production dialog presentation calls go through
`UI\UserInteraction`; new interaction tests use a recording implementation. Tests specifically
checking form focus/layout/keyboard dismissal use a zero-opacity, no-taskbar `TestDialog`
helper, so they can pump WinForms events without visible windows. Mouse integration fixtures
disable tooltips.
`IsolatedProjectTests` snapshots/restores map settings, application preferences and
`Project.Current`, stops the file watcher, and runs nonparallel on STA. Fixtures use the copied
output directory, not the checkout working directory. Tests do not use the system clipboard,
file associations, network or desktop screenshots.

`InternalsVisibleTo("Trizbort.Tests")` exposes narrow production seams: `MapEditing`, clipboard
graph reconstruction, version comparison, error/user-decision callbacks and PDF orchestration.
Canvas is sealed: `CanvasInput` centrally invokes its protected WinForms event handlers, without
reflecting private state or injecting OS input. Keep new tests against behavior, not fields.

Small defects exposed by the suite have dedicated regressions: endpoint-filtered intersections,
room validation classifications, tall-viewport wheel zoom, free-connector keyboard movement,
pasted metadata/reference independence, map-load metadata loss, empty-map statistics and
unlabeled counts, dangling-region links, exporter reuse, zero-extent PDF pages, and Automap EOF,
run-state, conflict-choice and error/cancellation behavior. Characterization cases intentionally
retain existing semantics such as connection reversal retaining directional labels.

Still not fully automated: dialog interaction, actual clipboard transfer, desktop mouse capture/
cursor feel, high-DPI/font appearance, minimap presentation and external IF-engine compilation
of generated source. Off-screen images are scoped geometry/color assertions, not screenshot
baselines or proof of pixel-perfect visual equivalence.
