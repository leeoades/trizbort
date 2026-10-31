using Trizbort.Domain.Enums;

namespace Trizbort.Editor.Commands;

public sealed partial class CommandCatalogue
{
  /// <summary>Connection, selection, delete and clipboard commands.</summary>
  private static void RegisterConnectionAndSelectionCommands(CommandCatalogue catalogue)
  {
    SelectCommands.Register(catalogue);
    EditCommands.Register(catalogue);
    ConnectionStyleCommands.Register(catalogue);
  }
}
