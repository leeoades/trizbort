using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.Util;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.UI;

public partial class SettingsDialog : Form
{
  private const int HorizontalMargin = 2;
  private const int VerticalMargin = 2;
  private const int ColorSwatchWidth = 24;
  private readonly TextBox _editBox;
  private bool _bUpdatingRegionText;
  private Region _currentRegion;
  private int _itemSelected;
  private Font _largeFont;
  private Font _lineFont;
  private Font _smallFont;
  private Font _subtitleFont;

  public static void ShowMapDialog()
  {
    using var dialog = new SettingsDialog();
    for (var index = 0; index < Colors.Count; ++index) dialog.ElementColors[index] = Settings.Color[index];

    var regCount = Settings.Regions.Count;
    var regNameList = Settings.Regions.Select(p => p.RegionName).ToList();
    var regTextColorList = Settings.Regions.Select(p => p.TextColor).ToList();
    var regBkgdColorList = Settings.Regions.Select(p => p.RColor).ToList();

    dialog.Title = Project.Current.Title;
    dialog.Author = Project.Current.Author;
    dialog.Description = Project.Current.Description;
    dialog.History = Project.Current.History;
    dialog.DefaultRoomName = Settings.DefaultRoomName;
    dialog.LargeFont = Settings.RoomNameFont;
    dialog.SmallFont = Settings.ObjectFont;
    dialog.LineFont = Settings.LineFont;
    dialog.SubtitleFont = Settings.SubtitleFont;
    dialog.LineWidth = Settings.LineWidth;
    dialog.HandDrawn = Settings.HandDrawn;
    dialog.SnapToGrid = Settings.SnapToGrid;
    dialog.GridSize = Settings.GridSize;
    dialog.IsGridVisible = Settings.IsGridVisible;
    dialog.ShowOrigin = Settings.ShowOrigin;
    dialog.DarknessStripeSize = Settings.DarknessStripeSize;
    dialog.ObjectListOffsetFromRoom = Settings.ObjectListOffsetFromRoom;
    dialog.ConnectionStalkLength = Settings.ConnectionStalkLength;
    dialog.PreferredDistanceBetweenRooms = Settings.PreferredDistanceBetweenRooms;
    dialog.TextOffsetFromConnection = Settings.TextOffsetFromConnection;
    dialog.HandleSize = Settings.HandleSize;
    dialog.SnapToElementSize = Settings.SnapToElementSize;
    dialog.DocumentSpecificMargins = Settings.DocumentSpecificMargins;
    dialog.DocHorizontalMargin = Settings.DocHorizontalMargin;
    dialog.DocVerticalMargin = Settings.DocVerticalMargin;
    dialog.WrapTextAtDashes = Settings.WrapTextAtDashes;
    dialog.ConnectionArrowSize = Settings.ConnectionArrowSize;
    dialog.DefaultRoomShape = Settings.DefaultRoomShape;
    if (UserInteraction.ShowDialog(dialog) == DialogResult.OK)
    {
      for (var index = 0; index < Colors.Count; ++index)
      {
        if (Settings.Color[index] != dialog.ElementColors[index]) Project.Current.IsDirty = true;
        Settings.Color[index] = dialog.ElementColors[index];
      }

      if (Project.Current.Title != dialog.Title) Project.Current.IsDirty = true;
      Project.Current.Title = dialog.Title;
      if (Project.Current.Author != dialog.Author) Project.Current.IsDirty = true;
      Project.Current.Author = dialog.Author;
      if (Project.Current.Description != dialog.Description) Project.Current.IsDirty = true;
      Project.Current.Description = dialog.Description;
      if (Project.Current.History != dialog.History) Project.Current.IsDirty = true;
      Project.Current.History = dialog.History;
      if (Settings.DefaultRoomName != dialog.DefaultRoomName) Project.Current.IsDirty = true;
      Settings.DefaultRoomName = dialog.DefaultRoomName;
      if (!Equals(Settings.RoomNameFont, dialog.LargeFont)) Project.Current.IsDirty = true;
      Settings.RoomNameFont = dialog.LargeFont;
      if (!Equals(Settings.ObjectFont, dialog.SmallFont)) Project.Current.IsDirty = true;
      Settings.ObjectFont = dialog.SmallFont;
      if (!Equals(Settings.SubtitleFont, dialog.SubtitleFont)) Project.Current.IsDirty = true;
      Settings.SubtitleFont = dialog.SubtitleFont;
      if (!Equals(Settings.LineFont, dialog.LineFont)) Project.Current.IsDirty = true;
      Settings.LineFont = dialog.LineFont;
      if (Settings.LineWidth != dialog.LineWidth) Project.Current.IsDirty = true;
      Settings.LineWidth = dialog.LineWidth;
      if (Settings.HandDrawn != dialog.HandDrawn) Project.Current.IsDirty = true;
      Settings.HandDrawn = dialog.HandDrawn;
      if (Settings.SnapToGrid != dialog.SnapToGrid) Project.Current.IsDirty = true;
      Settings.SnapToGrid = dialog.SnapToGrid;
      if (Settings.GridSize != dialog.GridSize) Project.Current.IsDirty = true;
      Settings.GridSize = dialog.GridSize;
      if (Settings.IsGridVisible != dialog.IsGridVisible) Project.Current.IsDirty = true;
      Settings.IsGridVisible = dialog.IsGridVisible;
      if (Settings.ShowOrigin != dialog.ShowOrigin) Project.Current.IsDirty = true;
      Settings.ShowOrigin = dialog.ShowOrigin;
      if (Settings.DarknessStripeSize != dialog.DarknessStripeSize) Project.Current.IsDirty = true;
      Settings.DarknessStripeSize = dialog.DarknessStripeSize;
      if (Settings.ObjectListOffsetFromRoom != dialog.ObjectListOffsetFromRoom) Project.Current.IsDirty = true;
      Settings.ObjectListOffsetFromRoom = dialog.ObjectListOffsetFromRoom;
      if (Settings.ConnectionStalkLength != dialog.ConnectionStalkLength) Project.Current.IsDirty = true;
      Settings.ConnectionStalkLength = dialog.ConnectionStalkLength;
      if (Settings.PreferredDistanceBetweenRooms != dialog.PreferredDistanceBetweenRooms) Project.Current.IsDirty = true;
      Settings.PreferredDistanceBetweenRooms = dialog.PreferredDistanceBetweenRooms;
      if (Settings.TextOffsetFromConnection != dialog.TextOffsetFromConnection) Project.Current.IsDirty = true;
      Settings.TextOffsetFromConnection = dialog.TextOffsetFromConnection;
      if (Settings.HandleSize != dialog.HandleSize) Project.Current.IsDirty = true;
      Settings.HandleSize = dialog.HandleSize;
      if (Settings.SnapToElementSize != dialog.SnapToElementSize) Project.Current.IsDirty = true;
      Settings.SnapToElementSize = dialog.SnapToElementSize;
      if (Settings.ConnectionArrowSize != dialog.ConnectionArrowSize) Project.Current.IsDirty = true;
      Settings.ConnectionArrowSize = dialog.ConnectionArrowSize;
      if (Settings.DocumentSpecificMargins != dialog.DocumentSpecificMargins) Project.Current.IsDirty = true;
      Settings.DocumentSpecificMargins = dialog.DocumentSpecificMargins;
      if (Settings.DocHorizontalMargin != dialog.DocHorizontalMargin) Project.Current.IsDirty = true;
      Settings.DocHorizontalMargin = dialog.DocHorizontalMargin;
      if (Settings.DocVerticalMargin != dialog.DocVerticalMargin) Project.Current.IsDirty = true;
      Settings.DocVerticalMargin = dialog.DocVerticalMargin;
      if (Settings.WrapTextAtDashes != dialog.WrapTextAtDashes) Project.Current.IsDirty = true;
      Settings.WrapTextAtDashes = dialog.WrapTextAtDashes;
      if (Settings.DefaultRoomShape != dialog.DefaultRoomShape) Project.Current.IsDirty = true;
      Settings.DefaultRoomShape = dialog.DefaultRoomShape;
    }

    var newRegCount = Settings.Regions.Count;
    var newReg = Settings.Regions.ToArray();
    if (newRegCount != regCount)
      Project.Current.IsDirty = true;
    else
      for (var index = 0; index < newRegCount; index++)
      {
        if (regBkgdColorList[index] != newReg[index].RColor)
          Project.Current.IsDirty = true;
        if (regTextColorList[index] != newReg[index].TextColor)
          Project.Current.IsDirty = true;
        if (regNameList[index] != newReg[index].RegionName)
          Project.Current.IsDirty = true;
      }
  }

