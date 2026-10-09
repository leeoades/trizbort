using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;

namespace Trizbort.Domain.Misc {
  internal static class MapEditing {
    public static void Move(IEnumerable<Element> elements, IList<Element> selection, Vector delta) {
      foreach (var element in selection) {
        if (element is IMoveable moveable) moveable.Position += delta;
        if (element is Connection connection) {
          foreach (var vertex in connection.VertexList)
            if (vertex.Port == null) vertex.Position += delta;
          connection.MoveCurveWaypointsBy(delta);
        }
      }
      MoveAttachedWaypoints(elements, selection, delta);
    }

    public static void MoveAttachedWaypoints(IEnumerable<Element> elements, IList<Element> selection, Vector delta) {
      if (delta == Vector.Zero) return;
      var nodes = new HashSet<Element>(selection.Where(element => element is ISizeable));
      foreach (var connection in elements.OfType<Connection>()) {
        if (!connection.HasCurveWaypoints || selection.Contains(connection)) continue;
        if (nodes.Contains(connection.VertexList[0].Port?.Owner) &&
            nodes.Contains(connection.VertexList[connection.VertexList.Count - 1].Port?.Owner))
          connection.MoveCurveWaypointsBy(delta);
      }
    }

    public static Vector Resize(ResizeHandle handle, Vector lastPosition, Vector position) {
      if (position == lastPosition) return lastPosition;
      var oldPosition = handle.OwnerPosition;
      handle.OwnerPosition = Setup.Settings.Snap(oldPosition + position - lastPosition);
      // Track applied movement, not cursor movement: snapping and minimum size may reject it.
      var applied = handle.OwnerPosition - oldPosition;
      if (applied.X != 0) lastPosition.X += applied.X;
      if (applied.Y != 0) lastPosition.Y += applied.Y;
      return lastPosition;
    }
  }
}
