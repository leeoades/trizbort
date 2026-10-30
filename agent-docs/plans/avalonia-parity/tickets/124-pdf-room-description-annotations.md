# 124 — PDF room-description annotations

Matrix rows: E028          Priority: P?          Size: M
Depends on: 123 (only if Lee picks option C in 123)          Suggested tier: mid

## What
In the exported PDF each room with a description gets a text annotation (note icon) positioned at the room, containing the description, as in the reference. Skip this ticket if Lee prefers Skia-only PDF (see decision in 123).

## Reference (read only these)
- `$REF/src/Trizbort/Export/MapPdfExporter.cs` — annotation block (lines ~39–57)

## Implement
1. Add NuGet `PDFsharp` (core build, no GDI/WPF dependency; check current stable 6.x package name and MIT licence before adding — stop and ask if the licence has changed).
2. Core: `PdfAnnotationPlan.Build(Project, mapBounds)` returns `(rect in PDF page coordinates, text)` per room with a non-empty description (Y flipped; room rectangle scaled by page scale). Pure and testable.
3. Avalonia: after the Skia PDF is written (123), open with `PdfReader.Open(path, PdfDocumentOpenMode.Modify)`, add `PdfTextAnnotation` per plan entry, save to a temp file then replace the original. Keep the colour (reference: orange) and `Open = false`.
4. Setting: none; annotations always on (as reference).

## Tests
- Core: `PdfAnnotationPlanTests.cs` — coordinates, flipping, skips empty descriptions.
- Avalonia: re-open the exported PDF with PDFsharp and assert annotation count equals rooms with descriptions.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- E028 -> Implemented; committed as `parity(124): PDF annotations`.

## Out of scope
- Link annotations between rooms.
