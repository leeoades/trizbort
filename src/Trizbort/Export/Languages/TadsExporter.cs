using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Trizbort.Domain;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export.Domain;

namespace Trizbort.Export.Languages;

internal class TadsExporter : CodeExporter
{
  private const char SingleQuote = '\'';
  private const char DoubleQuote = '"';

  public override List<KeyValuePair<string, string>> FileDialogFilters => new() {
    new KeyValuePair<string, string>("TADS Source Files", ".t"),
    new KeyValuePair<string, string>("Text Files", ".txt")
  };

  public override string FileDialogTitle => "Export TADS Source Code";

  protected override IEnumerable<string> ReservedWords => new[] {
    "Room", "Actor", "Thing", "Object", "Door", "Chair", "Heavy", "Fixture", "OpenableContainer", "Food", "GameMainDef",
    "if", "else", "me"
  };

  protected override void ExportContent(TextWriter writer)
  {
    if (ApplicationSettingsController.AppSettings.SaveTadstoAdv3Lite)
      foreach (var region in RegionsInExportOrder)
      {
        writer.WriteLine("{0}: Region", region.ExportName);
        writer.WriteLine(";");
        writer.WriteLine();
      }

    foreach (var location in LocationsInExportOrder)
    {
      writer.WriteLine(
        "{0}: {1} {2}",
        location.ExportName,
        location.Room.IsDark ? "DarkRoom" : "Room",
        ToTadsString(location.Room.Name, SingleQuote));
      if (!string.IsNullOrEmpty(location.Room.PrimaryDescription))
        writer.WriteLine("    {0}", ToTadsString(location.Room.PrimaryDescription, DoubleQuote));
      if (ApplicationSettingsController.AppSettings.SaveTadstoAdv3Lite && location.Room.Region != Region.DefaultRegion)
        writer.WriteLine("    regions = [{0}]", location.Room.Region);
      var anyExits = false;
      foreach (var direction in Directions.AllDirections)
      {
        var exit = location.GetBestExit(direction);
        if (exit != null)
        {
          if (!anyExits)
          {
            writer.WriteLine();
            anyExits = true;
          }

          writer.WriteLine("    {0} = {1}", ToTadsPropertyName(direction), exit.Target.ExportName);
        }
      }

      writer.WriteLine(";");
      writer.WriteLine();

      ExportThings(writer, location.Things, null, 1);
    }

    writer.WriteLine("me: Actor");
    if (LocationsInExportOrder.Count > 0)
    {
      var foundStart = false;
      foreach (var location in LocationsInExportOrder)
        if (location.Room.IsStartRoom)
        {
          if (foundStart) writer.WriteLine("/( {0} is an extra StartRoom. /*", location.ExportName);
          writer.WriteLine("    location = {0}", location.ExportName);
          foundStart = true;
        }

      if (!foundStart)
        writer.WriteLine("    location = {0}", LocationsInExportOrder[0].ExportName);
    }
    else
    {
      writer.WriteLine("    /* location = ... */");
    }

    writer.WriteLine(";");
    writer.WriteLine();

    writer.WriteLine("gameMain: GameMainDef");
    writer.WriteLine("    initialPlayerChar = me");
    writer.WriteLine(";");
    writer.WriteLine();
  }
  //    protected override Encoding Encoding => Encoding.ASCII;

  protected override void ExportHeader(
    TextWriter writer,
    string title,
    string author,
    string description,
    string history)
  {
    writer.WriteLine("#charset \"us-ascii\"");
    writer.WriteLine();
    if (ApplicationSettingsController.AppSettings.SaveTadstoAdv3Lite)
    {
      writer.WriteLine("#include <tads.h>");
      writer.WriteLine("#include \"advlite.h\"");
    }
    else
    {
      writer.WriteLine("#include <adv3.h>");
      writer.WriteLine("#include <en_us.h>");
    }

    writer.WriteLine();
    writer.WriteLine("versionInfo : GameID");
    writer.WriteLine("    name = {0}", ToTadsString(title, SingleQuote));
    writer.WriteLine("    byline = {0}", ToTadsString($"By {author}", SingleQuote));
    writer.WriteLine("    version = '1'");
    writer.WriteLine("    desc = {0}", ToTadsString(description, SingleQuote));
    if (!string.IsNullOrWhiteSpace(history)) ExportHistory(writer, history);
    writer.WriteLine(";");
    writer.WriteLine();
  }

  protected override string GetExportName(Room room, int? suffix)
  {
    var name = StripOddCharacters(room.Name);
    if (string.IsNullOrEmpty(name)) name = "room";

    if (suffix != null) name = $"{name}{suffix}";
    return name;
  }

  protected override string GetExportName(string displayName, int? suffix)
  {
    var name = StripOddCharacters(displayName);
    if (string.IsNullOrEmpty(name)) name = "item";
    if (suffix != null) name = $"{name}{suffix}";
    return name;
  }

  private void ExportHistory(TextWriter writer, string history)
  {
    writer.WriteLine();
    writer.WriteLine("    showAbout()");
    writer.WriteLine("    {");
    writer.WriteLine($"    {DoubleQuote}{history}{DoubleQuote};");
    writer.WriteLine("    }");
  }

  private static void ExportThings(TextWriter writer, List<Thing> things, Thing container, int indent)
  {
    foreach (var thing in things.Where(thing => thing.Container == container))
    {
      writer.WriteLine(
        "{0} {1}: {3} {2} {2}",
        Repeat('+', indent),
        thing.ExportName,
        ToTadsString(StripOddCharacters(thing.DisplayName, ' ', '-').Trim(), SingleQuote),
        thing.Contents.Count > 0 ? "Container" : "Thing");
      writer.WriteLine(";");
      writer.WriteLine();

      ExportThings(writer, thing.Contents, thing, indent + 1);
    }
  }

  private static string Repeat(char c, int times)
  {
    var text = string.Empty;
    for (var index = 0; index < times; ++index) text += c;
    return text;
  }

  private static string StripOddCharacters(string text, params char[] exclude)
  {
    var exclusions = new List<char>(exclude);
    if (string.IsNullOrEmpty(text)) return string.Empty;
    var result = string.Empty;
    foreach (var c in text)
      if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_' || exclusions.Contains(c))
        result += c;
    return result;
  }

  private static string ToTadsPropertyName(MappableDirection direction)
  {
    switch (direction)
    {
      case MappableDirection.North:
        return "north";
      case MappableDirection.South:
        return "south";
      case MappableDirection.East:
        return "east";
      case MappableDirection.West:
        return "west";
      case MappableDirection.NorthEast:
        return "northeast";
      case MappableDirection.NorthWest:
        return "northwest";
      case MappableDirection.SouthEast:
        return "southeast";
      case MappableDirection.SouthWest:
        return "southwest";
      case MappableDirection.Up:
        return "up";
      case MappableDirection.Down:
        return "down";
      case MappableDirection.In:
        return "in";
      case MappableDirection.Out:
        return "out";
      default:
        Debug.Assert(false, "Unrecognised automap direction.");
        return "north";
    }
  }

  private static string ToTadsString(string text, char quote)
  {
    if (text == null) text = string.Empty;
    return string.Format("{1}{0}{1}", text.Replace(quote.ToString(), $@"\{quote}"), quote);
  }
}