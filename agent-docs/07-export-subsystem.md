# Export Subsystem: IF-Language Source, Images, PDF

See [`README.md`](README.md) for the doc map. For `Room`/`Connection`/`Project` semantics, see
[`02-domain-model.md`](02-domain-model.md).

## Layout

- `Export\CodeExporter.cs` — abstract base shared by every IF-language generator. All common
  "turn `Project.Elements` into export-friendly objects" logic lives here.
- `Export\Domain\` — export-time DTOs: `ExportRegion`, `Exit`, `Location` (export view of a
  `Room`), `Thing` (parsed from a room's free-text `Objects` field).
- `Export\Languages\` — one file per target language:
  `Inform6Exporter.cs`, `Inform7Exporter.cs`, `TadsExporter.cs`, `AlanExporter.cs`,
  `HugoExporter.cs`, `ZilExporter.cs`, `QuestExporter.cs` (contains both `QuestExporter` and
  `QuestRoomsExporter`), `AdventuronExporter.cs`.
- Image export remains in `UI\MainForm.cs`; PDF orchestration lives in internal
  `Export\MapPdfExporter.cs`, called by the form (see below). Both reuse the same
  `Canvas`/`XGraphics` drawing code used for on-screen rendering.

## `CodeExporter` contract

```csharp
public abstract partial class CodeExporter : IDisposable
```

Every language exporter must implement:

- `FileDialogFilters`, `FileDialogTitle` — save-dialog UI metadata.
- `ReservedWords` — language-specific identifiers that generated names must avoid colliding
  with.
- `ExportContent(TextWriter writer)` — the language-specific body.
- `ExportHeader(TextWriter writer, string title, string author, string description, string
  history)` — the language-specific header/preamble.
- `GetExportName(Room room, int? suffix)` / `GetExportName(string displayName, int? suffix)` —
  produce valid, unique language identifiers from arbitrary room/object display names.

Public entry points: `string Export()` (returns generated source as a string) and
`void Export(string fileName)` (writes directly to a file). Both call `prepareContent()` first.
Preparation clears prior locations/regions/name lookup state, so an exporter instance can be
reused after map edits or switching projects without accumulating output.

### `prepareContent()` — shared input preparation (same for every language)

1. `findRegions()` — iterates the map-level `Settings.Regions` list (not `Room.Region` directly),
   excludes the default region, and maps each to an `ExportRegion` with a unique generated name.
   Note this list can contain regions no room actually uses — exporters rely on it for
   name/lookup purposes, not as a "distinct regions actually in use" computation.
2. `findRooms()` — iterates `Project.Current.Elements.OfType<Room>()`, builds a `Location` per
   room with a unique generated name.
3. `findExits()` — iterates `Project.Current.Elements.OfType<Connection>()`, resolves each
   connection's source/target room + compass point (via `Connection.GetSourceRoom`/
   `GetTargetRoom`, see [`02-domain-model.md`](02-domain-model.md)), and records `Exit` objects.
4. `pickBestExits()` — when multiple connections could serve as "the" exit in a given direction,
   picks the preferred one.
5. `findThings()` — parses each room's free-text `Objects` field into structured `Thing`
   objects (`Export\Domain\Thing.cs`) via the shared `Domain\Misc\ObjectList.Parse` (also used by
   `Room.Draw` for map display). **Indentation indicates containment** (a more-indented
   line is "inside" the preceding less-indented one); indentation may be spaces, tabs, or
   leading `-`/`*`/`•` bullets (`- Pouch`, `-- Gem`; a bullet run only counts if followed by
   whitespace). On the map, nested objects are drawn indented with a `•` marker
   (`ObjectList.FormatForDisplay`), and the Room Properties Objects box supports Tab/Shift+Tab
   and auto-continued bullets on Enter (`UI\ObjectListEditor`). Bracketed text (`[...]`) is **not**
   key/value properties — `Thing`'s constructor treats the bracket contents as a compact string
   of single-character flags, matched with `.Contains(...)`: `f`/`m`/`p` (gender/force-person),
   `1`/`2` (force singular/plural), `c` (container), `s` (scenery), `u` (supporter), `w` (worn),
   `h` (part-of), `!` (proper-named). Any other character inside the brackets produces a
   `WarningText` validation message. This free-text-to-structured-object parser is shared by all
   exporters — if you need to support a new object property/syntax, change it here rather than
   per-language.

Because this preparation is shared, **a bug in room/exit/object resolution usually affects every
exporter at once** — check `CodeExporter` before assuming a bug is language-specific.

## Worked example: Inform 7 (`Export\Languages\Inform7Exporter.cs`)

- `ExportHeader()` emits story title/author/description, a generated-map "volume", and a
  history "chapter".
- `ExportContent()` emits regionless rooms first, then each region as an Inform 7
  book+region, calling `printThisLoc()` per room.
- `printThisLoc()` emits the room declaration (`Room.PrimaryDescription`, dark-room state,
  region membership, start-room marker), then iterates `Location.Things` to emit object prose
  (article selection, type/properties via `whatItIs()` — handles persons/scenery/containers/
  supporters/plural/proper-named objects — containment as `in`/`part of`/`carried by`/`worn
  by`, plus `Understand ... as ...` synonyms), then loops `Directions.AllDirections` and, for
  each direction with a `GetBestExit`, emits either a plain exit (`writeNormalExit`) or a door
  (`writeDoor`, including lock state).
- Reciprocal connections are marked exported once they're emitted in one direction so they
  aren't duplicated; genuinely one-way exits emit the opposite direction as explicitly
  `nowhere`. Conditional exits become "Instead of going ..." rules.
- `getInform7Name()` maps `MappableDirection` → Inform 7 direction names.

Other language exporters follow the same shape (prepared `Location`/`Exit`/`Thing` data →
language-specific header/body/direction-name/object-prose methods) — use Inform7Exporter as the
template when adding a new target language, and look at the sibling exporter closest to the new
target's syntax for conventions.

## Adding a new export language

1. Create `Export\Languages\<Name>Exporter.cs` deriving from `CodeExporter`.
2. Implement the abstract members listed above.
3. Add a CLI option in `CommandLineOptions.cs` if it should be scriptable (see
   [`08-cli-and-entry-point.md`](08-cli-and-entry-point.md)) and a menu item in
   `MainForm.Designer.cs`/`MainForm.cs` calling the generic `exportCode<TExporter>()` helper.
4. There is no plugin/registry mechanism — new exporters are wired in explicitly at both the CLI
   dispatch and the menu level.

## Image export

`UI\MainForm.cs`: `FileExportImageMenuItem_Click()` → `saveImage(string fileName)`. Supports
PNG/JPEG/BMP/EMF. Computes canvas content bounds, creates a GDI bitmap or enhanced metafile,
calls `Canvas.Draw(...)` (the **same** draw method used for on-screen rendering — see
[`05-ui-and-canvas.md`](05-ui-and-canvas.md)) against it, and saves the result.

## PDF export

`UI\MainForm.cs`: `FileExportPDFMenuItem_Click()` → `savePDF(string fileName)` →
`MapPdfExporter.Save(Canvas, fileName)`. Creates a
`PdfDocument`/page sized to the canvas content bounds, renders via
`XGraphics.FromPdfPage(...)` + `Canvas.Draw(...)`, and adds room descriptions as
`PdfTextAnnotation` notes.
Page dimensions are at least one point, including horizontal/vertical connector-only maps.
The form retains export-filename preference updates. Tests call this same production exporter,
reopen the PDF and check page dimensions, metadata, room-description annotations and bounds;
Canvas drawing restores the previous viewport.

Regression tests cover all nine code exporter variants, shared identifier/region/exit/object/
door preparation, updated/repeated exports and annotation exclusion. Quest's random game ID is
validated then normalized for repeat comparisons; XML outputs are parsed structurally.
Adventuron intentionally exports rooms only. These tests do not compile outputs with external
IF tools or establish complete language-generator correctness.

Uses the **`PDFsharp-GDI` 6.2.4** NuGet package (`PackageReference` in `src/Trizbort/Trizbort.csproj`) — this
replaced the unmaintained `PDFsharp-gdi` 1.50.5147 package during the .NET 8 port (see
[`09-build-test-and-dotnet8-port.md`](09-build-test-and-dotnet8-port.md)). `PdfSharp.Drawing`
types (`XGraphics` etc.) are also used directly inside domain drawing code
(`Room.cs`/`Connection.cs`/`Domain\Misc\Drawing.cs`), not just in the export path.

## Text formatting note

There is **no shared word-wrap/text-formatting helper used across exporters** — each language
file formats strings locally (e.g. Inform 7's printable-string/`Understand` helpers, Inform 6's
word conversion, ZIL's object-word formatting). `Domain\Misc\TextBlock.cs` (word-wrap with
dash-splitting controlled by `Settings.WrapTextAtDashes`) exists but is used for on-canvas room/
connection text rendering, **not** for generated export source — don't assume exporters reuse
it.
