using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using Trizbort.Automap;
using Trizbort.UI;
using Trizbort.Util;
using Formatting = Newtonsoft.Json.Formatting;

namespace Trizbort.Domain.AppSettings {
  public static class ApplicationSettingsController {
    public const int RECENT_PROJECTS_MAX_COUNT = 4;
    private const string APP_SETTINGS_FILE_NAME = @".\appsettings.json";
    private static readonly string LegacyAppSettingsPath = Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Genstein"), "Trizbort"), "Settings.xml");

    private static ApplicationSettings sEttings;

    public static ApplicationSettings AppSettings => sEttings;

    static ApplicationSettingsController() {
      sEttings = new ApplicationSettings();
      LoadSettings();
    }

    public static void LoadSettings() {
      // if app settings don't exist, create a default one
      if (!File.Exists(APP_SETTINGS_FILE_NAME)) {
        if (File.Exists(LegacyAppSettingsPath)) {
          loadLegacyAppSettings();
        } else {
          ResetSettings();
        }
        SaveSettings();
      }
      else {
        sEttings = JsonConvert.DeserializeObject<ApplicationSettings>(File.ReadAllText(APP_SETTINGS_FILE_NAME));
      }
    }

    public static void ResetSettings() {
      sEttings.DontCareAboutVersion = new Version(0, 0, 0, 0);
      sEttings.Automap = AutomapSettings.Default;
      sEttings.InfiniteScrollBounds = false;
      sEttings.ShowMiniMap = true;
      sEttings.SaveAt100 = true;
      sEttings.SaveToImage = true;
      sEttings.SaveToPDF = true;
      sEttings.SaveTadstoAdv3Lite = true;
      sEttings.RecentProjects.Clear();
      sEttings.ShowObjectsInTooltips = true;
      sEttings.ShowDescriptionsInTooltips = true;
      sEttings.ApplyStyleToNewRooms = false;
      sEttings.DoubleClickToAddRoom = false;
    }

    public static void SaveSettings() {
      var serializeObject = JsonConvert.SerializeObject(sEttings,Formatting.Indented);
      File.WriteAllText(APP_SETTINGS_FILE_NAME, serializeObject);
    }

