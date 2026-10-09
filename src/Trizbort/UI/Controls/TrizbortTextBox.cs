using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Trizbort.UI.Controls;

public class TrizbortTextBox : TextBox
{
  private const int EmSetCueBanner = 0x1501;
  private string _cue;

  public string Watermark {
    get { return _cue; }
    set {
      _cue = value;
      UpdateCue();
    }
  }

  private void UpdateCue()
  {
    if (IsHandleCreated && _cue != null) SendMessage(Handle, EmSetCueBanner, 1, _cue);
  }

  protected override void OnHandleCreated(EventArgs e)
  {
    base.OnHandleCreated(e);
    UpdateCue();
  }

  [DllImport("user32.dll", CharSet = CharSet.Unicode)]
  private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);
}