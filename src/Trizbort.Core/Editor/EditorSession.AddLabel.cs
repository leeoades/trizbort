using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Add label with properties prompt (ticket 038).
public sealed partial class EditorSession
{
  /// <summary>
  ///   As <see cref="AddLabel" />, then asks <see cref="Prompts" /> to open the label's properties. The label and
  ///   any edits made in the dialog are one undo step; cancelling the dialog keeps the label.
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
      if (openProperties) await Prompts.EditElementAsync(label);
    }
    catch
    {
      transaction.Cancel();
      transaction.Dispose();
      throw;
    }
    transaction.Dispose();
    RaiseChanged(EditorChange.Model);
    PostStatusMessage("Added label");
    return label;
  }
}
