using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Navigation;

/// <summary>
///   The directions panel: a route from one room to another (<see cref="RouteFinder" />). Navigate to Here from where
///   automap has the player follows them as they play: the steps taken drop off the top, and going another way
///   reroutes from wherever they are.
/// </summary>
public sealed class Directions
{
  private Room _followed;

  /// <summary>Where the directions start.</summary>
  public Room From { get; private set; }

  /// <summary>Where they lead.</summary>
  public Room To { get; private set; }

  /// <summary>The directions start where automap has the player, and follow them as they play.</summary>
  public bool FromWhereYouAre { get; private set; }

  /// <summary>The directions panel is showing.</summary>
  public bool IsOpen { get; private set; }

  /// <summary>The commands to type, in order.</summary>
  public IReadOnlyList<RouteStep> Steps { get; private set; } = Array.Empty<RouteStep>();

  /// <summary>Directions have been worked out (or found to be impossible) for the rooms chosen.</summary>
  public bool HasRoute { get; private set; }

  /// <summary>What the panel says: how many moves, arrived, rerouted, or that no way is known.</summary>
  public string Status { get; private set; } = string.Empty;

  /// <summary>Anything above changed.</summary>
  public event Action Changed;

  private static IEnumerable<Element> Elements => Project.Current?.Elements ?? Enumerable.Empty<Element>();

  private static bool OnTheMap(Room room) => room != null && Elements.Contains(room);

  public void Open()
  {
    IsOpen = true;
    if (!HasRoute) Status = Prompt();
    Raise();
  }

  public void Close()
  {
    IsOpen = false;
    FromWhereYouAre = false;
    Raise();
  }

  /// <summary>
  ///   Navigate to Here: directions to <paramref name="room" />, from where automap has the player
  ///   (<paramref name="whereYouAre" />, followed as they play) or, without automap, from the room chosen with Navigate
  ///   from Here.
  /// </summary>
  public void NavigateTo(Room room, Room whereYouAre)
  {
    To = room;
    IsOpen = true;
    if (whereYouAre != null)
    {
      From = whereYouAre;
      FromWhereYouAre = true;
      _followed = whereYouAre;
    }

    if (From != null) Go();
    else
    {
      HasRoute = false;
      Steps = Array.Empty<RouteStep>();
      Status = Prompt();
      Raise();
    }
  }

  /// <summary>Navigate from Here: the directions start at <paramref name="room" />.</summary>
  public void NavigateFrom(Room room)
  {
    From = room;
    FromWhereYouAre = false;
    IsOpen = true;
    HasRoute = false;
    Steps = Array.Empty<RouteStep>();
    Status = Prompt();
    Raise();
  }

  /// <summary>Start from where automap has the player, and follow them.</summary>
  public void FromHere(Room whereYouAre)
  {
    if (whereYouAre == null) return;
    From = whereYouAre;
    FromWhereYouAre = true;
    _followed = whereYouAre;
    if (To != null) Go();
    else
    {
      Status = Prompt();
      Raise();
    }
  }

  /// <summary>Directions back the other way.</summary>
  public void Swap()
  {
    (From, To) = (To, From);
    FromWhereYouAre = false;
    if (From != null && To != null) Go();
    else
    {
      HasRoute = false;
      Steps = Array.Empty<RouteStep>();
      Status = Prompt();
      Raise();
    }
  }

  /// <summary>Works the directions out.</summary>
  public void Go() => Route(null);

  /// <summary>
  ///   Stop: the directions go (and the route stops glowing), but From and To stay, so Go works them out again.
  ///   Following where automap has the player carries on, so From is still where they are.
  /// </summary>
  public void Stop()
  {
    HasRoute = false;
    Steps = Array.Empty<RouteStep>();
    Status = Prompt();
    Raise();
  }

  /// <summary>Whether there's what's needed to work out directions.</summary>
  public bool CanGo => OnTheMap(From) && OnTheMap(To);

  /// <summary>
  ///   Automap has the player in <paramref name="whereYouAre" /> now: steps taken drop off the top; somewhere off the
  ///   route, the directions start again from there ("rerouting"); at the end, they've arrived, and the directions
  ///   stop.
  /// </summary>
  public void Follow(Room whereYouAre)
  {
    if (!IsOpen || !FromWhereYouAre || To == null || whereYouAre == _followed) return;
    _followed = whereYouAre;
    if (whereYouAre == null)
    {
      Status = "Automap has lost track of where you are: the directions carry on when it finds you.";
      Raise();
      return;
    }

    var here = RouteFinder.Place(whereYouAre);
    From = whereYouAre;
    // stopped: no directions to follow, but From keeps up with where they are, ready for Go
    if (!HasRoute)
    {
      Status = Prompt();
      Raise();
      return;
    }

    if (here == RouteFinder.Place(To))
    {
      // arrived: the directions stop (the route stops glowing, Stop goes back to Go), as they would at Stop
      Steps = Array.Empty<RouteStep>();
      HasRoute = false;
      Status = $"You've arrived at {To.Name}.";
      Raise();
      return;
    }

    var reached = Steps.ToList().FindIndex(step => step.To == here);
    if (reached >= 0)
    {
      Steps = Steps.Skip(reached + 1).ToList();
      Status = Summary();
      Raise();
      return;
    }

    if (Steps.Count > 0 && Steps[0].From == here) return;
    Route($"Rerouting from {whereYouAre.Name}. ");
  }

  private void Route(string reason)
  {
    if (!CanGo)
    {
      HasRoute = false;
      Steps = Array.Empty<RouteStep>();
      Status = Prompt();
      Raise();
      return;
    }

    var route = RouteFinder.Find(Elements, From, To);
    // already there: nothing to follow, so nothing to stop
    HasRoute = route is not { Count: 0 };
    Steps = route ?? Array.Empty<RouteStep>();
    Status = (reason ?? string.Empty) + (route == null
      ? $"The map doesn't show a way from {From.Name} to {To.Name}."
      : route.Count == 0
        ? $"You're at {To.Name}."
        : Summary());
    Raise();
  }

  private string Summary()
  {
    var moves = Steps.Count == 1 ? "1 move" : $"{Steps.Count} moves";
    var assumed = Steps.Count(step => step.Assumed);
    var note = assumed == 0 ? string.Empty
      : assumed == 1 ? " One way is only assumed: nobody has gone it yet."
      : $" {assumed} ways are only assumed: nobody has gone them yet.";
    return $"{moves} to {To.Name}.{note}";
  }

  private string Prompt()
  {
    if (From != null && !OnTheMap(From)) return $"{From.Name} isn't on the map any more.";
    if (To != null && !OnTheMap(To)) return $"{To.Name} isn't on the map any more.";
    if (From == null && To == null)
      return "Right-click a room and choose Navigate to Here (or Navigate from Here) to get directions.";
    if (From == null) return "Where from? Right-click a room and choose Navigate from Here.";
    if (To == null) return "Where to? Right-click a room and choose Navigate to Here.";
    return "Press Go for directions.";
  }

  private void Raise() => Changed?.Invoke();
}
