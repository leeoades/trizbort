using System;
using System.Windows.Forms;

namespace Trizbort.UI;

internal interface IUserInteraction
{
  DialogResult ShowMessage(
    IWin32Window owner,
    string text,
    string caption,
    MessageBoxButtons buttons,
    MessageBoxIcon icon,
    MessageBoxDefaultButton defaultButton);

  DialogResult ShowDialog(Form dialog, IWin32Window owner);
  DialogResult ShowDialog(CommonDialog dialog, IWin32Window owner);
  string GetClipboardText();
  void SetClipboardText(string text, TextDataFormat format);
}

internal static class UserInteraction
{
  private static IUserInteraction _current = new WindowsUserInteraction();

  internal static IUserInteraction Current {
    get { return _current; }
    set { _current = value ?? throw new ArgumentNullException(nameof(value)); }
  }

  public static DialogResult ShowMessage(
    string text,
    string caption = "",
    MessageBoxButtons buttons = MessageBoxButtons.OK,
    MessageBoxIcon icon = MessageBoxIcon.None,
    MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
  {
    return Current.ShowMessage(null, text, caption, buttons, icon, defaultButton);
  }

  public static DialogResult ShowMessage(
    IWin32Window owner,
    string text,
    string caption = "",
    MessageBoxButtons buttons = MessageBoxButtons.OK,
    MessageBoxIcon icon = MessageBoxIcon.None,
    MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
  {
    return Current.ShowMessage(owner, text, caption, buttons, icon, defaultButton);
  }

  public static DialogResult ShowDialog(Form dialog, IWin32Window owner = null)
  {
    return Current.ShowDialog(dialog, owner);
  }

  public static DialogResult ShowDialog(CommonDialog dialog, IWin32Window owner = null)
  {
    return Current.ShowDialog(dialog, owner);
  }

  public static string GetClipboardText()
  {
    return Current.GetClipboardText();
  }

  public static void SetClipboardText(string text, TextDataFormat format = TextDataFormat.UnicodeText)
  {
    Current.SetClipboardText(text, format);
  }

  private sealed class WindowsUserInteraction : IUserInteraction
  {
    public DialogResult ShowMessage(
      IWin32Window owner,
      string text,
      string caption,
      MessageBoxButtons buttons,
      MessageBoxIcon icon,
      MessageBoxDefaultButton defaultButton)
    {
      return owner == null
        ? MessageBox.Show(text, caption, buttons, icon, defaultButton)
        : MessageBox.Show(owner, text, caption, buttons, icon, defaultButton);
    }

    public DialogResult ShowDialog(Form dialog, IWin32Window owner)
    {
      return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    public DialogResult ShowDialog(CommonDialog dialog, IWin32Window owner)
    {
      return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    public string GetClipboardText()
    {
      return Clipboard.GetText();
    }

    public void SetClipboardText(string text, TextDataFormat format)
    {
      Clipboard.SetText(text, format);
    }
  }
}