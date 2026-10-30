using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trizbort.Automap.Utility;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.UI;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Automap;

public sealed class Automap
{
  private readonly Func<Room, string, AutomapSameDirectionResult> _chooseConflictingRoom;
  private readonly Action<string, string> _reportError;
  public bool UseDottedConnection { get; set; }


  private async Task WaitForStep(CancellationToken token)
  {
    token.ThrowIfCancellationRequested();
    // for diagnostic purposes, allow single stepping
    if (_settings.SingleStep && !_stepNow)
    {
      Status = "Automapping is waiting for you to step through it (with F11.)";
      while (!_stepNow) await Task.Delay(50, token);
      token.ThrowIfCancellationRequested();
      _stepNow = false;
    }
  }

  private async Task<string> WaitForNewLine(StreamReader reader, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();
    if (reader.EndOfStream) Status = "Automapping is waiting for more text.";
    while (reader.EndOfStream) await Task.Delay(500, token);
    return await reader.ReadLineAsync();
  }

  internal bool IsPrompt(string line, out string typedCommand)
  {
    if (line == null)
    {
      typedCommand = null;
      return false;
    }

    foreach (var promptMarker in _promptMarkers)
    {
      var startIndex = line.LastIndexOf(promptMarker);
      if (startIndex != -1 && startIndex < MaxCharactersBeforePrompt)
      {
        var command = line.Substring(startIndex + promptMarker.Length);
        typedCommand = command.Trim();
        return true;
      }
    }

    typedCommand = null;
    return false;
  }

  internal bool ExtractRoomName(string line, string previousLine, out string name)
  {
    name = null;

    string unused;
    if (previousLine != null && previousLine.Trim().Length > 0 && !IsPrompt(previousLine, out unused))
    {
      // the preceeding line, if any, must be blank, or a prompt
      SetFailureReason("the previous line, if any, must be blank or a prompt");
      return false;
    }

    if (string.IsNullOrEmpty(line))
    {
      // blank lines clearly aren't room names
      SetFailureReason("the line is blank");
      return false;
    }

    // look for leading whitespace
    if (line.TrimStart().Length != line.Length)
    {
      // lines which start with whitespace aren't room names
      SetFailureReason("the line starts with whitespace");
      return false;
    }

    // trim whitespace
    line = line.Trim();
    if (line.Length == 0)
    {
      // we just ran out of line
      SetFailureReason("the line only contains whitespace");
      return false;
    }

    //      if (!char.IsLetterOrDigit(line[line.Length - 1]))
    //      {
    //        // the last character of the room name must be a number or a letter
    //        SetFailureReason("the line didn't end with a letter or a number");
    //        return false;
    //      }

    // strip suffixes such as those in "Bedroom, on the bed" or "Bedroom (on the bed)" or "Bedroom - on the bed"
    bool strippedSuffix;
    do
    {
      strippedSuffix = false;
      foreach (var decorativeSuffixMarker in _roomDecorativeSuffixMarkers)
      {
        var indexOfMarker = line.IndexOf(decorativeSuffixMarker, StringComparison.Ordinal);
        if (indexOfMarker >= 0)
        {
          var suffixLength = line.Length - indexOfMarker;
          if (suffixLength > 30)
          {
            // this looks more like punctuation in a sentence, not a suffix
            SetFailureReason("this line looks more like a sentence");
            return false;
          }

          line = line.Substring(0, indexOfMarker);
          strippedSuffix = true;
          break;
        }
      }
    } while (strippedSuffix);

    // swallow any leading/trailing whitespace we just made
    line = line.Trim();
    if (line.Length == 0)
    {
      // we just ran out of line
      SetFailureReason("after stripping suffixes from the line, it was blank");
      return false;
    }

    if (!char.IsLetterOrDigit(line[line.Length - 1]))
    {
      // the last character of the room description must be a number or a letter
      SetFailureReason("after stripping suffixes from the line, it didn't end with a letter or a number");
      return false;
    }

    if (!char.IsLetterOrDigit(line[0]))
    {
      // the first character of the room description must be a number or a letter
      SetFailureReason("the line must start with a letter or a number");
      return false;
    }

    if (!StartsWithCapitalOrNonLetter(line))
    {
      // if the first character of the room description is a letter, it must be capitalised
      SetFailureReason("the line starts with a letter, but it isn't capitalised");
      return false;
    }

    // now verify each word of the room name
    var words = line.Split(_wordSeparators, StringSplitOptions.RemoveEmptyEntries);
    if (words.Length < 1)
    {
      // we must have some words
      SetFailureReason("there are no words on the line");
      return false;
    }

    var maxWordLength = 0;
    var wordCountWithAllCaps = 0;
    foreach (var word in words)
    {
      if (!IsRoomDescriptionWord(word))
      {
        // all words must look room description esque for this to be a room name
        SetFailureReason(
          "the word \"{0}\" doesn't look like a room description word{1}{2}{3}",
          word,
          HaveFailureReason() ? " (" : string.Empty,
          GetFailureReason(),
          HaveFailureReason() ? ")" : string.Empty);
        return false;
      }

      if (!StartsWithCapitalOrNonLetter(word) && word.Length >= 4)
      {
        // all longish words must start with a capital or non letter.
        SetFailureReason(
          "all words longer than {0} letters, such as \"{1}\", must start with a capital or non letter",
          4,
          word);
        return false;
      }

      maxWordLength = Math.Max(maxWordLength, word.Length);
      if (IsAllCaps(word)) ++wordCountWithAllCaps;
    }

    if (words.Length > 1 && maxWordLength < 3)
    {
      // we must have at least one word over n letters long
      SetFailureReason("there must be at least {0} word(s) over {1} letter(s) long", 1, 3);
      return false;
    }

    if (wordCountWithAllCaps == words.Length)
    {
      // at least one word must not be all caps
      SetFailureReason("at least one word must not be all caps");
      return false;
    }

    ClearFailureReason();
    name = line;
    return true;
  }

