using System;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;

namespace Trizbort.Automap;

/// <summary>
///   What the editor needs from the automap engine while a run is in progress: its state, the single-step keys and
///   its events. <see cref="Automap" /> implements it; tests substitute a fake.
/// </summary>
public interface IAutomapController
{
  bool IsRunning { get; }

  /// <summary>True while the current run waits for <see cref="Step" /> between commands (debug only).</summary>
  bool SingleStep { get; }

  string Status { get; }

  void Step();

  void RunToCompletion();

  /// <summary>True while the run plays the transcript a move at a time, waiting before each command.</summary>
  bool MoveByMove { get; }

  /// <summary>Play the transcript's next move, then pause before the one after.</summary>
  void PlayNextMove();

  /// <summary>Go back a move in the transcript and wait there, leaving the map as it is.</summary>
  void GoBack();

  /// <summary>Pause before the next move (playing the transcript a move at a time from then on).</summary>
  void Pause()
  {
  }

  /// <summary>How many moves are left to play in the transcript, counting the one automap is paused before.</summary>
  int MovesAhead() => 0;

  /// <summary>The room automap thinks the player is in, if it knows; null otherwise.</summary>
  Room CurrentRoom => null;

  /// <summary>True while mapping is suspended (automap follows the player but doesn't change the map).</summary>
  bool Suspended => false;

  /// <summary>Tells automap the player is in <paramref name="room" />: it maps on from there.</summary>
  void SetCurrentRoom(Room room)
  {
  }

  /// <summary>
  ///   Paused move by move before a command the game wrote while automap waited at the end of the transcript: the game
  ///   is being played now (Play to the End follows it live).
  /// </summary>
  bool GameWentOn => false;

  /// <summary>Whether there's a move to play after where automap is in the transcript.</summary>
  bool HasNextMove => true;

  /// <summary>Whether there's a move to go back to.</summary>
  bool CanGoBack => true;

  /// <summary>
  ///   Where automap is in the transcript: the moves (commands typed) played so far, and the moves in the transcript
  ///   so far. Cheap enough to call on a timer.
  /// </summary>
  (int Played, int Total) MoveCount() => (0, 0);

  /// <summary>Whether the room automap has the player in can be split off as a different room: the way in is known.</summary>
  bool CanSplitCurrentRoom => false;

  /// <summary>
  ///   "This is a different room": the room automap has the player in is another room with that name. Makes it, moves
  ///   the way in over to it and carries on from there.
  /// </summary>
  Task<bool> SplitCurrentRoomAsync() => Task.FromResult(false);

  /// <summary>Stop mapping for now, following the player through rooms already on the map.</summary>
  void Suspend()
  {
  }

  /// <summary>Map again after <see cref="Suspend" />.</summary>
  void Resume()
  {
  }

  /// <summary>Raised with the new status text each time it is set. May be raised off the UI thread.</summary>
  event Action<string> StatusChanged;

  /// <summary>Raised when a run stops because of an error. May be raised off the UI thread.</summary>
  event Action<Exception> AutomapFailed;
}
