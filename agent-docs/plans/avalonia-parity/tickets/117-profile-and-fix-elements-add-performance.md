# 117 — Profile and fix slow Elements.Add on large maps

Matrix rows: (none; performance) — issues I-009          Priority: P?          Size: M
Depends on: none          Suggested tier: mid (strong-tier review of the diff)

## What
Adding 1,000 rooms and 1,936 connections one by one through `Project.Elements.Add` takes ~11 s but loading the same map takes 0.13 s. Find the cause with measurements, then fix it so adds scale roughly linearly (target: the same build < 1 s). Affects automap, paste and undo restore.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/issues.md` — I-009
- `src/Trizbort.Core/Domain/Misc/BoundList.cs` — `Add`, `AddRange`, `Added`/`Removed` events
- `src/Trizbort.Core/Domain/Application/Project.cs` — `Elements` (line 52), `OnElementRemoved`, and whatever subscribes to `Elements.Added` (grep -n `Elements.Added`)
- `src/Trizbort.Core/Domain/Application/LegacyMapFileEngine.cs` lines 88–105 (load path that is fast)
- `agent-docs/plans/avalonia-parity/decisions.md` D1 undo benchmark (the script `scripts/bench/undo-snapshot-bench.cs` was run on Lee's PC and is not in this snapshot; recreate a minimal benchmark if absent)

## Implement
1. Measure first. Add `src/Trizbort.Core.Tests/Performance/ElementsAddBenchmarkTests.cs` (skipped unless env `TRIZBORT_PERF=1`) that builds N=250/500/1,000 rooms plus connections and records elapsed per phase with `Stopwatch`. Run with a sampling profiler if available (`dotnet-trace`) or manual stopwatch bisecting. Write the hot spots (method names and share) in the ticket's Notes section and the commit message.
2. Suspected causes to check, in order: per-add handlers on `Elements.Added` that rescan all elements (name uniqueness, id lookup `foreach (var existing in Elements.Where(...))` in `Project.cs` line ~131, validation `CheckValidation`, connection port lookups); `OnElementRemoved` quadratic work; change events (`IsDirty`, `Changed`) raised per add; list `Contains` before add.
3. Fix with the smallest change: index by id (`Dictionary<int,Element>`), defer validation (`BeginBulk()/EndBulk()` suppressing per-add events and raising one aggregate), keep public API. The loader and `CopyController.Paste` call the bulk API.
4. Keep behaviour identical: all existing tests stay green.

## Tests
- Core: the benchmark above with a loose threshold (1,000-room build < 3 s on CI, guard against regression), plus `ElementsBulkTests.cs` — events still raised once per add outside bulk mode, once after `EndBulk`.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- I-009 gets `Ticket: 117` with before/after numbers; committed as `parity(117): faster Elements.Add`.

## Out of scope
- Rendering performance, undo snapshot storage (003).
