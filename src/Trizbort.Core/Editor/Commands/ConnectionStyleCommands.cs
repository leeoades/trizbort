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
