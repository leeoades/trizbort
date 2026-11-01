using System;
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

  /// <summary>
  ///   Start a group of map changes that is recorded as one undo step titled <paramref name="title" />. Dispose the
  ///   result to end the step. Keep it short: do not await anything while it is open, because the user cannot undo
  ///   or redo while a step is open.
  /// </summary>
  IDisposable BeginBatch(string title);

  /// <summary>
  ///   Find the room in the current project that has the same element id as <paramref name="room" />, or null when
  ///   it no longer exists. Undo and redo replace room objects, so remembered rooms must be looked up again.
  /// </summary>
  Room ResolveRoom(Room room);
}

public delegate bool? RoomMatcher(string roomName, string roomDescription, Room room);

/// <summary>
///   Result of searching for a room: a certain match (if any) and otherwise the possible (ambiguous) matches.
/// </summary>
public readonly record struct RoomSearchResult(Room Certain, IReadOnlyList<Room> Maybes);
