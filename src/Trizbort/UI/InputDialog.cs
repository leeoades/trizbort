/*
 * Author: Tony Brix, http://tonybrix.info
 * License: MIT
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Trizbort.UI;

public enum InputBoxButtons {
  Ok,
  OkCancel,
  YesNo,
  YesNoCancel,
  Save,
  SaveCancel
}

public enum InputBoxResult {
  Cancel,
  Ok,
  Yes,
  No,
  Save
}

public struct InputDialogItem {
  public string Label;
  public string Text;
  public bool IsPassword;

  public InputDialogItem(string label)
  {
    Label = label;
    Text = "";
    IsPassword = false;
  }

  public InputDialogItem(string label, string text)
  {
    Label = label;
    Text = text;
    IsPassword = false;
  }

  public InputDialogItem(string label, bool isPassword)
  {
    Label = label;
    Text = "";
    IsPassword = isPassword;
  }

  public InputDialogItem(string label, string text, bool isPassword)
  {
    Label = label;
    Text = text;
    IsPassword = isPassword;
  }
}

public class InputDialog {
  private InputDialog(DialogForm dialog)
  {
    Result = dialog.InputResult;
    Items = new Dictionary<string, string>();
    for (var i = 0; i < dialog.Label.Length; i++) Items.Add(dialog.Label[i].Text, dialog.TextBox[i].Text);
  }

  public Dictionary<string, string> Items { get; }

  public InputBoxResult Result { get; }

  public static InputDialog Show(string title, string label)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label) }, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, string label, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label) }, buttons);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, string label, string text)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label, text) }, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, string label, string text, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label, text) }, buttons);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, string[] labels)
  {
    var items = new InputDialogItem[labels.Length];
    for (var i = 0; i < labels.Length; i++) items[i] = new InputDialogItem(labels[i]);

    var dialog = new DialogForm(title, items, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, string[] labels, InputBoxButtons buttons)
  {
    var items = new InputDialogItem[labels.Length];
    for (var i = 0; i < labels.Length; i++) items[i] = new InputDialogItem(labels[i]);

    var dialog = new DialogForm(title, items, buttons);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, InputDialogItem item)
  {
    var dialog = new DialogForm(title, new[] { item }, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, InputDialogItem item, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, new[] { item }, buttons);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, InputDialogItem[] items)
  {
    var dialog = new DialogForm(title, items, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(string title, InputDialogItem[] items, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, items, buttons);
    dialog.StartPosition = FormStartPosition.CenterScreen;
    UserInteraction.ShowDialog(dialog);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, string label)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label) }, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, string label, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label) }, buttons);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, string label, string text)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label, text) }, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, string label, string text, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, new[] { new InputDialogItem(label, text) }, buttons);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, string[] labels)
  {
    var items = new InputDialogItem[labels.Length];
    for (var i = 0; i < labels.Length; i++) items[i] = new InputDialogItem(labels[i]);

    var dialog = new DialogForm(title, items, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, string[] labels, InputBoxButtons buttons)
  {
    var items = new InputDialogItem[labels.Length];
    for (var i = 0; i < labels.Length; i++) items[i] = new InputDialogItem(labels[i]);

    var dialog = new DialogForm(title, items, buttons);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, InputDialogItem item)
  {
    var dialog = new DialogForm(title, new[] { item }, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, InputDialogItem item, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, new[] { item }, buttons);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, InputDialogItem[] items)
  {
    var dialog = new DialogForm(title, items, InputBoxButtons.Ok);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  public static InputDialog Show(IWin32Window window, string title, InputDialogItem[] items, InputBoxButtons buttons)
  {
    var dialog = new DialogForm(title, items, buttons);
    UserInteraction.ShowDialog(dialog, window);
    return new InputDialog(dialog);
  }

  private class DialogForm : Form {
    private readonly Button _button1;
    private readonly Button _button2;
    private readonly Button _button3;
    public readonly Label[] Label;
    public readonly TextBox[] TextBox;

    public DialogForm(string title, InputDialogItem[] items, InputBoxButtons buttons)
    {
      var minWidth = 312;
      Label = new Label[items.Length];
      for (var i = 0; i < Label.Length; i++) Label[i] = new Label();
      TextBox = new TextBox[items.Length];
      for (var i = 0; i < TextBox.Length; i++) TextBox[i] = new TextBox();
      _button2 = new Button();
      _button3 = new Button();
      _button1 = new Button();
      SuspendLayout();
      // 
      // label
      // 
      for (var i = 0; i < items.Length; i++) {
        Label[i].AutoSize = true;
        Label[i].Location = new Point(12, 9 + i * 39);
        Label[i].Name = "label[" + i + "]";
        Label[i].Text = items[i].Label;
        if (Label[i].Width > minWidth) minWidth = Label[i].Width;
      }

      // 
      // textBox
      // 
      for (var i = 0; i < items.Length; i++) {
        TextBox[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        TextBox[i].Location = new Point(12, 25 + i * 39);
        TextBox[i].Name = "textBox[" + i + "]";
        TextBox[i].Size = new Size(288, 20);
        TextBox[i].TabIndex = i;
        TextBox[i].Text = items[i].Text;
        if (items[i].IsPassword) TextBox[i].UseSystemPasswordChar = true;
      }

      // 
      // button1
      // 
      _button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      _button1.Location = new Point(208, 15 + 39 * Label.Length);
      _button1.Name = "button1";
      _button1.Size = new Size(92, 23);
      _button1.TabIndex = items.Length + 2;
      _button1.Text = "button1";
      _button1.UseVisualStyleBackColor = true;
      // 
      // button2
      // 
      _button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      _button2.Location = new Point(110, 15 + 39 * Label.Length);
      _button2.Name = "button2";
      _button2.Size = new Size(92, 23);
      _button2.TabIndex = items.Length + 1;
      _button2.Text = "button2";
      _button2.UseVisualStyleBackColor = true;
      // 
      // button3
      // 
      _button3.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      _button3.Location = new Point(12, 15 + 39 * Label.Length);
      _button3.Name = "button3";
      _button3.Size = new Size(92, 23);
      _button3.TabIndex = items.Length;
      _button3.Text = "button3";
      _button3.UseVisualStyleBackColor = true;
      //
      // Evaluate MessageBoxButtons
      //
      switch (buttons) {
        case InputBoxButtons.Ok:
          _button1.Text = "OK";
          _button1.Click += OK_Click;
          _button2.Visible = false;
          _button3.Visible = false;
          AcceptButton = _button1;
          break;
        case InputBoxButtons.OkCancel:
          _button1.Text = "Cancel";
          _button1.Click += Cancel_Click;
          _button2.Text = "OK";
          _button2.Click += OK_Click;
          _button3.Visible = false;
          AcceptButton = _button2;
          break;
        case InputBoxButtons.YesNo:
          _button1.Text = "No";
          _button1.Click += No_Click;
          _button2.Text = "Yes";
          _button2.Click += Yes_Click;
          _button3.Visible = false;
          AcceptButton = _button2;
          break;
        case InputBoxButtons.YesNoCancel:
          _button1.Text = "Cancel";
          _button1.Click += Cancel_Click;
          _button2.Text = "No";
          _button2.Click += No_Click;
          _button3.Text = "Yes";
          _button3.Click += Yes_Click;
          AcceptButton = _button3;
          break;
        case InputBoxButtons.Save:
          _button1.Text = "Save";
          _button1.Click += Save_Click;
          _button2.Visible = false;
          _button3.Visible = false;
          AcceptButton = _button1;
          break;
        case InputBoxButtons.SaveCancel:
          _button1.Text = "Cancel";
          _button1.Click += Cancel_Click;
          _button2.Text = "Save";
          _button2.Click += Save_Click;
          _button3.Visible = false;
          AcceptButton = _button2;
          break;
        default:
          throw new Exception("Invalid InputBoxButton Value");
      }

      // 
      // dialogForm
      // 
      AutoScaleDimensions = new SizeF(6F, 13F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(312, 47 + 39 * items.Length);
      for (var i = 0; i < Label.Length; i++) Controls.Add(Label[i]);
      for (var i = 0; i < TextBox.Length; i++) Controls.Add(TextBox[i]);
      Controls.Add(_button1);
      Controls.Add(_button2);
      Controls.Add(_button3);
      MaximizeBox = false;
      MinimizeBox = false;
      MaximumSize = new Size(99999, 85 + 39 * items.Length);
      Name = "dialogForm";
      ShowIcon = false;
      ShowInTaskbar = false;
      Text = title;
      ResumeLayout(false);
      PerformLayout();
      foreach (var l in Label)
        if (l.Width > minWidth)
          minWidth = l.Width;

      ClientSize = new Size(minWidth + 24, 47 + 39 * items.Length);
      MinimumSize = new Size(minWidth + 40, 85 + 39 * items.Length);
    }

    public InputBoxResult InputResult { get; private set; } = InputBoxResult.Cancel;

    private void OK_Click(object sender, EventArgs e)
    {
      InputResult = InputBoxResult.Ok;
      Close();
    }

    private void Cancel_Click(object sender, EventArgs e)
    {
      InputResult = InputBoxResult.Cancel;
      Close();
    }

    private void Yes_Click(object sender, EventArgs e)
    {
      InputResult = InputBoxResult.Yes;
      Close();
    }

    private void No_Click(object sender, EventArgs e)
    {
      InputResult = InputBoxResult.No;
      Close();
    }

    private void Save_Click(object sender, EventArgs e)
    {
      InputResult = InputBoxResult.Save;
      Close();
    }
  }
}