using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class AutomapSettingsDialog : Window
{
    public AutomapSettingsDialog()
    {
        InitializeComponent();
    }

    private void OnStartClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AutomapSettingsViewModel vm && string.IsNullOrWhiteSpace(vm.FileName))
        {
            return;
        }
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
