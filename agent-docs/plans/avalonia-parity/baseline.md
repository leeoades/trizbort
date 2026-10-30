# Baseline (tasks T0.1 / T0.2)

Recorded 2026-10-30 with `scripts/run-windows-checks.ps1` on Lee's PC, right after the legacy/new solution split.

| Field | Value |
|---|---|
| Commit | `71375b3` (`port-to-linux`), clean tree |
| OS | Windows 10.0.26200 |
| SDKs | 10.0.114, 10.0.204, 10.0.303 |
| Desktop runtimes | 8.0.33, 9.0.22, 10.0.8, 10.0.11, 10.0.14 |

## New app — `Trizbort.sln`

| Step | Result |
|---|---|
| Build (Debug) | Succeeded, 0 errors, 2 warnings — both CA2024 (`reader.EndOfStream` in async method) in `src/Trizbort.Core/Automap/Automap.cs` lines 999 and 1022 |
| `Trizbort.Core.Tests` | 394 passed, 0 failed, 0 skipped |
| `Trizbort.Avalonia.Tests` | 121 passed, 0 failed, 1 skipped (`CaptureAvaloniaManualFixtureScreenshots` — capture-only, skips without `TRIZBORT_CAPTURE_OUTPUT`) |

## Legacy app — `legacy/Trizbort.Legacy.sln` (original v2.3.1-beta.2)

| Step | Result |
|---|---|
| Build (Debug) | Succeeded, 0 errors, 0 warnings |
| `Trizbort.Tests` (net8.0-windows) | 568 passed, 0 failed, 0 skipped |

## Reference

`$REF` = `legacy/`. History check: `git log --follow legacy/src/Trizbort/UI/MainForm.cs` reaches `c29912f` (2013-03-16, "Initial commit of v1.2.1").

Not yet done: launching the Avalonia app and opening each `samples/*.trizbort` by hand (T0.1 step 4) — folded into the smoke-test script (T2.4).
