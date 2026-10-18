using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class RoomPropertiesDialog : Window
{
    public RoomPropertiesDialog()
    {
        InitializeComponent();
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
