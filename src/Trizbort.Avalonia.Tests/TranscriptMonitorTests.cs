using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Tests;

/// <summary>The little game monitor beside the automap bar.</summary>
[TestFixture]
public class TranscriptMonitorTests
{
    [Test]
    public void MonitorText_KeepsTheLastLinesThatFit_CountingWrappedLines()
    {
        var lines = new[] { "one", "two", new string('x', 25), "four" };

        MainWindowViewModel.MonitorText(lines, 10, 4).ShouldBe(new string('x', 25) + "\nfour");
        MainWindowViewModel.MonitorText(lines, 10, 5).ShouldBe("two\n" + new string('x', 25) + "\nfour");
        MainWindowViewModel.MonitorText(lines, 80, 24).ShouldBe(string.Join("\n", lines));
        MainWindowViewModel.MonitorText(System.Array.Empty<string>(), 80, 24).ShouldBe(string.Empty);
    }

    [AvaloniaTest]
    public void Monitor_ShowsTheGameTextInGreen()
    {
        var monitor = new TranscriptMonitor
        {
            Text = "West of House\nYou are standing in an open field west of a white house.\n\n>north"
        };
        var window = new Window { Content = monitor, Width = 300, Height = 240, Background = Brushes.White };
        window.Show();
        try
        {
            using var frame = window.CaptureRenderedFrame();
            frame.ShouldNotBeNull();
            var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
            if (!string.IsNullOrEmpty(shots)) frame.Save(System.IO.Path.Combine(shots, "transcript-monitor.png"));

            using var pixels = frame.Lock();
            var green = 0;
            var row = new int[frame.PixelSize.Width];
            {
                for (var y = 0; y < frame.PixelSize.Height; y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(pixels.Address + y * pixels.RowBytes, row, 0, row.Length);
                    for (var x = 0; x < frame.PixelSize.Width; x++)
                    {
                        var p = (uint)row[x];
                        var r = (p >> 16) & 0xFF;
                        var g = (p >> 8) & 0xFF;
                        var b = p & 0xFF;
                        if (g > 120 && g > r + 40 && g > b + 20) green++;
                    }
                }
            }

            green.ShouldBeGreaterThan(20);
        }
        finally
        {
            window.Close();
        }
    }

    private static string Lines(int count) =>
        string.Join("\n", Enumerable.Range(1, count).Select(i => i == count ? ">look" : "Line " + i));

    [AvaloniaTest]
    public void TheLastLine_StaysAtTheBottom_AsTheTextGrowsAndShrinks()
    {
        // after going back a move the text is shorter: the screen still shows its end, not its top
        var monitor = new TranscriptMonitor();
        var window = new Window { Width = 300, Height = 300, Content = monitor };
        try
        {
            window.Show();
            var screen = monitor.FindControl<BottomAnchor>("Screen")!;
            var text = monitor.FindControl<TextBlock>("ScreenText")!;
            void Settle()
            {
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
            }

            foreach (var count in new[] { 60, 40, 3 })
            {
                monitor.Text = Lines(count);
                Settle();
                text.Bounds.Bottom.ShouldBe(screen.Bounds.Height, 1, $"{count} lines");
                if (count > 3) text.Bounds.Top.ShouldBeLessThan(0, $"{count} lines overflow at the top");
            }
        }
        finally
        {
            window.Close();
        }
    }
}
