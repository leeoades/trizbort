using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Trizbort.Avalonia;

/// <summary>A source of "open this file" requests from the OS (a small seam so tests can fake it).</summary>
public interface IFileActivationSource
{
    event Action<string>? FileActivated;
}

/// <summary>
///   Opens files the OS asks the running app to open (macOS Finder open-with, a second launch forwarded by the OS).
///   Requests that arrive before the window is shown are queued until <see cref="MarkReady" />.
///   Windows and Linux pass the file on the command line instead (see StartupActions.RunAsync).
/// </summary>
public sealed class FileActivationHandler
{
    private readonly Func<string, Task> _open;
    private readonly List<string> _pending = new();
    private bool _ready;

    public FileActivationHandler(Func<string, Task> open)
    {
        _open = open;
    }

    public int PendingCount => _pending.Count;

    public void Attach(IFileActivationSource source)
    {
        source.FileActivated += OnFileActivated;
    }

    public void OnFileActivated(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!_ready)
        {
            _pending.Add(path);
            return;
        }
        _ = OpenSafeAsync(path);
    }

    /// <summary>The main window is shown: open the queued files (the last one wins, as only one map can be open).</summary>
    public Task MarkReady()
    {
        _ready = true;
        if (_pending.Count == 0) return Task.CompletedTask;
        var path = _pending[_pending.Count - 1];
        _pending.Clear();
        return OpenSafeAsync(path);
    }

    private async Task OpenSafeAsync(string path)
    {
        try
        {
            await _open(path);
        }
        catch (Exception)
        {
            // an activation must never crash the app
        }
    }
}

/// <summary>
///   Adapts Avalonia's <see cref="IActivatableLifetime" /> (file activation, used on macOS) to
///   <see cref="IFileActivationSource" />. Not verifiable without a Mac.
/// </summary>
public sealed class AvaloniaFileActivationSource : IFileActivationSource
{
    public event Action<string>? FileActivated;

    public AvaloniaFileActivationSource(IActivatableLifetime lifetime)
    {
        lifetime.Activated += (_, e) =>
        {
            if (e is not FileActivatedEventArgs files) return;
            foreach (var item in files.Files)
            {
                var path = item.TryGetLocalPath();
                if (path != null) FileActivated?.Invoke(path);
            }
        };
    }
}
