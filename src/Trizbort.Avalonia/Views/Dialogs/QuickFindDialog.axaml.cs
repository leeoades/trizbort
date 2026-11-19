using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>
///     Quick Find (ticket 086). Closes with the entered text on Find or Enter, and with null on Cancel, Esc or when it
///     loses focus.
/// </summary>
public partial class QuickFindDialog : Window
{
    private bool _wasActive;
    private bool _closed;

    public QuickFindDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
        Activated += OnWindowActivated;
        Deactivated += OnWindowDeactivated;
    }

    /// <summary>The text offered for completion.</summary>
    public void SetSuggestions(IEnumerable<string> suggestions)
    {
        FindBox.ItemsSource = suggestions.ToList();
    }

    /// <summary>Accepts the current text, as the Find button does.</summary>
    public void Accept()
    {
        Finish(FindBox.Text);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        FocusFindBox();
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        _wasActive = true;
        FocusFindBox();
    }

    /// <summary>
    ///     The caret in the text box, ready to type: focusing the find box itself (an AutoCompleteBox) doesn't put it in
    ///     the text box inside it. Once the box is laid out, so its text box is there.
    /// </summary>
    private void FocusFindBox()
    {
        FocusTextBox();
        Dispatcher.UIThread.Post(FocusTextBox, DispatcherPriority.Loaded);
    }

    private void FocusTextBox()
    {
        var text = FindBox.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        if (text == null)
        {
            FindBox.Focus();
            return;
        }

        text.Focus();
        text.SelectAll();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        // Like the reference dialog, losing focus closes it.
        if (_wasActive)
        {
            Finish(null);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.Enter)
        {
            e.Handled = true;
            Accept();
        }
    }

    private void OnFindClick(object? sender, RoutedEventArgs e)
    {
        Accept();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Finish(null);
    }

    private void Finish(string? text)
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        Close(text);
    }
}
