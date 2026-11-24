using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>A background's colour, border, shape and lines. Lee, 24 Nov 2026.</summary>
public partial class BackgroundPropertiesDialog : Window
{
    public BackgroundPropertiesDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
