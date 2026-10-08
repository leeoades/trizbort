using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Export.Domain;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.Export {
  public abstract partial class CodeExporter : IDisposable {
    private readonly Dictionary<Room, Location> mMapRoomToLocation = new Dictionary<Room, Location>();

    protected CodeExporter() {
      LocationsInExportOrder = new List<Location>();
      RegionsInExportOrder = new List<ExportRegion>();
    }

    public abstract List<KeyValuePair<string, string>> FileDialogFilters { get; }

    public abstract string FileDialogTitle { get; }

    protected List<Location> LocationsInExportOrder { get; }

    protected List<ExportRegion> RegionsInExportOrder { get; }

    protected abstract IEnumerable<string> ReservedWords { get; }

    public void Dispose() {
      Dispose(true);
    }

    protected static string Deaccent(string mystr) {
      var x = "";
      foreach (var c in mystr)
        if (c >= '�' && c <= '�')
          x = x + 'a';
        else if (c >= '�' && c <= '�') x = x + 'A';
        else if (c == '�') x = x + 'C';
        else if (c == '�') x = x + 'c';
        else if (c >= '�' && c <= '�') x = x + 'e';
        else if (c >= '�' && c <= '�') x = x + 'E';
        else if (c >= '�' && c <= '�') x = x + 'i';
        else if (c >= '�' && c <= '�') x = x + 'I';
        else if (c == '�') x = x + 'n';
        else if (c == '�') x = x + 'N';
        else if (c >= '�' && c <= '�') x = x + 'o';
        else if (c >= '�' && c <= '�') x = x + 'O';
        else if (c >= '�' && c <= '�') x = x + 'u';
        else if (c >= '�' && c <= '�') x = x + 'U';
        else x = x + c;
      return x;
    }

    public string Export() {
      string ss;
      using (var writer = new StringWriter()) {
        var title = Project.Current.Title;
        if (string.IsNullOrEmpty(title)) {
          title = PathHelper.SafeGetFilenameWithoutExtension(Project.Current.FileName);
          if (string.IsNullOrEmpty(title)) title = "A Trizbort Map";
        }

        var author = Project.Current.Author;
        if (string.IsNullOrEmpty(author)) author = "A Trizbort User";
        var history = Project.Current.History;

        prepareContent();
        ExportHeader(writer, title, author, Project.Current.Description ?? string.Empty, history);
        ExportContent(writer);

        ss = writer.ToString();
      }

      return ss;
    }


    public void Export(string fileName) {
      using (var writer = Create(fileName)) {
        var title = Project.Current.Title;
        if (string.IsNullOrEmpty(title)) {
          title = PathHelper.SafeGetFilenameWithoutExtension(Project.Current.FileName);
          if (string.IsNullOrEmpty(title)) title = "A Trizbort Map";
        }

        var author = Project.Current.Author;
        if (string.IsNullOrEmpty(author)) author = "A Trizbort User";

        var history = Project.Current.History;
        prepareContent();
        ExportHeader(writer, title, author, Project.Current.Description ?? string.Empty, history);
        ExportContent(writer);
      }
    }

    protected virtual StreamWriter Create(string fileName) {
      return new StreamWriter(fileName, false, Encoding.ASCII, 2 ^ 16);
    }

    protected virtual void Dispose(bool disposing) { }

    protected abstract void ExportContent(TextWriter writer);

    protected abstract void ExportHeader(TextWriter writer, string title, string author, string description,
                                         string history);

    protected abstract string GetExportName(Room room, int? suffix);
    protected abstract string GetExportName(string displayName, int? suffix);

    private void findExits() {
      // find the exits from each room,
      // file them by room, and assign them priorities.
      // don't decide yet which exit is "the" from a room in a particular direction,
      // since we need to compare all a room's exits for that.
      foreach (var connection in Project.Current.Elements.OfType<Connection>()) {
        var sourceRoom = connection.GetSourceRoom(out var sourceCompassPoint);
        var targetRoom = connection.GetTargetRoom(out var targetCompassPoint);

        if (sourceRoom == null || targetRoom == null) continue;

        if (sourceRoom == targetRoom && sourceCompassPoint == targetCompassPoint) continue;

        if (mMapRoomToLocation.TryGetValue(sourceRoom, out var sourceLocation) &&
            mMapRoomToLocation.TryGetValue(targetRoom, out var targetLocation)) {
          sourceLocation.AddExit(new Exit(sourceLocation, targetLocation, sourceCompassPoint, connection.StartText,
            connection));

          if (connection.Flow == ConnectionFlow.TwoWay)
            targetLocation.AddExit(new Exit(targetLocation, sourceLocation, targetCompassPoint, connection.EndText,
              connection));
        }
      }
    }

    private void findRegions() {
      var mapExportNameToRegion = new Dictionary<string, Region>(StringComparer.InvariantCultureIgnoreCase);

      foreach (var reservedWord in ReservedWords)
        mapExportNameToRegion.Add(reservedWord, null);

      foreach (var region in Settings.Regions.Where(p => p.RegionName != Region.DefaultRegion)) {
        var exportName = GetExportName(region.RegionName, null);
        if (exportName == string.Empty)
          exportName = "region";

        var index = 2;
        while (mapExportNameToRegion.ContainsKey(exportName))
          exportName = GetExportName(region.RegionName, index++);

        mapExportNameToRegion[exportName] = region;
        RegionsInExportOrder.Add(new ExportRegion(region, exportName));
      }
    }

    private void findRooms() {
      var mapExportNameToRoom = new Dictionary<string, Room>(StringComparer.InvariantCultureIgnoreCase);

      // prevent use of reserved words
      foreach (var reservedWord in ReservedWords) mapExportNameToRoom.Add(reservedWord, null);

      foreach (var region in RegionsInExportOrder) mapExportNameToRoom.Add(region.ExportName, null);

      foreach (var element in Project.Current.Elements.OfType<Room>()) {
        var room = element;

        // assign each room a unique export name.
        var exportName = GetExportName(room, null);
        if (exportName == string.Empty)
          exportName = "object";
        var index = 2;
        while (mapExportNameToRoom.ContainsKey(exportName)) exportName = GetExportName(room, index++);

        mapExportNameToRoom[exportName] = room;
        var location = new Location(room, exportName);
        LocationsInExportOrder.Add(location);
        mMapRoomToLocation[room] = location;
      }
    }

    private void findThings() {
      var mapExportNameToThing = new Dictionary<string, Thing>(StringComparer.InvariantCultureIgnoreCase);

      // prevent use of reserved words
      foreach (var reservedWord in ReservedWords) mapExportNameToThing.Add(reservedWord, null);

      foreach (var rooms in LocationsInExportOrder) mapExportNameToThing.Add(rooms.ExportName, null);

      foreach (var region in RegionsInExportOrder) mapExportNameToThing.Add(region.ExportName, null);

      foreach (var location in LocationsInExportOrder) {
        var objectsText = location.Room.Objects;
        if (string.IsNullOrEmpty(objectsText)) continue;

        // indentation (spaces, tabs or "-" bullets) denotes containment; see ObjectList
        var items = ObjectList.Parse(objectsText);
        var things = new List<Thing>();
        foreach (var item in items) {
          // assign each thing a unique export name.
          var exportName = GetExportName(item.Name, null);
          var index = 2;
          while (mapExportNameToThing.ContainsKey(exportName)) exportName = GetExportName(item.Name, index++);

          var container = item.ParentIndex >= 0 ? things[item.ParentIndex] : null;
          var thing = new Thing(item.Name, exportName, location, container, item.Indent, item.PropString);
          mapExportNameToThing.Add(exportName, thing);
          location.Things.Add(thing);
          things.Add(thing);
        }
      }
    }

    private void pickBestExits() {
      // for every direction from every room, if there are one or more exits
      // in said direction, pick the best one.
      foreach (var location in LocationsInExportOrder) location.PickBestExits();
    }

    private void prepareContent() {
      mMapRoomToLocation.Clear();
      LocationsInExportOrder.Clear();
      RegionsInExportOrder.Clear();
      findRegions();
      findRooms();
      findExits();
      pickBestExits();
      findThings();
    }
  }
}