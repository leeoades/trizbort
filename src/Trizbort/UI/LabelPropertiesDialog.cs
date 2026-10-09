using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;

namespace Trizbort.UI;

public sealed class LabelPropertiesDialog : Form {
  public LabelPropertiesDialog(MapLabel label)
  {
    Text = "Label Properties";
    StartPosition = FormStartPosition.CenterParent;
    FormBorderStyle = FormBorderStyle.FixedDialog;
    MinimizeBox = false;
    MaximizeBox = false;
    ShowInTaskbar = false;
    AutoScaleMode = AutoScaleMode.Font;
    ClientSize = new Size(430, 500);

    var layout = new TableLayoutPanel {
      Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 5
    };
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
    Controls.Add(layout);

    var text = new TextBox {
      Name = "labelText", Text = label.Text, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical,
      Dock = DockStyle.Fill
    };
    layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
    AddRow(layout, 0, "&Text", text);
    var textColor = ColorButton(label.TextColor);
    AddRow(layout, 1, "Text colour", textColor);

    var outlineGroup = CreateGroup("Outline", 4, out var outlineLayout);
    var outlineEnabled = new CheckBox {
      Name = "outlineEnabled", Text = "&Enabled", Checked = label.BorderStyle != BorderDashStyle.None, AutoSize = true
    };
    outlineLayout.Controls.Add(outlineEnabled, 0, 0);
    outlineLayout.SetColumnSpan(outlineEnabled, 2);
    var shape = new ComboBox
      { Name = "outlineShape", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    shape.Items.AddRange(
      Enum.GetValues<RoomShape>().Where(value => value != RoomShape.NotARoom).Cast<object>().ToArray());
    shape.SelectedItem = label.Shape;
    AddRow(outlineLayout, 1, "&Shape", shape);
    var outline = new ComboBox
      { Name = "outlineStyle", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    outline.Items.AddRange(
      Enum.GetValues<BorderDashStyle>().Where(value => value != BorderDashStyle.None && value != BorderDashStyle.Custom)
          .Cast<object>().ToArray());
    outline.SelectedItem = outlineEnabled.Checked ? label.BorderStyle : BorderDashStyle.Solid;
    AddRow(outlineLayout, 2, "Line &style", outline);
    var borderColor = ColorButton(label.BorderColor);
    borderColor.Name = "outlineColor";
    AddRow(outlineLayout, 3, "Colour", borderColor);
    layout.Controls.Add(outlineGroup, 0, 2);
    layout.SetColumnSpan(outlineGroup, 2);
    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 162));

    var backgroundGroup = CreateGroup("Background", 2, out var backgroundLayout);
    var background = new CheckBox
      { Name = "backgroundEnabled", Text = "E&nabled", Checked = label.HasBackground, AutoSize = true };
    backgroundLayout.Controls.Add(background, 0, 0);
    backgroundLayout.SetColumnSpan(background, 2);
    var backgroundColor = ColorButton(label.BackgroundColor);
    backgroundColor.Name = "backgroundColor";
    AddRow(backgroundLayout, 1, "Colour", backgroundColor);
    layout.Controls.Add(backgroundGroup, 0, 3);
    layout.SetColumnSpan(backgroundGroup, 2);
    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));

    var updateEnabledControls = () => {
      outline.Enabled = outlineEnabled.Checked;
      borderColor.Enabled = outlineEnabled.Checked;
      backgroundColor.Enabled = background.Checked;
      shape.Enabled = outlineEnabled.Checked || background.Checked;
    };
    outlineEnabled.CheckedChanged += (_, __) => updateEnabledControls();
    background.CheckedChanged += (_, __) => updateEnabledControls();
    updateEnabledControls();

    var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
    var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
    var ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
    buttons.Controls.AddRange(new Control[] { cancel, ok });
    layout.Controls.Add(buttons, 0, 4);
    layout.SetColumnSpan(buttons, 2);
    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
    AcceptButton = ok;
    CancelButton = cancel;
    ok.Click += (_, __) => {
      label.Text = text.Text;
      label.Shape = (RoomShape)shape.SelectedItem;
      label.BorderStyle = outlineEnabled.Checked ? (BorderDashStyle)outline.SelectedItem : BorderDashStyle.None;
      label.HasBackground = background.Checked;
      label.TextColor = textColor.BackColor;
      label.BorderColor = borderColor.BackColor;
      label.BackgroundColor = backgroundColor.BackColor;
    };
    Shown += (_, __) => {
      text.Focus();
      text.SelectAll();
    };
  }

  private static void AddRow(TableLayoutPanel layout, int row, string caption, Control control)
  {
    if (row > 0) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
    layout.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
    layout.Controls.Add(control, 1, row);
  }

  private static GroupBox CreateGroup(string caption, int rows, out TableLayoutPanel layout)
  {
    var group = new GroupBox { Text = caption, Dock = DockStyle.Fill, Padding = new Padding(10) };
    layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = rows };
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
    group.Controls.Add(layout);
    return group;
  }

  private static Button ColorButton(Color color)
  {
    var button = new Button
      { Text = "Choose...", BackColor = color, Dock = DockStyle.Fill, UseVisualStyleBackColor = false };
    button.ForeColor = color.GetBrightness() < 0.5f ? Color.White : Color.Black;
    button.Click += (_, __) => {
      using var dialog = new ColorDialog { Color = button.BackColor, FullOpen = true };
      if (UserInteraction.ShowDialog(dialog, button.FindForm()) == DialogResult.OK) {
        button.BackColor = dialog.Color;
        button.ForeColor = dialog.Color.GetBrightness() < 0.5f ? Color.White : Color.Black;
      }
    };
    return button;
  }
}