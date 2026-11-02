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
            // Undo and redo rebuild the map and report it, still marked clean, before setting whether it is saved.
            // Decide once that has finished, so a map that is still unsaved never loses its copies.
            else Dispatcher.UIThread.Post(Settle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery change handling failed: {ex.Message}");
        }
    }

    /// <summary>Acts on the map's final state after a change: write a copy later if it is unsaved, else clean up.</summary>
    private void Settle()
    {
        try
        {
            if (_disposed) return;
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
        var project = Project.Current;
        if (project == null || !project.IsDirty) DeleteAdopted();
    }

    private RecoveryEntry? _adopted;

    /// <summary>
    ///   A recovered copy whose work is now in the open map but not yet in a copy of ours: it is deleted once our own
    ///   copy is written, or when the map is saved or discarded, so the work always has a copy until then.
    /// </summary>
    public void DeleteWhenSafe(RecoveryEntry entry)
    {
        _adopted = entry;
    }

    private void DeleteAdopted()
    {
        var entry = _adopted;
        if (entry == null) return;
        _adopted = null;
        try
        {
            Store.Delete(entry);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Deleting a recovered copy failed: {ex.Message}");
        }
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
            if (!Store.WriteCopy(project, _appVersion)) return false;
            DeleteAdopted();
            return true;
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
        // Closing normally means the player has saved the map or chosen to discard it, so a recovered copy that was
        // waiting for that goes too.
        DeleteAdopted();
        Store.Dispose();
    }
}
