using Trizbort.UI.Controls;

namespace Trizbort.UI
{
  public partial class MainForm
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
            this._components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._menuStrip = new System.Windows.Forms.MenuStrip();
            this._fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._fileNewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this._fileOpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileOpenFromWebMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this._fileSaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileSaveAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._smartSaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this._fileExportMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportPDFMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportImageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem9 = new System.Windows.Forms.ToolStripSeparator();
            this._fileExportAlanMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportAdventuronMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportHugoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportInform7MenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportInform6MenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportTADSMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._zILToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExportQuestMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this._alanToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._adventuronToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._hugoToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._inform7ToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._inform6ToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._tADSToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._zILToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._questToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._questRoomsToTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this._fileRecentMapsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
            this._fileExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this._toggleTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this._editSelectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editSelectNoneMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._selectSpecialToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._selectAllRoomsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._selectedUnconnectedRoomsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._selectRoomsWObjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._selectRoomsWoObjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
            this._selectAllConnectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._selectDanglingConnectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._selectSelfLoopingConnectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this._editCopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editCopyColorToolMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editPasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
            this._editDeleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this._editPropertiesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._roomsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editAddRoomMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editRenameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editChangeRegionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
            this._editIsDarkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._makeRoomDarkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._makeRoomLightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
            this._startRoomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._endRoomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this._roomShapeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._handDrawnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._ellipseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._roundedEdgesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._octagonalEdgesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
            this._joinRoomsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
            this._swapObjectsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._swapNamesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._swapFormatsFillsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._swapRegionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._connectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._lineStylesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._plainLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this._toggleDottedLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toggleDirectionalLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this._upLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._downLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this._inLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._outLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._reverseLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._validationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._roomsMustHaveUniqueNamesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._roomsMustHaveADescriptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._roomsMustHaveASubtitleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._roomsMustNotHaveADanglingConnectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._viewZoomMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._viewZoomInMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewZoomOutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this._viewZoomFiftyPercentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewZoomOneHundredPercentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewZoomTwoHundredPercentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator20 = new System.Windows.Forms.ToolStripSeparator();
            this._viewZoomMiniIn = new System.Windows.Forms.ToolStripMenuItem();
            this._viewZoomMiniOut = new System.Windows.Forms.ToolStripMenuItem();
            this._viewEntireMapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewResetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
            this._viewMinimapMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator21 = new System.Windows.Forms.ToolStripSeparator();
            this._viewShowGridMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._automappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._automapStartMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._automapStopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._projectMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._projectSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._appSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this._projectResetToDefaultSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
            this._mapStatisticsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._mapStatisticsExportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._helpMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._onlineHelpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this._helpAboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStrip = new System.Windows.Forms.ToolStrip();
            this._toggleDottedLinesButton = new System.Windows.Forms.ToolStripButton();
            this._toggleDirectionalLinesButton = new System.Windows.Forms.ToolStripButton();
            this._toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this._statusBar = new System.Windows.Forms.StatusStrip();
            this.Canvas = new Trizbort.UI.Controls.Canvas();
            this._automapBar = new Trizbort.UI.Controls.AutomapBar();
            this._menuStrip.SuspendLayout();
            this._toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_menuStrip
            // 
            this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileMenu,
            this._editMenu,
            this._roomsToolStripMenuItem,
            this._connectionsToolStripMenuItem,
            this._validationToolStripMenuItem,
            this._viewMenu,
            this._automappingToolStripMenuItem,
            this._projectMenu,
            this._helpMenu});
            this._menuStrip.Location = new System.Drawing.Point(0, 0);
            this._menuStrip.Name = "m_menuStrip";
            this._menuStrip.Padding = new System.Windows.Forms.Padding(3, 1, 0, 1);
            this._menuStrip.Size = new System.Drawing.Size(962, 24);
            this._menuStrip.TabIndex = 1;
            // 
            // m_fileMenu
            // 
            this._fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileNewMenuItem,
            this._toolStripSeparator1,
            this._fileOpenMenuItem,
            this._fileOpenFromWebMenuItem,
            this._toolStripSeparator9,
            this._fileSaveMenuItem,
            this._fileSaveAsMenuItem,
            this._smartSaveToolStripMenuItem,
            this._toolStripSeparator10,
            this._fileExportMenu,
            this._toolStripSeparator12,
            this._fileRecentMapsMenuItem,
            this._toolStripMenuItem7,
            this._fileExitMenuItem});
            this._fileMenu.Name = "m_fileMenu";
            this._fileMenu.Size = new System.Drawing.Size(37, 22);
            this._fileMenu.Text = "&File";
            this._fileMenu.DropDownOpening += new System.EventHandler(this.FileMenu_DropDownOpening);
            // 
            // m_fileNewMenuItem
            // 
            this._fileNewMenuItem.Name = "m_fileNewMenuItem";
            this._fileNewMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this._fileNewMenuItem.Size = new System.Drawing.Size(270, 22);
            this._fileNewMenuItem.Text = "&New Map";
            this._fileNewMenuItem.Click += new System.EventHandler(this.FileNewMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this._toolStripSeparator1.Name = "toolStripSeparator1";
            this._toolStripSeparator1.Size = new System.Drawing.Size(267, 6);
            // 
            // m_fileOpenMenuItem
            // 
            this._fileOpenMenuItem.Name = "m_fileOpenMenuItem";
            this._fileOpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this._fileOpenMenuItem.Size = new System.Drawing.Size(270, 22);
            this._fileOpenMenuItem.Text = "&Open Map...";
            this._fileOpenMenuItem.Click += new System.EventHandler(this.FileOpenMenuItem_Click);
            // 
            // m_fileOpenFromWebMenuItem
            // 
            this._fileOpenFromWebMenuItem.Name = "m_fileOpenFromWebMenuItem";
            this._fileOpenFromWebMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.O)));
            this._fileOpenFromWebMenuItem.Size = new System.Drawing.Size(270, 22);
            this._fileOpenFromWebMenuItem.Text = "Open Map from Web...";
            this._fileOpenFromWebMenuItem.Click += new System.EventHandler(this.FileOpenFromWebMenuItemClick);
            // 
            // toolStripSeparator9
            // 
            this._toolStripSeparator9.Name = "toolStripSeparator9";
            this._toolStripSeparator9.Size = new System.Drawing.Size(267, 6);
            // 
            // m_fileSaveMenuItem
            // 
            this._fileSaveMenuItem.Name = "m_fileSaveMenuItem";
            this._fileSaveMenuItem.ShortcutKeyDisplayString = "";
            this._fileSaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this._fileSaveMenuItem.Size = new System.Drawing.Size(270, 22);
            this._fileSaveMenuItem.Text = "&Save Map";
            this._fileSaveMenuItem.Click += new System.EventHandler(this.FileSaveMenuItem_Click);
            // 
            // m_fileSaveAsMenuItem
            // 
            this._fileSaveAsMenuItem.Name = "m_fileSaveAsMenuItem";
            this._fileSaveAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.S)));
            this._fileSaveAsMenuItem.Size = new System.Drawing.Size(270, 22);
            this._fileSaveAsMenuItem.Text = "Save Map &As...";
            this._fileSaveAsMenuItem.Click += new System.EventHandler(this.FileSaveAsMenuItem_Click);
            // 
            // smartSaveToolStripMenuItem
            // 
            this._smartSaveToolStripMenuItem.Name = "smartSaveToolStripMenuItem";
            this._smartSaveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.S)));
            this._smartSaveToolStripMenuItem.Size = new System.Drawing.Size(270, 22);
            this._smartSaveToolStripMenuItem.Text = "S&mart Save";
            this._smartSaveToolStripMenuItem.Click += new System.EventHandler(this.SmartSaveToolStripMenuItemClick);
            // 
            // toolStripSeparator10
            // 
            this._toolStripSeparator10.Name = "toolStripSeparator10";
            this._toolStripSeparator10.Size = new System.Drawing.Size(267, 6);
            // 
            // m_fileExportMenu
            // 
            this._fileExportMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileExportPDFMenuItem,
            this._fileExportImageMenuItem,
            this._toolStripMenuItem9,
            this._fileExportAlanMenuItem,
            this._fileExportAdventuronMenuItem,
            this._fileExportHugoMenuItem,
            this._fileExportInform7MenuItem,
            this._fileExportInform6MenuItem,
            this._fileExportTADSMenuItem,
            this._zILToolStripMenuItem,
            this._fileExportQuestMenuItem,
            this._toolStripSeparator13,
            this._alanToTextToolStripMenuItem,
            this._adventuronToTextToolStripMenuItem,
            this._hugoToTextToolStripMenuItem,
            this._inform7ToTextToolStripMenuItem,
            this._inform6ToTextToolStripMenuItem,
            this._tADSToTextToolStripMenuItem,
            this._zILToClipboardToolStripMenuItem,
            this._questToTextToolStripMenuItem,
            this._questRoomsToTextToolStripMenuItem});
            this._fileExportMenu.Name = "m_fileExportMenu";
            this._fileExportMenu.Size = new System.Drawing.Size(270, 22);
            this._fileExportMenu.Text = "&Export";
            // 
            // m_fileExportPDFMenuItem
            // 
            this._fileExportPDFMenuItem.Name = "m_fileExportPDFMenuItem";
            this._fileExportPDFMenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportPDFMenuItem.Text = "&PDF...";
            this._fileExportPDFMenuItem.Click += new System.EventHandler(this.FileExportPDFMenuItem_Click);
            // 
            // m_fileExportImageMenuItem
            // 
            this._fileExportImageMenuItem.Name = "m_fileExportImageMenuItem";
            this._fileExportImageMenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportImageMenuItem.Text = "&Image...";
            this._fileExportImageMenuItem.Click += new System.EventHandler(this.FileExportImageMenuItem_Click);
            // 
            // toolStripMenuItem9
            // 
            this._toolStripMenuItem9.Name = "toolStripMenuItem9";
            this._toolStripMenuItem9.Size = new System.Drawing.Size(301, 6);
            // 
            // m_fileExportAlanMenuItem
            // 
            this._fileExportAlanMenuItem.Name = "m_fileExportAlanMenuItem";
            this._fileExportAlanMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.L)));
            this._fileExportAlanMenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportAlanMenuItem.Text = "&Alan...";
            this._fileExportAlanMenuItem.Click += new System.EventHandler(this.FileExportAlanMenuItem_Click);
            // 
            // m_fileExportAdventuronMenuItem
            // 
            this._fileExportAdventuronMenuItem.Name = "m_fileExportAdventuronMenuItem";
            this._fileExportAdventuronMenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportAdventuronMenuItem.Text = "&Adventuron...";
            this._fileExportAdventuronMenuItem.Click += new System.EventHandler(this.FileExportAdventuronMenuItem_Click);
            // 
            // m_fileExportHugoMenuItem
            // 
            this._fileExportHugoMenuItem.Name = "m_fileExportHugoMenuItem";
            this._fileExportHugoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.H)));
            this._fileExportHugoMenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportHugoMenuItem.Text = "&Hugo...";
            this._fileExportHugoMenuItem.Click += new System.EventHandler(this.FileExportHugoMenuItem_Click);
            // 
            // m_fileExportInform7MenuItem
            // 
            this._fileExportInform7MenuItem.Name = "m_fileExportInform7MenuItem";
            this._fileExportInform7MenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.D7)));
            this._fileExportInform7MenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportInform7MenuItem.Text = "Inform &7...";
            this._fileExportInform7MenuItem.Click += new System.EventHandler(this.FileExportInform7MenuItem_Click);
            // 
            // m_fileExportInform6MenuItem
            // 
            this._fileExportInform6MenuItem.Name = "m_fileExportInform6MenuItem";
            this._fileExportInform6MenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.D6)));
            this._fileExportInform6MenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportInform6MenuItem.Text = "Inform &6...";
            this._fileExportInform6MenuItem.Click += new System.EventHandler(this.FileExportInform6MenuItem_Click);
            // 
            // m_fileExportTADSMenuItem
            // 
            this._fileExportTADSMenuItem.Name = "m_fileExportTADSMenuItem";
            this._fileExportTADSMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.T)));
            this._fileExportTADSMenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportTADSMenuItem.Text = "&TADS...";
            this._fileExportTADSMenuItem.Click += new System.EventHandler(this.FileExportTadsMenuItem_Click);
            // 
            // zILToolStripMenuItem
            // 
            this._zILToolStripMenuItem.Name = "zILToolStripMenuItem";
            this._zILToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.Z)));
            this._zILToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._zILToolStripMenuItem.Text = "ZIL...";
            this._zILToolStripMenuItem.Click += new System.EventHandler(this.ZILToolStripMenuItemClick);
            // 
            // m_fileExportQuestMenuItem
            // 
            this._fileExportQuestMenuItem.Name = "m_fileExportQuestMenuItem";
            this._fileExportQuestMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.Q)));
            this._fileExportQuestMenuItem.Size = new System.Drawing.Size(304, 22);
            this._fileExportQuestMenuItem.Text = "&Quest...";
            this._fileExportQuestMenuItem.Click += new System.EventHandler(this.FileExportQuestMenuItem_Click);
            // 
            // toolStripSeparator13
            // 
            this._toolStripSeparator13.Name = "toolStripSeparator13";
            this._toolStripSeparator13.Size = new System.Drawing.Size(301, 6);
            // 
            // alanToTextToolStripMenuItem
            // 
            this._alanToTextToolStripMenuItem.Name = "alanToTextToolStripMenuItem";
            this._alanToTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.L)));
            this._alanToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._alanToTextToolStripMenuItem.Text = "Alan to Clipboard";
            this._alanToTextToolStripMenuItem.Click += new System.EventHandler(this.AlanToTextToolStripMenuItemClick);
            // 
            // adventuronToTextToolStripMenuItem
            // 
            this._adventuronToTextToolStripMenuItem.Name = "adventuronToTextToolStripMenuItem";
            this._adventuronToTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.A)));
            this._adventuronToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._adventuronToTextToolStripMenuItem.Text = "Adventuron to Clipboard";
            this._adventuronToTextToolStripMenuItem.Click += new System.EventHandler(this.AdventuronToTextToolStripMenuItemClick);
            // 
            // hugoToTextToolStripMenuItem
            // 
            this._hugoToTextToolStripMenuItem.Name = "hugoToTextToolStripMenuItem";
            this._hugoToTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.H)));
            this._hugoToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._hugoToTextToolStripMenuItem.Text = "Hugo to Clipboard";
            this._hugoToTextToolStripMenuItem.Click += new System.EventHandler(this.HugoToTextToolStripMenuItemClick);
            // 
            // inform7ToTextToolStripMenuItem
            // 
            this._inform7ToTextToolStripMenuItem.Name = "inform7ToTextToolStripMenuItem";
            this._inform7ToTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.D7)));
            this._inform7ToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._inform7ToTextToolStripMenuItem.Text = "Inform 7 to Clipboard";
            this._inform7ToTextToolStripMenuItem.Click += new System.EventHandler(this.Inform7ToTextToolStripMenuItemClick);
            // 
            // inform6ToTextToolStripMenuItem
            // 
            this._inform6ToTextToolStripMenuItem.Name = "inform6ToTextToolStripMenuItem";
            this._inform6ToTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.D6)));
            this._inform6ToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._inform6ToTextToolStripMenuItem.Text = "Inform 6 to Clipboard";
            this._inform6ToTextToolStripMenuItem.Click += new System.EventHandler(this.Inform6ToTextToolStripMenuItemClick);
            // 
            // tADSToTextToolStripMenuItem
            // 
            this._tADSToTextToolStripMenuItem.Name = "tADSToTextToolStripMenuItem";
            this._tADSToTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.T)));
            this._tADSToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._tADSToTextToolStripMenuItem.Text = "TADS to Clipboard";
            this._tADSToTextToolStripMenuItem.Click += new System.EventHandler(this.TADSToTextToolStripMenuItemClick);
            // 
            // zILToClipboardToolStripMenuItem
            // 
            this._zILToClipboardToolStripMenuItem.Name = "zILToClipboardToolStripMenuItem";
            this._zILToClipboardToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.Z)));
            this._zILToClipboardToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._zILToClipboardToolStripMenuItem.Text = "ZIL to Clipboard";
            this._zILToClipboardToolStripMenuItem.Click += new System.EventHandler(this.ZILToClipboardToolStripMenuItemClick);
            // 
            // questToTextToolStripMenuItem
            // 
            this._questToTextToolStripMenuItem.Name = "questToTextToolStripMenuItem";
            this._questToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._questToTextToolStripMenuItem.Text = "Quest to Clipboard";
            this._questToTextToolStripMenuItem.Click += new System.EventHandler(this.QuestToTextToolStripMenuItemClick);
            // 
            // questRoomsToTextToolStripMenuItem
            // 
            this._questRoomsToTextToolStripMenuItem.Name = "questRoomsToTextToolStripMenuItem";
            this._questRoomsToTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.Q)));
            this._questRoomsToTextToolStripMenuItem.Size = new System.Drawing.Size(304, 22);
            this._questRoomsToTextToolStripMenuItem.Text = "Quest to Clipboard (no header)";
            this._questRoomsToTextToolStripMenuItem.Click += new System.EventHandler(this.QuestRoomsToTextToolStripMenuItemClick);
            // 
            // toolStripSeparator12
            // 
            this._toolStripSeparator12.Name = "toolStripSeparator12";
            this._toolStripSeparator12.Size = new System.Drawing.Size(267, 6);
            // 
            // m_fileRecentMapsMenuItem
            // 
            this._fileRecentMapsMenuItem.Name = "m_fileRecentMapsMenuItem";
            this._fileRecentMapsMenuItem.Size = new System.Drawing.Size(270, 22);
            this._fileRecentMapsMenuItem.Text = "&Recent Maps";
            // 
            // toolStripMenuItem7
            // 
            this._toolStripMenuItem7.Name = "toolStripMenuItem7";
            this._toolStripMenuItem7.Size = new System.Drawing.Size(267, 6);
            // 
            // m_fileExitMenuItem
            // 
            this._fileExitMenuItem.Name = "m_fileExitMenuItem";
            this._fileExitMenuItem.Size = new System.Drawing.Size(270, 22);
            this._fileExitMenuItem.Text = "E&xit";
            this._fileExitMenuItem.Click += new System.EventHandler(this.FileExitMenuItem_Click);
            // 
            // m_editMenu
            // 
            this._editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripSeparator6,
            this._toggleTextToolStripMenuItem,
            this._toolStripSeparator8,
            this._editSelectAllMenuItem,
            this._editSelectNoneMenuItem,
            this._selectSpecialToolStripMenuItem,
            this._toolStripSeparator5,
            this._editCopyMenuItem,
            this._editCopyColorToolMenuItem,
            this._editPasteMenuItem,
            this._toolStripMenuItem8,
            this._editDeleteMenuItem,
            this._toolStripSeparator3,
            this._editPropertiesMenuItem});
            this._editMenu.Name = "m_editMenu";
            this._editMenu.Size = new System.Drawing.Size(39, 22);
            this._editMenu.Text = "&Edit";
            // 
            // toolStripSeparator6
            // 
            this._toolStripSeparator6.Name = "toolStripSeparator6";
            this._toolStripSeparator6.Size = new System.Drawing.Size(208, 6);
            // 
            // toggleTextToolStripMenuItem
            // 
            this._toggleTextToolStripMenuItem.Name = "toggleTextToolStripMenuItem";
            this._toggleTextToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F4)));
            this._toggleTextToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this._toggleTextToolStripMenuItem.Text = "Toggle Text";
            this._toggleTextToolStripMenuItem.Click += new System.EventHandler(this.ToggleTextToolStripMenuItemClick);
            // 
            // toolStripSeparator8
            // 
            this._toolStripSeparator8.Name = "toolStripSeparator8";
            this._toolStripSeparator8.Size = new System.Drawing.Size(208, 6);
            // 
            // m_editSelectAllMenuItem
            // 
            this._editSelectAllMenuItem.Name = "m_editSelectAllMenuItem";
            this._editSelectAllMenuItem.ShortcutKeyDisplayString = "Ctrl + A";
            this._editSelectAllMenuItem.Size = new System.Drawing.Size(211, 22);
            this._editSelectAllMenuItem.Text = "Select All";
            this._editSelectAllMenuItem.Click += new System.EventHandler(this.EditSelectAllMenuItem_Click);
            // 
            // m_editSelectNoneMenuItem
            // 
            this._editSelectNoneMenuItem.Name = "m_editSelectNoneMenuItem";
            this._editSelectNoneMenuItem.ShortcutKeyDisplayString = "Escape";
            this._editSelectNoneMenuItem.Size = new System.Drawing.Size(211, 22);
            this._editSelectNoneMenuItem.Text = "Select None";
            this._editSelectNoneMenuItem.Click += new System.EventHandler(this.EditSelectNoneMenuItem_Click);
            // 
            // selectSpecialToolStripMenuItem
            // 
            this._selectSpecialToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._selectAllRoomsToolStripMenuItem,
            this._selectedUnconnectedRoomsToolStripMenuItem,
            this._selectRoomsWObjectsToolStripMenuItem,
            this._selectRoomsWoObjectsToolStripMenuItem,
            this._toolStripSeparator15,
            this._selectAllConnectionsToolStripMenuItem,
            this._selectDanglingConnectionsToolStripMenuItem,
            this._selectSelfLoopingConnectionsToolStripMenuItem});
            this._selectSpecialToolStripMenuItem.Name = "selectSpecialToolStripMenuItem";
            this._selectSpecialToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this._selectSpecialToolStripMenuItem.Text = "Select Special";
            // 
            // selectAllRoomsToolStripMenuItem
            // 
            this._selectAllRoomsToolStripMenuItem.Name = "selectAllRoomsToolStripMenuItem";
            this._selectAllRoomsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this._selectAllRoomsToolStripMenuItem.Text = "Select All Rooms";
            this._selectAllRoomsToolStripMenuItem.Click += new System.EventHandler(this.SelectAllRoomsToolStripMenuItemClick);
            // 
            // selectedUnconnectedRoomsToolStripMenuItem
            // 
            this._selectedUnconnectedRoomsToolStripMenuItem.Name = "selectedUnconnectedRoomsToolStripMenuItem";
            this._selectedUnconnectedRoomsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this._selectedUnconnectedRoomsToolStripMenuItem.Text = "Selected Unconnected Rooms";
            this._selectedUnconnectedRoomsToolStripMenuItem.Click += new System.EventHandler(this.SelectedUnconnectedRoomsToolStripMenuItemClick);
            // 
            // selectRoomsWObjectsToolStripMenuItem
            // 
            this._selectRoomsWObjectsToolStripMenuItem.Name = "selectRoomsWObjectsToolStripMenuItem";
            this._selectRoomsWObjectsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this._selectRoomsWObjectsToolStripMenuItem.Text = "Select Rooms w/ Objects";
            this._selectRoomsWObjectsToolStripMenuItem.Click += new System.EventHandler(this.SelectRoomsWObjectsToolStripMenuItemClick);
            // 
            // selectRoomsWoObjectsToolStripMenuItem
            // 
            this._selectRoomsWoObjectsToolStripMenuItem.Name = "selectRoomsWoObjectsToolStripMenuItem";
            this._selectRoomsWoObjectsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this._selectRoomsWoObjectsToolStripMenuItem.Text = "Select Rooms w/o Objects";
            this._selectRoomsWoObjectsToolStripMenuItem.Click += new System.EventHandler(this.SelectRoomsWoObjectsToolStripMenuItemClick);
            // 
            // toolStripSeparator15
            // 
            this._toolStripSeparator15.Name = "toolStripSeparator15";
            this._toolStripSeparator15.Size = new System.Drawing.Size(241, 6);
            // 
            // selectAllConnectionsToolStripMenuItem
            // 
            this._selectAllConnectionsToolStripMenuItem.Name = "selectAllConnectionsToolStripMenuItem";
            this._selectAllConnectionsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this._selectAllConnectionsToolStripMenuItem.Text = "Select All Connections";
            this._selectAllConnectionsToolStripMenuItem.Click += new System.EventHandler(this.SelectAllConnectionsToolStripMenuItemClick);
            // 
            // selectDanglingConnectionsToolStripMenuItem
            // 
            this._selectDanglingConnectionsToolStripMenuItem.Name = "selectDanglingConnectionsToolStripMenuItem";
            this._selectDanglingConnectionsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this._selectDanglingConnectionsToolStripMenuItem.Text = "Select Dangling Connections";
            this._selectDanglingConnectionsToolStripMenuItem.Click += new System.EventHandler(this.SelectDanglingConnectionsToolStripMenuItemClick);
            // 
            // selectSelfLoopingConnectionsToolStripMenuItem
            // 
            this._selectSelfLoopingConnectionsToolStripMenuItem.Name = "selectSelfLoopingConnectionsToolStripMenuItem";
            this._selectSelfLoopingConnectionsToolStripMenuItem.Size = new System.Drawing.Size(244, 22);
            this._selectSelfLoopingConnectionsToolStripMenuItem.Text = "Select Self Looping Connections";
            this._selectSelfLoopingConnectionsToolStripMenuItem.Click += new System.EventHandler(this.SelectSelfLoopingConnectionsToolStripMenuItemClick);
            // 
            // toolStripSeparator5
            // 
            this._toolStripSeparator5.Name = "toolStripSeparator5";
            this._toolStripSeparator5.Size = new System.Drawing.Size(208, 6);
            // 
            // m_editCopyMenuItem
            // 
            this._editCopyMenuItem.Name = "m_editCopyMenuItem";
            this._editCopyMenuItem.ShortcutKeyDisplayString = "Ctrl + C";
            this._editCopyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this._editCopyMenuItem.Size = new System.Drawing.Size(211, 22);
            this._editCopyMenuItem.Text = "Copy";
            this._editCopyMenuItem.Click += new System.EventHandler(this.EditCopyMenuItemClick);
            // 
            // m_editCopyColorToolMenuItem
            // 
            this._editCopyColorToolMenuItem.Name = "m_editCopyColorToolMenuItem";
            this._editCopyColorToolMenuItem.ShortcutKeyDisplayString = "Ctrl + Alt + C";
            this._editCopyColorToolMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt)
            | System.Windows.Forms.Keys.C)));
            this._editCopyColorToolMenuItem.Size = new System.Drawing.Size(211, 22);
            this._editCopyColorToolMenuItem.Text = "Copy Color";
            this._editCopyColorToolMenuItem.Click += new System.EventHandler(this.EditCopyColorToolMenuItemClick);
            // 
            // m_editPasteMenuItem
            // 
            this._editPasteMenuItem.Name = "m_editPasteMenuItem";
            this._editPasteMenuItem.ShortcutKeyDisplayString = "Ctrl + V";
            this._editPasteMenuItem.Size = new System.Drawing.Size(211, 22);
            this._editPasteMenuItem.Text = "Paste";
            this._editPasteMenuItem.Click += new System.EventHandler(this.EditPasteMenuItemClick);
            // 
            // toolStripMenuItem8
            // 
            this._toolStripMenuItem8.Name = "toolStripMenuItem8";
            this._toolStripMenuItem8.Size = new System.Drawing.Size(208, 6);
            // 
            // m_editDeleteMenuItem
            // 
            this._editDeleteMenuItem.Name = "m_editDeleteMenuItem";
            this._editDeleteMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this._editDeleteMenuItem.Size = new System.Drawing.Size(211, 22);
            this._editDeleteMenuItem.Text = "&Delete";
            this._editDeleteMenuItem.Click += new System.EventHandler(this.EditDeleteMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this._toolStripSeparator3.Name = "toolStripSeparator3";
            this._toolStripSeparator3.Size = new System.Drawing.Size(208, 6);
            // 
            // m_editPropertiesMenuItem
            // 
            this._editPropertiesMenuItem.Name = "m_editPropertiesMenuItem";
            this._editPropertiesMenuItem.ShortcutKeyDisplayString = "Enter";
            this._editPropertiesMenuItem.Size = new System.Drawing.Size(211, 22);
            this._editPropertiesMenuItem.Text = "P&roperties";
            this._editPropertiesMenuItem.Click += new System.EventHandler(this.EditPropertiesMenuItem_Click);
            // 
            // roomsToolStripMenuItem
            // 
            this._roomsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._editAddRoomMenuItem,
            this._editRenameMenuItem,
            this._editChangeRegionMenuItem,
            this._toolStripSeparator16,
            this._editIsDarkMenuItem,
            this._makeRoomDarkToolStripMenuItem,
            this._makeRoomLightToolStripMenuItem,
            this._toolStripSeparator17,
            this._startRoomToolStripMenuItem,
            this._endRoomToolStripMenuItem,
            this._toolStripSeparator2,
            this._roomShapeToolStripMenuItem,
            this._toolStripSeparator18,
            this._joinRoomsToolStripMenuItem,
            this._toolStripSeparator19,
            this._swapObjectsToolStripMenuItem,
            this._swapNamesToolStripMenuItem,
            this._swapFormatsFillsToolStripMenuItem,
            this._swapRegionsToolStripMenuItem});
            this._roomsToolStripMenuItem.Name = "roomsToolStripMenuItem";
            this._roomsToolStripMenuItem.Size = new System.Drawing.Size(56, 22);
            this._roomsToolStripMenuItem.Text = "&Rooms";
            // 
            // m_editAddRoomMenuItem
            // 
            this._editAddRoomMenuItem.Name = "m_editAddRoomMenuItem";
            this._editAddRoomMenuItem.ShortcutKeyDisplayString = "R";
            this._editAddRoomMenuItem.Size = new System.Drawing.Size(229, 22);
            this._editAddRoomMenuItem.Text = "Add &Room";
            this._editAddRoomMenuItem.Click += new System.EventHandler(this.EditAddRoomMenuItem_Click);
            // 
            // m_editRenameMenuItem
            // 
            this._editRenameMenuItem.Name = "m_editRenameMenuItem";
            this._editRenameMenuItem.ShortcutKeyDisplayString = "";
            this._editRenameMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this._editRenameMenuItem.Size = new System.Drawing.Size(229, 22);
            this._editRenameMenuItem.Text = "Re&name";
            this._editRenameMenuItem.Click += new System.EventHandler(this.EditRenameMenuItem_Click);
            // 
            // m_editChangeRegionMenuItem
            // 
            this._editChangeRegionMenuItem.Name = "m_editChangeRegionMenuItem";
            this._editChangeRegionMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F2)));
            this._editChangeRegionMenuItem.Size = new System.Drawing.Size(229, 22);
            this._editChangeRegionMenuItem.Text = "Change Region";
            this._editChangeRegionMenuItem.Click += new System.EventHandler(this.EditChangeRegionMenuItemClick);
            // 
            // toolStripSeparator16
            // 
            this._toolStripSeparator16.Name = "toolStripSeparator16";
            this._toolStripSeparator16.Size = new System.Drawing.Size(226, 6);
            // 
            // m_editIsDarkMenuItem
            // 
            this._editIsDarkMenuItem.Checked = true;
            this._editIsDarkMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this._editIsDarkMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._editIsDarkMenuItem.Name = "m_editIsDarkMenuItem";
            this._editIsDarkMenuItem.Padding = new System.Windows.Forms.Padding(0);
            this._editIsDarkMenuItem.ShortcutKeyDisplayString = "K";
            this._editIsDarkMenuItem.Size = new System.Drawing.Size(229, 20);
            this._editIsDarkMenuItem.Text = "Toggle Dar&kness";
            this._editIsDarkMenuItem.Click += new System.EventHandler(this.EditIsDarkMenuItem_Click);
            // 
            // makeRoomDarkToolStripMenuItem
            // 
            this._makeRoomDarkToolStripMenuItem.Name = "makeRoomDarkToolStripMenuItem";
            this._makeRoomDarkToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.K)));
            this._makeRoomDarkToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._makeRoomDarkToolStripMenuItem.Text = "Force Darkness";
            this._makeRoomDarkToolStripMenuItem.Click += new System.EventHandler(this.MakeRoomDarkToolStripMenuItemClick);
            // 
            // makeRoomLightToolStripMenuItem
            // 
            this._makeRoomLightToolStripMenuItem.Name = "makeRoomLightToolStripMenuItem";
            this._makeRoomLightToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.K)));
            this._makeRoomLightToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._makeRoomLightToolStripMenuItem.Text = "Force Lighted";
            this._makeRoomLightToolStripMenuItem.Click += new System.EventHandler(this.MakeRoomLightToolStripMenuItemClick);
            // 
            // toolStripSeparator17
            // 
            this._toolStripSeparator17.Name = "toolStripSeparator17";
            this._toolStripSeparator17.Size = new System.Drawing.Size(226, 6);
            // 
            // startRoomToolStripMenuItem
            // 
            this._startRoomToolStripMenuItem.Name = "startRoomToolStripMenuItem";
            this._startRoomToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F5)));
            this._startRoomToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._startRoomToolStripMenuItem.Text = "Start Room";
            this._startRoomToolStripMenuItem.Click += new System.EventHandler(this.StartRoomToolStripMenuItemClick);
            // 
            // endRoomToolStripMenuItem
            // 
            this._endRoomToolStripMenuItem.Name = "endRoomToolStripMenuItem";
            this._endRoomToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F5)));
            this._endRoomToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._endRoomToolStripMenuItem.Text = "End Room";
            this._endRoomToolStripMenuItem.Click += new System.EventHandler(this.EndRoomToolStripMenuItemClick);
            // 
            // toolStripSeparator2
            // 
            this._toolStripSeparator2.Name = "toolStripSeparator2";
            this._toolStripSeparator2.Size = new System.Drawing.Size(226, 6);
            // 
            // roomShapeToolStripMenuItem
            // 
            this._roomShapeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._handDrawnToolStripMenuItem,
            this._ellipseToolStripMenuItem,
            this._roundedEdgesToolStripMenuItem,
            this._octagonalEdgesToolStripMenuItem});
            this._roomShapeToolStripMenuItem.Name = "roomShapeToolStripMenuItem";
            this._roomShapeToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._roomShapeToolStripMenuItem.Text = "Room &Shape";
            // 
            // handDrawnToolStripMenuItem
            // 
            this._handDrawnToolStripMenuItem.Name = "handDrawnToolStripMenuItem";
            this._handDrawnToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
            this._handDrawnToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._handDrawnToolStripMenuItem.Text = "Square Corners";
            this._handDrawnToolStripMenuItem.Click += new System.EventHandler(this.HandDrawnToolStripMenuItemClick);
            // 
            // ellipseToolStripMenuItem
            // 
            this._ellipseToolStripMenuItem.Name = "ellipseToolStripMenuItem";
            this._ellipseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this._ellipseToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._ellipseToolStripMenuItem.Text = "Ellipse";
            this._ellipseToolStripMenuItem.Click += new System.EventHandler(this.EllipseToolStripMenuItemClick);
            // 
            // roundedEdgesToolStripMenuItem
            // 
            this._roundedEdgesToolStripMenuItem.Name = "roundedEdgesToolStripMenuItem";
            this._roundedEdgesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this._roundedEdgesToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._roundedEdgesToolStripMenuItem.Text = "Rounded Edges";
            this._roundedEdgesToolStripMenuItem.Click += new System.EventHandler(this.RoundedEdgesToolStripMenuItemClick);
            // 
            // octagonalEdgesToolStripMenuItem
            // 
            this._octagonalEdgesToolStripMenuItem.Name = "octagonalEdgesToolStripMenuItem";
            this._octagonalEdgesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D8)));
            this._octagonalEdgesToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this._octagonalEdgesToolStripMenuItem.Text = "Octagonal Edges";
            this._octagonalEdgesToolStripMenuItem.Click += new System.EventHandler(this.OctagonalEdgesToolStripMenuItemClick);
            // 
            // toolStripSeparator18
            // 
            this._toolStripSeparator18.Name = "toolStripSeparator18";
            this._toolStripSeparator18.Size = new System.Drawing.Size(226, 6);
            // 
            // joinRoomsToolStripMenuItem
            // 
            this._joinRoomsToolStripMenuItem.Name = "joinRoomsToolStripMenuItem";
            this._joinRoomsToolStripMenuItem.ShortcutKeyDisplayString = "J";
            this._joinRoomsToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._joinRoomsToolStripMenuItem.Text = "&Join Rooms";
            this._joinRoomsToolStripMenuItem.Click += new System.EventHandler(this.JoinRoomsToolStripMenuItemClick);
            // 
            // toolStripSeparator19
            // 
            this._toolStripSeparator19.Name = "toolStripSeparator19";
            this._toolStripSeparator19.Size = new System.Drawing.Size(226, 6);
            // 
            // swapObjectsToolStripMenuItem
            // 
            this._swapObjectsToolStripMenuItem.Name = "swapObjectsToolStripMenuItem";
            this._swapObjectsToolStripMenuItem.ShortcutKeyDisplayString = "W";
            this._swapObjectsToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._swapObjectsToolStripMenuItem.Text = "S&wap Objects";
            this._swapObjectsToolStripMenuItem.Click += new System.EventHandler(this.SwapObjectsToolStripMenuItemClick);
            // 
            // swapNamesToolStripMenuItem
            // 
            this._swapNamesToolStripMenuItem.Name = "swapNamesToolStripMenuItem";
            this._swapNamesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this._swapNamesToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._swapNamesToolStripMenuItem.Text = "Swap Names";
            this._swapNamesToolStripMenuItem.Click += new System.EventHandler(this.SwapNamesToolStripMenuItemClick);
            // 
            // swapFormatsFillsToolStripMenuItem
            // 
            this._swapFormatsFillsToolStripMenuItem.Name = "swapFormatsFillsToolStripMenuItem";
            this._swapFormatsFillsToolStripMenuItem.ShortcutKeyDisplayString = "Shift+W";
            this._swapFormatsFillsToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._swapFormatsFillsToolStripMenuItem.Text = "Swap Formats / Fills";
            this._swapFormatsFillsToolStripMenuItem.Click += new System.EventHandler(this.SwapFormatsFillsToolStripMenuItemClick);
            // 
            // swapRegionsToolStripMenuItem
            // 
            this._swapRegionsToolStripMenuItem.Name = "swapRegionsToolStripMenuItem";
            this._swapRegionsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.W)));
            this._swapRegionsToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._swapRegionsToolStripMenuItem.Text = "Swap Regions";
            this._swapRegionsToolStripMenuItem.Click += new System.EventHandler(this.SwapRegionsToolStripMenuItemClick);
            // 
            // connectionsToolStripMenuItem
            // 
            this._connectionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._lineStylesMenuItem,
            this._reverseLineMenuItem});
            this._connectionsToolStripMenuItem.Name = "connectionsToolStripMenuItem";
            this._connectionsToolStripMenuItem.Size = new System.Drawing.Size(86, 22);
            this._connectionsToolStripMenuItem.Text = "&Connections";
            // 
            // m_lineStylesMenuItem
            // 
            this._lineStylesMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._plainLinesMenuItem,
            this._toolStripMenuItem3,
            this._toggleDottedLinesMenuItem,
            this._toggleDirectionalLinesMenuItem,
            this._toolStripMenuItem1,
            this._upLinesMenuItem,
            this._downLinesMenuItem,
            this._toolStripMenuItem2,
            this._inLinesMenuItem,
            this._outLinesMenuItem});
            this._lineStylesMenuItem.Name = "m_lineStylesMenuItem";
            this._lineStylesMenuItem.ShortcutKeyDisplayString = "";
            this._lineStylesMenuItem.Size = new System.Drawing.Size(153, 22);
            this._lineStylesMenuItem.Text = "&Line Styles";
            // 
            // m_plainLinesMenuItem
            // 
            this._plainLinesMenuItem.Name = "m_plainLinesMenuItem";
            this._plainLinesMenuItem.ShortcutKeyDisplayString = "P";
            this._plainLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._plainLinesMenuItem.Text = "&Plain";
            this._plainLinesMenuItem.Click += new System.EventHandler(this.PlainLinesMenuItem_Click);
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
            this._toggleDottedLinesMenuItem.Text = "Do&tted";
            this._toggleDottedLinesMenuItem.Click += new System.EventHandler(this.ToggleDottedLines_Click);
            // 
            // m_toggleDirectionalLinesMenuItem
            // 
            this._toggleDirectionalLinesMenuItem.Name = "m_toggleDirectionalLinesMenuItem";
            this._toggleDirectionalLinesMenuItem.ShortcutKeyDisplayString = "A";
            this._toggleDirectionalLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._toggleDirectionalLinesMenuItem.Text = "One Way &Arrow";
            this._toggleDirectionalLinesMenuItem.Click += new System.EventHandler(this.ToggleDirectionalLines_Click);
            // 
            // toolStripMenuItem1
            // 
            this._toolStripMenuItem1.Name = "toolStripMenuItem1";
            this._toolStripMenuItem1.Size = new System.Drawing.Size(169, 6);
            // 
            // m_upLinesMenuItem
            // 
            this._upLinesMenuItem.Name = "m_upLinesMenuItem";
            this._upLinesMenuItem.ShortcutKeyDisplayString = "U";
            this._upLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._upLinesMenuItem.Text = "&Up";
            this._upLinesMenuItem.Click += new System.EventHandler(this.UpLinesMenuItem_Click);
            // 
            // m_downLinesMenuItem
            // 
            this._downLinesMenuItem.Name = "m_downLinesMenuItem";
            this._downLinesMenuItem.ShortcutKeyDisplayString = "D";
            this._downLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._downLinesMenuItem.Text = "&Down";
            this._downLinesMenuItem.Click += new System.EventHandler(this.DownLinesMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this._toolStripMenuItem2.Name = "toolStripMenuItem2";
            this._toolStripMenuItem2.Size = new System.Drawing.Size(169, 6);
            // 
            // m_inLinesMenuItem
            // 
            this._inLinesMenuItem.Name = "m_inLinesMenuItem";
            this._inLinesMenuItem.ShortcutKeyDisplayString = "I";
            this._inLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._inLinesMenuItem.Text = "&In";
            this._inLinesMenuItem.Click += new System.EventHandler(this.InLinesMenuItem_Click);
            // 
            // m_outLinesMenuItem
            // 
            this._outLinesMenuItem.Name = "m_outLinesMenuItem";
            this._outLinesMenuItem.ShortcutKeyDisplayString = "O";
            this._outLinesMenuItem.Size = new System.Drawing.Size(172, 22);
            this._outLinesMenuItem.Text = "&Out";
            this._outLinesMenuItem.Click += new System.EventHandler(this.OutLinesMenuItem_Click);
            // 
            // m_reverseLineMenuItem
            // 
            this._reverseLineMenuItem.Name = "m_reverseLineMenuItem";
            this._reverseLineMenuItem.ShortcutKeyDisplayString = "V";
            this._reverseLineMenuItem.Size = new System.Drawing.Size(153, 22);
            this._reverseLineMenuItem.Text = "Re&verse Line";
            this._reverseLineMenuItem.Click += new System.EventHandler(this.ReverseLineMenuItem_Click);
            // 
            // validationToolStripMenuItem
            // 
            this._validationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._roomsMustHaveUniqueNamesToolStripMenuItem,
            this._roomsMustHaveADescriptionToolStripMenuItem,
            this._roomsMustHaveASubtitleToolStripMenuItem,
            this._roomsMustNotHaveADanglingConnectionToolStripMenuItem});
            this._validationToolStripMenuItem.Name = "validationToolStripMenuItem";
            this._validationToolStripMenuItem.Size = new System.Drawing.Size(71, 22);
            this._validationToolStripMenuItem.Text = "Validation";
            // 
            // roomsMustHaveUniqueNamesToolStripMenuItem
            // 
            this._roomsMustHaveUniqueNamesToolStripMenuItem.Name = "roomsMustHaveUniqueNamesToolStripMenuItem";
            this._roomsMustHaveUniqueNamesToolStripMenuItem.Size = new System.Drawing.Size(319, 22);
            this._roomsMustHaveUniqueNamesToolStripMenuItem.Text = "Rooms Must Have a Unique Name";
            this._roomsMustHaveUniqueNamesToolStripMenuItem.Click += new System.EventHandler(this.RoomsMustHaveUniqueNamesToolStripMenuItemClick);
            // 
            // roomsMustHaveADescriptionToolStripMenuItem
            // 
            this._roomsMustHaveADescriptionToolStripMenuItem.Name = "roomsMustHaveADescriptionToolStripMenuItem";
            this._roomsMustHaveADescriptionToolStripMenuItem.Size = new System.Drawing.Size(319, 22);
            this._roomsMustHaveADescriptionToolStripMenuItem.Text = "Rooms Must Have a Description";
            this._roomsMustHaveADescriptionToolStripMenuItem.Click += new System.EventHandler(this.RoomsMustHaveADescriptionToolStripMenuItemClick);
            // 
            // roomsMustHaveASubtitleToolStripMenuItem
            // 
            this._roomsMustHaveASubtitleToolStripMenuItem.Name = "roomsMustHaveASubtitleToolStripMenuItem";
            this._roomsMustHaveASubtitleToolStripMenuItem.Size = new System.Drawing.Size(319, 22);
            this._roomsMustHaveASubtitleToolStripMenuItem.Text = "Rooms Must Have a Subtitle";
            this._roomsMustHaveASubtitleToolStripMenuItem.Click += new System.EventHandler(this.RoomsMustHaveASubtitleToolStripMenuItemClick);
            // 
            // roomsMustNotHaveADanglingConnectionToolStripMenuItem
            // 
            this._roomsMustNotHaveADanglingConnectionToolStripMenuItem.Name = "roomsMustNotHaveADanglingConnectionToolStripMenuItem";
            this._roomsMustNotHaveADanglingConnectionToolStripMenuItem.Size = new System.Drawing.Size(319, 22);
            this._roomsMustNotHaveADanglingConnectionToolStripMenuItem.Text = "Rooms Must Not Have a Dangling Connection";
            this._roomsMustNotHaveADanglingConnectionToolStripMenuItem.Click += new System.EventHandler(this.RoomsMustNotHaveADanglingConnectionToolStripMenuItemClick);
            // 
            // m_viewMenu
            // 
            this._viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._viewZoomMenu,
            this._viewEntireMapMenuItem,
            this._viewResetMenuItem,
            this._toolStripMenuItem6,
            this._viewMinimapMenuItem,
            this._toolStripSeparator21,
            this._viewShowGridMenuItem});
            this._viewMenu.Name = "m_viewMenu";
            this._viewMenu.Size = new System.Drawing.Size(44, 22);
            this._viewMenu.Text = "&View";
            // 
            // m_viewZoomMenu
            // 
            this._viewZoomMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._viewZoomInMenuItem,
            this._viewZoomOutMenuItem,
            this._toolStripSeparator7,
            this._viewZoomFiftyPercentMenuItem,
            this._viewZoomOneHundredPercentMenuItem,
            this._viewZoomTwoHundredPercentMenuItem,
            this._toolStripSeparator20,
            this._viewZoomMiniIn,
            this._viewZoomMiniOut});
            this._viewZoomMenu.Name = "m_viewZoomMenu";
            this._viewZoomMenu.Size = new System.Drawing.Size(204, 22);
            this._viewZoomMenu.Text = "&Zoom";
            // 
            // m_viewZoomInMenuItem
            // 
            this._viewZoomInMenuItem.Name = "m_viewZoomInMenuItem";
            this._viewZoomInMenuItem.ShortcutKeyDisplayString = "+ / Mouse Wheel";
            this._viewZoomInMenuItem.Size = new System.Drawing.Size(205, 22);
            this._viewZoomInMenuItem.Text = "&In";
            this._viewZoomInMenuItem.Click += new System.EventHandler(this.ViewZoomInMenuItem_Click);
            // 
            // m_viewZoomOutMenuItem
            // 
            this._viewZoomOutMenuItem.Name = "m_viewZoomOutMenuItem";
            this._viewZoomOutMenuItem.ShortcutKeyDisplayString = "- / Mouse Wheel";
            this._viewZoomOutMenuItem.Size = new System.Drawing.Size(205, 22);
            this._viewZoomOutMenuItem.Text = "&Out";
            this._viewZoomOutMenuItem.Click += new System.EventHandler(this.ViewZoomOutMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            this._toolStripSeparator7.Name = "toolStripSeparator7";
            this._toolStripSeparator7.Size = new System.Drawing.Size(202, 6);
            // 
            // m_viewZoomFiftyPercentMenuItem
            // 
            this._viewZoomFiftyPercentMenuItem.Name = "m_viewZoomFiftyPercentMenuItem";
            this._viewZoomFiftyPercentMenuItem.Size = new System.Drawing.Size(205, 22);
            this._viewZoomFiftyPercentMenuItem.Text = "&50%";
            this._viewZoomFiftyPercentMenuItem.Click += new System.EventHandler(this.ViewZoomFiftyPercentMenuItem_Click);
            // 
            // m_viewZoomOneHundredPercentMenuItem
            // 
            this._viewZoomOneHundredPercentMenuItem.Name = "m_viewZoomOneHundredPercentMenuItem";
            this._viewZoomOneHundredPercentMenuItem.Size = new System.Drawing.Size(205, 22);
            this._viewZoomOneHundredPercentMenuItem.Text = "&100%";
            this._viewZoomOneHundredPercentMenuItem.Click += new System.EventHandler(this.ViewZoomOneHundredPercentMenuItem_Click);
            // 
            // m_viewZoomTwoHundredPercentMenuItem
            // 
            this._viewZoomTwoHundredPercentMenuItem.Name = "m_viewZoomTwoHundredPercentMenuItem";
            this._viewZoomTwoHundredPercentMenuItem.Size = new System.Drawing.Size(205, 22);
            this._viewZoomTwoHundredPercentMenuItem.Text = "&200%";
            this._viewZoomTwoHundredPercentMenuItem.Click += new System.EventHandler(this.ViewZoomTwoHundredPercentMenuItem_Click);
            // 
            // toolStripSeparator20
            // 
            this._toolStripSeparator20.Name = "toolStripSeparator20";
            this._toolStripSeparator20.Size = new System.Drawing.Size(202, 6);
            // 
            // m_viewZoomMiniIn
            // 
            this._viewZoomMiniIn.Name = "m_viewZoomMiniIn";
            this._viewZoomMiniIn.ShortcutKeyDisplayString = "ctrl / Mouse Wheel";
            this._viewZoomMiniIn.Size = new System.Drawing.Size(205, 22);
            this._viewZoomMiniIn.Text = "&-1%";
            this._viewZoomMiniIn.Click += new System.EventHandler(this.ViewZoomMiniIn_Click);
            // 
            // m_viewZoomMiniOut
            // 
            this._viewZoomMiniOut.Name = "m_viewZoomMiniOut";
            this._viewZoomMiniOut.ShortcutKeyDisplayString = "ctrl / Mouse Wheel";
            this._viewZoomMiniOut.Size = new System.Drawing.Size(205, 22);
            this._viewZoomMiniOut.Text = "&+1%";
            this._viewZoomMiniOut.Click += new System.EventHandler(this.ViewZoomMiniOut_Click);
            // 
            // m_viewEntireMapMenuItem
            // 
            this._viewEntireMapMenuItem.Name = "m_viewEntireMapMenuItem";
            this._viewEntireMapMenuItem.ShortcutKeyDisplayString = "Ctrl + Home";
            this._viewEntireMapMenuItem.Size = new System.Drawing.Size(204, 22);
            this._viewEntireMapMenuItem.Text = "&Entire Map";
            this._viewEntireMapMenuItem.Click += new System.EventHandler(this.ViewEntireMapMenuItem_Click);
            // 
            // m_viewResetMenuItem
            // 
            this._viewResetMenuItem.Name = "m_viewResetMenuItem";
            this._viewResetMenuItem.ShortcutKeyDisplayString = "Home";
            this._viewResetMenuItem.Size = new System.Drawing.Size(204, 22);
            this._viewResetMenuItem.Text = "&Reset";
            this._viewResetMenuItem.Click += new System.EventHandler(this.ViewResetMenuItem_Click);
            // 
            // toolStripMenuItem6
            // 
            this._toolStripMenuItem6.Name = "toolStripMenuItem6";
            this._toolStripMenuItem6.Size = new System.Drawing.Size(201, 6);
            // 
            // m_viewMinimapMenuItem
            // 
            this._viewMinimapMenuItem.Name = "m_viewMinimapMenuItem";
            this._viewMinimapMenuItem.Size = new System.Drawing.Size(204, 22);
            this._viewMinimapMenuItem.Text = "&Mini Map";
            this._viewMinimapMenuItem.Click += new System.EventHandler(this.ViewMinimapMenuItem_Click);
            // 
            // toolStripSeparator21
            // 
            this._toolStripSeparator21.Name = "toolStripSeparator21";
            this._toolStripSeparator21.Size = new System.Drawing.Size(201, 6);
            // 
            // m_viewShowGridMenuItem
            // 
            this._viewShowGridMenuItem.Name = "m_viewShowGridMenuItem";
            this._viewShowGridMenuItem.Size = new System.Drawing.Size(204, 22);
            this._viewShowGridMenuItem.Text = "Show &Grid";
            this._viewShowGridMenuItem.Click += new System.EventHandler(this.ViewShowGridMenuItem_Click);
            // 
            // automappingToolStripMenuItem
            // 
            this._automappingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._automapStartMenuItem,
            this._automapStopMenuItem});
            this._automappingToolStripMenuItem.Name = "automappingToolStripMenuItem";
            this._automappingToolStripMenuItem.Size = new System.Drawing.Size(69, 22);
            this._automappingToolStripMenuItem.Text = "&Automap";
            // 
            // m_automapStartMenuItem
            // 
            this._automapStartMenuItem.Name = "m_automapStartMenuItem";
            this._automapStartMenuItem.Size = new System.Drawing.Size(107, 22);
            this._automapStartMenuItem.Text = "&Start...";
            this._automapStartMenuItem.Click += new System.EventHandler(this.AutomapStartMenuItem_Click);
            // 
            // m_automapStopMenuItem
            // 
            this._automapStopMenuItem.Name = "m_automapStopMenuItem";
            this._automapStopMenuItem.Size = new System.Drawing.Size(107, 22);
            this._automapStopMenuItem.Text = "S&top";
            this._automapStopMenuItem.Click += new System.EventHandler(this.AutomapStopMenuItem_Click);
            // 
            // m_projectMenu
            // 
            this._projectMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._projectSettingsMenuItem,
            this._appSettingsToolStripMenuItem,
            this._toolStripSeparator11,
            this._projectResetToDefaultSettingsMenuItem,
            this._toolStripSeparator14,
            this._mapStatisticsToolStripMenuItem,
            this._mapStatisticsExportToolStripMenuItem});
            this._projectMenu.Name = "m_projectMenu";
            this._projectMenu.Size = new System.Drawing.Size(46, 22);
            this._projectMenu.Text = "&Tools";
            // 
            // m_projectSettingsMenuItem
            // 
            this._projectSettingsMenuItem.Name = "m_projectSettingsMenuItem";
            this._projectSettingsMenuItem.ShortcutKeyDisplayString = "Ctrl+Comma";
            this._projectSettingsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemcomma)));
            this._projectSettingsMenuItem.Size = new System.Drawing.Size(229, 22);
            this._projectSettingsMenuItem.Text = "Map &Settings...";
            this._projectSettingsMenuItem.Click += new System.EventHandler(this.ProjectSettingsMenuItem_Click);
            // 
            // appSettingsToolStripMenuItem
            // 
            this._appSettingsToolStripMenuItem.Name = "appSettingsToolStripMenuItem";
            this._appSettingsToolStripMenuItem.ShortcutKeyDisplayString = "";
            this._appSettingsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.S)));
            this._appSettingsToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._appSettingsToolStripMenuItem.Text = "&App Settings...";
            this._appSettingsToolStripMenuItem.Click += new System.EventHandler(this.AppSettingsToolStripMenuItemClick);
            // 
            // toolStripSeparator11
            // 
            this._toolStripSeparator11.Name = "toolStripSeparator11";
            this._toolStripSeparator11.Size = new System.Drawing.Size(226, 6);
            // 
            // m_projectResetToDefaultSettingsMenuItem
            // 
            this._projectResetToDefaultSettingsMenuItem.Name = "m_projectResetToDefaultSettingsMenuItem";
            this._projectResetToDefaultSettingsMenuItem.Size = new System.Drawing.Size(229, 22);
            this._projectResetToDefaultSettingsMenuItem.Text = "&Restore Default Map Settings";
            this._projectResetToDefaultSettingsMenuItem.Click += new System.EventHandler(this.ProjectResetToDefaultSettingsMenuItem_Click);
            // 
            // toolStripSeparator14
            // 
            this._toolStripSeparator14.Name = "toolStripSeparator14";
            this._toolStripSeparator14.Size = new System.Drawing.Size(226, 6);
            // 
            // mapStatisticsToolStripMenuItem
            // 
            this._mapStatisticsToolStripMenuItem.Name = "mapStatisticsToolStripMenuItem";
            this._mapStatisticsToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._mapStatisticsToolStripMenuItem.Text = "Map Statistics...";
            this._mapStatisticsToolStripMenuItem.Click += new System.EventHandler(this.MapStatisticsToolStripMenuItemClick);
            // 
            // mapStatisticsExportToolStripMenuItem
            // 
            this._mapStatisticsExportToolStripMenuItem.Name = "mapStatisticsExportToolStripMenuItem";
            this._mapStatisticsExportToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this._mapStatisticsExportToolStripMenuItem.Text = "Map Statistic E&xport...";
            this._mapStatisticsExportToolStripMenuItem.Click += new System.EventHandler(this.MapStatisticsExportToolStripMenuItemClick);
            // 
            // m_helpMenu
            // 
            this._helpMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._onlineHelpMenuItem,
            this._toolStripMenuItem4,
            this._helpAboutMenuItem});
            this._helpMenu.Name = "m_helpMenu";
            this._helpMenu.Size = new System.Drawing.Size(44, 22);
            this._helpMenu.Text = "&Help";
            // 
            // m_onlineHelpMenuItem
            // 
            this._onlineHelpMenuItem.Name = "m_onlineHelpMenuItem";
            this._onlineHelpMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this._onlineHelpMenuItem.Size = new System.Drawing.Size(171, 22);
            this._onlineHelpMenuItem.Text = "Online Help";
            this._onlineHelpMenuItem.Click += new System.EventHandler(this.HelpAndSupportMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this._toolStripMenuItem4.Name = "toolStripMenuItem4";
            this._toolStripMenuItem4.Size = new System.Drawing.Size(168, 6);
            // 
            // m_helpAboutMenuItem
            // 
            this._helpAboutMenuItem.Name = "m_helpAboutMenuItem";
            this._helpAboutMenuItem.Size = new System.Drawing.Size(171, 22);
            this._helpAboutMenuItem.Text = "&About";
            this._helpAboutMenuItem.Click += new System.EventHandler(this.HelpAboutMenuItem_Click);
            // 
            // m_toolStrip
            // 
            this._toolStrip.AutoSize = false;
            this._toolStrip.Dock = System.Windows.Forms.DockStyle.Left;
            this._toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._toolStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            this._toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toggleDottedLinesButton,
            this._toggleDirectionalLinesButton});
            this._toolStrip.Location = new System.Drawing.Point(0, 24);
            this._toolStrip.Name = "m_toolStrip";
            this._toolStrip.Padding = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this._toolStrip.Size = new System.Drawing.Size(38, 751);
            this._toolStrip.TabIndex = 2;
            // 
            // m_toggleDottedLinesButton
            // 
            this._toggleDottedLinesButton.AutoSize = false;
            this._toggleDottedLinesButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_toggleDottedLinesButton.BackgroundImage")));
            this._toggleDottedLinesButton.CheckOnClick = true;
            this._toggleDottedLinesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._toggleDottedLinesButton.Image = global::Trizbort.Properties.Resources.LineStyle;
            this._toggleDottedLinesButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toggleDottedLinesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toggleDottedLinesButton.Name = "m_toggleDottedLinesButton";
            this._toggleDottedLinesButton.Size = new System.Drawing.Size(32, 32);
            this._toggleDottedLinesButton.Text = "Toggle Dotted Lines (T)";
            this._toggleDottedLinesButton.Click += new System.EventHandler(this.ToggleDottedLines_Click);
            // 
            // m_toggleDirectionalLinesButton
            // 
            this._toggleDirectionalLinesButton.AutoSize = false;
            this._toggleDirectionalLinesButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_toggleDirectionalLinesButton.BackgroundImage")));
            this._toggleDirectionalLinesButton.CheckOnClick = true;
            this._toggleDirectionalLinesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._toggleDirectionalLinesButton.Image = global::Trizbort.Properties.Resources.LineDirection;
            this._toggleDirectionalLinesButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toggleDirectionalLinesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toggleDirectionalLinesButton.Name = "m_toggleDirectionalLinesButton";
            this._toggleDirectionalLinesButton.Size = new System.Drawing.Size(32, 32);
            this._toggleDirectionalLinesButton.Text = "Toggle One Way Arrow Lines (A)";
            this._toggleDirectionalLinesButton.Click += new System.EventHandler(this.ToggleDirectionalLines_Click);
            // 
            // toolStripSeparator4
            // 
            this._toolStripSeparator4.Name = "toolStripSeparator4";
            this._toolStripSeparator4.Size = new System.Drawing.Size(6, 6);
            // 
            // statusBar
            // 
            this._statusBar.ImageScalingSize = new System.Drawing.Size(32, 32);
            this._statusBar.Location = new System.Drawing.Point(0, 775);
            this._statusBar.Name = "statusBar";
            this._statusBar.Size = new System.Drawing.Size(962, 22);
            this._statusBar.TabIndex = 5;
            this._statusBar.Text = "statusStrip1";
            // 
            // Canvas
            // 
            this.Canvas.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Canvas.BackColor = System.Drawing.Color.White;
            this.Canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Canvas.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Canvas.Location = new System.Drawing.Point(38, 24);
            this.Canvas.Margin = new System.Windows.Forms.Padding(6);
            this.Canvas.MinimapVisible = false;
            this.Canvas.Name = "Canvas";
            this.Canvas.Size = new System.Drawing.Size(924, 724);
            this.Canvas.TabIndex = 0;
            // 
            // m_automapBar
            // 
            this._automapBar.BackColor = System.Drawing.SystemColors.Info;
            this._automapBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._automapBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._automapBar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._automapBar.ForeColor = System.Drawing.SystemColors.InfoText;
            this._automapBar.Location = new System.Drawing.Point(38, 748);
            this._automapBar.Margin = new System.Windows.Forms.Padding(6);
            this._automapBar.MaximumSize = new System.Drawing.Size(4096, 27);
            this._automapBar.MinimumSize = new System.Drawing.Size(2, 27);
            this._automapBar.Name = "m_automapBar";
            this._automapBar.Size = new System.Drawing.Size(924, 27);
            this._automapBar.TabIndex = 4;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(962, 797);
            this.Controls.Add(this.Canvas);
            this.Controls.Add(this._automapBar);
            this.Controls.Add(this._toolStrip);
            this.Controls.Add(this._menuStrip);
            this.Controls.Add(this._statusBar);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this._menuStrip;
            this.Name = "MainForm";
            this.Text = "Trizbort - Interactive Fiction Mapper";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this._menuStrip.ResumeLayout(false);
            this._menuStrip.PerformLayout();
            this._toolStrip.ResumeLayout(false);
            this._toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip _menuStrip;
        private System.Windows.Forms.ToolStripMenuItem _fileMenu;
        private System.Windows.Forms.ToolStripMenuItem _fileExitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _projectMenu;
        private System.Windows.Forms.ToolStripMenuItem _projectSettingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileNewMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem _editMenu;
        private System.Windows.Forms.ToolStripMenuItem _editDeleteMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem _editPropertiesMenuItem;
        private System.Windows.Forms.ToolStrip _toolStrip;
        private System.Windows.Forms.ToolStripButton _toggleDottedLinesButton;
        private System.Windows.Forms.ToolStripButton _toggleDirectionalLinesButton;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator4;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem _lineStylesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _viewMenu;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomMenu;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomInMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomOutMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomFiftyPercentMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomOneHundredPercentMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomTwoHundredPercentMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator20;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomMiniOut;
        private System.Windows.Forms.ToolStripMenuItem _viewZoomMiniIn;
        private System.Windows.Forms.ToolStripMenuItem _viewResetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editRenameMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem _fileOpenMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem _fileSaveMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileSaveAsMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator10;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator11;
        private System.Windows.Forms.ToolStripMenuItem _projectResetToDefaultSettingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportMenu;
        private System.Windows.Forms.ToolStripMenuItem _fileExportPDFMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportImageMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator12;
        private System.Windows.Forms.ToolStripMenuItem _helpMenu;
        private System.Windows.Forms.ToolStripMenuItem _helpAboutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _plainLinesMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem _toggleDottedLinesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _toggleDirectionalLinesMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem _upLinesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _downLinesMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem _inLinesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _outLinesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _reverseLineMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _onlineHelpMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem _automappingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _automapStartMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _automapStopMenuItem;
        private Trizbort.UI.Controls.AutomapBar _automapBar;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem _viewMinimapMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportAlanMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportAdventuronMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportHugoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportInform7MenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportQuestMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileRecentMapsMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem _editSelectAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editSelectNoneMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem _viewEntireMapMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem9;
        private System.Windows.Forms.ToolStripMenuItem _fileExportTADSMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExportInform6MenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem _editCopyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editPasteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editCopyColorToolMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _appSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _smartSaveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editChangeRegionMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator13;
        private System.Windows.Forms.ToolStripMenuItem _alanToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _adventuronToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _hugoToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _inform7ToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _inform6ToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _tADSToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _questToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _questRoomsToTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator14;
        private System.Windows.Forms.ToolStripMenuItem _mapStatisticsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _mapStatisticsExportToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _selectSpecialToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _selectAllRoomsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _selectAllConnectionsToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator15;
    private System.Windows.Forms.ToolStripMenuItem _selectedUnconnectedRoomsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _selectDanglingConnectionsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _selectSelfLoopingConnectionsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _selectRoomsWObjectsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _selectRoomsWoObjectsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _toggleTextToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _zILToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _zILToClipboardToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roomsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _connectionsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _editAddRoomMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator16;
    private System.Windows.Forms.ToolStripMenuItem _editIsDarkMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator2;
    private System.Windows.Forms.ToolStripMenuItem _roomShapeToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _handDrawnToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _ellipseToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roundedEdgesToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _octagonalEdgesToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator18;
    private System.Windows.Forms.ToolStripMenuItem _joinRoomsToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator19;
    private System.Windows.Forms.ToolStripMenuItem _swapObjectsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _swapNamesToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _swapFormatsFillsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _swapRegionsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _makeRoomDarkToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _makeRoomLightToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _validationToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roomsMustHaveUniqueNamesToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roomsMustHaveADescriptionToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roomsMustHaveASubtitleToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _roomsMustNotHaveADanglingConnectionToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem _startRoomToolStripMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator17;
    private System.Windows.Forms.ToolStripMenuItem _endRoomToolStripMenuItem;
    private System.Windows.Forms.StatusStrip _statusBar;
    private System.Windows.Forms.ToolStripMenuItem _fileOpenFromWebMenuItem;
    private System.Windows.Forms.ToolStripSeparator _toolStripSeparator21;
    private System.Windows.Forms.ToolStripMenuItem _viewShowGridMenuItem;
  }
}
