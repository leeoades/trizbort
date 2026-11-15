using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///     One line in the "which room?" list: a room on the map, or the new room the transcript may be showing
///     (<see cref="Room" /> null), with the description to compare.
/// </summary>
public sealed record DisambiguationChoice(string Name, string Description, Room? Room)
{
    public bool IsNewRoom => Room == null;
}

/// <summary>
///     State and outcome for the "which room does the transcript mean?" prompt: the rooms with that name, and a new
///     room, to pick from; OK takes the one picked, Cancel (or closing the window) maps nothing for it. "Don't ask
///     again" lets automap guess for the rest of the run.
/// </summary>
public class DisambiguateRoomsViewModel : ViewModelBase
{
    public const string NewRoomName = "<Add a new room>";

    private DisambiguationChoice? _selected;
    private bool _dontAskAgain;

    public DisambiguateRoomsViewModel(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates)
    {
        Candidates = candidates;
        ContextText = BuildContext(transcriptLine, roomDescription);
        var description = Normalise(roomDescription).Trim();
        Choices = candidates
            .Select(room => new DisambiguationChoice(room.Name, room.PrimaryDescription ?? string.Empty, room))
            .Append(new DisambiguationChoice(NewRoomName,
                description.Length == 0 ? "(The transcript doesn't describe it.)" : description, null))
            .ToList();
        OkCommand = new RelayCommand(ChooseSelected, () => _selected != null);
        CancelCommand = new RelayCommand(() => Complete(RoomDisambiguation.Nothing()));
        if (candidates.Count == 1)
        {
            _selected = Choices[0];
        }
    }

    public string IntroText =>
        "Trizbort isn't sure which room the transcript means. Several rooms have this name, or the room's " +
        "description has changed during play. Pick the room it is, or add a new room.";

    public IReadOnlyList<Room> Candidates { get; }

    /// <summary>The rooms with that name, then a new room.</summary>
    public IReadOnlyList<DisambiguationChoice> Choices { get; }

    public string ContextText { get; }

    public DisambiguationChoice? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                OnPropertyChanged(nameof(SelectedDescription));
                OkCommand.NotifyCanExecuteChanged();
                Preview?.Invoke(value?.Room);
            }
        }
    }

    /// <summary>
    ///     Shows the room picked on the map, with the rooms around it (null for a new room), while the dialog is open.
    /// </summary>
    public Action<Room?>? Preview { get; set; }

    public string SelectedDescription => _selected?.Description ?? string.Empty;

    /// <summary>Don't ask again while automapping: guess instead.</summary>
    public bool DontAskAgain
    {
        get => _dontAskAgain;
        set => SetProperty(ref _dontAskAgain, value);
    }

    public string DontAskAgainTip =>
        "For the rest of this run automap guesses instead of asking: the room whose description starts the same way; " +
        "if the game shows a description none of them has, a new room when you moved, the room you're in when you " +
        "didn't.";

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    /// <summary>What closing the window gives: nothing mapped.</summary>
    public RoomDisambiguation Result { get; private set; } = RoomDisambiguation.Nothing();

    public event EventHandler? CloseRequested;

    private static string BuildContext(string line, string description)
    {
        var text = Normalise(line).TrimEnd('\n');
        var desc = Normalise(description);
        return desc.Length == 0 ? text : text + "\n\n" + desc;
    }

    private static string Normalise(string? text)
    {
        return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private void ChooseSelected()
    {
        if (_selected == null)
        {
            return;
        }

        Complete(_selected.Room == null
            ? new RoomDisambiguation(null, true, DontAskAgain)
            : new RoomDisambiguation(_selected.Room, false, DontAskAgain));
    }

    private void Complete(RoomDisambiguation result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>The new-room line in the "which room?" list is in italics.</summary>
public static class DisambiguationChoiceStyle
{
    public static readonly global::Avalonia.Data.Converters.IValueConverter Italic =
        new global::Avalonia.Data.Converters.FuncValueConverter<bool, global::Avalonia.Media.FontStyle>(isNew =>
            isNew ? global::Avalonia.Media.FontStyle.Italic : global::Avalonia.Media.FontStyle.Normal);
}
