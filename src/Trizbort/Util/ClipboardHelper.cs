using Trizbort.UI;

namespace Trizbort.Util;

public static class ClipboardHelper {
  public static bool HasSomethingToPaste()
  {
    return !string.IsNullOrEmpty(UserInteraction.GetClipboardText());
  }
}