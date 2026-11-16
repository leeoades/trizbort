using System.Diagnostics;
using Avalonia.Controls;
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

/// <summary>Play to the End: a progress bar while automap plays the moves left, and the map locked until it's done.</summary>
[TestFixture]
[NonParallelizable]
public class PlayToEndProgressTests
{
    private static async Task WaitFor(Func<bool> condition, string what)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.ElapsedMilliseconds > 10000) Assert.Fail("Timed out waiting for " + what);
            await Task.Delay(10);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaTest]
    public async Task PlayingToTheEnd_ShowsProgress_ThenUnlocksTheMap_WhenItsDone()
    {
        var previous = Project.Current;
        Project.Current = new Project();
        var path = Path.GetTempFileName();
        var lines = new List<string> { ">look", "Room 0", "A plain room.", "" };
        for (var room = 1; room <= 12; room++) lines.AddRange(new[] { ">east", $"Room {room}", "A plain room.", "" });
        lines.Add(">");
        File.WriteAllText(path, string.Join("\n", lines));
        var vm = new MainWindowViewModel(new CommandService(), null!, null!, new AvaloniaAutomapCanvas());
        var settings = AutomapSettings.Default;
        settings.FileName = path;
        settings.MoveByMove = true;
        settings.AutoTidy = false;
        var window = new Window { DataContext = vm, Width = 400, Height = 900, Content = new AutomapPanel() };
        try
        {
            window.Show();
            await vm.StartAutomapAsync(settings);
            await WaitFor(() => AutomapEngine.Instance.NextCommand == "look", "the first move");

            await vm.PlayToEndCommand.ExecuteAsync(null);
            await WaitFor(() => AutomapEngine.Instance.NextCommand == null &&
                                AutomapEngine.Instance.MoveCount().Played == 13, "the end");
            vm.RefreshAutomapPanel();
            Dispatcher.UIThread.RunJobs();

            // done: the progress bar has gone, and the map can be changed again
            vm.IsPlayingToEnd.ShouldBeFalse();
            MapCanvasControl.ViewOnly.ShouldBeFalse();
            var panel = window.GetVisualDescendants().OfType<AutomapPanel>().ShouldHaveSingleItem();
            panel.FindControl<ProgressBar>("PlayToEndProgressBar").ShouldNotBeNull();
        }
        finally
        {
            vm.StopAutomap();
            MapCanvasControl.ViewOnly = false;
            window.Close();
            try { File.Delete(path); } catch (IOException) { }
            Project.Current = previous;
        }
    }
}