    public static void ShowAppDialog() {
      using var dialog = new AppSettingsDialog();
      dialog.InvertMouseWheel = sEttings.InvertMouseWheel;
      dialog.ShowFullPathInTitleBar = sEttings.ShowFullPathInTitleBar;
      dialog.DefaultFontName = sEttings.DefaultFontName;
      dialog.DefaultImageType = sEttings.DefaultImageType;
      dialog.PortAdjustDetail = sEttings.PortAdjustDetail;
      dialog.SaveToImage = sEttings.SaveToImage;
      dialog.SaveToPDF = sEttings.SaveToPDF;
      dialog.SaveTadsToAdv3Lite = sEttings.SaveTadstoAdv3Lite;
      dialog.SaveAt100 = sEttings.SaveAt100;
      dialog.SpecifyGenMargins = sEttings.SpecifyGenMargins;
      dialog.GenHorizontalMargin = sEttings.GenHorizontalMargin;
      dialog.GenVerticalMargin = sEttings.GenVerticalMargin;
      dialog.LoadLastProjectOnStart = sEttings.LoadLastProjectOnStart;
      dialog.ApplyStyleToNewRooms = sEttings.ApplyStyleToNewRooms;
      dialog.DoubleClickToAddRoom = sEttings.DoubleClickToAddRoom;
      dialog.ShowDescriptionsInTooltip = sEttings.ShowDescriptionsInTooltips;
      dialog.ShowObjectsInTooltip = sEttings.ShowObjectsInTooltips;
      dialog.LimitConnectionDescriptionCharactersInTooltip = sEttings.LimitConnectionDescriptionCharactersInTooltip;
      dialog.ToolTipConnectionDescriptionCharactersToShow = sEttings.ToolTipConnectionDescriptionCharactersToShow;        
      dialog.LimitRoomDescriptionCharactersInTooltip = sEttings.LimitRoomDescriptionCharactersInTooltip;
      dialog.ToolTipRoomDescriptionCharactersToShow = sEttings.ToolTipRoomDescriptionCharactersToShow;

      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK) {
        sEttings.InvertMouseWheel = dialog.InvertMouseWheel;
        sEttings.ShowFullPathInTitleBar = dialog.ShowFullPathInTitleBar;
        sEttings.DefaultFontName = dialog.DefaultFontName;
        sEttings.DefaultImageType = dialog.DefaultImageType;
        sEttings.PortAdjustDetail = dialog.PortAdjustDetail;
        sEttings.SaveAt100 = dialog.SaveAt100;
        sEttings.SaveToImage = dialog.SaveToImage;
        sEttings.SaveToPDF = dialog.SaveToPDF;
        sEttings.SaveTadstoAdv3Lite = dialog.SaveTadsToAdv3Lite;
        sEttings.SpecifyGenMargins = dialog.SpecifyGenMargins;
        sEttings.GenHorizontalMargin = (int) dialog.GenHorizontalMargin;
        sEttings.GenVerticalMargin = (int) dialog.GenVerticalMargin;
        sEttings.LoadLastProjectOnStart = dialog.LoadLastProjectOnStart;
        sEttings.ApplyStyleToNewRooms = dialog.ApplyStyleToNewRooms;
        sEttings.DoubleClickToAddRoom = dialog.DoubleClickToAddRoom;
        sEttings.ShowDescriptionsInTooltips = dialog.ShowDescriptionsInTooltip;
        sEttings.ShowObjectsInTooltips = dialog.ShowObjectsInTooltip;
        sEttings.ToolTipConnectionDescriptionCharactersToShow = dialog.ToolTipConnectionDescriptionCharactersToShow;
        sEttings.LimitConnectionDescriptionCharactersInTooltip = dialog.LimitConnectionDescriptionCharactersInTooltip;  
        sEttings.ToolTipRoomDescriptionCharactersToShow = dialog.ToolTipRoomDescriptionCharactersToShow;
        sEttings.LimitRoomDescriptionCharactersInTooltip = dialog.LimitRoomDescriptionCharactersInTooltip;
        SaveSettings();
      }
    }

