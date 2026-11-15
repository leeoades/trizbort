using Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class WhatMovedItDialog : Window
{
    private WhatMovedItViewModel? _viewModel;

    public WhatMovedItDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel != null) _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel = DataContext as WhatMovedItViewModel;
        if (_viewModel != null) _viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
