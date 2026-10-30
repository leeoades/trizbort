using Trizbort.Domain.AppSettings;

namespace Trizbort.Editor;

// Debug toggles, available only when the app was started with --debug (tickets 055, 108).
public sealed partial class EditorSession
{
  private bool _debugEnabled;

  /// <summary>
  ///   True when the app was started with <c>--debug</c>. The debug commands do nothing and the debug settings
  ///   have no effect on drawing while this is false.
  /// </summary>
  public bool DebugEnabled
  {
    get => _debugEnabled;
    set
    {
      if (_debugEnabled == value) return;
      _debugEnabled = value;
      RaiseChanged(EditorChange.Viewport);
    }
  }

  /// <summary>Draw the frames-per-second counter.</summary>
  public bool ShowFps => _debugEnabled && ApplicationSettingsController.AppSettings.DebugShowFps;

  /// <summary>Draw the pointer coordinates readout.</summary>
  public bool ShowMouseCoordinates => _debugEnabled && ApplicationSettingsController.AppSettings.DebugShowMouseCoordinates;

  /// <summary>False only while debug is on and element (room and label) rendering is switched off.</summary>
  public bool ElementRenderingEnabled => !_debugEnabled || !ApplicationSettingsController.AppSettings.DebugDisableElementRendering;

  /// <summary>False only while debug is on and connection rendering is switched off.</summary>
  public bool LineRenderingEnabled => !_debugEnabled || !ApplicationSettingsController.AppSettings.DebugDisableLineRendering;

  /// <summary>False only while debug is on and the grid polyline is switched off.</summary>
  public bool GridPolylineEnabled => !_debugEnabled || !ApplicationSettingsController.AppSettings.DebugDisableGridPolyline;

  public void ToggleDebugFps()
  {
    if (!_debugEnabled) return;
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugShowFps = !settings.DebugShowFps;
    RaiseChanged(EditorChange.Viewport);
  }

  public void ToggleDebugMouseCoordinates()
  {
    if (!_debugEnabled) return;
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugShowMouseCoordinates = !settings.DebugShowMouseCoordinates;
    RaiseChanged(EditorChange.Viewport);
  }

  public void ToggleDebugElementRendering()
  {
    if (!_debugEnabled) return;
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugDisableElementRendering = !settings.DebugDisableElementRendering;
    RaiseChanged(EditorChange.Viewport);
  }

  public void ToggleDebugLineRendering()
  {
    if (!_debugEnabled) return;
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugDisableLineRendering = !settings.DebugDisableLineRendering;
    RaiseChanged(EditorChange.Viewport);
  }

  public void ToggleDebugGridPolyline()
  {
    if (!_debugEnabled) return;
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugDisableGridPolyline = !settings.DebugDisableGridPolyline;
    RaiseChanged(EditorChange.Viewport);
  }
}
