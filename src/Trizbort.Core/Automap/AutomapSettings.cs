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
        AssumeTwoWayConnections = true
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
}