using Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia.Views;

public partial class MainWindow : Window
{
    private bool _isClosingConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isClosingConfirmed) return;

        if (DataContext is MainWindowViewModel vm && Project.Current.IsDirty)
        {
            e.Cancel = true;
            var canClose = await vm.CanLoseProjectAsync();
            if (canClose)
            {
                _isClosingConfirmed = true;
                Close();
            }
        }
    }
}
