using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class MapSettingsDialog : Window
{
    public MapSettingsDialog()
    {
        InitializeComponent();
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
