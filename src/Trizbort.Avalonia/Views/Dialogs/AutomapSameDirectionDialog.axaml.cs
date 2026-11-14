using Avalonia.Controls;
using Avalonia.Input;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class AutomapSameDirectionDialog : Window
{
    private AutomapSameDirectionViewModel? _viewModel;

    public AutomapSameDirectionDialog()
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

        _viewModel = DataContext as AutomapSameDirectionViewModel;
        if (_viewModel != null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (_viewModel != null && _viewModel.Selected == null && _viewModel.Options.Count > 0)
        {
            _viewModel.Selected = _viewModel.Options[0];
        }

        OptionList.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && !e.Handled)
        {
            // the same as Cancel, or closing the window: the view model's result maps nothing for this move
            e.Handled = true;
            Close();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
