using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Trizbort.UI {
  public class OnlineHelpDialog : Form {
    public const string UserGuideUrl = "https://github.com/leeoades/trizbort/blob/master/Docs/index.md";
    public const string OriginalHelpUrl = "https://trizbort.genstein.net/help/";

    public OnlineHelpDialog() {
      Text = "Online Help";
      FormBorderStyle = FormBorderStyle.FixedDialog;
      StartPosition = FormStartPosition.CenterParent;
      MaximizeBox = false;
      MinimizeBox = false;
      ShowInTaskbar = false;
      AutoScaleMode = AutoScaleMode.Font;
      AutoSize = true;
      AutoSizeMode = AutoSizeMode.GrowAndShrink;

      var layout = new TableLayoutPanel {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 1,
        Dock = DockStyle.Fill,
        Padding = new Padding(12)
      };
      layout.Controls.Add(new Label {
        AutoSize = true,
        MaximumSize = new Size(440, 0),
        Text = "The Trizbort v2 user guide is available online:",
        Margin = new Padding(3, 3, 3, 8)
      });
      layout.Controls.Add(CreateLink(UserGuideUrl));
      layout.Controls.Add(new Label {
        AutoSize = true,
        MaximumSize = new Size(440, 0),
        Text = "The original Trizbort v1 help, on which the guide is based, can be found here:",
        Margin = new Padding(3, 16, 3, 8)
      });
      layout.Controls.Add(CreateLink(OriginalHelpUrl));

      var closeButton = new Button {
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        DialogResult = DialogResult.OK,
        Text = "Close",
        Margin = new Padding(3, 16, 3, 3)
      };
      layout.Controls.Add(closeButton);
      AcceptButton = closeButton;
      CancelButton = closeButton;
      Controls.Add(layout);
    }

    private LinkLabel CreateLink(string url) {
      var link = new LinkLabel {
        AutoSize = true,
        Text = url
      };
      link.LinkClicked += (sender, e) => {
        try {
          Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
          link.LinkVisited = true;
        }
        catch (Win32Exception ex) {
          UserInteraction.ShowMessage(this, $"Unable to open the link:\n\n{url}\n\n{ex.Message}",
                          Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      };
      return link;
    }
  }
}