  private bool IsRoomDescriptionWord(string word)
  {
    if (string.IsNullOrEmpty(word))
    {
      // there must be a word
      SetFailureReason("the word contains no text");
      return false;
    }

    if (!char.IsLetterOrDigit(word[0]) && word[0] != '#')
    {
      // the first character must be a letter or a digit
      SetFailureReason("the word must begin with a letter or a digit");
      return false;
    }

    ClearFailureReason();
    return true;
  }

  private bool StartsWithCapitalOrNonLetter(string word)
  {
    if (string.IsNullOrEmpty(word))
      // there must be a word
      return false;
    if (char.IsLetter(word[0]) && !char.IsUpper(word[0]))
      // if it starts with a letter, it must start with a capital letter
      return false;
    return true;
  }

  private bool IsAllCaps(string word)
  {
    return word.ToUpper() == word;
  }

  internal bool ExtractParagraph(List<string> lines, int lineIndex, out string paragraph)
  {
    paragraph = null;
    while (lineIndex < lines.Count)
    {
      var line = lines[lineIndex].Trim();
      if (line == "[Previous turn undone.]")
      {
        // Ignore undo reports. This will have the effect of making this room
        // look like a non-verbose room.
        ++lineIndex;
        continue;
      }

      if (line.Length == 0)
        // we hit a blank line; give up
        break;

      string unused;
      if (IsPrompt(line, out unused) || ExtractRoomName(line, lineIndex > 0 ? lines[lineIndex - 1] : null, out unused))
        // we hit a prompt or a room name; give up
        break;

      if (paragraph == null)
        paragraph = line;
      else
        paragraph = string.Format("{0} {1}", paragraph, line);

      if (line.Length < 65)
        // we hit a short line; assume the end of the paragraph
        break;

      ++lineIndex;
    }

    return paragraph != null;
  }

  private Room FindRoom(string roomName, string roomDescription, string line)
  {
    return _canvas.FindRoom(roomName, roomDescription, line, (n, d, r) => Match(r, n, d));
  }

  private bool? Match(Room room, string name, string description)
  {
    if (room == null)
      // never match a null room; this shouldn't happen anyway
      return false;
    if (room.Name != name) return false;

    if (!_settings.VerboseTranscript)
      // transcript is not verbose;
      // must assume room with same name is same room,
      // otherwise the user has to disambiguate potentially without descriptions, and very frequently.
      return true;

    if (_settings.AssumeRoomsWithSameNameAreSameRoom)
      // ignore the description
      return true;

    if (room.MatchDescription(description))
      // the descriptions match; they're the same room
      return true;

    // the descriptions don't match; they may or may not be the same room
    return null;
  }

