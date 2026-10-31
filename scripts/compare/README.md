# Export comparison (legacy vs new app)

Both apps write the same set of files per sample map, then `compare_exports.py` diffs them.

Output layout: `<output>/<legacy|new>/<map-name>/{map.png, map.pdf, inform7.txt, inform6.txt, tads.txt, hugo.txt, zil.txt, quest.txt, alan.txt, adventuron.txt, errors.txt}`.
Maps come from `samples/` and `tests/manual/`.

## 1. Capture (Windows, from the repo root)

Legacy app (writes `<output>/legacy/...`):

    set TRIZBORT_COMPARE_OUTPUT=C:\temp\compare
    dotnet test legacy/Trizbort.Legacy.sln --filter "FullyQualifiedName~MapExportCaptureTests"

New app (writes `<output>/new/...`):

    set TRIZBORT_COMPARE_OUTPUT=C:\temp\compare
    dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj --filter "FullyQualifiedName~MapExportCaptureTests"

The tests are `[Explicit]` and ignore themselves when the variable is unset. If a runner skips explicit tests
despite the filter, select the test by exact name (`--filter "Name=CaptureAllMaps"`).

## 2. Compare

    pip install pillow numpy pypdfium2 pypdf     # pypdfium2 and pypdf are optional
    python scripts/compare/compare_exports.py C:\temp\compare

Writes `<output>/report/index.html` (maps sorted by worst image score), heat-maps, side-by-side images and
`.diff` files for code exports. The image score is the percentage of pixels whose blurred difference exceeds 48
after resizing the new image to the legacy size; a flagged aspect ratio means the sizes differ by more than 2%.
