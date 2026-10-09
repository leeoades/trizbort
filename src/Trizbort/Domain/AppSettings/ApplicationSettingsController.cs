using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using Trizbort.Automap;
using Trizbort.UI;
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

  public static void ShowAppDialog()
  {
    using var dialog = new AppSettingsDialog();
    dialog.InvertMouseWheel = AppSettings.InvertMouseWheel;
    dialog.ShowFullPathInTitleBar = AppSettings.ShowFullPathInTitleBar;
    dialog.DefaultFontName = AppSettings.DefaultFontName;
    dialog.DefaultImageType = AppSettings.DefaultImageType;
    dialog.PortAdjustDetail = AppSettings.PortAdjustDetail;
    dialog.SaveToImage = AppSettings.SaveToImage;
    dialog.SaveToPDF = AppSettings.SaveToPDF;
    dialog.SaveTadsToAdv3Lite = AppSettings.SaveTadstoAdv3Lite;
    dialog.SaveAt100 = AppSettings.SaveAt100;
    dialog.SpecifyGenMargins = AppSettings.SpecifyGenMargins;
    dialog.GenHorizontalMargin = AppSettings.GenHorizontalMargin;
    dialog.GenVerticalMargin = AppSettings.GenVerticalMargin;
    dialog.LoadLastProjectOnStart = AppSettings.LoadLastProjectOnStart;
    dialog.ApplyStyleToNewRooms = AppSettings.ApplyStyleToNewRooms;
    dialog.DoubleClickToAddRoom = AppSettings.DoubleClickToAddRoom;
    dialog.ShowDescriptionsInTooltip = AppSettings.ShowDescriptionsInTooltips;
    dialog.ShowObjectsInTooltip = AppSettings.ShowObjectsInTooltips;
    dialog.LimitConnectionDescriptionCharactersInTooltip = AppSettings.LimitConnectionDescriptionCharactersInTooltip;
    dialog.ToolTipConnectionDescriptionCharactersToShow = AppSettings.ToolTipConnectionDescriptionCharactersToShow;
    dialog.LimitRoomDescriptionCharactersInTooltip = AppSettings.LimitRoomDescriptionCharactersInTooltip;
    dialog.ToolTipRoomDescriptionCharactersToShow = AppSettings.ToolTipRoomDescriptionCharactersToShow;

    if (UserInteraction.ShowDialog(dialog) == DialogResult.OK)
    {
      AppSettings.InvertMouseWheel = dialog.InvertMouseWheel;
      AppSettings.ShowFullPathInTitleBar = dialog.ShowFullPathInTitleBar;
      AppSettings.DefaultFontName = dialog.DefaultFontName;
      AppSettings.DefaultImageType = dialog.DefaultImageType;
      AppSettings.PortAdjustDetail = dialog.PortAdjustDetail;
      AppSettings.SaveAt100 = dialog.SaveAt100;
      AppSettings.SaveToImage = dialog.SaveToImage;
      AppSettings.SaveToPDF = dialog.SaveToPDF;
      AppSettings.SaveTadstoAdv3Lite = dialog.SaveTadsToAdv3Lite;
      AppSettings.SpecifyGenMargins = dialog.SpecifyGenMargins;
      AppSettings.GenHorizontalMargin = (int)dialog.GenHorizontalMargin;
      AppSettings.GenVerticalMargin = (int)dialog.GenVerticalMargin;
      AppSettings.LoadLastProjectOnStart = dialog.LoadLastProjectOnStart;
      AppSettings.ApplyStyleToNewRooms = dialog.ApplyStyleToNewRooms;
      AppSettings.DoubleClickToAddRoom = dialog.DoubleClickToAddRoom;
      AppSettings.ShowDescriptionsInTooltips = dialog.ShowDescriptionsInTooltip;
      AppSettings.ShowObjectsInTooltips = dialog.ShowObjectsInTooltip;
      AppSettings.ToolTipConnectionDescriptionCharactersToShow = dialog.ToolTipConnectionDescriptionCharactersToShow;
      AppSettings.LimitConnectionDescriptionCharactersInTooltip = dialog.LimitConnectionDescriptionCharactersInTooltip;
      AppSettings.ToolTipRoomDescriptionCharactersToShow = dialog.ToolTipRoomDescriptionCharactersToShow;
      AppSettings.LimitRoomDescriptionCharactersInTooltip = dialog.LimitRoomDescriptionCharactersInTooltip;
      SaveSettings();
    }
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