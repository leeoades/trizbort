namespace Trizbort.UI.Controls
{
    partial class AutomapBar
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._statusLabel = new System.Windows.Forms.Label();
            this._stopButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_statusLabel
            // 
            this._statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._statusLabel.AutoEllipsis = true;
            this._statusLabel.Location = new System.Drawing.Point(3, 7);
            this._statusLabel.Name = "m_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(233, 15);
            this._statusLabel.TabIndex = 0;
            this._statusLabel.Text = "(Status)";
            // 
            // m_stopButton
            // 
            this._stopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._stopButton.BackColor = System.Drawing.SystemColors.Control;
            this._stopButton.ForeColor = System.Drawing.SystemColors.ControlText;
            this._stopButton.Location = new System.Drawing.Point(241, 2);
            this._stopButton.Name = "m_stopButton";
            this._stopButton.Size = new System.Drawing.Size(75, 23);
            this._stopButton.TabIndex = 1;
            this._stopButton.TabStop = false;
            this._stopButton.Text = "&Stop";
            this._stopButton.UseVisualStyleBackColor = false;
            this._stopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // AutomapBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Info;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this._stopButton);
            this.Controls.Add(this._statusLabel);
            this.ForeColor = System.Drawing.SystemColors.InfoText;
            this.MaximumSize = new System.Drawing.Size(4096, 29);
            this.MinimumSize = new System.Drawing.Size(2, 29);
            this.Name = "AutomapBar";
            this.Size = new System.Drawing.Size(320, 27);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.Button _stopButton;
    }
}
