using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class AutomapSameDirectionDialog : Window
{
    private AutomapSameDirectionViewModel? _viewModel;

    public AutomapSameDirectionDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as AutomapSameDirectionViewModel;
        if (_viewModel != null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        KeepExistingButton.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && !e.Handled)
        {
            e.Handled = true;
            _viewModel?.KeepExisting();
            if (_viewModel == null)
            {
                Close();
            }
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnKeepExistingClick(object? sender, RoutedEventArgs e) => _viewModel?.KeepExisting();

    private void OnKeepNewClick(object? sender, RoutedEventArgs e) => _viewModel?.KeepNew();

    private void OnKeepBothClick(object? sender, RoutedEventArgs e) => _viewModel?.KeepBoth();
}
