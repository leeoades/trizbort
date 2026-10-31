using Avalonia.Controls;
using Avalonia.Interactivity;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>
///     Load from Web (ticket 089). Closes with the entered address on OK or Enter, and with null on Cancel or Esc. An
///     address that is not an http or https URL keeps the dialog open and shows an error under the field.
/// </summary>
public partial class OpenUrlDialog : Window
{
    public OpenUrlDialog()
    {
        InitializeComponent();
    }

    /// <summary>The text in the URL box.</summary>
    public string UrlText
    {
        get => UrlBox.Text ?? string.Empty;
        set => UrlBox.Text = value;
    }

    /// <summary>The validation message currently shown, or an empty string.</summary>
    public string ErrorMessage => ErrorText.Text ?? string.Empty;

    /// <summary>Validates the text and closes with it when valid, as the OK button does. Returns true when it closed.</summary>
    public bool Accept()
    {
        if (!MapUrl.TryParse(UrlText, out var uri, out var error))
        {
            ErrorText.Text = error;
            UrlBox.Focus();
            UrlBox.SelectAll();
            return false;
        }

        ErrorText.Text = string.Empty;
        Close(uri.AbsoluteUri);
        return true;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UrlBox.Focus();
        UrlBox.SelectAll();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Accept();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