  private void NowInRoom(Room room)
  {
    _lastKnownRoom = room;
    _canvas.SelectRoom(room);
  }

  private void DeduceExitsFromDescription(Room room, string description)
  {
    if (!_settings.GuessExits)
      // we're disabled; do nothing
      return;

    if (string.IsNullOrEmpty(description))
      // we don't have a description to work from
      return;

    // lowercase the description
    description = description.ToLowerInvariant();

    // search it for the names of compass directions
    foreach (var pair in _namesForExitsInRoomDescriptions)
    {
      var directions = pair.Key;
      var namesOfExits = pair.Value;
      foreach (var directionName in namesOfExits)
      {
        var index = description.IndexOf(directionName);
        if (index != -1)
          // we found one
          if (index == 0 || !char.IsLetterOrDigit(description[index - 1]))
            // it's not the middle/end of a longer word
            if (index + directionName.Length == description.Length ||
                !char.IsLetterOrDigit(description[index + directionName.Length]))
              // it's not the middle/start of a longer word
              // add all relevant exits
              foreach (var direction in directions)
                _canvas.AddExitStub(room, direction);
      }
    }
  }

  private async Task ProcessTranscriptText(List<string> lines, CancellationToken token)
  {
    string previousLine = null;
    for (var index = 0; index < lines.Count; ++index)
    {
      token.ThrowIfCancellationRequested();
      var line = lines[index];
      string roomName;
      if (ExtractRoomName(line, previousLine, out roomName))
      {
        string roomDescription;
        ExtractParagraph(lines, index + 1, out roomDescription);

        // work out which room the transcript is referring to here, asking them if necessary
        var room = FindRoom(roomName, roomDescription, line);
        token.ThrowIfCancellationRequested();
        if (room == null)
        {
          // new room
          if (_lastKnownRoom != null && _lastMoveDirection != null)
          {
            // player moved to new room
            // is there already a connection in that direction?
            var otherRoom = _lastKnownRoom
                            .GetConnections(CompassPointHelper.GetCompassDirection(_lastMoveDirection.Value))
                            .FirstOrDefault()?.GetTargetRoom();
            if (otherRoom != null)
            {
              var decision = _chooseConflictingRoom(otherRoom, roomName);
              token.ThrowIfCancellationRequested();
              switch (decision)
              {
                case AutomapSameDirectionResult.KeepRoom1:
                  room = otherRoom;
                  break;
                case AutomapSameDirectionResult.KeepRoom2:
                  room = _canvas.CreateRoom(_lastKnownRoom, _lastMoveDirection.Value, roomName, line);
                  _canvas.Connect(_lastKnownRoom, _lastMoveDirection.Value, room, _settings.AssumeTwoWayConnections);
                  _canvas.RemoveRoom(otherRoom);
                  break;
                case AutomapSameDirectionResult.KeepBoth:
                  room = _canvas.CreateRoom(_lastKnownRoom, _lastMoveDirection.Value, roomName, line);
                  _canvas.Connect(_lastKnownRoom, _lastMoveDirection.Value, room, _settings.AssumeTwoWayConnections);
                  break;
                default:
                  throw new ArgumentOutOfRangeException();
              }
            }
            else
            {
              // if not added already, add room to map; and join it up to the previous one
              room = _canvas.CreateRoom(_lastKnownRoom, _lastMoveDirection.Value, roomName, line);
              _canvas.Connect(_lastKnownRoom, _lastMoveDirection.Value, room, _settings.AssumeTwoWayConnections);
              Trace(
                "{0}: {1} is now {2} from {3}.",
                FormatTranscriptLineForDisplay(line),
                roomName,
                _lastMoveDirection.Value.ToString().ToLower(),
                _lastKnownRoom.Name);
            }
          }
          else
          {
            if (_firstRoom || _gameName == roomName)
            {
              // most likely this is the game title
              _firstRoom = false;
              _gameName = roomName;
              await WaitForStep(token);
            }
            else
            {
              // player teleported to new room;
              // don't connect it up, as we don't know how they got there
              room = _canvas.CreateRoom(_lastKnownRoom, roomName);
              if (_lastKnownRoom == null) room.IsStartRoom = true;
              Trace("{0}: teleported to new room, {1}.", FormatTranscriptLineForDisplay(line), roomName);
              await WaitForStep(token);
            }
          }

          if (room != null)
          {
            DeduceExitsFromDescription(room, roomDescription);
            NowInRoom(room);
          }

          await WaitForStep(token);
        }
        else if (room != _lastKnownRoom)
        {
          // player moved to existing room
          if (_lastKnownRoom != null && _lastMoveDirection != null)
          {
            // player moved sensibly; ensure rooms are connected up
            _canvas.Connect(_lastKnownRoom, _lastMoveDirection.Value, room, _settings.AssumeTwoWayConnections);
            Trace(
              "{0}: {1} is now {2} from {3}.",
              FormatTranscriptLineForDisplay(line),
              roomName,
              _lastMoveDirection.Value.ToString().ToLower(),
              _lastKnownRoom.Name);
          }

          NowInRoom(room);
          await WaitForStep(token);
        }
        else
        {
          // player didn't change rooms
          Trace("{0}: still in {1}.", FormatTranscriptLineForDisplay(line), _lastKnownRoom.Name);
        }

        // add this description if the room doesn't have it already
        if (room != null) room.AddDescription(roomDescription);

        // now forget the last movement direction we saw.
        // we'll still place any rooms we see before we see a movement direction,
        // but we won't join them up to this room.
        // we might end up caring if, for example, the user gives multiple commands at one prompt,
        // or they're moved to one room and then teleported to another.
        _lastMoveDirection = null;
      }
      else
      {
        Trace(
          "{0}: {1}{2}{3}",
          FormatTranscriptLineForDisplay(line),
          HaveFailureReason() ? "not a room name because " : string.Empty,
          GetFailureReason(),
          HaveFailureReason() ? "." : string.Empty);
      }

      previousLine = line;
    }
  }

