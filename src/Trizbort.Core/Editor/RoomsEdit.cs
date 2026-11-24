using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

/// <summary>
///   What several rooms share, for editing them together (Lee, 24 Nov 2026): each value is the one they all have, or
///   null where they differ. Applied, a null leaves each room as it is, so only what was set changes.
/// </summary>
public sealed class RoomsEdit
{
  /// <summary>
  ///   Stands for "the rooms differ" in a colour box: a colour no room would have, so any pick (even No Colour) is a
  ///   change. The colour picker shows it as Mixed.
  /// </summary>
  public static readonly Color MixedColor = Color.FromArgb(1, 1, 2, 3);

  public bool? IsDark { get; set; }
  public bool? AutomapPause { get; set; }
  public RoomShape? Shape { get; set; }
  public BorderDashStyle? BorderStyle { get; set; }
  public HandDrawnStyle? HandDrawnStyle { get; set; }
  public string Region { get; set; }
  public Color? FillColor { get; set; }
  public Color? SecondFillColor { get; set; }
  public string SecondFillLocation { get; set; }
  public Color? BorderColor { get; set; }
  public Color? NameColor { get; set; }
  public Color? SubtitleColor { get; set; }
  public Color? ObjectTextColor { get; set; }

  /// <summary>The values the rooms share; null where they differ.</summary>
  public static RoomsEdit Of(IReadOnlyList<Room> rooms)
  {
    if (rooms == null || rooms.Count == 0) throw new ArgumentException("No rooms", nameof(rooms));
    return new RoomsEdit
    {
      IsDark = Shared(rooms, room => room.IsDark),
      AutomapPause = Shared(rooms, room => room.AutomapPause),
      Shape = Shared(rooms, room => room.Shape),
      BorderStyle = Shared(rooms, room => room.BorderStyle),
      HandDrawnStyle = Shared(rooms, room => room.HandDrawnStyle),
      Region = SharedText(rooms, room => string.IsNullOrEmpty(room.Region) ? Domain.Misc.Region.DefaultRegion : room.Region),
      FillColor = SharedColor(rooms, room => room.RoomFillColor),
      SecondFillColor = SharedColor(rooms, room => room.SecondFillColor),
      SecondFillLocation = SharedText(rooms, room => string.IsNullOrEmpty(room.SecondFillLocation) ? "Bottom" : room.SecondFillLocation),
      BorderColor = SharedColor(rooms, room => room.RoomBorderColor),
      NameColor = SharedColor(rooms, room => room.RoomNameColor),
      SubtitleColor = SharedColor(rooms, room => room.RoomSubtitleColor),
      ObjectTextColor = SharedColor(rooms, room => room.RoomObjectTextColor)
    };
  }

  /// <summary>Sets what isn't null on each room.</summary>
  public void ApplyTo(IEnumerable<Room> rooms)
  {
    foreach (var room in rooms)
    {
      if (IsDark is { } dark) room.IsDark = dark;
      if (AutomapPause is { } pause) room.AutomapPause = pause;
      if (Shape is { } shape) room.Shape = shape;
      if (BorderStyle is { } border) room.BorderStyle = border;
      if (HandDrawnStyle is { } hand) room.HandDrawnStyle = hand;
      if (Region != null) room.Region = Region;
      if (FillColor is { } fill) room.RoomFillColor = fill;
      if (SecondFillColor is { } second) room.SecondFillColor = second;
      if (SecondFillLocation != null) room.SecondFillLocation = SecondFillLocation;
      if (BorderColor is { } line) room.RoomBorderColor = line;
      if (NameColor is { } name) room.RoomNameColor = name;
      if (SubtitleColor is { } subtitle) room.RoomSubtitleColor = subtitle;
      if (ObjectTextColor is { } objects) room.RoomObjectTextColor = objects;
    }
  }

  private static T? Shared<T>(IReadOnlyList<Room> rooms, Func<Room, T> value) where T : struct
  {
    var first = value(rooms[0]);
    return rooms.All(room => EqualityComparer<T>.Default.Equals(value(room), first)) ? first : null;
  }

  private static string SharedText(IReadOnlyList<Room> rooms, Func<Room, string> value)
  {
    var first = value(rooms[0]);
    return rooms.All(room => string.Equals(value(room), first, StringComparison.Ordinal)) ? first : null;
  }

  private static Color? SharedColor(IReadOnlyList<Room> rooms, Func<Room, Color> value)
  {
    var first = value(rooms[0]).ToArgb();
    return rooms.All(room => value(room).ToArgb() == first) ? value(rooms[0]) : null;
  }
}
