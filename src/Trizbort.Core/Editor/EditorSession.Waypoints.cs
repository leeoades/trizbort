using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

// The selected curve waypoint of the single selected connection (ticket 064).
public sealed partial class EditorSession
{
  private CurveWaypoint? _selectedWaypoint;

  /// <summary>
  ///   The curve waypoint last dragged (or pressed while set) on the selected connection; Delete removes it before
  ///   removing elements. Cleared when the selection changes or an undo/redo happens.
  /// </summary>
  public CurveWaypoint? SelectedWaypoint
  {
    get => _selectedWaypoint;
    set
    {
      if (_selectedWaypoint == value) return;
      _selectedWaypoint = value;
      RaiseChanged(EditorChange.Selection);
    }
  }

  /// <summary>Remove the selected waypoint as one undo step. Returns false (doing nothing) when there is none to remove.</summary>
  public bool DeleteSelectedWaypoint()
  {
    if (!_selectedWaypoint.HasValue) return false;
    if (Selection.Count != 1 || Selection.Primary is not Connection connection) return false;

    var waypoint = _selectedWaypoint.Value;
    if (connection.GetCurveWaypoint(waypoint) == null) return false;

    Execute("Delete waypoint", () => connection.RemoveCurveWaypoint(waypoint), new Element[] { connection });
    _selectedWaypoint = null;
    RaiseChanged(EditorChange.Model);
    return true;
  }
}
