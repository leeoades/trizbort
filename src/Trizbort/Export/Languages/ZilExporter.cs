using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Trizbort.Domain;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export.Domain;
using Trizbort.Extensions;

namespace Trizbort.Export.Languages {
  internal class ZilExporter : CodeExporter {
    private const char SingleQuote = '\'';
    private const char DoubleQuote = '"';
    private const char Space = ' ';

    public override List<KeyValuePair<string, string>> FileDialogFilters => new List<KeyValuePair<string, string>> {
      new KeyValuePair<string, string>("ZIL Source File", ".zil"),
      new KeyValuePair<string, string>("Text Files", ".txt")
    };

    public override string FileDialogTitle => "Export ZIL Source Code";

    protected override IEnumerable<string> ReservedWords => new[] {"object", "objects"};

    protected override void ExportContent(TextWriter writer) {
      // export location
      bool needConditionalFunction = false, wroteConditionalFunction = false;
      foreach (var location in LocationsInExportOrder) {
        writer.WriteLine();
        writer.WriteLine($"<ROOM {location.ExportName}");
        writer.WriteLine($"    (DESC {ToZilString(location.Room.Name)})");
        writer.Write($"    (IN ROOMS)");

        if (!String.IsNullOrWhiteSpace(location.Room.PrimaryDescription)) {
          writer.WriteLine();
          writer.Write($"    (LDESC {ToZilString(location.Room.PrimaryDescription)})");
        }

        foreach (var direction in Directions.AllDirections) {
          var exit = location.GetBestExit(direction);
          if (exit != null && exit.Conditional) {
            writer.WriteLine();
            writer.Write($"    ({ToZilPropertyName(direction)} PER TRIZBORT-CONDITIONAL-EXIT)");
            needConditionalFunction = true;
          } else if (exit != null) {
            writer.WriteLine();
            writer.Write($"    ({ToZilPropertyName(direction)} TO {exit.Target.ExportName})");
            var oppositeDirection = CompassPointHelper.GetOpposite(direction);
            if (Exit.IsReciprocated(location, direction, exit.Target)) {
              var reciprocal = exit.Target.GetBestExit(oppositeDirection);
              reciprocal.Exported = true;
            }
          }
        }

        if (!location.Room.IsDark) {
          writer.WriteLine();
          writer.Write("    (FLAGS LIGHTBIT)");
        }

        writer.WriteLine(">");
        writer.WriteLine();

        if (needConditionalFunction && !wroteConditionalFunction) {
          writer.WriteLine();
          writer.WriteLine("<ROUTINE TRIZBORT-CONDITIONAL-EXIT ()");
          writer.WriteLine($"    <TELL {DoubleQuote}An export nymph appears on your keyboard. She says, 'You can't go that way, as that exit was marked as conditional, you know, a dotted line, in Trizbort. Obviously in your game you'll have a better rationale for this than, er, me.' She looks embarrassed. 'Bye!'{DoubleQuote} CR>");
          writer.WriteLine("    <RFALSE>>");
          writer.WriteLine();
          wroteConditionalFunction = true;
        }

        ExportThings(writer, location.Things, null, 1);
      }
    }


    protected override void ExportHeader(TextWriter writer, string title, string author, string description, string history) {
      var list = Project.Current.Elements.OfType<Room>().Where(p => p.IsStartRoom).ToList();
      var startingRoom = list.Count == 0 ? LocationsInExportOrder.First() : LocationsInExportOrder.Find(p => p.Room.Id == list.First().Id);

      writer.WriteLine($"{DoubleQuote}{title} main file{DoubleQuote}");
      writer.WriteLine();
      writer.WriteLine("<VERSION ZIP>");
      writer.WriteLine("<CONSTANT RELEASEID 1>");
      writer.WriteLine();
      writer.WriteLine($"{DoubleQuote}Main Loop{DoubleQuote}");
      writer.WriteLine();
      writer.WriteLine($"<CONSTANT GAME-BANNER {DoubleQuote}{title}|An interactive fiction by {author}{DoubleQuote}>");
      writer.WriteLine();
      writer.WriteLine($"<ROUTINE GO ()");
      writer.WriteLine($"    <CRLF> <CRLF>");
      writer.WriteLine($"    <TELL {ToZilString(description)} CR CR>");
      writer.WriteLine($"    <V-VERSION> <CRLF>");
      writer.WriteLine($"    <SETG HERE ,{startingRoom.ExportName}>");
      writer.WriteLine($"    <MOVE ,PLAYER ,HERE>");
      writer.WriteLine($"    <V-LOOK>");
      writer.WriteLine($"    <REPEAT ()");
      writer.WriteLine($"        <COND (<PARSER>");
      writer.WriteLine($"               <PERFORM ,PRSA ,PRSO ,PRSI>");
      writer.WriteLine($"               <COND (<NOT <GAME-VERB?>>");
      writer.WriteLine($"                      <APPLY <GETP ,HERE ,P?ACTION> ,M-END>");
      writer.WriteLine($"                      <CLOCKER>)>)>");
      writer.WriteLine($"        <SETG HERE <LOC ,WINNER>>>>");
      writer.WriteLine();
      writer.WriteLine($"<INSERT-FILE {DoubleQuote}parser{DoubleQuote}>");
      writer.WriteLine();

      if (!String.IsNullOrWhiteSpace(history)) ExportHistory(writer, history);

      writer.WriteLine($"{DoubleQuote}Objects{DoubleQuote}");
    }


