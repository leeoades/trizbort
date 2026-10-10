namespace Trizbort.UI
{
    partial class DisambiguateRoomsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DisambiguateRoomsDialog));
            this._label1 = new System.Windows.Forms.Label();
            this._label2 = new System.Windows.Forms.Label();
            this._transcriptContextTextBox = new System.Windows.Forms.TextBox();
            this._label3 = new System.Windows.Forms.Label();
            this._roomNamesListBox = new System.Windows.Forms.ListBox();
            this._label4 = new System.Windows.Forms.Label();
            this._label5 = new System.Windows.Forms.Label();
            this._roomDescriptionTextBox = new System.Windows.Forms.TextBox();
            this._thisRoomButton = new System.Windows.Forms.Button();
            this._newRoomButton = new System.Windows.Forms.Button();
            this._autoChooseTheRestButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this._label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._label1.Location = new System.Drawing.Point(26, 26);
            this._label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label1.Name = "label1";
            this._label1.Size = new System.Drawing.Size(962, 114);
            this._label1.TabIndex = 0;
            this._label1.Text = "Trizbort isn\'t sure which room the transcript is referring to.\r\n\r\nEither several " +
    "rooms have the same name but different descriptions, or the room description has" +
    " changed during play.\r\n";
            // 
            // label2
            // 
            this._label2.AutoSize = true;
            this._label2.Location = new System.Drawing.Point(26, 153);
            this._label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label2.Name = "label2";
            this._label2.Size = new System.Drawing.Size(282, 32);
            this._label2.TabIndex = 1;
            this._label2.Text = "When the transcript says:";
            // 
            // m_transcriptContextTextBox
            // 
            this._transcriptContextTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._transcriptContextTextBox.BackColor = System.Drawing.SystemColors.Window;
            this._transcriptContextTextBox.Location = new System.Drawing.Point(86, 216);
            this._transcriptContextTextBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._transcriptContextTextBox.Multiline = true;
            this._transcriptContextTextBox.Name = "m_transcriptContextTextBox";
            this._transcriptContextTextBox.ReadOnly = true;
            this._transcriptContextTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._transcriptContextTextBox.Size = new System.Drawing.Size(898, 172);
            this._transcriptContextTextBox.TabIndex = 2;
            // 
            // label3
            // 
            this._label3.AutoSize = true;
            this._label3.Location = new System.Drawing.Point(32, 416);
            this._label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label3.Name = "label3";
            this._label3.Size = new System.Drawing.Size(303, 32);
            this._label3.TabIndex = 3;
            this._label3.Text = "Which room does it mean?";
            // 
            // m_roomNamesListBox
            // 
            this._roomNamesListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this._roomNamesListBox.FormattingEnabled = true;
            this._roomNamesListBox.ItemHeight = 32;
            this._roomNamesListBox.Location = new System.Drawing.Point(86, 518);
            this._roomNamesListBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._roomNamesListBox.Name = "m_roomNamesListBox";
            this._roomNamesListBox.Size = new System.Drawing.Size(318, 164);
            this._roomNamesListBox.TabIndex = 5;
            this._roomNamesListBox.SelectedIndexChanged += new System.EventHandler(this.RoomNamesListBox_SelectedIndexChanged);
            // 
            // label4
            // 
            this._label4.AutoSize = true;
            this._label4.Location = new System.Drawing.Point(86, 480);
            this._label4.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label4.Name = "label4";
            this._label4.Size = new System.Drawing.Size(77, 32);
            this._label4.TabIndex = 4;
            this._label4.Text = "&Room";
            // 
            // label5
            // 
            this._label5.AutoSize = true;
            this._label5.Location = new System.Drawing.Point(414, 480);
            this._label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label5.Name = "label5";
            this._label5.Size = new System.Drawing.Size(136, 32);
            this._label5.TabIndex = 6;
            this._label5.Text = "&Description";
            // 
            // m_roomDescriptionTextBox
            // 
            this._roomDescriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._roomDescriptionTextBox.BackColor = System.Drawing.SystemColors.Window;
            this._roomDescriptionTextBox.Location = new System.Drawing.Point(420, 518);
            this._roomDescriptionTextBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._roomDescriptionTextBox.Multiline = true;
            this._roomDescriptionTextBox.Name = "m_roomDescriptionTextBox";
            this._roomDescriptionTextBox.ReadOnly = true;
            this._roomDescriptionTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._roomDescriptionTextBox.Size = new System.Drawing.Size(564, 164);
            this._roomDescriptionTextBox.TabIndex = 7;
            // 
            // m_thisRoomButton
            // 
            this._thisRoomButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._thisRoomButton.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this._thisRoomButton.Location = new System.Drawing.Point(130, 724);
            this._thisRoomButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._thisRoomButton.Name = "m_thisRoomButton";
            this._thisRoomButton.Size = new System.Drawing.Size(278, 48);
            this._thisRoomButton.TabIndex = 8;
            this._thisRoomButton.Text = "&This Room";
            this._thisRoomButton.UseVisualStyleBackColor = true;
            // 
            // m_newRoomButton
            // 
            this._newRoomButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._newRoomButton.DialogResult = System.Windows.Forms.DialogResult.No;
            this._newRoomButton.Location = new System.Drawing.Point(420, 724);
            this._newRoomButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._newRoomButton.Name = "m_newRoomButton";
            this._newRoomButton.Size = new System.Drawing.Size(278, 48);
            this._newRoomButton.TabIndex = 9;
            this._newRoomButton.Text = "A &New Room";
            this._newRoomButton.UseVisualStyleBackColor = true;
            // 
            // m_AutoChooseTheRestButton
            // 
            this._autoChooseTheRestButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._autoChooseTheRestButton.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this._autoChooseTheRestButton.Location = new System.Drawing.Point(710, 724);
            this._autoChooseTheRestButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._autoChooseTheRestButton.Name = "m_AutoChooseTheRestButton";
            this._autoChooseTheRestButton.Size = new System.Drawing.Size(278, 48);
            this._autoChooseTheRestButton.TabIndex = 10;
            this._autoChooseTheRestButton.Text = "&Auto-Choose the Rest";
            this._autoChooseTheRestButton.UseVisualStyleBackColor = true;
            // 
            // DisambiguateRoomsDialog
            // 
            this.AcceptButton = this._thisRoomButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1012, 796);
            this.Controls.Add(this._autoChooseTheRestButton);
            this.Controls.Add(this._newRoomButton);
            this.Controls.Add(this._thisRoomButton);
            this.Controls.Add(this._roomDescriptionTextBox);
            this.Controls.Add(this._label5);
            this.Controls.Add(this._label4);
            this.Controls.Add(this._roomNamesListBox);
            this.Controls.Add(this._label3);
            this.Controls.Add(this._transcriptContextTextBox);
            this.Controls.Add(this._label2);
            this.Controls.Add(this._label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1002, 779);
            this.Name = "DisambiguateRoomsDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Disambiguate Rooms";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _label1;
        private System.Windows.Forms.Label _label2;
        private System.Windows.Forms.TextBox _transcriptContextTextBox;
        private System.Windows.Forms.Label _label3;
        private System.Windows.Forms.ListBox _roomNamesListBox;
        private System.Windows.Forms.Label _label4;
        private System.Windows.Forms.Label _label5;
        private System.Windows.Forms.TextBox _roomDescriptionTextBox;
        private System.Windows.Forms.Button _thisRoomButton;
        private System.Windows.Forms.Button _newRoomButton;
        private System.Windows.Forms.Button _autoChooseTheRestButton;
    }
}
