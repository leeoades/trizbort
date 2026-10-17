using System.Windows.Forms;
using Trizbort.Domain.Enums;

namespace Trizbort.Extensions;

public static class KeysExtensions
{
  public static ModifierKeys ToModifierKeys(this Keys keys)
  {
    var result = ModifierKeys.None;
    if ((keys & Keys.Shift) == Keys.Shift) result |= ModifierKeys.Shift;
    if ((keys & Keys.Control) == Keys.Control) result |= ModifierKeys.Control;
    if ((keys & Keys.Alt) == Keys.Alt) result |= ModifierKeys.Alt;
    return result;
  }
}
