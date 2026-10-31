using System.Threading.Tasks;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Restore Default Map Settings (ticket 129).
public sealed partial class EditorSession
{
  /// <summary>
  ///   Asks for confirmation (reference wording) and on Yes resets the map settings to their defaults as one undo
  ///   step. Returns true when the settings were reset.
  /// </summary>
  public async Task<bool> RestoreDefaultMapSettingsAsync()
  {
    var yes = await Prompts.ConfirmAsync(
      "Restore default settings?\n\nThis will revert any changes to settings in this project.",
      "Restore Default Settings");
    if (!yes) return false;

    Execute("Restore default map settings", () => Settings.Reset());
    return true;
  }
}
