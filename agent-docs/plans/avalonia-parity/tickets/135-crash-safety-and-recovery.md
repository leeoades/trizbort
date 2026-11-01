# 135 Crash safety: recovery copy of unsaved work, crash log, no silent crashes
Requested by Lee (2026-11-01) after GitHub #24 crashed the app: "We basically don't want to lose work if at all possible."

## Behaviour
- **Recovery copy (on by default).** While the open map has unsaved changes, keep a copy in the per-user config folder
  (`AppPaths.ConfigFolder/recovery/`). Write it about 2 seconds after the last change (debounced, off the hot path),
  atomically (write a temp file, then move over). Files per app instance: `<session>.trizbort` (normal map format via
  MapSaver) and `<session>.json` (original file name or empty, map title, saved time UTC, app version).
- **Which instance owns it.** Each running instance holds `<session>.lock` open exclusively for its lifetime. A copy
  whose lock is not held belongs to an instance that ended without cleaning up.
- **Clean up.** Delete this instance's copy when the map is saved (no longer dirty), when the user discards changes
  (New/Open/close after "Don't save"), and on a normal exit.
- **On start**, before reopening the last map or a command-line file: for each orphaned copy ask
  "Trizbort Recast didn't close properly. Recover your unsaved changes to <title> (from <time>)?" with
  Recover / Discard / Decide later. Recover opens the copy as an unsaved map with the original file name (so Save writes
  there; Save As offered if there was none). Discard deletes it. Decide later keeps it for next start.
- **If something goes wrong:** handle `Dispatcher.UIThread.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException`
  and `TaskScheduler.UnobservedTaskException`: write the recovery copy immediately (best effort, never throw), write a
  crash log (`AppPaths.ConfigFolder/crash-logs/<UTC timestamp>.txt`: version, OS, exception), and for UI-thread errors show
  "Something went wrong: <message>. Your unsaved work has been kept and a crash log written to <path>." then carry on
  (mark handled) rather than closing.
- **Commands:** `CatalogueCommandAdapter.Execute` must not let a command's exception (sync or faulted task) escape or be
  swallowed to Debug only: report it through the same error path.
- **Setting:** App Settings, General: "Keep a recovery copy of unsaved changes" (default on). Off = no copies written
  (the crash log and the error message still happen).
