using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

/// <summary>
///   Rooms that a tidy has just moved slide to their new places on screen instead of jumping. Only the drawing
///   slides: each room is already at its new place on the map (for saving, undo, automap and the next tidy), and
///   <see cref="Show" /> puts the rooms where they appear to be, briefly, while the map is drawn.
/// </summary>
public static class RoomGlide
{
  /// <summary>How long a slide takes, in milliseconds.</summary>
  public const double DurationMs = 300;

  private static readonly Stopwatch _watch = Stopwatch.StartNew();
  private static readonly Dictionary<Room, (Vector From, double Start)> _gliding = new();

  /// <summary>The time now in milliseconds; tests replace it.</summary>
  public static Func<double> Clock { get; set; } = () => _watch.Elapsed.TotalMilliseconds;

  /// <summary>True while a room is still sliding: the map needs drawing again.</summary>
  public static bool IsActive
  {
    get
    {
      Expire();
      return _gliding.Count > 0;
    }
  }

  /// <summary>
  ///   Slides the rooms that moved from where they were in <paramref name="before" /> (where they appeared, if they
  ///   were still sliding) to where they are now.
  /// </summary>
  public static void Start(IReadOnlyDictionary<Room, Vector> before)
  {
    if (before == null) return;
    var now = Clock();
    foreach (var (room, was) in before)
    {
      if (room.Position == was) continue;
      // still sliding towards where it was: carry on from where it appears on the way there
      var from = _gliding.ContainsKey(room) ? Appears(room, was, now) : was;
      _gliding[room] = (from, now);
    }
  }

  /// <summary>Where each room is now, to pass to <see cref="Start" /> after moving them.</summary>
  public static Dictionary<Room, Vector> Positions(IEnumerable<Element> elements) =>
    elements.OfType<Room>().ToDictionary(room => room, room => room.Position);

  /// <summary>Stops every slide: the rooms show where they are.</summary>
  public static void Stop() => _gliding.Clear();

  /// <summary>Where <paramref name="room" /> appears now: on its way from where it was to where it is.</summary>
  public static Vector Appears(Room room) => Appears(room, room.Position, Clock());

  /// <summary>Where <paramref name="room" /> appears at <paramref name="now" /> on its way to <paramref name="to" />.</summary>
  private static Vector Appears(Room room, Vector to, double now)
  {
    if (!_gliding.TryGetValue(room, out var glide)) return to;
    var t = Math.Clamp((now - glide.Start) / DurationMs, 0, 1);
    var eased = 1 - Math.Pow(1 - t, 3); // fast at first, settling gently
    return new Vector(glide.From.X + (to.X - glide.From.X) * (float)eased,
      glide.From.Y + (to.Y - glide.From.Y) * (float)eased);
  }

  /// <summary>
  ///   Puts each sliding room where it appears, without telling anyone, until the returned handle is disposed, when
  ///   they go back to where they are. For drawing only.
  /// </summary>
  public static IDisposable Show()
  {
    Expire();
    if (_gliding.Count == 0) return Nothing.Instance;
    var now = Clock();
    var real = new List<(Room Room, Vector At)>();
    foreach (var room in _gliding.Keys)
    {
      real.Add((room, room.Position));
      room.ShowAt(Appears(room, room.Position, now));
    }

    return new Restore(real);
  }

  private static void Expire()
  {
    if (_gliding.Count == 0) return;
    var now = Clock();
    foreach (var room in _gliding.Where(glide => now - glide.Value.Start >= DurationMs).Select(glide => glide.Key)
               .ToList())
      _gliding.Remove(room);
  }

  private sealed class Restore : IDisposable
  {
    private readonly List<(Room Room, Vector At)> _real;
    public Restore(List<(Room Room, Vector At)> real) => _real = real;

    public void Dispose()
    {
      foreach (var (room, at) in _real) room.ShowAt(at);
    }
  }

  private sealed class Nothing : IDisposable
  {
    public static readonly Nothing Instance = new();
    public void Dispose() { }
  }
}
