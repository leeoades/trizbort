using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Controls;

/// <summary>
/// Button showing the current font; its flyout lets the user pick family, size, bold and italic with a live sample.
/// Changes are applied to <see cref="SelectedFont"/> only on Done; Cancel / Esc / dismissing discards them.
/// </summary>
public partial class FontPickerButton : UserControl
{
    private const string NotInstalledSuffix = " (not installed)";

    public static readonly StyledProperty<FontSpec> SelectedFontProperty =
        AvaloniaProperty.Register<FontPickerButton, FontSpec>(
            nameof(SelectedFont),
            defaultValue: FontSpec.Default,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyList<string>?> AvailableFamiliesProperty =
        AvaloniaProperty.Register<FontPickerButton, IReadOnlyList<string>?>(nameof(AvailableFamilies));

    public static readonly StyledProperty<bool> IncludeDefaultEntryProperty =
        AvaloniaProperty.Register<FontPickerButton, bool>(nameof(IncludeDefaultEntry));

    private sealed record FontItem(string Name, string Display);

    private string _draftFamily = string.Empty;
    private bool _loading;

    public FontPickerButton()
    {
        InitializeComponent();
        FamilyList.ItemTemplate = new FuncDataTemplate<FontItem>((item, _) => new TextBlock
        {
            Text = item?.Display ?? string.Empty,
            FontFamily = string.IsNullOrEmpty(item?.Name) ? FontFamily.Default : new FontFamily(item!.Name),
            FontSize = 14
        });
        UpdateButton();
        UpdateSample();
    }

    public FontSpec SelectedFont
    {
        get => GetValue(SelectedFontProperty);
        set => SetValue(SelectedFontProperty, value);
    }

    /// <summary>Family names offered in the list. Null (default) means the installed system fonts.</summary>
    public IReadOnlyList<string>? AvailableFamilies
    {
        get => GetValue(AvailableFamiliesProperty);
        set => SetValue(AvailableFamiliesProperty, value);
    }

    /// <summary>When true a "(Default)" entry (empty family) is offered; used for the app default font.</summary>
    public bool IncludeDefaultEntry
    {
        get => GetValue(IncludeDefaultEntryProperty);
        set => SetValue(IncludeDefaultEntryProperty, value);
    }

    internal string DraftFamily => _draftFamily;
    internal double DraftSize => (double)(SizeBox.Value ?? 10m);
    internal bool DraftBold => BoldToggle.IsChecked == true;
    internal bool DraftItalic => ItalicToggle.IsChecked == true;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedFontProperty)
        {
            UpdateButton();
        }
        else if (change.Property == AvailableFamiliesProperty || change.Property == IncludeDefaultEntryProperty)
        {
            LoadDraft();
        }
    }

    private IEnumerable<string> GetFamilyNames()
    {
        if (AvailableFamilies != null)
        {
            return AvailableFamilies;
        }

        try
        {
            return FontManager.Current.SystemFonts.Select(f => f.Name).Distinct().OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        catch
        {
            return new[] { "Arial", "Courier New", "Times New Roman", "Verdana" };
        }
    }

    private void LoadDraft()
    {
        var font = SelectedFont ?? FontSpec.Default;
        _loading = true;
        try
        {
            _draftFamily = font.Family ?? string.Empty;
            SizeBox.Value = (decimal)Math.Clamp(font.Size, 6, 72);
            BoldToggle.IsChecked = font.Bold;
            ItalicToggle.IsChecked = font.Italic;
            FilterBox.Text = string.Empty;
            RebuildList();
        }
        finally
        {
            _loading = false;
        }
        UpdateSample();
    }

    private void RebuildList()
    {
        var names = GetFamilyNames().ToList();
        var items = new List<FontItem>();
        if (IncludeDefaultEntry)
        {
            items.Add(new FontItem(string.Empty, "(Default)"));
        }

        var stored = _draftFamily;
        var missing = !string.IsNullOrEmpty(stored) &&
                      !names.Any(n => string.Equals(n, stored, StringComparison.OrdinalIgnoreCase));
        if (missing)
        {
            items.Add(new FontItem(stored, stored + NotInstalledSuffix));
        }

        items.AddRange(names.Select(n => new FontItem(n, n)));

        var filter = FilterBox.Text?.Trim();
        if (!string.IsNullOrEmpty(filter))
        {
            items = items.Where(i => i.Display.Contains(filter, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        var wasLoading = _loading;
        _loading = true;
        try
        {
            FamilyList.ItemsSource = items;
            FamilyList.SelectedItem = items.FirstOrDefault(i =>
                string.Equals(i.Name, _draftFamily, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _loading = wasLoading;
        }
    }

    private void UpdateButton()
    {
        var font = SelectedFont ?? FontSpec.Default;
        ButtonText.Text = font.Describe();
        ButtonText.FontFamily = string.IsNullOrEmpty(font.Family) ? FontFamily.Default : new FontFamily(font.Family);
        ButtonText.FontWeight = font.Bold ? FontWeight.Bold : FontWeight.Normal;
        ButtonText.FontStyle = font.Italic ? FontStyle.Italic : FontStyle.Normal;
        AutomationProperties.SetName(PickerButton, "Font: " + font.Describe());
    }

    private void UpdateSample()
    {
        var family = string.IsNullOrEmpty(_draftFamily) ? FontFamily.Default : new FontFamily(_draftFamily);
        var weight = DraftBold ? FontWeight.Bold : FontWeight.Normal;
        var style = DraftItalic ? FontStyle.Italic : FontStyle.Normal;
        foreach (var block in new[] { SampleText, RoomSampleText })
        {
            block.FontFamily = family;
            block.FontSize = DraftSize;
            block.FontWeight = weight;
            block.FontStyle = style;
        }
    }

    // Draft operations (also used by tests).
    internal void SelectFamily(string family)
    {
        _draftFamily = family ?? string.Empty;
        UpdateSample();
    }

    internal void Commit()
    {
        SelectedFont = new FontSpec(_draftFamily, DraftSize, DraftBold, DraftItalic);
        PickerButton.Flyout?.Hide();
    }

    internal void Cancel()
    {
        LoadDraft();
        PickerButton.Flyout?.Hide();
    }

    private void OnFlyoutOpened(object? sender, EventArgs e) => LoadDraft();

    private void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_loading)
        {
            RebuildList();
        }
    }

    private void OnFamilySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (FamilyList.SelectedItem is FontItem item)
        {
            SelectFamily(item.Name);
        }
    }

    private void OnSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (!_loading)
        {
            UpdateSample();
        }
    }

    private void OnStyleToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            UpdateSample();
        }
    }

    private void OnDoneClick(object? sender, RoutedEventArgs e) => Commit();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Cancel();

    private void OnFlyoutKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }
    }
}
