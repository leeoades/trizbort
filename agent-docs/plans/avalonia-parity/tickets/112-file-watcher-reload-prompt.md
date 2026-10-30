# 112 — Reload prompt when the open map changes on disk

Matrix rows: A005          Priority: P?          Size: S
Depends on: 011          Suggested tier: mid

## What
If another program modifies the open file, the user is asked whether to reload ("map modified by another program"; warns about losing unsaved changes when dirty). The watcher is suspended during the app's own saves and re-armed after Save As. Fixes I-003.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Watchers/TrizbortFileWatcher.cs` — change handler (lines 55–67)
- `src/Trizbort.Core/Domain/Watchers/TrizbortFileWatcher.cs` — `ConfirmReloadPrompt` (`Func<bool,bool>`), `UIThreadInvoker`
- `src/Trizbort.Core/Domain/Application/Project.cs` — `MapReloader` (line 23), reload call (~215)
- `$REF/src/Trizbort/UI/MainForm.cs` — where the reference assigns these three hooks (grep -n `MapReloader`)

## Implement
1. Replace the sync `Func<bool,bool>` with `Func<bool, Task<bool>> ConfirmReloadPromptAsync` in Core (keep the old property as an adapter for legacy tests), built on `IEditorPrompts.ConfirmAsync` from 011.
2. Avalonia: at startup set `UIThreadInvoker = a => Dispatcher.UIThread.Post(a)`, the prompt (message "The map was modified by another program. Reload it? Unsaved changes will be lost." when dirty), and `Project.MapReloader = path => OpenMapFromPathAsync(path)` (load without the unsaved prompt). Selection is cleared on reload; the view keeps origin/zoom.
3. Debounce duplicate events (editors fire 2–3 per save); one prompt at a time.
4. Verify the watcher path is the full path and that editors saving via rename (write temp, rename) still trigger on Linux; add a note to the matrix if `FileSystemWatcher` needs `Renamed` handling and implement it if so.
5. Suspend/resume around `SaveMapAsync` (check existing calls in `Project.Save`).

## Tests
- Core: `src/Trizbort.Core.Tests/FileWatcherTests.cs` — with a temp file, modify and assert prompt called once; declining does not reload; own save does not prompt.

## Done when
- Build + Core + Avalonia tests pass on Linux (watcher tests may use a polling timeout of 5 s).
- A005 -> Implemented; I-003 `Ticket: 112`; committed as `parity(112): file watcher reload`.

## Out of scope
- Merge/diff of external changes.
