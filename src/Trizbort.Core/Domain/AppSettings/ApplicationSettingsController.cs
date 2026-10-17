using System;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Trizbort.Automap;
using Trizbort.Util;
using Formatting = Newtonsoft.Json.Formatting;

namespace Trizbort.Domain.AppSettings;

public static class ApplicationSettingsController
{
  public const int RecentProjectsMaxCount = 4;
  private const string AppSettingsFileName = @".\appsettings.json";

  private static readonly string _legacyAppSettingsPath = Path.Combine(
    Path.Combine(
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Genstein"),
      "Trizbort"),
    "Settings.xml");

  static ApplicationSettingsController()
  {
    AppSettings = new ApplicationSettings();
    LoadSettings();
  }

  public static ApplicationSettings AppSettings { get; private set; }

  public static void LoadSettings()
  {
    // if app settings don't exist, create a default one
    if (!File.Exists(AppSettingsFileName))
    {
      if (File.Exists(_legacyAppSettingsPath))
        LoadLegacyAppSettings();
      else
        ResetSettings();
      SaveSettings();
    }
    else
    {
      AppSettings = JsonConvert.DeserializeObject<ApplicationSettings>(File.ReadAllText(AppSettingsFileName));
    }
  }

  public static void ResetSettings()
  {
    AppSettings.DontCareAboutVersion = new Version(0, 0, 0, 0);
    AppSettings.Automap = AutomapSettings.Default;
    AppSettings.InfiniteScrollBounds = false;
    AppSettings.ShowMiniMap = true;
    AppSettings.SaveAt100 = true;
    AppSettings.SaveToImage = true;
    AppSettings.SaveToPDF = true;
    AppSettings.SaveTadstoAdv3Lite = true;
    AppSettings.RecentProjects.Clear();
    AppSettings.ShowObjectsInTooltips = true;
    AppSettings.ShowDescriptionsInTooltips = true;
    AppSettings.ApplyStyleToNewRooms = false;
    AppSettings.DoubleClickToAddRoom = false;
  }

  public static void SaveSettings()
  {
    var serializeObject = JsonConvert.SerializeObject(AppSettings, Formatting.Indented);
    File.WriteAllText(AppSettingsFileName, serializeObject);
  }

  private static void LoadLegacyAppSettings()
  {
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
          if (!string.IsNullOrEmpty(versionText)) AppSettings.DontCareAboutVersion = new Version(versionText);
          AppSettings.InfiniteScrollBounds = root["infiniteScrollBounds"].ToBool(AppSettings.InfiniteScrollBounds);
          AppSettings.ShowMiniMap = root["showMiniMap"].ToBool(AppSettings.ShowMiniMap);

          AppSettings.LoadLastProjectOnStart =
            root["loadLastProjectOnStart"].ToBool(AppSettings.LoadLastProjectOnStart);
          AppSettings.LastProjectFileName = root["lastProjectFileName"].Text;
          AppSettings.LastExportImageFileName = root["lastExportedImageFileName"].Text;
          AppSettings.LastExportInform7FileName = root["lastExportedInform7FileName"].Text;
          AppSettings.LastExportInform6FileName = root["lastExportedInform6FileName"].Text;
          AppSettings.LastExportTadsFileName = root["lastExportedTadsFileName"].Text;
          AppSettings.LastExportHugoFileName = root["lastExportedHugoFileName"].Text;
          AppSettings.LastExportZilFileName = root["lastExportedZilFileName"].Text;
          AppSettings.LastExportQuestFileName = root["lastExportedQuestFileName"].Text;

          AppSettings.InvertMouseWheel = root["invertMouseWheel"].ToBool(AppSettings.InvertMouseWheel);
          AppSettings.PortAdjustDetail = root["portAdjustDetail"].ToInt(AppSettings.PortAdjustDetail);
          AppSettings.DefaultFontName = root["defaultFontName"].Text;

          if (AppSettings.DefaultFontName.Length == 0)
            AppSettings.DefaultFontName =
              "Arial"; // important for compatibility with 1.5.9.3 and before. Otherwise it's set to MS Sans Serif

          AppSettings.DefaultImageType = root["defaultImageType"].ToInt(AppSettings.DefaultImageType);
          AppSettings.SaveToImage = root["saveToImage"].ToBool(AppSettings.SaveToImage);
          AppSettings.SaveToPDF = root["saveToPDF"].ToBool(AppSettings.SaveToPDF);
          AppSettings.SaveTadstoAdv3Lite = root["saveTADSToADV3Lite"].ToBool(AppSettings.SaveTadstoAdv3Lite);
          AppSettings.SaveAt100 = root["saveAt100"].ToBool(AppSettings.SaveAt100);
          AppSettings.SpecifyGenMargins = root["specifyMargins"].ToBool(AppSettings.SpecifyGenMargins);
          AppSettings.GenHorizontalMargin = root["horizontalMargin"].ToInt(AppSettings.GenHorizontalMargin);
          AppSettings.GenVerticalMargin = root["verticalMargin"].ToInt(AppSettings.GenVerticalMargin);
          AppSettings.ShowObjectsInTooltips = root["showObjectsInTooltips"].ToBool(true);
          AppSettings.ShowDescriptionsInTooltips = root["showDescriptionsInTooltips"].ToBool(true);

          AppSettings.CanvasWidth = root["canvasWidth"].ToInt(AppSettings.CanvasWidth);
          AppSettings.CanvasHeight = root["canvasHeight"].ToInt(AppSettings.CanvasHeight);
          if (AppSettings.CanvasWidth == 0) AppSettings.CanvasWidth = 624;
          if (AppSettings.CanvasHeight == 0) AppSettings.CanvasHeight = 450;

          var recentProjects = root["recentProjects"];
          string fileName;
          var index = 0;
          do
          {
            fileName = recentProjects[$"fileName{index++}"].Text;
            if (!string.IsNullOrEmpty(fileName)) AppSettings.RecentProjects.Add(fileName);
          } while (!string.IsNullOrEmpty(fileName));

          var automap = root["automap"];
          var settingsAutomap = AppSettings.Automap;
          settingsAutomap.FileName = automap["transcriptFileName"].ToText(AppSettings.Automap.FileName);
          settingsAutomap.VerboseTranscript =
            automap["verboseTranscript"].ToBool(AppSettings.Automap.VerboseTranscript);
          settingsAutomap.AssumeRoomsWithSameNameAreSameRoom = automap["assumeRoomsWithSameNameAreSameRoom"]
            .ToBool(AppSettings.Automap.AssumeRoomsWithSameNameAreSameRoom);
          settingsAutomap.GuessExits = automap["guessExits"].ToBool(AppSettings.Automap.GuessExits);
          settingsAutomap.AddObjectCommand = automap["addObjectCommand"].ToText(AppSettings.Automap.AddObjectCommand);
          settingsAutomap.AddRegionCommand = automap["addRegionCommand"].ToText(AppSettings.Automap.AddRegionCommand);
          AppSettings.Automap = settingsAutomap;
        }
      }
    }
    catch (Exception)
    {
      // ignored
    }
  }

  public static void OpenProject(string fileName)
  {
    AppSettings.LastProjectFileName = fileName;
    if (AppSettings.RecentProjects.Contains(fileName)) AppSettings.RecentProjects.Remove(fileName);
    AppSettings.RecentProjects.Insert(0, fileName);

    if (AppSettings.RecentProjects.Count > RecentProjectsMaxCount)
      AppSettings.RecentProjects.RemoveRange(
        RecentProjectsMaxCount,
        AppSettings.RecentProjects.Count - RecentProjectsMaxCount);
    SaveSettings();
  }
}