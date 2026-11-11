using CommunityToolkit.Mvvm.Input;
using Trizbort.Automap;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>One answer in the "same direction" question: what it's called, and what it does.</summary>
public sealed record SameDirectionOption(string Title, string Explanation, AutomapSameDirectionResult Result);

/// <summary>
///     The "same direction" automap question: a way out already mapped now leads somewhere new. The answers are listed
///     on the left; the one picked is explained on the right. OK takes it; Cancel (or closing) maps nothing for this
///     move.
/// </summary>
public class AutomapSameDirectionViewModel : ViewModelBase
{
    private SameDirectionOption? _selected;

    public AutomapSameDirectionViewModel(string? existingName, string? newName)
    {
        ExistingName = existingName ?? string.Empty;
        NewName = newName ?? string.Empty;
        OkCommand = new RelayCommand(Ok, () => _selected != null);
        CancelCommand = new RelayCommand(() => Complete(AutomapSameDirectionResult.Cancel));
    }

    public AutomapSameDirectionViewModel(Room existing, string newName)
        : this(existing?.Name, newName)
    {
    }

    public string ExistingName { get; }

    public string NewName { get; }

    public string Message =>
        $"Room '{ExistingName}' is already defined in the same direction as room '{NewName}'.  What would you like to do?";

    public string KeepExistingLabel => $"Keep '{ExistingName}'";

    public string KeepNewLabel => $"Keep '{NewName}'";

    public string KeepBothLabel => "Keep Both";

    /// <summary>The room the player left, when known: it may be another room with its name, or move (a lift).</summary>
    public string FromName { get; init; } = string.Empty;

    public bool CanSplit => !string.IsNullOrEmpty(FromName);

    public string DifferentRoomLabel => $"This is a different '{FromName}'";

    public string MovesLabel => $"'{FromName}' moves (a lift, a vehicle)";

    /// <summary>The answers, in order; the last two only when the room the player left is known.</summary>
    public IReadOnlyList<SameDirectionOption> Options => _options ??= BuildOptions();

    private IReadOnlyList<SameDirectionOption>? _options;

    private IReadOnlyList<SameDirectionOption> BuildOptions()
    {
        var options = new List<SameDirectionOption>
        {
            new(KeepExistingLabel,
                $"This way still leads to '{ExistingName}'. Keep it as it is, and don't map '{NewName}' along it.",
                AutomapSameDirectionResult.KeepRoom1),
            new(KeepNewLabel,
                $"This way leads to '{NewName}', not '{ExistingName}': map '{NewName}' there in its place.",
                AutomapSameDirectionResult.KeepRoom2),
            new(KeepBothLabel,
                $"This way can lead to either room, depending on something in the game. Keep both joins: from then on " +
                "automap goes by the room the game shows.",
                AutomapSameDirectionResult.KeepBoth),
            new($"The same place at different times",
                $"'{ExistingName}' and '{NewName}' are one place under different circumstances (the bottom of a shaft, " +
                "then the closet once it has come down into it). Keep both joins this way, and put the two rooms side " +
                "by side so they read as a pair. From then on automap goes by the room the game shows.",
                AutomapSameDirectionResult.SamePlace)
        };
        if (CanSplit)
        {
            var second = $"{FromName} (2)";
            options.Add(new(DifferentRoomLabel,
                $"You weren't in the '{FromName}' on the map, but another room with its name. Automap adds '{second}', " +
                $"moves the way you came in over to it, and joins '{NewName}' to it.",
                AutomapSameDirectionResult.DifferentRoom));
            options.Add(new(MovesLabel,
                $"'{FromName}' takes you with it, so its ways out lead to different places. Automap adds a copy of " +
                $"'{FromName}' here, joined to it by what moved it (it asks you what), and from then on works out " +
                "which place it's at from the way you get out.",
                AutomapSameDirectionResult.Moves));
        }

        return options;
    }

    public SameDirectionOption? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                OnPropertyChanged(nameof(Explanation));
                OkCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Explanation => _selected?.Explanation ?? string.Empty;

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    /// <summary>What was chosen; Cancel (map nothing for this move) until then.</summary>
    public AutomapSameDirectionResult Result { get; private set; } = AutomapSameDirectionResult.Cancel;

    public event EventHandler? CloseRequested;

    private void Ok()
    {
        if (_selected == null) return;
        Complete(_selected.Result);
    }

    public void KeepExisting() => Complete(AutomapSameDirectionResult.KeepRoom1);

    public void KeepNew() => Complete(AutomapSameDirectionResult.KeepRoom2);

    public void KeepBoth() => Complete(AutomapSameDirectionResult.KeepBoth);

    public void Split() => Complete(AutomapSameDirectionResult.Split);

    public void Moves() => Complete(AutomapSameDirectionResult.Moves);

    public void DifferentRoom() => Complete(AutomapSameDirectionResult.DifferentRoom);

    private void Complete(AutomapSameDirectionResult result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
