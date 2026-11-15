using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Automap;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>What one answer to "what moved it?" is: a command typed in the room, nothing the player did, or other words.</summary>
public enum WhatMovedItKind
{
    Command,
    ByItself,
    Other
}

/// <summary>One answer in the list: "pull peg → second", "Nothing I did", "Something else".</summary>
public sealed record WhatMovedItOption(string Title, WhatMovedItKind Kind, string? Label = null);

/// <summary>
///     What moved it?: a room that moves has turned up somewhere new. The commands typed in it are listed (questions
///     and their answers as one: "pull peg → second"), newest last, with the suggestion picked; then "nothing I did" and
///     "something else". OK gives the choice; Cancel (or closing) gives null, and the join shows "?".
/// </summary>
public sealed partial class WhatMovedItViewModel : ViewModelBase
{
    public WhatMovedItViewModel(string roomName, string comeOutIn, IReadOnlyList<DoneInRoom> done, int suggested)
    {
        RoomName = roomName;
        ComeOutIn = comeOutIn;
        var options = done.Select(item => new WhatMovedItOption(item.Display, WhatMovedItKind.Command, item.Label)).ToList();
        options.Add(new WhatMovedItOption("Nothing I did (it moves by itself, or time passed)", WhatMovedItKind.ByItself));
        options.Add(new WhatMovedItOption("Something else...", WhatMovedItKind.Other));
        Options = options;
        _selected = suggested >= 0 && suggested < done.Count ? options[suggested] : options[done.Count];
        OkCommand = new RelayCommand(Ok);
        CancelCommand = new RelayCommand(() => Complete(null));
    }

    public string RoomName { get; }

    public string ComeOutIn { get; }

    public string IntroText => string.IsNullOrWhiteSpace(ComeOutIn)
        ? $"'{RoomName}' has moved. What moved it?"
        : $"'{RoomName}' has moved: you came out in '{ComeOutIn}'. What moved it?";

    public IReadOnlyList<WhatMovedItOption> Options { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AsksOther))]
    [NotifyPropertyChangedFor(nameof(Explanation))]
    private WhatMovedItOption _selected;

    /// <summary>"Something else" is picked: the words for it are typed in.</summary>
    public bool AsksOther => Selected?.Kind == WhatMovedItKind.Other;

    [ObservableProperty]
    private string _otherText = string.Empty;

    public string Explanation => Selected?.Kind switch
    {
        WhatMovedItKind.ByItself =>
            $"The join between the stops is labelled '{Trizbort.Automap.Automap.ByItself}', and automap never follows it. It still works out where '{RoomName}' is from the way you go out.",
        WhatMovedItKind.Other =>
            "Type the command that moved it. The join is labelled with it, and automap follows the join when you type it.",
        _ =>
            "The join between the stops is labelled with it, and when you type it again automap moves you to the other stop straight away."
    };

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public WhatMovedItChoice? Result { get; private set; }

    public event EventHandler? CloseRequested;

    private void Ok()
    {
        switch (Selected?.Kind)
        {
            case WhatMovedItKind.ByItself:
                Complete(WhatMovedItChoice.Itself);
                break;
            case WhatMovedItKind.Other:
                Complete(string.IsNullOrWhiteSpace(OtherText) ? null : new WhatMovedItChoice(OtherText.Trim()));
                break;
            case WhatMovedItKind.Command:
                Complete(new WhatMovedItChoice(Selected.Label!));
                break;
            default:
                Complete(null);
                break;
        }
    }

    private void Complete(WhatMovedItChoice? result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
