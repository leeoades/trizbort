using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>What several selected rooms share, edited together. Lee, 24 Nov 2026.</summary>
public partial class RoomsPropertiesDialog : Window
{
    public RoomsPropertiesDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
