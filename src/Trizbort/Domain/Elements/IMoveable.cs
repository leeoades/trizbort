using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements {
  internal interface IMoveable {
    Vector Position { get; set; }
    float X { get; }
    float Y { get; }
  }
}