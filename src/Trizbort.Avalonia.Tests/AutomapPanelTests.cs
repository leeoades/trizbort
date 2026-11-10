using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.Tests;

/// <summary>The automap panel docked beside the map: where you are, the transcript, and mapping.</summary>
[TestFixture]
[NonParallelizable]
public class AutomapPanelTests
{
    private static async Task WaitFor(Func<bool> condition, string what)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.ElapsedMilliseconds > 5000) Assert.Fail("Timed out waiting for " + what);
            await Task.Delay(10);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public async Task ThePanel_ShowsWhereYouAre_TheTranscript_AndMapping()
    {
        var previous = Project.Current;
        Project.Current = new Project();
        var path = Path.GetTempFileName();
        File.WriteAllText(path, string.Join("\n", ">look", "Hall", "A grand hall. A door leads north.", "",
            ">north", "Kitchen", "A small kitchen.", "", ">"));
        var vm = new MainWindowViewModel(new CommandService(), null!, null!, new AvaloniaAutomapCanvas());
        var settings = AutomapSettings.Default;
        settings.FileName = path;
        settings.MoveByMove = true;
        // the panel on its own (a whole main window would stay subscribed to the map after the test)
        var window = new Window { DataContext = vm, Width = 400, Height = 900, Content = new AutomapPanel() };
        try
        {
            window.Show();
            await vm.StartAutomapAsync(settings);
            await WaitFor(() => AutomapEngine.Instance.NextCommand == "look", "the first move");
            vm.PlayNextMoveCommand.Execute(null);
            await WaitFor(() => AutomapEngine.Instance.NextCommand == "north", "the second move");
            vm.RefreshAutomapPanel();
            Dispatcher.UIThread.RunJobs();

            var panel = window.GetVisualDescendants().OfType<AutomapPanel>().ShouldHaveSingleItem();
            panel.IsEffectivelyVisible.ShouldBeTrue();
            panel.FindControl<TextBlock>("RoomName")!.Text.ShouldBe("Hall");
            panel.FindControl<TextBlock>("TranscriptNote")!.Text.ShouldBe("Paused before \"north\".");
            panel.FindControl<TextBlock>("MappingNote")!.Text.ShouldStartWith("Mapping");
            // stepping, not "go to start / end": the buttons say what they do
            panel.FindControl<Button>("PreviousMoveButton")!.Content.ShouldBe("◀ Previous Move");
            panel.FindControl<Button>("NextMoveButton")!.Content.ShouldBe("Next Move ▶");
            var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
            if (!string.IsNullOrEmpty(shots))
            {
                using var frame = window.CaptureRenderedFrame();
                frame?.Save(Path.Combine(shots, "automap-panel.png"));
            }

            // tracking only, from the panel, reaches the engine
            panel.FindControl<RadioButton>("TrackingRadio")!.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            AutomapEngine.Instance.Suspended.ShouldBeTrue();
            panel.FindControl<TextBlock>("MappingNote")!.Text.ShouldStartWith("Tracking only");

            panel.FindControl<RadioButton>("MappingRadio")!.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            AutomapEngine.Instance.Suspended.ShouldBeFalse();
        }
        finally
        {
            vm.StopAutomap();
            window.Close();
            try { File.Delete(path); } catch (IOException) { }
            Project.Current = previous;
        }
    }
}
