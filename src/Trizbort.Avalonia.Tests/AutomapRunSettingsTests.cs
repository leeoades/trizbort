using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>Automap Settings from the automap panel's cog, while automap runs.</summary>
[TestFixture]
public class AutomapRunSettingsTests
{
    [AvaloniaTest]
    public void WhileAutomapRuns_OnlyTheOptionsThatCanChange_Show_WithOK()
    {
        var vm = new AutomapSettingsViewModel(null!);
        vm.Initialize(AutomapSettings.Default);
        vm.WhileRunning = true;
        var dialog = new AutomapSettingsDialog { DataContext = vm };
        dialog.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
            if (!string.IsNullOrEmpty(shots))
            {
                using var frame = dialog.CaptureRenderedFrame();
                frame?.Save(System.IO.Path.Combine(shots, "automap-run-settings.png"));
            }

            dialog.Title.ShouldBe("Automap Settings");
            // the transcript can't change part way through
            dialog.FindControl<TextBox>("FileNameBox")!.IsEffectivelyVisible.ShouldBeFalse();
            var ok = dialog.FindControl<Button>("OkButton")!;
            ok.Content.ShouldBe("OK");
            ok.IsEnabled.ShouldBeTrue();
            dialog.FindControl<CheckBox>("AutoTidyCheck")!.IsEffectivelyVisible.ShouldBeTrue();
            dialog.FindControl<CheckBox>("BestGuessCheck")!.IsEffectivelyVisible.ShouldBeTrue();
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void StartingAutomap_ShowsTheTranscript_AndStart()
    {
        var vm = new AutomapSettingsViewModel(null!);
        vm.Initialize(AutomapSettings.Default);
        var dialog = new AutomapSettingsDialog { DataContext = vm };
        dialog.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            dialog.Title.ShouldBe("Automapping");
            dialog.FindControl<TextBox>("FileNameBox")!.IsEffectivelyVisible.ShouldBeTrue();
            dialog.FindControl<Button>("OkButton")!.Content.ShouldBe("Start Auto_mapping");
        }
        finally
        {
            dialog.Close();
        }
    }
}
