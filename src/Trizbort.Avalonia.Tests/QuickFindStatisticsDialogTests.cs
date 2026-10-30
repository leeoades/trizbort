using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using CoreKeyGesture = Trizbort.Editor.Commands.KeyGesture;

namespace Trizbort.Avalonia.Tests;

/// <summary>Tickets 044, 086, 087 and 088: Quick Find dialog, statistics window and the new commands.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class QuickFindStatisticsDialogTests
{
    private static Task<string?> ShowQuickFind(QuickFindDialog dialog, Window owner)
    {
        owner.Show();
        return dialog.ShowDialog<string?>(owner);
    }

    [AvaloniaTest]
    public async Task QuickFind_FindButton_ReturnsTheText()
    {
        var dialog = new QuickFindDialog();
        dialog.SetSuggestions(new[] { "Cellar", "Hall" });
        var task = ShowQuickFind(dialog, new Window());
        dialog.FindControl<AutoCompleteBox>("FindBox")!.Text = "hall";

        dialog.FindControl<Button>("FindButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        (await task).ShouldBe("hall");
    }

    [AvaloniaTest]
    public async Task QuickFind_EnterKey_ReturnsTheText()
    {
        var dialog = new QuickFindDialog();
        var task = ShowQuickFind(dialog, new Window());
        dialog.FindControl<AutoCompleteBox>("FindBox")!.Text = "lamp";

        dialog.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        (await task).ShouldBe("lamp");
    }

    [AvaloniaTest]
    public async Task QuickFind_CancelButton_ReturnsNull()
    {
        var dialog = new QuickFindDialog();
        var task = ShowQuickFind(dialog, new Window());
        dialog.FindControl<AutoCompleteBox>("FindBox")!.Text = "lamp";

        dialog.FindControl<Button>("CancelButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        (await task).ShouldBeNull();
    }

    [AvaloniaTest]
    public async Task QuickFind_EscapeKey_ReturnsNull()
    {
        var dialog = new QuickFindDialog();
        var task = ShowQuickFind(dialog, new Window());

        dialog.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        (await task).ShouldBeNull();
    }

    [AvaloniaTest]
    public void StatisticsWindow_ShowsTheReportText()
    {
        var window = new MapStatisticsWindow { ReportText = "# of Rooms: 3" };
        window.Show();

        window.FindControl<TextBox>("ReportBox")!.Text.ShouldBe("# of Rooms: 3");
        window.FindControl<TextBox>("ReportBox")!.IsReadOnly.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaTest]
    public void StatisticsWindow_CopyButton_PutsTheTextOnTheClipboard()
    {
        string? copied = null;
        var window = new MapStatisticsWindow
        {
            ReportText = "report",
            CopyAction = text =>
            {
                copied = text;
                return Task.CompletedTask;
            }
        };
        window.Show();

        window.FindControl<Button>("CopyButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        copied.ShouldBe("report");
        window.Close();
    }

    [Test]
    public void Commands_AreRegistered()
    {
        var catalogue = AppCommands.CreateCatalogue(null);

        catalogue.Find("edit.quickFind")!.Gestures.ShouldBe(new[] { new CoreKeyGesture(InputKey.F, ModifierKeys.Primary) });
        catalogue.Find("tools.mapStatistics").ShouldNotBeNull();
        catalogue.Find("tools.mapStatisticsExport").ShouldNotBeNull();
        catalogue.Find("help.online")!.Gestures.ShouldBe(new[] { new CoreKeyGesture(InputKey.F1, ModifierKeys.None) });
        catalogue.Find("help.originalHelp")!.MenuPath.ShouldBe("Help");
        CatalogueValidator.Validate(catalogue).ShouldBeEmpty();
    }
}
