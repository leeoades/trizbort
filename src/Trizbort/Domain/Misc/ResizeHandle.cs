using System.Windows.Forms;
using PdfSharp.Drawing;
using Trizbort.Domain.Elements;
using Trizbort.Setup;
using Trizbort.UI.Controls;

namespace Trizbort.Domain.Misc {
  /// <summary>
  ///   A visual handle by which an element may be resized.
  /// </summary>
  internal class ResizeHandle {
    private readonly CompassPoint _mCompassPoint;
    private readonly ISizeable _mOwner;

    public ResizeHandle(CompassPoint compassPoint, ISizeable owner) {
      _mCompassPoint = compassPoint;
      _mOwner = owner;
    }

    public Cursor Cursor {
      get {
        switch (_mCompassPoint) {
          case CompassPoint.NorthWest:
          case CompassPoint.SouthEast:
            return Cursors.SizeNWSE;
          case CompassPoint.NorthEast:
          case CompassPoint.SouthWest:
            return Cursors.SizeNESW;
          case CompassPoint.North:
          case CompassPoint.South:
            return Cursors.SizeNS;
          case CompassPoint.East:
          case CompassPoint.West:
            return Cursors.SizeWE;
          default:
            return null;
        }
      }
    }

    public Vector OwnerPosition {
      get {
        var pos = _mOwner.InnerBounds.GetCorner(_mCompassPoint);
        return pos;
      }
      set {
        setX(value.X);
        setY(value.Y);
      }
    }

    public Vector Position {
      get {
        var tBounds = _mOwner.InnerBounds;

        var pos = tBounds.GetCorner(_mCompassPoint);
        if (_mOwner is Room) pos = tBounds.GetCorner(_mCompassPoint, ((Room) _mOwner).Shape, ((Room) _mOwner).Corners);
        pos.X -= Size.X / 2;
        pos.Y -= Size.Y / 2;
        return pos;
      }
    }

    private Rect Bounds => new Rect(Position, Size);

    private static Vector Size => new Vector(Settings.HandleSize);

    public void Draw(Canvas canvas, XGraphics graphics, Palette palette, DrawingContext context) {
      Drawing.DrawHandle(canvas, graphics, palette, Bounds, context, false, false);
    }

    public bool HitTest(Vector pos) {
      return Bounds.Contains(pos);
    }

    private void setX(float value) {
      switch (_mCompassPoint) {
        case CompassPoint.North:
        case CompassPoint.South:
          break;
        case CompassPoint.NorthWest:
        case CompassPoint.WestNorthWest:
        case CompassPoint.West:
        case CompassPoint.WestSouthWest:
        case CompassPoint.SouthWest:
        default:
          if (_mOwner.Width - (value - _mOwner.X) >= 1) {
            var old = _mOwner.X;
            _mOwner.Position = new Vector(value, _mOwner.Position.Y);
            _mOwner.Size = new Vector(_mOwner.Size.X - (_mOwner.X - old), _mOwner.Size.Y);
          }

          break;
        case CompassPoint.NorthEast:
        case CompassPoint.EastNorthEast:
        case CompassPoint.East:
        case CompassPoint.EastSouthEast:
        case CompassPoint.SouthEast:
          if (value - _mOwner.X >= 1) _mOwner.Size = new Vector(value - _mOwner.X, _mOwner.Size.Y);
          break;
      }
    }

    private void setY(float value) {
      switch (_mCompassPoint) {
        case CompassPoint.East:
        case CompassPoint.West:
          break;
        case CompassPoint.NorthWest:
        case CompassPoint.NorthNorthWest:
        case CompassPoint.North:
        case CompassPoint.NorthNorthEast:
        case CompassPoint.NorthEast:
          if (_mOwner.Height - (value - _mOwner.Y) >= 1) {
            var old = _mOwner.Y;
            _mOwner.Position = new Vector(_mOwner.Position.X, value);
            _mOwner.Size = new Vector(_mOwner.Size.X, _mOwner.Size.Y - (_mOwner.Y - old));
          }

          break;
        case CompassPoint.SouthWest:
        case CompassPoint.SouthSouthWest:
        case CompassPoint.South:
        case CompassPoint.SouthSouthEast:
        case CompassPoint.SouthEast:
          if (value - _mOwner.Y >= 1) _mOwner.Size = new Vector(_mOwner.Size.X, value - _mOwner.Y);
          break;
      }
    }
  }
}