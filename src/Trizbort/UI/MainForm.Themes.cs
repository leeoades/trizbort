using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Setup;

namespace Trizbort.UI {
  public partial class MainForm {
    private const string ThemeFilter = "Trizbort theme (*.trizbort-theme)|*.trizbort-theme";

    private void InitializeThemeMenu() {
      var menu = new ToolStripMenuItem("&Themes");
      foreach (var theme in MapTheme.BuiltInThemes())
        menu.DropDownItems.Add(new ToolStripMenuItem(theme.Name, null, (_, __) => ApplyTheme(theme)));
      menu.DropDownItems.Add(new ToolStripSeparator());
      menu.DropDownItems.Add(new ToolStripMenuItem("&Import theme...", null, (_, __) => ImportTheme()));
      menu.DropDownItems.Add(new ToolStripMenuItem("&Export current theme...", null, (_, __) => ExportTheme()));
      menu.DropDownItems.Add(new ToolStripSeparator());
      menu.DropDownItems.Add(new ToolStripMenuItem("Infer default room style from &rooms...", null, (_, __) => InferDefaultRoomStyle()));
      var settingsIndex = m_projectSettingsMenuItem.Owner.Items.IndexOf(m_projectSettingsMenuItem);
      m_projectSettingsMenuItem.Owner.Items.Insert(settingsIndex + 1, menu);
    }

    private void ApplyTheme(MapTheme theme) {
      var choice = DialogResult.Yes;
      if (MapTheme.HasIndividualStyles(Project.Current))
        choice = MessageBox.Show(this,
          $"Apply the '{theme.Name}' theme?\n\nYes: replace individual room styles and room, connection and label colours.\n" +
          "No: keep individual styles and change only map-wide settings.\nCancel: leave the map unchanged.\n\n" +
          "Map text, room sizes and positions, region membership and game properties are preserved. There is no undo.",
          "Apply theme", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
      if (choice == DialogResult.Cancel) return;
      try {
        theme.Apply(choice == DialogResult.Yes);
        Canvas.Refresh();
      } catch (InvalidDataException exception) {
        ShowThemeError(exception);
      } catch (ArgumentException exception) {
        ShowThemeError(exception);
      }
    }

    private void ImportTheme() {
      using (var dialog = new OpenFileDialog { Filter = ThemeFilter, Title = "Import map theme" }) {
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        MapTheme theme;
        try {
          theme = MapTheme.Load(dialog.FileName);
        } catch (IOException exception) {
          ShowThemeError(exception);
          return;
        } catch (UnauthorizedAccessException exception) {
          ShowThemeError(exception);
          return;
        } catch (JsonException exception) {
          ShowThemeError(exception);
          return;
        }
        ApplyTheme(theme);
      }
    }

    private void ExportTheme() {
      using (var dialog = new SaveFileDialog {
        Filter = ThemeFilter, DefaultExt = "trizbort-theme", AddExtension = true,
        Title = "Export current map theme", FileName = "My theme.trizbort-theme"
      }) {
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try {
          MapTheme.Capture(Path.GetFileNameWithoutExtension(dialog.FileName)).Save(dialog.FileName);
        } catch (IOException exception) {
          ShowThemeError(exception);
        } catch (UnauthorizedAccessException exception) {
          ShowThemeError(exception);
        } catch (JsonException exception) {
          ShowThemeError(exception);
        }
      }
    }

    private void InferDefaultRoomStyle() {
      var inference = RoomStyleInference.Analyze(Project.Current);
      if (!inference.HasChanges) {
        MessageBox.Show(this,
          $"No room styling is shared by at least {RoomStyleInference.DefaultThreshold:P0} of rooms that isn't already the map default, " +
          "and no rooms have overrides that duplicate the defaults.",
          "Infer default room style", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
      }

      var changes = inference.Changes.Count > 0 ? "  • " + string.Join("\n  • ", inference.Changes) : "  (map defaults already match)";
      var message = $"Styling shared by at least {RoomStyleInference.DefaultThreshold:P0} of rooms will become the map default:\n\n{changes}\n\n" +
                    $"{inference.RedundantOverrides} individual colour override(s) on {inference.RoomsSimplified} room(s) will be removed " +
                    "because the default now provides them. Rooms that differ keep their own styling, and the map will look the same.\n\n" +
                    "Room shapes are stored per room, so a new default shape applies to new rooms and themes. There is no undo.\n\nContinue?";
      if (MessageBox.Show(this, message, "Infer default room style", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
      inference.Apply();
      Canvas.Refresh();
    }

    private void ShowThemeError(Exception exception) {
      MessageBox.Show(this, $"The theme could not be used.\n\n{exception.Message}",
        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }
}
