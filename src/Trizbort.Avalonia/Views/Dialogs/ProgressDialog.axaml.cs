using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>
///     Shows a message and a progress bar while long work runs off the UI thread (Tidy Map). The bar fills over the
///     time the work is expected to take at most; Cancel, Esc or closing the window cancels the work.
/// </summary>
public partial class ProgressDialog : Window
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private int _expectedMs = 1000;
    private bool _finished;

    public ProgressDialog()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => UpdateBar();
        Opened += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    /// <summary>Raised when the user cancels (Cancel, Esc or closing the window) before the work finished.</summary>
    public event EventHandler? Cancelled;

    public void Configure(string title, string message, int expectedMs)
    {
        Title = title;
        MessageText.Text = message;
        _expectedMs = Math.Max(1, expectedMs);
        _clock.Restart();
        UpdateBar();
    }

    /// <summary>The bar's value, 0 to 100 (never quite full until the work is done).</summary>
    public double Progress => Bar.Value;

    /// <summary>Closes the dialog because the work is done.</summary>
    public void Finish()
    {
        _finished = true;
        Bar.Value = 100;
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (!_finished)
        {
            _finished = true;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateBar()
    {
        Bar.Value = Math.Min(95, 100.0 * _clock.ElapsedMilliseconds / _expectedMs);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        MessageText.Text = "Cancelling...";
        CancelButton.IsEnabled = false;
        Close();
    }
}
