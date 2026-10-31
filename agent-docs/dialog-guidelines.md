# Avalonia dialog guidelines

Shared styles live in `src/Trizbort.Avalonia/Styles/DialogStyles.axaml` (included from `App.axaml`).
Worked example: `Views/Dialogs/MapLabelPropertiesDialog.axaml`.

## Style classes
- `HeaderedContentControl.group` - group box (bold header, thin border, 12 px padding).
- `TextBlock.hint` - muted 12 px wrapping help text. `TextBlock.error` - validation message.
- `Label.field-label` / `TextBlock.field-label` - vertically centred field label.
- `Button.dialog-button` - MinWidth 80.

## Layout
- Window padding 16; grid row gap 8, column gap 12 (use a 12 px spacer column); group gap 12.
- Label column `Auto`; set `Grid.IsSharedSizeScope="True"` on the root and `SharedSizeGroup="L"` on label columns where groups must align.
- Labels left of single-line fields, above multi-line fields; end with a colon; use legacy wording.
- Button row bottom right, 8 px gap, OK then Cancel (`IsDefault` / `IsCancel`).
- Dependants: bind `IsEnabled` (or `IsVisible` if legacy hid them), placed below their parent, indented 16 px.

## Access keys and focus
- `_` prefix on every label, checkbox and button; unique letter per dialog (per tab). Labels use `Label` with `Target="{Binding #Field}"`.
- Avoid letters clashing with window-level shortcuts.
- Tab order = visual order; use `TabIndex` only if they differ. Focus the first field on `Loaded` (select all text).

## Sizing
- Set `Width`/`Height` so nothing scrolls at default size (96 DPI, Windows and Linux); `MinWidth`/`MinHeight` = 85 % of default.
- Text areas stretch with `*` rows; no fixed heights for growing text. `ScrollViewer` is only a safety net.
- Prefer `SizeToContent="Height"` + `MaxHeight` when there is no growing text area.

## Tooltips and validation
- `ToolTip.Tip` on controls whose purpose is not obvious; copy legacy wording.
- Validation message in `TextBlock.error` under the field; OK stays enabled and focuses the offending field.
- Use theme resources only (no hard-coded colours); check Light and Dark.

## Skeleton
```xml
<Window ... ShowInTaskbar="False" WindowStartupLocation="CenterOwner">
  <Grid RowDefinitions="*,Auto" Margin="16" Grid.IsSharedSizeScope="True">
    <HeaderedContentControl Classes="group" Header="Group">
      <Grid ColumnDefinitions="Auto,12,*" RowDefinitions="Auto" RowSpacing="8">
        <Label Classes="field-label" Content="_Name:" Target="{Binding #NameBox}" />
        <TextBox Grid.Column="2" x:Name="NameBox" />
      </Grid>
    </HeaderedContentControl>
    <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8" Margin="0,12,0,0">
      <Button Classes="dialog-button" Content="OK" IsDefault="True" />
      <Button Classes="dialog-button" Content="Cancel" IsCancel="True" />
    </StackPanel>
  </Grid>
</Window>
```

## Follow-ups (from ticket 070)

- Align label columns across groups in one dialog with `Grid.IsSharedSizeScope` on the dialog root and
  `SharedSizeGroup="Label"` on each label column (the Label dialog does not do this yet).
- `DialogWindow` base class and a `DialogLayoutAssert` test helper are not built yet; tickets 071-082 may add them.
- Headless screenshot tests do not load `App.axaml`; `DialogCaptureTests` adds `Styles/DialogStyles.axaml`
  explicitly. Do the same in any new visual test.
