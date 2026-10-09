using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.UI {
  internal partial class RoomPropertiesDialog : Form {
    private const int HorizontalMargin = 2;
    private const int VerticalMargin = 2;
    private const int ColorSwatchWidth = 24;
    private const string NoColorSet = "No Color Set";
    private static Tab _lastClosedTab = Tab.Objects;
    private readonly int _roomId;
    private bool _adjustingPosition;

    public RoomPropertiesDialog(PropertiesStartType start, int id) {
      InitializeComponent();

      _cboHandDrawn.Items.AddRange(new object[] {
        $"Map setting ({(Settings.HandDrawn ? "hand-drawn" : "straight")})",
        "Hand-drawn",
        "Straight"
      });
      _cboHandDrawn.SelectedIndex = 0;

      _roomId = id;

      // load regions control
      _cboRegion.Items.Clear();
      foreach (var region in Settings.Regions.OrderBy(p => p.RegionName != Domain.Misc.Region.DefaultRegion)
        .ThenBy(p => p.RegionName)) _cboRegion.Items.Add(region.RegionName);

      _cboRegion.DrawMode = DrawMode.OwnerDrawFixed;
      _cboRegion.DrawItem += RegionListBox_DrawItem;

      _cboReference.Items.Add("");
      foreach (var room in Project.Current.Elements.OfType<Room>().Where(p => p.Id != _roomId).OrderBy(p => p.Name))
        _cboReference.Items.Add(room);

      if (Settings.Regions.Count > 0)
        _cboRegion.SelectedIndex = 0;

      if (start == PropertiesStartType.Region) {
        _tabControl.SelectedTab = _tabRegions;
        ActiveControl = _cboRegion;
      } else {
        _tabControl.SelectedIndex = (int)_lastClosedTab;
        switch (_lastClosedTab) {
          case Tab.Description:
            ActiveControl = _descriptionTextBox;
            break;
          case Tab.Objects:
            ActiveControl = _txtObjects;
            break;
          case Tab.Colors:
            ActiveControl = _changeRoomFillButton;
            break;
          case Tab.Regions:
            ActiveControl = _cboRegion;
            break;
          case Tab.RoomShapes:
            ActiveControl = _cboDrawType;
            break;
        }
      }
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
      _lastClosedTab = (Tab)_tabControl.SelectedIndex;
      base.OnFormClosed(e);
    }

    public bool AllCornersEqual {
      get => _chkCornersSame.Checked;
      set => _chkCornersSame.Checked = value;
    }

    public BorderDashStyle BorderStyle {
      get => (BorderDashStyle) Enum.Parse(typeof(BorderDashStyle), _cboBorderStyle.SelectedItem.ToString());
      set => _cboBorderStyle.SelectedItem = value.ToString();
    }

    public CornerRadii Corners {
      get => new CornerRadii {
        BottomLeft = (double) _txtBottomLeft.Value,
        BottomRight = (double) _txtBottomRight.Value,
        TopRight = (double) _txtTopRight.Value,
        TopLeft = (double) _txtTopLeft.Value
      };
      set {
        _txtBottomRight.Value = new decimal(value.BottomRight);
        _txtTopLeft.Value = new decimal(value.TopLeft);
        _txtBottomLeft.Value = new decimal(value.BottomLeft);
        _txtTopRight.Value = new decimal(value.TopRight);
      }
    }

    public string Description {
      get => _descriptionTextBox.Text;
      set => _descriptionTextBox.Text = value;
    }

    public bool Ellipse {
      get => _cboDrawType.SelectedItem.ToString() == "Ellipse";
      set {
        if (value) _cboDrawType.SelectedItem = "Ellipse";
      }
    }

    public HandDrawnStyle HandDrawnStyle {
      get {
        switch (_cboHandDrawn.SelectedIndex) {
          case 1: return HandDrawnStyle.HandDrawn;
          case 2: return HandDrawnStyle.Straight;
          default: return HandDrawnStyle.MapDefault;
        }
      }
      set {
        switch (value) {
          case HandDrawnStyle.HandDrawn: _cboHandDrawn.SelectedIndex = 1; break;
          case HandDrawnStyle.Straight: _cboHandDrawn.SelectedIndex = 2; break;
          default: _cboHandDrawn.SelectedIndex = 0; break;
        }
      }
    }

    private bool IsPreviewHandDrawn => _cboHandDrawn.SelectedIndex == 1 ||
                                       _cboHandDrawn.SelectedIndex <= 0 && Settings.HandDrawn;

    public bool IsDark {
      get => _isDarkCheckBox.Checked;
      set => _isDarkCheckBox.Checked = value;
    }

    public bool IsEndRoom {
      get => _chkEndRoom.Checked;
      set => _chkEndRoom.Checked = value;
    }

    public bool IsReference => _cboReference.SelectedItem?.ToString() != string.Empty;

    public bool IsStartRoom {
      get => _chkStartRoom.Checked;
      set => _chkStartRoom.Checked = value;
    }

    public string Objects {
      get => _txtObjects.Text;
      set => _txtObjects.Text = value;
    }

    public bool ObjectsCustomPosition {
      get => _chkCustomPosition.Checked;
      set => _chkCustomPosition.Checked = value;
    }

    public int ObjectsCustomPositionDown {
      get => (int) _txtDown.Value;
      set => _txtDown.Value = value;
    }

    public int ObjectsCustomPositionRight {
      get => (int) _txtRight.Value;
      set => _txtRight.Value = value;
    }

    public CompassPoint ObjectsPosition {
      get {
        if (_nCheckBox.Checked) return CompassPoint.North;
        if (_southCheckBox.Checked) return CompassPoint.South;
        if (_eCheckBox.Checked) return CompassPoint.East;
        if (_wCheckBox.Checked) return CompassPoint.West;
        if (_neCheckBox.Checked) return CompassPoint.NorthEast;
        if (_nwCheckBox.Checked) return CompassPoint.NorthWest;
        if (_seCheckBox.Checked) return CompassPoint.SouthEast;
        if (_swCheckBox.Checked) return CompassPoint.SouthWest;
        return CompassPoint.WestSouthWest;
      }
      set {
        switch (value) {
          case CompassPoint.North:
            _nCheckBox.Checked = true;
            break;
          case CompassPoint.South:
            _southCheckBox.Checked = true;
            break;
          case CompassPoint.East:
            _eCheckBox.Checked = true;
            break;
          case CompassPoint.West:
            _wCheckBox.Checked = true;
            break;
          case CompassPoint.NorthEast:
            _neCheckBox.Checked = true;
            break;
          case CompassPoint.NorthWest:
            _nwCheckBox.Checked = true;
            break;
          case CompassPoint.SouthEast:
            _seCheckBox.Checked = true;
            break;
          case CompassPoint.SouthWest:
            _swCheckBox.Checked = true;
            break;
          default:
            _cCheckBox.Checked = true;
            break;
        }
      }
    }

    // Added for Room specific colors
    public Color ObjectTextColor {
      get => _objectTextTextBox.Watermark == NoColorSet ? Color.Transparent : _objectTextTextBox.BackColor;
      set {
        if (value == Color.Transparent) {
          _objectTextTextBox.BackColor = Color.White;
          _objectTextTextBox.Watermark = NoColorSet;
        } else {
          _objectTextTextBox.BackColor = value;
          _objectTextTextBox.Watermark = string.Empty;
        }
      }
    }

    public bool Octagonal {
      get => _cboDrawType.SelectedItem.ToString() == "Octagonal";
      set {
        if (value) _cboDrawType.SelectedItem = "Octagonal";
      }
    }

    public Room ReferenceRoom {
      get {
        if (_cboReference.SelectedItem != null && _cboReference.SelectedItem.ToString() != "")
          return (Room) _cboReference.SelectedItem;
        return null;
      }
      set => _cboReference.SelectedItem = value;
    }

    // Added for Room specific colors
    public Color RoomBorderColor {
      get => _roomBorderTextBox.Watermark == NoColorSet ? Color.Transparent : _roomBorderTextBox.BackColor;
      set {
        if (value == Color.Transparent) {
          _roomBorderTextBox.BackColor = Color.White;
          _roomBorderTextBox.Watermark = NoColorSet;
        } else {
          _roomBorderTextBox.BackColor = value;
          _roomBorderTextBox.Watermark = string.Empty;
        }
      }
    }

    // Added for Room specific colors
    public Color RoomFillColor {
      get => _roomFillTextBox.Watermark == NoColorSet ? Color.Transparent : _roomFillTextBox.BackColor;
      set {
        if (value == Color.Transparent) {
          _roomFillTextBox.BackColor = Color.White;
          _roomFillTextBox.Watermark = NoColorSet;
        } else {
          _roomFillTextBox.BackColor = value;
          _roomFillTextBox.Watermark = string.Empty;
        }
      }
    }

    public string RoomName {
      get => _txtName.Text.Trim();
      set => _txtName.Text = value;
    }

    // Added for Room specific colors
    public Color RoomNameColor {
      get => _roomTextTextBox.Watermark == NoColorSet ? Color.Transparent : _roomTextTextBox.BackColor;
      set {
        if (value == Color.Transparent) {
          _roomTextTextBox.BackColor = Color.White;
          _roomTextTextBox.Watermark = NoColorSet;
        } else {
          _roomTextTextBox.BackColor = value;
          _roomTextTextBox.Watermark = string.Empty;
        }
      }
    }

    public string RoomRegion {
      get => _cboRegion.SelectedItem?.ToString() ?? string.Empty;
      set => _cboRegion.SelectedItem = value;
    }

    public string RoomSubTitle {
      get => _txtSubTitle.Text;
      set => _txtSubTitle.Text = value;
    }

    public Color RoomSubtitleColor {
      get => _subTitleTextTextBox.Watermark == NoColorSet ? Color.Transparent : _subTitleTextTextBox.BackColor;
      set {
        if (value == Color.Transparent) {
          _subTitleTextTextBox.BackColor = Color.White;
          _subTitleTextTextBox.Watermark = NoColorSet;
        } else {
          _subTitleTextTextBox.BackColor = value;
          _subTitleTextTextBox.Watermark = string.Empty;
        }
      }
    }

    public bool RoundedCorners {
      get => _cboDrawType.SelectedItem.ToString() == "Rounded Corners";
      set {
        if (value) _cboDrawType.SelectedItem = "Rounded Corners";
      }
    }

    // Added for Room specific colors
    public Color SecondFillColor {
      get => _secondFillTextBox.Watermark == NoColorSet ? Color.Transparent : _secondFillTextBox.BackColor;
      set {
        if (value == Color.Transparent) {
          _secondFillTextBox.BackColor = Color.White;
          _secondFillTextBox.Watermark = NoColorSet;
        } else {
          _secondFillTextBox.BackColor = value;
          _secondFillTextBox.Watermark = string.Empty;
        }
      }
    }

    // Added for Room specific colors
    public string SecondFillLocation {
      get {
        switch (_comboBox1.SelectedIndex) {
          case 0:
            return "Bottom";
          case 1:
            return "BottomRight";
          case 2:
            return "BottomLeft";
          case 3:
            return "Left";
          case 4:
            return "Right";
          case 5:
            return "TopRight";
          case 6:
            return "TopLeft";
          case 7:
            return "Top";
          default:
            return "Bottom";
        }
      }
      set {
        switch (value) {
          case "Bottom":
            _comboBox1.SelectedIndex = 0;
            break;
          case "BottomRight":
            _comboBox1.SelectedIndex = 1;
            break;
          case "BottomLeft":
            _comboBox1.SelectedIndex = 2;
            break;
          case "Left":
            _comboBox1.SelectedIndex = 3;
            break;
          case "Right":
            _comboBox1.SelectedIndex = 4;
            break;
          case "TopRight":
            _comboBox1.SelectedIndex = 5;
            break;
          case "TopLeft":
            _comboBox1.SelectedIndex = 6;
            break;
          case "Top":
            _comboBox1.SelectedIndex = 7;
            break;
          default:
            _comboBox1.SelectedIndex = 0;
            break;
        }
      }
    }

    public RoomShape Shape {
      get => (RoomShape) _cboDrawType.SelectedIndex;
      set => _cboDrawType.SelectedIndex = (int) value;
    }

    public bool StraightEdges {
      get => _cboDrawType.SelectedItem.ToString() == "Straight Edges";
      set {
        if (value) _cboDrawType.SelectedItem = "Straight Edges";
      }
    }

    // Added for Room specific colors
    private void Button1Click(object sender, EventArgs e) {
      ChangeRoomBorderColor();
    }

    // Added for Room specific colors
    private void Button2Click(object sender, EventArgs e) {
      ChangeRoomTextColor();
    }

    // Added for Room specific colors
    private void Button3Click(object sender, EventArgs e) {
      ChangeObjectTextColor();
    }

    private void CboDrawTypeSelectedIndexChanged(object sender, EventArgs e) {
      if (_cboDrawType.SelectedItem.ToString() == "Ellipse") {
        _groupRoundedCorners.Visible = false;
      } else if (_cboDrawType.SelectedItem.ToString() == "Rounded Corners") {
        _groupRoundedCorners.Location = new Point(_cboDrawType.Left, _cboDrawType.Bottom + 20);
        _groupRoundedCorners.Visible = true;
      } else {
        _groupRoundedCorners.Visible = false;
      }

      _pnlSampleRoomShape.Invalidate();
    }

    private void ChangeObjectTextColor() {
      if (_tabControl.SelectedTab == _tabColors) ObjectTextColor = Colors.ShowColorDialog(ObjectTextColor, this);
    }

    private void ChangeRoomBorderColor() {
      if (_tabControl.SelectedTab == _tabColors) RoomBorderColor = Colors.ShowColorDialog(RoomBorderColor, this);
    }

    private void ChangeRoomFillColor() {
      if (_tabControl.SelectedTab == _tabColors) RoomFillColor = Colors.ShowColorDialog(RoomFillColor, this);
    }

    private void ChangeRoomTextColor() {
      if (_tabControl.SelectedTab == _tabColors) RoomNameColor = Colors.ShowColorDialog(RoomNameColor, this);
    }

    // Added for Room specific colors
    private void ChangeSecondFillColor() {
      if (_tabControl.SelectedTab == _tabColors) SecondFillColor = Colors.ShowColorDialog(SecondFillColor, this);
    }

    private void ChangeSubtitleColor() {
      if (_tabControl.SelectedTab == _tabColors) RoomSubtitleColor = Colors.ShowColorDialog(RoomSubtitleColor, this);
    }

    private void ChkCornersSameCheckedChanged(object sender, EventArgs e) {
      _txtBottomLeft.Enabled = !_chkCornersSame.Checked;
      _txtBottomRight.Enabled = !_chkCornersSame.Checked;
      _txtTopRight.Enabled = !_chkCornersSame.Checked;
      if (_chkCornersSame.Checked) {
        _txtBottomLeft.Value = _txtTopLeft.Value;
        _txtBottomRight.Value = _txtTopLeft.Value;
        _txtTopRight.Value = _txtTopLeft.Value;
      }
    }

    private void CboHandDrawnSelectedIndexChanged(object sender, EventArgs e) {
      _pnlSampleRoomShape.Invalidate();
    }

    private void ChkStartRoomCheckedChanged(object sender, EventArgs e) {
      if (_chkStartRoom.Checked) {
        var list = Project.Current.Elements.OfType<Room>().Where(p => p.IsStartRoom && p.Id != _roomId).ToList();

        if (list.Count <= 0) return;

        if (UserInteraction.ShowMessage(
              $"The room '{list.First().Name}' is set as the starting room.  Do you want to change it to this room?",
              "Change Starting Room", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
          Project.Current.Elements.OfType<Room>().ToList().ForEach(p => p.IsStartRoom = false);
        else
          _chkStartRoom.Checked = false;
      }
    }

    private void LblObjectSyntaxHelpClick(object sender, EventArgs e) {
      _pnlObjectSyntaxHelp.Visible = !_pnlObjectSyntaxHelp.Visible;
    }

    private void ChangeLargeFontButtonClick(object sender, EventArgs e) {
      ChangeRoomFillColor();
    }

    // Added for Room specific colors
    private void ChangeSecondFillButtonClick(object sender, EventArgs e) {
      ChangeSecondFillColor();
    }

    private void ChangeSubtitleTextButtonClick(object sender, EventArgs e) {
      ChangeSubtitleColor();
    }

    private void DescriptionTextBoxKeyDown(object sender, KeyEventArgs e) {
      SelectAllHandler(sender, e);
    }

    private void ObjectTextTextBoxButtonCustomClick(object sender, EventArgs e) {
      ObjectTextColor = Color.Transparent;
      _changeObjectTextButton.Focus();
    }

    private void ObjectTextTextBoxDoubleClick(object sender, EventArgs e) {
      ChangeObjectTextColor();
    }

    private void ObjectTextTextBoxEnter(object sender, EventArgs e) {
      _changeObjectTextButton.Focus();
    }

    private void OkButtonClick(object sender, EventArgs e) {
      if (string.IsNullOrWhiteSpace(_txtName.Text)) {
        UserInteraction.ShowMessage("The room name can't be empty. Please put something in there.", "Empty name",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _txtName.Focus();
        DialogResult = DialogResult.None;
      } else if (!_txtName.Text.Any(char.IsLetter)) {
        UserInteraction.ShowMessage("The room name must contain one letter.", "Non-alphabetic name", MessageBoxButtons.OK,
          MessageBoxIcon.Warning);
        _txtName.Focus();
        DialogResult = DialogResult.None;
      }
    }

    private void RoomBorderTextBoxButtonCustomClick(object sender, EventArgs e) {
      RoomBorderColor = Color.Transparent;
      _changeRoomBorderButton.Focus();
    }

    private void RoomBorderTextBoxDoubleClick(object sender, EventArgs e) {
      ChangeRoomBorderColor();
    }

    private void RoomBorderTextBoxEnter(object sender, EventArgs e) {
      _changeRoomBorderButton.Focus();
    }


    private void RoomFillTextBoxButtonCustomClick(object sender, EventArgs e) {
      RoomFillColor = Color.Transparent;
      _changeRoomFillButton.Focus();
    }

    private void RoomFillTextBoxDoubleClick(object sender, EventArgs e) {
      ChangeRoomFillColor();
    }

    private void RoomFillTextBoxEnter(object sender, EventArgs e) {
      _changeRoomFillButton.Focus();
    }

    private void RoomTextTextBoxButtonCustomClick(object sender, EventArgs e) {
      RoomNameColor = Color.Transparent;
      _changeRoomTextButton.Focus();
    }

    private void RoomTextTextBoxDoubleClick(object sender, EventArgs e) {
      ChangeRoomTextColor();
    }

    private void RoomTextTextBoxEnter(object sender, EventArgs e) {
      _changeRoomTextButton.Focus();
    }

    private void SecondFillTextBoxButtonCustomClick(object sender, EventArgs e) {
      SecondFillColor = Color.Transparent;
      _changeSecondFillButton.Focus();
    }

    private void SecondFillTextBoxDoubleClick(object sender, EventArgs e) {
      ChangeSecondFillColor();
    }

    private void SecondFillTextBoxEnter(object sender, EventArgs e) {
      _changeSecondFillButton.Focus();
    }

    private void SubTitleTextTextBoxButtonCustomClick(object sender, EventArgs e) {
      RoomSubtitleColor = Color.Transparent;
      _changeSubtitleTextButton.Focus();
    }

    private void SubTitleTextTextBoxDoubleClick(object sender, EventArgs e) {
      ChangeSubtitleColor();
    }

    private void SubTitleTextTextBoxEnter(object sender, EventArgs e) {
      _changeSubtitleTextButton.Focus();
    }


    private void TabControlEnter(object sender, EventArgs e) {
      switch (_tabControl.SelectedIndex) {
        case (int) Tab.Objects:
          SetObjectsTabFocus();
          break;
        case (int) Tab.Description:
          SetDescriptionTabFocus();
          break;
        case (int) Tab.Regions:
          SetRegionsTabFocus();
          break;
        case (int) Tab.Colors:
          SetColorsTabFocus();
          break;
      }
    }


    private void PnlSampleRoomShapePaint(object sender, PaintEventArgs e) {
      var graph = e.Graphics;
      graph.SmoothingMode = SmoothingMode.AntiAlias;
      var pen = new Pen(Color.Black, 2.0f) {LineJoin = LineJoin.Round};

      var rect = new RectangleF(10, 10, 3 * Settings.GridSize, 2 * Settings.GridSize);
      var handDrawn = IsPreviewHandDrawn;
      var random = Sketch.Seeded(0);
      var shape = _cboDrawType.SelectedItem?.ToString();

      PointF[] outline;
      if (shape == "Rounded Corners") {
        var outlinePoints = Sketch.RoundedRectangle(rect, (float) _txtTopLeft.Value, (float) _txtTopRight.Value,
          (float) _txtBottomRight.Value, (float) _txtBottomLeft.Value);
        outline = handDrawn ? Sketch.ClosedCurve(outlinePoints, random) : outlinePoints;
      } else if (shape == "Ellipse") {
        var outlinePoints = Sketch.Ellipse(rect);
        outline = handDrawn ? Sketch.ClosedCurve(outlinePoints, random) : outlinePoints;
      } else {
        PointF[] vertices;
        if (shape == "Octagonal") {
          var qw = rect.Width / 4;
          var qh = rect.Height / 4;
          vertices = new[] {
            new PointF(rect.Left, rect.Bottom - qh), new PointF(rect.Left, rect.Top + qh),
            new PointF(rect.Left + qw, rect.Top), new PointF(rect.Right - qw, rect.Top),
            new PointF(rect.Right, rect.Top + qh), new PointF(rect.Right, rect.Bottom - qh),
            new PointF(rect.Right - qw, rect.Bottom), new PointF(rect.Left + qw, rect.Bottom)
          };
        } else {
          vertices = new[] {
            new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top),
            new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom)
          };
        }
        outline = handDrawn ? Sketch.Polygon(vertices, random) : vertices;
      }

      graph.DrawPolygon(pen, outline);
    }

    private void PositionCheckBox_CheckedChanged(object sender, EventArgs e) {
      if (_adjustingPosition)
        return;

      _adjustingPosition = true;
      try {
        var checkBox = (CheckBox) sender;
        if (checkBox.Checked)
          foreach (Control other in checkBox.Parent.Controls) {
            var box = other as CheckBox;
            if (box != null && other != checkBox) box.Checked = false;
          }
        else
          _southCheckBox.Checked = true;
      }
      finally {
        _adjustingPosition = false;
      }
    }


    private void RedrawSampleOnChange(object sender, EventArgs e) {
      if (sender == _txtTopLeft && _chkCornersSame.Checked) {
        _txtBottomLeft.Value = _txtTopLeft.Value;
        _txtBottomRight.Value = _txtTopLeft.Value;
        _txtTopRight.Value = _txtTopLeft.Value;
      }

      _pnlSampleRoomShape.Invalidate();
    }

    private void RegionListBox_DrawItem(object sender, DrawItemEventArgs e) {
      using var palette = new Palette();
      e.DrawBackground();

      var colorBounds = new Rectangle(e.Bounds.Left + HorizontalMargin, e.Bounds.Top + VerticalMargin, ColorSwatchWidth,
        e.Bounds.Height - VerticalMargin * 2);
      var textBounds = new Rectangle(colorBounds.Right + HorizontalMargin, e.Bounds.Top,
        e.Bounds.Width - colorBounds.Width - HorizontalMargin * 2, e.Bounds.Height);
      var firstOrDefault = Settings.Regions.FirstOrDefault(p => p.RegionName == _cboRegion.Items[e.Index].ToString());
      if (firstOrDefault != null) e.Graphics.FillRectangle(palette.Brush(firstOrDefault.RColor), colorBounds);
      e.Graphics.DrawRectangle(palette.Pen(e.ForeColor, 0), colorBounds);
      e.Graphics.DrawString(_cboRegion.Items[e.Index].ToString(), e.Font, palette.Brush(e.ForeColor), textBounds,
        StringFormats.Left);
    }

    private void RoomPropertiesDialog_KeyUp(object sender, KeyEventArgs e) {
      if (e.Alt)
        switch (e.KeyCode) {
          case Keys.Y:
            _cboBorderStyle.Focus();
            break;

          case Keys.F:
            ChangeRoomFillColor();
            break;

          case Keys.S:
            ChangeSecondFillColor();
            break;

          case Keys.B:
            ChangeRoomBorderColor();
            break;

          case Keys.T:
            ChangeRoomTextColor();
            break;

          case Keys.J:
            ChangeObjectTextColor();
            break;

          case Keys.O:
            _tabControl.SelectedIndex = (int) Tab.Objects;
            SetObjectsTabFocus();
            break;

          case Keys.E:
            _tabControl.SelectedIndex = (int) Tab.Description;
            SetDescriptionTabFocus();
            break;

          case Keys.G:
            _tabControl.SelectedIndex = (int) Tab.Regions;
            SetRegionsTabFocus();
            break;

          case Keys.C:
            _tabControl.SelectedIndex = (int) Tab.Colors;
            SetColorsTabFocus();
            break;
        }
    }

    private static void SelectAllHandler(object sender, KeyEventArgs e) {
      if (e.Control && e.KeyCode == Keys.A) {
        ((TextBox) sender).SelectAll();
        e.Handled = true;
      }
    }

    private void SetColorsTabFocus() {
      _changeRoomFillButton.Focus();
    }

    private void SetDescriptionTabFocus() {
      _descriptionTextBox.Focus();
      _descriptionTextBox.SelectAll();
    }

    private void SetObjectsTabFocus() {
      _txtObjects.Focus();
    }

    private void SetRegionsTabFocus() {
      _cboRegion.Focus();
    }

    private void TxtObjectsKeyDown(object sender, KeyEventArgs e) {
      SelectAllHandler(sender, e);
      if (e.Handled) return;

      ObjectListEditor.EditResult? result = null;
      if (e.KeyCode == Keys.Tab && !e.Control && !e.Alt)
        result = ObjectListEditor.ChangeIndent(_txtObjects.Text, _txtObjects.SelectionStart, _txtObjects.SelectionLength, e.Shift);
      else if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None)
        result = ObjectListEditor.NewLine(_txtObjects.Text, _txtObjects.SelectionStart, _txtObjects.SelectionLength);

      if (result == null) return;

      _txtObjects.Text = result.Value.Text;
      _txtObjects.Select(result.Value.SelectionStart, result.Value.SelectionLength);
      _txtObjects.ScrollToCaret();
      e.Handled = true;
      e.SuppressKeyPress = true;
    }

    private enum Tab {
      Description,
      Objects,
      Colors,
      Regions,
      RoomShapes
    }
  }
}