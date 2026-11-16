using System.Diagnostics;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 16 Nov 2026, playing Hollywood Hijinx a move at a time: every few moves the window froze for a second and
///   a half after Next Move. Automap runs on the window's thread, and the rethink (the solver) ran there too. Now the
///   move goes on the map straight away and the tidying is worked out in the background.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("InternalOnly")]
public class NextMoveKeepsTheWindowFreeTests
{
    private static string RepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (!File.Exists(Path.Combine(directory!.FullName, "Trizbort.sln"))) directory = directory.Parent;
        return Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
    }

    // nov-13 is the game Lee was playing (beside the map he made of it, in neat-layouts)
    [AvaloniaTest]
    [TestCase("hollywood-hijinx-nov-6", "transcript.txt")]
    [TestCase("neat-layouts", "hollywood-hijinx-nov-13.txt")]
    public async Task NextMove_NeverHoldsUpTheWindow_WhileTheMapIsTidied(string folder, string transcript)
    {
        var previous = Project.Current;
        Project.Current = new Project();
        var commands = new CommandService();
        var vm = new MainWindowViewModel(commands, null!, null!, new AvaloniaAutomapCanvas(session: commands.Session));
        var settings = AutomapSettings.Default;
        settings.FileName = RepositoryFile("tests", "automap", folder, transcript);
        settings.MoveByMove = true;
        settings.AutoTidy = true;
        var longest = 0.0;
        var longestAt = "";
        try
        {
            await vm.StartAutomapAsync(settings);
            var clock = Stopwatch.StartNew();
            while (AutomapEngine.Instance.NextCommand == null && clock.ElapsedMilliseconds < 10000)
            {
                await Task.Delay(5);
                Dispatcher.UIThread.RunJobs();
            }

            for (var move = 0; move < 200 && AutomapEngine.Instance.NextCommand != null; move++)
            {
                var command = AutomapEngine.Instance.NextCommand;
                var (played, _) = AutomapEngine.Instance.MoveCount();
                vm.PlayNextMove();
                var watch = Stopwatch.StartNew();
                var last = watch.Elapsed.TotalMilliseconds;
                while (watch.ElapsedMilliseconds < 20000)
                {
                    await Task.Delay(2);
                    Dispatcher.UIThread.RunJobs();
                    // this loop runs on the window's thread: a long gap is the window held up
                    var now = watch.Elapsed.TotalMilliseconds;
                    if (now - last > longest)
                    {
                        longest = now - last;
                        longestAt = command;
                    }

                    last = now;
                    var (nowPlayed, total) = AutomapEngine.Instance.MoveCount();
                    if (nowPlayed > played && (AutomapEngine.Instance.NextCommand != null || nowPlayed >= total)) break;
                }
            }

            TestContext.Progress.WriteLine($"The window was held up for at most {longest:0} ms (at >{longestAt}), " +
                                           $"{Project.Current.Elements.OfType<Room>().Count()} rooms.");
            Project.Current.Elements.OfType<Room>().Count().ShouldBeGreaterThan(10);
            longest.ShouldBeLessThan(400);
        }
        finally
        {
            vm.StopAutomap();
            Project.Current = previous;
        }
    }
}
