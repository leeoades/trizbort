# Build, Test, and .NET Framework 4.8 → .NET 8 Port Notes

See [`README.md`](README.md) for the doc map. Read this before touching
`src/Trizbort/Trizbort.csproj`, `src/Trizbort.Tests/Trizbort.Tests.csproj`,
`src/Trizbort/app.config`, or `src/Trizbort/app.manifest`.

## Current state

- `src/Trizbort/Trizbort.csproj` — SDK-style, `net8.0-windows`, `UseWindowsForms=true`, `PackageReference`s
  (no more `packages.config`). **No `PlatformTarget=x86`** (removed — see below); builds
  AnyCPU/x64 by default.
- `src/Trizbort.Tests/Trizbort.Tests.csproj` — SDK-style, `net8.0-windows`, `Microsoft.NET.Test.Sdk`
  + NUnit 3.14.0 + NUnit3TestAdapter 4.6.0 + NUnit.Analyzers + Shouldly + FluentAssertions.
- Automated tests are included in the root solution. Build and run the full solution with:
  ```
  dotnet build Trizbort.sln -c Debug
  dotnet test Trizbort.sln -c Debug
  ```
- To build or test one project directly:
  ```
  dotnet build src\Trizbort\Trizbort.csproj -c Debug
  dotnet test src\Trizbort.Tests\Trizbort.Tests.csproj -c Debug
  ```
- Manual map fixtures and runners are in `tests\manual`. Map files and batch runners are copied
  into the test output for automated round-trip/reference checks; visual scenarios still need
  inspection. Sample maps/transcripts remain in the root `samples` directory.
- This is still a **Windows Forms, Windows-only application**. Porting the runtime from .NET
  Framework 4.8 to .NET 8 did **not** make it cross-platform — WinForms-on-.NET-Core remains
  Windows-only by design. True cross-platform would require a separate UI rewrite (e.g. Avalonia
  UI), not covered by this port.

## Gotchas encountered during the port (read before repeating this work elsewhere)

These are generic lessons for porting any legacy WinForms app to modern .NET, not just
Trizbort-specific trivia — useful if a future task touches another legacy project, or if more
latent issues surface here.

### 1. `ContextMenu`/`MenuItem`/`Menu`/`MainMenu` were removed from WinForms on .NET 5+

Only `ContextMenuStrip`/`ToolStripMenuItem`/`MenuStrip` remain. Fixed in
`Domain\StatusBar\ZoomStatusWidget.cs` (swapped `ContextMenu`/`MenuItem` for
`ContextMenuStrip`/`ToolStripMenuItem`). Grep for `System.Windows.Forms.ContextMenu\b` or
`MenuItem\b` (not `ToolStripMenuItem`) if more of these turn up.

### 2. PDFsharp package rename

`PDFsharp-gdi` 1.50.5147 (old) has **no .NET 8 support**. Replaced with **`PDFsharp-GDI` 6.2.4**
(note the capitalized "GDI" in the new package ID) which does support `net8.0-windows`. API is
compatible — `XGraphics`/`PdfDocument`/etc. usage in `UI\MainForm.cs`, `Domain\Elements\Room.cs`,
`Domain\Elements\Connection.cs`, `Domain\Misc\Drawing.cs` did not need code changes, only the
package reference.

### 3. `AutoUpdater.NET`

The port initially migrated from a vendored DLL to the `AutoUpdater.NET.Official` NuGet
package (1.9.3). The update checker and its dependency have since been removed; releases are
downloaded manually from the repository.

### 4. Keep project boundaries explicit

The application and automated tests now live in separate project roots:
`src/Trizbort/` and `src/Trizbort.Tests/`. SDK-style implicit globs are relative to each
project directory, so manual fixtures/scripts in `tests/manual/` and root-level assets are not
compiled into the app project. Keep future project-specific source/resources under that
project's directory and add intentional cross-project dependencies with `ProjectReference`.

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

### 7. `CA1416` platform-compatibility analyzer noise

Targeting plain `net8.0-windows` produced ~2100 `CA1416` warnings for Windows-only API calls.
Trying the `net8.0-windows7.0` TFM did not reduce the count. Since this app is intentionally
Windows-only, warnings were suppressed via:
```xml
<NoWarn>$(NoWarn);CA1416</NoWarn>
```
in `Trizbort.csproj`. This is a deliberate, justified suppression — don't "fix" it by removing
the `NoWarn` without expecting ~2100 warnings to reappear.

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

## Safety note for anyone testing this app interactively in a shared/non-sandboxed environment

**Do not take full-screen/desktop screenshots to verify UI changes in a shared environment** —
there is no guarantee the capture is scoped to an isolated sandbox; it can capture another
user's live desktop. Prefer process status checks (`HasExited`, `MainWindowTitle`), log output,
and scoped/headless repro harnesses (as described above) instead.

## Release process

The legacy ClickOnce distribution (`Trizbort.application` pointing at `trizbort.com`) is dead
and is **not** produced any more. Releases are built by `.github/workflows/release.yml`:

1. Push the workflow/app changes, then an increasing tag `vMAJOR.MINOR.PATCH[.REVISION]`
   (e.g. `git tag v1.8.0`, then `git push leeoades v1.8.0`). Prerelease suffixes are rejected.
2. The workflow (windows-latest) stamps `AssemblyVersion`/`AssemblyFileVersion` in
   `src/Trizbort/Properties/AssemblyInfo.cs` from the tag (padded to 4 parts) — the committed
   version remains the version used for local builds; release stamping is not committed.
3. Runs `dotnet test`, then `dotnet publish` as a self-contained, compressed single-file
   `win-x64` `Trizbort.exe` (no .NET install required).
4. Zips it (plus `Trizbort.dll.config` and licence files) as `Trizbort-<version>-win-x64.zip`
   and creates a GitHub Release with the zip asset and generated notes.

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
uncategorized). Examples:

```
dotnet test Trizbort.sln -c Debug
dotnet test Trizbort.sln -c Release
dotnet test Trizbort.sln -c Debug --filter "TestCategory=Integration|TestCategory=Rendering"
dotnet test Trizbort.sln -c Debug --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
```

`coverlet.collector` produces Cobertura XML under the results directory. `coverage.runsettings`
includes only application code and excludes generated designer/compiler code. Coverage is a
guide to missing behavior, not a promise of completeness; there is no percentage gate.
`.github\workflows\tests.yml` runs Release tests on Windows for PRs and branch pushes and uploads
TRX/coverage artifacts even on failure (14-day retention). The release workflow is unchanged.

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
