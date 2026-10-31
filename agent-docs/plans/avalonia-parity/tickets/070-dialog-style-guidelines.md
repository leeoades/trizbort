# 070 — Define and apply shared dialog style guidelines

Matrix rows: D001–D007, D015, D016          Priority: P?          Size: M
Depends on: none                             Suggested tier: mid

## What
Dialogs 071–090 all look and behave consistently: same spacing, group headers, label alignment, access keys, minimum sizes and theme behaviour. This ticket creates the shared styles and a short guideline document; the other tickets apply them.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/*.md` — "Context checklist" tables (what was lost: grouping, hints, access keys, clipped default sizes).
- `src/Trizbort.Avalonia/Views/Dialogs/*.axaml`, `src/Trizbort.Avalonia/Views/AboutDialog.axaml` — current ad-hoc layouts.
- `src/Trizbort.Avalonia/App.axaml` and its `Styles/` (grep for existing styles) — where to add shared styles.
- Screenshots in `screenshots/winforms/` for group-box look (e.g. `ConnectionPropertiesDialog__main__default.png`).

## Implement
1. Avalonia: add `Styles/DialogStyles.axaml` (included from `App.axaml`) with style classes, using theme resources only (no hard-coded colours, so Light and Dark both work):
   - `Expander`-free **GroupBox**: a `HeaderedContentControl` template `Classes="group"` with a bold header, a thin border using `SystemControlForegroundBaseLowBrush`, 12 px inner padding. Used wherever the legacy dialog had a group box.
   - `TextBlock.hint` (muted, wrapping, 12 px) for intro/help text; `TextBlock.field-label` (right-aligned or top-aligned per rule below); `TextBlock.error` (red, for validation messages).
   - Standard `Grid` metrics: label column `Auto` with `SharedSizeGroup="L"` (set `Grid.IsSharedSizeScope` on the dialog root) so labels never clip; 8 px row gap, 12 px column gap, 16 px window padding; button row right-aligned with 8 px gap, OK left of Cancel (legacy order), `MinWidth=80`.
2. Rules (write them in `agent-docs/dialog-guidelines.md`, one page, with a short XAML skeleton; this is the one doc this ticket may add):
   - Labels sit left of single-line fields, above multi-line fields; end with a colon; use the legacy label wording unless a dialog ticket says otherwise.
   - Access keys: `_` prefix on every label/checkbox/button with a unique letter per dialog (per tab); labels target their field with `Target="{Binding #Field}"`; OK/Cancel are `IsDefault`/`IsCancel`. Avoid keys that clash with the window-level shortcuts.
   - Tab order = visual order; set `TabIndex` only when visual order differs; initial focus set via `FocusManager`/`Loaded` handler to the first field (select all text).
   - Dependent controls: use `IsEnabled` bindings (or `IsVisible` when legacy hid them) and place dependants directly below/right of their parent, indented 16 px.
   - Sizing: set `Width`/`Height` so every control is visible without scrolling at the default size on 96 DPI with the default font on Windows and Linux; `MinWidth`/`MinHeight` = 85 % of default; text areas stretch (`*` rows), never fixed height; keep `ScrollViewer` only as a safety net. Prefer `SizeToContent="Height"` + `MaxHeight` for dialogs without growing text areas.
   - Tooltips: `ToolTip.Tip` on any control whose purpose is not obvious from its label, wording copied from legacy where it existed.
   - Validation: message in `TextBlock.error` under the field; OK stays enabled but focuses the offending field (legacy behaviour), or OK disabled with a visible reason — pick the first.
3. Add a base `DialogWindow` class (code-behind only, `src/Trizbort.Avalonia/Views/Dialogs/DialogWindow.cs`) with `ShowInTaskbar=false`, `WindowStartupLocation=CenterOwner`, `CanResize` property default true, Esc closes with cancel result, and `FocusFirst()` helper. Convert **one** dialog (About, `AboutDialog.axaml`) to it as a demonstration; other tickets convert the rest.
4. Add a "dialog audit" headless test helper `DialogLayoutAssert.AssertNoClipping(Window)`: opens the window at its default size, arranges it, and fails if any visible `Control` with `IsVisible` and `Focusable` has bounds outside the client area or a `ScrollViewer` has `Extent.Height > Viewport.Height` (tolerance 1 px). Also `AssertAccessKeysUnique(Window)` (no duplicate `_x` letter among visible controls of the same tab) and `AssertThemeBrushesOnly` is NOT required; instead verify manually in both themes (see Done when).

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/DialogLayoutAssertTests.cs` — helper passes on a sample window, fails on a deliberately clipped one; About dialog passes both assertions.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Guideline doc and helpers committed; About uses `DialogWindow`.
- Manually checked in Light and Dark theme (screenshot attached to PR notes).
- Matrix rows: add "style guidelines: 070" to Notes of D001–D007, D015.
- Committed as `parity(070): shared dialog style guidelines`.

## Out of scope
- Converting the other dialogs (tickets 071–090).
- A new theming system; macOS native dialogs.
