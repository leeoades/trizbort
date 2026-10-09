using Trizbort.Domain.Elements;

namespace Trizbort.Domain.Misc;

public abstract class MoveablePort : Port
{
  protected MoveablePort(Element owner) : base(owner)
  {
  }

  public abstract Port DockedAt { get; }
  public abstract void DockAt(Port port);

  public abstract void SetPosition(Vector pos);
}