  private string FormatTranscriptLineForDisplay(string line)
  {
    var displayLine = line;
    const int maxDisplayLineLength = 60;
    if (displayLine.Length > maxDisplayLineLength)
      displayLine = displayLine.Substring(0, maxDisplayLineLength - 3) + "...";
    while (displayLine.Length < maxDisplayLineLength) displayLine += " ";
    return "|" + displayLine;
  }

  private void ProcessPromptCommand(string command)
  {
    // unless we find one, this command does not involve moving in a given direction
    _lastMoveDirection = null;

    // first process trizbort commands
    if (command.ToUpper().StartsWith(_settings.AddRegionCommand.ToUpper()))
    {
      var regionName = command.Substring(_settings.AddRegionCommand.Length).Trim();

      if (!string.IsNullOrEmpty(regionName) && _lastKnownRoom != null)
      {
        // region already exists, just set the room to it
        if (Settings.Regions.Find(p => p.RegionName.Equals(regionName, StringComparison.OrdinalIgnoreCase)) == null)
          Settings.Regions.Add(
            new Region { RegionName = regionName, TextColor = Settings.Color[Colors.Subtitle], RColor = Color.White });
        _lastKnownRoom.Region = regionName;
      }

      return;
    }

    if (command.ToUpper().StartsWith(_settings.AddObjectCommand.ToUpper()))
    {
      // the user wants to add an object to the map
      var objectName = command.Substring(_settings.AddObjectCommand.Length).Trim();

      if (!string.IsNullOrEmpty(objectName) && _lastKnownRoom != null)
      {
        if (!string.IsNullOrEmpty(_lastKnownRoom.Objects))
        {
          var alreadyExists = false;
          foreach (var line in _lastKnownRoom.Objects.Replace("\r", string.Empty).Split(
                     new[] { '\n' },
                     StringSplitOptions.RemoveEmptyEntries))
            if (StringComparer.InvariantCultureIgnoreCase.Compare(line.Trim(), objectName) == 0)
            {
              alreadyExists = true;
              break;
            }

          if (!alreadyExists) _lastKnownRoom.Objects += "\r\n" + objectName;
        }
        else
        {
          _lastKnownRoom.Objects = objectName;
        }
      }

      return;
    }


    // TODO: We entirely don't handle "go east. n. s then w." etc. and I don't see an easy way of doing so.

    // split the command into individual words
    var parts = command.Split(_wordSeparators, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0)
      // there's no command left over
      return;

    // strip out words we consider fluff, like "to".
    var words = new List<string>();
    foreach (var word in parts)
    {
      var stripWord = false;
      foreach (var strippable in _wordsToStripFromCommands)
        if (StringComparer.InvariantCultureIgnoreCase.Compare(word, strippable) == 0)
        {
          stripWord = true;
          break;
        }

      if (word[0] == '[')
        for (var temp = 1; temp < word.Length; temp++)
        {
          if (word[temp] == ']')
          {
            stripWord = true;
            break;
          }

          if (word[temp] < '0' || word[temp] > '9') break;
        }

      if (stripWord) continue;

      words.Add(word);
    }

    // if the command starts with a word meaning "go", remove it.
    if (words.Count > 0)
      foreach (var wordMeaningGo in _wordsMeaningGo)
        if (StringComparer.InvariantCultureIgnoreCase.Compare(words[0], wordMeaningGo) == 0)
        {
          words.RemoveAt(0);
          break;
        }

    if (words.Count == 2 && words[0].Equals("trypush"))
      foreach (var pair in _namesForMovementCommands)
      {
        var direction = pair.Key;
        var wordsForDirection = pair.Value;
        foreach (var wordForDirection in wordsForDirection)
          if (StringComparer.InvariantCultureIgnoreCase.Compare(words[1], wordForDirection) == 0)
          {
            var delta = CompassPointHelper.GetAutomapDirectionVector(CompassPointHelper.GetCompassDirection(direction));
            delta.X *= _lastKnownRoom.Width + Settings.PreferredDistanceBetweenRooms;
            delta.X += _lastKnownRoom.X;
            delta.Y *= _lastKnownRoom.Height + Settings.PreferredDistanceBetweenRooms;
            delta.Y += _lastKnownRoom.Y;
            _lastKnownRoom.Position = Settings.Snap(delta);
          }
      }

    // look for custom tb trizbort commands
    if (words.Count > 0)
      if (words[0].Equals("tb", StringComparison.OrdinalIgnoreCase))
        if (words.Count > 1)
        {
          if (words[1].Equals("dotted", StringComparison.OrdinalIgnoreCase)) UseDottedConnection = true;

          if (words[1].Equals("exit", StringComparison.OrdinalIgnoreCase))
            if (words.Count > 2)
            {
              var direction = GetDirection(words[2]);
              if (direction != MappableDirection.None)
                _canvas.AddExitStub(_lastKnownRoom, direction);
            }

          if (words[1].Equals("noexit", StringComparison.OrdinalIgnoreCase))
            if (words.Count > 2)
            {
              var direction = GetDirection(words[2]);
              if (direction != MappableDirection.None)
                _canvas.RemoveExitStub(_lastKnownRoom, direction);
            }
        }


    // we should have just one word left
    if (words.Count != 1)
      // we have no words left, or more than one
      return;

    // the word we have left is hopefully a direction
    var possibleDirection = words[0];

    // work out which direction it is, if any
    foreach (var pair in _namesForMovementCommands)
    {
      var direction = pair.Key;
      var wordsForDirection = pair.Value;
      foreach (var wordForDirection in wordsForDirection)
        if (StringComparer.InvariantCultureIgnoreCase.Compare(possibleDirection, wordForDirection) == 0)
        {
          // aha, we know which direction it was
          _lastMoveDirection = direction;

          // remove any stub exit in this direction;
          // we'll either add a proper connection shortly, or they player can't go that way
          _canvas.RemoveExitStub(_lastKnownRoom, _lastMoveDirection.Value);

          return;
        }
    }

    // the word wasn't for a direction after all.
  }

