using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

/// <summary>The sets offered by Edit &gt; Select Special.</summary>
public enum SelectSpecialKind
{
  Rooms,
  UnconnectedRooms,
  RoomsWithObjects,
  RoomsWithoutObjects,
  Connections,
  DanglingConnections,
  SelfLoopingConnections
}

/// <summary>Select All / None / Select Special commands.</summary>
public static class SelectCommands
{
  /// <summary>The elements of <paramref name="project" /> matching <paramref name="kind" />, in map order.</summary>
  public static List<Element> Find(Project project, SelectSpecialKind kind)
  {
    var elements = project.Elements;
    switch (kind)
    {
      case SelectSpecialKind.Rooms:
        return elements.OfType<Room>().Cast<Element>().ToList();
      case SelectSpecialKind.UnconnectedRooms:
        return elements.OfType<Room>().Where(room => room.GetConnections().Count == 0).Cast<Element>().ToList();
      case SelectSpecialKind.RoomsWithObjects:
        return elements.OfType<Room>().Where(room => room.ListOfObjects().Count > 0).Cast<Element>().ToList();
      case SelectSpecialKind.RoomsWithoutObjects:
        return elements.OfType<Room>().Where(room => room.ListOfObjects().Count == 0).Cast<Element>().ToList();
      case SelectSpecialKind.Connections:
        return elements.OfType<Connection>().Cast<Element>().ToList();
      case SelectSpecialKind.DanglingConnections:
        return elements.OfType<Connection>().Where(connection => connection.IsDangling).Cast<Element>().ToList();
      case SelectSpecialKind.SelfLoopingConnections:
        return elements.OfType<Connection>().Where(IsSelfLooping).Cast<Element>().ToList();
      default:
        return new List<Element>();
    }
  }

  private static bool IsSelfLooping(Connection connection)
  {
    var source = connection.GetSourceRoom();
    var target = connection.GetTargetRoom();
    return source != null && target != null && source == target;
  }

  public static void Register(CommandCatalogue catalogue)
  {
    catalogue.Register(new EditorCommand
    {
      Id = "edit.selectAll",
      Title = "Select _All",
      MenuPath = "Edit",
      Group = "1-select",
      Gestures = new[] { new KeyGesture(InputKey.A, ModifierKeys.Primary) },
      CanExecuteFunc = s => s.Selection.Count < s.Project.Elements.Count,
      ExecuteAsyncFunc = s => Run(() => s.SelectAll())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "edit.selectNone",
      Title = "Select _None",
      MenuPath = "Edit",
      Group = "1-select",
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(InputKey.Escape, ModifierKeys.None) },
      CanExecuteFunc = s => s.Selection.Count > 0,
      ExecuteAsyncFunc = s => Run(() => s.ClearSelection())
    });

    RegisterSpecial(catalogue, "select.special.rooms", "All _Rooms", SelectSpecialKind.Rooms);
    RegisterSpecial(catalogue, "select.special.unconnectedRooms", "_Unconnected Rooms", SelectSpecialKind.UnconnectedRooms);
    RegisterSpecial(catalogue, "select.special.roomsWithObjects", "Rooms with _Objects", SelectSpecialKind.RoomsWithObjects);
    RegisterSpecial(catalogue, "select.special.roomsWithoutObjects", "Rooms _without Objects", SelectSpecialKind.RoomsWithoutObjects);
    RegisterSpecial(catalogue, "select.special.connections", "All _Connections", SelectSpecialKind.Connections);
    RegisterSpecial(catalogue, "select.special.danglingConnections", "_Dangling Connections", SelectSpecialKind.DanglingConnections);
    RegisterSpecial(catalogue, "select.special.selfLoopingConnections", "_Self Looping Connections", SelectSpecialKind.SelfLoopingConnections);
  }

  private static void RegisterSpecial(CommandCatalogue catalogue, string id, string title, SelectSpecialKind kind)
  {
    catalogue.Register(new EditorCommand
    {
      Id = id,
      Title = title,
      MenuPath = "Edit/Select Special",
      Group = "1-select",
      CanExecuteFunc = s => Find(s.Project, kind).Count > 0,
      ExecuteAsyncFunc = s => Run(() => s.SelectSpecial(kind))
    });
  }

  private static System.Threading.Tasks.Task Run(System.Action action)
  {
    action();
    return System.Threading.Tasks.Task.CompletedTask;
  }
}
