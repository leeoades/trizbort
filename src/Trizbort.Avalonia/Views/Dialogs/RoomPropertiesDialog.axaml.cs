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
        // Tunnel so AcceptsTab / focus traversal does not consume Tab before we see it.
        ObjectsBox.AddHandler(InputElement.KeyDownEvent, OnObjectsKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnObjectsKeyDown(object? sender, KeyEventArgs e) => HandleObjectsKey(ObjectsBox, e);

    /// <summary>
    ///   Tab / Shift+Tab indent or outdent the selected lines and Enter continues the bullet list, as in the
    ///   reference dialog. Marks the event handled when it applied an edit.
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
        ObjectListEditor.EditResult? result = null;
        if (e.Key == Key.Tab)
        {
            result = ObjectListEditor.ChangeIndent(text, start, length, (e.KeyModifiers & KeyModifiers.Shift) != 0);
        }
        else if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Shift) == 0)
        {
            result = ObjectListEditor.NewLine(text, start, length, LineEndingOf(text, box.NewLine));
        }

        if (result == null)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true; // never insert a tab character or leave the box
            }
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
