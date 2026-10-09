using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Trizbort.Domain.Misc;

namespace Trizbort.UI {
  public partial class RegionSettings : Form {
    private readonly string _originalName;
    private readonly List<Region> _regions;

    public RegionSettings(Region region, List<Region> regions) {
      InitializeComponent();
      RegionToChange = new Region {RColor = region.RColor, RegionName = region.RegionName, TextColor = region.TextColor};

      this._regions = regions;
      _originalName = RegionToChange.RegionName;
      _txtRegionName.Text = RegionToChange.RegionName;
      if (RegionToChange.RegionName == Domain.Misc.Region.DefaultRegion)
        _txtRegionName.Enabled = false;
      _pnlRegionColor.BackColor = RegionToChange.RColor;
      _lblTextColor.ForeColor = RegionToChange.TextColor;
    }

    public Region RegionToChange { get; }

    private void BtnRegionColorClick(object sender, EventArgs e) {
      RegionToChange.RColor = Colors.ShowColorDialog(RegionToChange.RColor, this);
      RefreshColorPanel();
    }

    private void BtnRegionTextColorClick(object sender, EventArgs e) {
      RegionToChange.TextColor = Colors.ShowColorDialog(RegionToChange.TextColor, this);
      RefreshColorPanel();
    }

    private void OkButtonClick(object sender, EventArgs e) {
      _txtRegionName.Text = _txtRegionName.Text.Trim().Replace("\"", "'");
      if (Domain.Misc.Region.ValidRegionName(_txtRegionName.Text)) {
        if (!_txtRegionName.Text.Equals(_originalName, StringComparison.OrdinalIgnoreCase) && _regions.Any(p => p.RegionName.Equals(_txtRegionName.Text, StringComparison.OrdinalIgnoreCase))) {
          UserInteraction.ShowMessage($"A Region already exists with the name '{_txtRegionName.Text}'");
        } else {
          RegionToChange.RegionName = _txtRegionName.Text;
          DialogResult = DialogResult.OK;
        }
      } else {
        UserInteraction.ShowMessage("You can't have an empty region name", "Empty Region Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
        _txtRegionName.Focus();
      }
    }

    private void RefreshColorPanel() {
      _pnlRegionColor.BackColor = RegionToChange.RColor;
      _lblTextColor.ForeColor = RegionToChange.TextColor;
    }

    private void TxtRegionNameKeyPress(object sender, KeyPressEventArgs e) {
      if (e.KeyChar.ToString() == "_" || e.KeyChar.ToString() == ":")
        e.Handled = true;
    }
  }
}