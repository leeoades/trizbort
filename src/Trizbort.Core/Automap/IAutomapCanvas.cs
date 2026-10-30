using System.Collections.Generic;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;

namespace Trizbort.Automap;

public interface IAutomapCanvas
{
  /// <summary>
  ///   Find rooms matching the given name and, if the given description isn't null, the given description.
  ///   Never prompts; the caller decides what to do with ambiguous matches.
  /// </summary>
  RoomSearchResult FindRooms(string roomName, string roomDescription, RoomMatcher matcher);

  Room CreateRoom(Room existing, string name);

  Room CreateRoom(Room existing, MappableDirection directionFromExisting, string roomName, string line);

  void Connect(Room source, MappableDirection directionFromSource, Room target, bool assumeTwoWayConnections);

  void AddExitStub(Room room, MappableDirection direction);

  void RemoveExitStub(Room room, MappableDirection direction);

  void SelectRoom(Room room);
  void RemoveRoom(Room otherRoom);
}

public delegate bool? RoomMatcher(string roomName, string roomDescription, Room room);

/// <summary>
///   Result of searching for a room: a certain match (if any) and otherwise the possible (ambiguous) matches.
/// </summary>
public readonly record struct RoomSearchResult(Room Certain, IReadOnlyList<Room> Maybes);
