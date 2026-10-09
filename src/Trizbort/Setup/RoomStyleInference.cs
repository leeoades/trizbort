using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Region = Trizbort.Domain.Misc.Region;
using MapColors = Trizbort.Domain.Misc.Colors;

namespace Trizbort.Setup {
  /// <summary>
  /// Finds room styling shared by a strong majority of rooms, promotes it to the map defaults
  /// and removes the per-room overrides that become redundant.
  /// </summary>
  public sealed class RoomStyleInference {
    public const double DEFAULT_THRESHOLD = 0.75;
    public const int MINIMUM_ROOMS = 3;

    private readonly Project _mProject;
    private readonly Dictionary<int, Color> _mColorUpdates = new Dictionary<int, Color>();
    private readonly Dictionary<Region, Color> _mFillUpdates = new Dictionary<Region, Color>();
    private readonly Dictionary<Region, Color> _mTextUpdates = new Dictionary<Region, Color>();
    private readonly List<string> _mChanges = new List<string>();

    private RoomStyleInference(Project project) {
      _mProject = project;
    }

    public RoomShape? InferredShape { get; private set; }
    public IReadOnlyList<string> Changes => _mChanges;
    public int RedundantOverrides { get; private set; }
    public int RoomsSimplified { get; private set; }
    public bool HasChanges => InferredShape.HasValue || _mColorUpdates.Count > 0 || _mFillUpdates.Count > 0 || _mTextUpdates.Count > 0 || RedundantOverrides > 0;

    public static RoomStyleInference Analyze(Project project, double threshold = DEFAULT_THRESHOLD) {
      var inference = new RoomStyleInference(project);
      var rooms = project.Elements.OfType<Room>().ToList();

      var shape = majority(rooms, room => room.Shape, threshold);
      if (shape.HasValue && shape.Value != Settings.DefaultRoomShape) {
        inference.InferredShape = shape.Value;
        inference._mChanges.Add($"Default room shape: {Settings.DefaultRoomShape} -> {shape.Value}");
      }

      foreach (var group in rooms.GroupBy(regionOf)) {
        var region = group.Key;
        var label = region.RegionName == Region.DefaultRegion ? "rooms with no region" : $"region '{region.RegionName}'";
        inference.inferColor(group.ToList(), room => room.RoomFillColor, region.RColor, threshold, color => inference._mFillUpdates[region] = color, $"Fill colour for {label}");
        inference.inferColor(group.ToList(), room => room.RoomNameColor, region.TextColor, threshold, color => inference._mTextUpdates[region] = color, $"Name text colour for {label}");
      }

      inference.inferGlobal(rooms, room => room.RoomBorderColor, MapColors.Border, threshold, "Room border colour");
      inference.inferGlobal(rooms, room => room.RoomSubtitleColor, MapColors.Subtitle, threshold, "Room subtitle colour");
      inference.inferGlobal(rooms, room => room.RoomObjectTextColor, MapColors.SmallText, threshold, "Room object text colour");

      inference.countRedundantOverrides(rooms);
      return inference;
    }

    public void Apply() {
      if (!HasChanges) return;
      if (InferredShape.HasValue) Settings.DefaultRoomShape = InferredShape.Value;
      foreach (var pair in _mColorUpdates) Settings.Color[pair.Key] = pair.Value;
      foreach (var pair in _mFillUpdates) pair.Key.RColor = pair.Value;
      foreach (var pair in _mTextUpdates) pair.Key.TextColor = pair.Value;

      foreach (var room in _mProject.Elements.OfType<Room>()) {
        var region = regionOf(room);
        if (sameColor(room.RoomFillColor, region.RColor)) room.RoomFillColor = Color.Transparent;
        if (sameColor(room.RoomNameColor, region.TextColor)) room.RoomNameColor = Color.Transparent;
        if (sameColor(room.RoomBorderColor, Settings.Color[MapColors.Border])) room.RoomBorderColor = Color.Transparent;
        if (sameColor(room.RoomSubtitleColor, Settings.Color[MapColors.Subtitle])) room.RoomSubtitleColor = Color.Transparent;
        if (sameColor(room.RoomObjectTextColor, Settings.Color[MapColors.SmallText])) room.RoomObjectTextColor = Color.Transparent;
      }

      _mProject.IsDirty = true;
      Settings.NotifyThemeApplied();
    }

    private void inferColor(List<Room> rooms, Func<Room, Color> overrideOf, Color current, double threshold, Action<Color> update, string description) {
      var argb = majority(rooms, room => effective(overrideOf(room), current).ToArgb(), threshold);
      if (!argb.HasValue || argb.Value == current.ToArgb()) return;
      var color = Color.FromArgb(argb.Value);
      update(color);
      _mChanges.Add($"{description}: {ColorTranslator.ToHtml(current)} -> {ColorTranslator.ToHtml(color)}");
    }

    private void inferGlobal(List<Room> rooms, Func<Room, Color> overrideOf, int index, double threshold, string description) {
      inferColor(rooms, overrideOf, Settings.Color[index], threshold, color => _mColorUpdates[index] = color, description);
    }

    private void countRedundantOverrides(IEnumerable<Room> rooms) {
      var border = _mColorUpdates.TryGetValue(MapColors.Border, out var b) ? b : Settings.Color[MapColors.Border];
      var subtitle = _mColorUpdates.TryGetValue(MapColors.Subtitle, out var s) ? s : Settings.Color[MapColors.Subtitle];
      var smallText = _mColorUpdates.TryGetValue(MapColors.SmallText, out var t) ? t : Settings.Color[MapColors.SmallText];
      foreach (var room in rooms) {
        var region = regionOf(room);
        var fill = _mFillUpdates.TryGetValue(region, out var f) ? f : region.RColor;
        var text = _mTextUpdates.TryGetValue(region, out var n) ? n : region.TextColor;
        var count = new[] {
          sameColor(room.RoomFillColor, fill), sameColor(room.RoomNameColor, text), sameColor(room.RoomBorderColor, border),
          sameColor(room.RoomSubtitleColor, subtitle), sameColor(room.RoomObjectTextColor, smallText)
        }.Count(redundant => redundant);
        RedundantOverrides += count;
        if (count > 0) RoomsSimplified++;
      }
    }

    private static T? majority<T>(IReadOnlyCollection<Room> rooms, Func<Room, T> selector, double threshold) where T : struct {
      if (rooms.Count < MINIMUM_ROOMS) return null;
      var best = rooms.GroupBy(selector).OrderByDescending(group => group.Count()).First();
      return best.Count() >= Math.Ceiling(rooms.Count * threshold) ? best.Key : (T?) null;
    }

    private static Region regionOf(Room room) {
      return Settings.Regions.FirstOrDefault(p => p.RegionName.Equals(room.Region ?? string.Empty, StringComparison.OrdinalIgnoreCase)) ??
             Settings.Regions.First(p => p.RegionName.Equals(Region.DefaultRegion, StringComparison.OrdinalIgnoreCase));
    }

    private static Color effective(Color roomOverride, Color fallback) {
      return roomOverride != Color.Transparent ? roomOverride : fallback;
    }

    private static bool sameColor(Color roomOverride, Color fallback) {
      return roomOverride != Color.Transparent && roomOverride.ToArgb() == fallback.ToArgb();
    }
  }
}
