using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Input;

namespace Trizbort.Editor.Commands;

/// <summary>Plain / Dotted / One Way, Reverse Line and Rotate Connector commands.</summary>
public static class ConnectionStyleCommands
{
  private static readonly KeyGesture[] _plainGestures = { new KeyGesture(InputKey.P, ModifierKeys.None) };
  private static readonly KeyGesture[] _dottedGestures = { new KeyGesture(InputKey.T, ModifierKeys.None) };
  private static readonly KeyGesture[] _oneWayGestures = { new KeyGesture(InputKey.A, ModifierKeys.None) };
  private static readonly KeyGesture[] _reverseGestures = { new KeyGesture(InputKey.V, ModifierKeys.None) };

  /// <summary>A join between two different rooms, which can be drawn split.</summary>
  private static bool CanSplit(Connection connection)
  {
    var source = connection.GetSourceRoom();
    var target = connection.GetTargetRoom();
    return source != null && target != null && source != target;
  }

  /// <summary>The split join selected, alone: the one Bring 'X' to 'Y' works on.</summary>
  private static Connection SplitJoin(EditorSession s) =>
    s.Selection.Connections.Count() == 1 && s.Selection.Connections.First() is { IsDrawnSplit: true } join ? join : null;

  private static void RegisterBring(CommandCatalogue catalogue, string id, bool end)
  {
    string Title(EditorSession s)
    {
      var join = SplitJoin(s);
      if (join == null) return end ? "_Bring the Far Room Here" : "Bring the _Near Room Here";
      var room = end ? join.GetTargetRoom() : join.GetSourceRoom();
      var to = end ? join.GetSourceRoom() : join.GetTargetRoom();
      return (end ? "_Bring '" : "B_ring '") + Escape(room.Name) + "' to '" + Escape(to.Name) + "'...";
    }

    catalogue.Register(new EditorCommand
    {
      Id = id,
      Title = end ? "_Bring the Far Room Here" : "Bring the _Near Room Here",
      DynamicTitle = Title,
      MenuPath = "Edit",
      Group = "3-connection",
      Scope = CommandScope.Canvas,
      ContextTargets = ContextTargets.Connection,
      CanExecuteFunc = s => SplitJoin(s) != null,
      ExecuteAsyncFunc = s => CommandCatalogue.BringAcrossAsync(s, end)
    });
  }

  /// <summary>A room's name in a menu title: its underscores aren't mnemonics.</summary>
  private static string Escape(string name) => (name ?? string.Empty).Replace("_", "__");

