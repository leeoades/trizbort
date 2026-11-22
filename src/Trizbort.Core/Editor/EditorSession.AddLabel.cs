using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Add label with properties prompt (ticket 038).
public sealed partial class EditorSession
{
  /// <summary>
  ///   As <see cref="AddLabel" />, then asks <see cref="Prompts" /> to open the label's properties. Adding the label
  ///   is one undo step, and what's changed in the dialog another; cancelling the dialog keeps the label.
  /// </summary>
  public async Task<MapLabel> AddLabelAsync(Vector? centre, bool openProperties = true)
  {
    var label = new MapLabel(Project);
    var point = centre ?? Viewport.Origin;
    label.Position = Settings.Snap(new Vector(point.X - label.Size.X / 2, point.Y - label.Size.Y / 2));

    var transaction = BeginTransaction("Add label", new Element[] { label });
    try
    {
      Project.Elements.Add(label);
      Selection.Set(new Element[] { label });
      Project.ActiveSelectedElement = label;
      RaiseChanged(EditorChange.Model);
    }
    catch
    {
      transaction.Cancel();
      transaction.Dispose();
      throw;
    }
    transaction.Dispose();

    // its properties after: the dialog is shown beside the map (a room clicked there can be linked), so no step is
    // held open while it is; what it changes is a step of its own
    if (openProperties) await Prompts.EditElementAsync(label);
    RaiseChanged(EditorChange.Model);
    PostStatusMessage("Added label");
    return label;
  }
}
