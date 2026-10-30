using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trizbort.Domain;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Export.Domain;

namespace Trizbort.Export.Languages;

internal class AlanExporter : CodeExporter
{
  public override string FileDialogTitle => "Export Alan Source Code";

  public override List<KeyValuePair<string, string>> FileDialogFilters => new() {
    new KeyValuePair<string, string>("Alan Source Files", ".i"),
    new KeyValuePair<string, string>("Text Files", ".txt")
  };

  protected override IEnumerable<string> ReservedWords => new[]
    { "object", "objects", "thing", "things", "door", "doors", "is", "are", "in", "on", "and", "outside", "inside" };

  protected override StreamWriter Create(string fileName)
  {
    if (Path.GetExtension(fileName) == ".inform")
    {
      var directoryName = Path.Combine(fileName, "Source");
      Directory.CreateDirectory(directoryName);
      fileName = Path.Combine(directoryName, "Story.ni");
    }

    return base.Create(fileName);
  }

  protected override void ExportHeader(
    TextWriter writer,
    string title,
    string author,
    string description,
    string history)
  {
    writer.WriteLine(
      "-- Alan currently does not process metadata such as {0}, so those will be in the metadata below.",
      string.IsNullOrWhiteSpace(description) ? "title and author" : "title, author, or description");
    writer.WriteLine("-- Trizbort exports History to the 'about' verb.");
    writer.WriteLine("-- All other metadata will be in comments below.");
    writer.WriteLine();
    writer.WriteLine("-- \"{0}\" by {1}", title, author);

    if (!string.IsNullOrWhiteSpace(description))
    {
      writer.WriteLine("-- description: {0}{1}", description, description.EndsWith(".") ? string.Empty : ".");
      writer.WriteLine();
    }

    if (!string.IsNullOrWhiteSpace(history)) ExportHistory(writer, history);
  }

  private static void ExportHistory(TextWriter writer, string history)
  {
    var historyCoded = history.Replace("\r\n", "\"\r\n    \"");
    writer.WriteLine("Verb about");
    writer.WriteLine("    \"{0}\"", historyCoded);
    writer.WriteLine("End Verb about.");
    writer.WriteLine("");
  }

  private void PrintThisLoc(TextWriter writer, Location location)
  {
    // remember we've exported this location

    writer.WriteLine("The {0} isa location Name '{1}'", location.ExportName, location.Room.Name);

    var nowhereExits = "";

    var description = location.Room.PrimaryDescription;
    if (!string.IsNullOrWhiteSpace(description))
    {
      writer.WriteLine("  Description");
      writer.WriteLine("  \"{0}\"", description);
    }
    else
    {
      writer.WriteLine("  Description \"\"");
    }

    foreach (var direction in Directions.AllDirections)
    {
      var exit = location.GetBestExit(direction);

      if (exit != null && !exit.Exported)
      {
        // remember we've exported this exit
        exit.Exported = true;

        writer.WriteLine("  Exit {0} to {1}.", GetAlanName(direction), exit.Target.ExportName);
        if (exit.Conditional)
        {
          writer.WriteLine("    Check");
          writer.WriteLine("      \"This was marked as a conditional exit in Trizbort, so you'll want to change it.\"");
        }

        writer.WriteLine("  End exit.");
      }
      else
      {
        if (string.IsNullOrWhiteSpace(nowhereExits))
          nowhereExits = GetAlanName(direction);
        else
          nowhereExits += " " + GetAlanName(direction);
      }
    }

    if (!string.IsNullOrWhiteSpace(nowhereExits))
    {
      writer.WriteLine();
      writer.WriteLine("  Exit {0} to nowhere", nowhereExits);
      writer.WriteLine("    Check");
      writer.WriteLine("      \"You can't go that way.\"");
      writer.WriteLine("  End exit.");
    }

    if (location.Room.IsDark) writer.WriteLine("  Is Not lit.");

    writer.WriteLine("end The {0}.", location.ExportName);
    writer.WriteLine("");

    if (location.Room.IsStartRoom)
    {
      writer.WriteLine("The hero Isa actor at {0}", location.ExportName);
      writer.WriteLine("End The Hero.");
      writer.WriteLine();
    }

    foreach (var thing in location.Things)
    {
      writer.WriteLine(
        "The {0} isa {1} at {2}.",
        thing.ExportName,
        thing.IsPerson ? "actor" : "thing",
        location.ExportName);
      writer.WriteLine("  IsDisplayedAs {0}.", thing.DisplayName);
      writer.WriteLine("End The {0}.", thing.ExportName);
      /*if (!string.IsNullOrWhiteSpace(thing.WarningText))
      {
        string warningCode = thing.WarningText.TrimEnd();
        warningCode = warningCode.Replace("\n", "\n-- ");
        writer.WriteLine("-- {0}", warningCode);
      }*/
      writer.WriteLine();
    }
  }

  protected override void ExportContent(TextWriter writer)
  {
    foreach (var location in LocationsInExportOrder) PrintThisLoc(writer, location);
  }

  protected override string GetExportName(Room room, int? suffix)
  {
    return CreateExportName(room.Name, suffix);
  }

  protected override string GetExportName(string displayName, int? suffix)
  {
    return CreateExportName(displayName, suffix);
  }

  private string CreateExportName(string name, int? suffix)
  {
    var spaceless = true;

    if (ContainsOddCharacters(name))
      name = StripOddCharacters(name);

    if (ContainsWord(name, ReservedWords) && suffix == null) suffix = 1;

    if (suffix != null)
      name = $"{name}{(spaceless ? string.Empty : " ")}{suffix}";

    return name;
  }

  private static bool ContainsWord(string text, IEnumerable<string> words)
  {
    return words.Any(word => ContainsWord(text, word));
  }

  private static bool ContainsWord(string text, string word)
  {
    if (string.IsNullOrEmpty(text)) return string.IsNullOrEmpty(word);
    var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    return words.Any(wordFound => StringComparer.InvariantCultureIgnoreCase.Compare(word, wordFound) == 0);
  }

  private static bool ContainsOddCharacters(string text)
  {
    return text.Any(c => c != ' ' && c != '-' && !char.IsLetterOrDigit(c));
  }

  private static string StripOddCharacters(string text, params char[] exceptChars)
  {
    var exceptCharsList = new List<char>(exceptChars);
    var newText = text.Where(c => c == ' ' || c == '-' || char.IsLetterOrDigit(c) || exceptCharsList.Contains(c))
                      .Aggregate(string.Empty, (current, c) => current + c);
    return string.IsNullOrEmpty(newText) ? "object" : newText;
  }

  private static string GetAlanName(MappableDirection direction)
  {
    switch (direction)
    {
      case MappableDirection.North:
        return "North";
      case MappableDirection.South:
        return "South";
      case MappableDirection.East:
        return "East";
      case MappableDirection.West:
        return "West";
      case MappableDirection.NorthEast:
        return "Northeast";
      case MappableDirection.SouthEast:
        return "Southeast";
      case MappableDirection.NorthWest:
        return "Northwest";
      case MappableDirection.SouthWest:
        return "Southwest";
      case MappableDirection.Up:
        return "Up";
      case MappableDirection.Down:
        return "Down";
      case MappableDirection.In:
        return "Inside";
      case MappableDirection.Out:
        return "Outside";
      default:
        return "";
        throw new InvalidOperationException("Cannot convert a direction to its Inform 7 equivalent.");
    }
  }
}