  public SettingsDialog()
  {
    ElementColors = new Color[Colors.Count];
    Regions = Settings.Regions;
    InitializeComponent();

    _editBox = new TextBox
      { Location = new Point(0, 0), Size = new Size(0, 0), Font = new Font("Tahoma", 8.25f), AcceptsReturn = true };
    _editBox.Hide();

    _regionListing.Controls.AddRange(new Control[] { _editBox });
    _editBox.Text = string.Empty;
    _editBox.BorderStyle = BorderStyle.FixedSingle;

    _editBox.KeyPress += EditBoxKeyPress;
    _editBox.LostFocus += FocusOver;
    _editBox.Enter += EditBoxEnter;
    _editBox.Leave += EditBoxLeave;

    _colorListBox.DrawMode = DrawMode.OwnerDrawFixed;
    _colorListBox.DrawItem += ColorListBox_DrawItem;
    _colorListBox.SelectedIndex = 0;

    AddRegionsToListbox();

    _regionListing.DrawMode = DrawMode.OwnerDrawFixed;
    _regionListing.DrawItem += RegionListBox_DrawItem;
    _regionListing.SelectedIndex = 0;

    _documentVerticalMargins.Enabled = _documentHorizontalMargins.Enabled = _documentSpecificMargins.Checked;
  }

