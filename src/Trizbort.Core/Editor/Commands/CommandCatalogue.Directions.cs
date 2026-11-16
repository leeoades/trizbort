using Trizbort.Domain.Elements;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>
  ///   Directions (satnav): Navigate to Here and Navigate from Here on a room's right-click menu (and the Project
  ///   menu, for the selected room), and showing the directions panel.
  /// </summary>
  private static void RegisterDirectionsCommands(CommandCatalogue catalogue)
  {
    catalogue.Register(new EditorCommand
    {
      Id = "directions.to",
      Title = "Navigate _to Selected Room",
      ContextTitle = "Navigate _to Here",
      MenuPath = "Project/Directions",
      Group = "1-directions",
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Selection.Count == 1 && s.PrimaryRoom != null,
      // from where automap has the player, when it knows; otherwise from the room chosen with Navigate from Here
      ExecuteAsyncFunc = s => Run(() =>
        s.Directions.NavigateTo(s.PrimaryRoom, s.Automap.IsRunning ? s.Automap.CurrentRoom : null))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "directions.from",
      Title = "Navigate _from Selected Room...",
      ContextTitle = "Navigate _from Here...",
      MenuPath = "Project/Directions",
      Group = "1-directions",
      ContextTargets = ContextTargets.Room,
      CanExecuteFunc = s => s.Selection.Count == 1 && s.PrimaryRoom != null,
      ExecuteAsyncFunc = s => Run(() => s.Directions.NavigateFrom(s.PrimaryRoom))
    });
    catalogue.Register(new EditorCommand
    {
      Id = "directions.show",
      Title = "Show _Directions",
      MenuPath = "Project/Directions",
      Group = "2-panel",
      IsCheckedFunc = s => s.Directions.IsOpen,
      ExecuteAsyncFunc = s => Run(() =>
      {
        if (s.Directions.IsOpen) s.Directions.Close();
        else s.Directions.Open();
      })
    });
  }
}
