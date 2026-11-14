using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>One branch of the map in the list: "Living Room and 9 more rooms", ticked to go along.</summary>
public sealed partial class AreaBranchItem : ObservableObject
{
    public AreaBranchItem(AreaBranch branch)
    {
        Branch = branch;
    }

    public AreaBranch Branch { get; }

    public string Description => Branch.Description + " (off " + Branch.Via.Name + ")";

    /// <summary>The rooms in the branch, for the tooltip.</summary>
    public string RoomNames => string.Join(", ", Branch.Rooms.Select(room => room.Name).Distinct().OrderBy(name => name));

    [ObservableProperty]
    private bool _isChecked = true;
}

/// <summary>
///     Move to a New Area (or Bring 'X' to 'Y'): the branches of the map hanging off the rooms being moved, each
///     ticked to go along too. Ticking or unticking shows on the map which rooms will move. OK gives the ticked
///     branches; Cancel (or closing) gives null, and nothing moves.
/// </summary>
public sealed partial class AreaBranchesViewModel : ViewModelBase
{
    public AreaBranchesViewModel(string title, IReadOnlyList<Room> moving, IReadOnlyList<AreaBranch> branches)
    {
        Title = title;
        Moving = moving;
        Branches = branches.Select(branch => new AreaBranchItem(branch)).ToList();
        foreach (var item in Branches)
        {
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(AreaBranchItem.IsChecked)) return;
                OnPropertyChanged(nameof(Summary));
                Preview?.Invoke(RoomsToMove());
            };
        }

        OkCommand = new RelayCommand(() => Complete(Branches.Where(item => item.IsChecked).Select(item => item.Branch).ToList()));
        CancelCommand = new RelayCommand(() => Complete(null));
    }

    public string Title { get; }

    public IReadOnlyList<Room> Moving { get; }

    public IReadOnlyList<AreaBranchItem> Branches { get; }

    public string IntroText =>
        (Branches.Count == 1 ? "This part of the map is" : "These parts of the map are") +
        " joined to the rest only through the rooms you're moving. Ticked ones go too, keeping their layout; " +
        "joins to rooms left behind are split.";

    /// <summary>"12 rooms will move": highlighted on the map.</summary>
    public string Summary
    {
        get
        {
            var count = RoomsToMove().Count;
            return count == 1 ? "1 room will move (highlighted on the map)." : count + " rooms will move (highlighted on the map).";
        }
    }

    /// <summary>Shows the rooms that would move on the map behind the dialog.</summary>
    public Action<IReadOnlyList<Room>>? Preview { get; set; }

    /// <summary>The rooms being moved, and those of the ticked branches.</summary>
    public IReadOnlyList<Room> RoomsToMove() =>
        Moving.Concat(Branches.Where(item => item.IsChecked).SelectMany(item => item.Branch.Rooms)).Distinct().ToList();

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public IReadOnlyList<AreaBranch>? Result { get; private set; }

    public event EventHandler? CloseRequested;

    private void Complete(IReadOnlyList<AreaBranch>? result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
