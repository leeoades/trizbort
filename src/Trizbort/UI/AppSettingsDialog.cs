using System;
using System.Windows.Forms;

namespace Trizbort.UI {
  public partial class AppSettingsDialog : Form {
    public AppSettingsDialog() {
      InitializeComponent();
    }

    public string DefaultFontName { get => _txtDefaultFontName.Text; set => _txtDefaultFontName.Text = value; }

    public int DefaultImageType { get => _cboImageSaveType.SelectedIndex; set => _cboImageSaveType.SelectedIndex = value; }

    public float GenHorizontalMargin { get => (float) _preferredHorizontalMargin.Value; set => _preferredHorizontalMargin.Value = (decimal) value; }

    public float GenVerticalMargin { get => (float) _preferredVerticalMargin.Value; set => _preferredVerticalMargin.Value = (decimal) value; }

    public bool ApplyStyleToNewRooms { get => _chkApplyStyleToNewRooms.Checked; set => _chkApplyStyleToNewRooms.Checked = value; }

    public bool DoubleClickToAddRoom { get => _chkDoubleClickToAddRoom.Checked; set => _chkDoubleClickToAddRoom.Checked = value; }

    public bool InvertMouseWheel { get => _invertWheelCheckBox.Checked; set => _invertWheelCheckBox.Checked = value; }

    public bool LoadLastProjectOnStart { get => _chkLoadLast.Checked; set => _chkLoadLast.Checked = value; }

    public int PortAdjustDetail { get => _cboPortAdjustDetail.SelectedIndex; set => _cboPortAdjustDetail.SelectedIndex = value; }

    public bool SaveAt100 { get => _chkSaveAtZoom.Checked; set => _chkSaveAtZoom.Checked = value; }

    public bool SaveTadsToAdv3Lite { get => _chkSaveTADSToADV3Lite.Checked; set => _chkSaveTADSToADV3Lite.Checked = value; }

    public bool SaveToImage { get => _chkSaveToImage.Checked; set => _chkSaveToImage.Checked = value; }

    public bool SaveToPDF { get => _chkSaveToPDF.Checked; set => _chkSaveToPDF.Checked = value; }

    public bool ShowFullPathInTitleBar { get => _chkFullPathTitleBar.Checked; set => _chkFullPathTitleBar.Checked = value; }

    public bool ShowDescriptionsInTooltip { get => _chkShowDescriptionsInTooltip.Checked; set => _chkShowDescriptionsInTooltip.Checked = value; }
    public bool ShowObjectsInTooltip { get => _chkShowObjectsInTooltip.Checked; set => _chkShowObjectsInTooltip.Checked = value; }

    public bool SpecifyGenMargins { get => _chkSpecifyMargins.Checked; set => _chkSpecifyMargins.Checked = value; }

    public bool LimitConnectionDescriptionCharactersInTooltip
    {
      get => _chkLimitConnectionDescriptionTooltipChars.Checked;
      set => _chkLimitConnectionDescriptionTooltipChars.Checked = value;
    }

    public int ToolTipConnectionDescriptionCharactersToShow
    {
      get => (int)_txtNumOfConnectionDescriptionChars.Value;
      set => _txtNumOfConnectionDescriptionChars.Value = value;
    }

    public bool LimitRoomDescriptionCharactersInTooltip
    {
      get => _chkLimitRoomDescriptionTooltipChars.Checked;
      set => _chkLimitRoomDescriptionTooltipChars.Checked = value;
    }
    public int ToolTipRoomDescriptionCharactersToShow
    {
      get => (int)_txtNumOfRoomDescriptionChars.Value;
      set => _txtNumOfRoomDescriptionChars.Value = value;
    }

		private void CboImageSaveTypeEnter(object sender, EventArgs e) {
      _cboImageSaveType.DroppedDown = true;
    }

    private void CboPortAdjustDetailEnter(object sender, EventArgs e) {
      _cboPortAdjustDetail.DroppedDown = true;
    }

		private void ChkLimitDescriptionTooltipCharsCheckedChanged(object sender, EventArgs e)
    {
      var checkBox = (CheckBox)sender;

      SetTooltipRoomDesciptionLimitUI(checkBox.Checked);
    }

    private void SetTooltipRoomDesciptionLimitUI(bool areWeLimiting)
    {
      _chkLimitRoomDescriptionTooltipChars.Enabled = areWeLimiting;
      _txtNumOfRoomDescriptionChars.Enabled = areWeLimiting;
    }    
    
    private void SetTooltipConnectionDesciptionLimitUI(bool areWeLimiting)
    {
      _chkLimitConnectionDescriptionTooltipChars.Enabled = areWeLimiting;
      _txtNumOfConnectionDescriptionChars.Enabled = areWeLimiting;
    }

    private void AppSettingsDialog_Load(object sender, EventArgs e)
    {
      SetTooltipRoomDesciptionLimitUI(_chkShowDescriptionsInTooltip.Checked);
      SetTooltipConnectionDesciptionLimitUI(_chkShowDescriptionsInTooltip.Checked);
    }

		private void ChkLimitConnectionDescriptionTooltipCharsCheckedChanged(object sender, EventArgs e)
		{
      var checkBox = (CheckBox)sender;

      SetTooltipConnectionDesciptionLimitUI(checkBox.Checked);
		}

		private void ChkShowDescriptionsInTooltipCheckedChanged(object sender, EventArgs e)
    {
      SetTooltipRoomDesciptionLimitUI(_chkShowDescriptionsInTooltip.Checked);
      SetTooltipConnectionDesciptionLimitUI(_chkShowDescriptionsInTooltip.Checked);
		}
    }
}