  public string Author {
    get { return _authorTextBox.Text; }
    set { _authorTextBox.Text = value; }
  }

  public float ConnectionArrowSize {
    get { return (float)_arrowSizeUpDown.Value; }
    set { _arrowSizeUpDown.Value = (decimal)value; }
  }

  public float ConnectionStalkLength {
    get { return (float)_connectionStalkLengthUpDown.Value; }
    set { _connectionStalkLengthUpDown.Value = (decimal)value; }
  }

  public float DarknessStripeSize {
    get { return (float)_darknessStripeSizeNumericUpDown.Value; }
    set { _darknessStripeSizeNumericUpDown.Value = (decimal)value; }
  }

  public string DefaultRoomName {
    get { return _txtDefaultRoomName.Text; }
    set { _txtDefaultRoomName.Text = value; }
  }

  public RoomShape DefaultRoomShape {
    get { return (RoomShape)_cboRoomShape.SelectedIndex; }
    set { _cboRoomShape.SelectedIndex = (int)value; }
  }

  public string Description {
    get { return _descriptionTextBox.Text; }
    set { _descriptionTextBox.Text = value; }
  }

  public float DocHorizontalMargin {
    get { return (float)_documentHorizontalMargins.Value; }
    set { _documentHorizontalMargins.Value = (decimal)value; }
  }

  public bool DocumentSpecificMargins {
    get { return _documentSpecificMargins.Checked; }
    set { _documentSpecificMargins.Checked = value; }
  }

  public bool WrapTextAtDashes {
    get { return _wrapTextAtDashes.Checked; }
    set { _wrapTextAtDashes.Checked = value; }
  }

  public float DocVerticalMargin {
    get { return (float)_documentVerticalMargins.Value; }
    set { _documentVerticalMargins.Value = (decimal)value; }
  }

  public Color[] ElementColors { get; }

  public float GridSize {
    get { return (float)_gridSizeUpDown.Value; }
    set { _gridSizeUpDown.Value = (decimal)value; }
  }

  public float HandleSize {
    get { return (float)_handleSizeUpDown.Value; }
    set { _handleSizeUpDown.Value = (decimal)value; }
  }

  public string History {
    get { return _historyTextBox.Text; }
    set { _historyTextBox.Text = value; }
  }

  public bool IsGridVisible {
    get { return _showGridCheckBox.Checked; }
    set { _showGridCheckBox.Checked = value; }
  }

