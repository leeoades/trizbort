using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///     State and outcome for the "which room does the transcript mean?" prompt. The result defaults to
///     <see cref="RoomDisambiguation.CreateNew" />, which is what closing the window gives.
/// </summary>
public class DisambiguateRoomsViewModel : ViewModelBase
{
    private Room? _selectedRoom;

    public DisambiguateRoomsViewModel(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates)
    {
        Candidates = candidates;
        ContextText = BuildContext(transcriptLine, roomDescription);
        ThisRoomCommand = new RelayCommand(ChooseThisRoom, () => _selectedRoom != null);
        NewRoomCommand = new RelayCommand(ChooseNewRoom);
        AutoChooseRestCommand = new RelayCommand(ChooseAutoRest);
        if (candidates.Count == 1)
        {
            _selectedRoom = candidates[0];
        }
    }

    public string IntroText =>
        "Trizbort isn't sure which room the transcript is referring to." + "\n\n" +
        "Either several rooms have the same name but different descriptions, " +
        "or the room description has changed during play.";

    public IReadOnlyList<Room> Candidates { get; }

    public string ContextText { get; }

    public Room? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (SetProperty(ref _selectedRoom, value))
            {
                OnPropertyChanged(nameof(SelectedDescription));
                OnPropertyChanged(nameof(CanChooseThisRoom));
                ThisRoomCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedDescription => _selectedRoom?.PrimaryDescription ?? string.Empty;

    public bool CanChooseThisRoom => _selectedRoom != null;

    public RelayCommand ThisRoomCommand { get; }

    public RelayCommand NewRoomCommand { get; }

    public RelayCommand AutoChooseRestCommand { get; }

    public RoomDisambiguation Result { get; private set; } = RoomDisambiguation.CreateNew();

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

    private void ChooseThisRoom()
    {
        if (_selectedRoom == null)
        {
            return;
        }

        Complete(RoomDisambiguation.Choose(_selectedRoom));
    }

    private void ChooseNewRoom() => Complete(RoomDisambiguation.CreateNew());

    private void ChooseAutoRest()
    {
        if (Candidates.Count == 0)
        {
            Complete(RoomDisambiguation.CreateNew());
            return;
        }

        Complete(RoomDisambiguation.ChooseFirstAndStopAsking(Candidates[0]));
    }

    private void Complete(RoomDisambiguation result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
