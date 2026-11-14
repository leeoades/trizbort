using Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class AreaBranchesDialog : Window
{
    private AreaBranchesViewModel? _viewModel;

    public AreaBranchesDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel != null) _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel = DataContext as AreaBranchesViewModel;
        if (_viewModel != null) _viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
