using System.Drawing;

namespace Trizbort.UI
{
  partial class AutomapRoomSameDirectionDialog
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
            this._btnRoom1 = new System.Windows.Forms.Button();
            this._btnRoom2 = new System.Windows.Forms.Button();
            this._lblMessage = new System.Windows.Forms.Label();
            this._btnKeepBoth = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnRoom1
            // 
            this._btnRoom1.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this._btnRoom1.Location = new System.Drawing.Point(522, 26);
            this._btnRoom1.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnRoom1.Name = "btnRoom1";
            this._btnRoom1.Size = new System.Drawing.Size(334, 46);
            this._btnRoom1.TabIndex = 0;
            this._btnRoom1.Click += new System.EventHandler(this.BtnRoom1Click);
            // 
            // btnRoom2
            // 
            this._btnRoom2.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this._btnRoom2.Location = new System.Drawing.Point(522, 84);
            this._btnRoom2.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnRoom2.Name = "btnRoom2";
            this._btnRoom2.Size = new System.Drawing.Size(334, 46);
            this._btnRoom2.TabIndex = 1;
            this._btnRoom2.Click += new System.EventHandler(this.BtnRoom2Click);
            // 
            // lblMessage
            // 
            this._lblMessage.Location = new System.Drawing.Point(26, 26);
            this._lblMessage.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._lblMessage.Name = "lblMessage";
            this._lblMessage.Size = new System.Drawing.Size(484, 162);
            this._lblMessage.TabIndex = 2;
            this._lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnKeepBoth
            // 
            this._btnKeepBoth.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this._btnKeepBoth.Location = new System.Drawing.Point(522, 142);
            this._btnKeepBoth.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnKeepBoth.Name = "btnKeepBoth";
            this._btnKeepBoth.Size = new System.Drawing.Size(334, 46);
            this._btnKeepBoth.TabIndex = 3;
            this._btnKeepBoth.Text = "Keep Both";
            this._btnKeepBoth.Click += new System.EventHandler(this.BtnKeepBothClick);
            // 
            // AutomapRoomSameDirectionDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(880, 214);
            this.Controls.Add(this._btnKeepBoth);
            this.Controls.Add(this._lblMessage);
            this.Controls.Add(this._btnRoom2);
            this.Controls.Add(this._btnRoom1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AutomapRoomSameDirectionDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Shown += new System.EventHandler(this.AutomapRoomSameDirectionDialog_Shown);
            this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button _btnRoom1;
    private System.Windows.Forms.Button _btnRoom2;
    private System.Windows.Forms.Label _lblMessage;
    private System.Windows.Forms.Button _btnKeepBoth;
  }
}