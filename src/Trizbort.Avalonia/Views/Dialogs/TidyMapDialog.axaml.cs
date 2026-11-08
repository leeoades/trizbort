using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Automap;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>Tidy Map...: which rooms to tidy, and which way. Closes with the choice, or null when cancelled.</summary>
public partial class TidyMapDialog : Window
{
    public TidyMapDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    /// <summary>Shows <paramref name="suggested" /> and how many rooms are selected (two or more can be tidied alone).</summary>
    public void Configure(TidyChoice suggested, int selectedRooms)
    {
        var canSelect = selectedRooms > 1;
        SelectedRadio.IsEnabled = canSelect;
        SelectedRadio.Content = canSelect ? $"The _selected rooms ({selectedRooms})" : "The _selected rooms";
        NoSelectionHint.IsVisible = !canSelect;
        Choice = suggested with { SelectedOnly = suggested.SelectedOnly && canSelect };
    }

    /// <summary>The choice shown.</summary>
    public TidyChoice Choice
    {
        get => new(SelectedRadio.IsChecked == true && SelectedRadio.IsEnabled,
            ThoroughRadio.IsChecked == true ? TidyMethod.Thorough : TidyMethod.Quick);
        set
        {
            SelectedRadio.IsChecked = value.SelectedOnly;
            WholeMapRadio.IsChecked = !value.SelectedOnly;
            ThoroughRadio.IsChecked = value.Method == TidyMethod.Thorough;
            QuickRadio.IsChecked = value.Method != TidyMethod.Thorough;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        TidyButton.Focus();
    }

    private void OnTidyClick(object? sender, RoutedEventArgs e)
    {
        Close(Choice);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
