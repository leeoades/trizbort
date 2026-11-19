using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///     One line in the "which room?" list: a room on the map, or the new room the transcript may be showing
///     (<see cref="Room" /> null), with the description to compare (shown beside the list, not in it). Under the name:
///     the room's subtitle, the objects in it, and, when those don't tell it from another room with its name, the
///     ways out of it (<see cref="Detail" />).
/// </summary>
public sealed record DisambiguationChoice(
    string Name,
    string Description,
    Room? Room,
    string Detail = "",
    string Subtitle = "",
    string Objects = "")
{
    public bool IsNewRoom => Room == null;

    /// <summary>Whether the ways out tell this room apart from another with its name, its subtitle and objects.</summary>
    public bool HasDetail => !string.IsNullOrEmpty(Detail);

    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);

    public bool HasObjects => !string.IsNullOrEmpty(Objects);
}

/// <summary>
///     State and outcome for the "which room does the transcript mean?" prompt: the rooms with that name, and a new
///     room, to pick from; OK takes the one picked, Cancel (or closing the window) maps nothing for it. "Don't ask
///     again" lets automap guess for the rest of the run.
/// </summary>
public class DisambiguateRoomsViewModel : ViewModelBase
{
    public const string NewRoomName = "<Add a new room>";

    /// <summary>The new-room line while tracking only: nothing is added to the map.</summary>
    public const string NotOnTheMapName = "<Somewhere not on the map>";

    private DisambiguationChoice? _selected;
    private bool _dontAskAgain;

    public DisambiguateRoomsViewModel(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates,
        bool trackingOnly = false)
    {
        TrackingOnly = trackingOnly;
        Candidates = candidates;
        ContextText = BuildContext(transcriptLine, roomDescription);
        var description = Normalise(roomDescription).Trim();
        // rooms with one name (a maze of rooms all alike): each with its subtitle and objects, and the ways out when
        // those don't tell it apart from the others
        var ways = Trizbort.Automap.RoomTells.WaysApart(candidates);
        Choices = candidates
            .Select(room => new DisambiguationChoice(room.Name, room.PrimaryDescription ?? string.Empty, room,
                ways.TryGetValue(room, out var way) ? way : string.Empty,
                room.SubTitle?.Trim() ?? string.Empty, Trizbort.Automap.RoomTells.ObjectList(room)))
            .Append(new DisambiguationChoice(trackingOnly ? NotOnTheMapName : NewRoomName,
                description.Length == 0 ? "(The transcript doesn't describe it.)" : description, null))
            .ToList();
        OkCommand = new RelayCommand(ChooseSelected, () => _selected != null);
        CancelCommand = new RelayCommand(() => Complete(RoomDisambiguation.Nothing()));
        if (candidates.Count == 1)
        {
            _selected = Choices[0];
        }
    }

    public string IntroText => TrackingOnly
        ? "Trizbort isn't sure which room the transcript means. Several rooms have this name, or the room's " +
          "description has changed during play. Pick the room it is. Automap is tracking only, so it adds nothing " +
          "to the map: to add a room, switch to Mapping on the automap panel first."
        : "Trizbort isn't sure which room the transcript means. Several rooms have this name, or the room's " +
          "description has changed during play. Pick the room it is, or add a new room.";

    /// <summary>Automap is tracking only (not mapping): a room not on the map isn't added.</summary>
    public bool TrackingOnly { get; }

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
    ///     The user picked a room on the map behind the dialog: if it's one of the rooms asked about, it's picked in the
    ///     list too. Returns whether it was.
    /// </summary>
    public bool PickFromMap(Room room)
    {
        var choice = Choices.FirstOrDefault(item => item.Room == room);
        if (choice == null) return false;
        Selected = choice;
        return true;
    }

    /// <summary>
    ///     Shows the room picked on the map, with the rooms around it (null for a new room), while the dialog is open.
    /// </summary>
    public Action<Room?>? Preview { get; set; }

    /// <summary>The room picked: its description, then the objects in it (one a line), as the map has them.</summary>
    public string SelectedDescription
    {
        get
        {
            if (_selected == null) return string.Empty;
            var objects = (_selected.Room?.Objects ?? string.Empty)
                .Split(new[] { '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToList();
            if (objects.Count == 0) return _selected.Description;
            var listed = "Objects:\n" + string.Join("\n", objects.Select(item => "  " + item));
            return _selected.Description.Length == 0 ? listed : _selected.Description + "\n\n" + listed;
        }
    }

    /// <summary>Don't ask again: make a best guess instead, from now on (Automap Settings turns it off again).</summary>
    public bool DontAskAgain
    {
        get => _dontAskAgain;
        set => SetProperty(ref _dontAskAgain, value);
    }

    public string DontAskAgainTip =>
        "From now on automap makes a best guess instead of asking: the room whose description starts the same way; " +
        "if the game shows a description none of them has, a new room when you moved, the room you're in when you " +
        "didn't. To be asked again, turn it off in Automap Settings (the cog on the automap panel).";

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
