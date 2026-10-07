using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Trizbort.UI {
  public class OnlineHelpDialog : Form {
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
        Text = "Online help is available for v1 of Trizbort. Some information may differ from v2.",
        Margin = new Padding(3, 3, 3, 8)
      });
      layout.Controls.Add(createLink("https://trizbort.genstein.net/help/"));
      layout.Controls.Add(new Label {
        AutoSize = true,
        Text = "For Trizbort v2, visit the project repository:",
        Margin = new Padding(3, 16, 3, 8)
      });
      layout.Controls.Add(createLink("https://github.com/leeoades/trizbort"));

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

    private LinkLabel createLink(string url) {
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
          MessageBox.Show(this, $"Unable to open the link:\n\n{url}\n\n{ex.Message}",
                          Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      };
      return link;
    }
  }
}
