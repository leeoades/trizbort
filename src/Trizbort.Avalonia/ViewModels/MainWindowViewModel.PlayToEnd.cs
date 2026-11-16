using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.ViewModels;

// Play to the End: a progress bar in the automap panel while automap plays the moves left, and the map locked (it
// can be looked around, not changed; the menus and toolbar are off) until it's done or paused.
public partial class MainWindowViewModel
{
    private int _playToEndFrom;
    private int _playToEndTarget;
    private Action? _playToEndUnlock;

    /// <summary>Automap is playing the moves left: the progress bar shows, and the map is locked.</summary>
    [ObservableProperty]
    private bool _isPlayingToEnd;

    /// <summary>How far through the moves it's got, 0 to 100.</summary>
    [ObservableProperty]
    private double _playToEndProgress;

    /// <summary>"Playing to the end: move 120 of 456".</summary>
    [ObservableProperty]
    private string _playToEndText = string.Empty;

    /// <summary>
    ///     Starts showing progress, and locks the map, when automap is playing through moves (Play to the End, F5, or
    ///     a run started without pausing): at least <paramref name="atLeast" /> to go.
    /// </summary>
    private void StartPlayToEndProgress(int atLeast = 1)
    {
        var engine = AutomapEngine.Instance;
        var (played, total) = engine.MoveCount();
        if (IsPlayingToEnd || !engine.IsRunning || engine.MoveByMove || total - played < atLeast) return;
        _playToEndFrom = played;
        _playToEndTarget = total;
        IsPlayingToEnd = true;
        ShowPlayToEndProgress(played);
        MapCanvasControl.ViewOnly = true;
        var window = GetActiveWindow();
        if (window != null) _playToEndUnlock = AvaloniaEditorPrompts.LockAllButTheMap(window, keepAutomapPanel: true);
    }

    /// <summary>With the automap panel's updates: how far it's got, and whether it's done (or paused).</summary>
    private void FollowPlayToEnd()
    {
        // playing through moves however it started (F5 too); following a game as it's played is a move at a time
        if (!IsPlayingToEnd) StartPlayToEndProgress(atLeast: 2);
        if (!IsPlayingToEnd) return;
        var engine = AutomapEngine.Instance;
        var (played, _) = engine.MoveCount();
        ShowPlayToEndProgress(played);
        if (played >= _playToEndTarget || engine.MoveByMove || !engine.IsRunning)
        {
            EndPlayToEndProgress();
            return;
        }

        // a question automap asked on the way unlocks the map when it's answered: still playing, so lock it again
        MapCanvasControl.ViewOnly = true;
    }

    /// <summary>Done, paused or stopped: the progress bar goes, and the map can be changed again.</summary>
    internal void EndPlayToEndProgress()
    {
        if (!IsPlayingToEnd) return;
        IsPlayingToEnd = false;
        MapCanvasControl.ViewOnly = false;
        _playToEndUnlock?.Invoke();
        _playToEndUnlock = null;
    }

    private void ShowPlayToEndProgress(int played)
    {
        var moves = Math.Max(1, _playToEndTarget - _playToEndFrom);
        var done = Math.Clamp(played - _playToEndFrom, 0, moves);
        PlayToEndProgress = 100.0 * done / moves;
        PlayToEndText = string.Format(System.Globalization.CultureInfo.CurrentCulture,
            "Playing to the end: move {0:N0} of {1:N0}", played, _playToEndTarget);
    }
}
