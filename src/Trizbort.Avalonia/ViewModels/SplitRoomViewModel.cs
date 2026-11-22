using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///     One way out of the room in Split Room, drawn at its side of the room it's in: the original on the left, or the
///     new one on the right once it's clicked across.
/// </summary>
public sealed partial class SplitRoomJoin : ObservableObject
{
    private readonly Action<SplitRoomJoin> _toggle;

    public SplitRoomJoin(SplitExit exit, bool moves, Action<SplitRoomJoin> toggle)
    {
        Exit = exit;
        Join = exit.Join;
        Text = exit.Text;
        Side = exit.Side;
        _moves = moves;
        _toggle = toggle;
    }

    /// <summary>The way in or out it is: half of a two-way join, or a whole one-way join or loop.</summary>
    public SplitExit Exit { get; }

    public Connection Join { get; }

    public string Text { get; }

    /// <summary>The side of the room it leaves from.</summary>
    public CompassPoint Side { get; }

    /// <summary>It goes to the new room.</summary>
    [ObservableProperty]
    private bool _moves;

    public string Tip => Moves ? "Click to keep it on this room instead" : "Click to move it to the other room";

    partial void OnMovesChanged(bool value) => OnPropertyChanged(nameof(Tip));

    [RelayCommand]
    private void Toggle() => _toggle(this);
}

/// <summary>
///     One of the nine places around a room in Split Room: the eight compass sides (the room itself in the middle),
///     holding the ways out from that side.
/// </summary>
public sealed class SplitRoomCell
{
    public SplitRoomCell(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public int Row { get; }

    public int Column { get; }

    public ObservableCollection<SplitRoomJoin> Joins { get; } = new();
}

/// <summary>
///     Split Room: the room is two places with one name. Both are shown, the original on the left and the new one on
///     the right, with the ways out at their sides; clicking a way moves it across (a way that loops back into the room
///     becomes a one-way join into the other one). While automapping, which room the player is in. OK gives the
///     choice; Cancel (or closing) gives null. Lee, 21 Nov 2026: depict both rooms, and click the exits across.
/// </summary>
public sealed partial class SplitRoomViewModel : ViewModelBase
{
    private readonly SplitRoomCell[] _original = NineCells();
    private readonly SplitRoomCell[] _new = NineCells();

    public SplitRoomViewModel(Room room, string newName, IReadOnlyList<Connection> joins,
        IReadOnlyCollection<Connection> ticked, RoomSplitWhere? where)
    {
        RoomName = room.Name;
        NewName = newName;
        Description = room.PrimaryDescription ?? string.Empty;
        Objects = (room.Objects ?? string.Empty).Replace("\r", string.Empty).Replace("\n", ", ").Trim(' ', ',');
        // each way in and out on its own (a two-way join is two), the way the player just came in ticked
        var exits = RoomSplit.ExitsOf(room).Where(exit => joins.Contains(exit.Join)).ToList();
        var suggested = ticked.Count > 0 ? RoomSplit.SuggestedExit(exits.Where(exit => ticked.Contains(exit.Join)).ToList()) : null;
        Joins = exits.Select(exit => new SplitRoomJoin(exit, exit == suggested, Toggle)).ToList();
        foreach (var join in Joins) Place(join);
        _where = where ?? RoomSplitWhere.Neither;
        OkCommand = new RelayCommand(Ok);
        CancelCommand = new RelayCommand(() => Complete(null));
    }

    public string RoomName { get; }

    public string NewName { get; }

    /// <summary>What the game says there: the same for both, as they're alike.</summary>
    public string Description { get; }

    public bool HasDescription => Description.Length > 0;

    /// <summary>The objects noted in the original room (they mark it; the new one has none yet).</summary>
    public string Objects { get; }

    public bool HasObjects => Objects.Length > 0;

    public string IntroText =>
        $"'{RoomName}' is two places with the same name and description. Click a way in or out to move it to the " +
        "other room.";

    public string OriginalHeading => "Original: the one on the map";

    // Lee, 22 Nov 2026: splitting while you're in the room, you're in the new one; else move your token on the map
    public string NewHeading => _where == RoomSplitWhere.New ? "New: you are here" : "New";

    public IReadOnlyList<SplitRoomJoin> Joins { get; }

    /// <summary>The nine places around the original room, with the ways that stay.</summary>
    public IReadOnlyList<SplitRoomCell> OriginalCells => _original;

    /// <summary>The nine places around the new room, with the ways moved across.</summary>
    public IReadOnlyList<SplitRoomCell> NewCells => _new;

    /// <summary>Where the player will be: in the new room when automap has them in the room being split.</summary>
    private readonly RoomSplitWhere _where;

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RoomSplitChoice? Result { get; private set; }

    public event EventHandler? CloseRequested;

    private void Toggle(SplitRoomJoin join)
    {
        Unplace(join);
        join.Moves = !join.Moves;
        Place(join);
    }

    private void Place(SplitRoomJoin join) => CellFor(join).Joins.Add(join);

    private void Unplace(SplitRoomJoin join) => CellFor(join).Joins.Remove(join);

    private SplitRoomCell CellFor(SplitRoomJoin join)
    {
        var (row, column) = Cell(join.Side);
        return (join.Moves ? _new : _original)[row * 3 + column];
    }

    /// <summary>The row and column (0 to 2) of a side around the room, the room itself in the middle.</summary>
    internal static (int Row, int Column) Cell(CompassPoint side) => side switch
    {
        CompassPoint.North => (0, 1),
        CompassPoint.NorthNorthEast or CompassPoint.NorthEast or CompassPoint.EastNorthEast => (0, 2),
        CompassPoint.East => (1, 2),
        CompassPoint.EastSouthEast or CompassPoint.SouthEast or CompassPoint.SouthSouthEast => (2, 2),
        CompassPoint.South => (2, 1),
        CompassPoint.SouthSouthWest or CompassPoint.SouthWest or CompassPoint.WestSouthWest => (2, 0),
        CompassPoint.West => (1, 0),
        _ => (0, 0)
    };

    private static SplitRoomCell[] NineCells()
    {
        var cells = new SplitRoomCell[9];
        for (var row = 0; row < 3; row++)
        for (var column = 0; column < 3; column++)
            cells[row * 3 + column] = new SplitRoomCell(row, column);
        return cells;
    }

    private void Ok()
    {
        var moving = Joins.Where(join => join.Moves).ToList();
        Complete(new RoomSplitChoice(moving.Select(join => join.Join).Distinct().ToList(), _where,
            moving.Select(join => join.Exit).ToList()));
    }

    private void Complete(RoomSplitChoice? result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
