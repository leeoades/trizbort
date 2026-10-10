using System.Windows.Forms;

namespace Trizbort.UI.Controls
{
  public partial class Canvas
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer _components = null;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._components = new System.ComponentModel.Container();
            this._ctxCanvasMenu = new System.Windows.Forms.ContextMenuStrip(this._components);
            this._addRoomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this._sendToBackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._bringToFrontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this._lineStylesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._plainLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this._toggleDottedLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toggleDirectionalLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this._upLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._downLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this._inLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._outLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._reverseLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._darkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this._startRoomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._endRoomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this._regionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this._roomShapeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._handDrawnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._ellipseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._roundedEdgesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._octagonalEdgesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._joinRoomsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._swapObjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._objectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._namesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._formatsFillsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._regionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this._roomPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this._mapSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._applicationSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._lblZoom = new System.Windows.Forms.Label();
            this._trizbortToolTip1 = new Trizbort.UI.Controls.TrizbortToolTip();
            this._vScrollBar = new System.Windows.Forms.VScrollBar();
            this._hScrollBar = new System.Windows.Forms.HScrollBar();
            this._minimap = new Trizbort.UI.Controls.Minimap();
            this._ctxCanvasMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ctxCanvasMenu
            // 
            this._ctxCanvasMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this._ctxCanvasMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._addRoomToolStripMenuItem,
            this._toolStripSeparator7,
            this._sendToBackToolStripMenuItem,
            this._bringToFrontToolStripMenuItem,
            this._toolStripSeparator3,
            this._lineStylesMenuItem,
            this._reverseLineMenuItem,
            this._renameToolStripMenuItem,
            this._darkToolStripMenuItem,
            this._toolStripSeparator6,
            this._startRoomToolStripMenuItem,
            this._endRoomToolStripMenuItem,
            this._toolStripMenuItem1,
            this._regionToolStripMenuItem,
            this._toolStripMenuItem2,
            this._roomShapeToolStripMenuItem,
            this._joinRoomsToolStripMenuItem,
            this._swapObjectsToolStripMenuItem,
            this._toolStripSeparator1,
            this._roomPropertiesToolStripMenuItem,
            this._toolStripSeparator2,
            this._mapSettingsToolStripMenuItem,
            this._applicationSettingsToolStripMenuItem});
            this._ctxCanvasMenu.Name = "ctxCanvasMenu";
            this._ctxCanvasMenu.Size = new System.Drawing.Size(190, 398);
            this._ctxCanvasMenu.Opening += new System.ComponentModel.CancelEventHandler(this.CtxCanvasMenuOpening);
            // 
            // addRoomToolStripMenuItem
            // 
            this._addRoomToolStripMenuItem.Name = "addRoomToolStripMenuItem";
            this._addRoomToolStripMenuItem.ShortcutKeyDisplayString = "R";
            this._addRoomToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._addRoomToolStripMenuItem.Text = "Add &Room";
            this._addRoomToolStripMenuItem.Click += new System.EventHandler(this.AddRoomToolStripMenuItemClick);
            // 
            // toolStripSeparator7
            // 
            this._toolStripSeparator7.ForeColor = System.Drawing.SystemColors.ControlText;
            this._toolStripSeparator7.Name = "toolStripSeparator7";
            this._toolStripSeparator7.Size = new System.Drawing.Size(186, 6);
            // 
            // sendToBackToolStripMenuItem
            // 
            this._sendToBackToolStripMenuItem.Name = "sendToBackToolStripMenuItem";
            this._sendToBackToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._sendToBackToolStripMenuItem.Text = "Send to Back";
            this._sendToBackToolStripMenuItem.Click += new System.EventHandler(this.SendToBackToolStripMenuItemClick);
            // 
            // bringToFrontToolStripMenuItem
            // 
            this._bringToFrontToolStripMenuItem.Name = "bringToFrontToolStripMenuItem";
            this._bringToFrontToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._bringToFrontToolStripMenuItem.Text = "Bring to Front";
            this._bringToFrontToolStripMenuItem.Click += new System.EventHandler(this.BringToFrontToolStripMenuItemClick);
            // 
            // toolStripSeparator3
            // 
            this._toolStripSeparator3.ForeColor = System.Drawing.SystemColors.ControlText;
            this._toolStripSeparator3.Name = "toolStripSeparator3";
            this._toolStripSeparator3.Size = new System.Drawing.Size(186, 6);
            // 
            // m_lineStylesMenuItem
            // 
            this._lineStylesMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._plainLinesMenuItem,
            this._toolStripMenuItem3,
            this._toggleDottedLinesMenuItem,
            this._toggleDirectionalLinesMenuItem,
            this._toolStripSeparator4,
            this._upLinesMenuItem,
            this._downLinesMenuItem,
            this._toolStripSeparator5,
            this._inLinesMenuItem,
            this._outLinesMenuItem});
            this._lineStylesMenuItem.Name = "m_lineStylesMenuItem";
            this._lineStylesMenuItem.ShortcutKeyDisplayString = "";
            this._lineStylesMenuItem.Size = new System.Drawing.Size(189, 22);
            this._lineStylesMenuItem.Text = "&Line Styles";
            // 
            // m_plainLinesMenuItem
            // 
            this._plainLinesMenuItem.Name = "m_plainLinesMenuItem";
            this._plainLinesMenuItem.ShortcutKeyDisplayString = "P";
            this._plainLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._plainLinesMenuItem.Text = "Plain";
            this._plainLinesMenuItem.Click += new System.EventHandler(this.PlainLinesMenuItemClick);
            // 
            // toolStripMenuItem3
            // 
            this._toolStripMenuItem3.Name = "toolStripMenuItem3";
            this._toolStripMenuItem3.Size = new System.Drawing.Size(169, 6);
            // 
            // m_toggleDottedLinesMenuItem
            // 
            this._toggleDottedLinesMenuItem.Name = "m_toggleDottedLinesMenuItem";
            this._toggleDottedLinesMenuItem.ShortcutKeyDisplayString = "T";
            this._toggleDottedLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._toggleDottedLinesMenuItem.Text = "Dotted";
            this._toggleDottedLinesMenuItem.Click += new System.EventHandler(this.ToggleDottedLinesMenuItemClick);
            // 
            // m_toggleDirectionalLinesMenuItem
            // 
            this._toggleDirectionalLinesMenuItem.Name = "m_toggleDirectionalLinesMenuItem";
            this._toggleDirectionalLinesMenuItem.ShortcutKeyDisplayString = "A";
            this._toggleDirectionalLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._toggleDirectionalLinesMenuItem.Text = "One Way Arrow";
            this._toggleDirectionalLinesMenuItem.Click += new System.EventHandler(this.ToggleDirectionalLinesMenuItemClick);
            // 
            // toolStripSeparator4
            // 
            this._toolStripSeparator4.Name = "toolStripSeparator4";
            this._toolStripSeparator4.Size = new System.Drawing.Size(169, 6);
            // 
            // m_upLinesMenuItem
            // 
            this._upLinesMenuItem.Name = "m_upLinesMenuItem";
            this._upLinesMenuItem.ShortcutKeyDisplayString = "U";
            this._upLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._upLinesMenuItem.Text = "Up";
            this._upLinesMenuItem.Click += new System.EventHandler(this.UpLinesMenuItemClick);
            // 
            // m_downLinesMenuItem
            // 
            this._downLinesMenuItem.Name = "m_downLinesMenuItem";
            this._downLinesMenuItem.ShortcutKeyDisplayString = "D";
            this._downLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._downLinesMenuItem.Text = "Down";
            this._downLinesMenuItem.Click += new System.EventHandler(this.DownLinesMenuItemClick);
            // 
            // toolStripSeparator5
            // 
            this._toolStripSeparator5.Name = "toolStripSeparator5";
            this._toolStripSeparator5.Size = new System.Drawing.Size(169, 6);
            // 
            // m_inLinesMenuItem
            // 
            this._inLinesMenuItem.Name = "m_inLinesMenuItem";
            this._inLinesMenuItem.ShortcutKeyDisplayString = "I";
            this._inLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._inLinesMenuItem.Text = "In";
            this._inLinesMenuItem.Click += new System.EventHandler(this.InLinesMenuItemClick);
            // 
            // m_outLinesMenuItem
            // 
            this._outLinesMenuItem.Name = "m_outLinesMenuItem";
            this._outLinesMenuItem.ShortcutKeyDisplayString = "O";
            this._outLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._outLinesMenuItem.Text = "Out";
            this._outLinesMenuItem.Click += new System.EventHandler(this.OutLinesMenuItemClick);
            // 
            // m_reverseLineMenuItem
            // 
            this._reverseLineMenuItem.Name = "m_reverseLineMenuItem";
            this._reverseLineMenuItem.ShortcutKeyDisplayString = "V";
            this._reverseLineMenuItem.Size = new System.Drawing.Size(189, 22);
            this._reverseLineMenuItem.Text = "Reverse Line";
            this._reverseLineMenuItem.Click += new System.EventHandler(this.ReverseLineMenuItemClick);
            // 
            // renameToolStripMenuItem
            // 
            this._renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            this._renameToolStripMenuItem.ShortcutKeyDisplayString = "F2";
            this._renameToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._renameToolStripMenuItem.Text = "Rename";
            this._renameToolStripMenuItem.Visible = false;
            this._renameToolStripMenuItem.Click += new System.EventHandler(this.RenameToolStripMenuItemClick);
            // 
            // darkToolStripMenuItem
            // 
            this._darkToolStripMenuItem.Name = "darkToolStripMenuItem";
            this._darkToolStripMenuItem.ShortcutKeyDisplayString = "K";
            this._darkToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._darkToolStripMenuItem.Text = "Toggle &Darkness";
            this._darkToolStripMenuItem.Visible = false;
            this._darkToolStripMenuItem.Click += new System.EventHandler(this.DarkToolStripMenuItemClick1);
            // 
            // toolStripSeparator6
            // 
            this._toolStripSeparator6.Name = "toolStripSeparator6";
            this._toolStripSeparator6.Size = new System.Drawing.Size(186, 6);
            // 
            // startRoomToolStripMenuItem
            // 
            this._startRoomToolStripMenuItem.Name = "startRoomToolStripMenuItem";
            this._startRoomToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F5)));
            this._startRoomToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._startRoomToolStripMenuItem.Text = "Start Room";
            this._startRoomToolStripMenuItem.Click += new System.EventHandler(this.StartRoomToolStripMenuItemClick);
            // 
            // endRoomToolStripMenuItem
            // 
            this._endRoomToolStripMenuItem.Name = "endRoomToolStripMenuItem";
            this._endRoomToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F5)));
            this._endRoomToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._endRoomToolStripMenuItem.Text = "End Room";
            this._endRoomToolStripMenuItem.Click += new System.EventHandler(this.EndRoomToolStripMenuItemClick);
            // 
            // toolStripMenuItem1
            // 
            this._toolStripMenuItem1.Name = "toolStripMenuItem1";
            this._toolStripMenuItem1.Size = new System.Drawing.Size(186, 6);
            this._toolStripMenuItem1.Visible = false;
            // 
            // regionToolStripMenuItem
            // 
            this._regionToolStripMenuItem.Name = "regionToolStripMenuItem";
            this._regionToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._regionToolStripMenuItem.Text = "&Set Region";
            this._regionToolStripMenuItem.Visible = false;
            // 
            // toolStripMenuItem2
            // 
            this._toolStripMenuItem2.Name = "toolStripMenuItem2";
            this._toolStripMenuItem2.Size = new System.Drawing.Size(186, 6);
            this._toolStripMenuItem2.Visible = false;
            // 
            // roomShapeToolStripMenuItem
            // 
            this._roomShapeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._handDrawnToolStripMenuItem,
            this._ellipseToolStripMenuItem,
            this._roundedEdgesToolStripMenuItem,
            this._octagonalEdgesToolStripMenuItem});
            this._roomShapeToolStripMenuItem.Name = "roomShapeToolStripMenuItem";
            this._roomShapeToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._roomShapeToolStripMenuItem.Text = "Room Shape";
            this._roomShapeToolStripMenuItem.Visible = false;
            // 
            // handDrawnToolStripMenuItem
            // 
            this._handDrawnToolStripMenuItem.Name = "handDrawnToolStripMenuItem";
            this._handDrawnToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+H";
            this._handDrawnToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._handDrawnToolStripMenuItem.Text = "Square Corners";
            this._handDrawnToolStripMenuItem.Click += new System.EventHandler(this.HandDrawnToolStripMenuItemClick);
            // 
            // ellipseToolStripMenuItem
            // 
            this._ellipseToolStripMenuItem.Name = "ellipseToolStripMenuItem";
            this._ellipseToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
            this._ellipseToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._ellipseToolStripMenuItem.Text = "Ellipse";
            this._ellipseToolStripMenuItem.Click += new System.EventHandler(this.EllipseToolStripMenuItemClick);
            // 
            // roundedEdgesToolStripMenuItem
            // 
            this._roundedEdgesToolStripMenuItem.Name = "roundedEdgesToolStripMenuItem";
            this._roundedEdgesToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+R";
            this._roundedEdgesToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._roundedEdgesToolStripMenuItem.Text = "Rounded Edges";
            this._roundedEdgesToolStripMenuItem.Click += new System.EventHandler(this.RoundedEdgesToolStripMenuItemClick);
            // 
            // octagonalEdgesToolStripMenuItem
            // 
            this._octagonalEdgesToolStripMenuItem.Name = "octagonalEdgesToolStripMenuItem";
            this._octagonalEdgesToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+8";
            this._octagonalEdgesToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._octagonalEdgesToolStripMenuItem.Text = "Octagonal Edges";
            this._octagonalEdgesToolStripMenuItem.Click += new System.EventHandler(this.OctagonalEdgesToolStripMenuItemClick);
            // 
            // joinRoomsToolStripMenuItem
            // 
            this._joinRoomsToolStripMenuItem.Name = "joinRoomsToolStripMenuItem";
            this._joinRoomsToolStripMenuItem.ShortcutKeyDisplayString = "J";
            this._joinRoomsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._joinRoomsToolStripMenuItem.Text = "&Join Rooms";
            this._joinRoomsToolStripMenuItem.Visible = false;
            this._joinRoomsToolStripMenuItem.Click += new System.EventHandler(this.JoinRoomsToolStripMenuItemClick);
            // 
            // swapObjectsToolStripMenuItem
            // 
            this._swapObjectsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._objectsToolStripMenuItem,
            this._namesToolStripMenuItem,
            this._formatsFillsToolStripMenuItem,
            this._regionsToolStripMenuItem});
            this._swapObjectsToolStripMenuItem.Name = "swapObjectsToolStripMenuItem";
            this._swapObjectsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._swapObjectsToolStripMenuItem.Text = "S&wap";
            this._swapObjectsToolStripMenuItem.Visible = false;
            // 
            // objectsToolStripMenuItem
            // 
            this._objectsToolStripMenuItem.Name = "objectsToolStripMenuItem";
            this._objectsToolStripMenuItem.ShortcutKeyDisplayString = "W";
            this._objectsToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this._objectsToolStripMenuItem.Text = "Objects";
            this._objectsToolStripMenuItem.Click += new System.EventHandler(this.ObjectsToolStripMenuItemClick);
            // 
            // namesToolStripMenuItem
            // 
            this._namesToolStripMenuItem.Name = "namesToolStripMenuItem";
            this._namesToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+W";
            this._namesToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this._namesToolStripMenuItem.Text = "Names";
            this._namesToolStripMenuItem.Click += new System.EventHandler(this.NamesToolStripMenuItemClick);
            // 
            // formatsFillsToolStripMenuItem
            // 
            this._formatsFillsToolStripMenuItem.Name = "formatsFillsToolStripMenuItem";
            this._formatsFillsToolStripMenuItem.ShortcutKeyDisplayString = "Shift+W";
            this._formatsFillsToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this._formatsFillsToolStripMenuItem.Text = "Formats / Fills";
            this._formatsFillsToolStripMenuItem.Click += new System.EventHandler(this.FormatsFillsToolStripMenuItemClick);
            // 
            // regionsToolStripMenuItem
            // 
            this._regionsToolStripMenuItem.Name = "regionsToolStripMenuItem";
            this._regionsToolStripMenuItem.ShortcutKeyDisplayString = "Alt+W";
            this._regionsToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this._regionsToolStripMenuItem.Text = "Regions";
            this._regionsToolStripMenuItem.Click += new System.EventHandler(this.RegionsToolStripMenuItemClick);
            // 
            // toolStripSeparator1
            // 
            this._toolStripSeparator1.Name = "toolStripSeparator1";
            this._toolStripSeparator1.Size = new System.Drawing.Size(186, 6);
            this._toolStripSeparator1.Visible = false;
            // 
            // roomPropertiesToolStripMenuItem
            // 
            this._roomPropertiesToolStripMenuItem.Name = "roomPropertiesToolStripMenuItem";
            this._roomPropertiesToolStripMenuItem.ShortcutKeyDisplayString = "Enter";
            this._roomPropertiesToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._roomPropertiesToolStripMenuItem.Text = "&Properties...";
            this._roomPropertiesToolStripMenuItem.Visible = false;
            this._roomPropertiesToolStripMenuItem.Click += new System.EventHandler(this.RoomPropertiesToolStripMenuItemClick);
            // 
            // toolStripSeparator2
            // 
            this._toolStripSeparator2.Name = "toolStripSeparator2";
            this._toolStripSeparator2.Size = new System.Drawing.Size(186, 6);
            this._toolStripSeparator2.Visible = false;
            // 
            // mapSettingsToolStripMenuItem
            // 
            this._mapSettingsToolStripMenuItem.Name = "mapSettingsToolStripMenuItem";
            this._mapSettingsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._mapSettingsToolStripMenuItem.Text = "&Map Settings...";
            this._mapSettingsToolStripMenuItem.Click += new System.EventHandler(this.MapSettingsToolStripMenuItemClick);
            // 
            // applicationSettingsToolStripMenuItem
            // 
            this._applicationSettingsToolStripMenuItem.Name = "applicationSettingsToolStripMenuItem";
            this._applicationSettingsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this._applicationSettingsToolStripMenuItem.Text = "&Application Settings...";
            this._applicationSettingsToolStripMenuItem.Click += new System.EventHandler(this.ApplicationSettingsToolStripMenuItemClick);
            // 
            // lblZoom
            // 
            this._lblZoom.AutoSize = true;
            this._lblZoom.BackColor = System.Drawing.Color.Transparent;
            this._lblZoom.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._lblZoom.ForeColor = System.Drawing.SystemColors.GrayText;
            this._lblZoom.Location = new System.Drawing.Point(2, 2);
            this._lblZoom.Name = "lblZoom";
            this._lblZoom.Size = new System.Drawing.Size(50, 20);
            this._lblZoom.TabIndex = 7;
            this._lblZoom.Text = "Zoom";
            // 
            // trizbortToolTip1
            // 
            this._trizbortToolTip1.BackColor = System.Drawing.Color.LightBlue;
            this._trizbortToolTip1.BodyText = null;
            this._trizbortToolTip1.FooterText = null;
            this._trizbortToolTip1.ForeColor = System.Drawing.Color.Black;
            this._trizbortToolTip1.GradientColor = System.Drawing.Color.Empty;
            this._trizbortToolTip1.HoverElement = null;
            this._trizbortToolTip1.IsShown = false;
            this._trizbortToolTip1.LastOwner = null;
            this._trizbortToolTip1.OwnerDraw = true;
            this._trizbortToolTip1.TitleText = null;
            // 
            // m_vScrollBar
            // 
            this._vScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            this._vScrollBar.Location = new System.Drawing.Point(704, 0);
            this._vScrollBar.Name = "m_vScrollBar";
            this._vScrollBar.Size = new System.Drawing.Size(14, 492);
            this._vScrollBar.TabIndex = 8;
            this._vScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ScrollBar_Scroll);
            // 
            // m_hScrollBar
            // 
            this._hScrollBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._hScrollBar.Location = new System.Drawing.Point(0, 481);
            this._hScrollBar.Name = "m_hScrollBar";
            this._hScrollBar.Size = new System.Drawing.Size(704, 11);
            this._hScrollBar.TabIndex = 9;
            this._hScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ScrollBar_Scroll);
            // 
            // m_minimap
            // 
            this._minimap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._minimap.AutoSize = true;
            this._minimap.Canvas = null;
            this._minimap.Location = new System.Drawing.Point(504, 0);
            this._minimap.Name = "m_minimap";
            this._minimap.Size = new System.Drawing.Size(200, 124);
            this._minimap.TabIndex = 10;
            this._minimap.TabStop = false;
            // 
            // Canvas
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.AutoScroll = true;
            this.ContextMenuStrip = this._ctxCanvasMenu;
            this.Controls.Add(this._minimap);
            this.Controls.Add(this._hScrollBar);
            this.Controls.Add(this._vScrollBar);
            this.Controls.Add(this._lblZoom);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Canvas";
            this.Size = new System.Drawing.Size(718, 492);
            this._ctxCanvasMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip _ctxCanvasMenu;
        private System.Windows.Forms.ToolStripMenuItem _regionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _darkToolStripMenuItem;
        private System.Windows.Forms.Label _lblZoom;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem _roomPropertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem _mapSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _applicationSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _joinRoomsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _swapObjectsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _objectsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _namesToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _formatsFillsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _regionsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roomShapeToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _renameToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem1;
    private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem2;
    private System.Windows.Forms.ToolStripMenuItem _handDrawnToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _ellipseToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roundedEdgesToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _octagonalEdgesToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _addRoomToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator3;
    private System.Windows.Forms.ToolStripMenuItem _lineStylesMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _plainLinesMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem3;
    private System.Windows.Forms.ToolStripMenuItem _toggleDottedLinesMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _toggleDirectionalLinesMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator4;
    private System.Windows.Forms.ToolStripMenuItem _upLinesMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _downLinesMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator5;
    private System.Windows.Forms.ToolStripMenuItem _inLinesMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _outLinesMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _reverseLineMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _startRoomToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _endRoomToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator6;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator7;
    private System.Windows.Forms.ToolStripMenuItem _sendToBackToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _bringToFrontToolStripMenuItem;
    private TrizbortToolTip _trizbortToolTip1;
    private System.Windows.Forms.VScrollBar _vScrollBar;
    private System.Windows.Forms.HScrollBar _hScrollBar;
    private Minimap _minimap;
  }
}
