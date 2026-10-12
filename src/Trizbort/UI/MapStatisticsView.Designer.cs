namespace Trizbort.UI
{
  partial class MapStatisticsView
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
            this._txtStats = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtStats
            // 
            this._txtStats.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._txtStats.Location = new System.Drawing.Point(26, 26);
            this._txtStats.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtStats.Multiline = true;
            this._txtStats.Name = "txtStats";
            this._txtStats.ReadOnly = true;
            this._txtStats.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtStats.Size = new System.Drawing.Size(800, 616);
            this._txtStats.TabIndex = 0;
            // 
            // MapStatisticsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(850, 666);
            this.Controls.Add(this._txtStats);
            this.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MapStatisticsView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Map Statistics";
            this.Load += new System.EventHandler(this.MapStatisticsView_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox _txtStats;
  }
}