using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Trizbort.UI.Controls {
  public class TrizbortTextBox : TextBox {
    private string _cue;

    public string Watermark {
      get => _cue;
      set { _cue = value; UpdateCue(); }
    }

    private void UpdateCue() {
      if (IsHandleCreated && _cue != null) {
        SendMessage(Handle, EmSetCueBanner, (IntPtr) 1, _cue);
      }
    }

    protected override void OnHandleCreated(EventArgs e) {
      base.OnHandleCreated(e);
      UpdateCue();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);
    private const int EmSetCueBanner = 0x1501;
  }
}