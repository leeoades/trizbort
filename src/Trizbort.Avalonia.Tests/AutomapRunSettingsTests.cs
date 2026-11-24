using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
            // Lee, 23 Nov 2026: the game screen is the panel's to show; direction words are the map's; and rooms with the
            // same name are always told apart by their descriptions too
            dialog.FindControl<CheckBox>("GameScreenCheck").ShouldBeNull();
            dialog.FindControl<CheckBox>("BestGuessCheck").ShouldBeNull();
            dialog.FindControl<CheckBox>("NamesOnlyCheck").ShouldBeNull();

            // Lee, 24 Nov 2026: assume joins go both ways, there while automap runs too; and an (i) beside each option
            var twoWay = dialog.FindControl<CheckBox>("TwoWayCheck")!;
            twoWay.IsEffectivelyVisible.ShouldBeTrue();
            twoWay.Content.ShouldBe("Assume _joins go both ways.");
            var tips = dialog.GetVisualDescendants().OfType<Trizbort.Avalonia.Controls.InfoTip>()
                .Where(tip => tip.IsEffectivelyVisible).ToList();
            // guess exits, tidy, both ways, and the two commands (the transcript's two options are for starting)
            tips.Count.ShouldBe(5);
            tips.ShouldAllBe(tip => !string.IsNullOrWhiteSpace(tip.Text));
            tips.ShouldContain(tip => tip.Text!.Contains("way back"));
        }
        finally
        {
            dialog.Close();
        }
    }

    // Lee, 24 Nov 2026: the (i) beside a long option was off the edge of the dialog; and the i wasn't in the middle
    // of its circle
    [AvaloniaTest]
    public void EveryInfoMark_FitsInTheDialog_EvenAtItsNarrowest()
    {
        var vm = new AutomapSettingsViewModel(null!);
        vm.Initialize(AutomapSettings.Default);
        var dialog = new AutomapSettingsDialog { DataContext = vm };
        dialog.Show();
        try
        {
            foreach (var width in new[] { dialog.Width, dialog.MinWidth })
            {
                dialog.Width = width;
                Dispatcher.UIThread.RunJobs();
                var tips = dialog.GetVisualDescendants().OfType<Trizbort.Avalonia.Controls.InfoTip>()
                    .Where(tip => tip.IsEffectivelyVisible).ToList();
                tips.Count.ShouldBe(7);
                foreach (var tip in tips)
                {
                    var corner = global::Avalonia.VisualExtensions.TranslatePoint(tip,
                        new global::Avalonia.Point(tip.Bounds.Width, tip.Bounds.Height), dialog)!.Value;
                    corner.X.ShouldBeLessThanOrEqualTo(dialog.ClientSize.Width - 16, $"the (i) for \"{tip.Text}\" at {width}");
                    tip.Bounds.Width.ShouldBe(16);
                }
            }

            // the i drawn in the middle of the 16x16 circle, not a font's letter, which sits where the font puts it
            var glyph = Trizbort.Avalonia.Controls.InfoTip.Glyph.Bounds;
            (glyph.X + glyph.Width / 2).ShouldBe(8, 0.01);
            (glyph.Y + glyph.Height / 2).ShouldBe(8, 0.01);
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

    [Test]
    public void Settings_AlwaysUseDescriptions_AndANewRunAsksWhichRoomAgain()
    {
        var saved = AutomapSettings.Default;
        saved.VerboseTranscript = false;
        saved.BestGuessWhichRoom = true;
        saved.AbbreviateDirections = false;

        var starting = new AutomapSettingsViewModel(null!);
        starting.Initialize(saved);
        var started = starting.GetSettings();
        started.VerboseTranscript.ShouldBeTrue();
        started.BestGuessWhichRoom.ShouldBeFalse();
        // as it was: the map's Direction words setting draws them
        started.AbbreviateDirections.ShouldBeFalse();

        // part way through, Don't Ask Again stays
        var running = new AutomapSettingsViewModel(null!) { WhileRunning = true };
        running.Initialize(saved);
        running.GetSettings().BestGuessWhichRoom.ShouldBeTrue();
    }
}
