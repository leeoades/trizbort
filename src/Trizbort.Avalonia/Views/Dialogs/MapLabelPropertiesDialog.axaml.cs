using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class MapLabelPropertiesDialog : Window
{
    public MapLabelPropertiesDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
        // Ctrl+B, Ctrl+I and Ctrl+K in the text, before the text box takes them
        LabelText.AddHandler(KeyDownEvent, OnTextKeyDown, RoutingStrategies.Tunnel);
        LinkSearchBox.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>OK was pressed: shown alongside the map rather than over it, it can't return a result by itself.</summary>
    public bool Accepted { get; private set; }

    private MapLabelPropertiesViewModel? ViewModel => DataContext as MapLabelPropertiesViewModel;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // As the reference dialog: the label text is focused and selected, so typing replaces it.
        LabelText.Focus();
        LabelText.SelectAll();
    }

    private (int Start, int End) Selection()
    {
        var text = LabelText.Text ?? string.Empty;
        var start = Math.Clamp(LabelText.SelectionStart, 0, text.Length);
        var end = Math.Clamp(LabelText.SelectionEnd, 0, text.Length);
        if (start == end) start = end = Math.Clamp(LabelText.CaretIndex, 0, text.Length);
        return start <= end ? (start, end) : (end, start);
    }

    /// <summary>The edit made in the text, what it selects selected; the toolbar's buttons are Markdown, so Markdown is ticked.</summary>
    private void Apply(MarkdownEdit edit)
    {
        if (ViewModel is { } viewModel) viewModel.Markdown = true;
        LabelText.Text = edit.Text;
        LabelText.CaretIndex = edit.SelectionEnd;
        LabelText.SelectionStart = edit.SelectionStart;
        LabelText.SelectionEnd = edit.SelectionEnd;
        LabelText.Focus();
    }

    private void Wrap(string mark)
    {
        var (start, end) = Selection();
        Apply(MarkdownEditing.Wrap(LabelText.Text, start, end, mark));
    }

    private void Lines(MarkdownEditing.LineKind kind)
    {
        var (start, end) = Selection();
        Apply(MarkdownEditing.Lines(LabelText.Text, start, end, kind));
    }

    private void OnBoldClick(object? sender, RoutedEventArgs e) => Wrap("**");

    private void OnItalicClick(object? sender, RoutedEventArgs e) => Wrap("*");

    private void OnCodeClick(object? sender, RoutedEventArgs e) => Wrap("`");

    private void OnHeadingClick(object? sender, RoutedEventArgs e) => Lines(MarkdownEditing.LineKind.Heading);

    private void OnBulletClick(object? sender, RoutedEventArgs e) => Lines(MarkdownEditing.LineKind.Bullet);

    private void OnNumberedClick(object? sender, RoutedEventArgs e) => Lines(MarkdownEditing.LineKind.Numbered);

    private void OnRuleClick(object? sender, RoutedEventArgs e)
    {
        var (start, end) = Selection();
        Apply(MarkdownEditing.Block(LabelText.Text, start, end, MarkdownEditing.Rule));
    }

    private void OnTableClick(object? sender, RoutedEventArgs e)
    {
        var (start, end) = Selection();
        Apply(MarkdownEditing.Block(LabelText.Text, start, end, MarkdownEditing.SampleTable, true));
    }

    /// <summary>The Add Link panel, searching for what's selected (a room's name typed already, say).</summary>
    private void OnLinkClick(object? sender, RoutedEventArgs e) => OpenLinkPanel();

    private void OpenLinkPanel()
    {
        if (ViewModel is not { } viewModel) return;
        var (start, end) = Selection();
        var selected = (LabelText.Text ?? string.Empty).Substring(start, end - start).Trim();
        viewModel.LinkSearch = selected.Length is > 0 and <= 40 && !selected.Contains('\n') ? selected : string.Empty;
        viewModel.LinkPanelOpen = true;
        LinkSearchBox.Focus();
        LinkSearchBox.SelectAll();
    }

    private void OnCloseLinkClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel) viewModel.LinkPanelOpen = false;
        LabelText.Focus();
    }

    private void OnInsertLinkClick(object? sender, RoutedEventArgs e) => InsertLink();

    private void OnLinkListDoubleTapped(object? sender, TappedEventArgs e) => InsertLink();

    /// <summary>
    ///   The link chosen, where the caret is in the text (or for what's selected there, its words kept); the panel
    ///   stays open for the next one, a table's worth of rooms, say.
    /// </summary>
    private void InsertLink()
    {
        if (ViewModel is not { } viewModel) return;
        var (start, end) = Selection();
        var selected = (LabelText.Text ?? string.Empty).Substring(start, end - start);
        if (viewModel.LinkFor(selected) is not { } link) return;
        Apply(MarkdownEditing.Insert(LabelText.Text, start, end, link));
    }

    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        var command = e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Meta;
        if (!command) return;
        switch (e.Key)
        {
            case Key.B:
                Wrap("**");
                e.Handled = true;
                break;
            case Key.I:
                Wrap("*");
                e.Handled = true;
                break;
            case Key.K:
                OpenLinkPanel();
                e.Handled = true;
                break;
        }
    }

    /// <summary>In the search: Enter puts in the link chosen; up and down choose another; Escape closes the panel.</summary>
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is not { } viewModel) return;
        var choices = viewModel.LinkChoices;
        var index = viewModel.SelectedLinkChoice is { } chosen ? choices.IndexOf(chosen) : -1;
        switch (e.Key)
        {
            case Key.Enter:
                InsertLink();
                e.Handled = true;
                break;
            case Key.Down when choices.Count > 0:
                viewModel.SelectedLinkChoice = choices[Math.Min(choices.Count - 1, index + 1)];
                LinkList.ScrollIntoView(viewModel.SelectedLinkChoice);
                e.Handled = true;
                break;
            case Key.Up when choices.Count > 0:
                viewModel.SelectedLinkChoice = choices[Math.Max(0, index - 1)];
                LinkList.ScrollIntoView(viewModel.SelectedLinkChoice);
                e.Handled = true;
                break;
            case Key.Escape:
                viewModel.LinkPanelOpen = false;
                LabelText.Focus();
                e.Handled = true;
                break;
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Accepted = true;
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
