using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