  private MappableDirection GetDirection(string possibleDirection)
  {
    foreach (var pair in _namesForMovementCommands)
    {
      var direction = pair.Key;
      var wordsForDirection = pair.Value;
      foreach (var wordForDirection in wordsForDirection)
        if (StringComparer.InvariantCultureIgnoreCase.Compare(possibleDirection, wordForDirection) == 0)
          return direction;
    }

    return MappableDirection.None;
  }


  #region Private Member Variables

  private Room _lastKnownRoom;
  private bool _firstRoom = true;
  private string _gameName = string.Empty;

  private MappableDirection? _lastMoveDirection;

  private IAutomapCanvas _canvas;

  private AutomapSettings _settings;
  private volatile bool _stepNow;
  private string _failureReason = string.Empty;

  private CancellationTokenSource _tokenSource;

  private static readonly char[] _wordSeparators = { ' ' };
  private static readonly string[] _roomDecorativeSuffixMarkers = { ",", "(", "[", "{", " - " };
  private static readonly string[] _promptMarkers = { ">" };
  private const int MaxCharactersBeforePrompt = 42;

  //static readonly string[] s_commandSeparators = { ".", " then " };
  private static readonly string[] _wordsToStripFromCommands = { "the", "a", "to", "on" };
  private static readonly string[] _wordsMeaningGo = { "go", "walk", "move" };

