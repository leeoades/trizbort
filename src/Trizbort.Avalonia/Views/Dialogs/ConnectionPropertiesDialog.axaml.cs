using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class ConnectionPropertiesDialog : Window
{
    public ConnectionPropertiesDialog()
    {
        InitializeComponent();
    }

    private void OnPresetUpDownClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionPropertiesViewModel vm)
            vm.SetLabelPreset(ConnectionLabel.Up);
    }

    private void OnPresetDownUpClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionPropertiesViewModel vm)
            vm.SetLabelPreset(ConnectionLabel.Down);
    }

    private void OnPresetInOutClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionPropertiesViewModel vm)
            vm.SetLabelPreset(ConnectionLabel.In);
    }

    private void OnPresetOutInClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionPropertiesViewModel vm)
            vm.SetLabelPreset(ConnectionLabel.Out);
    }

    private void OnPresetClearClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionPropertiesViewModel vm)
        {
            vm.StartText = string.Empty;
            vm.MidText = string.Empty;
            vm.EndText = string.Empty;
        }
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
