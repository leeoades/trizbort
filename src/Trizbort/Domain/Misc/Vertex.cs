using System;
using Trizbort.Domain.Elements;

namespace Trizbort.Domain.Misc {
  /// <summary>
  ///   A vertex on a connection.
  /// </summary>
  /// <remarks>
  ///   Connections are multi-segment lines between vertices.
  ///   Each vertex is fixed either to a point in space or
  ///   to an element's port.
  /// </remarks>
  public class Vertex {
    private Port m_port;

    private Vector m_position;

    public Vertex() { }

    public Vertex(Port port) {
      Port = port;
    }

    public Vertex(Vector position) {
      Position = position;
    }

    public Connection Connection { get; set; }

    public Port Port {
      get => m_port;
      set {
        if (m_port != value) {
          m_position = Vector.Zero;
          m_port = value;
          RaiseChanged();
        }
      }
    }

    public Vector Position {
      get {
        if (m_port != null) return m_port.Position;
        return m_position;
      }
      set {
        if (m_position != value) {
          m_position = value;
          m_port = null;
          RaiseChanged();
        }
      }
    }

    public event EventHandler Changed;

    private void RaiseChanged() {
      var changed = Changed;
      if (changed != null) changed(this, EventArgs.Empty);
    }
  }
}