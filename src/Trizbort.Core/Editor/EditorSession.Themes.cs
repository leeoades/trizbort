using System;
using Trizbort.Setup;

namespace Trizbort.Editor;

public sealed partial class EditorSession
{
  /// <summary>Applies a map theme as one undo step.</summary>
  public void ApplyTheme(MapTheme theme, bool replaceIndividualStyles)
  {
    if (theme == null) throw new ArgumentNullException(nameof(theme));
    Execute("Apply theme", () => theme.Apply(replaceIndividualStyles));
    RaiseChanged(EditorChange.Model);
  }

  /// <summary>Applies an analysed room style inference as one undo step.</summary>
  public void ApplyRoomStyleInference(RoomStyleInference inference)
  {
    if (inference == null) throw new ArgumentNullException(nameof(inference));
    if (!inference.HasChanges) return;
    Execute("Infer default room style", inference.Apply);
    RaiseChanged(EditorChange.Model);
  }
}
