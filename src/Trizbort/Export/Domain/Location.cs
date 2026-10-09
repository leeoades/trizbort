using System.Collections.Generic;
using Trizbort.Domain;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;

namespace Trizbort.Export.Domain
{
    public class Location {
      private readonly List<Exit> _mExits = new List<Exit>();
      private readonly Dictionary<MappableDirection, Exit> _mMapDirectionToBestExit = new Dictionary<MappableDirection, Exit>();

      public Location(Room room, string exportName) {
        Room = room;
        ExportName = exportName;
      }

      public string ExportName { get; }

      public Room Room { get; }

      public List<Thing> Things { get; } = new List<Thing>();

      public void AddExit(Exit exit) {
        _mExits.Add(exit);
      }

      public Exit GetBestExit(MappableDirection direction) {
        return _mMapDirectionToBestExit.TryGetValue(direction, out var exit) ? exit : null;
      }

      public void PickBestExits() {
        _mMapDirectionToBestExit.Clear();
        foreach (var direction in Directions.AllDirections) {
          var exit = pickBestExit(direction);
          if (exit != null) _mMapDirectionToBestExit.Add(direction, exit);
        }
      }

      private Exit pickBestExit(MappableDirection direction) {
        // sort exits by priority for this direction only
        _mExits.Sort((a, b) => {
          var one = a.GetPriority(direction);
          var two = b.GetPriority(direction);
          return two - one;
        });

        // pick the highest priority exit if its direction matches;
        // if the highest priority exit's direction doesn't match,
        // there's no exit in this direction.
        if (_mExits.Count > 0) {
          var exit = _mExits[0];
          if (exit.PrimaryDirection == direction || exit.SecondaryDirection == direction) return exit;
        }

        return null;
      }
    }
  }
