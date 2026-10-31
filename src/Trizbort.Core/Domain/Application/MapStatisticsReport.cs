using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Trizbort.Domain.Elements;
using Trizbort.Setup;

namespace Trizbort.Domain.Application;

/// <summary>Map dimensions (with and without export margins) supplied by the UI, which owns the bounds code.</summary>
public readonly record struct MapDimensions(
  float WithMarginsWidth,
  float WithMarginsHeight,
  float WithoutMarginsWidth,
  float WithoutMarginsHeight);

/// <summary>Builds the Map Statistics report text (ticket 087). Reads <see cref="Project.Current" />.</summary>
public static class MapStatisticsReport
{
  private const string NoRooms = "No rooms to take stats of.";

  public static string Build(MapDimensions dimensions)
  {
    if (MapStatistics.NumberOfRooms == 0) return NoRooms;

    var sb = new StringBuilder();
    void Line(string text = "") => sb.Append(text).Append('\n');

    Line($"# of Rooms: {MapStatistics.NumberOfRooms}");
    Line($"# of Dark Rooms: {MapStatistics.NumberOfDarkRooms}");
    Line($"# of Unconnected Rooms: {MapStatistics.NumberOfFloatingRooms}");
    Line();

    if (MapStatistics.NumberOfStartRooms == 1) Line($"Start room = {MapStatistics.StartRoomName}");
    else if (MapStatistics.NumberOfStartRooms > 1) Line("More than one start room.");
    else Line("No start room.");

    if (MapStatistics.NumberOfEndRooms == 1) Line($"End room = {MapStatistics.EndRoomName}");
    else if (MapStatistics.NumberOfEndRooms > 1) Line($"End rooms = ({MapStatistics.EndRoomName})");
    else Line("No end room.");

    Line();
    Line($"Dimensions with margins: height {Num(dimensions.WithMarginsHeight)}, width {Num(dimensions.WithMarginsWidth)}");
    Line($"Dimensions without margins: height {Num(dimensions.WithoutMarginsHeight)}, width {Num(dimensions.WithoutMarginsWidth)}");

    Line();
    Line($"# of Connections: {MapStatistics.NumberOfConnections} total, {MapStatistics.UnlabeledConnections} unlabeled, " +
         $"{MapStatistics.NumberOfOneWayConnections} one-way, {MapStatistics.NumberOfDottedConnections} dashed/dotted, " +
         $"{MapStatistics.UpDown} up/down, {MapStatistics.InOut} in/out.");
    Line($"# of Dangling Connections: {MapStatistics.NumberOfDanglingConnections}");
    Line($"# of Self Looping Connections: {MapStatistics.NumberOfLoopingConnections}");
    Line($"# of Dead Ends: {MapStatistics.NumberOfDeadEnds}");

    Line();
    Line($"{MapStatistics.NumberOfDoors} door{Plural(MapStatistics.NumberOfDoors)}, {MapStatistics.NumberOfLockedDoors} locked, " +
         $"{MapStatistics.NumberOfLockableDoors} lockable, {MapStatistics.NumberOfOpenDoors} open, {MapStatistics.NumberOfOpenableDoors} openable");

    var regions = Settings.Regions.OrderBy(p => p.RegionName)
                          .Where(p => p.RegionName != Misc.Region.DefaultRegion).ToList();

    Line();
    Line($"# of Regions: {MapStatistics.NumberOfRegions}");
    Line($"# of Rooms without a region: {MapStatistics.NumberOfRoomsWithoutRegion()}");
    if (MapStatistics.NumberOfRegions > 0)
    {
      Line("Regions:");
      foreach (var region in regions)
      {
        var count = MapStatistics.NumberOfRoomsInRegion(region.RegionName);
        Line($"{region.RegionName} ({count} {(count == 1 ? "room" : "rooms")})");
      }
    }

    Line();
    Line($"Total # Objects in All Rooms: {MapStatistics.NumberOfTotalObjectsInRooms}");
    Line($"Total # Rooms with Objects: {MapStatistics.NumberOfRoomsWithObjects}, {MapStatistics.NumberOfRoomsWithXObjects(1, false)} with 1, " +
         $"{MapStatistics.NumberOfRoomsWithXObjects(2, false)} with 2, {MapStatistics.NumberOfRoomsWithXObjects(3, true)} with 3+");

    Line();
    Line($"Total # of Rooms with subtitles: {MapStatistics.NumberOfRoomsWithSubtitles}");
    Line($"Total # of Rooms with descriptions: {MapStatistics.NumberOfDescribedRooms}");

    Line();
    Line($"{new string('=', 30)}Odd stuff below here");

    Line();
    Line($"Duplicate rooms: {MapStatistics.DuplicateNamedRooms}.");

    Line();
    Line($"Room shapes: {MapStatistics.NumberOfRectangularRooms} rectangular, {MapStatistics.NumberOfEllipticalRooms} elliptical, " +
         $"{MapStatistics.NumberOfRoundCornerRooms} round cornered, {MapStatistics.NumberOfOctagonalRooms} octagonal.");

    Line();
    if (MapStatistics.NumberOfRegions > 0)
    {
      foreach (var region in regions)
      {
        var count = MapStatistics.NumberOfRoomsInRegion(region.RegionName);
        var names = string.Join(
          ", ",
          Project.Current.Elements.OfType<Room>().OrderBy(p => p.Name).Where(p => p.Region == region.RegionName)
                 .Select(p => p.Name));
        if (count == 0) Line($"{region.RegionName} has no rooms.");
        else if (count == 1) Line($"Only room in {region.RegionName}: {names}");
        else Line($"List of rooms in {region.RegionName}: {names}");
      }
      Line();
    }

    var bent = MapStatistics.BentConnections(true);
    Line($"{MapStatistics.UpDown} up-down connection{Plural(MapStatistics.UpDown)}");
    Line($"{MapStatistics.InOut} in-out connection{Plural(MapStatistics.InOut)}");
    Line($"{MapStatistics.CustomConnections} custom connection{Plural(MapStatistics.CustomConnections)}");
    Line($"{MapStatistics.DiagonalConnections(2)} diagonal connection{Plural(MapStatistics.DiagonalConnections(2))} (2-way)");
    Line($"{MapStatistics.DiagonalConnections(1)} diagonal connection{Plural(MapStatistics.DiagonalConnections(1))} (1-way)");
    Line($"{MapStatistics.HasMiddleText} connection{Plural(MapStatistics.HasMiddleText)} with middle text");
    Line($"{bent} bent connection{Plural(bent)}, {MapStatistics.BentConnections(false)} with no text");
    Line();

    Line(MapStatistics.DupConnectionList("in"));
    Line(MapStatistics.DupConnectionList("out"));
    Line(MapStatistics.DupConnectionList("up"));
    Line(MapStatistics.DupConnectionList("down"));

    return sb.ToString();
  }

  private static string Plural(int count)
  {
    return count == 1 ? string.Empty : "s";
  }

  private static string Num(float value)
  {
    return value.ToString(CultureInfo.InvariantCulture);
  }
}
