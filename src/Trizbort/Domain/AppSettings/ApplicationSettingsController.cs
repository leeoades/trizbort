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
    public const int RecentProjectsMaxCount = 4;
    private const string AppSettingsFileName = @".\appsettings.json";
    private static readonly string _legacyAppSettingsPath = Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Genstein"), "Trizbort"), "Settings.xml");

    private static ApplicationSettings _settings;

    public static ApplicationSettings AppSettings => _settings;

    static ApplicationSettingsController() {
      _settings = new ApplicationSettings();
      LoadSettings();
    }

    public static void LoadSettings() {
      // if app settings don't exist, create a default one
      if (!File.Exists(AppSettingsFileName)) {
        if (File.Exists(_legacyAppSettingsPath)) {
          LoadLegacyAppSettings();
        } else {
          ResetSettings();
        }
        SaveSettings();
      }
      else {
        _settings = JsonConvert.DeserializeObject<ApplicationSettings>(File.ReadAllText(AppSettingsFileName));
      }
    }

    public static void ResetSettings() {
      _settings.DontCareAboutVersion = new Version(0, 0, 0, 0);
      _settings.Automap = AutomapSettings.Default;
      _settings.InfiniteScrollBounds = false;
      _settings.ShowMiniMap = true;
      _settings.SaveAt100 = true;
      _settings.SaveToImage = true;
      _settings.SaveToPDF = true;
      _settings.SaveTadstoAdv3Lite = true;
      _settings.RecentProjects.Clear();
      _settings.ShowObjectsInTooltips = true;
      _settings.ShowDescriptionsInTooltips = true;
      _settings.ApplyStyleToNewRooms = false;
      _settings.DoubleClickToAddRoom = false;
    }

    public static void SaveSettings() {
      var serializeObject = JsonConvert.SerializeObject(_settings,Formatting.Indented);
      File.WriteAllText(AppSettingsFileName, serializeObject);
    }

    public static void ShowAppDialog() {
      using var dialog = new AppSettingsDialog();
      dialog.InvertMouseWheel = _settings.InvertMouseWheel;
      dialog.ShowFullPathInTitleBar = _settings.ShowFullPathInTitleBar;
      dialog.DefaultFontName = _settings.DefaultFontName;
      dialog.DefaultImageType = _settings.DefaultImageType;
      dialog.PortAdjustDetail = _settings.PortAdjustDetail;
      dialog.SaveToImage = _settings.SaveToImage;
      dialog.SaveToPDF = _settings.SaveToPDF;
      dialog.SaveTadsToAdv3Lite = _settings.SaveTadstoAdv3Lite;
      dialog.SaveAt100 = _settings.SaveAt100;
      dialog.SpecifyGenMargins = _settings.SpecifyGenMargins;
      dialog.GenHorizontalMargin = _settings.GenHorizontalMargin;
      dialog.GenVerticalMargin = _settings.GenVerticalMargin;
      dialog.LoadLastProjectOnStart = _settings.LoadLastProjectOnStart;
      dialog.ApplyStyleToNewRooms = _settings.ApplyStyleToNewRooms;
      dialog.DoubleClickToAddRoom = _settings.DoubleClickToAddRoom;
      dialog.ShowDescriptionsInTooltip = _settings.ShowDescriptionsInTooltips;
      dialog.ShowObjectsInTooltip = _settings.ShowObjectsInTooltips;
      dialog.LimitConnectionDescriptionCharactersInTooltip = _settings.LimitConnectionDescriptionCharactersInTooltip;
      dialog.ToolTipConnectionDescriptionCharactersToShow = _settings.ToolTipConnectionDescriptionCharactersToShow;
      dialog.LimitRoomDescriptionCharactersInTooltip = _settings.LimitRoomDescriptionCharactersInTooltip;
      dialog.ToolTipRoomDescriptionCharactersToShow = _settings.ToolTipRoomDescriptionCharactersToShow;

      if (UserInteraction.ShowDialog(dialog) == DialogResult.OK) {
        _settings.InvertMouseWheel = dialog.InvertMouseWheel;
        _settings.ShowFullPathInTitleBar = dialog.ShowFullPathInTitleBar;
        _settings.DefaultFontName = dialog.DefaultFontName;
        _settings.DefaultImageType = dialog.DefaultImageType;
        _settings.PortAdjustDetail = dialog.PortAdjustDetail;
        _settings.SaveAt100 = dialog.SaveAt100;
        _settings.SaveToImage = dialog.SaveToImage;
        _settings.SaveToPDF = dialog.SaveToPDF;
        _settings.SaveTadstoAdv3Lite = dialog.SaveTadsToAdv3Lite;
        _settings.SpecifyGenMargins = dialog.SpecifyGenMargins;
        _settings.GenHorizontalMargin = (int) dialog.GenHorizontalMargin;
        _settings.GenVerticalMargin = (int) dialog.GenVerticalMargin;
        _settings.LoadLastProjectOnStart = dialog.LoadLastProjectOnStart;
        _settings.ApplyStyleToNewRooms = dialog.ApplyStyleToNewRooms;
        _settings.DoubleClickToAddRoom = dialog.DoubleClickToAddRoom;
        _settings.ShowDescriptionsInTooltips = dialog.ShowDescriptionsInTooltip;
        _settings.ShowObjectsInTooltips = dialog.ShowObjectsInTooltip;
        _settings.ToolTipConnectionDescriptionCharactersToShow = dialog.ToolTipConnectionDescriptionCharactersToShow;
        _settings.LimitConnectionDescriptionCharactersInTooltip = dialog.LimitConnectionDescriptionCharactersInTooltip;
        _settings.ToolTipRoomDescriptionCharactersToShow = dialog.ToolTipRoomDescriptionCharactersToShow;
        _settings.LimitRoomDescriptionCharactersInTooltip = dialog.LimitRoomDescriptionCharactersInTooltip;
        SaveSettings();
      }
    }

    private static void LoadLegacyAppSettings() {
        try
        {
          if (File.Exists(_legacyAppSettingsPath))
          {
            var doc = new XmlDocument();
            doc.Load(_legacyAppSettingsPath);
            var root = new XmlElementReader(doc.DocumentElement);
            if (root.Name == "settings")
            {
              var versionText = root["dontCareAboutVersion"].Text;
              if (!string.IsNullOrEmpty(versionText))
              {
                _settings.DontCareAboutVersion = new Version(versionText);
              }
              _settings.InfiniteScrollBounds = root["infiniteScrollBounds"].ToBool(_settings.InfiniteScrollBounds);
              _settings.ShowMiniMap = root["showMiniMap"].ToBool(_settings.ShowMiniMap);

             _settings.LoadLastProjectOnStart = root["loadLastProjectOnStart"].ToBool(_settings.LoadLastProjectOnStart);
             _settings.LastProjectFileName = root["lastProjectFileName"].Text;
             _settings.LastExportImageFileName = root["lastExportedImageFileName"].Text;
             _settings.LastExportInform7FileName = root["lastExportedInform7FileName"].Text;
             _settings.LastExportInform6FileName = root["lastExportedInform6FileName"].Text;
             _settings.LastExportTadsFileName = root["lastExportedTadsFileName"].Text;
             _settings.LastExportHugoFileName = root["lastExportedHugoFileName"].Text;
             _settings.LastExportZilFileName = root["lastExportedZilFileName"].Text;
             _settings.LastExportQuestFileName = root["lastExportedQuestFileName"].Text;

              _settings.InvertMouseWheel = root["invertMouseWheel"].ToBool(_settings.InvertMouseWheel);
              _settings.PortAdjustDetail = root["portAdjustDetail"].ToInt(_settings.PortAdjustDetail);
              _settings.DefaultFontName = root["defaultFontName"].Text;

              if (_settings.DefaultFontName.Length == 0) _settings.DefaultFontName = "Arial"; // important for compatibility with 1.5.9.3 and before. Otherwise it's set to MS Sans Serif

              _settings.DefaultImageType = root["defaultImageType"].ToInt(_settings.DefaultImageType);
              _settings.SaveToImage = root["saveToImage"].ToBool(_settings.SaveToImage);
              _settings.SaveToPDF = root["saveToPDF"].ToBool(_settings.SaveToPDF);
              _settings.SaveTadstoAdv3Lite = root["saveTADSToADV3Lite"].ToBool(_settings.SaveTadstoAdv3Lite);
              _settings.SaveAt100 = root["saveAt100"].ToBool(_settings.SaveAt100);
              _settings.SpecifyGenMargins = root["specifyMargins"].ToBool(_settings.SpecifyGenMargins);
              _settings.GenHorizontalMargin = root["horizontalMargin"].ToInt(_settings.GenHorizontalMargin);
              _settings.GenVerticalMargin = root["verticalMargin"].ToInt(_settings.GenVerticalMargin);
              _settings.ShowObjectsInTooltips = root["showObjectsInTooltips"].ToBool(true);
              _settings.ShowDescriptionsInTooltips = root["showDescriptionsInTooltips"].ToBool(true);

              _settings.CanvasWidth = root["canvasWidth"].ToInt(_settings.CanvasWidth);
              _settings.CanvasHeight = root["canvasHeight"].ToInt(_settings.CanvasHeight);
              if (_settings.CanvasWidth == 0) { _settings.CanvasWidth = 624; }
              if (_settings.CanvasHeight == 0) { _settings.CanvasHeight = 450; }

              var recentProjects = root["recentProjects"];
              string fileName;
              var index = 0;
              do
              {
                fileName = recentProjects[$"fileName{index++}"].Text;
                if (!string.IsNullOrEmpty(fileName))
                {
                  _settings.RecentProjects.Add(fileName);
                }
              } while (!string.IsNullOrEmpty(fileName));

              var automap = root["automap"];
              var settingsAutomap = _settings.Automap;
              settingsAutomap.FileName = automap["transcriptFileName"].ToText(_settings.Automap.FileName);
              settingsAutomap.VerboseTranscript = automap["verboseTranscript"].ToBool(_settings.Automap.VerboseTranscript);
              settingsAutomap.AssumeRoomsWithSameNameAreSameRoom = automap["assumeRoomsWithSameNameAreSameRoom"].ToBool(_settings.Automap.AssumeRoomsWithSameNameAreSameRoom);
              settingsAutomap.GuessExits = automap["guessExits"].ToBool(_settings.Automap.GuessExits);
              settingsAutomap.AddObjectCommand = automap["addObjectCommand"].ToText(_settings.Automap.AddObjectCommand);
              settingsAutomap.AddRegionCommand = automap["addRegionCommand"].ToText(_settings.Automap.AddRegionCommand);
              _settings.Automap = settingsAutomap;
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

      if (AppSettings.RecentProjects.Count > RecentProjectsMaxCount) {
        AppSettings.RecentProjects.RemoveRange(RecentProjectsMaxCount, AppSettings.RecentProjects.Count - RecentProjectsMaxCount);
      }
      SaveSettings();
    }
  }
}