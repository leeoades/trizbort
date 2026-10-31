using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

public sealed partial class EditorSession
{
  /// <summary>
  ///   Call after a map file has become the current project (File > Open, a recent map, the map reopened at start up,
  ///   a reload, a URL). Like the reference's Canvas.OnProjectChanged: starts a fresh history, forgets the line style,
  ///   flow and label chosen for new connections, and zooms to fit the map (and keeps fitting while the window is
  ///   still being sized, until the user changes the view).
  /// </summary>
  public void OnMapLoaded()
  {
    Undo.Clear();
    ResetNewConnectionDefaults();
    Viewport.FitToMapAndKeepFitting(() => Project.Elements);
  }

  /// <summary>Plain two-way unlabelled solid lines for new connections (the reference's Canvas.Reset).</summary>
  public void ResetNewConnectionDefaults()
  {
    NewConnection.Style = ConnectionStyle.Solid;
    NewConnection.Flow = ConnectionFlow.TwoWay;
    NewConnection.Label = ConnectionLabel.None;
  }
}
