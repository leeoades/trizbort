using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class MapLabelPropertiesDialog : Window
{
    public MapLabelPropertiesDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // As the reference dialog: the label text is focused and selected, so typing replaces it.
        LabelText.Focus();
        LabelText.SelectAll();
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
