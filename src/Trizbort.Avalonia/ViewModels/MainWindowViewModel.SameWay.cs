using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Automap;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///     The note in the automap panel when the last move went a way already seen leading somewhere else: automap kept
///     both, side by side, without asking, and the note offers to map the move again if the room left moves (a lift) or
///     was another room with its name (a maze).
/// </summary>
public partial class MainWindowViewModel
{
    private AutomapEngine.SameWayNote? _shownSameWay;
    private bool _canCorrectSameWay;

    /// <summary>There's a note about the last move.</summary>
    [ObservableProperty]
    private bool _hasSameWay;

    /// <summary>"East from Hall now leads to Kitchen as well as Pantry..."</summary>
    [ObservableProperty]
    private string _sameWayText = string.Empty;

    /// <summary>"'Hall' Moves"</summary>
    [ObservableProperty]
    private string _sameWayMovesText = string.Empty;

    /// <summary>"A Different 'Hall'"</summary>
    [ObservableProperty]
    private string _sameWayDifferentText = string.Empty;

    /// <summary>The move can be mapped again (there's an undo history to take it off the map with).</summary>
    public bool CanCorrectSameWay => _canCorrectSameWay;

    /// <summary>Brings the note up to date with the last move.</summary>
    private void RefreshSameWay()
    {
        var engine = AutomapEngine.Instance;
        var note = engine.SameWay;
        if (engine.CanCorrectSameWay != _canCorrectSameWay)
        {
            _canCorrectSameWay = engine.CanCorrectSameWay;
            OnPropertyChanged(nameof(CanCorrectSameWay));
            SameWayMovesCommand.NotifyCanExecuteChanged();
            SameWayDifferentRoomCommand.NotifyCanExecuteChanged();
        }

        if (ReferenceEquals(note, _shownSameWay)) return;
        _shownSameWay = note;
        HasSameWay = note != null;
        if (note == null) return;
        var way = note.Way.Length > 0 ? char.ToUpperInvariant(note.Way[0]) + note.Way[1..] : note.Way;
        SameWayText = $"{way} from {note.From} now leads to {note.Now} as well as {note.Before}. Both are on the map, " +
                      "side by side.";
        SameWayMovesText = $"'{note.From}' Moves";
        SameWayDifferentText = $"A Different '{note.From}'";
    }

    /// <summary>The room left moves (a lift): that move is mapped again, with a stop of it where it went.</summary>
    [RelayCommand(CanExecute = nameof(CanCorrectSameWay))]
    public void SameWayMoves() => CorrectSameWay(AutomapSameDirectionResult.Moves);

    /// <summary>The room left was another with its name (a maze): that move is mapped again from a new room.</summary>
    [RelayCommand(CanExecute = nameof(CanCorrectSameWay))]
    public void SameWayDifferentRoom() => CorrectSameWay(AutomapSameDirectionResult.DifferentRoom);

    /// <summary>Both rooms are right: the note goes.</summary>
    [RelayCommand]
    public void DismissSameWay()
    {
        AutomapEngine.Instance.DismissSameWay();
        RefreshAutomapPanel();
    }

    private void CorrectSameWay(AutomapSameDirectionResult answer)
    {
        AutomapEngine.Instance.CorrectSameWay(answer);
        RefreshAutomapPanel();
    }
}
