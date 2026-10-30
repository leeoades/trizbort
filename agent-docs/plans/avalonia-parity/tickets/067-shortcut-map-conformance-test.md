# 067 — Add shortcut-map conformance and modifier-exactness tests

Matrix rows: (all K and M gesture rows)          Priority: P2          Size: M
Depends on: 010, 021, 025, 027, 029, 030, 031, 032, 047, 048, 050        Suggested tier: mid

## What
A Core test fails when the catalogue deviates from the approved `shortcut-map.md`: no duplicate gesture per scope, bare-letter canvas commands never fire with Primary/Alt, macOS variants resolve, every K-row command id exists.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/shortcut-map.md` — section 2 table and section 4 macOS variants (read only these)

## Implement
1. Core: `src/Trizbort.Core.Tests/Editor/ShortcutMapConformanceTests.cs` with an in-test expected table (id, gesture, scope, macOS gesture). Add a key-dispatch test: `Primary+V` dispatches paste not reverse, `Primary+D` duplicate not Down, `Primary+Shift+A` regions not Select All.
2. Do not weaken the test to pass; file a ticket against the offending command instead.

## Tests
- The test file above.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(067): Add shortcut-map conformance and modifier-exactness tests`.

## Out of scope
- Avalonia key translation (010).
