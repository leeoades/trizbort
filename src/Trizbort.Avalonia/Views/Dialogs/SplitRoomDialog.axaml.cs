using Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class SplitRoomDialog : Window
{
    private SplitRoomViewModel? _viewModel;

    public SplitRoomDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel != null) _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel = DataContext as SplitRoomViewModel;
        if (_viewModel != null) _viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
