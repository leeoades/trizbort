using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using Trizbort.Domain.Recovery;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Turns unexpected exceptions into: a recovery copy written immediately, a crash log, and (for UI-thread errors) a
///   message instead of a silent crash (ticket 135). Everything here is best effort and never throws.
/// </summary>
public static class CrashReporter
{
    private static bool _installed;
    private static Func<Window?> _getOwner = () => null;
    private static IDialogService? _dialogs;

    /// <summary>Hooks the three global handlers. Safe to call once at start-up.</summary>
    public static void Install(IDialogService? dialogs, Func<Window?> getOwner)
    {
        _dialogs = dialogs;
        _getOwner = getOwner;
        if (_installed) return;
        _installed = true;

        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            ReportUiThreadError(e.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Capture(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Capture(e.Exception);
            e.SetObserved();
        };
    }

    /// <summary>
    ///   Keeps the work and logs the exception. Returns the crash log path, or null if it could not be written.
    /// </summary>
    public static string? Capture(Exception exception)
    {
        var service = RecoveryService.Current;
        try
        {
            service?.WriteNow(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery on crash failed: {ex.Message}");
        }

        return RecoveryStore.WriteCrashLog(exception, service?.AppVersion ?? string.Empty);
    }

    public static string BuildMessage(string exceptionMessage, string? logPath)
    {
        // The .NET message (exceptionMessage) is only in the log: it means nothing to the player.
        const string issues = "https://github.com/leeoades/trizbort/issues";
        return logPath == null
            ? $"Something went wrong and {AppInfo.ProductName} couldn't finish what it was doing. Your unsaved work has been kept."
            : $"Something went wrong and {AppInfo.ProductName} couldn't finish what it was doing. Your unsaved work has been kept.\n\n" +
              $"The details are saved in:\n{logPath}\n\nPlease send that file to {issues} so it can be fixed.";
    }

    /// <summary>The error path for UI-thread exceptions and for failed commands: capture, tell the user, carry on.</summary>
    public static void ReportUiThreadError(Exception exception)
    {
        string? path = null;
        try
        {
            path = Capture(exception);
            ShowMessage(BuildMessage(exception.Message, path));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Reporting an error failed: {ex.Message}");
        }
    }

    /// <summary>Test hook: replaces how the message is shown.</summary>
    public static Action<string>? MessageSink { get; set; }

    private static void ShowMessage(string message)
    {
        if (MessageSink != null)
        {
            MessageSink(message);
            return;
        }

        var dialogs = _dialogs;
        if (dialogs == null) return;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _ = dialogs.ShowMessageAsync(_getOwner(), AppInfo.ProductName, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not show the error message: {ex.Message}");
            }
        });
    }
}
