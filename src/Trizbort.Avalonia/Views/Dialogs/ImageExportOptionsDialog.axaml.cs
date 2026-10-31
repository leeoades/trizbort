using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>Asks for the scale and content of an image export (E018, E022).</summary>
public partial class ImageExportOptionsDialog : Window
{
    public const int ModeActualSize = 0;
    public const int ModeCurrentZoom = 1;
    public const int ModeCustom = 2;

    public ImageExportOptionsDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    /// <summary>The chosen scale mode (<see cref="ModeActualSize" />, <see cref="ModeCurrentZoom" />, <see cref="ModeCustom" />).</summary>
    public int Mode
    {
        get => CustomRadio.IsChecked == true ? ModeCustom : CurrentZoomRadio.IsChecked == true ? ModeCurrentZoom : ModeActualSize;
        set
        {
            ActualSizeRadio.IsChecked = value != ModeCurrentZoom && value != ModeCustom;
            CurrentZoomRadio.IsChecked = value == ModeCurrentZoom;
            CustomRadio.IsChecked = value == ModeCustom;
        }
    }

    /// <summary>The custom scale in percent.</summary>
    public int CustomScalePercent
    {
        get => (int)Math.Clamp(CustomPercent.Value ?? 100m, 1m, 800m);
        set => CustomPercent.Value = Math.Clamp(value, 1, 800);
    }

    /// <summary>True when "Selection only" is ticked (never true without a selection).</summary>
    public bool SelectionOnly
    {
        get => SelectionOnlyCheck.IsChecked == true && SelectionOnlyCheck.IsEnabled;
        set => SelectionOnlyCheck.IsChecked = value;
    }

    /// <summary>Sets the current zoom shown on its radio button and whether there is a selection to export.</summary>
    public void Configure(double zoom, bool hasSelection)
    {
        CurrentZoomRadio.Content = $"Whole map at current _zoom ({zoom:P0})";
        SelectionOnlyCheck.IsEnabled = hasSelection;
        NoSelectionHint.IsVisible = !hasSelection;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        (Mode == ModeCustom ? CustomRadio : Mode == ModeCurrentZoom ? CurrentZoomRadio : ActualSizeRadio).Focus();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
