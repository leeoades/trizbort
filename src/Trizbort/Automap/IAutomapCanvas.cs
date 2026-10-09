using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;

namespace Trizbort.Automap
{
  internal interface IAutomapCanvas
  {
    /// <summary>
    ///   Find a room matching the given name and, if the given description isn't null, the given description.
    /// </summary>
    Room FindRoom(string roomName, string roomDescription, string line, RoomMatcher matcher);

    Room CreateRoom(Room existing, string name);

    Room CreateRoom(Room existing, MappableDirection directionFromExisting, string roomName, string line);

    void Connect(Room source, MappableDirection directionFromSource, Room target, bool assumeTwoWayConnections);

    void AddExitStub(Room room, MappableDirection direction);

    void RemoveExitStub(Room room, MappableDirection direction);

    void SelectRoom(Room room);
    void RemoveRoom(Room otherRoom);
  }

  internal delegate bool? RoomMatcher(string roomName, string roomDescription, Room room);
}