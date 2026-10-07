# CLI & Entry Point

See [`README.md`](README.md) for the doc map.

## Startup

`Program.cs` is the executable entry point (`[STAThread] static void Main(string[] args)`):
enables WinForms visual styles, then constructs and runs `UI\MainForm`. It does **not** parse
command-line arguments itself — all CLI handling happens inside `MainForm`.

`MainForm` parses arguments with CommandLineParser:
```csharp
Parser.Default.ParseArguments<CommandLineOptions>(args)
```
and dispatches recognized options via `commandLineActions(...)`.

## `Domain\Application\CommandLineOptions.cs` — all flags

| Flag | Property | Effect |
|---|---|---|
| positional `[Value(1)]` | `FileName` | Opens this map file on startup (`OpenProject(options.FileName)`). |
| `-a` / `--loadlastproject` | `LoadLastProject` | Reopen the last-used project (from `ApplicationSettings`, see [`03-storage-and-persistence.md`](03-storage-and-persistence.md)). |
| `-m` / `--automap <transcript>` | `Transcript` | Run automap against the given transcript file — see below and [`06-automap.md`](06-automap.md). |
| `-q` / `--quicksave <path>` | `QuickSave` | Save the (possibly automapped) project to this path non-interactively. |
| `-s` / `--smartsave` | `SmartSave` | After opening a file, also run "smart save" (saves generated image/PDF outputs in addition to the map, per the broader save pipeline). |
| `-n` / `--name <name>` | `Name` | Name the current map (declared; verify current consumption in `MainForm` before relying on it — not strongly wired at last inspection). |
| `-x` / `--exit` | `Exit` | Exit after processing other flags — use this for scriptable/batch automation. |
| `--inform6` / `--inform7` / `--tads` / `--alan` / `--hugo` / `--zil` / `--quest` / `--quest rooms` | `I6`/`I7`/`Tads`/`Alan`/`Hugo`/`Zil`/`Quest`/`QuestRooms` | Export to the given language/format non-interactively — see [`07-export-subsystem.md`](07-export-subsystem.md). |

## CLI automap flow

```
--automap <transcript>
  → MainForm.commandLineActions() sees options.Transcript != null
  → clAutoMap(options):
      1. copies the transcript path into ApplicationSettingsController.AppSettings.Automap.FileName
      2. await Canvas.StartAutomapping(cmdLineAutomap, justParseFile: true)
         → Automap.StartCL(...)   (full-file parse, not live/continuing mode)
      3. Canvas.StopAutomapping()
      4. if --quicksave was also given, saveAsCmdLineProject(options.QuickSave)
      5. Project.Current.IsDirty = false
```

See [`06-automap.md`](06-automap.md) for what `Automap.StartCL` actually does to the transcript.

## CLI export flow

```
--inform7 <path>  (etc.)
  → MainForm.commandLineActions() sees the relevant option non-empty
  → exportCodeCl<TExporter>(path)
     → new TExporter(...).Export(path)      // same CodeExporter used interactively
```
Mapping: `I6→Inform6Exporter`, `I7→Inform7Exporter`, `Tads→TadsExporter`, `Alan→AlanExporter`,
`Hugo→HugoExporter`, `Zil→ZilExporter`, `Quest→QuestExporter`,
`QuestRooms→QuestRoomsExporter`. All export flags can combine with each other and with
`--exit` in a single invocation (`MainForm.commandLineActions`, `UI\MainForm.cs:207-249`).

## CLI save flow

```
--quicksave <path>                          (only applied directly if --automap was NOT also given;
                                              when both are given, clAutoMap's own quicksave handling runs instead)
  → saveAsCmdLineProject(path)
     → Project.Save() / MapSaver    (same pipeline as interactive Ctrl+S — see 03-storage-and-persistence.md)
```

`--smartsave` only takes effect when a positional `FileName` was opened and no `--automap`/
`--loadlastproject` already loaded a project (`options.FileName != null && !projectLoaded` at
`UI\MainForm.cs:221-229`); it then calls `smartSave(true)` after opening, which saves any
configured image/PDF outputs in addition to the map itself.

`--exit` calls `Close()` as the very last step of `commandLineActions`, after all export/save
flags have already run (`UI\MainForm.cs:247`) — so it's safe to combine `--exit` with any
combination of the other flags in one invocation.

`-n`/`--name` (`options.Name`) is declared in `CommandLineOptions` but is **not read anywhere**
in `MainForm.commandLineActions` or `clAutoMap` as of this writing — treat it as currently a
no-op if you see it used, and double-check before relying on it.


## Example invocations (for manual testing)

```
Trizbort.exe mygame.trizbort
Trizbort.exe --automap transcript.txt --quicksave mygame.trizbort --exit
Trizbort.exe mygame.trizbort --inform7 story.ni --exit
```
