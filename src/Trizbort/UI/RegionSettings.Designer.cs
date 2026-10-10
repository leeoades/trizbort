namespace Trizbort.UI
{
    partial class RegionSettings
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
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._label5 = new System.Windows.Forms.Label();
            this._label1 = new System.Windows.Forms.Label();
            this._label2 = new System.Windows.Forms.Label();
            this._btnRegionColor = new System.Windows.Forms.Button();
            this._btnRegionTextColor = new System.Windows.Forms.Button();
            this._pnlRegionColor = new System.Windows.Forms.Panel();
            this._lblTextColor = new System.Windows.Forms.Label();
            this._txtRegionName = new System.Windows.Forms.TextBox();
            this._pnlRegionColor.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.Location = new System.Drawing.Point(240, 164);
            this._okButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._okButton.Name = "m_okButton";
            this._okButton.Size = new System.Drawing.Size(150, 46);
            this._okButton.TabIndex = 6;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this.OkButtonClick);
            // 
            // m_cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(402, 164);
            this._cancelButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cancelButton.Name = "m_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(150, 46);
            this._cancelButton.TabIndex = 7;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this._label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._label5.AutoSize = true;
            this._label5.Location = new System.Drawing.Point(86, 18);
            this._label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label5.Name = "label5";
            this._label5.Size = new System.Drawing.Size(84, 32);
            this._label5.TabIndex = 0;
            this._label5.Text = "&Name:";
            // 
            // label1
            // 
            this._label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._label1.AutoSize = true;
            this._label1.Location = new System.Drawing.Point(20, 66);
            this._label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label1.Name = "label1";
            this._label1.Size = new System.Drawing.Size(158, 32);
            this._label1.TabIndex = 2;
            this._label1.Text = "&Region Color:";
            // 
            // label2
            // 
            this._label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._label2.AutoSize = true;
            this._label2.Location = new System.Drawing.Point(46, 112);
            this._label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._label2.Name = "label2";
            this._label2.Size = new System.Drawing.Size(127, 32);
            this._label2.TabIndex = 4;
            this._label2.Text = "&Text Color:";
            // 
            // btnRegionColor
            // 
            this._btnRegionColor.Location = new System.Drawing.Point(180, 58);
            this._btnRegionColor.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnRegionColor.Name = "btnRegionColor";
            this._btnRegionColor.Size = new System.Drawing.Size(48, 46);
            this._btnRegionColor.TabIndex = 3;
            this._btnRegionColor.Text = "...";
            this._btnRegionColor.UseVisualStyleBackColor = true;
            this._btnRegionColor.Click += new System.EventHandler(this.BtnRegionColorClick);
            // 
            // btnRegionTextColor
            // 
            this._btnRegionTextColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnRegionTextColor.Location = new System.Drawing.Point(180, 105);
            this._btnRegionTextColor.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnRegionTextColor.Name = "btnRegionTextColor";
            this._btnRegionTextColor.Size = new System.Drawing.Size(48, 46);
            this._btnRegionTextColor.TabIndex = 5;
            this._btnRegionTextColor.Text = "...";
            this._btnRegionTextColor.UseVisualStyleBackColor = true;
            this._btnRegionTextColor.Click += new System.EventHandler(this.BtnRegionTextColorClick);
            // 
            // pnlRegionColor
            // 
            this._pnlRegionColor.Controls.Add(this._lblTextColor);
            this._pnlRegionColor.Location = new System.Drawing.Point(240, 58);
            this._pnlRegionColor.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._pnlRegionColor.Name = "pnlRegionColor";
            this._pnlRegionColor.Size = new System.Drawing.Size(310, 93);
            this._pnlRegionColor.TabIndex = 8;
            // 
            // lblTextColor
            // 
            this._lblTextColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._lblTextColor.AutoSize = true;
            this._lblTextColor.Location = new System.Drawing.Point(82, 32);
            this._lblTextColor.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._lblTextColor.Name = "lblTextColor";
            this._lblTextColor.Size = new System.Drawing.Size(158, 32);
            this._lblTextColor.TabIndex = 3;
            this._lblTextColor.Text = "&Region Color:";
            // 
            // txtRegionName
            // 
            this._txtRegionName.Location = new System.Drawing.Point(180, 12);
            this._txtRegionName.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtRegionName.Name = "txtRegionName";
            this._txtRegionName.Size = new System.Drawing.Size(370, 39);
            this._txtRegionName.TabIndex = 9;
            this._txtRegionName.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtRegionNameKeyPress);
            // 
            // RegionSettings
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(576, 234);
            this.Controls.Add(this._txtRegionName);
            this.Controls.Add(this._pnlRegionColor);
            this.Controls.Add(this._btnRegionTextColor);
            this.Controls.Add(this._btnRegionColor);
            this.Controls.Add(this._label2);
            this.Controls.Add(this._label1);
            this.Controls.Add(this._label5);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RegionSettings";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Region Settings";
            this._pnlRegionColor.ResumeLayout(false);
            this._pnlRegionColor.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Label _label5;
        private System.Windows.Forms.Label _label1;
        private System.Windows.Forms.Label _label2;
        private System.Windows.Forms.Button _btnRegionColor;
        private System.Windows.Forms.Button _btnRegionTextColor;
        private System.Windows.Forms.Panel _pnlRegionColor;
        private System.Windows.Forms.Label _lblTextColor;
        private System.Windows.Forms.TextBox _txtRegionName;
    }
}