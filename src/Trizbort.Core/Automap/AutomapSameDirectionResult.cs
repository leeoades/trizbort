namespace Trizbort.Automap;

public enum AutomapSameDirectionResult
{
  KeepRoom1,
  KeepRoom2,
  KeepBoth,

  /// <summary>
  ///   The room the player left is two places that look alike (a lift, a moving room): make it two rooms, the second
  ///   reached from the first by what the player did in it, and the new room joined from the second.
  /// </summary>
  Split,

  /// <summary>Map nothing for this move (the question was cancelled): the next room is joined to nothing.</summary>
  Cancel,

  /// <summary>
  ///   The room the player left moves (a lift, a car, a train): it's at another stop now. It's marked as moving, a copy
  ///   of it (with its name) stands for it at the new stop, joined from it by what moved it, and the new room is joined
  ///   from the copy. From then on automap works out which stop the player is at from the way out.
  /// </summary>
  Moves,

  /// <summary>
  ///   The room the player left isn't the one automap took it for, but another with its name: make that room ("Maze
  ///   (2)"), move the way the player came into it over to it, and join the new room from it.
  /// </summary>
  DifferentRoom,

  /// <summary>
  ///   The two rooms are one place under different circumstances (the shaft bottom, then the closet once it has come
  ///   down into it): keep both joins, and put the rooms side by side so they read as a pair.
  /// </summary>
  SamePlace
}
