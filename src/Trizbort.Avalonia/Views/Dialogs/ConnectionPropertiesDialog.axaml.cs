using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class ConnectionPropertiesDialog : Window
{
    public ConnectionPropertiesDialog()
    {
        InitializeComponent();
    }

    private void OnClearTextClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionPropertiesViewModel vm)
            vm.ClearText();
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
