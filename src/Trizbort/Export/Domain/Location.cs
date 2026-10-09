using System.Collections.Generic;
using Trizbort.Domain;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;

namespace Trizbort.Export.Domain;

public class Location {
  private readonly List<Exit> _exits = new();
  private readonly Dictionary<MappableDirection, Exit> _mapDirectionToBestExit = new();

  public Location(Room room, string exportName)
  {
    Room = room;
    ExportName = exportName;
  }

  public string ExportName { get; }

  public Room Room { get; }

  public List<Thing> Things { get; } = new();

  public void AddExit(Exit exit)
  {
    _exits.Add(exit);
  }

  public Exit GetBestExit(MappableDirection direction)
  {
    return _mapDirectionToBestExit.TryGetValue(direction, out var exit) ? exit : null;
  }

  public void PickBestExits()
  {
    _mapDirectionToBestExit.Clear();
    foreach (var direction in Directions.AllDirections) {
      var exit = PickBestExit(direction);
      if (exit != null) _mapDirectionToBestExit.Add(direction, exit);
    }
  }

  private Exit PickBestExit(MappableDirection direction)
  {
    // sort exits by priority for this direction only
    _exits.Sort((a, b) => {
      var one = a.GetPriority(direction);
      var two = b.GetPriority(direction);
      return two - one;
    });

    // pick the highest priority exit if its direction matches;
    // if the highest priority exit's direction doesn't match,
    // there's no exit in this direction.
    if (_exits.Count > 0) {
      var exit = _exits[0];
      if (exit.PrimaryDirection == direction || exit.SecondaryDirection == direction) return exit;
    }

    return null;
  }
}