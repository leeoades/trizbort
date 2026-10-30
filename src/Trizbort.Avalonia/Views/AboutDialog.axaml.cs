using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Views;

public partial class AboutDialog : Window
{
    // Licence text carried over from the WinForms About dialog (legacy/src/Trizbort/UI/AboutDialog.resx).
    private const string LicenceNotice =
        "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n\n" +
        "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n\n" +
        "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";

    public AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = GetVersionText();
        LicenceBox.Text = LicenceNotice;
    }

    private static string GetVersionText()
    {
        var assembly = typeof(AboutDialog).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            version = assembly.GetName().Version?.ToString() ?? "Unknown";
        }

        // Drop build metadata such as "+commit-hash" for display.
        var metadataStart = version.IndexOf('+');
        if (metadataStart > 0)
        {
            version = version[..metadataStart];
        }

        return $"Version {version}";
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        OkButton.Focus();
    }

    private async void OnLinkClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not HyperlinkButton { Tag: string url })
        {
            return;
        }

        var opened = false;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            try
            {
                opened = await topLevel.Launcher.LaunchUriAsync(uri);
            }
            catch (Exception)
            {
                opened = false;
            }
        }

        StatusText.Text = opened ? string.Empty : $"Unable to open the link: {url}";
        StatusText.IsVisible = !opened;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
