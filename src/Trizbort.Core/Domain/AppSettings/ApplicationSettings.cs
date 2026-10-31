using System;
using System.Collections.Generic;
using Trizbort.Automap;

namespace Trizbort.Domain.AppSettings;

public class ApplicationSettings
{
  private const string AppSettingsFileName = @".\appsettings.json";

  public bool ApplyStyleToNewRooms { get; set; }
  public bool DoubleClickToAddRoom { get; set; }
  public AutomapSettings Automap { get; set; } = AutomapSettings.Default;
  public int CanvasHeight { get; set; }
  public int CanvasWidth { get; set; }
  public WindowGeometry WindowState { get; set; }
  public bool DebugDisableElementRendering { get; set; }
  public bool DebugDisableGridPolyline { get; set; }
  public bool DebugDisableLineRendering { get; set; }
  public bool DebugDisableTextRendering { get; set; }
  [Newtonsoft.Json.JsonProperty("DebugShowFPS")]
  public bool DebugShowFps { get; set; }
  public bool DebugShowMouseCoordinates { get; set; }
  public string DefaultFontName { get; set; } = "Arial";
  public int DefaultImageType { get; set; }
  public Version DontCareAboutVersion { get; set; }
  public int GenHorizontalMargin { get; set; }
  public int GenVerticalMargin { get; set; }
  public bool InfiniteScrollBounds { get; set; }
  public bool InvertMouseWheel { get; set; }
  public string LastExportAdventuronFileName { get; set; }
  public string LastExportAlanFileName { get; set; }
  public string LastExportHugoFileName { get; set; }
  public string LastExportImageFileName { get; set; }
  public string LastExportInform6FileName { get; set; }
  public string LastExportPdfFileName { get; set; }
  public string LastExportInform7FileName { get; set; }
  public string LastExportTadsFileName { get; set; }
  public string LastExportZilFileName { get; set; }
  public string LastExportQuestFileName { get; set; }
  public string LastExportQuestRoomsFileName { get; set; }
  public string LastProjectFileName { get; set; }
  public bool LoadLastProjectOnStart { get; set; }
  public int MouseDragButton { get; set; }
  public int MapSettingsLastTab { get; set; }
  public int RoomDialogLastTab { get; set; }
  public int PortAdjustDetail { get; set; }
  public List<string> RecentProjects { get; } = new();
  public bool SaveAt100 { get; set; }

  /// <summary>
  ///   Scale chosen in the image export options prompt: 0 = 100%, 1 = current zoom, 2 = custom percent.
  ///   -1 means not chosen yet (the prompt then starts from <see cref="SaveAt100" />).
  /// </summary>
  public int ExportImageScaleMode { get; set; } = -1;

  /// <summary>Custom scale percent chosen in the image export options prompt.</summary>
  public int ExportImageCustomScalePercent { get; set; } = 100;

  /// <summary>Whether the image export options prompt last had "Selection only" ticked.</summary>
  public bool ExportImageSelectionOnly { get; set; }

  public bool SaveTadstoAdv3Lite { get; set; }
  public bool SaveToImage { get; set; }
  public bool SaveToPDF { get; set; }
  public bool ShowMiniMap { get; set; }
  public bool ShowObjectsInTooltips { get; set; } = true;
  public bool ShowDescriptionsInTooltips { get; set; } = true;
  public bool LimitRoomDescriptionCharactersInTooltip { get; set; } = false;
  public int ToolTipRoomDescriptionCharactersToShow { get; set; } = 50;
  public bool LimitConnectionDescriptionCharactersInTooltip { get; set; } = false;
  public int ToolTipConnectionDescriptionCharactersToShow { get; set; } = 50;
  public bool SpecifyGenMargins { get; set; }
  public bool SpecifyWrapping { get; set; }
  public bool ShowFullPathInTitleBar { get; set; }

  public bool ShowTooltips => ShowDescriptionsInTooltips || ShowObjectsInTooltips;
}