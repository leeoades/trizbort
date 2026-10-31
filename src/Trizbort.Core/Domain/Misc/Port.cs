using Trizbort.Domain.Elements;
using Trizbort.Setup;

namespace Trizbort.Domain.Misc;

/// <summary>
///   A docking point at which a connection's vertex may join to an element.
/// </summary>
public class Port
{
  protected Port(Element owner)
  {
    Owner = owner;
  }

  public virtual bool HasStalk => StalkPosition != Position;

  /// <summary>
  ///   Get the unique identifier for this port amongst all of the owner's ports.
  /// </summary>
  public virtual string Id { get; }

  public Element Owner { get; }

  public virtual Vector Position => Owner.GetPortPosition(this);
  public virtual Vector StalkPosition => Owner.GetPortStalkPosition(this);
  public virtual float X => Position.X;
  public virtual float Y => Position.Y;
  public static Vector Size => new(Settings.HandleSize);
  public Rect VisualBounds => new(VisualPosition - Size / 2, Size);
  public Vector VisualPosition => Position;

  public float Distance(Vector pos)
  {
    return pos.DistanceFromRect(VisualBounds);
  }
}
