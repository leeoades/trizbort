namespace Trizbort.UI
{
    partial class AboutDialog
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
          System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutDialog));
          _label1 = new System.Windows.Forms.Label();
          _label2 = new System.Windows.Forms.Label();
          _versionLabel = new System.Windows.Forms.Label();
          _label4 = new System.Windows.Forms.Label();
          _linkLabel1 = new System.Windows.Forms.LinkLabel();
          _pictureBox1 = new System.Windows.Forms.PictureBox();
          _linkLabel4 = new System.Windows.Forms.LinkLabel();
          _textBox1 = new System.Windows.Forms.TextBox();
          _label3 = new System.Windows.Forms.Label();
          _linkLabel3 = new System.Windows.Forms.LinkLabel();
          _linkLabel2 = new System.Windows.Forms.LinkLabel();
          _label5 = new System.Windows.Forms.Label();
          ((System.ComponentModel.ISupportInitialize)_pictureBox1).BeginInit();
          SuspendLayout();
          // 
          // label1
          // 
          _label1.AutoSize = true;
          _label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          _label1.Location = new System.Drawing.Point(188, 9);
          _label1.Name = "label1";
          _label1.Size = new System.Drawing.Size(51, 15);
          _label1.TabIndex = 1;
          _label1.Text = "Trizbort";
          // 
          // label2
          // 
          _label2.AutoSize = true;
          _label2.Font = new System.Drawing.Font("Segoe UI", 9F);
          _label2.Location = new System.Drawing.Point(188, 25);
          _label2.Name = "label2";
          _label2.Size = new System.Drawing.Size(145, 15);
          _label2.TabIndex = 2;
          _label2.Text = "Interactive Fiction Mapper";
          // 
          // m_versionLabel
          // 
          _versionLabel.AutoSize = true;
          _versionLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
          _versionLabel.Location = new System.Drawing.Point(188, 53);
          _versionLabel.Name = "m_versionLabel";
          _versionLabel.Size = new System.Drawing.Size(99, 15);
          _versionLabel.TabIndex = 3;
          _versionLabel.Text = "Version Unknown";
          // 
          // label4
          // 
          _label4.AutoSize = true;
          _label4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          _label4.Location = new System.Drawing.Point(188, 81);
          _label4.Name = "label4";
          _label4.Size = new System.Drawing.Size(119, 15);
          _label4.TabIndex = 4;
          _label4.Text = "Original by Genstein";
          // 
          // linkLabel1
          // 
          _linkLabel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
          _linkLabel1.Font = new System.Drawing.Font("Segoe UI", 9F);
          _linkLabel1.LinkArea = new System.Windows.Forms.LinkArea(91, 16);
          _linkLabel1.Location = new System.Drawing.Point(191, 304);
          _linkLabel1.Name = "linkLabel1";
          _linkLabel1.Size = new System.Drawing.Size(309, 53);
          _linkLabel1.TabIndex = 6;
          _linkLabel1.TabStop = true;
          _linkLabel1.Text = ("Uses PDFsharp v1.50.5147, copyright (c) 2005-2012 empira Software GmbH, Cologne (" + "Germany), www.pdfsharp.net\r\n");
          _linkLabel1.UseCompatibleTextRendering = true;
          _linkLabel1.LinkClicked += OnLinkClicked;
          // 
          // pictureBox1
          // 
          _pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left));
          _pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
          _pictureBox1.Image = global::Trizbort.Properties.Resources.Wizard4;
          _pictureBox1.Location = new System.Drawing.Point(0, 0);
          _pictureBox1.Name = "pictureBox1";
          _pictureBox1.Size = new System.Drawing.Size(182, 364);
          _pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
          _pictureBox1.TabIndex = 0;
          _pictureBox1.TabStop = false;
          // 
          // linkLabel4
          // 
          _linkLabel4.AutoSize = true;
          _linkLabel4.Font = new System.Drawing.Font("Segoe UI", 9F);
          _linkLabel4.Location = new System.Drawing.Point(188, 94);
          _linkLabel4.Name = "linkLabel4";
          _linkLabel4.Size = new System.Drawing.Size(162, 15);
          _linkLabel4.TabIndex = 10;
          _linkLabel4.TabStop = true;
          _linkLabel4.Text = "github.com/genstein/trizbort";
          _linkLabel4.LinkClicked += OnLinkClicked;
          // 
          // textBox1
          // 
          _textBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
          _textBox1.Location = new System.Drawing.Point(188, 197);
          _textBox1.Multiline = true;
          _textBox1.Name = "textBox1";
          _textBox1.ReadOnly = true;
          _textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
          _textBox1.Size = new System.Drawing.Size(309, 104);
          _textBox1.TabIndex = 11;
          _textBox1.Text = resources.GetString("textBox1.Text");
          // 
          // label3
          // 
          _label3.AutoSize = true;
          _label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          _label3.Location = new System.Drawing.Point(188, 114);
          _label3.Name = "label3";
          _label3.Size = new System.Drawing.Size(218, 15);
          _label3.TabIndex = 12;
          _label3.Text = "Enhancements by Jason Lautzenheiser";
          // 
          // linkLabel3
          // 
          _linkLabel3.AutoSize = true;
          _linkLabel3.Font = new System.Drawing.Font("Segoe UI", 9F);
          _linkLabel3.Location = new System.Drawing.Point(188, 129);
          _linkLabel3.Name = "linkLabel3";
          _linkLabel3.Size = new System.Drawing.Size(218, 15);
          _linkLabel3.TabIndex = 13;
          _linkLabel3.TabStop = true;
          _linkLabel3.Text = "github.com/JasonLautzenheiser/trizbort";
          _linkLabel3.LinkClicked += OnLinkClicked;
          // 
          // linkLabel2
          // 
          _linkLabel2.AutoSize = true;
          _linkLabel2.Font = new System.Drawing.Font("Segoe UI", 9F);
          _linkLabel2.Location = new System.Drawing.Point(188, 167);
          _linkLabel2.Name = "linkLabel2";
          _linkLabel2.Size = new System.Drawing.Size(168, 15);
          _linkLabel2.TabIndex = 15;
          _linkLabel2.TabStop = true;
          _linkLabel2.Text = "github.com/LeeOades/trizbort";
          _linkLabel2.LinkClicked += OnLinkClicked;
          // 
          // label5
          // 
          _label5.AutoSize = true;
          _label5.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          _label5.Location = new System.Drawing.Point(188, 154);
          _label5.Name = "label5";
          _label5.Size = new System.Drawing.Size(209, 15);
          _label5.TabIndex = 14;
          _label5.Text = "Further Enhancements by Lee Oades";
          // 
          // AboutDialog
          // 
          AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
          AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
          AutoSize = true;
          ClientSize = new System.Drawing.Size(512, 364);
          Controls.Add(_linkLabel2);
          Controls.Add(_label5);
          Controls.Add(_linkLabel3);
          Controls.Add(_label3);
          Controls.Add(_textBox1);
          Controls.Add(_linkLabel4);
          Controls.Add(_linkLabel1);
          Controls.Add(_label4);
          Controls.Add(_versionLabel);
          Controls.Add(_label2);
          Controls.Add(_label1);
          Controls.Add(_pictureBox1);
          Font = new System.Drawing.Font("Segoe UI", 9F);
          FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
          Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
          MaximizeBox = false;
          MinimizeBox = false;
          ShowIcon = false;
          ShowInTaskbar = false;
          StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
          Text = "About";
          ((System.ComponentModel.ISupportInitialize)_pictureBox1).EndInit();
          ResumeLayout(false);
          PerformLayout();
        }

        private System.Windows.Forms.Label _label5;

        #endregion

        private System.Windows.Forms.PictureBox _pictureBox1;
        private System.Windows.Forms.Label _label1;
        private System.Windows.Forms.Label _label2;
        private System.Windows.Forms.Label _versionLabel;
        private System.Windows.Forms.Label _label4;
        private System.Windows.Forms.LinkLabel _linkLabel1;
        private System.Windows.Forms.LinkLabel _linkLabel2;
        private System.Windows.Forms.LinkLabel _linkLabel4;
        private System.Windows.Forms.TextBox _textBox1;
        private System.Windows.Forms.Label _label3;
        private System.Windows.Forms.LinkLabel _linkLabel3;
    }
}
