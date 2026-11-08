namespace Trizbort.Automap;

public struct AutomapSettings
{
  public static AutomapSettings Default {
    get {
      var settings = new AutomapSettings {
        FileName = string.Empty,
        SingleStep = false,
        VerboseTranscript = true,
        AssumeRoomsWithSameNameAreSameRoom = false,
        GuessExits = true,
        AddObjectCommand = "tb see",
        AddRegionCommand = "tb region",
        ContinueTranscript = false,
        AssumeTwoWayConnections = true,
        AutoTidy = true,
        AbbreviateDirections = true,
        MoveByMove = false
      };
      return settings;
    }
  }

  public string FileName;
  public bool SingleStep;
  public bool VerboseTranscript;
  public bool AssumeRoomsWithSameNameAreSameRoom;
  public bool GuessExits;
  public string AddObjectCommand;
  public string AddRegionCommand;
  public bool ContinueTranscript;
  public bool AssumeTwoWayConnections;

  /// <summary>Rooms near the player rearrange themselves after each move so they don't get in each other's way.</summary>
  public bool AutoTidy;

  /// <summary>Label joins with short directions ("n / u / in") rather than whole words ("north / up / in").</summary>
  public bool AbbreviateDirections;

  /// <summary>
  ///   Play the transcript a move at a time: automap waits before each command until it is told to play the next move
  ///   (<see cref="Automap.PlayNextMove" />) or to play to the end (<see cref="Automap.PlayToEnd" />).
  /// </summary>
  public bool MoveByMove;
}