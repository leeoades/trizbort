using Avalonia.Controls;
using Avalonia.Input;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>Asks, when a way already seen leading to one room now leads to another, which it is.</summary>
public partial class AutomapSameWayDialog : Window
{
    private AutomapSameWayViewModel? _viewModel;

    public AutomapSameWayDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as AutomapSameWayViewModel;
        if (_viewModel != null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        BothRightButton.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && !e.Handled)
        {
            // as closing the window: both are kept
            e.Handled = true;
            Close();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
