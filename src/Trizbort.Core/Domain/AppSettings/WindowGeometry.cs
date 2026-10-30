using System;
using System.Collections.Generic;

namespace Trizbort.Domain.AppSettings;

/// <summary>
///   A screen's usable rectangle, in the same pixel units as <see cref="WindowGeometry" />.
/// </summary>
public readonly struct ScreenRect
{
  public ScreenRect(int x, int y, int width, int height)
  {
    X = x;
    Y = y;
    Width = width;
    Height = height;
  }

  public int X { get; }
  public int Y { get; }
  public int Width { get; }
  public int Height { get; }
}

/// <summary>
///   Where the main window was at exit. <see cref="X" />, <see cref="Y" />, <see cref="Width" /> and
///   <see cref="Height" /> are the normal (not maximised) bounds.
/// </summary>
public sealed class WindowGeometry
{
  public const int MinimumWidth = 640;
  public const int MinimumHeight = 400;

  public int X { get; set; }
  public int Y { get; set; }
  public int Width { get; set; }
  public int Height { get; set; }
  public bool Maximised { get; set; }

  /// <summary>
  ///   Adjusts <paramref name="saved" /> so the window is at least the minimum size and visible on one of
  ///   <paramref name="screens" /> (a monitor may have been unplugged). A window that is off every screen is centred
  ///   on the first screen. Returns null when nothing was saved.
  /// </summary>
  public static WindowGeometry Fit(WindowGeometry saved, IReadOnlyList<ScreenRect> screens)
  {
    if (saved == null) return null;

    var width = Math.Max(saved.Width, MinimumWidth);
    var height = Math.Max(saved.Height, MinimumHeight);
    var result = new WindowGeometry { X = saved.X, Y = saved.Y, Width = width, Height = height, Maximised = saved.Maximised };
    if (screens == null || screens.Count == 0) return result;

    var best = screens[0];
    long bestArea = -1;
    foreach (var screen in screens)
    {
      var area = IntersectionArea(saved.X, saved.Y, width, height, screen);
      if (area > bestArea)
      {
        bestArea = area;
        best = screen;
      }
    }

    result.Width = Math.Min(width, Math.Max(best.Width, MinimumWidth));
    result.Height = Math.Min(height, Math.Max(best.Height, MinimumHeight));

    if (bestArea <= 0)
    {
      result.X = best.X + (best.Width - result.Width) / 2;
      result.Y = best.Y + (best.Height - result.Height) / 2;
      return result;
    }

    result.X = Math.Max(best.X, Math.Min(saved.X, best.X + best.Width - result.Width));
    result.Y = Math.Max(best.Y, Math.Min(saved.Y, best.Y + best.Height - result.Height));
    return result;
  }

  private static long IntersectionArea(int x, int y, int width, int height, ScreenRect screen)
  {
    var left = Math.Max(x, screen.X);
    var top = Math.Max(y, screen.Y);
    var right = Math.Min(x + width, screen.X + screen.Width);
    var bottom = Math.Min(y + height, screen.Y + screen.Height);
    if (right <= left || bottom <= top) return 0;
    return (long)(right - left) * (bottom - top);
  }
}
