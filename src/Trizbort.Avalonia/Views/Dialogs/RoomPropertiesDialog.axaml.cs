using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class RoomPropertiesDialog : Window
{
    public RoomPropertiesDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
        // Tunnel (and bubble, for events raised on a child) so Esc closes the open syntax hint before Cancel sees it.
        AddHandler(KeyDownEvent, OnDialogKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ObjectsBox.AddHandler(InputElement.KeyDownEvent, OnObjectsKeyDown, RoutingStrategies.Tunnel);
        Opened += (_, _) => FitToScreen();
    }

    /// <summary>
    ///   A screen shorter than the dialog (a laptop, Linux with a panel) would hide OK and Cancel: open no taller than
    ///   the screen's working area. The rest scrolls.
    /// </summary>
    private void FitToScreen()
    {
        var screen = Screens?.ScreenFromVisual(this);
        if (screen == null) return;
        var available = screen.WorkingArea.Height / screen.Scaling - 48;
        if (available > MinHeight && Height > available) Height = available;
    }

    private void OnDialogKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && SyntaxToggle.IsChecked == true)
        {
            SyntaxToggle.IsChecked = false;
            e.Handled = true;
        }
    }

    private void OnSyntaxToggleChanged(object? sender, RoutedEventArgs e)
    {
        SyntaxPopup.IsOpen = SyntaxToggle.IsChecked == true;
    }

    private void OnObjectsKeyDown(object? sender, KeyEventArgs e) => HandleObjectsKey(ObjectsBox, e);

    /// <summary>
    ///   Enter continues the bullet list, as in the reference dialog. Tab is not intercepted (it moves focus; users
    ///   type the dashes). Marks the event handled when it applied an edit.
    /// </summary>
    internal static void HandleObjectsKey(TextBox box, KeyEventArgs e)
    {
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt)) != 0)
        {
            return;
        }

        var start = Math.Min(box.SelectionStart, box.SelectionEnd);
        var length = Math.Abs(box.SelectionEnd - box.SelectionStart);
        var text = box.Text ?? string.Empty;
        if (e.Key != Key.Enter || (e.KeyModifiers & KeyModifiers.Shift) != 0)
        {
            return;
        }

        var result = ObjectListEditor.NewLine(text, start, length, LineEndingOf(text, box.NewLine));
        if (result == null)
        {
            return;
        }

        box.Text = result.Text;
        box.CaretIndex = result.SelectionStart + result.SelectionLength;
        box.SelectionStart = result.SelectionStart;
        box.SelectionEnd = result.SelectionStart + result.SelectionLength;
        e.Handled = true;
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

    /// <summary>Keep the text's existing line ending so Enter never mixes \n and \r\n.</summary>
    private static string LineEndingOf(string text, string fallback)
    {
        if (text.Contains("\r\n")) return "\r\n";
        if (text.Contains('\n')) return "\n";
        return fallback;
    }
}
