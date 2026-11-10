using System;
using System.Collections.Generic;
using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

/// <summary>What a little sign in a room's top left corner says about it.</summary>
public enum RoomBadgeKind
{
  /// <summary>The game starts here.</summary>
  StartRoom,

  /// <summary>Automap doesn't map from here (<see cref="Room.AutomapPause" />).</summary>
  NoAutomap
}

/// <summary>A little round sign in a room's top left corner, at <see cref="Centre" />.</summary>
public readonly record struct RoomBadge(RoomBadgeKind Kind, Vector Centre, float Radius)
{
  /// <summary>Whether <paramref name="point" /> is on the sign, give or take a little, for its tooltip.</summary>
  public bool Contains(Vector point)
  {
    var dx = point.X - Centre.X;
    var dy = point.Y - Centre.Y;
    var reach = Radius + 2;
    return dx * dx + dy * dy <= reach * reach;
  }

  /// <summary>The sign's tooltip title.</summary>
  public string Title => Kind switch
  {
    RoomBadgeKind.StartRoom => "Start room",
    _ => "Don't automap from here"
  };

  /// <summary>The sign's tooltip text.</summary>
  public string Description => Kind switch
  {
    RoomBadgeKind.StartRoom => "The game starts here. Change it with Start Room in the room's menu or properties.",
    _ => "Automap maps nothing while you're in this room (a maze, a lift...), and carries on where you come out. " +
         "Change it in the room's menu or properties."
  };
}

/// <summary>The signs a room shows in its top left corner, left to right; drawing and tooltips both use these.</summary>
public static class RoomBadges
{
  public static IReadOnlyList<RoomBadge> Of(Room room)
  {
    var badges = new List<RoomBadge>();
    if (room == null) return badges;
    var bounds = room.InnerBounds;
    var radius = Math.Min(6f, Math.Min(bounds.Width, bounds.Height) / 6);
    if (radius < 2) return badges;

    var x = bounds.Left + radius + 4;
    var y = bounds.Top + radius + 4;
    void Add(RoomBadgeKind kind)
    {
      badges.Add(new RoomBadge(kind, new Vector(x, y), radius));
      x += radius * 2 + 3;
    }

    if (room.IsStartRoom) Add(RoomBadgeKind.StartRoom);
    if (room.AutomapPause) Add(RoomBadgeKind.NoAutomap);
    return badges;
  }

  /// <summary>The sign on <paramref name="room" /> at <paramref name="point" />, if any.</summary>
  public static RoomBadge? At(Room room, Vector point)
  {
    foreach (var badge in Of(room))
      if (badge.Contains(point))
        return badge;
    return null;
  }
}
