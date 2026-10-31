using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 089: the Load from Web dialog validates the address before it closes.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class OpenUrlDialogTests
{
    private static void EnsureTheme()
    {
        if (Application.Current != null && !Application.Current.Styles.OfType<FluentTheme>().Any())
        {
            Application.Current.Styles.Add(new FluentTheme());
        }
    }

    [AvaloniaTest]
    public void Accept_InvalidAddress_KeepsDialogOpenAndShowsError()
    {
        EnsureTheme();
        var dialog = new OpenUrlDialog();
        dialog.Show();

        dialog.UrlText = "ftp://example.com/map.trizbort";

        dialog.Accept().ShouldBeFalse();
        dialog.ErrorMessage.ShouldNotBeNullOrEmpty();
        dialog.IsVisible.ShouldBeTrue();
        dialog.Close();
    }

    [AvaloniaTest]
    public void Accept_EmptyAddress_ShowsError()
    {
        EnsureTheme();
        var dialog = new OpenUrlDialog();
        dialog.Show();

        dialog.UrlText = "   ";

        dialog.Accept().ShouldBeFalse();
        dialog.ErrorMessage.ShouldNotBeNullOrEmpty();
        dialog.Close();
    }

    [AvaloniaTest]
    public void Accept_HttpsAddress_ClosesWithoutError()
    {
        EnsureTheme();
        var dialog = new OpenUrlDialog();
        dialog.Show();

        dialog.UrlText = " https://example.com/map.trizbort ";

        dialog.Accept().ShouldBeTrue();
        dialog.ErrorMessage.ShouldBeEmpty();
    }
}
