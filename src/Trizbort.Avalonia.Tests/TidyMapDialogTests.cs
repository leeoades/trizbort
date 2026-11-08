using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>Tidy Map...: the dialog that asks which rooms and which way, and the progress window while it works.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class TidyMapDialogTests
{
    [AvaloniaTest]
    public void SelectedRooms_CanBeChosen_OnlyWithTwoOrMoreSelected()
    {
        var dialog = new TidyMapDialog();

        dialog.Configure(new TidyChoice(true, TidyMethod.Thorough), 1);
        dialog.Choice.ShouldBe(new TidyChoice(false, TidyMethod.Thorough));
        dialog.FindControl<RadioButton>("SelectedRadio")!.IsEnabled.ShouldBeFalse();

        dialog.Configure(new TidyChoice(true, TidyMethod.Quick), 3);
        dialog.Choice.ShouldBe(new TidyChoice(true, TidyMethod.Quick));
        dialog.FindControl<RadioButton>("SelectedRadio")!.Content!.ToString()!.ShouldContain("(3)");
    }

    [AvaloniaTest]
    public async Task QuickWork_ReturnsItsResult_WithoutAProgressWindow()
    {
        var owner = new Window();
        owner.Show();
        var prompts = new AvaloniaEditorPrompts(() => owner);

        var result = await prompts.RunWithProgressAsync("Tidy map", "Tidying...", 1000, _ => 42);

        result.ShouldBe(42);
        owner.OwnedWindows.ShouldBeEmpty();
    }

    [AvaloniaTest]
    public async Task SlowWork_RunsOffTheUiThread_AndCancelStopsIt()
    {
        var owner = new Window();
        owner.Show();
        var prompts = new AvaloniaEditorPrompts(() => owner);
        var ranOnUiThread = true;

        var running = prompts.RunWithProgressAsync("Tidy map", "Tidying...", 5000, token =>
        {
            ranOnUiThread = Dispatcher.UIThread.CheckAccess();
            while (!token.IsCancellationRequested) Thread.Sleep(10);
            return 42;
        });

        // the UI keeps running while the work does; the progress window appears after a moment
        var waited = 0;
        while (owner.OwnedWindows.Count == 0 && waited < 3000)
        {
            await Task.Delay(20);
            Dispatcher.UIThread.RunJobs();
            waited += 20;
        }

        var progress = owner.OwnedWindows.OfType<ProgressDialog>().ShouldHaveSingleItem();
        ranOnUiThread.ShouldBeFalse();
        progress.Close();

        (await running).ShouldBe(0);
    }
}
