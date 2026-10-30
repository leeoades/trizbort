namespace Trizbort.UI {
	partial class AppSettingsDialog {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer _components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (_components != null)) {
				_components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            this._components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppSettingsDialog));
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._groupBox1 = new System.Windows.Forms.GroupBox();
            this._chkFullPathTitleBar = new System.Windows.Forms.CheckBox();
            this._chkSaveAtZoom = new System.Windows.Forms.CheckBox();
            this._invertWheelCheckBox = new System.Windows.Forms.CheckBox();
            this._chkLoadLast = new System.Windows.Forms.CheckBox();
            this._cboPortAdjustDetail = new System.Windows.Forms.ComboBox();
            this._labelG = new System.Windows.Forms.Label();
            this._txtDefaultFontName = new System.Windows.Forms.TextBox();
            this._labelFont = new System.Windows.Forms.Label();
            this._cboImageSaveType = new System.Windows.Forms.ComboBox();
            this._label2 = new System.Windows.Forms.Label();
            this._toolTip1 = new System.Windows.Forms.ToolTip(this._components);
            this._toolTip2 = new System.Windows.Forms.ToolTip(this._components);
            this._groupBox2 = new System.Windows.Forms.GroupBox();
            this._chkSaveTADSToADV3Lite = new System.Windows.Forms.CheckBox();
            this._chkSaveToImage = new System.Windows.Forms.CheckBox();
            this._chkSaveToPDF = new System.Windows.Forms.CheckBox();
            this._groupBox3 = new System.Windows.Forms.GroupBox();
            this._preferredHorizontalMargin = new System.Windows.Forms.NumericUpDown();
            this._preferredVerticalMargin = new System.Windows.Forms.NumericUpDown();
            this._chkSpecifyMargins = new System.Windows.Forms.CheckBox();
            this._labelH = new System.Windows.Forms.Label();
            this._labelV = new System.Windows.Forms.Label();
            this._tabControl1 = new System.Windows.Forms.TabControl();
            this._tabGeneral = new System.Windows.Forms.TabPage();
            this._tabToolTips = new System.Windows.Forms.TabPage();
            this._groupBox4 = new System.Windows.Forms.GroupBox();
            this._chkLimitRoomDescriptionTooltipChars = new System.Windows.Forms.CheckBox();
            this._chkShowDescriptionsInTooltip = new System.Windows.Forms.CheckBox();
            this._txtNumOfRoomDescriptionChars = new System.Windows.Forms.NumericUpDown();
            this._txtNumOfConnectionDescriptionChars = new System.Windows.Forms.NumericUpDown();
            this._chkLimitConnectionDescriptionTooltipChars = new System.Windows.Forms.CheckBox();
            this._chkShowObjectsInTooltip = new System.Windows.Forms.CheckBox();
            this._tabMap = new System.Windows.Forms.TabPage();
            this._grpMapPreferences = new System.Windows.Forms.GroupBox();
            this._chkApplyStyleToNewRooms = new System.Windows.Forms.CheckBox();
            this._chkDoubleClickToAddRoom = new System.Windows.Forms.CheckBox();
            this._groupBox1.SuspendLayout();
            this._groupBox2.SuspendLayout();
            this._groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._preferredHorizontalMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._preferredVerticalMargin)).BeginInit();
            this._tabControl1.SuspendLayout();
            this._tabGeneral.SuspendLayout();
            this._tabToolTips.SuspendLayout();
            this._groupBox4.SuspendLayout();
            this._tabMap.SuspendLayout();
            this._grpMapPreferences.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._txtNumOfRoomDescriptionChars)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._txtNumOfConnectionDescriptionChars)).BeginInit();
            this.SuspendLayout();
            // 
            // m_okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(280, 338);
            this._okButton.Name = "m_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 14;
            this._okButton.Text = "&OK";
            this._okButton.UseVisualStyleBackColor = true;
            // 
            // m_cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(361, 338);
            this._cancelButton.Name = "m_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 15;
            this._cancelButton.Text = "C&ancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this._groupBox1.BackColor = System.Drawing.Color.Transparent;
            this._groupBox1.Controls.Add(this._chkFullPathTitleBar);
            this._groupBox1.Controls.Add(this._chkSaveAtZoom);
            this._groupBox1.Controls.Add(this._invertWheelCheckBox);
            this._groupBox1.Controls.Add(this._chkLoadLast);
            this._groupBox1.Controls.Add(this._cboPortAdjustDetail);
            this._groupBox1.Controls.Add(this._labelG);
            this._groupBox1.Location = new System.Drawing.Point(6, 6);
            this._groupBox1.Name = "groupBox1";
            this._groupBox1.Size = new System.Drawing.Size(400, 83);
            this._groupBox1.TabIndex = 0;
            this._groupBox1.TabStop = false;
            this._groupBox1.Text = "Preferences";
            // 
            // chkFullPathTitleBar
            // 
            this._chkFullPathTitleBar.AutoSize = true;
            this._chkFullPathTitleBar.Location = new System.Drawing.Point(23, 60);
            this._chkFullPathTitleBar.Name = "chkFullPathTitleBar";
            this._chkFullPathTitleBar.Size = new System.Drawing.Size(162, 19);
            this._chkFullPathTitleBar.TabIndex = 2;
            this._chkFullPathTitleBar.Text = "Show Full Path in Title Bar";
            this._chkFullPathTitleBar.UseVisualStyleBackColor = true;
            // 
            // chkSaveAtZoom
            // 
            this._chkSaveAtZoom.AutoSize = true;
            this._chkSaveAtZoom.Location = new System.Drawing.Point(23, 20);
            this._chkSaveAtZoom.Name = "chkSaveAtZoom";
            this._chkSaveAtZoom.Size = new System.Drawing.Size(135, 19);
            this._chkSaveAtZoom.TabIndex = 0;
            this._chkSaveAtZoom.Text = "&Save images at 100%";
            this._toolTip1.SetToolTip(this._chkSaveAtZoom, "If this is unchecked, images will be saved at their current zoom %\r\n");
            this._chkSaveAtZoom.UseVisualStyleBackColor = true;
            // 
            // m_invertWheelCheckBox
            // 
            this._invertWheelCheckBox.AutoSize = true;
            this._invertWheelCheckBox.Location = new System.Drawing.Point(23, 40);
            this._invertWheelCheckBox.Name = "m_invertWheelCheckBox";
            this._invertWheelCheckBox.Size = new System.Drawing.Size(166, 19);
            this._invertWheelCheckBox.TabIndex = 1;
            this._invertWheelCheckBox.Text = "Invert Mouse Wheel &Zoom";
            this._invertWheelCheckBox.UseVisualStyleBackColor = true;
            // 
            // chkLoadLast
            // 
            this._chkLoadLast.AutoSize = true;
            this._chkLoadLast.Location = new System.Drawing.Point(203, 20);
            this._chkLoadLast.Name = "chkLoadLast";
            this._chkLoadLast.Size = new System.Drawing.Size(159, 19);
            this._chkLoadLast.TabIndex = 3;
            this._chkLoadLast.Text = "&Open last project on start";
            this._toolTip2.SetToolTip(this._chkLoadLast, "If this is checked, Trizbort will load the last project on startup\r\n");
            this._chkLoadLast.UseVisualStyleBackColor = true;
            // 
            // cboPortAdjustDetail
            // 
            this._cboPortAdjustDetail.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboPortAdjustDetail.FormattingEnabled = true;
            this._cboPortAdjustDetail.Items.AddRange(new object[] {
            "NSEW (4)",
            "Diagonals (8)",
            "All ports (16)"});
            this._cboPortAdjustDetail.Location = new System.Drawing.Point(300, 40);
            this._cboPortAdjustDetail.Name = "cboPortAdjustDetail";
            this._cboPortAdjustDetail.Size = new System.Drawing.Size(84, 23);
            this._cboPortAdjustDetail.TabIndex = 4;
            this._cboPortAdjustDetail.Enter += new System.EventHandler(this.CboPortAdjustDetailEnter);
            // 
            // labelG
            // 
            this._labelG.AutoSize = true;
            this._labelG.Location = new System.Drawing.Point(194, 43);
            this._labelG.Name = "labelG";
            this._labelG.Size = new System.Drawing.Size(105, 15);
            this._labelG.TabIndex = 4;
            this._labelG.Text = "Port Ad&just Detail :";
            // 
            // txtDefaultFontName
            // 
            this._txtDefaultFontName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._txtDefaultFontName.BackColor = System.Drawing.SystemColors.Window;
            this._txtDefaultFontName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtDefaultFontName.CausesValidation = false;
            this._txtDefaultFontName.Location = new System.Drawing.Point(309, 57);
            this._txtDefaultFontName.Name = "txtDefaultFontName";
            this._txtDefaultFontName.Size = new System.Drawing.Size(75, 23);
            this._txtDefaultFontName.TabIndex = 13;
            // 
            // labelFont
            // 
            this._labelFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._labelFont.AutoSize = true;
            this._labelFont.Location = new System.Drawing.Point(195, 59);
            this._labelFont.Name = "labelFont";
            this._labelFont.Size = new System.Drawing.Size(113, 15);
            this._labelFont.TabIndex = 13;
            this._labelFont.Text = "Default &Font Name :";
            // 
            // cboImageSaveType
            // 
            this._cboImageSaveType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboImageSaveType.FormattingEnabled = true;
            this._cboImageSaveType.Items.AddRange(new object[] {
            "PNG ",
            "JPEG ",
            "BMP ",
            "Enhanced Metafiles (EMF)"});
            this._cboImageSaveType.Location = new System.Drawing.Point(183, 51);
            this._cboImageSaveType.Name = "cboImageSaveType";
            this._cboImageSaveType.Size = new System.Drawing.Size(201, 23);
            this._cboImageSaveType.TabIndex = 8;
            this._cboImageSaveType.Enter += new System.EventHandler(this.CboImageSaveTypeEnter);
            // 
            // label2
            // 
            this._label2.AutoSize = true;
            this._label2.Location = new System.Drawing.Point(36, 54);
            this._label2.Name = "label2";
            this._label2.Size = new System.Drawing.Size(141, 15);
            this._label2.TabIndex = 8;
            this._label2.Text = "&Default Image Save Type :";
            // 
            // groupBox2
            // 
            this._groupBox2.BackColor = System.Drawing.Color.Transparent;
            this._groupBox2.Controls.Add(this._chkSaveTADSToADV3Lite);
            this._groupBox2.Controls.Add(this._chkSaveToImage);
            this._groupBox2.Controls.Add(this._chkSaveToPDF);
            this._groupBox2.Controls.Add(this._cboImageSaveType);
            this._groupBox2.Controls.Add(this._label2);
            this._groupBox2.Location = new System.Drawing.Point(6, 95);
            this._groupBox2.Name = "groupBox2";
            this._groupBox2.Size = new System.Drawing.Size(400, 83);
            this._groupBox2.TabIndex = 5;
            this._groupBox2.TabStop = false;
            this._groupBox2.Text = "Smart Save";
            // 
            // chkSaveTADSToADV3Lite
            // 
            this._chkSaveTADSToADV3Lite.Checked = true;
            this._chkSaveTADSToADV3Lite.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkSaveTADSToADV3Lite.Location = new System.Drawing.Point(236, 21);
            this._chkSaveTADSToADV3Lite.Name = "chkSaveTADSToADV3Lite";
            this._chkSaveTADSToADV3Lite.Size = new System.Drawing.Size(148, 23);
            this._chkSaveTADSToADV3Lite.TabIndex = 7;
            this._chkSaveTADSToADV3Lite.Text = "Save TADS to ADV3Lite";
            // 
            // chkSaveToImage
            // 
            this._chkSaveToImage.Checked = true;
            this._chkSaveToImage.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkSaveToImage.Location = new System.Drawing.Point(118, 21);
            this._chkSaveToImage.Name = "chkSaveToImage";
            this._chkSaveToImage.Size = new System.Drawing.Size(100, 23);
            this._chkSaveToImage.TabIndex = 6;
            this._chkSaveToImage.Text = "Save to Image";
            // 
            // chkSaveToPDF
            // 
            this._chkSaveToPDF.Checked = true;
            this._chkSaveToPDF.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkSaveToPDF.Location = new System.Drawing.Point(23, 21);
            this._chkSaveToPDF.Name = "chkSaveToPDF";
            this._chkSaveToPDF.Size = new System.Drawing.Size(100, 23);
            this._chkSaveToPDF.TabIndex = 5;
            this._chkSaveToPDF.Text = "Save to PDF";
            // 
            // groupBox3
            // 
            this._groupBox3.BackColor = System.Drawing.Color.Transparent;
            this._groupBox3.Controls.Add(this._preferredHorizontalMargin);
            this._groupBox3.Controls.Add(this._preferredVerticalMargin);
            this._groupBox3.Controls.Add(this._chkSpecifyMargins);
            this._groupBox3.Controls.Add(this._labelH);
            this._groupBox3.Controls.Add(this._labelV);
            this._groupBox3.Controls.Add(this._txtDefaultFontName);
            this._groupBox3.Controls.Add(this._labelFont);
            this._groupBox3.Location = new System.Drawing.Point(6, 184);
            this._groupBox3.Name = "groupBox3";
            this._groupBox3.Size = new System.Drawing.Size(400, 89);
            this._groupBox3.TabIndex = 9;
            this._groupBox3.TabStop = false;
            this._groupBox3.Text = "App defaults";
            // 
            // m_preferredHorizontalMargin
            // 
            this._preferredHorizontalMargin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._preferredHorizontalMargin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._preferredHorizontalMargin.DecimalPlaces = 1;
            this._preferredHorizontalMargin.Location = new System.Drawing.Point(78, 27);
            this._preferredHorizontalMargin.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            this._preferredHorizontalMargin.Name = "m_preferredHorizontalMargin";
            this._preferredHorizontalMargin.Size = new System.Drawing.Size(55, 23);
            this._preferredHorizontalMargin.TabIndex = 9;
            this._preferredHorizontalMargin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._preferredHorizontalMargin.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // m_preferredVerticalMargin
            // 
            this._preferredVerticalMargin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._preferredVerticalMargin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._preferredVerticalMargin.DecimalPlaces = 1;
            this._preferredVerticalMargin.Location = new System.Drawing.Point(78, 55);
            this._preferredVerticalMargin.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            this._preferredVerticalMargin.Name = "m_preferredVerticalMargin";
            this._preferredVerticalMargin.Size = new System.Drawing.Size(55, 23);
            this._preferredVerticalMargin.TabIndex = 10;
            this._preferredVerticalMargin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._preferredVerticalMargin.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // chkSpecifyMargins
            // 
            this._chkSpecifyMargins.Checked = true;
            this._chkSpecifyMargins.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkSpecifyMargins.Location = new System.Drawing.Point(142, 26);
            this._chkSpecifyMargins.Name = "chkSpecifyMargins";
            this._chkSpecifyMargins.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this._chkSpecifyMargins.Size = new System.Drawing.Size(118, 23);
            this._chkSpecifyMargins.TabIndex = 11;
            this._chkSpecifyMargins.Text = "Specify margins";
            // 
            // labelH
            // 
            this._labelH.AutoSize = true;
            this._labelH.Location = new System.Drawing.Point(10, 30);
            this._labelH.Name = "labelH";
            this._labelH.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this._labelH.Size = new System.Drawing.Size(62, 15);
            this._labelH.TabIndex = 9;
            this._labelH.Text = "Horizontal";
            // 
            // labelV
            // 
            this._labelV.AutoSize = true;
            this._labelV.Location = new System.Drawing.Point(10, 59);
            this._labelV.Name = "labelV";
            this._labelV.Size = new System.Drawing.Size(45, 15);
            this._labelV.TabIndex = 10;
            this._labelV.Text = "Vertical";
            // 
            // tabControl1
            // 
            this._tabControl1.Controls.Add(this._tabGeneral);
            this._tabControl1.Controls.Add(this._tabToolTips);
            this._tabControl1.Controls.Add(this._tabMap);
            this._tabControl1.Location = new System.Drawing.Point(4, 1);
            this._tabControl1.Name = "tabControl1";
            this._tabControl1.SelectedIndex = 0;
            this._tabControl1.Size = new System.Drawing.Size(432, 316);
            this._tabControl1.TabIndex = 17;
            // 
            // tabGeneral
            // 
            this._tabGeneral.Controls.Add(this._groupBox3);
            this._tabGeneral.Controls.Add(this._groupBox2);
            this._tabGeneral.Controls.Add(this._groupBox1);
            this._tabGeneral.Location = new System.Drawing.Point(4, 24);
            this._tabGeneral.Name = "tabGeneral";
            this._tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this._tabGeneral.Size = new System.Drawing.Size(424, 288);
            this._tabGeneral.TabIndex = 0;
            this._tabGeneral.Text = "General";
            this._tabGeneral.UseVisualStyleBackColor = true;
            // 
            // tabToolTips
            // 
            this._tabToolTips.Controls.Add(this._groupBox4);
            this._tabToolTips.Controls.Add(this._chkShowObjectsInTooltip);
            this._tabToolTips.Location = new System.Drawing.Point(4, 24);
            this._tabToolTips.Name = "tabToolTips";
            this._tabToolTips.Padding = new System.Windows.Forms.Padding(3);
            this._tabToolTips.Size = new System.Drawing.Size(424, 288);
            this._tabToolTips.TabIndex = 1;
            this._tabToolTips.Text = "ToolTips";
            this._tabToolTips.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this._groupBox4.Controls.Add(this._chkLimitRoomDescriptionTooltipChars);
            this._groupBox4.Controls.Add(this._chkShowDescriptionsInTooltip);
            this._groupBox4.Controls.Add(this._txtNumOfRoomDescriptionChars);
            this._groupBox4.Controls.Add(this._txtNumOfConnectionDescriptionChars);
            this._groupBox4.Controls.Add(this._chkLimitConnectionDescriptionTooltipChars);
            this._groupBox4.Location = new System.Drawing.Point(7, 39);
            this._groupBox4.Name = "groupBox4";
            this._groupBox4.Size = new System.Drawing.Size(411, 111);
            this._groupBox4.TabIndex = 23;
            this._groupBox4.TabStop = false;
            // 
            // chkLimitRoomDescriptionTooltipChars
            // 
            this._chkLimitRoomDescriptionTooltipChars.Location = new System.Drawing.Point(68, 33);
            this._chkLimitRoomDescriptionTooltipChars.Name = "chkLimitRoomDescriptionTooltipChars";
            this._chkLimitRoomDescriptionTooltipChars.Size = new System.Drawing.Size(272, 19);
            this._chkLimitRoomDescriptionTooltipChars.TabIndex = 2;
            this._chkLimitRoomDescriptionTooltipChars.Text = "Limit Characters of Room Description to";
            this._chkLimitRoomDescriptionTooltipChars.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._chkLimitRoomDescriptionTooltipChars.CheckedChanged += new System.EventHandler(this.ChkLimitDescriptionTooltipCharsCheckedChanged);
            // 
            // chkShowDescriptionsInTooltip
            // 
            this._chkShowDescriptionsInTooltip.BackColor = System.Drawing.Color.White;
            this._chkShowDescriptionsInTooltip.Checked = true;
            this._chkShowDescriptionsInTooltip.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowDescriptionsInTooltip.Location = new System.Drawing.Point(6, 0);
            this._chkShowDescriptionsInTooltip.Name = "chkShowDescriptionsInTooltip";
            this._chkShowDescriptionsInTooltip.Size = new System.Drawing.Size(185, 19);
            this._chkShowDescriptionsInTooltip.TabIndex = 1;
            this._chkShowDescriptionsInTooltip.Text = "Show Descriptions in Tooltip";
            this._chkShowDescriptionsInTooltip.UseVisualStyleBackColor = false;
            this._chkShowDescriptionsInTooltip.CheckedChanged += new System.EventHandler(this.ChkShowDescriptionsInTooltipCheckedChanged);
            // 
            // txtNumOfRoomDescriptionChars
            // 
            this._txtNumOfRoomDescriptionChars.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._txtNumOfRoomDescriptionChars.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtNumOfRoomDescriptionChars.DecimalPlaces = 1;
            this._txtNumOfRoomDescriptionChars.Location = new System.Drawing.Point(346, 31);
            this._txtNumOfRoomDescriptionChars.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            this._txtNumOfRoomDescriptionChars.Name = "txtNumOfRoomDescriptionChars";
            this._txtNumOfRoomDescriptionChars.Size = new System.Drawing.Size(55, 23);
            this._txtNumOfRoomDescriptionChars.TabIndex = 3;
            this._txtNumOfRoomDescriptionChars.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._txtNumOfRoomDescriptionChars.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // txtNumOfConnectionDescriptionChars
            // 
            this._txtNumOfConnectionDescriptionChars.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._txtNumOfConnectionDescriptionChars.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtNumOfConnectionDescriptionChars.DecimalPlaces = 1;
            this._txtNumOfConnectionDescriptionChars.Location = new System.Drawing.Point(346, 71);
            this._txtNumOfConnectionDescriptionChars.Maximum = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            this._txtNumOfConnectionDescriptionChars.Name = "txtNumOfConnectionDescriptionChars";
            this._txtNumOfConnectionDescriptionChars.Size = new System.Drawing.Size(55, 23);
            this._txtNumOfConnectionDescriptionChars.TabIndex = 6;
            this._txtNumOfConnectionDescriptionChars.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._txtNumOfConnectionDescriptionChars.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // chkLimitConnectionDescriptionTooltipChars
            // 
            this._chkLimitConnectionDescriptionTooltipChars.Location = new System.Drawing.Point(68, 73);
            this._chkLimitConnectionDescriptionTooltipChars.Name = "chkLimitConnectionDescriptionTooltipChars";
            this._chkLimitConnectionDescriptionTooltipChars.Size = new System.Drawing.Size(272, 19);
            this._chkLimitConnectionDescriptionTooltipChars.TabIndex = 4;
            this._chkLimitConnectionDescriptionTooltipChars.Text = "Limit Characters of Connection Description to";
            this._chkLimitConnectionDescriptionTooltipChars.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._chkLimitConnectionDescriptionTooltipChars.CheckedChanged += new System.EventHandler(this.ChkLimitConnectionDescriptionTooltipCharsCheckedChanged);
            // 
            // chkShowObjectsInTooltip
            // 
            this._chkShowObjectsInTooltip.Checked = true;
            this._chkShowObjectsInTooltip.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowObjectsInTooltip.Location = new System.Drawing.Point(13, 10);
            this._chkShowObjectsInTooltip.Name = "chkShowObjectsInTooltip";
            this._chkShowObjectsInTooltip.Size = new System.Drawing.Size(168, 19);
            this._chkShowObjectsInTooltip.TabIndex = 0;
            this._chkShowObjectsInTooltip.Text = "Show Objects in Tooltip";
            // 
            // tabMap
            // 
            this._tabMap.Controls.Add(this._grpMapPreferences);
            this._tabMap.Location = new System.Drawing.Point(4, 24);
            this._tabMap.Name = "tabMap";
            this._tabMap.Padding = new System.Windows.Forms.Padding(3);
            this._tabMap.Size = new System.Drawing.Size(424, 288);
            this._tabMap.TabIndex = 2;
            this._tabMap.Text = "Map";
            this._tabMap.UseVisualStyleBackColor = true;
            // 
            // grpMapPreferences
            // 
            this._grpMapPreferences.Controls.Add(this._chkApplyStyleToNewRooms);
            this._grpMapPreferences.Controls.Add(this._chkDoubleClickToAddRoom);
            this._grpMapPreferences.Location = new System.Drawing.Point(7, 6);
            this._grpMapPreferences.Name = "grpMapPreferences";
            this._grpMapPreferences.Size = new System.Drawing.Size(411, 80);
            this._grpMapPreferences.TabIndex = 0;
            this._grpMapPreferences.TabStop = false;
            this._grpMapPreferences.Text = "Preferences";
            // 
            // chkApplyStyleToNewRooms
            // 
            this._chkApplyStyleToNewRooms.AutoSize = true;
            this._chkApplyStyleToNewRooms.Location = new System.Drawing.Point(10, 22);
            this._chkApplyStyleToNewRooms.Name = "chkApplyStyleToNewRooms";
            this._chkApplyStyleToNewRooms.Size = new System.Drawing.Size(170, 19);
            this._chkApplyStyleToNewRooms.TabIndex = 0;
            this._chkApplyStyleToNewRooms.Text = "Apply style to new rooms";
            this._toolTip2.SetToolTip(this._chkApplyStyleToNewRooms, "If this is checked, new rooms adopt the style of the last selected room");
            this._chkApplyStyleToNewRooms.UseVisualStyleBackColor = true;
            // 
            // chkDoubleClickToAddRoom
            // 
            this._chkDoubleClickToAddRoom.AutoSize = true;
            this._chkDoubleClickToAddRoom.Location = new System.Drawing.Point(10, 47);
            this._chkDoubleClickToAddRoom.Name = "chkDoubleClickToAddRoom";
            this._chkDoubleClickToAddRoom.Size = new System.Drawing.Size(170, 19);
            this._chkDoubleClickToAddRoom.TabIndex = 1;
            this._chkDoubleClickToAddRoom.Text = "Double click to add room";
            this._toolTip2.SetToolTip(this._chkDoubleClickToAddRoom, "If this is checked, double clicking on an empty space on the map creates a new room");
            this._chkDoubleClickToAddRoom.UseVisualStyleBackColor = true;
            // 
            // AppSettingsDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(446, 370);
            this.Controls.Add(this._tabControl1);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AppSettingsDialog";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Application Settings";
            this.Load += new System.EventHandler(this.AppSettingsDialog_Load);
            this._groupBox1.ResumeLayout(false);
            this._groupBox1.PerformLayout();
            this._groupBox2.ResumeLayout(false);
            this._groupBox2.PerformLayout();
            this._groupBox3.ResumeLayout(false);
            this._groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._preferredHorizontalMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._preferredVerticalMargin)).EndInit();
            this._tabControl1.ResumeLayout(false);
            this._tabGeneral.ResumeLayout(false);
            this._tabToolTips.ResumeLayout(false);
            this._groupBox4.ResumeLayout(false);
            this._tabMap.ResumeLayout(false);
            this._grpMapPreferences.ResumeLayout(false);
            this._grpMapPreferences.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._txtNumOfRoomDescriptionChars)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._txtNumOfConnectionDescriptionChars)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

    private System.Windows.Forms.Button _okButton;
		private System.Windows.Forms.Button _cancelButton;
		private System.Windows.Forms.GroupBox _groupBox1;
        private System.Windows.Forms.CheckBox _invertWheelCheckBox;
        private System.Windows.Forms.ComboBox _cboPortAdjustDetail;
        private System.Windows.Forms.Label _labelG;
        private System.Windows.Forms.ComboBox _cboImageSaveType;
        private System.Windows.Forms.Label _label2;
        private System.Windows.Forms.TextBox _txtDefaultFontName;
        private System.Windows.Forms.Label _labelFont;
        private System.Windows.Forms.CheckBox _chkSaveAtZoom;
        private System.Windows.Forms.CheckBox _chkLoadLast;
        private System.Windows.Forms.ToolTip _toolTip2;
        private System.Windows.Forms.ToolTip _toolTip1;
        private System.Windows.Forms.GroupBox _groupBox2;
        private System.Windows.Forms.CheckBox _chkSpecifyMargins;
        private System.Windows.Forms.CheckBox _chkSaveTADSToADV3Lite;
        private System.Windows.Forms.CheckBox _chkSaveToImage;
        private System.Windows.Forms.CheckBox _chkSaveToPDF;
        private System.Windows.Forms.GroupBox _groupBox3;
        private System.Windows.Forms.NumericUpDown _preferredHorizontalMargin;
        private System.Windows.Forms.Label _labelH;
        private System.Windows.Forms.NumericUpDown _preferredVerticalMargin;
        private System.Windows.Forms.Label _labelV;

    private System.Windows.Forms.CheckBox _chkFullPathTitleBar;
    private System.Windows.Forms.TabControl _tabControl1;
    private System.Windows.Forms.TabPage _tabGeneral;
		private System.Windows.Forms.TabPage _tabToolTips;
		private System.Windows.Forms.NumericUpDown _txtNumOfRoomDescriptionChars;
		private System.Windows.Forms.CheckBox _chkLimitRoomDescriptionTooltipChars;
		private System.Windows.Forms.NumericUpDown _txtNumOfConnectionDescriptionChars;
		private System.Windows.Forms.CheckBox _chkLimitConnectionDescriptionTooltipChars;
		private System.Windows.Forms.GroupBox _groupBox4;
		private System.Windows.Forms.CheckBox _chkShowDescriptionsInTooltip;
		private System.Windows.Forms.CheckBox _chkShowObjectsInTooltip;
		private System.Windows.Forms.TabPage _tabMap;
		private System.Windows.Forms.GroupBox _grpMapPreferences;
		private System.Windows.Forms.CheckBox _chkApplyStyleToNewRooms;
		private System.Windows.Forms.CheckBox _chkDoubleClickToAddRoom;
	}
}