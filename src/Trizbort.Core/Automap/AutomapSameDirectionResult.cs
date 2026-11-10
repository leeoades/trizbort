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
  Cancel
}