  public Font LargeFont {
    get { return _largeFont; }
    set {
      _largeFont = value;
      _largeFontNameTextBox.Text = Drawing.FontName(_largeFont);
      _largeFontSizeTextBox.Text = ((int)Math.Round(_largeFont.Size)).ToString();
    }
  }

  public Font LineFont {
    get { return _lineFont; }
    set {
      _lineFont = value;
      _lineFontNameTextBox.Text = Drawing.FontName(_lineFont);
      _lineFontSizeTextBox.Text = ((int)Math.Round(_lineFont.Size)).ToString();
    }
  }

  public bool HandDrawn {
    get { return _handDrawnCheckBox.Checked; }
    set { _handDrawnCheckBox.Checked = value; }
  }

  public float LineWidth {
    get { return (float)_lineWidthUpDown.Value; }
    set { _lineWidthUpDown.Value = (decimal)value; }
  }

  public float ObjectListOffsetFromRoom {
    get { return (float)_objectListOffsetFromRoomNumericUpDown.Value; }
    set { _objectListOffsetFromRoomNumericUpDown.Value = (decimal)value; }
  }

  public float PreferredDistanceBetweenRooms {
    get { return (float)_preferredDistanceBetweenRoomsUpDown.Value; }
    set { _preferredDistanceBetweenRoomsUpDown.Value = (decimal)value; }
  }

  public List<Region> Regions { get; }

  public bool ShowOrigin {
    get { return _showOriginCheckBox.Checked; }
    set { _showOriginCheckBox.Checked = value; }
  }

  public Font SmallFont {
    get { return _smallFont; }
    set {
      _smallFont = value;
      _smallFontNameTextBox.Text = Drawing.FontName(_smallFont);
      _smallFontSizeTextBox.Text = ((int)Math.Round(_smallFont.Size)).ToString();
    }
  }

  public float SnapToElementSize {
    get { return (float)_snapToElementDistanceUpDown.Value; }
    set { _snapToElementDistanceUpDown.Value = (decimal)value; }
  }

  public bool SnapToGrid {
    get { return _snapToGridCheckBox.Checked; }
    set { _snapToGridCheckBox.Checked = value; }
  }

  public Font SubtitleFont {
    get { return _subtitleFont; }
    set {
      _subtitleFont = value;
      _subtitleFontNameTextBox.Text = Drawing.FontName(_subtitleFont);
      _subtitleFontSizeTextBox.Text = ((int)Math.Round(_subtitleFont.Size)).ToString();
    }
  }

  public float TextOffsetFromConnection {
    get { return (float)_textOffsetFromLineUpDown.Value; }
    set { _textOffsetFromLineUpDown.Value = (decimal)value; }
  }

  public string Title {
    get { return _titleTextBox.Text; }
    set { _titleTextBox.Text = value; }
  }

  private void AddRegionsToListbox()
  {
    _regionListing.Items.Clear();
    foreach (var region in Regions.OrderBy(p => p.RegionName != Domain.Misc.Region.DefaultRegion)
                                  .ThenBy(p => p.RegionName)) _regionListing.Items.Add(region.RegionName);
  }

  private void BtnAddRegionClick(object sender, EventArgs e)
  {
    var region = new Region
      { RegionName = NextAvailableRegionName(), RColor = Color.White, TextColor = Settings.Color[Colors.Subtitle] };
    Regions.Add(region);
    AddRegionsToListbox();
    _colorListBox.Invalidate();

    var newOne = _regionListing.FindString(region.RegionName);

    _regionListing.SelectedIndex = newOne;
    _regionListing.Focus();
  }

  private void BtnChangeClick(object sender, EventArgs e)
  {
    ChangeRegionColor();
    _regionListing.Focus();
  }

  private void BtnDeleteRegionClick(object sender, EventArgs e)
  {
    DeleteRegion();
  }

  private void ChangeLargeFontButton_Click(object sender, EventArgs e)
  {
    LargeFont = ShowFontDialog(LargeFont);
  }

  private void ChangeLineFontButton_Click(object sender, EventArgs e)
  {
    LineFont = ShowFontDialog(LineFont);
  }

