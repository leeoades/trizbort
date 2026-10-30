using System;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

// Automap hooks: the engine the run/step commands talk to, and following the room being mapped (tickets 061, 126, 128).
public sealed partial class EditorSession
{
  private global::Trizbort.Automap.IAutomapController _automap;

  /// <summary>
  ///   The automap engine the automap commands act on; the shared engine unless a test substitutes one. Assign it
  ///   before subscribing to <see cref="AutomapStatusChanged" /> or <see cref="AutomapFailed" />.
  /// </summary>
  public global::Trizbort.Automap.IAutomapController Automap
  {
    get => _automap ??= global::Trizbort.Automap.Automap.Instance;
    set => _automap = value;
  }

  /// <summary>Re-exposes the engine's status text changes. May be raised off the UI thread.</summary>
  public event Action<string> AutomapStatusChanged
  {
    add => Automap.StatusChanged += value;
    remove => Automap.StatusChanged -= value;
  }

  /// <summary>Re-exposes the engine's failure event. May be raised off the UI thread.</summary>
  public event Action<Exception> AutomapFailed
  {
    add => Automap.AutomapFailed += value;
    remove => Automap.AutomapFailed -= value;
  }

  /// <summary>
  ///   Pans the viewport, without changing the zoom, just far enough to bring the element fully into view with a
  ///   small margin. Does nothing when it is already visible.
  /// </summary>
  public void EnsureVisible(Element element)
  {
    if (element == null) return;

    var bounds = element is Room room ? room.InnerBounds : element.UnionBoundsWith(Rect.Empty, false);
    if (bounds == Rect.Empty) return;

    var visible = Viewport.VisibleWorldBounds;
    var margin = 20f / Viewport.ZoomFactor;
    var dx = PanToFit(bounds.Left, bounds.Right, visible.Left, visible.Right, margin);
    var dy = PanToFit(bounds.Top, bounds.Bottom, visible.Top, visible.Bottom, margin);
    if (dx == 0 && dy == 0) return;

    Viewport.Pan(new Vector(dx, dy));
  }

  private static float PanToFit(float low, float high, float visibleLow, float visibleHigh, float margin)
  {
    // too big to fit with a margin: centre it
    if (high - low + 2 * margin >= visibleHigh - visibleLow)
      return (low + high) / 2 - (visibleLow + visibleHigh) / 2;
    if (low - margin < visibleLow) return low - margin - visibleLow;
    if (high + margin > visibleHigh) return high + margin - visibleHigh;
    return 0;
  }
}
