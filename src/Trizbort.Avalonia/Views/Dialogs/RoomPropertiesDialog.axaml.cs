using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class RoomPropertiesDialog : Window
{
    public RoomPropertiesDialog()
    {
        InitializeComponent();
    }

    /// <summary>Used for the "already the start room" question; defaults to answering no.</summary>
    public IEditorPrompts Prompts { get; set; } = NonInteractiveEditorPrompts.Instance;

    private async void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RoomPropertiesViewModel vm)
        {
            Close(true);
            return;
        }

        if (await vm.TryAcceptAsync(Prompts))
        {
            vm.SaveLastTab();
            Close(true);
        }
        else if (vm.NameError != null)
        {
            NameBox.Focus();
            NameBox.SelectAll();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RoomPropertiesViewModel vm)
        {
            vm.SaveLastTab();
        }

        Close(false);
    }
}
