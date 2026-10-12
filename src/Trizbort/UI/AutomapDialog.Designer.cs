namespace Trizbort.UI
{
    partial class AutomapDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutomapDialog));
            this._label1 = new System.Windows.Forms.Label();
            this._label2 = new System.Windows.Forms.Label();
            this._textBox = new System.Windows.Forms.TextBox();
            this._browseButton = new System.Windows.Forms.Button();
            this._startButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._singleStepCheckBox = new System.Windows.Forms.CheckBox();
            this._roomsWithSameNameAreSameRoomCheckBox = new System.Windows.Forms.CheckBox();
            this._verboseTranscriptCheckBox = new System.Windows.Forms.CheckBox();
            this._guessExitsCheckBox = new System.Windows.Forms.CheckBox();
            this._groupBox1 = new System.Windows.Forms.GroupBox();
            this._chkAssumeTwoWayConnections = new System.Windows.Forms.CheckBox();
            this._startFromEndCheckBox = new System.Windows.Forms.CheckBox();
            this._groupBox2 = new System.Windows.Forms.GroupBox();
            this._label6 = new System.Windows.Forms.Label();
            this._label7 = new System.Windows.Forms.Label();
            this._addRegionCommandTextBox = new System.Windows.Forms.TextBox();
            this._label5 = new System.Windows.Forms.Label();
            this._label4 = new System.Windows.Forms.Label();
            this._addObjectCommandTextBox = new System.Windows.Forms.TextBox();
            this._label3 = new System.Windows.Forms.Label();
            this._groupBox1.SuspendLayout();
            this._groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this._label1.Location = new System.Drawing.Point(13, 13);
            this._label1.Name = "label1";
            this._label1.Size = new System.Drawing.Size(372, 35);
            this._label1.TabIndex = 0;
            this._label1.Text = "Trizbort can automatically generate a map from a transcript of a game, even while" +
    " the game is in progress.";
            // 
            // label2
            // 
            this._label2.AutoSize = true;
            this._label2.Location = new System.Drawing.Point(16, 51);
            this._label2.Name = "label2";
            this._label2.Size = new System.Drawing.Size(79, 15);
            this._label2.TabIndex = 1;
            this._label2.Text = "&Transcript File";
            // 
            // m_textBox
            // 
            this._textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._textBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._textBox.Location = new System.Drawing.Point(16, 69);
            this._textBox.Name = "m_textBox";
            this._textBox.Size = new System.Drawing.Size(378, 23);
            this._textBox.TabIndex = 1;
            // 
            // m_browseButton
            // 
            this._browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._browseButton.Location = new System.Drawing.Point(399, 69);
            this._browseButton.Name = "m_browseButton";
            this._browseButton.Size = new System.Drawing.Size(75, 23);
            this._browseButton.TabIndex = 2;
            this._browseButton.Text = "&Browse...";
            this._browseButton.UseVisualStyleBackColor = true;
            this._browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // m_startButton
            // 
            this._startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._startButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._startButton.Location = new System.Drawing.Point(278, 367);
            this._startButton.Name = "m_startButton";
            this._startButton.Size = new System.Drawing.Size(116, 23);
            this._startButton.TabIndex = 11;
            this._startButton.Text = "Start Automapping";
            this._startButton.UseVisualStyleBackColor = true;
            // 
            // m_cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(400, 367);
            this._cancelButton.Name = "m_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 12;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // m_singleStepCheckBox
            // 
            this._singleStepCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._singleStepCheckBox.AutoSize = true;
            this._singleStepCheckBox.Location = new System.Drawing.Point(16, 369);
            this._singleStepCheckBox.Name = "m_singleStepCheckBox";
            this._singleStepCheckBox.Size = new System.Drawing.Size(142, 19);
            this._singleStepCheckBox.TabIndex = 10;
            this._singleStepCheckBox.Text = "&Step with F11 (Debug)";
            this._singleStepCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_roomsWithSameNameAreSameRoomCheckBox
            // 
            this._roomsWithSameNameAreSameRoomCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._roomsWithSameNameAreSameRoomCheckBox.AutoSize = true;
            this._roomsWithSameNameAreSameRoomCheckBox.Location = new System.Drawing.Point(13, 46);
            this._roomsWithSameNameAreSameRoomCheckBox.Name = "m_roomsWithSameNameAreSameRoomCheckBox";
            this._roomsWithSameNameAreSameRoomCheckBox.Size = new System.Drawing.Size(260, 19);
            this._roomsWithSameNameAreSameRoomCheckBox.TabIndex = 4;
            this._roomsWithSameNameAreSameRoomCheckBox.Text = "&Assume no two rooms have the same name.";
            this._roomsWithSameNameAreSameRoomCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_verboseTranscriptCheckBox
            // 
            this._verboseTranscriptCheckBox.AutoSize = true;
            this._verboseTranscriptCheckBox.Location = new System.Drawing.Point(13, 22);
            this._verboseTranscriptCheckBox.Name = "m_verboseTranscriptCheckBox";
            this._verboseTranscriptCheckBox.Size = new System.Drawing.Size(312, 19);
            this._verboseTranscriptCheckBox.TabIndex = 3;
            this._verboseTranscriptCheckBox.Text = "Treat transcript as &VERBOSE; expect room descriptions.";
            this._verboseTranscriptCheckBox.UseVisualStyleBackColor = true;
            this._verboseTranscriptCheckBox.CheckedChanged += new System.EventHandler(this.VerboseTranscriptCheckBox_CheckedChanged);
            // 
            // m_guessExitsCheckBox
            // 
            this._guessExitsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._guessExitsCheckBox.AutoSize = true;
            this._guessExitsCheckBox.Location = new System.Drawing.Point(13, 71);
            this._guessExitsCheckBox.Name = "m_guessExitsCheckBox";
            this._guessExitsCheckBox.Size = new System.Drawing.Size(371, 19);
            this._guessExitsCheckBox.TabIndex = 5;
            this._guessExitsCheckBox.Text = "&Guess possible exits from a room by reading its initial description.";
            this._guessExitsCheckBox.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this._groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._groupBox1.Controls.Add(this._chkAssumeTwoWayConnections);
            this._groupBox1.Controls.Add(this._startFromEndCheckBox);
            this._groupBox1.Controls.Add(this._roomsWithSameNameAreSameRoomCheckBox);
            this._groupBox1.Controls.Add(this._verboseTranscriptCheckBox);
            this._groupBox1.Controls.Add(this._guessExitsCheckBox);
            this._groupBox1.Location = new System.Drawing.Point(16, 99);
            this._groupBox1.Name = "groupBox1";
            this._groupBox1.Size = new System.Drawing.Size(458, 148);
            this._groupBox1.TabIndex = 3;
            this._groupBox1.TabStop = false;
            this._groupBox1.Text = "&Options";
            // 
            // chkAssumeTwoWayConnections
            // 
            this._chkAssumeTwoWayConnections.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._chkAssumeTwoWayConnections.AutoSize = true;
            this._chkAssumeTwoWayConnections.Location = new System.Drawing.Point(13, 119);
            this._chkAssumeTwoWayConnections.Name = "chkAssumeTwoWayConnections";
            this._chkAssumeTwoWayConnections.Size = new System.Drawing.Size(226, 19);
            this._chkAssumeTwoWayConnections.TabIndex = 7;
            this._chkAssumeTwoWayConnections.Text = "Always assume two-way connections.";
            this._chkAssumeTwoWayConnections.UseVisualStyleBackColor = true;
            // 
            // m_startFromEndCheckBox
            // 
            this._startFromEndCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._startFromEndCheckBox.AutoSize = true;
            this._startFromEndCheckBox.Location = new System.Drawing.Point(13, 95);
            this._startFromEndCheckBox.Name = "m_startFromEndCheckBox";
            this._startFromEndCheckBox.Size = new System.Drawing.Size(294, 19);
            this._startFromEndCheckBox.TabIndex = 6;
            this._startFromEndCheckBox.Text = "&Start processing from the end of the transcript file. ";
            this._startFromEndCheckBox.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this._groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._groupBox2.Controls.Add(this._label6);
            this._groupBox2.Controls.Add(this._label7);
            this._groupBox2.Controls.Add(this._addRegionCommandTextBox);
            this._groupBox2.Controls.Add(this._label5);
            this._groupBox2.Controls.Add(this._label4);
            this._groupBox2.Controls.Add(this._addObjectCommandTextBox);
            this._groupBox2.Controls.Add(this._label3);
            this._groupBox2.Location = new System.Drawing.Point(16, 253);
            this._groupBox2.Name = "groupBox2";
            this._groupBox2.Size = new System.Drawing.Size(458, 108);
            this._groupBox2.TabIndex = 8;
            this._groupBox2.TabStop = false;
            this._groupBox2.Text = "&Commands";
            // 
            // label6
            // 
            this._label6.AutoSize = true;
            this._label6.Location = new System.Drawing.Point(170, 77);
            this._label6.Name = "label6";
            this._label6.Size = new System.Drawing.Size(262, 15);
            this._label6.TabIndex = 9;
            this._label6.Text = "followed by region name to add room to region.";
            // 
            // label7
            // 
            this._label7.AutoSize = true;
            this._label7.Location = new System.Drawing.Point(48, 77);
            this._label7.Name = "label7";
            this._label7.Size = new System.Drawing.Size(15, 15);
            this._label7.TabIndex = 9;
            this._label7.Text = ">";
            this._label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // m_addRegionCommandTextBox
            // 
            this._addRegionCommandTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._addRegionCommandTextBox.Location = new System.Drawing.Point(65, 75);
            this._addRegionCommandTextBox.Name = "m_addRegionCommandTextBox";
            this._addRegionCommandTextBox.Size = new System.Drawing.Size(100, 23);
            this._addRegionCommandTextBox.TabIndex = 9;
            // 
            // label5
            // 
            this._label5.AutoSize = true;
            this._label5.Location = new System.Drawing.Point(170, 49);
            this._label5.Name = "label5";
            this._label5.Size = new System.Drawing.Size(256, 15);
            this._label5.TabIndex = 8;
            this._label5.Text = "followed by an object name adds it to the map.";
            this._label5.Click += new System.EventHandler(this.Label5_Click);
            // 
            // label4
            // 
            this._label4.AutoSize = true;
            this._label4.Location = new System.Drawing.Point(48, 49);
            this._label4.Name = "label4";
            this._label4.Size = new System.Drawing.Size(15, 15);
            this._label4.TabIndex = 8;
            this._label4.Text = ">";
            this._label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // m_addObjectCommandTextBox
            // 
            this._addObjectCommandTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._addObjectCommandTextBox.Location = new System.Drawing.Point(65, 47);
            this._addObjectCommandTextBox.Name = "m_addObjectCommandTextBox";
            this._addObjectCommandTextBox.Size = new System.Drawing.Size(100, 23);
            this._addObjectCommandTextBox.TabIndex = 8;
            // 
            // label3
            // 
            this._label3.AutoSize = true;
            this._label3.Location = new System.Drawing.Point(10, 24);
            this._label3.Name = "label3";
            this._label3.Size = new System.Drawing.Size(133, 15);
            this._label3.TabIndex = 8;
            this._label3.Text = "At the in-game prompt:";
            // 
            // AutomapDialog
            // 
            this.AcceptButton = this._startButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(486, 402);
            this.Controls.Add(this._groupBox2);
            this.Controls.Add(this._groupBox1);
            this.Controls.Add(this._singleStepCheckBox);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._startButton);
            this.Controls.Add(this._browseButton);
            this.Controls.Add(this._textBox);
            this.Controls.Add(this._label2);
            this.Controls.Add(this._label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AutomapDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Automapping";
            this._groupBox1.ResumeLayout(false);
            this._groupBox1.PerformLayout();
            this._groupBox2.ResumeLayout(false);
            this._groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _label1;
        private System.Windows.Forms.Label _label2;
        private System.Windows.Forms.TextBox _textBox;
        private System.Windows.Forms.Button _browseButton;
        private System.Windows.Forms.Button _startButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.CheckBox _singleStepCheckBox;
        private System.Windows.Forms.CheckBox _roomsWithSameNameAreSameRoomCheckBox;
        private System.Windows.Forms.CheckBox _verboseTranscriptCheckBox;
        private System.Windows.Forms.CheckBox _guessExitsCheckBox;
        private System.Windows.Forms.GroupBox _groupBox1;
        private System.Windows.Forms.GroupBox _groupBox2;
        private System.Windows.Forms.Label _label5;
        private System.Windows.Forms.Label _label4;
        private System.Windows.Forms.TextBox _addObjectCommandTextBox;
        private System.Windows.Forms.Label _label3;
        private System.Windows.Forms.Label _label6;
        private System.Windows.Forms.Label _label7;
        private System.Windows.Forms.TextBox _addRegionCommandTextBox;
        private System.Windows.Forms.CheckBox _startFromEndCheckBox;
    private System.Windows.Forms.CheckBox _chkAssumeTwoWayConnections;
  }
}