  private void ChangeRegionColor()
  {
    var selectedIndex = _regionListing.SelectedIndex;
    if (selectedIndex == -1) return;
    var region = Regions.FirstOrDefault(p => p.RegionName == _regionListing.Items[selectedIndex].ToString());
    if (region != null)
    {
      var originalRegionName = region.RegionName;
      var frm = new RegionSettings(region, Regions);
      if (UserInteraction.ShowDialog(frm) == DialogResult.OK)
      {
        region.RColor = frm.RegionToChange.RColor;
        region.TextColor = frm.RegionToChange.TextColor;
        region.RegionName = frm.RegionToChange.RegionName;
        UpdateExistingRoomRegions(frm.RegionToChange.RegionName, originalRegionName);
        AddRegionsToListbox();
        _regionListing.Invalidate();
        _regionListing.SelectedIndex = selectedIndex;
      }
    }
  }

  private void ChangeSmallFontButton_Click(object sender, EventArgs e)
  {
    SmallFont = ShowFontDialog(SmallFont);
  }

  private void ChangeSubtitleFontButton_Click(object sender, EventArgs e)
  {
    SubtitleFont = ShowFontDialog(SubtitleFont);
  }

  private void ColorListBox_DrawItem(object sender, DrawItemEventArgs e)
  {
    using var palette = new Palette();
    e.DrawBackground();

    const int colorHorizontalMargin = 2;
    const int colorVerticalMargin = 2;
    const int colorWidth = 24;
    var colorBounds = new Rectangle(
      e.Bounds.Left + colorHorizontalMargin,
      e.Bounds.Top + colorVerticalMargin,
      colorWidth,
      e.Bounds.Height - colorVerticalMargin * 2);
    var textBounds = new Rectangle(
      colorBounds.Right + colorHorizontalMargin,
      e.Bounds.Top,
      e.Bounds.Width - colorBounds.Width - colorHorizontalMargin * 2,
      e.Bounds.Height);
    e.Graphics.FillRectangle(palette.Brush(ElementColors[e.Index]), colorBounds);
    e.Graphics.DrawRectangle(palette.Pen(e.ForeColor, 0), colorBounds);
    var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
    e.Graphics.DrawString(
      _colorListBox.Items[e.Index].ToString(),
      e.Font,
      palette.Brush(e.ForeColor),
      textBounds,
      format);
  }

  private void CreateEditBox()
  {
    _itemSelected = _regionListing.SelectedIndex;

    if (_regionListing.Items[_itemSelected].ToString() == Domain.Misc.Region.DefaultRegion) return;

    var r = _regionListing.GetItemRectangle(_itemSelected);
    var itemText = _regionListing.Items[_itemSelected].ToString();

    var colorBounds = new Rectangle(
      r.Left + HorizontalMargin,
      r.Top + VerticalMargin,
      ColorSwatchWidth,
      r.Height - VerticalMargin * 2);
    var textBounds = new Rectangle(
      colorBounds.Right + HorizontalMargin,
      r.Top,
      r.Width - colorBounds.Width - HorizontalMargin * 2,
      r.Height);

    _editBox.Location = new Point(textBounds.X + 1, textBounds.Y + 1);
    _editBox.AutoSize = false;
    _editBox.Size = new Size(textBounds.Width, r.Height + 2);
    _editBox.Show();
    _editBox.Text = itemText;
    _editBox.Focus();
    _editBox.SelectAll();
  }

  private void DeleteRegion()
  {
    _itemSelected = _regionListing.SelectedIndex;
    if (_regionListing.Items[_itemSelected].ToString() == Domain.Misc.Region.DefaultRegion) return;
    foreach (var tRoom in Project.Current.Elements.OfType<Room>()
                                 .Where(tRoom => tRoom.Region == _regionListing.Items[_itemSelected].ToString()))
      tRoom.Region = Domain.Misc.Region.DefaultRegion;
    Regions.RemoveAll(p => p.RegionName == _regionListing.Items[_itemSelected].ToString());
    AddRegionsToListbox();

    _regionListing.SelectedIndex = _itemSelected == 0 ? 0 :
      _itemSelected + 1 >= _regionListing.Items.Count ? _regionListing.Items.Count - 1 : _itemSelected;
    _regionListing.Focus();
  }

  private void EditBoxEnter(object sender, EventArgs e)
  {
    AcceptButton = null;
    CancelButton = null;
  }

