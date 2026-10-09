using System.Windows.Forms;
using PdfSharp.Drawing;
using Trizbort.Domain.Elements;
using Trizbort.Setup;
using Trizbort.UI.Controls;

namespace Trizbort.Domain.Misc;

/// <summary>
///   A visual handle by which an element may be resized.
/// </summary>
internal class ResizeHandle {
  private readonly CompassPoint _compassPoint;
  private readonly ISizeable _owner;

  public ResizeHandle(CompassPoint compassPoint, ISizeable owner)
  {
    _compassPoint = compassPoint;
    _owner = owner;
  }

  public Cursor Cursor {
    get {
      switch (_compassPoint) {
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
      var pos = _owner.InnerBounds.GetCorner(_compassPoint);
      return pos;
    }
    set {
      SetX(value.X);
      SetY(value.Y);
    }
  }

  public Vector Position {
    get {
      var tBounds = _owner.InnerBounds;

      var pos = tBounds.GetCorner(_compassPoint);
      if (_owner is Room) pos = tBounds.GetCorner(_compassPoint, ((Room)_owner).Shape, ((Room)_owner).Corners);
      pos.X -= Size.X / 2;
      pos.Y -= Size.Y / 2;
      return pos;
    }
  }

  private Rect Bounds => new(Position, Size);

  private static Vector Size => new(Settings.HandleSize);

  public void Draw(Canvas canvas, XGraphics graphics, Palette palette, DrawingContext context)
  {
    Drawing.DrawHandle(canvas, graphics, palette, Bounds, context, false, false);
  }

  public bool HitTest(Vector pos)
  {
    return Bounds.Contains(pos);
  }

  private void SetX(float value)
  {
    switch (_compassPoint) {
      case CompassPoint.North:
      case CompassPoint.South:
        break;
      case CompassPoint.NorthWest:
      case CompassPoint.WestNorthWest:
      case CompassPoint.West:
      case CompassPoint.WestSouthWest:
      case CompassPoint.SouthWest:
      default:
        if (_owner.Width - (value - _owner.X) >= 1) {
          var old = _owner.X;
          _owner.Position = new Vector(value, _owner.Position.Y);
          _owner.Size = new Vector(_owner.Size.X - (_owner.X - old), _owner.Size.Y);
        }

        break;
      case CompassPoint.NorthEast:
      case CompassPoint.EastNorthEast:
      case CompassPoint.East:
      case CompassPoint.EastSouthEast:
      case CompassPoint.SouthEast:
        if (value - _owner.X >= 1) _owner.Size = new Vector(value - _owner.X, _owner.Size.Y);
        break;
    }
  }

  private void SetY(float value)
  {
    switch (_compassPoint) {
      case CompassPoint.East:
      case CompassPoint.West:
        break;
      case CompassPoint.NorthWest:
      case CompassPoint.NorthNorthWest:
      case CompassPoint.North:
      case CompassPoint.NorthNorthEast:
      case CompassPoint.NorthEast:
        if (_owner.Height - (value - _owner.Y) >= 1) {
          var old = _owner.Y;
          _owner.Position = new Vector(_owner.Position.X, value);
          _owner.Size = new Vector(_owner.Size.X, _owner.Size.Y - (_owner.Y - old));
        }

        break;
      case CompassPoint.SouthWest:
      case CompassPoint.SouthSouthWest:
      case CompassPoint.South:
      case CompassPoint.SouthSouthEast:
      case CompassPoint.SouthEast:
        if (value - _owner.Y >= 1) _owner.Size = new Vector(_owner.Size.X, value - _owner.Y);
        break;
    }
  }
}