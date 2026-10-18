using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Trizbort.UI.Controls;

public partial class AutomapBar : UserControl
{
  public AutomapBar()
  {
    InitializeComponent();
  }

  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public string Status {
    set { _statusLabel.Text = value; }
  }

  public event EventHandler StopClick;

  private void StopButton_Click(object sender, EventArgs e)
  {
    var stopClick = StopClick;
    stopClick?.Invoke(this, EventArgs.Empty);
  }
}