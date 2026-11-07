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

  /// <summary>
  ///   True while the map tidies itself after each move (<see cref="TidyAround" />): a new room whose spot is taken
  ///   goes there anyway and the tidy moves the other rooms aside, rather than the new room being put further away.
  /// </summary>
  bool MakeSpaceForNewRooms { get; set; }

  /// <summary>Label joins with short directions ("n / u / in") rather than whole words.</summary>
  bool AbbreviateDirections { get; set; }

  /// <summary>
  ///   Record that <paramref name="direction" /> leads from <paramref name="room" /> straight back to it ("north" in
  ///   the forest returns to the forest), as a loop from the room to itself.
  /// </summary>
  void ConnectLoop(Room room, MappableDirection direction);

  void Connect(Room source, MappableDirection directionFromSource, Room target, bool assumeTwoWayConnections);

  /// <summary>
  ///   Join two rooms the player travelled between by something other than a compass direction (a magic word, "enter
  ///   boat"). The join is one-way and dotted, labelled with <paramref name="command" />, and leaves each room on the
  ///   side facing the other. Using the same command the other way makes it two-way.
  /// </summary>
  void ConnectByCommand(Room source, Room target, string command);

  void AddExitStub(Room room, MappableDirection direction);

  void RemoveExitStub(Room room, MappableDirection direction);

  /// <summary>
  ///   Let the rooms near <paramref name="room" /> rearrange themselves so they don't get in each other's way
  ///   (<see cref="LayoutTidy" />). Called inside the move's batch, so it is part of the same undo step.
  /// </summary>
  void TidyAround(Room room);

  void SelectRoom(Room room);
  void RemoveRoom(Room otherRoom);

  /// <summary>
  ///   Start a group of map changes that is recorded as one undo step titled <paramref name="title" />. Dispose the
  ///   result to end the step. Keep it short: do not await anything while it is open, because the user cannot undo
  ///   or redo while a step is open. Returns null, starting nothing, while the user is part-way through a change of
  ///   their own (such as a drag); try again shortly.
  /// </summary>
  IDisposable BeginBatch(string title);

  /// <summary>
  ///   Find the room in the current project that is <paramref name="room" />, or null when it no longer exists. Undo
  ///   and redo replace room objects, so remembered rooms must be looked up again; a new room that reused a deleted
  ///   room's id doesn't count.
  /// </summary>
  Room ResolveRoom(Room room);
}

public delegate bool? RoomMatcher(string roomName, string roomDescription, Room room);

/// <summary>
///   Result of searching for a room: a certain match (if any) and otherwise the possible (ambiguous) matches.
/// </summary>
public readonly record struct RoomSearchResult(Room Certain, IReadOnlyList<Room> Maybes);