  private static readonly Dictionary<MappableDirection, List<string>> _namesForMovementCommands = new() {
    { MappableDirection.North, new List<string> { "north", "n", "fore", "f" } },
    { MappableDirection.South, new List<string> { "south", "s", "aft", "a" } },
    { MappableDirection.East, new List<string> { "east", "e", "starboard", "sb" } },
    { MappableDirection.West, new List<string> { "west", "w", "port", "p" } },
    { MappableDirection.NorthEast, new List<string> { "northeast", "ne" } },
    { MappableDirection.SouthEast, new List<string> { "southeast", "se" } },
    { MappableDirection.SouthWest, new List<string> { "southwest", "sw" } },
    { MappableDirection.NorthWest, new List<string> { "northwest", "nw" } },
    { MappableDirection.Up, new List<string> { "up", "u" } },
    { MappableDirection.Down, new List<string> { "down", "d" } },
    { MappableDirection.In, new List<string> { "in", "inside" } },
    { MappableDirection.Out, new List<string> { "out", "outside" } }
  };

  private static readonly Dictionary<List<MappableDirection>, List<string>> _namesForExitsInRoomDescriptions = new() {
    {
      new List<MappableDirection> { MappableDirection.North },
      new List<string> { "north", "northward", "fore", "northern" }
    }, {
      new List<MappableDirection> { MappableDirection.South },
      new List<string> { "south", "southward", "aft", "southern" }
    }, {
      new List<MappableDirection> { MappableDirection.East },
      new List<string> { "east", "eastward", "starboard", "eastern" }
    }, {
      new List<MappableDirection> { MappableDirection.West }, new List<string> { "west", "westward", "port", "western" }
    }, {
      new List<MappableDirection> { MappableDirection.NorthEast },
      new List<string> { "northeast", "northeastward", "northeastern" }
    }, {
      new List<MappableDirection> { MappableDirection.SouthEast },
      new List<string> { "southeast", "southeastward", "southeastern" }
    }, {
      new List<MappableDirection> { MappableDirection.SouthWest },
      new List<string> { "southwest", "southwestward", "southwestern" }
    }, {
      new List<MappableDirection> { MappableDirection.NorthWest },
      new List<string> { "northwest", "northwest", "northwestern" }
    }, {
      new List<MappableDirection> { MappableDirection.Up },
      new List<string> { "up", "upward", "upwards", "ascend", "above" }
    }, {
      new List<MappableDirection> { MappableDirection.Down },
      new List<string> { "down", "downward", "downwards", "descend", "below" }
    }, {
      new List<MappableDirection> {
        MappableDirection.North, MappableDirection.South, MappableDirection.East, MappableDirection.West,
        MappableDirection.NorthEast, MappableDirection.NorthWest, MappableDirection.SouthEast,
        MappableDirection.SouthWest
      },
      new List<string> { "all directions", "every direction" }
    }
  };

  #endregion

  #region Static Initialization

  // This implements the static initialization design pattern for a singleton.
  // It prevents having two automap instances trying to write to the map simultaneously.

  private Automap() : this(ShowError, ChooseRoom)
  {
  }

  internal Automap(
    Action<string, string> reportError,
    Func<Room, string, AutomapSameDirectionResult> chooseConflictingRoom)
  {
    _reportError = reportError;
    _chooseConflictingRoom = chooseConflictingRoom;
    Status = "Automap is not running.";
  }

