namespace Trizbort.UI
{
    partial class QuickFind
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
            this._labelX1 = new System.Windows.Forms.Label();
            this._btnFind = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._txtFind = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // labelX1
            // 
            this._labelX1.AutoSize = true;
            this._labelX1.Location = new System.Drawing.Point(22, 12);
            this._labelX1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._labelX1.Name = "labelX1";
            this._labelX1.Size = new System.Drawing.Size(448, 32);
            this._labelX1.TabIndex = 2;
            this._labelX1.Text = "Enter room name, description, or objects";
            // 
            // btnFind
            // 
            this._btnFind.Location = new System.Drawing.Point(504, 148);
            this._btnFind.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnFind.Name = "btnFind";
            this._btnFind.Size = new System.Drawing.Size(150, 46);
            this._btnFind.TabIndex = 3;
            this._btnFind.Text = "Find";
            this._btnFind.UseVisualStyleBackColor = true;
            this._btnFind.Click += new System.EventHandler(this.BtnFindClick);
            // 
            // btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(660, 148);
            this._btnCancel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnCancel.Name = "btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(150, 46);
            this._btnCancel.TabIndex = 4;
            this._btnCancel.Text = "Cancel";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
            // 
            // txtFind
            // 
            this._txtFind.AcceptsReturn = true;
            this._txtFind.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this._txtFind.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this._txtFind.Location = new System.Drawing.Point(29, 65);
            this._txtFind.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtFind.Name = "txtFind";
            this._txtFind.Size = new System.Drawing.Size(784, 39);
            this._txtFind.TabIndex = 6;
            this._txtFind.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtFindKeyPress);
            // 
            // QuickFind
            // 
            this.AcceptButton = this._btnFind;
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(828, 218);
            this.Controls.Add(this._txtFind);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnFind);
            this.Controls.Add(this._labelX1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "QuickFind";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Quick Find";
            this.Activated += new System.EventHandler(this.QuickFind_Activated);
            this.Deactivate += new System.EventHandler(this.QuickFind_Deactivate);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.QuickFind_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    #endregion
    private System.Windows.Forms.Label _labelX1;
    private System.Windows.Forms.Button _btnFind;
    private System.Windows.Forms.Button _btnCancel;
    private System.Windows.Forms.TextBox _txtFind;
  }
}