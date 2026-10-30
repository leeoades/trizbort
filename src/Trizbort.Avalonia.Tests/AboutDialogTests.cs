using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Views;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class AboutDialogTests
{
    [AvaloniaTest]
    public void VersionText_IsNonEmptyAndPrefixed()
    {
        var dialog = new AboutDialog();

        var versionText = dialog.FindControl<TextBlock>("VersionText");

        versionText.ShouldNotBeNull();
        (versionText.Text ?? string.Empty).ShouldStartWith("Version ");
        (versionText.Text ?? string.Empty).Length.ShouldBeGreaterThan("Version ".Length);
    }

    [AvaloniaTest]
    public void RepositoryLinks_ArePresentWithGitHubUrls()
    {
        var dialog = new AboutDialog();
        dialog.Show();
        var links = dialog.GetVisualDescendants().OfType<HyperlinkButton>().ToList();
        dialog.Close();

        links.Count.ShouldBe(3);
        links.Select(link => link.Tag?.ToString() ?? string.Empty).ShouldBe(new[]
        {
            "https://github.com/genstein/trizbort",
            "https://github.com/JasonLautzenheiser/trizbort",
            "https://github.com/LeeOades/trizbort"
        });
    }

    [AvaloniaTest]
    public void Licence_ContainsLicenceNotice()
    {
        var dialog = new AboutDialog();

        var licenceBox = dialog.FindControl<TextBox>("LicenceBox");

        licenceBox.ShouldNotBeNull();
        licenceBox.IsReadOnly.ShouldBeTrue();
        (licenceBox.Text ?? string.Empty).ShouldContain("THE SOFTWARE IS PROVIDED \"AS IS\"");
    }

    [AvaloniaTest]
    public void OkButton_IsDefaultAndCancel()
    {
        var dialog = new AboutDialog();

        var okButton = dialog.FindControl<Button>("OkButton");

        okButton.ShouldNotBeNull();
        okButton.IsDefault.ShouldBeTrue();
        okButton.IsCancel.ShouldBeTrue();
    }

    [AvaloniaTest]
    public void Layout_LicenceBoxAndOkButtonAreNotClipped()
    {
        EnsureDialogStyles();
        var dialog = new AboutDialog();
        dialog.Show();
        try
        {
            Settle(dialog);

            var licenceBox = dialog.FindControl<TextBox>("LicenceBox").ShouldNotBeNull();
            var okButton = dialog.FindControl<Button>("OkButton").ShouldNotBeNull();

            licenceBox.Bounds.Height.ShouldBeGreaterThanOrEqualTo(120);
            AssertInsideClient(dialog, licenceBox);
            AssertInsideClient(dialog, okButton);
        }
        finally
        {
            dialog.Close();
        }
    }

    private static void AssertInsideClient(AboutDialog dialog, Control control)
    {
        var origin = control.TranslatePoint(new Point(0, 0), dialog).ShouldNotBeNull();

        origin.X.ShouldBeGreaterThanOrEqualTo(0);
        origin.Y.ShouldBeGreaterThanOrEqualTo(0);
        (origin.X + control.Bounds.Width).ShouldBeLessThanOrEqualTo(dialog.ClientSize.Width);
        (origin.Y + control.Bounds.Height).ShouldBeLessThanOrEqualTo(dialog.ClientSize.Height);
    }

    private static void EnsureDialogStyles()
    {
        // The headless test app doesn't load App.axaml, so add the theme and dialog styles explicitly.
        if (Application.Current == null)
        {
            return;
        }

        if (!Application.Current.Styles.OfType<FluentTheme>().Any())
        {
            Application.Current.Styles.Add(new FluentTheme());
        }

        if (!Application.Current.Styles.OfType<global::Avalonia.Markup.Xaml.Styling.StyleInclude>().Any())
        {
            Application.Current.Styles.Add(new global::Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://Trizbort.Avalonia/"))
            {
                Source = new Uri("avares://Trizbort.Avalonia/Styles/DialogStyles.axaml")
            });
        }
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
