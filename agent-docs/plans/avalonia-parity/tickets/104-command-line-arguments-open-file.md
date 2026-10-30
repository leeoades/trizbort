# 104 — Parse command-line arguments and open the named map on launch

Matrix rows: A001, A019 (parsing only)          Priority: P?          Size: M
Depends on: 100                                 Suggested tier: mid

## What
`trizbort [file.trizbort]` opens that map after the window appears; parse errors and unknown options are ignored (no crash, no help text), as in the reference. A single parsing and startup-action entry point is created that tickets 103, 105, 107 and 108 extend. New in Avalonia: `--debug` and `--config-dir <path>` are recognised (`--debug` is consumed by 108/055; `--config-dir` by 100 and tests).

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `MainForm_Load` (grep -n `CommandLineOptions`, `Parser.Default`)
- `src/Trizbort.Core/Domain/Application/CommandLineOptions.cs` (copied from reference; uses CommandLineParser)
- `agent-docs/08-cli-and-entry-point.md`

## Implement
1. Core: extend `CommandLineOptions` with `[Option("debug")] bool Debug` and `[Option("config-dir")] string ConfigDir`. Add `CommandLineParsing.Parse(string[] args)` returning `CommandLineOptions` (never null, never throws; uses `Parser` with `HelpWriter = null`, `IgnoreUnknownArguments = true`). Note reference `Value(0) Executable` assumes args include the exe path (`Environment.GetCommandLineArgs()`); define Avalonia behaviour as: first positional argument = file name, and drop the `Executable` property use.
2. Avalonia: `Program.Main` stores parsed options in `App.Options` before `StartWithClassicDesktopLifetime`; applies `ConfigDir` to `AppPaths.OverrideConfigFolder` (100) first.
3. Avalonia: add `StartupActions.RunAsync(MainWindowViewModel vm, CommandLineOptions o)` called from `MainWindow.Opened` (once). First action: if `o.FileName` is a URL or existing file, load it via `IStorageService.OpenMapFromPathAsync(path)` (new; shared by 102/103) and add to recent list (101). Relative paths resolve against the current directory at launch.
4. A missing named file shows a message ("File not found: …") and leaves a new empty map.

## Tests
- Core: `src/Trizbort.Core.Tests/CommandLineParsingTests.cs` — file only, flags only, unknown flags ignored, quoted path with spaces, `--debug`, empty args.
- Avalonia: `StartupTests.cs` — opening a sample from `samples/` through `StartupActions` with a fake storage.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Matrix row A001 updated (A019 partly); committed as `parity(104): command-line open file`.

## Out of scope
- File association registration (109). Export/automap/save flags (105, 107). Single-instance hand-off (reference has none; A028).
