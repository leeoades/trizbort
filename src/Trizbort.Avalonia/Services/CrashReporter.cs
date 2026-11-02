using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using Trizbort.Domain.Application;
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
    ///   Keeps the work if it can and logs the exception. Never throws.
    /// </summary>
    public static CrashCapture Capture(Exception exception)
    {
        var service = RecoveryService.Current;
        var work = KeepWork(service);
        var logPath = RecoveryStore.WriteCrashLog(exception, service?.AppVersion ?? string.Empty);
        return new CrashCapture(logPath, work);
    }

    /// <summary>How long a crash on another thread waits for the UI thread to copy the map.</summary>
    public static TimeSpan UiThreadWait { get; set; } = TimeSpan.FromSeconds(2);

    private static UnsavedWork KeepWork(RecoveryService? service)
    {
        try
        {
            var project = Project.Current;
            if (project == null || !project.IsDirty) return UnsavedWork.None;
            if (service == null) return UnsavedWork.NotKept;

            if (Dispatcher.UIThread.CheckAccess()) return service.WriteNow(false) ? UnsavedWork.Kept : UnsavedWork.NotKept;

            // The map is edited on the UI thread, so the copy is made there. If the UI thread can't do it in time,
            // the last copy written stays as it is rather than being replaced by one read mid-edit.
            var write = Dispatcher.UIThread.InvokeAsync(() => service.WriteNow(false)).GetTask();
            return write.Wait(UiThreadWait) && write.Result ? UnsavedWork.Kept : UnsavedWork.NotKept;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery on crash failed: {ex.Message}");
            return UnsavedWork.NotKept;
        }
    }

    public static string BuildMessage(CrashCapture capture)
    {
        // The .NET message is only in the log: it means nothing to the player.
        const string issues = "https://github.com/leeoades/trizbort/issues";
        var message = $"Something went wrong and {AppInfo.ProductName} couldn't finish what it was doing.";
        message += capture.Work switch
        {
            UnsavedWork.Kept => " A copy of your unsaved changes has been kept.",
            UnsavedWork.NotKept => " Please save your map now.",
            _ => string.Empty
        };
        if (capture.LogPath != null)
            message += $"\n\nThe details are saved in:\n{capture.LogPath}\n\nPlease send that file to {issues} so it can be fixed.";
        return message;
    }

    /// <summary>The error path for UI-thread exceptions and for failed commands: capture, tell the user, carry on.</summary>
    public static void ReportUiThreadError(Exception exception)
    {
        try
        {
            ShowMessage(BuildMessage(Capture(exception)));
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

/// <summary>What happened to the player's unsaved changes when something went wrong.</summary>
public enum UnsavedWork
{
    /// <summary>There were none.</summary>
    None,

    /// <summary>A recovery copy was written just now.</summary>
    Kept,

    /// <summary>There are unsaved changes and no fresh copy could be written (recovery off, or the write failed).</summary>
    NotKept
}

/// <summary>The result of <see cref="CrashReporter.Capture" />: the crash log (null if it couldn't be written) and the work.</summary>
public readonly record struct CrashCapture(string? LogPath, UnsavedWork Work);
