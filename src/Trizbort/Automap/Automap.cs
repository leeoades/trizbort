using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Trizbort.Automap
{
  public sealed class Automap
  {
    private readonly Action<string, string> _reportError;
    private readonly Func<Room, string, AutomapSameDirectionResult> _chooseConflictingRoom;
    public bool UseDottedConnection { get; set; } = false;


    private async Task waitForStep(CancellationToken token)
    {
      token.ThrowIfCancellationRequested();
      // for diagnostic purposes, allow single stepping
      if (_mSettings.SingleStep && !_mStepNow)
      {
        Status = "Automapping is waiting for you to step through it (with F11.)";
        while (!_mStepNow)
        {
          await Task.Delay(50, token);
        }
        token.ThrowIfCancellationRequested();
        _mStepNow = false;
      }
    }

    private async Task<string> waitForNewLine(StreamReader reader, CancellationToken token)
    {
      token.ThrowIfCancellationRequested();
      if (reader.EndOfStream)
      {
        Status = "Automapping is waiting for more text.";
      }
      while (reader.EndOfStream)
      {
        await Task.Delay(500, token);
      }
      return await reader.ReadLineAsync();
    }

    internal bool IsPrompt(string line, out string typedCommand)
    {
      if (line == null) {
        typedCommand = null;
        return false;
      }
      foreach (var promptMarker in SPromptMarkers)
      {
        var startIndex = line.LastIndexOf(promptMarker);
        if (startIndex != -1 && startIndex < MAX_CHARACTERS_BEFORE_PROMPT)
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
        setFailureReason("the previous line, if any, must be blank or a prompt");
        return false;
      }

      if (string.IsNullOrEmpty(line))
      {
        // blank lines clearly aren't room names
        setFailureReason("the line is blank");
        return false;
      }

      // look for leading whitespace
      if (line.TrimStart().Length != line.Length)
      {
        // lines which start with whitespace aren't room names
        setFailureReason("the line starts with whitespace");
        return false;
      }

      // trim whitespace
      line = line.Trim();
      if (line.Length == 0)
      {
        // we just ran out of line
        setFailureReason("the line only contains whitespace");
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
        foreach (var decorativeSuffixMarker in SRoomDecorativeSuffixMarkers)
        {
          var indexOfMarker = line.IndexOf(decorativeSuffixMarker, StringComparison.Ordinal);
          if (indexOfMarker >= 0)
          {
            var suffixLength = line.Length - indexOfMarker;
            if (suffixLength > 30)
            {
              // this looks more like punctuation in a sentence, not a suffix
              setFailureReason("this line looks more like a sentence");
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
        setFailureReason("after stripping suffixes from the line, it was blank");
        return false;
      }

      if (!char.IsLetterOrDigit(line[line.Length - 1]))
      {
        // the last character of the room description must be a number or a letter
        setFailureReason("after stripping suffixes from the line, it didn't end with a letter or a number");
        return false;
      }

      if (!char.IsLetterOrDigit(line[0]))
      {
        // the first character of the room description must be a number or a letter
        setFailureReason("the line must start with a letter or a number");
        return false;
      }

      if (!startsWithCapitalOrNonLetter(line))
      {
        // if the first character of the room description is a letter, it must be capitalised
        setFailureReason("the line starts with a letter, but it isn't capitalised");
        return false;
      }

      // now verify each word of the room name
      var words = line.Split(SWordSeparators, StringSplitOptions.RemoveEmptyEntries);
      if (words.Length < 1)
      {
        // we must have some words
        setFailureReason("there are no words on the line");
        return false;
      }

      var maxWordLength = 0;
      var wordCountWithAllCaps = 0;
      foreach (var word in words)
      {
        if (!isRoomDescriptionWord(word))
        {
          // all words must look room description esque for this to be a room name
          setFailureReason("the word \"{0}\" doesn't look like a room description word{1}{2}{3}", word, haveFailureReason() ? " (" : string.Empty, getFailureReason(), haveFailureReason() ? ")" : string.Empty);
          return false;
        }
        if (!startsWithCapitalOrNonLetter(word) && word.Length >= 4)
        {
          // all longish words must start with a capital or non letter.
          setFailureReason("all words longer than {0} letters, such as \"{1}\", must start with a capital or non letter", 4, word);
          return false;
        }
        maxWordLength = Math.Max(maxWordLength, word.Length);
        if (isAllCaps(word))
        {
          ++wordCountWithAllCaps;
        }
      }

      if (words.Length > 1 && maxWordLength < 3)
      {
        // we must have at least one word over n letters long
        setFailureReason("there must be at least {0} word(s) over {1} letter(s) long", 1, 3);
        return false;
      }

      if (wordCountWithAllCaps == words.Length)
      {
        // at least one word must not be all caps
        setFailureReason("at least one word must not be all caps");
        return false;
      }

      clearFailureReason();
      name = line;
      return true;
    }

    private bool isRoomDescriptionWord(string word)
    {
      if (string.IsNullOrEmpty(word))
      {
        // there must be a word
        setFailureReason("the word contains no text");
        return false;
      }
      if (!char.IsLetterOrDigit(word[0]) && word[0] != '#')
      {
        // the first character must be a letter or a digit
        setFailureReason("the word must begin with a letter or a digit");
        return false;
      }
      clearFailureReason();
      return true;
    }

    private bool startsWithCapitalOrNonLetter(string word)
    {
      if (string.IsNullOrEmpty(word))
      {
        // there must be a word
        return false;
      }
      if (char.IsLetter(word[0]) && !char.IsUpper(word[0]))
      {
        // if it starts with a letter, it must start with a capital letter
        return false;
      }
      return true;
    }

    private bool isAllCaps(string word)
    {
      return (word.ToUpper() == word);
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
        {
          // we hit a blank line; give up
          break;
        }

        string unused;
        if (IsPrompt(line, out unused) || ExtractRoomName(line, lineIndex > 0 ? lines[lineIndex - 1] : null, out unused))
        {
          // we hit a prompt or a room name; give up
          break;
        }

        if (paragraph == null)
        {
          paragraph = line;
        }
        else
        {
          paragraph = string.Format("{0} {1}", paragraph, line);
        }

        if (line.Length < 65)
        {
          // we hit a short line; assume the end of the paragraph
          break;
        }

        ++lineIndex;
      }

      return paragraph != null;
    }

    private Room findRoom(string roomName, string roomDescription, string line)
    {
      return _mCanvas.FindRoom(roomName, roomDescription, line, (n, d, r) => match(r, n, d));
    }

    private bool? match(Room room, string name, string description)
    {
      if (room == null)
      {
        // never match a null room; this shouldn't happen anyway
        return false;
      }
      if (room.Name != name)
      {
        return false;
      }

      if (!_mSettings.VerboseTranscript)
      {
        // transcript is not verbose;
        // must assume room with same name is same room,
        // otherwise the user has to disambiguate potentially without descriptions, and very frequently.
        return true;
      }

      if (_mSettings.AssumeRoomsWithSameNameAreSameRoom)
      {
        // ignore the description
        return true;
      }

      if (room.MatchDescription(description))
      {
        // the descriptions match; they're the same room
        return true;
      }

      // the descriptions don't match; they may or may not be the same room
      return null;
    }

    private void nowInRoom(Room room)
    {
      _mLastKnownRoom = room;
      _mCanvas.SelectRoom(room);
    }

    private void deduceExitsFromDescription(Room room, string description)
    {
      if (!_mSettings.GuessExits)
      {
        // we're disabled; do nothing
        return;
      }

      if (string.IsNullOrEmpty(description))
      {
        // we don't have a description to work from
        return;
      }

      // lowercase the description
      description = description.ToLowerInvariant();

      // search it for the names of compass directions
      foreach (var pair in SNamesForExitsInRoomDescriptions)
      {
        var directions = pair.Key;
        var namesOfExits = pair.Value;
        foreach (var directionName in namesOfExits)
        {
          var index = description.IndexOf(directionName);
          if (index != -1)
          {
            // we found one
            if (index == 0 || !char.IsLetterOrDigit(description[index - 1]))
            {
              // it's not the middle/end of a longer word
              if (index + directionName.Length == description.Length || !char.IsLetterOrDigit(description[index + directionName.Length]))
              {
                // it's not the middle/start of a longer word

                // add all relevant exits
                foreach (var direction in directions)
                {
                  _mCanvas.AddExitStub(room, direction);
                }
              }
            }
          }
        }
      }
    }

    private async Task processTranscriptText(List<string> lines, CancellationToken token)
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
          var room = findRoom(roomName, roomDescription, line);
          token.ThrowIfCancellationRequested();
          if (room == null)
          {
            // new room
            if (_mLastKnownRoom != null && _mLastMoveDirection != null)
            {
              // player moved to new room
              // is there already a connection in that direction?
              var mOtherRoom = _mLastKnownRoom.GetConnections(CompassPointHelper.GetCompassDirection(_mLastMoveDirection.Value)).FirstOrDefault()?.GetTargetRoom();
              if (mOtherRoom != null)
              {
                var decision = _chooseConflictingRoom(mOtherRoom, roomName);
                token.ThrowIfCancellationRequested();
                switch (decision)
                {
                  case AutomapSameDirectionResult.KeepRoom1:
                    room = mOtherRoom;
                    break;
                  case AutomapSameDirectionResult.KeepRoom2:
                    room = _mCanvas.CreateRoom(_mLastKnownRoom, _mLastMoveDirection.Value, roomName, line);
                    _mCanvas.Connect(_mLastKnownRoom, _mLastMoveDirection.Value, room, _mSettings.AssumeTwoWayConnections);
                    _mCanvas.RemoveRoom(mOtherRoom);
                    break;
                  case AutomapSameDirectionResult.KeepBoth:
                    room = _mCanvas.CreateRoom(_mLastKnownRoom, _mLastMoveDirection.Value, roomName, line);
                    _mCanvas.Connect(_mLastKnownRoom, _mLastMoveDirection.Value, room, _mSettings.AssumeTwoWayConnections);
                    break;
                  default:
                    throw new ArgumentOutOfRangeException();
                }
              }
              else
              {

                // if not added already, add room to map; and join it up to the previous one
                room = _mCanvas.CreateRoom(_mLastKnownRoom, _mLastMoveDirection.Value, roomName, line);
                _mCanvas.Connect(_mLastKnownRoom, _mLastMoveDirection.Value, room, _mSettings.AssumeTwoWayConnections);
                trace("{0}: {1} is now {2} from {3}.", formatTranscriptLineForDisplay(line), roomName, _mLastMoveDirection.Value.ToString().ToLower(), _mLastKnownRoom.Name);
              }
            }
            else
            {
              if ((_mFirstRoom) || (_mGameName == roomName))
              {
                // most likely this is the game title
                _mFirstRoom = false;
                _mGameName = roomName;
                await waitForStep(token);
              }
              else
              {
                // player teleported to new room;
                // don't connect it up, as we don't know how they got there
                room = _mCanvas.CreateRoom(_mLastKnownRoom, roomName);
                if (_mLastKnownRoom == null) { room.IsStartRoom = true; }
                trace("{0}: teleported to new room, {1}.", formatTranscriptLineForDisplay(line), roomName);
                await waitForStep(token);
              }
            }
            if (room != null)
            {
              deduceExitsFromDescription(room, roomDescription);
              nowInRoom(room);
            }
            await waitForStep(token);
          }
          else if (room != _mLastKnownRoom)
          {
            // player moved to existing room
            if (_mLastKnownRoom != null && _mLastMoveDirection != null)
            {
              // player moved sensibly; ensure rooms are connected up
              _mCanvas.Connect(_mLastKnownRoom, _mLastMoveDirection.Value, room, _mSettings.AssumeTwoWayConnections);
              trace("{0}: {1} is now {2} from {3}.", formatTranscriptLineForDisplay(line), roomName, _mLastMoveDirection.Value.ToString().ToLower(), _mLastKnownRoom.Name);
            }

            nowInRoom(room);
            await waitForStep(token);
          }
          else
          {
            // player didn't change rooms
            trace("{0}: still in {1}.", formatTranscriptLineForDisplay(line), _mLastKnownRoom.Name);
          }

          // add this description if the room doesn't have it already
          if (room != null) room.AddDescription(roomDescription);

          // now forget the last movement direction we saw.
          // we'll still place any rooms we see before we see a movement direction,
          // but we won't join them up to this room.
          // we might end up caring if, for example, the user gives multiple commands at one prompt,
          // or they're moved to one room and then teleported to another.
          _mLastMoveDirection = null;
        }
        else
        {
          trace("{0}: {1}{2}{3}", formatTranscriptLineForDisplay(line), haveFailureReason() ? "not a room name because " : string.Empty, getFailureReason(), haveFailureReason() ? "." : string.Empty);
        }
        previousLine = line;
      }
    }

    private string formatTranscriptLineForDisplay(string line)
    {
      var displayLine = line;
      const int maxDisplayLineLength = 60;
      if (displayLine.Length > maxDisplayLineLength)
      {
        displayLine = displayLine.Substring(0, maxDisplayLineLength - 3) + "...";
      }
      while (displayLine.Length < maxDisplayLineLength)
      {
        displayLine += " ";
      }
      return "|" + displayLine;
    }

    private void processPromptCommand(string command)
    {

      // unless we find one, this command does not involve moving in a given direction
      _mLastMoveDirection = null;

      // first process trizbort commands
      if (command.ToUpper().StartsWith(_mSettings.AddRegionCommand.ToUpper()))
      {
        var regionName = command.Substring(_mSettings.AddRegionCommand.Length).Trim();

        if (!string.IsNullOrEmpty(regionName) && _mLastKnownRoom != null)
        {
          // region already exists, just set the room to it
          if (Settings.Regions.Find(p => p.RegionName.Equals(regionName, StringComparison.OrdinalIgnoreCase)) == null)
          {
            Settings.Regions.Add(new Region { RegionName = regionName, TextColor = Settings.Color[Colors.Subtitle], RColor = System.Drawing.Color.White });
          }
          _mLastKnownRoom.Region = regionName;
        }

        return;
      }

      if (command.ToUpper().StartsWith(_mSettings.AddObjectCommand.ToUpper()))
      {
        // the user wants to add an object to the map
        var objectName = command.Substring(_mSettings.AddObjectCommand.Length).Trim();

        if (!string.IsNullOrEmpty(objectName) && _mLastKnownRoom != null)
        {
          if (!string.IsNullOrEmpty(_mLastKnownRoom.Objects))
          {
            var alreadyExists = false;
            foreach (var line in _mLastKnownRoom.Objects.Replace("\r", string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
              if (StringComparer.InvariantCultureIgnoreCase.Compare(line.Trim(), objectName) == 0)
              {
                alreadyExists = true;
                break;
              }
            }
            if (!alreadyExists)
            {
              _mLastKnownRoom.Objects += "\r\n" + objectName;
            }
          }
          else
          {
            _mLastKnownRoom.Objects = objectName;
          }
        }
        return;
      }


      // TODO: We entirely don't handle "go east. n. s then w." etc. and I don't see an easy way of doing so.

      // split the command into individual words
      var parts = command.Split(SWordSeparators, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
      {
        // there's no command left over
        return;
      }

      // strip out words we consider fluff, like "to".
      var words = new List<string>();
      foreach (var word in parts)
      {
        var stripWord = false;
        foreach (var strippable in SWordsToStripFromCommands)
        {
          if (StringComparer.InvariantCultureIgnoreCase.Compare(word, strippable) == 0)
          {
            stripWord = true;
            break;
          }
        }
        if (word[0] == '[')
        {
          for (var temp = 1; temp < word.Length; temp++)
          {
            if (word[temp] == ']')
            {
              stripWord = true;
              break;
            }
            if ((word[temp] < '0') || (word[temp] > '9')) { break; }
          }
        }

        if (stripWord)
        {
          continue;
        }

        words.Add(word);
      }

      // if the command starts with a word meaning "go", remove it.
      if (words.Count > 0)
      {
        foreach (var wordMeaningGo in SWordsMeaningGo)
        {
          if (StringComparer.InvariantCultureIgnoreCase.Compare(words[0], wordMeaningGo) == 0)
          {
            words.RemoveAt(0);
            break;
          }
        }
      }

      if ((words.Count == 2) && (words[0].Equals("trypush")))
      {
        foreach (var pair in SNamesForMovementCommands)
        {
          var direction = pair.Key;
          var wordsForDirection = pair.Value;
          foreach (var wordForDirection in wordsForDirection)
          {
            if (StringComparer.InvariantCultureIgnoreCase.Compare(words[1], wordForDirection) == 0)
            {
              Vector delta = CompassPointHelper.GetAutomapDirectionVector(CompassPointHelper.GetCompassDirection(direction));
              delta.X *= _mLastKnownRoom.Width + Settings.PreferredDistanceBetweenRooms;
              delta.X += _mLastKnownRoom.X;
              delta.Y *= _mLastKnownRoom.Height + Settings.PreferredDistanceBetweenRooms;
              delta.Y += _mLastKnownRoom.Y;
              _mLastKnownRoom.Position = Settings.Snap(delta);
            }
          }
        }
      }

      // look for custom tb trizbort commands
      if (words.Count > 0)
      {
        if (words[0].Equals("tb", StringComparison.OrdinalIgnoreCase))
        {
          if (words.Count > 1)
          {
            if (words[1].Equals("dotted", StringComparison.OrdinalIgnoreCase))
            {
              UseDottedConnection = true;
            }

            if (words[1].Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
              if (words.Count > 2)
              {
                var direction = getDirection(words[2]);
                if (direction != MappableDirection.None)
                  _mCanvas.AddExitStub(_mLastKnownRoom, direction);
              }
            }

            if (words[1].Equals("noexit", StringComparison.OrdinalIgnoreCase))
            {
              if (words.Count > 2)
              {
                var direction = getDirection(words[2]);
                if (direction != MappableDirection.None)
                  _mCanvas.RemoveExitStub(_mLastKnownRoom, direction);
              }
            }
          }


        }
      }



      // we should have just one word left
      if (words.Count != 1)
      {
        // we have no words left, or more than one
        return;
      }

      // the word we have left is hopefully a direction
      var possibleDirection = words[0];

      // work out which direction it is, if any
      foreach (var pair in SNamesForMovementCommands)
      {
        var direction = pair.Key;
        var wordsForDirection = pair.Value;
        foreach (var wordForDirection in wordsForDirection)
        {
          if (StringComparer.InvariantCultureIgnoreCase.Compare(possibleDirection, wordForDirection) == 0)
          {
            // aha, we know which direction it was
            _mLastMoveDirection = direction;

            // remove any stub exit in this direction;
            // we'll either add a proper connection shortly, or they player can't go that way
            _mCanvas.RemoveExitStub(_mLastKnownRoom, _mLastMoveDirection.Value);

            return;
          }
        }
      }

      // the word wasn't for a direction after all.
    }

    private MappableDirection getDirection(string possibleDirection)
    {
      foreach (var pair in SNamesForMovementCommands)
      {
        var direction = pair.Key;
        var wordsForDirection = pair.Value;
        foreach (var wordForDirection in wordsForDirection)
        {
          if (StringComparer.InvariantCultureIgnoreCase.Compare(possibleDirection, wordForDirection) == 0)
          {
            return direction;
          }
        }
      }
      return MappableDirection.None;
    }


    #region Private Member Variables

    private Room _mLastKnownRoom;
    private bool _mFirstRoom = true;
    private string _mGameName = string.Empty;

    private MappableDirection? _mLastMoveDirection;

    private IAutomapCanvas _mCanvas;

    private AutomapSettings _mSettings;
    private volatile bool _mStepNow;
    private string _sFailureReason = string.Empty;

    private CancellationTokenSource _mTokenSource;

    private static readonly char[] SWordSeparators = { ' ' };
    private static readonly string[] SRoomDecorativeSuffixMarkers = { ",", "(", "[", "{", " - " };
    private static readonly string[] SPromptMarkers = { ">" };
    private const int MAX_CHARACTERS_BEFORE_PROMPT = 42;

    //static readonly string[] s_commandSeparators = { ".", " then " };
    private static readonly string[] SWordsToStripFromCommands = { "the", "a", "to", "on" };
    private static readonly string[] SWordsMeaningGo = { "go", "walk", "move" };

    private static readonly Dictionary<MappableDirection, List<string>> SNamesForMovementCommands = new Dictionary<MappableDirection, List<string>>
    {
      {MappableDirection.North, new List<string> {"north", "n", "fore", "f"}},
      {MappableDirection.South, new List<string> {"south", "s", "aft", "a"}},
      {MappableDirection.East, new List<string> {"east", "e", "starboard", "sb"}},
      {MappableDirection.West, new List<string> {"west", "w", "port", "p"}},
      {MappableDirection.NorthEast, new List<string> {"northeast", "ne"}},
      {MappableDirection.SouthEast, new List<string> {"southeast", "se"}},
      {MappableDirection.SouthWest, new List<string> {"southwest", "sw"}},
      {MappableDirection.NorthWest, new List<string> {"northwest", "nw"}},
      {MappableDirection.Up, new List<string> {"up", "u"}},
      {MappableDirection.Down, new List<string> {"down", "d"}},
      {MappableDirection.In, new List<string> {"in", "inside"}},
      {MappableDirection.Out, new List<string> {"out", "outside"}}
    };

    private static readonly Dictionary<List<MappableDirection>, List<string>> SNamesForExitsInRoomDescriptions = new Dictionary<List<MappableDirection>, List<string>>
    {
      {new List<MappableDirection> {MappableDirection.North}, new List<string> {"north", "northward", "fore", "northern"}},
      {new List<MappableDirection> {MappableDirection.South}, new List<string> {"south", "southward", "aft", "southern"}},
      {new List<MappableDirection> {MappableDirection.East}, new List<string> {"east", "eastward", "starboard", "eastern"}},
      {new List<MappableDirection> {MappableDirection.West}, new List<string> {"west", "westward", "port", "western"}},
      {new List<MappableDirection> {MappableDirection.NorthEast}, new List<string> {"northeast", "northeastward", "northeastern"}},
      {new List<MappableDirection> {MappableDirection.SouthEast}, new List<string> {"southeast", "southeastward", "southeastern"}},
      {new List<MappableDirection> {MappableDirection.SouthWest}, new List<string> {"southwest", "southwestward", "southwestern"}},
      {new List<MappableDirection> {MappableDirection.NorthWest}, new List<string> {"northwest", "northwest", "northwestern"}},
      {new List<MappableDirection> {MappableDirection.Up}, new List<string> {"up", "upward", "upwards", "ascend", "above"}},
      {new List<MappableDirection> {MappableDirection.Down}, new List<string> {"down", "downward", "downwards", "descend", "below"}},
      {new List<MappableDirection> {MappableDirection.North, MappableDirection.South, MappableDirection.East, MappableDirection.West, MappableDirection.NorthEast, MappableDirection.NorthWest, MappableDirection.SouthEast, MappableDirection.SouthWest}, new List<string> {"all directions", "every direction"}}
    };

    #endregion

    #region Static Initialization

    // This implements the static initialization design pattern for a singleton.
    // It prevents having two automap instances trying to write to the map simultaneously.

    private Automap() : this(showError, chooseRoom) { }

    internal Automap(Action<string, string> reportError, Func<Room, string, AutomapSameDirectionResult> chooseConflictingRoom)
    {
      this._reportError = reportError;
      this._chooseConflictingRoom = chooseConflictingRoom;
      Status = "Automap is not running.";
    }

    private static void showError(string message, string title) =>
      UserInteraction.ShowMessage(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static AutomapSameDirectionResult chooseRoom(Room existing, string name) {
      using var dialog = new AutomapRoomSameDirectionDialog {Room1 = existing, Room2 = name};
      UserInteraction.ShowDialog(dialog);
      return dialog.Result;
    }

    private void initializeRun(IAutomapCanvas canvas, AutomapSettings settings) {
      _mCanvas = canvas;
      _mSettings = settings;
      _mFirstRoom = true;
      _mLastKnownRoom = null;
      _mLastMoveDirection = null;
      _mGameName = string.Empty;
      _mStepNow = false;
      UseDottedConnection = false;
    }

    public static Automap Instance { get; } = new Automap();

    #endregion

    #region Public Interface

    public void Step()
    {
      _mStepNow = true;
    }

    public void RunToCompletion()
    {
      _mSettings.SingleStep = false;
      Step();
    }

    public string Status { get; private set; }

    public bool Running
    {
      get { return (_mTokenSource != null && !_mTokenSource.IsCancellationRequested); }
    }

    public void Stop()
    {
      if (_mTokenSource != null)
      {
        try
        {
          _mTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
          _mTokenSource = null;
        }
      }

      Status = "Automap is not running.";
    }


    internal async Task StartCl(IAutomapCanvas canvas, AutomapSettings settings)
    {
      if (Running) Stop();
      initializeRun(canvas, settings);
      using var tokenSource = new CancellationTokenSource();
      _mTokenSource = tokenSource;
      Debug.Assert(_mSettings.AssumeRoomsWithSameNameAreSameRoom || _mSettings.VerboseTranscript, "Must assume rooms with same name are same room unless transcript is verbose.");
      Status = "Automapping has started.";
      List<string> lines = new List<string>();
      try
      {
        using (var stream = File.Open(_mSettings.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
          using (var reader = new StreamReader(stream))
          {
            while (!reader.EndOfStream)
            {
              var line = reader.ReadLine();
              lines.Add(line);

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
            await processTranscriptText(linesBetweenPrompts, tokenSource.Token);
            tokenSource.Token.ThrowIfCancellationRequested();

            // we've now dealt with all lines to this point
            linesBetweenPrompts.Clear();

            // process the next command
            processPromptCommand(command);

            trace("{0}: {1}{2}", formatTranscriptLineForDisplay(line), _mLastMoveDirection != null ? "GO " : string.Empty, _mLastMoveDirection != null ? _mLastMoveDirection.Value.ToString().ToUpperInvariant() : string.Empty);
          }
          else
          {
            // this line isn't a prompt;
            // hang onto it for now in case we meet a prompt shortly.
            linesBetweenPrompts.Add(line);
          }

        }
        await processTranscriptText(linesBetweenPrompts, tokenSource.Token);
      }
      catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
      {
        if (ReferenceEquals(_mTokenSource, tokenSource)) Status = "Automap is not running.";
        return;
      }
      catch (IOException ex)
      {
        // couldn't read from the file
        trace("Automap: Error reading line in file.\nError message: " + ex.Message);
        _reportError("Error opening transcript file:\n" + ex.Message + "\n\nAutomapping halted.", "File Error");
        Status = "Automapping halted.";
        return;
      }
      catch (UnauthorizedAccessException)
      {
        _reportError("Could not gain access to the transcript file. Your interpreter may be restricting access to it. Try again in a few minutes " +
                        "or with scripting off in your interpreter.\n\nAutomapping halted.", "Access Error");
        Status = "Automapping halted.";
        return;
      }
      finally {
        if (ReferenceEquals(_mTokenSource, tokenSource)) _mTokenSource = null;
      }

      trace("Automap: Gentle thread exit.");
      Status = "Automapping has completed.";
    }

    private List<string> getTextToNextPrompt(PeekingStreamReader reader)
    {
      string line = reader.PeekReadLine();
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
      if (Running)
      {
        Stop();
      }

      initializeRun(canvas, settings);
      using var tokenSource = new CancellationTokenSource();
      _mTokenSource = tokenSource;
      Debug.Assert(_mSettings.AssumeRoomsWithSameNameAreSameRoom || _mSettings.VerboseTranscript, "Must assume rooms with same name are same room unless transcript is verbose.");
      Status = "Automapping has started.";

      try {
        using var stream = File.Open(_mSettings.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new PeekingStreamReader(stream);
        var lastline = "";

        if (_mSettings.ContinueTranscript)
        {
          while (!reader.EndOfStream)
            lastline = await reader.ReadLineAsync();
        }

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
          if (_mSettings.ContinueTranscript)
          {
            line = lastline;
            _mSettings.ContinueTranscript = false;
          }
          else
          {
            // ...read a line of text
            line = await waitForNewLine(reader, tokenSource.Token);
            atFileEnd = reader.EndOfStream; // store this now so that it's still valid when we use it below
          }

          //Trace("[" + line + "]");
          string command;
          if (IsPrompt(line, out command))
          {
            // this is a prompt line

            // let's process everything leading up to it since the last prompt, but not necessarily this new prompt itself
            await processTranscriptText(linesBetweenPrompts, tokenSource.Token);
            tokenSource.Token.ThrowIfCancellationRequested();

            // we've now dealt with all lines to this point
            linesBetweenPrompts.Clear();

            // handle the case where we're at the end of the file, waiting for user input
            if (atFileEnd)
            {
              // we've already read the prompt, now just read the command when the player enters it
              command = (await waitForNewLine(reader, tokenSource.Token)).Trim();
            }

            //                  var nextParagraph = getTextToNextPrompt(reader);

            // process the next command
            tokenSource.Token.ThrowIfCancellationRequested();
            processPromptCommand(command);

            //                  if (command.ToUpper().Equals("EXITS"))
            //                  {
            //                    // parse the exits command.
            //                    DeduceExitsFromDescription(m_lastKnownRoom, String.Join(" ", nextParagraph));
            //                  }

            trace("{0}: {1}{2}", formatTranscriptLineForDisplay(line), _mLastMoveDirection != null ? "GO " : string.Empty, _mLastMoveDirection != null ? _mLastMoveDirection.Value.ToString().ToUpperInvariant() : string.Empty);
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
        if (ReferenceEquals(_mTokenSource, tokenSource)) Status = "Automap is not running.";
        return;
      }
      catch (IOException ex)
      {
        // couldn't read from the file
        trace("Automap: Error reading line in file.\nError message: " + ex.Message);
        _reportError("Error opening transcript file:\n" + ex.Message + "\n\nAutomapping halted.", "File Error");
        Status = "Automapping halted.";
        return;
      }
      catch (UnauthorizedAccessException)
      {
        _reportError("Could not gain access to the transcript file. Your interpreter may be restricting access to it. Try again in a few minutes " +
                        "or with scripting off in your interpreter.\n\nAutomapping halted.", "Access Error");
        Status = "Automapping halted.";
        return;
      }
      finally {
        if (ReferenceEquals(_mTokenSource, tokenSource)) _mTokenSource = null;
      }

    }

    #endregion

    #region Debugging

    [Conditional("DEBUG")]
    private void setFailureReason(string format, params object[] args)
    {
      _sFailureReason = string.Format(format, args);
    }

    [Conditional("DEBUG")]
    private void clearFailureReason()
    {
      _sFailureReason = string.Empty;
    }

    private string getFailureReason()
    {
      return _sFailureReason;
    }

    private bool haveFailureReason()
    {
      return !string.IsNullOrEmpty(_sFailureReason);
    }

    [Conditional("DEBUG")]
    private static void trace(string format, params object[] args)
    {
      Debug.WriteLine(format, args);
    }

    #endregion
  }
}