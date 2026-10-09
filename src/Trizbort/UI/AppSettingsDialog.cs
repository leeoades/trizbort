using System;
using System.Windows.Forms;

namespace Trizbort.UI;

public partial class AppSettingsDialog : Form {
  public AppSettingsDialog()
  {
    InitializeComponent();
  }

  public string DefaultFontName {
    get { return _txtDefaultFontName.Text; }
    set { _txtDefaultFontName.Text = value; }
  }

  public int DefaultImageType {
    get { return _cboImageSaveType.SelectedIndex; }
    set { _cboImageSaveType.SelectedIndex = value; }
  }

  public float GenHorizontalMargin {
    get { return (float)_preferredHorizontalMargin.Value; }
    set { _preferredHorizontalMargin.Value = (decimal)value; }
  }

  public float GenVerticalMargin {
    get { return (float)_preferredVerticalMargin.Value; }
    set { _preferredVerticalMargin.Value = (decimal)value; }
  }

  public bool ApplyStyleToNewRooms {
    get { return _chkApplyStyleToNewRooms.Checked; }
    set { _chkApplyStyleToNewRooms.Checked = value; }
  }

  public bool DoubleClickToAddRoom {
    get { return _chkDoubleClickToAddRoom.Checked; }
    set { _chkDoubleClickToAddRoom.Checked = value; }
  }

  public bool InvertMouseWheel {
    get { return _invertWheelCheckBox.Checked; }
    set { _invertWheelCheckBox.Checked = value; }
  }

  public bool LoadLastProjectOnStart {
    get { return _chkLoadLast.Checked; }
    set { _chkLoadLast.Checked = value; }
  }

  public int PortAdjustDetail {
    get { return _cboPortAdjustDetail.SelectedIndex; }
    set { _cboPortAdjustDetail.SelectedIndex = value; }
  }

  public bool SaveAt100 {
    get { return _chkSaveAtZoom.Checked; }
    set { _chkSaveAtZoom.Checked = value; }
  }

  public bool SaveTadsToAdv3Lite {
    get { return _chkSaveTADSToADV3Lite.Checked; }
    set { _chkSaveTADSToADV3Lite.Checked = value; }
  }

  public bool SaveToImage {
    get { return _chkSaveToImage.Checked; }
    set { _chkSaveToImage.Checked = value; }
  }

  public bool SaveToPDF {
    get { return _chkSaveToPDF.Checked; }
    set { _chkSaveToPDF.Checked = value; }
  }

  public bool ShowFullPathInTitleBar {
    get { return _chkFullPathTitleBar.Checked; }
    set { _chkFullPathTitleBar.Checked = value; }
  }

  public bool ShowDescriptionsInTooltip {
    get { return _chkShowDescriptionsInTooltip.Checked; }
    set { _chkShowDescriptionsInTooltip.Checked = value; }
  }

  public bool ShowObjectsInTooltip {
    get { return _chkShowObjectsInTooltip.Checked; }
    set { _chkShowObjectsInTooltip.Checked = value; }
  }

  public bool SpecifyGenMargins {
    get { return _chkSpecifyMargins.Checked; }
    set { _chkSpecifyMargins.Checked = value; }
  }

  public bool LimitConnectionDescriptionCharactersInTooltip {
    get { return _chkLimitConnectionDescriptionTooltipChars.Checked; }
    set { _chkLimitConnectionDescriptionTooltipChars.Checked = value; }
  }

  public int ToolTipConnectionDescriptionCharactersToShow {
    get { return (int)_txtNumOfConnectionDescriptionChars.Value; }
    set { _txtNumOfConnectionDescriptionChars.Value = value; }
  }

  public bool LimitRoomDescriptionCharactersInTooltip {
    get { return _chkLimitRoomDescriptionTooltipChars.Checked; }
    set { _chkLimitRoomDescriptionTooltipChars.Checked = value; }
  }

  public int ToolTipRoomDescriptionCharactersToShow {
    get { return (int)_txtNumOfRoomDescriptionChars.Value; }
    set { _txtNumOfRoomDescriptionChars.Value = value; }
  }

  private void CboImageSaveTypeEnter(object sender, EventArgs e)
  {
    _cboImageSaveType.DroppedDown = true;
  }

  private void CboPortAdjustDetailEnter(object sender, EventArgs e)
  {
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