  public static void Register(CommandCatalogue catalogue)
  {
    catalogue.Register(new EditorCommand
    {
      Id = "connection.plain",
      Title = "_Plain Lines",
      MenuPath = "Edit",
      Group = "3-connection",
      Scope = CommandScope.Canvas,
      Gestures = _plainGestures,
      ContextTargets = ContextTargets.Connection,
      ContextOrder = 50, ContextGroup = "5-connection",
      ExecuteAsyncFunc = s => Run(() => s.ApplyPlainConnectionSettings())
    });
    catalogue.Register(new EditorCommand
    {
      Id = "connection.toggleDotted",
      Title = "_Dotted Lines",
      MenuPath = "Edit",
      Group = "3-connection",
      Scope = CommandScope.Canvas,
      Gestures = _dottedGestures,
      ContextTargets = ContextTargets.Connection,
      ContextOrder = 51, ContextGroup = "5-connection",
      ToolbarOrder = 66, ToolbarGroup = "2-edit", ShowInToolbar = true, IconKey = "IconDotted",
      IsCheckedFunc = s => CurrentStyle(s) == ConnectionStyle.Dashed,
      ExecuteAsyncFunc = s => Run(() =>
        s.SetConnectionStyle(CurrentStyle(s) == ConnectionStyle.Dashed ? ConnectionStyle.Solid : ConnectionStyle.Dashed))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "connection.toggleOneWay",
      Title = "_One Way Arrow",
      MenuPath = "Edit",
      Group = "3-connection",
      Scope = CommandScope.Canvas,
      Gestures = _oneWayGestures,
      ContextTargets = ContextTargets.Connection,
      ContextOrder = 52, ContextGroup = "5-connection",
      ToolbarOrder = 67, ToolbarGroup = "2-edit", ShowInToolbar = true, IconKey = "IconOneWay",
      IsCheckedFunc = s => CurrentFlow(s) == ConnectionFlow.OneWay,
      ExecuteAsyncFunc = s => Run(() =>
        s.SetConnectionFlow(CurrentFlow(s) == ConnectionFlow.OneWay ? ConnectionFlow.TwoWay : ConnectionFlow.OneWay))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "connection.reverse",
      Title = "_Reverse Line",
      MenuPath = "Edit",
      Group = "3-connection",
      Scope = CommandScope.Canvas,
      Gestures = _reverseGestures,
      ContextTargets = ContextTargets.Connection,
      ContextOrder = 53, ContextGroup = "5-connection",
      CanExecuteFunc = s => s.Selection.Connections.Any(),
      ExecuteAsyncFunc = s => Run(() => s.ReverseSelectedConnections())
    });

    catalogue.Register(new EditorCommand
    {
      Id = "connection.toggleSplit",
      Title = "_Split Join",
      MenuPath = "Edit",
      Group = "3-connection",
      Scope = CommandScope.Canvas,
      ContextTargets = ContextTargets.Connection,
      ContextOrder = 54, ContextGroup = "5-connection",
      CanExecuteFunc = s => s.Selection.Connections.Any(CanSplit),
      IsCheckedFunc = s => s.Selection.Connections.FirstOrDefault(CanSplit) is { Split: true },
      ExecuteAsyncFunc = s => Run(() =>
      {
        // a long join drawn as a stub out of each room, labelled with the room at the other end ("to Building")
        var joins = s.Selection.Connections.Where(CanSplit).ToList();
        if (joins.Count == 0) return;
        var on = !joins[0].Split;
        s.Execute(on ? "Split join" : "Join up split join", () =>
        {
          foreach (var join in joins) join.Split = on;
        });
      })
    });

    // a split join: bring the room at one end across to the other, and join them whole again
    RegisterBring(catalogue, "connection.bringEnd", true);
    RegisterBring(catalogue, "connection.bringStart", false);

    // Rotation has no menu entry in the reference, so these are key-only.
    RegisterRotate(catalogue, "connection.rotate.cw", "Rotate Connector Clockwise", InputKey.OemOpenBrackets, ModifierKeys.None, true, false);
    RegisterRotate(catalogue, "connection.rotate.ccw", "Rotate Connector Anticlockwise", InputKey.OemCloseBrackets, ModifierKeys.None, false, false);
    RegisterRotate(catalogue, "connection.rotate.ctrlCw", "Rotate Other Connector Clockwise", InputKey.OemOpenBrackets, ModifierKeys.Primary, true, true);
    RegisterRotate(catalogue, "connection.rotate.ctrlCcw", "Rotate Other Connector Anticlockwise", InputKey.OemCloseBrackets, ModifierKeys.Primary, false, true);
  }

  private static void RegisterRotate(
    CommandCatalogue catalogue, string id, string title, InputKey key, ModifierKeys modifiers, bool clockwise, bool ctrlFlag)
  {
    catalogue.Register(new EditorCommand
    {
      Id = id,
      Title = title,
      Hidden = true,
      Scope = CommandScope.Canvas,
      Gestures = new[] { new KeyGesture(key, modifiers) },
      CanExecuteFunc = s => s.Selection.Count == 1 && s.Selection.Primary is Connection connection &&
                            connection.ConnectedRoomToRotate(!ctrlFlag) != -1,
      ExecuteAsyncFunc = s => Run(() => s.RotateSelectedConnector(clockwise, ctrlFlag))
    });
  }

  /// <summary>The style the toggle shows: the primary selected connection's, otherwise the new-connection default.</summary>
  private static ConnectionStyle CurrentStyle(EditorSession session)
  {
    return session.Selection.Primary is Connection connection ? connection.Style : session.NewConnection.Style;
  }

  private static ConnectionFlow CurrentFlow(EditorSession session)
  {
    return session.Selection.Primary is Connection connection ? connection.Flow : session.NewConnection.Flow;
  }

  private static Task Run(System.Action action)
  {
    action();
    return Task.CompletedTask;
  }
}