  private void EditBoxKeyPress(object sender, KeyPressEventArgs e)
  {
    if (e.KeyChar.ToString() == "_" || e.KeyChar.ToString() == ":")
    {
      e.Handled = true;
      return;
    }

    if (e.KeyChar == (char)Keys.Enter || e.KeyChar == (char)Keys.Return) UpdateHideRegionTextBox();

    if (e.KeyChar == (char)Keys.Escape)
    {
      _bUpdatingRegionText = true;
      _editBox.Hide();
      _bUpdatingRegionText = false;
    }
  }

  private void EditBoxLeave(object sender, EventArgs e)
  {
    AcceptButton = _okButton;
    CancelButton = _cancelButton;
  }

  private void FocusOver(object sender, EventArgs e)
  {
    if (_editBox.Visible)
    {
      UpdateHideRegionTextBox();
      _regionListing.Focus();
    }
  }

  private void DocumentSpecificMarginsCheckedChanged(object sender, EventArgs e)
  {
    _documentHorizontalMargins.Enabled = _documentVerticalMargins.Enabled = _documentSpecificMargins.Checked;
  }

  private void OkButtonClick(object sender, EventArgs e)
  {
    if (string.IsNullOrWhiteSpace(_txtDefaultRoomName.Text))
    {
      UserInteraction.ShowMessage(
        "The default room name can't be empty. Please put something in there.",
        "Empty default name",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
      _txtDefaultRoomName.Focus();
      DialogResult = DialogResult.None;
    }
    else if (!_txtDefaultRoomName.Text.Any(char.IsLetter))
    {
      UserInteraction.ShowMessage(
        "The default room name must contain one letter. Please include a letter.",
        "Invalid default name",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
      _txtDefaultRoomName.Focus();
      DialogResult = DialogResult.None;
    }
  }

  private void RegionListingKeyDown(object sender, KeyEventArgs e)
  {
    if (e.KeyData == Keys.F2) CreateEditBox();
  }

  private void RegionListingKeyPress(object sender, KeyPressEventArgs e)
  {
  }

  private void RegionListingKeyUp(object sender, KeyEventArgs e)
  {
    if (e.KeyCode == Keys.Delete) DeleteRegion();
  }

  private void RegionListingSelectedIndexChanged(object sender, EventArgs e)
  {
    _currentRegion = _regionListing.SelectedItem == null
      ? null
      : Regions.Find(p => p.RegionName == _regionListing.SelectedItem.ToString());

    if (_regionListing.SelectedItem == null ||
        _regionListing.SelectedItem.ToString() == Domain.Misc.Region.DefaultRegion)
      _btnDeleteRegion.Enabled = false;
    else
      _btnDeleteRegion.Enabled = true;
  }

  private string NextAvailableRegionName()
  {
    var num = 1;
    var newRegionName = "Region1";

    while (Regions.Exists(p => p.RegionName.Equals(newRegionName, StringComparison.OrdinalIgnoreCase)))
    {
      num++;
      newRegionName = $"Region{num}";
    }

    return newRegionName;
  }

  private void OnChangeColor(object sender, EventArgs e)
  {
    if (_colorListBox.SelectedItems.Count == 1)
    {
      var color = ColorDialogHelper.ShowColorDialog(ElementColors[_colorListBox.SelectedIndex], this);
      if (color != Color.Empty)
        ElementColors[_colorListBox.SelectedIndex] = color;
    }
    else
    {
      var color = ColorDialogHelper.ShowColorDialog(Color.Empty, this);
      if (color != Color.Empty)
        foreach (int selectedIndex in _colorListBox.SelectedIndices)
          ElementColors[selectedIndex] = color;
    }

    _colorListBox.Invalidate();
  }

  private void OnChangeRegionColor(object sender, EventArgs e)
  {
    ChangeRegionColor();
  }

  private bool RegionAlreadyExists(string pNew)
  {
    if (Regions.Any(p => p != _currentRegion && p.RegionName.Equals(pNew, StringComparison.OrdinalIgnoreCase)))
    {
      UserInteraction.ShowMessage($"A Region already exists with the name '{pNew}'");
      return true;
    }

    return false;
  }

  private void RegionListBox_DrawItem(object sender, DrawItemEventArgs e)
  {
    if (e.Index < 0) return;
    var txtColorFont = new Font("Arial", 6);
    using var palette = new Palette();
    e.DrawBackground();

    var colorBounds = new Rectangle(
      e.Bounds.Left + HorizontalMargin,
      e.Bounds.Top + VerticalMargin,
      ColorSwatchWidth,
      e.Bounds.Height - VerticalMargin * 2);
    var textBounds = new Rectangle(
      colorBounds.Right + HorizontalMargin,
      e.Bounds.Top,
      e.Bounds.Width - colorBounds.Width - HorizontalMargin * 2,
      e.Bounds.Height);
    var foundRegion = Regions.FirstOrDefault(p => p.RegionName == _regionListing.Items[e.Index].ToString());
    if (foundRegion != null)
    {
      e.Graphics.FillRectangle(palette.Brush(foundRegion.RColor), colorBounds);
      e.Graphics.DrawRectangle(palette.Pen(e.ForeColor, 0), colorBounds);
      e.Graphics.DrawString(
        _regionListing.Items[e.Index].ToString(),
        e.Font,
        palette.Brush(e.ForeColor),
        textBounds,
        StringFormats.Left);
      e.Graphics.DrawString(
        "123",
        txtColorFont,
        palette.Brush(foundRegion.TextColor),
        colorBounds,
        StringFormats.Center);
    }
  }

  private void SettingsDialog_FormClosing(object sender, FormClosingEventArgs e)
  {
    Properties.Settings.Default.SettingsLastTabIndex = _tabControl1.SelectedIndex;
    Properties.Settings.Default.Save();
  }

  private void SettingsDialog_Load(object sender, EventArgs e)
  {
    try
    {
      var tab = Properties.Settings.Default.SettingsLastTabIndex;

      _tabControl1.SelectedIndex = Convert.ToInt32(tab);
    }
    catch
    {
      // ignored
    }
  }

  private Font ShowFontDialog(Font font)
  {
    using var dialog = new FontDialog();
    if (font != null)
      dialog.Font = new Font(font.Name, font.Size, font.Style);
    if (UserInteraction.ShowDialog(dialog, this) == DialogResult.OK)
      return new Font(dialog.Font.Name, dialog.Font.Size, dialog.Font.Style, GraphicsUnit.World);

    return font;
  }

  private void TabControl1Selected(object sender, TabControlEventArgs e)
  {
    switch (e.TabPage.Name)
    {
      case "tabRegions":
        _regionListing.Focus();
        break;
    }
  }

  private void UpdateExistingRoomRegions(string pNew, string pOld)
  {
    var original = pOld;
    var newname = pNew;

    foreach (var tRoom in Project.Current.Elements.OfType<Room>().Where(tRoom => tRoom.Region == original))
      tRoom.Region = newname;
  }

  private void UpdateHideRegionTextBox()
  {
    if (!_bUpdatingRegionText)
    {
      _bUpdatingRegionText = true;
      _editBox.Text = _editBox.Text.Trim().Replace("\"", "'");
      if (Domain.Misc.Region.ValidRegionName(_editBox.Text))
        if (UpdateRegionName(_editBox.Text, _regionListing.Items[_itemSelected].ToString()))
        {
          _editBox.Hide();
          AddRegionsToListbox();
          _regionListing.SelectedIndex = _itemSelected == 0 ? 0 :
            _itemSelected + 1 >= _regionListing.Items.Count ? _regionListing.Items.Count - 1 : _itemSelected;
          _regionListing.Focus();
        }
        else
        {
          _editBox.Focus();
          _editBox.SelectAll();
        }
      else
        _editBox.Hide();

      _bUpdatingRegionText = false;
    }
  }

  private bool UpdateRegionName(string pNew, string pOld)
  {
    pNew = pNew.Trim();

    if (RegionAlreadyExists(pNew)) return false;

    UpdateExistingRoomRegions(pNew, pOld);

    Regions.First(p => p.RegionName == _regionListing.Items[_itemSelected].ToString()).RegionName = pNew;
    _regionListing.Items[_itemSelected] = pNew;
    return true;
  }
}