    protected override string GetExportName(Room room, int? suffix) {
      var name = room.Name.ToUpper().Replace(' ', '-');
      if (suffix != null || ContainsWord(name, ReservedWords) || ContainsOddCharacters(name)) name = StripOddCharacters(name.Replace(" ", "-"));
      if (suffix != null) name = $"{name}-{suffix}";

      return name;
    }

    protected override string GetExportName(string displayName, int? suffix) {
      var name = StripOddCharacters(displayName);

      name = name.ToUpper().Replace(' ', '-');

      if (String.IsNullOrEmpty(name)) name = "item";
      if (suffix != null) name = $"{name}{suffix}";
      return name;
    }

    private static bool ContainsOddCharacters(string text) {
      return text.Any(c => c != ' ' && c != '-' && !char.IsLetterOrDigit(c));
    }

    private static bool ContainsWord(string text, IEnumerable<string> words) {
      return words.Any(word => ContainsWord(text, word));
    }

    private static bool ContainsWord(string text, string word) {
      if (String.IsNullOrEmpty(text)) return String.IsNullOrEmpty(word);
      var words = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
      return words.Any(wordFound => StringComparer.InvariantCultureIgnoreCase.Compare(word, wordFound) == 0);
    }

    private void ExportHistory(TextWriter writer, string history) {
      writer.WriteLine("<SYNTAX ABOUT = V-ABOUT>");
      writer.WriteLine();
      writer.WriteLine("<ROUTINE V-ABOUT ()");
      writer.WriteLine($"    <TELL {ToZilString(history)} CR>>");
      writer.WriteLine();
    }

    private static void ExportThings(TextWriter writer, List<Thing> things, Thing container, int indent) {
      foreach (var thing in things.Where(p => p.Container == container)) {
        writer.WriteLine();
        writer.WriteLine($"<OBJECT {thing.ExportName}");

        if (thing.Container == null)
          writer.WriteLine($"    (IN {thing.Location.ExportName})");
        else
          writer.WriteLine($"    (IN {thing.Container.ExportName})");

        writer.WriteLine($"    (DESC {ToZilString(thing.DisplayName)})");

        var words = GetObjectWords(thing);
        if (words.Count > 0) writer.WriteLine($"    (SYNONYM {words[words.Count - 1]})");
        if (words.Count > 1) writer.WriteLine($"    (ADJECTIVE {String.Join($"{Space}", words.Take(words.Count - 1))})");

        writer.WriteLine($"    (FLAGS {GetFlags(thing)})>");
        writer.WriteLine();

        if (thing.Contents.Any())
          ExportThings(writer, thing.Contents, thing, indent++);
      }
    }

    private static string GetFlags(Thing thing) {
      var flags = new StringBuilder("TAKEBIT");

      if (thing.DisplayName.StartsWithVowel()) flags.Append(" VOWELBIT");

      if (thing.Contents.Any()) flags.Append(" CONTBIT");

      return flags.ToString();
    }

    private static IList<string> GetObjectWords(Thing thing) {
      var synonyms = String.Empty;
      var list = new List<string>();

      var words = thing.DisplayName.Split(' ').ToList();

      words.ForEach(p => list.Add(StripOddCharacters(p).ToUpper()));

      return list;
    }

    private static string StripOddCharacters(string text, params char[] exceptChars) {
      var exceptCharsList = new List<char>(exceptChars);
      var newText = text.Where(c => c == ' ' || c == '-' || char.IsLetterOrDigit(c) || exceptCharsList.Contains(c)).Aggregate(String.Empty, (current, c) => current + c);
      return String.IsNullOrEmpty(newText) ? "object" : newText;
    }

    private static string ToZilPropertyName(MappableDirection direction) {
      switch (direction) {
        case MappableDirection.North:
          return "NORTH";
        case MappableDirection.South:
          return "SOUTH";
        case MappableDirection.East:
          return "EAST";
        case MappableDirection.West:
          return "WEST";
        case MappableDirection.NorthEast:
          return "NE";
        case MappableDirection.SouthEast:
          return "SE";
        case MappableDirection.SouthWest:
          return "SW";
        case MappableDirection.NorthWest:
          return "NW";
        case MappableDirection.Up:
          return "UP";
        case MappableDirection.Down:
          return "DOWN";
        case MappableDirection.In:
          return "IN";
        case MappableDirection.Out:
          return "OUT";
        default:
          throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
      }
    }

    private static string ToZilString(string str) {
      if (str == null) str = String.Empty;
      return DoubleQuote + str.Replace('\n', '|').Replace($"{DoubleQuote}", $"\\{DoubleQuote}") + DoubleQuote;
    }
  }
}