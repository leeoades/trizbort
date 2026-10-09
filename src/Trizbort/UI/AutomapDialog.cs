using System;
using System.Windows.Forms;
using Trizbort.Automap;
using Trizbort.Domain.AppSettings;
using Trizbort.Util;

namespace Trizbort.UI
{
  internal partial class AutomapDialog : Form
  {
    public AutomapDialog()
    {
      InitializeComponent();
#if DEBUG
      m_singleStepCheckBox.Visible = true;
#else
            m_singleStepCheckBox.Visible = false;
#endif
      Data = ApplicationSettingsController.AppSettings.Automap;
    }

    public AutomapSettings Data
    {
      get
      {
        var settings = new AutomapSettings
        {
          FileName = m_textBox.Text,
          SingleStep = m_singleStepCheckBox.Checked,
          VerboseTranscript = m_verboseTranscriptCheckBox.Checked,
          AssumeRoomsWithSameNameAreSameRoom = !m_verboseTranscriptCheckBox.Checked || m_roomsWithSameNameAreSameRoomCheckBox.Checked,
          GuessExits = m_guessExitsCheckBox.Checked,
          AddObjectCommand = m_addObjectCommandTextBox.Text,
          AddRegionCommand = m_addRegionCommandTextBox.Text,
          ContinueTranscript = m_startFromEndCheckBox.Checked,
          AssumeTwoWayConnections = chkAssumeTwoWayConnections.Checked
        };
        return settings;
      }
      set
      {
        m_textBox.Text = value.FileName;
        m_singleStepCheckBox.Checked = value.SingleStep;
        m_verboseTranscriptCheckBox.Checked = value.VerboseTranscript;
        m_roomsWithSameNameAreSameRoomCheckBox.Enabled = m_verboseTranscriptCheckBox.Enabled;
        m_roomsWithSameNameAreSameRoomCheckBox.Checked = m_roomsWithSameNameAreSameRoomCheckBox.Enabled && value.AssumeRoomsWithSameNameAreSameRoom;
        m_guessExitsCheckBox.Checked = value.GuessExits;
        m_addObjectCommandTextBox.Text = value.AddObjectCommand;
        m_addRegionCommandTextBox.Text = value.AddRegionCommand;
        m_startFromEndCheckBox.Checked = value.ContinueTranscript;
        chkAssumeTwoWayConnections.Checked = value.AssumeTwoWayConnections;
      }
    }

    protected override void OnClosed(EventArgs e)
    {
      if (DialogResult == DialogResult.OK)
        ApplicationSettingsController.AppSettings.Automap = Data;
      base.OnClosed(e);
    }

    private void BrowseButton_Click(object sender, EventArgs e) {
      using var dialog = new OpenFileDialog();
      dialog.Filter = "Text and Log Files(*.txt, *.log)|*.txt;*.log|All Files|*.*||";
      dialog.Title = "Open Transcript";
      dialog.FileName = m_textBox.Text;
      dialog.InitialDirectory = PathHelper.SafeGetDirectoryName(m_textBox.Text);
      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK)
        m_textBox.Text = dialog.FileName;
    }

    private void VerboseTranscriptCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      m_roomsWithSameNameAreSameRoomCheckBox.Enabled = m_verboseTranscriptCheckBox.Checked;
      m_roomsWithSameNameAreSameRoomCheckBox.Checked = !m_verboseTranscriptCheckBox.Checked;
    }

        private void Label5_Click(object sender, EventArgs e)
        {

        }
    }
}