    private static void loadLegacyAppSettings() {
        try
        {
          if (File.Exists(LegacyAppSettingsPath))
          {
            var doc = new XmlDocument();
            doc.Load(LegacyAppSettingsPath);
            var root = new XmlElementReader(doc.DocumentElement);
            if (root.Name == "settings")
            {
              var versionText = root["dontCareAboutVersion"].Text;
              if (!string.IsNullOrEmpty(versionText))
              {
                sEttings.DontCareAboutVersion = new Version(versionText);
              }
              sEttings.InfiniteScrollBounds = root["infiniteScrollBounds"].ToBool(sEttings.InfiniteScrollBounds);
              sEttings.ShowMiniMap = root["showMiniMap"].ToBool(sEttings.ShowMiniMap);

             sEttings.LoadLastProjectOnStart = root["loadLastProjectOnStart"].ToBool(sEttings.LoadLastProjectOnStart);
             sEttings.LastProjectFileName = root["lastProjectFileName"].Text;
             sEttings.LastExportImageFileName = root["lastExportedImageFileName"].Text;
             sEttings.LastExportInform7FileName = root["lastExportedInform7FileName"].Text;
             sEttings.LastExportInform6FileName = root["lastExportedInform6FileName"].Text;
             sEttings.LastExportTadsFileName = root["lastExportedTadsFileName"].Text;
             sEttings.LastExportHugoFileName = root["lastExportedHugoFileName"].Text;
             sEttings.LastExportZilFileName = root["lastExportedZilFileName"].Text;
             sEttings.LastExportQuestFileName = root["lastExportedQuestFileName"].Text;

              sEttings.InvertMouseWheel = root["invertMouseWheel"].ToBool(sEttings.InvertMouseWheel);
              sEttings.PortAdjustDetail = root["portAdjustDetail"].ToInt(sEttings.PortAdjustDetail);
              sEttings.DefaultFontName = root["defaultFontName"].Text;

              if (sEttings.DefaultFontName.Length == 0) sEttings.DefaultFontName = "Arial"; // important for compatibility with 1.5.9.3 and before. Otherwise it's set to MS Sans Serif

              sEttings.DefaultImageType = root["defaultImageType"].ToInt(sEttings.DefaultImageType);
              sEttings.SaveToImage = root["saveToImage"].ToBool(sEttings.SaveToImage);
              sEttings.SaveToPDF = root["saveToPDF"].ToBool(sEttings.SaveToPDF);
              sEttings.SaveTadstoAdv3Lite = root["saveTADSToADV3Lite"].ToBool(sEttings.SaveTadstoAdv3Lite);
              sEttings.SaveAt100 = root["saveAt100"].ToBool(sEttings.SaveAt100);
              sEttings.SpecifyGenMargins = root["specifyMargins"].ToBool(sEttings.SpecifyGenMargins);
              sEttings.GenHorizontalMargin = root["horizontalMargin"].ToInt(sEttings.GenHorizontalMargin);
              sEttings.GenVerticalMargin = root["verticalMargin"].ToInt(sEttings.GenVerticalMargin);
              sEttings.ShowObjectsInTooltips = root["showObjectsInTooltips"].ToBool(true);
              sEttings.ShowDescriptionsInTooltips = root["showDescriptionsInTooltips"].ToBool(true);

              sEttings.CanvasWidth = root["canvasWidth"].ToInt(sEttings.CanvasWidth);
              sEttings.CanvasHeight = root["canvasHeight"].ToInt(sEttings.CanvasHeight);
              if (sEttings.CanvasWidth == 0) { sEttings.CanvasWidth = 624; }
              if (sEttings.CanvasHeight == 0) { sEttings.CanvasHeight = 450; }

              var recentProjects = root["recentProjects"];
              string fileName;
              var index = 0;
              do
              {
                fileName = recentProjects[$"fileName{index++}"].Text;
                if (!string.IsNullOrEmpty(fileName))
                {
                  sEttings.RecentProjects.Add(fileName);
                }
              } while (!string.IsNullOrEmpty(fileName));

              var automap = root["automap"];
              var settingsAutomap = sEttings.Automap;
              settingsAutomap.FileName = automap["transcriptFileName"].ToText(sEttings.Automap.FileName);
              settingsAutomap.VerboseTranscript = automap["verboseTranscript"].ToBool(sEttings.Automap.VerboseTranscript);
              settingsAutomap.AssumeRoomsWithSameNameAreSameRoom = automap["assumeRoomsWithSameNameAreSameRoom"].ToBool(sEttings.Automap.AssumeRoomsWithSameNameAreSameRoom);
              settingsAutomap.GuessExits = automap["guessExits"].ToBool(sEttings.Automap.GuessExits);
              settingsAutomap.AddObjectCommand = automap["addObjectCommand"].ToText(sEttings.Automap.AddObjectCommand);
              settingsAutomap.AddRegionCommand = automap["addRegionCommand"].ToText(sEttings.Automap.AddRegionCommand);
              sEttings.Automap = settingsAutomap;
            }
          }
        }
        catch (Exception)
        {
          // ignored
        }
      }

    public static void OpenProject(string fileName) {
      AppSettings.LastProjectFileName = fileName;
      if (AppSettings.RecentProjects.Contains(fileName)) {
        AppSettings.RecentProjects.Remove(fileName);
      }
      AppSettings.RecentProjects.Insert(0, fileName);

      if (AppSettings.RecentProjects.Count > RECENT_PROJECTS_MAX_COUNT) {
        AppSettings.RecentProjects.RemoveRange(RECENT_PROJECTS_MAX_COUNT, AppSettings.RecentProjects.Count - RECENT_PROJECTS_MAX_COUNT);
      }
      SaveSettings();
    }
  }
}