  private static void ShowError(string message, string title)
  {
    UserInteraction.ShowMessage(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
  }

  private static AutomapSameDirectionResult ChooseRoom(Room existing, string name)
  {
    using var dialog = new AutomapRoomSameDirectionDialog { Room1 = existing, Room2 = name };
    UserInteraction.ShowDialog(dialog);
    return dialog.Result;
  }

  private void InitializeRun(IAutomapCanvas canvas, AutomapSettings settings)
  {
    _canvas = canvas;
    _settings = settings;
    _firstRoom = true;
    _lastKnownRoom = null;
    _lastMoveDirection = null;
    _gameName = string.Empty;
    _stepNow = false;
    UseDottedConnection = false;
  }

  public static Automap Instance { get; } = new();

  #endregion

  #region Public Interface

  public void Step()
  {
    _stepNow = true;
  }

  public void RunToCompletion()
  {
    _settings.SingleStep = false;
    Step();
  }

  public string Status { get; private set; }

  public bool Running => _tokenSource != null && !_tokenSource.IsCancellationRequested;

  public void Stop()
  {
    if (_tokenSource != null)
      try
      {
        _tokenSource.Cancel();
      }
      catch (ObjectDisposedException)
      {
        _tokenSource = null;
      }

    Status = "Automap is not running.";
  }


  internal async Task StartCl(IAutomapCanvas canvas, AutomapSettings settings)
  {
    if (Running) Stop();
    InitializeRun(canvas, settings);
    using var tokenSource = new CancellationTokenSource();
    _tokenSource = tokenSource;
    Debug.Assert(
      _settings.AssumeRoomsWithSameNameAreSameRoom || _settings.VerboseTranscript,
      "Must assume rooms with same name are same room unless transcript is verbose.");
    Status = "Automapping has started.";
    var lines = new List<string>();
    var ownsRun = false;
    try
    {
      using (var stream = File.Open(_settings.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      {
        using (var reader = new StreamReader(stream))
        {
          while (!reader.EndOfStream)
          {
            var line = reader.ReadLine();
            lines.Add(line);
          }
        }
      }

      // keep track of lines we read between here and the next prompt
      var linesBetweenPrompts = new List<string>();
      Status = "Automapping is processing the transcript.";

      foreach (var line in lines)
      {
        tokenSource.Token.ThrowIfCancellationRequested();
        string command;
        if (IsPrompt(line, out command))
        {
          // this is a prompt line

          // let's process everything leading up to it since the last prompt, but not necessarily this new prompt itself
          await ProcessTranscriptText(linesBetweenPrompts, tokenSource.Token);
          tokenSource.Token.ThrowIfCancellationRequested();

          // we've now dealt with all lines to this point
          linesBetweenPrompts.Clear();

          // process the next command
          ProcessPromptCommand(command);

          Trace(
            "{0}: {1}{2}",
            FormatTranscriptLineForDisplay(line),
            _lastMoveDirection != null ? "GO " : string.Empty,
            _lastMoveDirection != null ? _lastMoveDirection.Value.ToString().ToUpperInvariant() : string.Empty);
        }
        else
        {
          // this line isn't a prompt;
          // hang onto it for now in case we meet a prompt shortly.
          linesBetweenPrompts.Add(line);
        }
      }

      await ProcessTranscriptText(linesBetweenPrompts, tokenSource.Token);
    }
    catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
    {
      if (ReferenceEquals(_tokenSource, tokenSource)) Status = "Automap is not running.";
      return;
    }
    catch (IOException ex)
    {
      // couldn't read from the file
      Trace("Automap: Error reading line in file.\nError message: " + ex.Message);
      _reportError("Error opening transcript file:\n" + ex.Message + "\n\nAutomapping halted.", "File Error");
      Status = "Automapping halted.";
      return;
    }
    catch (UnauthorizedAccessException)
    {
      _reportError(
        "Could not gain access to the transcript file. Your interpreter may be restricting access to it. Try again in a few minutes " +
        "or with scripting off in your interpreter.\n\nAutomapping halted.",
        "Access Error");
      Status = "Automapping halted.";
      return;
    }
    finally
    {
      ownsRun = ReferenceEquals(_tokenSource, tokenSource);
      if (ownsRun) _tokenSource = null;
    }

    Trace("Automap: Gentle thread exit.");
    if (ownsRun) Status = "Automapping has completed.";
  }

  private List<string> GetTextToNextPrompt(PeekingStreamReader reader)
  {
    var line = reader.PeekReadLine();
    string command;
    var list = new List<string>();
    while (!IsPrompt(line, out command))
    {
      list.Add(line);
      line = reader.PeekReadLine();
    }

    return list;
  }


  internal async Task Start(IAutomapCanvas canvas, AutomapSettings settings)
  {
    if (Running) Stop();

    InitializeRun(canvas, settings);
    using var tokenSource = new CancellationTokenSource();
    _tokenSource = tokenSource;
    Debug.Assert(
      _settings.AssumeRoomsWithSameNameAreSameRoom || _settings.VerboseTranscript,
      "Must assume rooms with same name are same room unless transcript is verbose.");
    Status = "Automapping has started.";

    try
    {
      using var stream = File.Open(_settings.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var reader = new PeekingStreamReader(stream);
      var lastline = "";

      if (_settings.ContinueTranscript)
        while (!reader.EndOfStream)
          lastline = await reader.ReadLineAsync();

      // keep track of lines we read between here and the next prompt
      var linesBetweenPrompts = new List<string>();
      Status = "Automapping is processing the transcript.";

      var promptLine = string.Empty;
      var line = string.Empty;
      var atFileEnd = false;
      // loop until cancelled
      while (true)
      {
        tokenSource.Token.ThrowIfCancellationRequested();
        if (_settings.ContinueTranscript)
        {
          line = lastline;
          _settings.ContinueTranscript = false;
        }
        else
        {
          // ...read a line of text
          line = await WaitForNewLine(reader, tokenSource.Token);
          atFileEnd = reader.EndOfStream; // store this now so that it's still valid when we use it below
        }

        //Trace("[" + line + "]");
        string command;
        if (IsPrompt(line, out command))
        {
          // this is a prompt line

          // let's process everything leading up to it since the last prompt, but not necessarily this new prompt itself
          await ProcessTranscriptText(linesBetweenPrompts, tokenSource.Token);
          tokenSource.Token.ThrowIfCancellationRequested();

          // we've now dealt with all lines to this point
          linesBetweenPrompts.Clear();

          // handle the case where we're at the end of the file, waiting for user input
          if (atFileEnd)
            // we've already read the prompt, now just read the command when the player enters it
            command = (await WaitForNewLine(reader, tokenSource.Token)).Trim();

          //                  var nextParagraph = getTextToNextPrompt(reader);

          // process the next command
          tokenSource.Token.ThrowIfCancellationRequested();
          ProcessPromptCommand(command);

          //                  if (command.ToUpper().Equals("EXITS"))
          //                  {
          //                    // parse the exits command.
          //                    DeduceExitsFromDescription(m_lastKnownRoom, String.Join(" ", nextParagraph));
          //                  }

          Trace(
            "{0}: {1}{2}",
            FormatTranscriptLineForDisplay(line),
            _lastMoveDirection != null ? "GO " : string.Empty,
            _lastMoveDirection != null ? _lastMoveDirection.Value.ToString().ToUpperInvariant() : string.Empty);
        }
        else
        {
          // this line isn't a prompt;
          // hang onto it for now in case we meet a prompt shortly.
          linesBetweenPrompts.Add(line);
        }
      }
    }
    catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
    {
      if (ReferenceEquals(_tokenSource, tokenSource)) Status = "Automap is not running.";
    }
    catch (IOException ex)
    {
      // couldn't read from the file
      Trace("Automap: Error reading line in file.\nError message: " + ex.Message);
      _reportError("Error opening transcript file:\n" + ex.Message + "\n\nAutomapping halted.", "File Error");
      Status = "Automapping halted.";
    }
    catch (UnauthorizedAccessException)
    {
      _reportError(
        "Could not gain access to the transcript file. Your interpreter may be restricting access to it. Try again in a few minutes " +
        "or with scripting off in your interpreter.\n\nAutomapping halted.",
        "Access Error");
      Status = "Automapping halted.";
    }
    finally
    {
      if (ReferenceEquals(_tokenSource, tokenSource)) _tokenSource = null;
    }
  }

  #endregion

  #region Debugging

  [Conditional("DEBUG")]
  private void SetFailureReason(string format, params object[] args)
  {
    _failureReason = string.Format(format, args);
  }

  [Conditional("DEBUG")]
  private void ClearFailureReason()
  {
    _failureReason = string.Empty;
  }

  private string GetFailureReason()
  {
    return _failureReason;
  }

  private bool HaveFailureReason()
  {
    return !string.IsNullOrEmpty(_failureReason);
  }

  [Conditional("DEBUG")]
  private static void Trace(string format, params object[] args)
  {
    Debug.WriteLine(format, args);
  }

  #endregion
}