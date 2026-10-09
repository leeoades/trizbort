namespace Trizbort.UI
{
    partial class ConnectionPropertiesDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer _components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (_components != null))
            {
                _components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectionPropertiesDialog));
			this._cancelButton = new System.Windows.Forms.Button();
			this._okButton = new System.Windows.Forms.Button();
			this._oneWayCheckBox = new System.Windows.Forms.CheckBox();
			this._dottedCheckBox = new System.Windows.Forms.CheckBox();
			this._groupBox1 = new System.Windows.Forms.GroupBox();
			this._endLabel = new System.Windows.Forms.Label();
			this._endTextBox = new System.Windows.Forms.TextBox();
			this._label2 = new System.Windows.Forms.Label();
			this._label1 = new System.Windows.Forms.Label();
			this._middleTextBox = new System.Windows.Forms.TextBox();
			this._startTextBox = new System.Windows.Forms.TextBox();
			this._customRadioButton = new System.Windows.Forms.RadioButton();
			this._oiRadioButton = new System.Windows.Forms.RadioButton();
			this._ioRadioButton = new System.Windows.Forms.RadioButton();
			this._duRadioButton = new System.Windows.Forms.RadioButton();
			this._udRadioButton = new System.Windows.Forms.RadioButton();
			this._groupBox2 = new System.Windows.Forms.GroupBox();
			this._connectionColorClear = new System.Windows.Forms.Button();
			this._connectionColorBox = new Trizbort.UI.Controls.TrizbortTextBox();
			this._connectionColorChange = new System.Windows.Forms.Button();
			this._label11 = new System.Windows.Forms.Label();
			this._chkDoor = new System.Windows.Forms.CheckBox();
			this._label3 = new System.Windows.Forms.Label();
			this._txtName = new System.Windows.Forms.TextBox();
			this._chkLockable = new System.Windows.Forms.CheckBox();
			this._chkLocked = new System.Windows.Forms.CheckBox();
			this._chkOpen = new System.Windows.Forms.CheckBox();
			this._chkOpenable = new System.Windows.Forms.CheckBox();
			this._label4 = new System.Windows.Forms.Label();
			this._txtDescription = new System.Windows.Forms.TextBox();
			this._groupBox1.SuspendLayout();
			this._groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_cancelButton
			// 
			this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._cancelButton.Location = new System.Drawing.Point(435, 441);
			this._cancelButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._cancelButton.Name = "m_cancelButton";
			this._cancelButton.Size = new System.Drawing.Size(94, 29);
			this._cancelButton.TabIndex = 3;
			this._cancelButton.Text = "Cancel";
			this._cancelButton.UseVisualStyleBackColor = true;
			// 
			// m_okButton
			// 
			this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._okButton.Location = new System.Drawing.Point(334, 441);
			this._okButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._okButton.Name = "m_okButton";
			this._okButton.Size = new System.Drawing.Size(94, 29);
			this._okButton.TabIndex = 2;
			this._okButton.Text = "OK";
			this._okButton.UseVisualStyleBackColor = true;
			// 
			// m_oneWayCheckBox
			// 
			this._oneWayCheckBox.AutoSize = true;
			this._oneWayCheckBox.Location = new System.Drawing.Point(11, 55);
			this._oneWayCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._oneWayCheckBox.Name = "m_oneWayCheckBox";
			this._oneWayCheckBox.Size = new System.Drawing.Size(134, 24);
			this._oneWayCheckBox.TabIndex = 1;
			this._oneWayCheckBox.Text = "One Way &Arrow";
			this._oneWayCheckBox.UseVisualStyleBackColor = true;
			// 
			// m_dottedCheckBox
			// 
			this._dottedCheckBox.AutoSize = true;
			this._dottedCheckBox.Location = new System.Drawing.Point(12, 28);
			this._dottedCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._dottedCheckBox.Name = "m_dottedCheckBox";
			this._dottedCheckBox.Size = new System.Drawing.Size(78, 24);
			this._dottedCheckBox.TabIndex = 0;
			this._dottedCheckBox.Text = "&Dotted";
			this._dottedCheckBox.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this._groupBox1.Controls.Add(this._endLabel);
			this._groupBox1.Controls.Add(this._endTextBox);
			this._groupBox1.Controls.Add(this._label2);
			this._groupBox1.Controls.Add(this._label1);
			this._groupBox1.Controls.Add(this._middleTextBox);
			this._groupBox1.Controls.Add(this._startTextBox);
			this._groupBox1.Controls.Add(this._customRadioButton);
			this._groupBox1.Controls.Add(this._oiRadioButton);
			this._groupBox1.Controls.Add(this._ioRadioButton);
			this._groupBox1.Controls.Add(this._duRadioButton);
			this._groupBox1.Controls.Add(this._udRadioButton);
			this._groupBox1.Location = new System.Drawing.Point(15, 150);
			this._groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._groupBox1.Name = "groupBox1";
			this._groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._groupBox1.Size = new System.Drawing.Size(514, 132);
			this._groupBox1.TabIndex = 1;
			this._groupBox1.TabStop = false;
			this._groupBox1.Text = "&Text";
			// 
			// m_endLabel
			// 
			this._endLabel.AutoSize = true;
			this._endLabel.Location = new System.Drawing.Point(270, 90);
			this._endLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._endLabel.Name = "m_endLabel";
			this._endLabel.Size = new System.Drawing.Size(34, 20);
			this._endLabel.TabIndex = 9;
			this._endLabel.Text = "&End";
			// 
			// m_endTextBox
			// 
			this._endTextBox.Location = new System.Drawing.Point(306, 86);
			this._endTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._endTextBox.Name = "m_endTextBox";
			this._endTextBox.Size = new System.Drawing.Size(194, 27);
			this._endTextBox.TabIndex = 10;
			// 
			// label2
			// 
			this._label2.AutoSize = true;
			this._label2.Location = new System.Drawing.Point(255, 58);
			this._label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._label2.Name = "label2";
			this._label2.Size = new System.Drawing.Size(56, 20);
			this._label2.TabIndex = 7;
			this._label2.Text = "&Middle";
			// 
			// label1
			// 
			this._label1.AutoSize = true;
			this._label1.Location = new System.Drawing.Point(262, 25);
			this._label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._label1.Name = "label1";
			this._label1.Size = new System.Drawing.Size(40, 20);
			this._label1.TabIndex = 5;
			this._label1.Text = "&Start";
			// 
			// m_middleTextBox
			// 
			this._middleTextBox.Location = new System.Drawing.Point(306, 54);
			this._middleTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._middleTextBox.Name = "m_middleTextBox";
			this._middleTextBox.Size = new System.Drawing.Size(194, 27);
			this._middleTextBox.TabIndex = 8;
			// 
			// m_startTextBox
			// 
			this._startTextBox.Location = new System.Drawing.Point(306, 21);
			this._startTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._startTextBox.Name = "m_startTextBox";
			this._startTextBox.Size = new System.Drawing.Size(194, 27);
			this._startTextBox.TabIndex = 6;
			// 
			// m_customRadioButton
			// 
			this._customRadioButton.AutoSize = true;
			this._customRadioButton.Checked = true;
			this._customRadioButton.Location = new System.Drawing.Point(11, 88);
			this._customRadioButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._customRadioButton.Name = "m_customRadioButton";
			this._customRadioButton.Size = new System.Drawing.Size(80, 24);
			this._customRadioButton.TabIndex = 4;
			this._customRadioButton.TabStop = true;
			this._customRadioButton.Text = "&Custom";
			this._customRadioButton.UseVisualStyleBackColor = true;
			this._customRadioButton.CheckedChanged += new System.EventHandler(this.OnRadioButtonCheckedChanged);
			// 
			// m_oiRadioButton
			// 
			this._oiRadioButton.AutoSize = true;
			this._oiRadioButton.Location = new System.Drawing.Point(119, 55);
			this._oiRadioButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._oiRadioButton.Name = "m_oiRadioButton";
			this._oiRadioButton.Size = new System.Drawing.Size(72, 24);
			this._oiRadioButton.TabIndex = 3;
			this._oiRadioButton.Text = "&Out/In";
			this._oiRadioButton.UseVisualStyleBackColor = true;
			this._oiRadioButton.CheckedChanged += new System.EventHandler(this.OnRadioButtonCheckedChanged);
			// 
			// m_ioRadioButton
			// 
			this._ioRadioButton.AutoSize = true;
			this._ioRadioButton.Location = new System.Drawing.Point(119, 22);
			this._ioRadioButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._ioRadioButton.Name = "m_ioRadioButton";
			this._ioRadioButton.Size = new System.Drawing.Size(72, 24);
			this._ioRadioButton.TabIndex = 2;
			this._ioRadioButton.Text = "&In/Out";
			this._ioRadioButton.UseVisualStyleBackColor = true;
			this._ioRadioButton.CheckedChanged += new System.EventHandler(this.OnRadioButtonCheckedChanged);
			// 
			// m_duRadioButton
			// 
			this._duRadioButton.AutoSize = true;
			this._duRadioButton.Location = new System.Drawing.Point(11, 55);
			this._duRadioButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._duRadioButton.Name = "m_duRadioButton";
			this._duRadioButton.Size = new System.Drawing.Size(94, 24);
			this._duRadioButton.TabIndex = 1;
			this._duRadioButton.Text = "&Down/Up";
			this._duRadioButton.UseVisualStyleBackColor = true;
			this._duRadioButton.CheckedChanged += new System.EventHandler(this.OnRadioButtonCheckedChanged);
			// 
			// m_udRadioButton
			// 
			this._udRadioButton.AutoSize = true;
			this._udRadioButton.Location = new System.Drawing.Point(11, 22);
			this._udRadioButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._udRadioButton.Name = "m_udRadioButton";
			this._udRadioButton.Size = new System.Drawing.Size(94, 24);
			this._udRadioButton.TabIndex = 0;
			this._udRadioButton.Text = "&Up/Down";
			this._udRadioButton.UseVisualStyleBackColor = true;
			this._udRadioButton.CheckedChanged += new System.EventHandler(this.OnRadioButtonCheckedChanged);
			// 
			// groupBox2
			// 
			this._groupBox2.Controls.Add(this._connectionColorClear);
			this._groupBox2.Controls.Add(this._connectionColorBox);
			this._groupBox2.Controls.Add(this._connectionColorChange);
			this._groupBox2.Controls.Add(this._label11);
			this._groupBox2.Controls.Add(this._oneWayCheckBox);
			this._groupBox2.Controls.Add(this._dottedCheckBox);
			this._groupBox2.Location = new System.Drawing.Point(15, 50);
			this._groupBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._groupBox2.Name = "groupBox2";
			this._groupBox2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._groupBox2.Size = new System.Drawing.Size(514, 90);
			this._groupBox2.TabIndex = 0;
			this._groupBox2.TabStop = false;
			this._groupBox2.Text = "&Style";
			// 
			// connectionColorClear
			// 
			this._connectionColorClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._connectionColorClear.Image = ((System.Drawing.Image)(resources.GetObject("connectionColorClear.Image")));
			this._connectionColorClear.Location = new System.Drawing.Point(428, 24);
			this._connectionColorClear.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._connectionColorClear.Name = "connectionColorClear";
			this._connectionColorClear.Size = new System.Drawing.Size(35, 29);
			this._connectionColorClear.TabIndex = 26;
			this._connectionColorClear.Text = "...";
			this._connectionColorClear.UseVisualStyleBackColor = true;
			this._connectionColorClear.Click += new System.EventHandler(this.ConnectionColorClearClick);
			// 
			// connectionColorBox
			// 
			this._connectionColorBox.BackColor = System.Drawing.SystemColors.Control;
			this._connectionColorBox.Cursor = System.Windows.Forms.Cursors.Arrow;
			this._connectionColorBox.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this._connectionColorBox.Location = new System.Drawing.Point(291, 25);
			this._connectionColorBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._connectionColorBox.Name = "connectionColorBox";
			this._connectionColorBox.ReadOnly = true;
			this._connectionColorBox.Size = new System.Drawing.Size(133, 27);
			this._connectionColorBox.TabIndex = 25;
			this._connectionColorBox.Watermark = "testing";
			this._connectionColorBox.DoubleClick += new System.EventHandler(this.ConnectionColorBoxDoubleClick);
			this._connectionColorBox.Enter += new System.EventHandler(this.ConnectionColorBoxEnter);
			// 
			// connectionColorChange
			// 
			this._connectionColorChange.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._connectionColorChange.Location = new System.Drawing.Point(465, 24);
			this._connectionColorChange.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._connectionColorChange.Name = "connectionColorChange";
			this._connectionColorChange.Size = new System.Drawing.Size(36, 29);
			this._connectionColorChange.TabIndex = 23;
			this._connectionColorChange.Text = "...";
			this._connectionColorChange.UseVisualStyleBackColor = true;
			this._connectionColorChange.Click += new System.EventHandler(this.ConnectionColorChangeClick);
			// 
			// label11
			// 
			this._label11.AutoSize = true;
			this._label11.BackColor = System.Drawing.Color.Transparent;
			this._label11.Location = new System.Drawing.Point(244, 30);
			this._label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._label11.Name = "label11";
			this._label11.Size = new System.Drawing.Size(45, 20);
			this._label11.TabIndex = 22;
			this._label11.Text = "Color";
			// 
			// chkDoor
			// 
			this._chkDoor.AutoSize = true;
			this._chkDoor.Location = new System.Drawing.Point(15, 302);
			this._chkDoor.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._chkDoor.Name = "chkDoor";
			this._chkDoor.Size = new System.Drawing.Size(65, 24);
			this._chkDoor.TabIndex = 4;
			this._chkDoor.Text = "Door";
			this._chkDoor.UseVisualStyleBackColor = true;
			this._chkDoor.CheckedChanged += new System.EventHandler(this.ChkDoorCheckedChanged);
			// 
			// label3
			// 
			this._label3.AutoSize = true;
			this._label3.Location = new System.Drawing.Point(18, 20);
			this._label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._label3.Name = "label3";
			this._label3.Size = new System.Drawing.Size(52, 20);
			this._label3.TabIndex = 7;
			this._label3.Text = "Name:";
			// 
			// txtName
			// 
			this._txtName.Location = new System.Drawing.Point(72, 16);
			this._txtName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._txtName.Name = "txtName";
			this._txtName.Size = new System.Drawing.Size(455, 27);
			this._txtName.TabIndex = 8;
			// 
			// chkLockable
			// 
			this._chkLockable.AutoSize = true;
			this._chkLockable.Location = new System.Drawing.Point(274, 302);
			this._chkLockable.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._chkLockable.Name = "chkLockable";
			this._chkLockable.Size = new System.Drawing.Size(90, 24);
			this._chkLockable.TabIndex = 9;
			this._chkLockable.Text = "Lockable";
			this._chkLockable.UseVisualStyleBackColor = true;
			// 
			// chkLocked
			// 
			this._chkLocked.AutoSize = true;
			this._chkLocked.Location = new System.Drawing.Point(372, 302);
			this._chkLocked.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._chkLocked.Name = "chkLocked";
			this._chkLocked.Size = new System.Drawing.Size(78, 24);
			this._chkLocked.TabIndex = 10;
			this._chkLocked.Text = "Locked";
			this._chkLocked.UseVisualStyleBackColor = true;
			// 
			// chkOpen
			// 
			this._chkOpen.AutoSize = true;
			this._chkOpen.Location = new System.Drawing.Point(192, 302);
			this._chkOpen.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._chkOpen.Name = "chkOpen";
			this._chkOpen.Size = new System.Drawing.Size(67, 24);
			this._chkOpen.TabIndex = 12;
			this._chkOpen.Text = "Open";
			this._chkOpen.UseVisualStyleBackColor = true;
			// 
			// chkOpenable
			// 
			this._chkOpenable.AutoSize = true;
			this._chkOpenable.Location = new System.Drawing.Point(94, 302);
			this._chkOpenable.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._chkOpenable.Name = "chkOpenable";
			this._chkOpenable.Size = new System.Drawing.Size(96, 24);
			this._chkOpenable.TabIndex = 11;
			this._chkOpenable.Text = "Openable";
			this._chkOpenable.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this._label4.AutoSize = true;
			this._label4.Location = new System.Drawing.Point(11, 335);
			this._label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._label4.Name = "label4";
			this._label4.Size = new System.Drawing.Size(88, 20);
			this._label4.TabIndex = 13;
			this._label4.Text = "Description:";
			// 
			// txtDescription
			// 
			this._txtDescription.Location = new System.Drawing.Point(15, 355);
			this._txtDescription.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this._txtDescription.Multiline = true;
			this._txtDescription.Name = "txtDescription";
			this._txtDescription.Size = new System.Drawing.Size(500, 78);
			this._txtDescription.TabIndex = 14;
			// 
			// ConnectionPropertiesDialog
			// 
			this.AcceptButton = this._okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this._cancelButton;
			this.ClientSize = new System.Drawing.Size(544, 485);
			this.Controls.Add(this._label4);
			this.Controls.Add(this._txtDescription);
			this.Controls.Add(this._chkOpen);
			this.Controls.Add(this._chkOpenable);
			this.Controls.Add(this._chkLocked);
			this.Controls.Add(this._chkLockable);
			this.Controls.Add(this._label3);
			this.Controls.Add(this._txtName);
			this.Controls.Add(this._chkDoor);
			this.Controls.Add(this._groupBox2);
			this.Controls.Add(this._groupBox1);
			this.Controls.Add(this._cancelButton);
			this.Controls.Add(this._okButton);
			this.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConnectionPropertiesDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Connection Properties";
			this._groupBox1.ResumeLayout(false);
			this._groupBox1.PerformLayout();
			this._groupBox2.ResumeLayout(false);
			this._groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.CheckBox _oneWayCheckBox;
        private System.Windows.Forms.CheckBox _dottedCheckBox;
        private System.Windows.Forms.GroupBox _groupBox1;
        private System.Windows.Forms.RadioButton _customRadioButton;
        private System.Windows.Forms.RadioButton _oiRadioButton;
        private System.Windows.Forms.RadioButton _ioRadioButton;
        private System.Windows.Forms.RadioButton _duRadioButton;
        private System.Windows.Forms.RadioButton _udRadioButton;
        private System.Windows.Forms.Label _endLabel;
        private System.Windows.Forms.TextBox _endTextBox;
        private System.Windows.Forms.Label _label2;
        private System.Windows.Forms.Label _label1;
        private System.Windows.Forms.TextBox _middleTextBox;
        private System.Windows.Forms.TextBox _startTextBox;
        private System.Windows.Forms.GroupBox _groupBox2;
        private System.Windows.Forms.Button _connectionColorChange;
        private System.Windows.Forms.Label _label11;
    private System.Windows.Forms.CheckBox _chkDoor;
    private System.Windows.Forms.Label _label3;
    private System.Windows.Forms.TextBox _txtName;
    private System.Windows.Forms.CheckBox _chkLockable;
    private System.Windows.Forms.CheckBox _chkLocked;
    private System.Windows.Forms.CheckBox _chkOpen;
    private System.Windows.Forms.CheckBox _chkOpenable;
    private System.Windows.Forms.Label _label4;
    private System.Windows.Forms.TextBox _txtDescription;
    private Controls.TrizbortTextBox _connectionColorBox;
    private System.Windows.Forms.Button _connectionColorClear;
  }
}
