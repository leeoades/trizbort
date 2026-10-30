using System;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>
///   Creates rooms for the editing commands (Add Room, double-click) with the "new room" defaults.
/// </summary>
public static class RoomFactory
{
  /// <summary>
  ///   Build a new, unattached room centred on <paramref name="centre" /> (map space). The caller adds it to the
  ///   project.
  /// </summary>
  /// <remarks>
  ///   Defaults come from <paramref name="styleSource" /> when it is not null: size, <c>IsDark</c> and
  ///   <c>ObjectsPosition</c> are always copied (the legacy "new room" template). Without a source the size is
  ///   3 x 2 grid cells, lit, objects below the name.
  ///   When <paramref name="applyStyle" /> is true the rest of the style is copied too, exactly the properties of
  ///   <see cref="Room.CopyStyleFrom" />: region, shape (including ellipse, rounded, octagonal, straight edges and
  ///   corner radii), hand-drawn style, border style, border/fill/second-fill colours and location, name,
  ///   subtitle and object text colours, dark flag and objects position.
  ///   The top-left is <c>centre - size / 2</c>, snapped to the grid, and nudged diagonally while another movable
  ///   element sits exactly on it.
  /// </remarks>
  public static Room CreateNewRoom(Project project, Vector centre, Room styleSource, bool applyStyle)
  {
    if (project == null) throw new ArgumentNullException(nameof(project));

    var room = new Room(project);
    if (styleSource != null)
    {
      room.Size = styleSource.Size;
      room.IsDark = styleSource.IsDark;
      room.ObjectsPosition = styleSource.ObjectsPosition;
      if (applyStyle) room.CopyStyleFrom(styleSource);
    }
    else
    {
      room.Size = new Vector(Settings.GridSize * 3, Settings.GridSize * 2);
      room.IsDark = false;
      room.ObjectsPosition = CompassPoint.South;
    }

    var position = Settings.Snap(centre - room.Size / 2);
    var step = Math.Max(2, Settings.GridSize);
    var clash = true;
    while (clash)
    {
      clash = false;
      foreach (var element in project.Elements)
      {
        if (element is IMoveable moveable && moveable.Position == position)
        {
          position.X += step;
          position.Y += step;
          clash = true;
        }
      }
    }

    room.Position = position;
    return room;
  }
}
