using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using PdfSharp.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export;
using Trizbort.Properties;
using Trizbort.UI.Controls;
using Trizbort.Util;
using Settings = Trizbort.Setup.Settings;
using Trizbort.Domain.StatusBar;
using Trizbort.Export.Languages;
using Trizbort.Extensions;

namespace Trizbort.UI {
  public partial class MainForm : Form {
    private static readonly TimeSpan _idleProcessingEveryNSeconds = TimeSpan.FromSeconds(0.2);
    private readonly CommandController _commandController;
    private readonly string _caption;
    public Canvas Canvas;

    // TODO: private ToolStripStatusLabel statusLabel;
    private Status _trizStatusBar;

    private DateTime _lastUpdateUITime;
    private SynchronizationContext _synchronizationContext;

    public MainForm() {
      InitializeComponent();
      InitializeThemeMenu();
      _editMenu.DropDownItems.Add(new ToolStripMenuItem("Add &Label", null, (_, __) => Canvas.AddLabel(false)) {
        ShortcutKeyDisplayString = "L"
      });
      _synchronizationContext = SynchronizationContext.Current;
      TrizbortApplication.MainForm = this;

      _commandController = new CommandController(Canvas);

      _caption = Text;

      Application.Idle += OnIdle;
      _lastUpdateUITime = DateTime.MinValue;

      _automapBar.StopClick += OnMAutomapBarOnStopClick;
    }


    public sealed override string Text { get => base.Text; set => base.Text = value; }

    public void OpenProject() {
      if (!CheckLoseProject())
        return;

      using var dialog = new OpenFileDialog();
      var lastProjectName = ApplicationSettingsController.AppSettings.LastProjectFileName;
      if (!Uri.IsWellFormedUriString(lastProjectName, UriKind.RelativeOrAbsolute))
        dialog.InitialDirectory = PathHelper.SafeGetDirectoryName(lastProjectName);

      dialog.Filter = $"{Project.FilterString}|All Files|*.*||";
      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK) {
        OpenProject(dialog.FileName);
      }
    }

    protected override void OnClosing(CancelEventArgs e) {
      if (!CheckLoseProject()) {
        e.Cancel = true;
        return;
      }

      ApplicationSettingsController.AppSettings.CanvasHeight = Height - 27;
      ApplicationSettingsController.AppSettings.CanvasWidth = Width - 8;
      ApplicationSettingsController.SaveSettings();
      Canvas.StopAutomapping();

      Project.FileWatcher.Dispose();

      base.OnClosing(e);
    }

