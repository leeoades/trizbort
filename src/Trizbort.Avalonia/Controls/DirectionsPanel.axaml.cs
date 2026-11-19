using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///     The directions panel (satnav), docked beside the map while it's open: from, to, Go, and the commands to type.
///     Bound to <see cref="MainWindowViewModel" />.
/// </summary>
public partial class DirectionsPanel : UserControl
{
    public DirectionsPanel()
    {
        InitializeComponent();
    }

    private async void OnCopy(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null) await clipboard.SetTextAsync(vm.DirectionsText);
    }

    private void OnAvoidStep(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && sender is MenuItem { DataContext: DirectionStepItem step })
            vm.AvoidDirectionStep(step);
    }

    private void OnAllowWay(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && sender is Button { DataContext: AvoidedWayItem way })
            vm.AllowAvoidedWay(way);
    }

    private void OnStepSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && Steps.SelectedItem is DirectionStepItem step) vm.ShowDirectionStep(step);
    }
}
