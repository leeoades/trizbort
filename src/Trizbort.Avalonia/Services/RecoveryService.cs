using System.Diagnostics;
using Avalonia.Threading;
using Trizbort.Domain.Application;
using Trizbort.Domain.Recovery;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Keeps a recovery copy of the open map while it has unsaved changes (ticket 135). Watches the session: a model
///   change on a dirty map schedules a write about <see cref="DefaultDelay" /> after the last change (debounced, on the
///   UI thread because the map is read); when the map is no longer dirty (saved, new, opened, discarded) the copy is
///   deleted. Never throws into the app.
/// </summary>
public sealed class RecoveryService : IDisposable
{
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(2);

    private readonly EditorSession _session;
    private readonly Func<bool> _isEnabled;
    private readonly string _appVersion;
    private readonly TimeSpan _delay;
    private readonly object _timerGate = new();
    private Timer? _timer;
    private bool _disposed;

    public RecoveryService(
        EditorSession session,
        RecoveryStore store,
        Func<bool> isEnabled,
        string appVersion,
        TimeSpan? delay = null)
    {
        _session = session;
        Store = store;
        _isEnabled = isEnabled;
        _appVersion = appVersion;
        _delay = delay ?? DefaultDelay;
        _session.Changed += OnSessionChanged;
    }

    /// <summary>The service of the running app, set by App start-up; null in tests that do not create one.</summary>
    public static RecoveryService? Current { get; set; }

    public RecoveryStore Store { get; }

    public string AppVersion => _appVersion;

    private bool IsEnabled()
    {
        try
        {
            return _isEnabled();
        }
        catch (Exception)
        {
            return true;
        }
    }

    private void OnSessionChanged(EditorChange change)
    {
        if ((change & (EditorChange.Model | EditorChange.Dirty)) == 0) return;
        try
        {
            var project = Project.Current;
            if (project != null && project.IsDirty && IsEnabled()) Schedule();
            else Cleanup();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery change handling failed: {ex.Message}");
        }
    }

    private void Schedule()
    {
        lock (_timerGate)
        {
            if (_disposed) return;
            if (_timer == null) _timer = new Timer(OnTimer, null, _delay, Timeout.InfiniteTimeSpan);
            else _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    private void CancelTimer()
    {
        lock (_timerGate)
        {
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnTimer(object? state)
    {
        // the map must be read on the UI thread
        Dispatcher.UIThread.Post(() => WriteNow(false));
    }

    /// <summary>Deletes this instance's copy and stops any pending write.</summary>
    public void Cleanup()
    {
        CancelTimer();
        Store.DeleteCopy();
    }

    /// <summary>
    ///   Writes the copy now if the map is dirty and copies are enabled (or <paramref name="force" /> to write even
    ///   a clean map). Returns true if a copy was written.
    /// </summary>
    public bool WriteNow(bool force)
    {
        try
        {
            if (_disposed) return false;
            var project = Project.Current;
            if (project == null) return false;
            if (!IsEnabled()) return false;
            if (!force && !project.IsDirty) return false;
            CancelTimer();
            return Store.WriteCopy(project, _appVersion);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery write failed: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        lock (_timerGate)
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }

        _session.Changed -= OnSessionChanged;
        Store.Dispose();
    }
}
