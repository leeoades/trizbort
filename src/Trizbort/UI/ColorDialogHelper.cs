using System.Drawing;
using System.Windows.Forms;

namespace Trizbort.UI;

public static class ColorDialogHelper
{
  public static Color ShowColorDialog(Color color, Form parent = null)
  {
    using var dialog = new ColorDialog();
    dialog.Color = color == Color.Transparent ? Color.White : color;

    return UserInteraction.ShowDialog(dialog, parent) == DialogResult.OK ? dialog.Color : color;
  }
}
