/*
    Copyright (c) 2010-2018 by Genstein and Jason Lautzenheiser.

    This file is (or was originally) part of Trizbort, the Interactive Fiction Mapper.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

namespace Trizbort.UI
{
    partial class AboutDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
          label1 = new System.Windows.Forms.Label();
          label2 = new System.Windows.Forms.Label();
          m_versionLabel = new System.Windows.Forms.Label();
          label4 = new System.Windows.Forms.Label();
          linkLabel1 = new System.Windows.Forms.LinkLabel();
          pictureBox1 = new System.Windows.Forms.PictureBox();
          linkLabel4 = new System.Windows.Forms.LinkLabel();
          textBox1 = new System.Windows.Forms.TextBox();
          label3 = new System.Windows.Forms.Label();
          linkLabel3 = new System.Windows.Forms.LinkLabel();
          linkLabel2 = new System.Windows.Forms.LinkLabel();
          label5 = new System.Windows.Forms.Label();
          ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
          SuspendLayout();
          // 
          // label1
          // 
          label1.AutoSize = true;
          label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          label1.Location = new System.Drawing.Point(188, 9);
          label1.Name = "label1";
          label1.Size = new System.Drawing.Size(51, 15);
          label1.TabIndex = 1;
          label1.Text = "Trizbort";
          // 
          // label2
          // 
          label2.AutoSize = true;
          label2.Font = new System.Drawing.Font("Segoe UI", 9F);
          label2.Location = new System.Drawing.Point(188, 25);
          label2.Name = "label2";
          label2.Size = new System.Drawing.Size(145, 15);
          label2.TabIndex = 2;
          label2.Text = "Interactive Fiction Mapper";
          // 
          // m_versionLabel
          // 
          m_versionLabel.AutoSize = true;
          m_versionLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
          m_versionLabel.Location = new System.Drawing.Point(188, 53);
          m_versionLabel.Name = "m_versionLabel";
          m_versionLabel.Size = new System.Drawing.Size(99, 15);
          m_versionLabel.TabIndex = 3;
          m_versionLabel.Text = "Version Unknown";
          // 
          // label4
          // 
          label4.AutoSize = true;
          label4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          label4.Location = new System.Drawing.Point(188, 81);
          label4.Name = "label4";
          label4.Size = new System.Drawing.Size(119, 15);
          label4.TabIndex = 4;
          label4.Text = "Original by Genstein";
          // 
          // linkLabel1
          // 
          linkLabel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
          linkLabel1.Font = new System.Drawing.Font("Segoe UI", 9F);
          linkLabel1.LinkArea = new System.Windows.Forms.LinkArea(91, 16);
          linkLabel1.Location = new System.Drawing.Point(191, 304);
          linkLabel1.Name = "linkLabel1";
          linkLabel1.Size = new System.Drawing.Size(309, 53);
          linkLabel1.TabIndex = 6;
          linkLabel1.TabStop = true;
          linkLabel1.Text = ("Uses PDFsharp v1.50.5147, copyright (c) 2005-2012 empira Software GmbH, Cologne (" + "Germany), www.pdfsharp.net\r\n");
          linkLabel1.UseCompatibleTextRendering = true;
          linkLabel1.LinkClicked += onLinkClicked;
          // 
          // pictureBox1
          // 
          pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left));
          pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
          pictureBox1.Image = global::Trizbort.Properties.Resources.Wizard4;
          pictureBox1.Location = new System.Drawing.Point(0, 0);
          pictureBox1.Name = "pictureBox1";
          pictureBox1.Size = new System.Drawing.Size(182, 364);
          pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
          pictureBox1.TabIndex = 0;
          pictureBox1.TabStop = false;
          // 
          // linkLabel4
          // 
          linkLabel4.AutoSize = true;
          linkLabel4.Font = new System.Drawing.Font("Segoe UI", 9F);
          linkLabel4.Location = new System.Drawing.Point(188, 94);
          linkLabel4.Name = "linkLabel4";
          linkLabel4.Size = new System.Drawing.Size(162, 15);
          linkLabel4.TabIndex = 10;
          linkLabel4.TabStop = true;
          linkLabel4.Text = "github.com/genstein/trizbort";
          linkLabel4.LinkClicked += onLinkClicked;
          // 
          // textBox1
          // 
          textBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
          textBox1.Location = new System.Drawing.Point(188, 197);
          textBox1.Multiline = true;
          textBox1.Name = "textBox1";
          textBox1.ReadOnly = true;
          textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
          textBox1.Size = new System.Drawing.Size(309, 104);
          textBox1.TabIndex = 11;
          textBox1.Text = resources.GetString("textBox1.Text");
          // 
          // label3
          // 
          label3.AutoSize = true;
          label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          label3.Location = new System.Drawing.Point(188, 114);
          label3.Name = "label3";
          label3.Size = new System.Drawing.Size(218, 15);
          label3.TabIndex = 12;
          label3.Text = "Enhancements by Jason Lautzenheiser";
          // 
          // linkLabel3
          // 
          linkLabel3.AutoSize = true;
          linkLabel3.Font = new System.Drawing.Font("Segoe UI", 9F);
          linkLabel3.Location = new System.Drawing.Point(188, 129);
          linkLabel3.Name = "linkLabel3";
          linkLabel3.Size = new System.Drawing.Size(218, 15);
          linkLabel3.TabIndex = 13;
          linkLabel3.TabStop = true;
          linkLabel3.Text = "github.com/JasonLautzenheiser/trizbort";
          linkLabel3.LinkClicked += onLinkClicked;
          // 
          // linkLabel2
          // 
          linkLabel2.AutoSize = true;
          linkLabel2.Font = new System.Drawing.Font("Segoe UI", 9F);
          linkLabel2.Location = new System.Drawing.Point(188, 167);
          linkLabel2.Name = "linkLabel2";
          linkLabel2.Size = new System.Drawing.Size(168, 15);
          linkLabel2.TabIndex = 15;
          linkLabel2.TabStop = true;
          linkLabel2.Text = "github.com/LeeOades/trizbort";
          linkLabel2.LinkClicked += onLinkClicked;
          // 
          // label5
          // 
          label5.AutoSize = true;
          label5.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
          label5.Location = new System.Drawing.Point(188, 154);
          label5.Name = "label5";
          label5.Size = new System.Drawing.Size(209, 15);
          label5.TabIndex = 14;
          label5.Text = "Further Enhancements by Lee Oades";
          // 
          // AboutDialog
          // 
          AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
          AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
          AutoSize = true;
          ClientSize = new System.Drawing.Size(512, 364);
          Controls.Add(linkLabel2);
          Controls.Add(label5);
          Controls.Add(linkLabel3);
          Controls.Add(label3);
          Controls.Add(textBox1);
          Controls.Add(linkLabel4);
          Controls.Add(linkLabel1);
          Controls.Add(label4);
          Controls.Add(m_versionLabel);
          Controls.Add(label2);
          Controls.Add(label1);
          Controls.Add(pictureBox1);
          Font = new System.Drawing.Font("Segoe UI", 9F);
          FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
          Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
          MaximizeBox = false;
          MinimizeBox = false;
          ShowIcon = false;
          ShowInTaskbar = false;
          StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
          Text = "About";
          ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
          ResumeLayout(false);
          PerformLayout();
        }

        private System.Windows.Forms.Label label5;

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label m_versionLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.LinkLabel linkLabel4;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.LinkLabel linkLabel3;
    }
}
