using CommunityToolkit.Mvvm.Input;
using Trizbort.Automap;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///     The automap question when a way already seen leading to one room now leads to another ("East from Hall now leads
///     to Kitchen, and led to Pantry before"). Both are real, so the first answer keeps both; the others say the room
///     the player left moves (a lift) or was another room with its name (a maze). Closing the window keeps both.
/// </summary>
public class AutomapSameWayViewModel : ViewModelBase
{
    public AutomapSameWayViewModel(string? from, string? way, string? before, string? now)
    {
        From = from ?? string.Empty;
        Way = string.IsNullOrWhiteSpace(way) ? "That way" : char.ToUpperInvariant(way[0]) + way[1..];
        Before = before ?? string.Empty;
        Now = now ?? string.Empty;
        BothRightCommand = new RelayCommand(() => Complete(AutomapSameDirectionResult.SamePlace));
        MovesCommand = new RelayCommand(() => Complete(AutomapSameDirectionResult.Moves), () => CanSayWhichRoom);
        DifferentRoomCommand =
            new RelayCommand(() => Complete(AutomapSameDirectionResult.DifferentRoom), () => CanSayWhichRoom);
    }

    /// <summary>The room the player left ("Hall"); empty when automap doesn't know.</summary>
    public string From { get; }

    /// <summary>The way the player went ("East", "Enter closet").</summary>
    public string Way { get; }

    /// <summary>The room the way was seen leading to before ("Pantry").</summary>
    public string Before { get; }

    /// <summary>The room it leads to now ("Kitchen").</summary>
    public string Now { get; }

    public string Message => string.IsNullOrEmpty(From)
        ? $"{Way} now leads to {Now}, and it led to {Before} before."
        : $"{Way} from {From} now leads to {Now}, and it led to {Before} before.";

    public string BothRightTitle => "Both Are Right";

    public string BothRightText =>
        $"The way leads to either, depending on something in the game, or they're one place at different times. Both " +
        $"are kept, with '{Now}' beside '{Before}'.";

    /// <summary>The room the player left is known, so it can be the one that moves, or another by its name.</summary>
    public bool CanSayWhichRoom => !string.IsNullOrEmpty(From);

    public string MovesTitle => $"'{From}' Moves";

    public string MovesText =>
        $"'{From}' is a lift or a vehicle that took you with it. Automap adds a stop of it here, joined by what moved " +
        "it (it asks you what), and works out which stop you're at from then on.";

    public string DifferentTitle => $"A Different '{From}'";

    public string DifferentText =>
        $"You weren't in the '{From}' on the map, but another room with its name. Automap adds '{From} (2)', moves the " +
        $"way you came in over to it, and joins '{Now}' to it.";

    public RelayCommand BothRightCommand { get; }

    public RelayCommand MovesCommand { get; }

    public RelayCommand DifferentRoomCommand { get; }

    /// <summary>What was chosen; both kept until then (closing the window keeps both too).</summary>
    public AutomapSameDirectionResult Result { get; private set; } = AutomapSameDirectionResult.SamePlace;

    public event EventHandler? CloseRequested;

    private void Complete(AutomapSameDirectionResult result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
