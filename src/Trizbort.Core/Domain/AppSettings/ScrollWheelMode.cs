namespace Trizbort.Domain.AppSettings;

/// <summary>What the scroll wheel / trackpad scroll does on the map canvas (issue #21).</summary>
public enum ScrollWheelMode
{
  /// <summary>Whole wheel notches zoom; fractional or horizontal (trackpad-like) scrolls pan.</summary>
  Automatic = 0,

  /// <summary>Scrolling always zooms.</summary>
  AlwaysZoom = 1,

  /// <summary>Scrolling always pans (Ctrl + scroll still zooms).</summary>
  AlwaysPan = 2
}
