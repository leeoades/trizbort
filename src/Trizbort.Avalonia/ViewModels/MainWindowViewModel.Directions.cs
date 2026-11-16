using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Elements;
using Trizbort.Navigation;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>One step in the directions panel: "3. north", and where it leads.</summary>
public sealed record DirectionStepItem(int Number, string Command, Room To, bool Assumed)
{
    public string Text => $"{Number}. {Command}";

    public string Where => "to " + To.Name + (Assumed ? " (assumed: nobody has gone this way yet)" : string.Empty);
}

public partial class MainWindowViewModel
{
    private Directions? Directions => _commandService.Session?.Directions;
    private bool _directionsHooked;
    private bool _canGoDirections;
    private bool _showingDirections;
    private bool _canNavigateFromWhereYouAre;

    /// <summary>The directions panel is showing.</summary>
    [ObservableProperty]
    private bool _isDirectionsOpen;

    /// <summary>Where the directions start ("Where you are: Hall" while following automap).</summary>
    [ObservableProperty]
    private string _directionsFrom = string.Empty;

    /// <summary>Where they lead.</summary>
    [ObservableProperty]
    private string _directionsTo = string.Empty;

    /// <summary>How many moves, arrived, rerouting, or that no way is known.</summary>
    [ObservableProperty]
    private string _directionsStatus = string.Empty;

    /// <summary>There are commands to copy.</summary>
    [ObservableProperty]
    private bool _hasDirectionsSteps;

    /// <summary>The commands to type, in order.</summary>
    public ObservableCollection<DirectionStepItem> DirectionsSteps { get; } = new();

    /// <summary>Go: there's a room to start from and one to go to; Stop: there are directions showing.</summary>
    public bool CanGoDirections => _canGoDirections || _showingDirections;

    /// <summary>Directions are showing, so the Go button stops them.</summary>
    public bool IsShowingDirections => _showingDirections;

    /// <summary>"Go", or "Stop" while directions are showing.</summary>
    public string GoDirectionsText => _showingDirections ? "Stop" : "Go";

    /// <summary>What the Go button does.</summary>
    public string GoDirectionsTip => _showingDirections
        ? "Clear the directions; From and To stay, so Go works them out again"
        : "Work out the directions";

    /// <summary>From Where You Are: automap knows which room the player is in.</summary>
    public bool CanNavigateFromWhereYouAre => _canNavigateFromWhereYouAre;

    /// <summary>The commands, one a line, for Copy Commands.</summary>
    public string DirectionsText =>
        string.Join(Environment.NewLine, Directions?.Steps.Select(step => step.Command) ?? Enumerable.Empty<string>());

    /// <summary>Listens to the session's directions, so the panel shows what they are.</summary>
    private void HookDirections()
    {
        if (_directionsHooked || Directions == null) return;
        _directionsHooked = true;
        Directions.Changed += () => RunOnUiThread(RefreshDirections);
        RefreshDirections();
    }

    /// <summary>Brings the directions panel up to date.</summary>
    internal void RefreshDirections()
    {
        var directions = Directions;
        if (directions == null) return;
        IsDirectionsOpen = directions.IsOpen;
        DirectionsFrom = directions.From == null ? "(not chosen)"
            : directions.FromWhereYouAre ? $"Where you are: {directions.From.Name}"
            : directions.From.Name;
        DirectionsTo = directions.To?.Name ?? "(not chosen)";
        DirectionsStatus = directions.Status;
        DirectionsSteps.Clear();
        var number = 1;
        foreach (var step in directions.Steps)
            DirectionsSteps.Add(new DirectionStepItem(number++, step.Command, step.To, step.Assumed));
        HasDirectionsSteps = DirectionsSteps.Count > 0;
        // the route glows blue on the map while the directions are open
        var glow = directions.IsOpen && directions.Steps.Count > 0 ? directions.Steps : null;
        if (!ReferenceEquals(glow, Trizbort.Avalonia.Drawing.RouteGlow.Route))
        {
            Trizbort.Avalonia.Drawing.RouteGlow.Route = glow;
            _commandService.InvalidateCanvas();
        }
        if (_canGoDirections != directions.CanGo || _showingDirections != directions.HasRoute)
        {
            _canGoDirections = directions.CanGo;
            _showingDirections = directions.HasRoute;
            OnPropertyChanged(nameof(CanGoDirections));
            OnPropertyChanged(nameof(IsShowingDirections));
            OnPropertyChanged(nameof(GoDirectionsText));
            OnPropertyChanged(nameof(GoDirectionsTip));
            GoDirectionsCommand.NotifyCanExecuteChanged();
        }

        RefreshDirectionsWhereYouAre();
    }

    /// <summary>While automap runs: follow the player, and whether From Where You Are can be used.</summary>
    private void FollowDirections(Room? whereYouAre)
    {
        Directions?.Follow(whereYouAre);
        RefreshDirectionsWhereYouAre();
    }

    private void RefreshDirectionsWhereYouAre()
    {
        var can = IsAutomapActive && AutomapEngine.Instance.CurrentRoom != null;
        if (can == _canNavigateFromWhereYouAre) return;
        _canNavigateFromWhereYouAre = can;
        OnPropertyChanged(nameof(CanNavigateFromWhereYouAre));
        DirectionsFromWhereYouAreCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Go: works the directions out. Stop, while they're showing: clears them, keeping From and To.</summary>
    [RelayCommand(CanExecute = nameof(CanGoDirections))]
    public void GoDirections()
    {
        if (Directions == null) return;
        if (Directions.HasRoute) Directions.Stop();
        else Directions.Go();
    }

    /// <summary>Swap: directions back the other way.</summary>
    [RelayCommand]
    public void SwapDirections() => Directions?.Swap();

    /// <summary>Close the directions panel.</summary>
    [RelayCommand]
    public void CloseDirections() => Directions?.Close();

    /// <summary>From Where You Are: start from the room automap has the player in, and follow them.</summary>
    [RelayCommand(CanExecute = nameof(CanNavigateFromWhereYouAre))]
    public void DirectionsFromWhereYouAre() => Directions?.FromHere(AutomapEngine.Instance.CurrentRoom);

    /// <summary>Clicking a step: select the room it leads to, and glide the map there.</summary>
    public void ShowDirectionStep(DirectionStepItem step)
    {
        var session = _commandService.Session;
        if (session == null || !session.Project.Elements.Contains(step.To)) return;
        session.Selection.Set(new System.Collections.Generic.List<Element> { step.To });
        session.CenterOnElement(step.To);
        _commandService.InvalidateCanvas();
    }
}
