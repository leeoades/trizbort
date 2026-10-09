using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Trizbort.UI;

public partial class AboutDialog : Form {
  public AboutDialog()
  {
    InitializeComponent();
    try {
      _versionLabel.Text = $"Version {Assembly.GetExecutingAssembly().GetName().Version.ToString().Trim('.', '0')}";
    }
    catch (Exception) {
      // ignored
    }
  }

  private void OnLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
  {
    var label = (LinkLabel)sender;
    var url = label.Text.Substring(label.LinkArea.Start, label.LinkArea.Length);
    if (!url.StartsWith("http")) url = "http://" + url;
    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
  }
}