# 105 — CLI code export flags (--inform6/--inform7/--tads/--alan/--hugo/--zil/--quest/--quest rooms)

Matrix rows: A002, E015 (export part), E014          Priority: P?          Size: M
Depends on: 104, 106                                  Suggested tier: mid

## What
After loading the map named on the command line, each export flag writes that language's source to the given file, with no dialogs. Combined with `--exit` the app closes afterwards (107). Errors go to stderr and set a non-zero exit code (improvement over the reference, which stays silent; flagged in Notes below).

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `ExportCodeCl<T>`, and the flag block in `MainForm_Load` (lines ~194–243)
- `src/Trizbort.Core/Domain/Application/CommandLineOptions.cs` — `I6`, `I7`, `Tads`, `Alan`, `Hugo`, `Zil`, `Quest`, `QuestRooms`
- Note: the option name `"quest rooms"` contains a space and can never be typed; define it as `--questrooms` (reference is effectively broken here; no `--adventuron` exists in the reference but add `--adventuron` for completeness since the exporter exists).

## Implement
1. Core: `HeadlessExport.Run(CommandLineOptions o, Project p)` in `src/Trizbort.Core/Export/HeadlessExport.cs` using `CodeExporter.ExportToFile` (from 106) per non-empty flag; returns a list of `(flag, file, error?)`.
2. Core: rename the Quest-rooms option to `questrooms`.
3. Avalonia: `StartupActions` (104) calls it after load when any export flag is set; writes `Exported <lang> to <file>` to stdout and errors to stderr; exit code stored in `App.ExitCode` and returned from `Main`.
4. Notes for Lee: reference exit codes were always 0; this ticket introduces 0 = ok, 1 = any failure. Reverting is a one-line change.

## Tests
- Core: `src/Trizbort.Core.Tests/HeadlessExportTests.cs` — load `samples/` map, export each language to a temp dir, files non-empty; invalid folder reports an error.
- Avalonia: none.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Matrix rows A002, E014, E015 updated; committed as `parity(105): CLI code export`.

## Out of scope
- `--exit`, `--smartsave`, `--automap`, `--quicksave` (107). Interactive exports (118).
