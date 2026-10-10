using System;
using System.Windows.Forms;
using Trizbort.Automap;
using Trizbort.Domain.AppSettings;
using Trizbort.Util;

namespace Trizbort.UI;

internal partial class AutomapDialog : Form
{
  public AutomapDialog()
  {
    InitializeComponent();
    #if DEBUG
    _singleStepCheckBox.Visible = true;
    #else
      _singleStepCheckBox.Visible = false;
    #endif
    Data = ApplicationSettingsController.AppSettings.Automap;
  }

  public AutomapSettings Data {
    get {
      var settings = new AutomapSettings {
        FileName = _textBox.Text,
        SingleStep = _singleStepCheckBox.Checked,
        VerboseTranscript = _verboseTranscriptCheckBox.Checked,
        AssumeRoomsWithSameNameAreSameRoom =
          !_verboseTranscriptCheckBox.Checked || _roomsWithSameNameAreSameRoomCheckBox.Checked,
        GuessExits = _guessExitsCheckBox.Checked,
        AddObjectCommand = _addObjectCommandTextBox.Text,
        AddRegionCommand = _addRegionCommandTextBox.Text,
        ContinueTranscript = _startFromEndCheckBox.Checked,
        AssumeTwoWayConnections = _chkAssumeTwoWayConnections.Checked
      };
      return settings;
    }
    set {
      _textBox.Text = value.FileName;
      _singleStepCheckBox.Checked = value.SingleStep;
      _verboseTranscriptCheckBox.Checked = value.VerboseTranscript;
      _roomsWithSameNameAreSameRoomCheckBox.Enabled = _verboseTranscriptCheckBox.Enabled;
      _roomsWithSameNameAreSameRoomCheckBox.Checked =
        _roomsWithSameNameAreSameRoomCheckBox.Enabled && value.AssumeRoomsWithSameNameAreSameRoom;
      _guessExitsCheckBox.Checked = value.GuessExits;
      _addObjectCommandTextBox.Text = value.AddObjectCommand;
      _addRegionCommandTextBox.Text = value.AddRegionCommand;
      _startFromEndCheckBox.Checked = value.ContinueTranscript;
      _chkAssumeTwoWayConnections.Checked = value.AssumeTwoWayConnections;
    }
  }

  protected override void OnClosed(EventArgs e)
  {
    if (DialogResult == DialogResult.OK)
      ApplicationSettingsController.AppSettings.Automap = Data;
    base.OnClosed(e);
  }

  private void BrowseButton_Click(object sender, EventArgs e)
  {
    using var dialog = new OpenFileDialog();
    dialog.Filter = "Text and Log Files(*.txt, *.log)|*.txt;*.log|All Files|*.*||";
    dialog.Title = "Open Transcript";
    dialog.FileName = _textBox.Text;
    dialog.InitialDirectory = PathHelper.SafeGetDirectoryName(_textBox.Text);
    if (UserInteraction.ShowDialog(dialog) == DialogResult.OK)
      _textBox.Text = dialog.FileName;
  }

  private void VerboseTranscriptCheckBox_CheckedChanged(object sender, EventArgs e)
  {
    _roomsWithSameNameAreSameRoomCheckBox.Enabled = _verboseTranscriptCheckBox.Checked;
    _roomsWithSameNameAreSameRoomCheckBox.Checked = !_verboseTranscriptCheckBox.Checked;
  }

  private void Label5_Click(object sender, EventArgs e)
  {
  }
}