    private void AlanToTextToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<AlanExporter>();
    }

    private void AdventuronToTextToolStripMenuItemClick(object sender, EventArgs e)
    {
        ExportCode<AdventuronExporter>();
    }


        private void AppSettingsToolStripMenuItemClick(object sender, EventArgs e) {
      ApplicationSettingsController.ShowAppDialog();
    }

    private void AutomapStartMenuItem_Click(object sender, EventArgs e) {
      using var dialog = new AutomapDialog();
      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK) Canvas.StartAutomapping(dialog.Data);
    }

    private void AutomapStopMenuItem_Click(object sender, EventArgs e) {
      Canvas.StopAutomapping();
    }

    private bool CheckLoseProject() {
      if (Project.Current.IsDirty) {
        // see if the user would like to save
        var result = UserInteraction.ShowMessage(this, $"Do you want to save changes to {Project.Current.Name}?", Text, MessageBoxButtons.YesNoCancel);
        switch (result) {
          case DialogResult.Yes:
            // user would like to save
            if (!SaveProject()) return false;

            // user saved; carry on
            return true;

          case DialogResult.No:
            // user wouldn't like to save; carry on
            return true;

          default:
            // user cancelled; cancel
            return false;
        }
      }

      // project doesn't need saving; carry on
      return true;
    }


    private async Task<bool> ClAutoMap(CommandLineOptions options) {
      var projectLoaded = false;
      try {
        var cmdLineAutomap = ApplicationSettingsController.AppSettings.Automap;
        cmdLineAutomap.FileName = options.Transcript;

        await Canvas.StartAutomapping(cmdLineAutomap, true);
        Canvas.StopAutomapping();

        if (options.QuickSave != null) {
          SaveAsCmdLineProject(options.QuickSave);
          Project.Current.IsDirty = false;
        }

        Project.Current.IsDirty = false;
        projectLoaded = true;
      }
      catch (Exception) {
        // ignored
      }

      Project.Current.IsDirty = false;
      return projectLoaded;
    }


    private bool CommandLineActions(CommandLineOptions options) {
      var projectLoaded = false;

      if (options.LoadLastProject) {
        OpenProject(ApplicationSettingsController.AppSettings.LastProjectFileName);
        projectLoaded = true;
      }

      if (options.Transcript != null) projectLoaded = ClAutoMap(options).Result;

      if (options.QuickSave != null && options.Transcript == null) {
        SaveAsCmdLineProject(options.QuickSave);
        Project.Current.IsDirty = false;
      }


      if (options.FileName != null)
        if (!projectLoaded) {
          OpenProject(options.FileName);
          projectLoaded = true;

          if (options.SmartSave) SmartSave(true);
        }

      if (!string.IsNullOrWhiteSpace(options.I6)) ExportCodeCl<Inform6Exporter>(options.I6);

      if (!string.IsNullOrWhiteSpace(options.I7)) ExportCodeCl<Inform7Exporter>(options.I7);

      if (!string.IsNullOrWhiteSpace(options.Tads)) ExportCodeCl<TadsExporter>(options.Tads);


      if (!string.IsNullOrWhiteSpace(options.Alan)) ExportCodeCl<AlanExporter>(options.Alan);

      if (!string.IsNullOrWhiteSpace(options.Hugo)) ExportCodeCl<HugoExporter>(options.Hugo);

      if (!string.IsNullOrWhiteSpace(options.Zil)) ExportCodeCl<ZilExporter>(options.Zil);

      if (!string.IsNullOrWhiteSpace(options.Quest)) ExportCodeCl<QuestExporter>(options.Quest);

      if (!string.IsNullOrWhiteSpace(options.QuestRooms)) ExportCodeCl<QuestRoomsExporter>(options.QuestRooms);

      if (options.Exit) Close();

      return projectLoaded;
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, string lpszFile);

    [DllImport("gdi32.dll")]
    private static extern int DeleteEnhMetaFile(IntPtr hemf);

    private void DownLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Down);
    }

    private void EditAddRoomMenuItem_Click(object sender, EventArgs e) {
      Canvas.AddRoom(false);
    }

    private void EditDeleteMenuItem_Click(object sender, EventArgs e) {
      Canvas.DeleteSelection();
    }

    private void EditIsDarkMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetRoomLighting(LightingActionType.Toggle);
    }

    private void EditPropertiesMenuItem_Click(object sender, EventArgs e) {
      if (Canvas.HasSingleSelectedElement && Canvas.SelectedElement.HasDialog) _commandController.ShowElementProperties(Canvas.SelectedElement);
    }

    private void EditRenameMenuItem_Click(object sender, EventArgs e) {
      if (Canvas.HasSingleSelectedElement && Canvas.SelectedElement.HasDialog) _commandController.ShowElementProperties(Canvas.SelectedElement);
    }

    private void EditSelectAllMenuItem_Click(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.All);
    }

    private void EditSelectNoneMenuItem_Click(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.None);
    }

    private void EllipseToolStripMenuItemClick(object sender, EventArgs e) {
      foreach (var room in Canvas.SelectedRooms) room.Shape = RoomShape.Ellipse;
      Invalidate();
    }

    private void EndRoomToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetEndRoom();
    }

    private void ExportCode<T>() where T : CodeExporter, new() {
      using var exporter = new T();
      var s = exporter.Export();
      UserInteraction.SetClipboardText(s, TextDataFormat.Text);
    }

    private bool ExportCode<T>(ref string lastExportFileName) where T : CodeExporter, new() {
      using var exporter = new T();
      using var dialog = new SaveFileDialog();
      // compose filter string for file dialog
      var filterString = string.Empty;
      var filters = exporter.FileDialogFilters;
      foreach (var filter in filters) {
        if (!string.IsNullOrEmpty(filterString)) filterString += "|";
        filterString += $"{filter.Key}|*{filter.Value}";
      }

      if (!string.IsNullOrEmpty(filterString)) filterString += "|";
      filterString += "All Files|*.*||";
      dialog.Filter = filterString;

      // set default filter by extension
      var extension = PathHelper.SafeGetExtension(lastExportFileName);
      for (var filterIndex = 0; filterIndex < filters.Count; ++filterIndex)
        if (StringComparer.InvariantCultureIgnoreCase.Compare(extension, filters[filterIndex].Value) == 0) {
          dialog.FilterIndex = filterIndex + 1; // 1 based index
          break;
        }

      // show dialog
      dialog.Title = exporter.FileDialogTitle;
      dialog.InitialDirectory = PathHelper.SafeGetDirectoryName(lastExportFileName);
      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK)
        try {
          // export source code
          exporter.Export(dialog.FileName);
          lastExportFileName = dialog.FileName;
          return true;
        }
        catch (Exception ex) {
          UserInteraction.ShowMessage(Program.MainForm, $"There was a problem exporting the map:\n\n{ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

      return false;
    }

    private void ExportCodeCl<T>(string exportFile) where T : CodeExporter, new() {
      using var exporter = new T();
      exporter.Export(exportFile);
    }

    private string ExportImage() {
      var folder = PathHelper.SafeGetDirectoryName(Project.Current.FileName);
      var fileName = PathHelper.SafeGetFilenameWithoutExtension(Project.Current.FileName);

      var extension = GetExtensionForDefaultImageType();

      var imageFile = Path.Combine(folder, fileName + extension);
      try {
        if (!SaveImage(imageFile))
          return string.Empty;
      }
      catch (Exception) {
        return string.Empty;
      }

      return imageFile;
    }

    private string ExportPDF() {
      var folder = PathHelper.SafeGetDirectoryName(Project.Current.FileName);
      var fileName = PathHelper.SafeGetFilenameWithoutExtension(Project.Current.FileName);
      var pdfFile = Path.Combine(folder, fileName + ".pdf");
      try {
        SavePDF(pdfFile);
      }
      catch (Exception) {
        return string.Empty;
      }

      return pdfFile;
    }

    private void FileExitMenuItem_Click(object sender, EventArgs e) {
      Close();
    }

    private void FileExportAlanMenuItem_Click(object sender, EventArgs e) {
      var fileName = ApplicationSettingsController.AppSettings.LastExportAlanFileName;
      if (ExportCode<AlanExporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportAlanFileName = fileName;
    }

    private void FileExportAdventuronMenuItem_Click(object sender, EventArgs e)
    {
        var fileName = ApplicationSettingsController.AppSettings.LastExportAdventuronFileName;
        if (ExportCode<AdventuronExporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportAdventuronFileName = fileName;
    }

    private void FileExportHugoMenuItem_Click(object sender, EventArgs e) {
      var fileName = ApplicationSettingsController.AppSettings.LastExportHugoFileName;
      if (ExportCode<HugoExporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportHugoFileName = fileName;
    }

    private void FileExportImageMenuItem_Click(object sender, EventArgs e) {
      using var dialog = new SaveFileDialog();
      dialog.Filter = "PNG Images|*.png|JPEG Images|*.jpg|BMP Images|*.bmp|Enhanced Metafiles (EMF)|*.emf|All Files|*.*||";
      dialog.Title = "Export Image";
      dialog.DefaultExt = GetExtensionForDefaultImageType();
      dialog.InitialDirectory = PathHelper.SafeGetDirectoryName(ApplicationSettingsController.AppSettings.LastExportImageFileName);
      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK) {
        ApplicationSettingsController.AppSettings.LastExportImageFileName = Path.GetDirectoryName(dialog.FileName) + @"\";
        if (!SaveImage(dialog.FileName)) UserInteraction.ShowMessage("There was an error saving the image file.  Please make sure the image is not already opened.", "Export Image", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
      }
    }


    private void FileExportInform6MenuItem_Click(object sender, EventArgs e) {
      var fileName = ApplicationSettingsController.AppSettings.LastExportInform6FileName;
      if (ExportCode<Inform6Exporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportInform6FileName = fileName;
    }

    private void FileExportInform7MenuItem_Click(object sender, EventArgs e) {
      var fileName = ApplicationSettingsController.AppSettings.LastExportInform7FileName;
      if (ExportCode<Inform7Exporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportInform7FileName = fileName;
    }

    private void FileExportPDFMenuItem_Click(object sender, EventArgs e) {
      using var dialog = new SaveFileDialog();
      dialog.Filter = "PDF Files|*.pdf|All Files|*.*||";
      dialog.Title = "Export PDF";
      dialog.InitialDirectory = PathHelper.SafeGetDirectoryName(ApplicationSettingsController.AppSettings.LastExportImageFileName);
      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK)
        try {
          SavePDF(dialog.FileName);
        }
        catch (Exception ex) {
          UserInteraction.ShowMessage(Program.MainForm, $"There was a problem exporting the map:\n\n{ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FileExportQuestMenuItem_Click(object sender, EventArgs e) {
      var fileName = ApplicationSettingsController.AppSettings.LastExportQuestFileName;
      if (ExportCode<QuestExporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportQuestFileName = fileName;
    }

    private void FileExportTadsMenuItem_Click(object sender, EventArgs e) {
      var fileName = ApplicationSettingsController.AppSettings.LastExportTadsFileName;
      if (ExportCode<TadsExporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportTadsFileName = fileName;
    }


        private void FileMenu_DropDownOpening(object sender, EventArgs e) {
      SetupMruMenu();

      SetupExportMenu();
    }

    private void FileNewMenuItem_Click(object sender, EventArgs e) {
      if (!CheckLoseProject())
        return;

      Project.Current = new Project();
      Settings.Reset();
    }

    private void FileOpenMenuItem_Click(object sender, EventArgs e) {
      OpenProject();
    }

    private void FileRecentProject_Click(object sender, EventArgs e) {
      if (!CheckLoseProject()) return;

      var fileName = (string) ((ToolStripMenuItem) sender).Tag;
      OpenProject(fileName);
    }

    private void FileSaveAsMenuItem_Click(object sender, EventArgs e) {
      SaveAsProject();
    }

    private void FileSaveMenuItem_Click(object sender, EventArgs e) {
      if (Project.Current.FileName.IsUrl())
        SaveAsProject();
      else
        SaveProject();
    }

    private static string GetExtensionForDefaultImageType() {
      var extension = ".png";
      switch (ApplicationSettingsController.AppSettings.DefaultImageType) {
        case 0:
          extension = ".png";
          break;
        case 1:
          extension = ".jpg";
          break;
        case 2:
          extension = ".bmp";
          break;
        case 3:
          extension = ".emf";
          break;
      }

      return extension;
    }

    private void HandDrawnToolStripMenuItemClick(object sender, EventArgs e) {
      foreach (var room in Canvas.SelectedRooms) room.Shape = RoomShape.SquareCorners;
      Invalidate();
    }

    private void HelpAboutMenuItem_Click(object sender, EventArgs e) {
      using var dialog = new AboutDialog();
      UserInteraction.ShowDialog(dialog);
    }

    private void HelpAndSupportMenuItem_Click(object sender, EventArgs e) {
      using var dialog = new OnlineHelpDialog();
      UserInteraction.ShowDialog(dialog, this);
    }

    private void HugoToTextToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<HugoExporter>();
    }

    private void Inform6ToTextToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<Inform6Exporter>();
    }

    private void Inform7ToTextToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<Inform7Exporter>();
    }

    private void InLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.In);
    }

    private void JoinRoomsToolStripMenuItemClick(object sender, EventArgs e) {
      Canvas.JoinSelectedRooms(Canvas.SelectedRooms.First(), Canvas.SelectedRooms.Last());
    }

    private void EditChangeRegionMenuItemClick(object sender, EventArgs e) {
      if (Canvas.HasSingleSelectedElement && Canvas.SelectedElement.HasDialog && Canvas.SelectedElement is Room element) {
        var room = element;
        room.ShowDialog(PropertiesStartType.Region);
      }
    }

    private void EditCopyColorToolMenuItemClick(object sender, EventArgs e) {
      Canvas.CopySelectedColor();
    }

    private void EditCopyMenuItemClick(object sender, EventArgs e) {
      Canvas.CopySelectedElements();
    }

    private void EditPasteMenuItemClick(object sender, EventArgs e) {
      Canvas.Paste(false);
    }

    private void MainForm_Load(object sender, EventArgs e) {
      SetupStatusBar();
      Canvas.MinimapVisible = ApplicationSettingsController.AppSettings.ShowMiniMap;
      var projectLoaded = false;

      var args = Environment.GetCommandLineArgs();

      List<Error> parseErrors;
      var ext = Parser.Default.ParseArguments<CommandLineOptions>(args).WithNotParsed(errors => parseErrors = errors.ToList());

      if (ext.Tag == ParserResultType.Parsed) {
        var result = (Parsed<CommandLineOptions>) ext;
        projectLoaded = CommandLineActions(result.Value);
      }

      if (ApplicationSettingsController.AppSettings.LoadLastProjectOnStart && !projectLoaded)
        try {
          if (ApplicationSettingsController.AppSettings.LastProjectFileName.IsUrl() || File.Exists(ApplicationSettingsController.AppSettings.LastProjectFileName))
            BeginInvoke((MethodInvoker) delegate { OpenProject(ApplicationSettingsController.AppSettings.LastProjectFileName); });
        }
        catch (Exception) {
          // ignored
        }
    }

    private void SetupStatusBar() {
      _trizStatusBar = new Status(_statusBar);
      _trizStatusBar.UpdateStatusBar();
    }

    private void MakeRoomDarkToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetRoomLighting(LightingActionType.ForceDark);
    }

    private void MakeRoomLightToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetRoomLighting(LightingActionType.ForceLight);
    }

    private void MapStatisticsExportToolStripMenuItemClick(object sender, EventArgs e) {
      var frm = new MapStatisticsView();
      frm.MapStatisticsView_Export(sender, e);
    }

    private void MapStatisticsToolStripMenuItemClick(object sender, EventArgs e) {
      var frm = new MapStatisticsView();
      UserInteraction.ShowDialog(frm);
    }

    private void OctagonalEdgesToolStripMenuItemClick(object sender, EventArgs e) {
      foreach (var room in Canvas.SelectedRooms) room.Shape = RoomShape.Octagonal;
      Invalidate();
    }

    private void OnIdle(object sender, EventArgs e) {
      var now = DateTime.Now;
      if (now - _lastUpdateUITime > _idleProcessingEveryNSeconds) {
        _lastUpdateUITime = now;
        Task.Run(UpdateCommandUI);
      }
    }

    private void OnMAutomapBarOnStopClick(object sender, EventArgs e) {
      Canvas.StopAutomapping();
    }

    private void OutLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Out);
    }

    private void PlainLinesMenuItem_Click(object sender, EventArgs e) {
      Canvas.ApplyNewPlainConnectionSettings();
    }

    private void ProjectResetToDefaultSettingsMenuItem_Click(object sender, EventArgs e) {
      if (UserInteraction.ShowMessage("Restore default settings?\n\nThis will revert any changes to settings in this project.", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) Settings.Reset();
    }

    private void ProjectSettingsMenuItem_Click(object sender, EventArgs e) {
      Settings.ShowMapDialog();
      Canvas.Refresh();
    }

    private void QuestRoomsToTextToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<QuestRoomsExporter>();
    }

    private void QuestToTextToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<QuestExporter>();
    }

    private void ReverseLineMenuItem_Click(object sender, EventArgs e) {
      Canvas.ReverseLineDirection();
    }

    private void RoomsMustHaveADescriptionToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetValidation(ValidationType.RoomDescription);
      Project.Current.Canvas.Invalidate();
    }

    private void RoomsMustHaveASubtitleToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetValidation(ValidationType.RoomSubTitle);
      Project.Current.Canvas.Invalidate();
    }

    private void RoomsMustHaveUniqueNamesToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetValidation(ValidationType.RoomUniqueName);
      Project.Current.Canvas.Invalidate();
    }

    private void RoomsMustNotHaveADanglingConnectionToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetValidation(ValidationType.RoomDanglingConnection);
      Project.Current.Canvas.Invalidate();
    }

    private void RoundedEdgesToolStripMenuItemClick(object sender, EventArgs e) {
      foreach (var room in Canvas.SelectedRooms) room.Shape = RoomShape.RoundedCorners;
      Invalidate();
    }

    private void SaveAsCmdLineProject(string outfile) {
      ApplicationSettingsController.AppSettings.LastProjectFileName = outfile;
      Project.Current.FileName = outfile;
      if (Project.Current.Save()) {
        if (ApplicationSettingsController.AppSettings.RecentProjects.Contains(Project.Current.FileName)) {
          ApplicationSettingsController.AppSettings.RecentProjects.Remove(Project.Current.FileName);
        }
        ApplicationSettingsController.AppSettings.RecentProjects.Insert(0, Project.Current.FileName);
        if (ApplicationSettingsController.AppSettings.RecentProjects.Count > ApplicationSettingsController.RecentProjectsMaxCount) {
          ApplicationSettingsController.AppSettings.RecentProjects.RemoveRange(ApplicationSettingsController.RecentProjectsMaxCount, ApplicationSettingsController.AppSettings.RecentProjects.Count - ApplicationSettingsController.RecentProjectsMaxCount);
        }
        Project.Current.IsDirty = false;
      }
    }

    private bool SaveAsProject() {
      using var dialog = new SaveFileDialog();
      if (!Project.Current.FileName.IsUrl()) {
        if (!string.IsNullOrEmpty(Project.Current.FileName))
          dialog.FileName = Project.Current.FileName;
        else
          dialog.InitialDirectory = PathHelper.SafeGetDirectoryName(ApplicationSettingsController.AppSettings.LastProjectFileName);
      } else {
        dialog.FileName = Path.GetFileName(Project.Current.FileName);
      }

      dialog.Filter = $"{Project.FilterString}|All Files|*.*||";
      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK) {
        ApplicationSettingsController.AppSettings.LastProjectFileName = dialog.FileName;
        Project.Current.FileName = dialog.FileName;
        if (Project.Current.Save(true)) {
          if (ApplicationSettingsController.AppSettings.RecentProjects.Contains(Project.Current.FileName)) {
            ApplicationSettingsController.AppSettings.RecentProjects.Remove(Project.Current.FileName);
          }
          ApplicationSettingsController.AppSettings.RecentProjects.Insert(0, Project.Current.FileName);
          if (ApplicationSettingsController.AppSettings.RecentProjects.Count > ApplicationSettingsController.RecentProjectsMaxCount) {
            ApplicationSettingsController.AppSettings.RecentProjects.RemoveRange(ApplicationSettingsController.RecentProjectsMaxCount, ApplicationSettingsController.AppSettings.RecentProjects.Count - ApplicationSettingsController.RecentProjectsMaxCount);
          }
          return true;
        }
      }

      return false;
    }

    private bool SaveImage(string fileName) {
      var succeeded = true;

      var format = ImageFormat.Png;
      var ext = Path.GetExtension(fileName);
      if (StringComparer.InvariantCultureIgnoreCase.Compare(ext, ".jpg") == 0
          || StringComparer.InvariantCultureIgnoreCase.Compare(ext, ".jpeg") == 0)
        format = ImageFormat.Jpeg;
      else if (StringComparer.InvariantCultureIgnoreCase.Compare(ext, ".bmp") == 0)
        format = ImageFormat.Bmp;
      else if (StringComparer.InvariantCultureIgnoreCase.Compare(ext, ".emf") == 0) format = ImageFormat.Emf;

      var size = Canvas.ComputeCanvasBounds(true).Size * (ApplicationSettingsController.AppSettings.SaveAt100 ? 1.0f : Canvas.ZoomFactor);
      size.X = Numeric.Clamp(size.X, 16, 8192);
      size.Y = Numeric.Clamp(size.Y, 16, 8192);

      try {
        if (Equals(format, ImageFormat.Emf)) {
          using var nativeGraphics = Graphics.FromHwnd(Canvas.Handle);
          using var stream = new MemoryStream();
          try {
            var dc = nativeGraphics.GetHdc();
            using var metafile = new Metafile(stream, dc);
            using (var imageGraphics = Graphics.FromImage(metafile)) {
              using (var graphics = XGraphics.FromGraphics(imageGraphics, new XSize(size.X, size.Y))) {
                Canvas.Draw(graphics, true, size.X, size.Y);
              }
            }

            var handle = metafile.GetHenhmetafile();
            var copy = CopyEnhMetaFile(handle, fileName);
            if (copy == IntPtr.Zero)
              succeeded = false;

            DeleteEnhMetaFile(copy);
          }
          catch {
            succeeded = false;
          }
          finally {
            nativeGraphics.ReleaseHdc();
          }
        }
        else {
          using var bitmap = new Bitmap((int) Math.Ceiling(size.X), (int) Math.Ceiling(size.Y));
          using (var imageGraphics = Graphics.FromImage(bitmap)) {
            using (var graphics = XGraphics.FromGraphics(imageGraphics, new XSize(size.X, size.Y))) {
              Canvas.Draw(graphics, true, size.X, size.Y);
            }
          }

          bitmap.Save(fileName, format);
        }
      }
      catch {
        succeeded = false;
      }

      return succeeded;
    }

    private void SavePDF(string fileName) {
      ApplicationSettingsController.AppSettings.LastExportImageFileName = fileName;
      MapPdfExporter.Save(Canvas, fileName);
    }

    private bool SaveProject() {
      if (Project.Current.FileName.IsUrl())
      {
        UserInteraction.ShowMessage("You are trying to save a map loaded from the web.  Please use the 'Save Map As...' to save the map to your local drive.", "Problem saving map.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        return false;
      }

      if (!Project.Current.HasFileName) return SaveAsProject();

      if (Project.Current.Save()) {
        if (ApplicationSettingsController.AppSettings.RecentProjects.Contains(Project.Current.FileName)) {
          ApplicationSettingsController.AppSettings.RecentProjects.Remove(Project.Current.FileName);
        }
        ApplicationSettingsController.AppSettings.RecentProjects.Insert(0, Project.Current.FileName);
        if (ApplicationSettingsController.AppSettings.RecentProjects.Count > ApplicationSettingsController.RecentProjectsMaxCount) {
          ApplicationSettingsController.AppSettings.RecentProjects.RemoveRange(ApplicationSettingsController.RecentProjectsMaxCount, ApplicationSettingsController.AppSettings.RecentProjects.Count - ApplicationSettingsController.RecentProjectsMaxCount);
        }
        return true;
      }

      return false;
    }

    private void SelectAllConnectionsToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.Connections);
    }

    private void SelectAllRoomsToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.Rooms);
    }

    private void SelectDanglingConnectionsToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.DanglingConnections);
    }

    private void SelectedUnconnectedRoomsToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.UnconnectedRooms);
    }

    private void SelectRoomsWObjectsToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.RoomsWithObjects);
    }

    private void SelectRoomsWoObjectsToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.RoomsWithOutObjects);
    }

    private void SelectSelfLoopingConnectionsToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.Select(SelectTypes.SelfLoopingConnections);
    }

    private void SetupExportMenu() {
      if (Project.Current.Elements.OfType<Room>().Any()) {
        _fileExportAlanMenuItem.Enabled = true;
        _fileExportHugoMenuItem.Enabled = true;
        _fileExportInform7MenuItem.Enabled = true;
        _fileExportInform6MenuItem.Enabled = true;
        _fileExportTADSMenuItem.Enabled = true;
        _zILToolStripMenuItem.Enabled = true;
      } else {
        _fileExportAlanMenuItem.Enabled = false;
        _fileExportHugoMenuItem.Enabled = false;
        _fileExportInform7MenuItem.Enabled = false;
        _fileExportInform6MenuItem.Enabled = false;
        _fileExportTADSMenuItem.Enabled = false;
        _zILToolStripMenuItem.Enabled = false;
      }
    }

    private void SetupMruMenu() {
      var existingItems = _fileRecentMapsMenuItem.DropDownItems.Cast<ToolStripItem>().ToList();
      foreach (var existingItem in existingItems) {
        existingItem.Click -= FileRecentProject_Click;
        existingItem.Dispose();
      }

      if (ApplicationSettingsController.AppSettings.RecentProjects.Count == 0) {
        _fileRecentMapsMenuItem.Enabled = false;
      } else {
        _fileRecentMapsMenuItem.Enabled = true;
        var index = 1;
        var removedFiles = new List<string>();
        foreach (var recentProject in ApplicationSettingsController.AppSettings.RecentProjects)
          if (recentProject.IsUrl() || File.Exists(recentProject)) {
            var menuItem = new ToolStripMenuItem($"&{index++} {recentProject}") {Tag = recentProject};
            menuItem.Click += FileRecentProject_Click;
            _fileRecentMapsMenuItem.DropDownItems.Add(menuItem);
          } else {
            removedFiles.Add(recentProject);
          }

        if (removedFiles.Any()) removedFiles.ForEach(p => ApplicationSettingsController.AppSettings.RecentProjects.Remove(p));
      }
    }

    private void SmartSave(bool silent = false) {
      if (!ApplicationSettingsController.AppSettings.SaveToPDF && !ApplicationSettingsController.AppSettings.SaveToImage) {
        if (!silent)
          UserInteraction.ShowMessage("Your settings are set to not save anything. Please check your App Settings if this is not what you want.");
        return;
      }

      var saved = false;
      if (Project.Current.FileName.IsUrl() || (!Project.Current.HasFileName || Project.Current.IsDirty)) {
        if (UserInteraction.ShowMessage("Your project needs to be saved before we can do a SmartSave.  Would you like to save the project now?", "Save Project?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
          saved = Project.Current.FileName.IsUrl() ? SaveAsProject() : SaveProject();
        }
      } else {
        saved = true;
      }


      if (saved) {
        if (Project.Current.HasFileName) {
          var bSaveError = false;
          var pDFFile = string.Empty;
          if (ApplicationSettingsController.AppSettings.SaveToPDF) {
            pDFFile = ExportPDF();
            if (pDFFile == string.Empty) {
              UserInteraction.ShowMessage("There was an error saving the PDF file during the SmartSave.  Please make sure the PDF is not already opened.", "Smart Save", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
              bSaveError = true;
            }
          }

          var imageFile = string.Empty;
          if (ApplicationSettingsController.AppSettings.SaveToImage) {
            imageFile = ExportImage();
            if (imageFile == string.Empty) {
              UserInteraction.ShowMessage("There was an error saving the Image file during the SmartSave.  Please make sure the Image is not already opened.", "Smart Save", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
              bSaveError = true;
            }
          }

          if (!bSaveError) {
            var text = string.Empty;
            if (ApplicationSettingsController.AppSettings.SaveToPDF) text += $"PDF file has been saved to {pDFFile}";

            if (ApplicationSettingsController.AppSettings.SaveToImage) {
              if (text != string.Empty)
                text += Environment.NewLine;
              text += $"Image file has been saved to {imageFile}";
            }

            if (!silent) UserInteraction.ShowMessage(text, "Smart Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
          }
        }
      } else {
        UserInteraction.ShowMessage("No files have been saved during the SmartSave.");
      }
    }

    private void SmartSaveToolStripMenuItemClick(object sender, EventArgs e) {
      SmartSave();
    }

    private void StartRoomToolStripMenuItemClick(object sender, EventArgs e) {
      _commandController.SetStartRoom();
    }

    private void SwapFormatsFillsToolStripMenuItemClick(object sender, EventArgs e) {
      Canvas.SwapRoomFill();
    }

    private void SwapNamesToolStripMenuItemClick(object sender, EventArgs e) {
      Canvas.SwapRoomNames();
    }

    private void SwapObjectsToolStripMenuItemClick(object sender, EventArgs e) {
      Canvas.SwapRooms();
    }

    private void SwapRegionsToolStripMenuItemClick(object sender, EventArgs e) {
      Canvas.SwapRoomRegions();
    }

    private void TADSToTextToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<TadsExporter>();
    }

    private void ToggleDirectionalLines_Click(object sender, EventArgs e) {
      _commandController.ToggleConnectionFlow(Canvas.NewConnectionFlow);
    }

    private void ToggleDottedLines_Click(object sender, EventArgs e) {
      _commandController.ToggleConnectionStyle(Canvas.NewConnectionStyle);
    }

    private void ToggleTextToolStripMenuItemClick(object sender, EventArgs e) {
      Canvas.ToggleText();
    }

    private void UpdateCommandUI() {

      _synchronizationContext.Post(o => {
        // caption
        Text = $"{(ApplicationSettingsController.AppSettings.ShowFullPathInTitleBar && !string.IsNullOrEmpty(Project.Current.FileName) ? Project.Current.FileName : Project.Current.Name)}{(Project.Current.IsDirty ? "*" : string.Empty)} - {_caption} - {Application.ProductVersion}";
        _trizStatusBar.UpdateStatusBar();

        // line drawing options
        _toggleDottedLinesButton.Checked = Canvas.NewConnectionStyle == ConnectionStyle.Dashed;
        _toggleDottedLinesMenuItem.Checked = _toggleDottedLinesButton.Checked;
        _toggleDirectionalLinesButton.Checked = Canvas.NewConnectionFlow == ConnectionFlow.OneWay;
        _toggleDirectionalLinesMenuItem.Checked = _toggleDirectionalLinesButton.Checked;
        _plainLinesMenuItem.Checked = !_toggleDirectionalLinesMenuItem.Checked && !_toggleDottedLinesMenuItem.Checked && Canvas.NewConnectionLabel == ConnectionLabel.None;
        _upLinesMenuItem.Checked = Canvas.NewConnectionLabel == ConnectionLabel.Up;
        _downLinesMenuItem.Checked = Canvas.NewConnectionLabel == ConnectionLabel.Down;
        _inLinesMenuItem.Checked = Canvas.NewConnectionLabel == ConnectionLabel.In;
        _outLinesMenuItem.Checked = Canvas.NewConnectionLabel == ConnectionLabel.Out;

        // selection-specific commands
        var hasSelectedElement = Canvas.SelectedElement != null;
        _editDeleteMenuItem.Enabled = hasSelectedElement;
        _editPropertiesMenuItem.Enabled = Canvas.HasSingleSelectedElement;
        _editSelectNoneMenuItem.Enabled = hasSelectedElement;
        _editSelectAllMenuItem.Enabled = Canvas.SelectedElementCount < Project.Current.Elements.Count;
        _editCopyMenuItem.Enabled = Canvas.SelectedElement != null;
        _editCopyColorToolMenuItem.Enabled = Canvas.HasSingleSelectedElement && Canvas.SelectedElement is Room;
        _editPasteMenuItem.Enabled = ClipboardHelper.HasSomethingToPaste();
        if (Canvas.HasSingleSelectedElement) //Allow flipping light in all rooms if 1+ are selected. Issue #138 flicker
          _editIsDarkMenuItem.Enabled = Canvas.HasSingleSelectedElement && Canvas.SelectedElement is Room;
        else
          _editIsDarkMenuItem.Enabled = hasSelectedElement;
        _editIsDarkMenuItem.Checked = Canvas.HasSingleSelectedElement && Canvas.SelectedElement is Room && ((Room) Canvas.SelectedElement).IsDark;
        _editRenameMenuItem.Enabled = Canvas.HasSingleSelectedElement && Canvas.SelectedElement is Room;
        _joinRoomsToolStripMenuItem.Enabled = Canvas.SelectedRooms.Count == 2 && !Project.Current.AreRoomsConnected(Canvas.SelectedRooms);
        _swapObjectsToolStripMenuItem.Enabled = Canvas.SelectedRooms.Count == 2;
        _swapNamesToolStripMenuItem.Enabled = Canvas.SelectedRooms.Count == 2;
        _swapFormatsFillsToolStripMenuItem.Enabled = Canvas.SelectedRooms.Count == 2;
        _swapRegionsToolStripMenuItem.Enabled = Canvas.SelectedRooms.Count == 2;

        _startRoomToolStripMenuItem.Enabled = Canvas.HasSingleSelectedElement && Canvas.SelectedElement is Room;
        _startRoomToolStripMenuItem.Checked = Canvas.HasSingleSelectedElement && Canvas.SelectedElement is Room && ((Room) Canvas.SelectedElement).IsStartRoom;
        _endRoomToolStripMenuItem.Enabled = Canvas.HasSelectedRooms;
        _endRoomToolStripMenuItem.Checked = Canvas.HasSelectedRooms && (Canvas.SelectedRooms.Any(p=>p.IsEndRoom));


        _roomsMustHaveUniqueNamesToolStripMenuItem.Checked = Project.Current.MustHaveUniqueNames;
        _roomsMustHaveADescriptionToolStripMenuItem.Checked = Project.Current.MustHaveDescription;
        _roomsMustHaveASubtitleToolStripMenuItem.Checked = Project.Current.MustHaveSubtitle;
        _roomsMustNotHaveADanglingConnectionToolStripMenuItem.Checked = Project.Current.MustHaveNoDanglingConnectors;

        _editChangeRegionMenuItem.Enabled = Canvas.SelectedRooms.Any() && Settings.Regions.Count > 1;
        _handDrawnToolStripMenuItem.Enabled = Canvas.SelectedRooms.Any();
        _ellipseToolStripMenuItem.Enabled = Canvas.SelectedRooms.Any();
        _roundedEdgesToolStripMenuItem.Enabled = Canvas.SelectedRooms.Any();
        _octagonalEdgesToolStripMenuItem.Enabled = Canvas.SelectedRooms.Any();
        _reverseLineMenuItem.Enabled = Canvas.HasSelectedElement<Connection>();

        // automapping
        _automapStartMenuItem.Enabled = !Canvas.IsAutomapping;
        _automapStopMenuItem.Enabled = Canvas.IsAutomapping;
        _automapBar.Visible = Canvas.IsAutomapping;
        _automapBar.Status = Canvas.AutomappingStatus;

        // minimap
        _viewMinimapMenuItem.Checked = Canvas.MinimapVisible;

        _viewShowGridMenuItem.Checked = Settings.IsGridVisible;

        UpdateToolStripImages();
        Canvas.UpdateScrollBars();


      }, null);

    }

    private void UpdateToolStripImages() {
      foreach (ToolStripItem item in _toolStrip.Items) {
        if (!(item is ToolStripButton))
          continue;

        var button = (ToolStripButton) item;
        button.BackgroundImage = button.Checked ? Resources.ToolStripBackground2 : Resources.ToolStripBackground;
      }
    }

    private void UpLinesMenuItem_Click(object sender, EventArgs e) {
      _commandController.SetConnectionLabel(ConnectionLabel.Up);
    }

    private void ViewEntireMapMenuItem_Click(object sender, EventArgs e) {
      Canvas.ZoomToFit();
    }

    private void ViewMinimapMenuItem_Click(object sender, EventArgs e) {
      Canvas.MinimapVisible = !Canvas.MinimapVisible;
      ApplicationSettingsController.AppSettings.ShowMiniMap = Canvas.MinimapVisible;
    }

    private void ViewShowGridMenuItem_Click(object sender, EventArgs e) {
      _viewShowGridMenuItem.Checked = !_viewShowGridMenuItem.Checked;
      Settings.IsGridVisible = _viewShowGridMenuItem.Checked;
      Project.Current.IsDirty = true;
    }

    private void ViewResetMenuItem_Click(object sender, EventArgs e) {
      Canvas.ResetZoomOrigin();
    }

    private void ViewZoomFiftyPercentMenuItem_Click(object sender, EventArgs e) {
      Canvas.ZoomFactor = 0.5f;
    }

    private void ViewZoomInMenuItem_Click(object sender, EventArgs e) {
      Canvas.ZoomIn();
    }

    private void ViewZoomMiniIn_Click(object sender, EventArgs e) {
      Canvas.ZoomInMicro();
    }

    private void ViewZoomMiniOut_Click(object sender, EventArgs e) {
      Canvas.ZoomOutMicro();
    }

    private void ViewZoomOneHundredPercentMenuItem_Click(object sender, EventArgs e) {
      Canvas.ZoomFactor = 1.0f;
    }

    private void ViewZoomOutMenuItem_Click(object sender, EventArgs e) {
      Canvas.ZoomOut();
    }

    private void ViewZoomTwoHundredPercentMenuItem_Click(object sender, EventArgs e) {
      Canvas.ZoomFactor = 2.0f;
    }

    private void ZILToClipboardToolStripMenuItemClick(object sender, EventArgs e) {
      ExportCode<ZilExporter>();
    }

    private void ZILToolStripMenuItemClick(object sender, EventArgs e) {
      var fileName = ApplicationSettingsController.AppSettings.LastExportZilFileName;
      if (ExportCode<ZilExporter>(ref fileName)) ApplicationSettingsController.AppSettings.LastExportZilFileName = fileName;
    }

    private void FileOpenFromWebMenuItemClick(object sender, EventArgs e) {
      string url = string.Empty;
      InputDialogItem[] items = {
        new InputDialogItem("URL", url)
      };

      InputDialog input = InputDialog.Show("Load from Web", items, InputBoxButtons.OkCancel);
      if (input.Result == InputBoxResult.Ok) {
        OpenUrl(input.Items["URL"]);
      }
    }

    private void OpenUrl(string url) {
      if (!CheckLoseProject())
        return;

      var uri = new Uri(url);

      OpenProjectFromUrl(uri);
    }

    public void OpenProject(string fileName) {
      var project = new Project {FileName = fileName};
      if (project.Load()) {
        Project.Current = project;
        ApplicationSettingsController.OpenProject(fileName);
      }
    }

    private void OpenProjectFromUrl(Uri uri) {
      var project = new Project { FileName = Path.GetFileName(uri.AbsoluteUri) };
      if (project.Load(uri))
      {
        Project.Current = project;
        ApplicationSettingsController.OpenProject(uri.AbsoluteUri);
      }

    }
  }
}