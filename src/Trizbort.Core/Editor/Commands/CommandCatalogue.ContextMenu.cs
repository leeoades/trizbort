using System;

namespace Trizbort.Editor.Commands;

// How the right-click menus are laid out: a few items at the top, the rest grouped in submenus (Room, Shape, Swap,
// Rooms, Automap, Add, Arrange for a room; Line and Label for a join). Kept in one place so the whole menu can be
// seen at once; the commands themselves say only which targets they apply to.
public sealed partial class CommandCatalogue
{
  private void ArrangeContextMenus()
  {
    // a room
    Arrange("room.toggleDark", "Roo_m", 50, "5-room");
    Arrange("room.setStart", "Roo_m", 51, "5-room");
    Arrange("room.setEnd", "Roo_m", 52, "5-room");
    Arrange("room.toggleMoves", "Roo_m", 53, "5-room");
    Arrange("room.shape.square", "S_hape", 54, "5-room");
    Arrange("room.shape.ellipse", "S_hape", 55, "5-room");
    Arrange("room.shape.rounded", "S_hape", 56, "5-room");
    Arrange("room.shape.octagonal", "S_hape", 57, "5-room");
    Arrange("room.swap.objects", "S_wap", 58, "5-room");
    Arrange("room.swap.names", "S_wap", 59, "5-room");
    Arrange("room.swap.formats", "S_wap", 60, "5-room");
    Arrange("room.swap.regions", "S_wap", 61, "5-room");
    Arrange("room.join", "Room_s", 62, "6-rooms");
    Arrange("room.merge", "Room_s", 63, "6-rooms");
    Arrange("room.split", "Room_s", 64, "6-rooms");
    Arrange("room.newArea", "Room_s", 65, "6-rooms");
    Arrange("room.setRegion", "Room_s", 66, "6-rooms");
    Arrange("map.tidy", "Room_s", 67, "6-rooms");
    Arrange("room.toggleAutomapPause", "_Automap", 68, "6-rooms");
    Arrange("automap.imHere", "_Automap", 69, "6-rooms", plainTitle: true);
    Arrange("automap.differentRoom", "_Automap", 70, "6-rooms", plainTitle: true);
    // only on a copy of a room, so they stay where they're seen
    Arrange("room.copy.goTo", null, 71, "7-copy");
    Arrange("room.copy.moveJoins", null, 72, "7-copy");
    Arrange("room.copy.ownRoom", null, 73, "7-copy");

    // a join
    Arrange("connection.plain", "_Line", 50, "5-connection");
    Arrange("connection.toggleDotted", "_Line", 51, "5-connection");
    Arrange("connection.toggleOneWay", "_Line", 52, "5-connection");
    Arrange("connection.reverse", "_Line", 53, "5-connection");
    Arrange("connection.label.up", "La_bel", 54, "5-connection");
    Arrange("connection.label.down", "La_bel", 55, "5-connection");
    Arrange("connection.label.in", "La_bel", 56, "5-connection");
    Arrange("connection.label.out", "La_bel", 57, "5-connection");
    Arrange("connection.toggleSplit", null, 58, "6-split");
    Arrange("connection.bringEnd", null, 59, "6-split");
    Arrange("connection.bringStart", null, 60, "6-split");

    // anything
    Arrange("project.addRoom", "A_dd", 80, "8-add", "_Room");
    Arrange("project.addLabel", "A_dd", 81, "8-add", "_Map Label");
    Arrange("element.sendToBack", "Arra_nge", 82, "8-add");
    Arrange("element.bringToFront", "Arra_nge", 83, "8-add");
  }

  /// <summary>Puts a command in the right-click menu: in <paramref name="submenu" /> (null: at the top).</summary>
  private void Arrange(string id, string submenu, int order, string group, string contextTitle = null,
    bool plainTitle = false)
  {
    if (!_byId.TryGetValue(id, out var command)) return;
    var arranged = command with
    {
      ContextSubmenu = submenu,
      ContextOrder = order,
      ContextGroup = group,
      ContextTitle = plainTitle ? null : contextTitle ?? command.ContextTitle
    };
    _byId[id] = arranged;
    _commands[_commands.IndexOf(command)] = arranged;
  }
}
