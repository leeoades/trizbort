using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>One join in Split Room's list, as it reads from the room, and whether it goes to the new room.</summary>
public sealed partial class SplitRoomJoin : ObservableObject
{
    public SplitRoomJoin(Connection join, string text, bool moves)
    {
        Join = join;
        Text = text;
        _moves = moves;
    }

    public Connection Join { get; }

    public string Text { get; }

    [ObservableProperty]
    private bool _moves;
}

/// <summary>
///     Split Room: the room is two places with one name. The joins that go to the new room are ticked; while
///     automapping, which room the player is in. OK gives the choice; Cancel (or closing) gives null.
/// </summary>
public sealed partial class SplitRoomViewModel : ViewModelBase
{
    public SplitRoomViewModel(Room room, string newName, IReadOnlyList<Connection> joins,
        IReadOnlyCollection<Connection> ticked, RoomSplitWhere? where)
    {
        RoomName = room.Name;
        NewName = newName;
        Joins = joins.Select(join => new SplitRoomJoin(join, RoomSplit.Describe(join, room), ticked.Contains(join))).ToList();
        AsksWhere = where != null;
        _inNew = where == RoomSplitWhere.New;
        _inOriginal = where == RoomSplitWhere.Original;
        _inNeither = where is null or RoomSplitWhere.Neither;
        OkCommand = new RelayCommand(Ok);
        CancelCommand = new RelayCommand(() => Complete(null));
    }

    public string RoomName { get; }

    public string NewName { get; }

    public string IntroText =>
        $"'{RoomName}' is two places with one name. Tick the joins that belong to the other one: they move to a new " +
        $"room, '{NewName}', at the same sides.";

    public IReadOnlyList<SplitRoomJoin> Joins { get; }

    /// <summary>While automapping: asks which room the player is in.</summary>
    public bool AsksWhere { get; }

    public string InOriginalText => $"In '{RoomName}'";

    public string InNewText => $"In '{NewName}'";

    [ObservableProperty]
    private bool _inOriginal;

    [ObservableProperty]
    private bool _inNew;

    [ObservableProperty]
    private bool _inNeither;

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RoomSplitChoice? Result { get; private set; }

    public event EventHandler? CloseRequested;

    private void Ok()
    {
        var where = !AsksWhere || InNeither ? RoomSplitWhere.Neither : InNew ? RoomSplitWhere.New : RoomSplitWhere.Original;
        Complete(new RoomSplitChoice(Joins.Where(join => join.Moves).Select(join => join.Join).ToList(), where));
    }

    private void Complete(RoomSplitChoice? result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
