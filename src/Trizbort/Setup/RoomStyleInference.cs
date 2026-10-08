using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Region = Trizbort.Domain.Misc.Region;
using MapColors = Trizbort.Domain.Misc.Colors;

namespace Trizbort.Setup {
  /// <summary>
  /// Finds room styling shared by a strong majority of rooms, promotes it to the map defaults
  /// and removes the per-room overrides that become redundant.
  /// </summary>
  public sealed class RoomStyleInference {
    public const double DefaultThreshold = 0.75;
    public const int MinimumRooms = 3;

    private readonly Project mProject;
    private readonly Dictionary<int, Color> mColorUpdates = new Dictionary<int, Color>();
    private readonly Dictionary<Region, Color> mFillUpdates = new Dictionary<Region, Color>();
    private readonly Dictionary<Region, Color> mTextUpdates = new Dictionary<Region, Color>();
    private readonly List<string> mChanges = new List<string>();

    private RoomStyleInference(Project project) {
      mProject = project;
    }

    public RoomShape? InferredShape { get; private set; }
    public IReadOnlyList<string> Changes => mChanges;
    public int RedundantOverrides { get; private set; }
    public int RoomsSimplified { get; private set; }
    public bool HasChanges => InferredShape.HasValue || mColorUpdates.Count > 0 || mFillUpdates.Count > 0 || mTextUpdates.Count > 0 || RedundantOverrides > 0;

    public static RoomStyleInference Analyze(Project project, double threshold = DefaultThreshold) {
      var inference = new RoomStyleInference(project);
      var rooms = project.Elements.OfType<Room>().ToList();

      var shape = Majority(rooms, room => room.Shape, threshold);
      if (shape.HasValue && shape.Value != Settings.DefaultRoomShape) {
        inference.InferredShape = shape.Value;
        inference.mChanges.Add($"Default room shape: {Settings.DefaultRoomShape} -> {shape.Value}");
      }

      foreach (var group in rooms.GroupBy(RegionOf)) {
        var region = group.Key;
        var label = region.RegionName == Region.DefaultRegion ? "rooms with no region" : $"region '{region.RegionName}'";
        inference.InferColor(group.ToList(), room => room.RoomFillColor, region.RColor, threshold, color => inference.mFillUpdates[region] = color, $"Fill colour for {label}");
        inference.InferColor(group.ToList(), room => room.RoomNameColor, region.TextColor, threshold, color => inference.mTextUpdates[region] = color, $"Name text colour for {label}");
      }

      inference.InferGlobal(rooms, room => room.RoomBorderColor, MapColors.Border, threshold, "Room border colour");
      inference.InferGlobal(rooms, room => room.RoomSubtitleColor, MapColors.Subtitle, threshold, "Room subtitle colour");
      inference.InferGlobal(rooms, room => room.RoomObjectTextColor, MapColors.SmallText, threshold, "Room object text colour");

      inference.CountRedundantOverrides(rooms);
      return inference;
    }

    public void Apply() {
      if (!HasChanges) return;
      if (InferredShape.HasValue) Settings.DefaultRoomShape = InferredShape.Value;
      foreach (var pair in mColorUpdates) Settings.Color[pair.Key] = pair.Value;
      foreach (var pair in mFillUpdates) pair.Key.RColor = pair.Value;
      foreach (var pair in mTextUpdates) pair.Key.TextColor = pair.Value;

      foreach (var room in mProject.Elements.OfType<Room>()) {
        var region = RegionOf(room);
        if (SameColor(room.RoomFillColor, region.RColor)) room.RoomFillColor = Color.Transparent;
        if (SameColor(room.RoomNameColor, region.TextColor)) room.RoomNameColor = Color.Transparent;
        if (SameColor(room.RoomBorderColor, Settings.Color[MapColors.Border])) room.RoomBorderColor = Color.Transparent;
        if (SameColor(room.RoomSubtitleColor, Settings.Color[MapColors.Subtitle])) room.RoomSubtitleColor = Color.Transparent;
        if (SameColor(room.RoomObjectTextColor, Settings.Color[MapColors.SmallText])) room.RoomObjectTextColor = Color.Transparent;
      }

      mProject.IsDirty = true;
      Settings.NotifyThemeApplied();
    }

    private void InferColor(List<Room> rooms, Func<Room, Color> overrideOf, Color current, double threshold, Action<Color> update, string description) {
      var argb = Majority(rooms, room => Effective(overrideOf(room), current).ToArgb(), threshold);
      if (!argb.HasValue || argb.Value == current.ToArgb()) return;
      var color = Color.FromArgb(argb.Value);
      update(color);
      mChanges.Add($"{description}: {ColorTranslator.ToHtml(current)} -> {ColorTranslator.ToHtml(color)}");
    }

    private void InferGlobal(List<Room> rooms, Func<Room, Color> overrideOf, int index, double threshold, string description) {
      InferColor(rooms, overrideOf, Settings.Color[index], threshold, color => mColorUpdates[index] = color, description);
    }

    private void CountRedundantOverrides(IEnumerable<Room> rooms) {
      var border = mColorUpdates.TryGetValue(MapColors.Border, out var b) ? b : Settings.Color[MapColors.Border];
      var subtitle = mColorUpdates.TryGetValue(MapColors.Subtitle, out var s) ? s : Settings.Color[MapColors.Subtitle];
      var smallText = mColorUpdates.TryGetValue(MapColors.SmallText, out var t) ? t : Settings.Color[MapColors.SmallText];
      foreach (var room in rooms) {
        var region = RegionOf(room);
        var fill = mFillUpdates.TryGetValue(region, out var f) ? f : region.RColor;
        var text = mTextUpdates.TryGetValue(region, out var n) ? n : region.TextColor;
        var count = new[] {
          SameColor(room.RoomFillColor, fill), SameColor(room.RoomNameColor, text), SameColor(room.RoomBorderColor, border),
          SameColor(room.RoomSubtitleColor, subtitle), SameColor(room.RoomObjectTextColor, smallText)
        }.Count(redundant => redundant);
        RedundantOverrides += count;
        if (count > 0) RoomsSimplified++;
      }
    }

    private static T? Majority<T>(IReadOnlyCollection<Room> rooms, Func<Room, T> selector, double threshold) where T : struct {
      if (rooms.Count < MinimumRooms) return null;
      var best = rooms.GroupBy(selector).OrderByDescending(group => group.Count()).First();
      return best.Count() >= Math.Ceiling(rooms.Count * threshold) ? best.Key : (T?) null;
    }

    private static Region RegionOf(Room room) {
      return Settings.Regions.FirstOrDefault(p => p.RegionName.Equals(room.Region ?? string.Empty, StringComparison.OrdinalIgnoreCase)) ??
             Settings.Regions.First(p => p.RegionName.Equals(Region.DefaultRegion, StringComparison.OrdinalIgnoreCase));
    }

    private static Color Effective(Color roomOverride, Color fallback) {
      return roomOverride != Color.Transparent ? roomOverride : fallback;
    }

    private static bool SameColor(Color roomOverride, Color fallback) {
      return roomOverride != Color.Transparent && roomOverride.ToArgb() == fallback.ToArgb();
    }
  }
}
