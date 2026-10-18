using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class AppSettingsDialog : Window
{
    public AppSettingsDialog()
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
