using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Trizbort.UI.Controls {
  public class TrizbortTextBox : TextBox {
    private string _mCue;

    public string Watermark {
      get => _mCue;
      set { _mCue = value; updateCue(); }
    }

    private void updateCue() {
      if (IsHandleCreated && _mCue != null) {
        SendMessage(Handle, EM_SETCUEBANNER, (IntPtr) 1, _mCue);
      }
    }

    protected override void OnHandleCreated(EventArgs e) {
      base.OnHandleCreated(e);
      updateCue();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);
    private const int EM_SETCUEBANNER = 0x1501;
  }
}