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
    private Port _mPort;

    private Vector _mPosition;

    public Vertex() { }

    public Vertex(Port port) {
      Port = port;
    }

    public Vertex(Vector position) {
      Position = position;
    }

    public Connection Connection { get; set; }

    public Port Port {
      get => _mPort;
      set {
        if (_mPort != value) {
          _mPosition = Vector.Zero;
          _mPort = value;
          raiseChanged();
        }
      }
    }

    public Vector Position {
      get {
        if (_mPort != null) return _mPort.Position;
        return _mPosition;
      }
      set {
        if (_mPosition != value) {
          _mPosition = value;
          _mPort = null;
          raiseChanged();
        }
      }
    }

    public event EventHandler Changed;

    private void raiseChanged() {
      var changed = Changed;
      if (changed != null) changed(this, EventArgs.Empty);
    }
  }
}