using Trizbort.Navigation;

namespace Trizbort.Editor;

public sealed partial class EditorSession
{
  /// <summary>Directions from one room to another, for the directions panel.</summary>
  public Directions Directions { get; } = new();
}
