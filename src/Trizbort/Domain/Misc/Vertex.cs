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
    private Port _port;

    private Vector _position;

    public Vertex() { }

    public Vertex(Port port) {
      Port = port;
    }

    public Vertex(Vector position) {
      Position = position;
    }

    public Connection Connection { get; set; }

    public Port Port {
      get => _port;
      set {
        if (_port != value) {
          _position = Vector.Zero;
          _port = value;
          RaiseChanged();
        }
      }
    }

    public Vector Position {
      get {
        if (_port != null) return _port.Position;
        return _position;
      }
      set {
        if (_position != value) {
          _position = value;
          _port = null;
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