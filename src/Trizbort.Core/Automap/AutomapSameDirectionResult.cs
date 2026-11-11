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
  Cancel,

  /// <summary>
  ///   The room the player left moves (a lift, a car, a train): it's at another stop now. It's marked as moving, a copy
  ///   of it (with its name) stands for it at the new stop, joined from it by what moved it, and the new room is joined
  ///   from the copy. From then on automap works out which stop the player is at from the way out.
  /// </summary>
  Moves
}
