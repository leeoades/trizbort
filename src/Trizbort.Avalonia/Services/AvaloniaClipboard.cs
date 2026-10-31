using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using ICoreClipboard = Trizbort.Editor.IClipboard;

namespace Trizbort.Avalonia.Services;

/// <summary>
///     Core <see cref="ICoreClipboard" /> over the system clipboard of the active top level. Without a top level
///     (or a clipboard) it behaves like an empty clipboard that ignores writes.
/// </summary>
public sealed class AvaloniaClipboard(Func<TopLevel?> getTopLevel) : ICoreClipboard
{
    private readonly Func<TopLevel?> _getTopLevel = getTopLevel;

    public async Task SetTextAsync(string text)
    {
        var clipboard = await OnUiThreadAsync(() => _getTopLevel()?.Clipboard);
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public async Task<string> GetTextAsync()
    {
        var clipboard = await OnUiThreadAsync(() => _getTopLevel()?.Clipboard);
        return clipboard == null ? null! : await clipboard.TryGetTextAsync() ?? null!;
    }

    private static Task<IClipboard?> OnUiThreadAsync(Func<IClipboard?> action)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? Task.FromResult(action())
            : Dispatcher.UIThread.InvokeAsync(action).GetTask();
    }
}
