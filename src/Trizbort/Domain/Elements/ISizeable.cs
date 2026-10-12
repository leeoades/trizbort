using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

internal interface ISizeable : IMoveable
{
  Vector Size { get; set; }
  float Width { get; }
  float Height { get; }
  Rect InnerBounds { get; }
}