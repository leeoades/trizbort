using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Watchers;
using Trizbort.UI;

namespace Trizbort;

internal static class Program
{
  public static MainForm MainForm { get; private set; }

  /// <summary>
  ///   The main entry point for the application.
  /// </summary>
  [STAThread]
  private static void Main(string[] args)
  {
    ApplicationConfiguration.Initialize();

    CopyController.SetClipboardTextHandler = text => UserInteraction.SetClipboardText(text);
    CopyController.GetClipboardTextHandler = () => UserInteraction.GetClipboardText();
    Automap.Automap.ErrorReporter = (msg, title) => UserInteraction.ShowMessage(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    Automap.Automap.ConflictingRoomChooser = (existing, name) => {
      using var dialog = new AutomapRoomSameDirectionDialog { Room1 = existing, Room2 = name };
      UserInteraction.ShowDialog(dialog);
      return dialog.Result;
    };
    MapLoader.DefaultUnknownFileReporter = message => UserInteraction.ShowMessage(
      message,
      "Not a valid file",
      MessageBoxButtons.OK,
      MessageBoxIcon.Exclamation);
    LegacyMapFileEngine.DefaultErrorReporter = ex => UserInteraction.ShowMessage(
      MainForm,
      $"There was a problem loading the map:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
      Application.ProductName,
      MessageBoxButtons.OK,
      MessageBoxIcon.Error);
    LegacyMapFileEngine.DefaultWarningReporter = (message, title) => UserInteraction.ShowMessage(message, title);
    Project.SaveErrorReporter = ex => UserInteraction.ShowMessage(
      MainForm,
      $"There was a problem saving the map:\n\n{ex.Message}",
      Application.ProductName,
      MessageBoxButtons.OK,
      MessageBoxIcon.Error);
    Project.BackupReporter = (filename, success) => {
      if (success)
        UserInteraction.ShowMessage($"Your project has been backed up to {filename}.", "Project backed up.", MessageBoxButtons.OK, MessageBoxIcon.Information);
      else
        UserInteraction.ShowMessage("Your project has not yet been saved to a file. There is nothing to backup.", "Nothing to backup.", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
    };
    Project.MapReloader = filename => MainForm?.OpenProject(filename);
    Project.SelectedElementsProvider = () => (IEnumerable<Element>)MainForm?.Canvas.SelectedElements ?? Array.Empty<Element>();
    Project.DocVersionWarningReporter = (warning, appVers, docVers) => {
      var infoList = $"Executable Version = {appVers}{Environment.NewLine}Document Version = {docVers}{Environment.NewLine}{Environment.NewLine}";
      var newVersionText = "Visit https://github.com/leeoades/trizbort/releases to download the latest version.";
      string message;
      switch (warning)
      {
        case DocumentVersionWarning.Major:
          message = $"{infoList}The document is ahead a major version. Information is very likely to be lost.{Environment.NewLine}{Environment.NewLine}{newVersionText}";
          break;
        case DocumentVersionWarning.Minor:
          message = $"{infoList}The document is ahead a minor version. Information is likely to be lost.{Environment.NewLine}{Environment.NewLine}{newVersionText}";
          break;
        case DocumentVersionWarning.Build:
          message = $"{infoList}The document is ahead a build. Information is somewhat likely to be lost.{Environment.NewLine}{Environment.NewLine}{newVersionText}";
          break;
        default:
          message = $"{infoList}The document is ahead a minor revision. Information may possibly be lost.{Environment.NewLine}{Environment.NewLine}{newVersionText}";
          break;
      }
      UserInteraction.ShowMessage(MainForm, message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    };
    TrizbortFileWatcher.UIThreadInvoker = action => {
      if (MainForm?.Canvas != null && MainForm.Canvas.IsHandleCreated)
        MainForm.Canvas.BeginInvoke(action);
      else
        action();
    };
    TrizbortFileWatcher.ConfirmReloadPrompt = isDirty => {
      var dirtyMsg = isDirty ? " and lose changes made in Trizbort" : "";
      return UserInteraction.ShowMessage(
        MainForm?.Canvas,
        $"This map has been modified by another program.{Environment.NewLine}Do you want to reload it{dirtyMsg}?",
        "Reload",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question) == DialogResult.Yes;
    };

    using var form = new MainForm();
    MainForm = form;
    Application.Run(form);
    MainForm = null;
  }
}
