using System;
using System.Drawing;
using System.Windows.Forms;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.UI;

public partial class ConnectionPropertiesDialog : Form
{
  private const string NoColorSet = "No Color Set";


  public ConnectionPropertiesDialog()
  {
    InitializeComponent();
  }

  public Color ConnectionColor {
    get { return _connectionColorBox.Text == NoColorSet ? Color.Transparent : _connectionColorBox.BackColor; }
    set {
      if (value == Color.Transparent)
      {
        _connectionColorBox.BackColor = Color.White;
        _connectionColorBox.Text = NoColorSet;
      }
      else
      {
        _connectionColorBox.BackColor = value;
        _connectionColorBox.Text = string.Empty;
      }
    }
  }

  public string ConnectionDescription {
    get { return _txtDescription.Text; }
    set {
      _txtDescription.Text = value;
      UpdateControls();
    }
  }

  public string ConnectionName {
    get { return _txtName.Text; }
    set {
      _txtName.Text = value;
      UpdateControls();
    }
  }

  public Door Door {
    get {
      return _chkDoor.Checked
        ? new Door {
          Lockable = _chkLockable.Checked, Locked = _chkLocked.Checked, Open = _chkOpen.Checked,
          Openable = _chkOpenable.Checked
        }
        : null;
    }
    set {
      if (value != null)
      {
        _chkDoor.Checked = true;
        _chkLockable.Checked = value.Lockable;
        _chkLocked.Checked = value.Locked;
        _chkOpen.Checked = value.Open;
        _chkOpenable.Checked = value.Openable;
      }
    }
  }

  public string EndText {
    get { return _endTextBox.Text; }
    set {
      _endTextBox.Text = value;
      UpdateControls();
    }
  }


  public bool IsDirectional {
    get { return _oneWayCheckBox.Checked; }
    set { _oneWayCheckBox.Checked = value; }
  }

  public bool IsDotted {
    get { return _dottedCheckBox.Checked; }
    set { _dottedCheckBox.Checked = value; }
  }

  public string MidText {
    get { return _middleTextBox.Text; }
    set {
      _middleTextBox.Text = value;
      UpdateControls();
    }
  }

  public string StartText {
    get { return _startTextBox.Text; }
    set {
      _startTextBox.Text = value;
      UpdateControls();
    }
  }

  protected override void OnLoad(EventArgs e)
  {
    base.OnLoad(e);
    // WinForms DPI scaling shrinks auto-sized check boxes and radio buttons inside group boxes a second
    // time, clipping their text; toggling AutoSize makes them measure themselves again.
    foreach (var group in new Control[] { _groupBox1, _groupBox2 })
      foreach (Control control in group.Controls)
        if (control is ButtonBase && control.AutoSize)
        {
          control.AutoSize = false;
          control.AutoSize = true;
        }
  }

  private void ChangeConnectionColor()
  {
    ConnectionColor = Colors.ShowColorDialog(ConnectionColor, this);
  }

  private void ChkDoorCheckedChanged(object sender, EventArgs e)
  {
    _chkOpen.Enabled = _chkDoor.Checked;
    _chkLockable.Enabled = _chkDoor.Checked;
    _chkLocked.Enabled = _chkDoor.Checked;
    _chkOpenable.Enabled = _chkDoor.Checked;
  }


  private void ConnectionColorBoxDoubleClick(object sender, EventArgs e)
  {
    ChangeConnectionColor();
  }

  private void ConnectionColorChangeClick(object sender, EventArgs e)
  {
    ChangeConnectionColor();
  }

  private bool MatchText(ConnectionLabel label)
  {
    Connection.GetText(label, out var start, out var end);
    return StartText == start && EndText == end && string.IsNullOrEmpty(MidText);
  }

  private void OnRadioButtonCheckedChanged(object sender, EventArgs e)
  {
    if (_udRadioButton.Checked)
      SetText(ConnectionLabel.Up);
    else if (_duRadioButton.Checked)
      SetText(ConnectionLabel.Down);
    else if (_ioRadioButton.Checked)
      SetText(ConnectionLabel.In);
    else if (_oiRadioButton.Checked) SetText(ConnectionLabel.Out);
  }

  private void SetText(ConnectionLabel label)
  {
    Connection.GetText(label, out var start, out var end);
    StartText = start;
    EndText = end;
  }

  private void UpdateControls()
  {
    if (MatchText(ConnectionLabel.Up))
      _udRadioButton.Checked = true;
    else if (MatchText(ConnectionLabel.Down))
      _duRadioButton.Checked = true;
    else if (MatchText(ConnectionLabel.In))
      _ioRadioButton.Checked = true;
    else if (MatchText(ConnectionLabel.Out))
      _oiRadioButton.Checked = true;
    else
      _customRadioButton.Checked = true;
  }

  private void ConnectionColorClearClick(object sender, EventArgs e)
  {
    ConnectionColor = Color.Transparent;
  }

  private void ConnectionColorBoxEnter(object sender, EventArgs e)
  {
    _connectionColorChange.Focus();
  }
}