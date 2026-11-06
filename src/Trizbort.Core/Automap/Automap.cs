using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Trizbort.Automap.Utility;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Automap;

public sealed class Automap : IAutomapController
{
  private bool _dontAskAboutAmbiguities;
  private readonly Action<string, string> _reportError;
  public bool UseDottedConnection { get; set; }


  private async Task WaitForStep(CancellationToken token)
  {
    token.ThrowIfCancellationRequested();
    // for diagnostic purposes, allow single stepping
    if (!_settings.SingleStep) return;

    if (_stepNow)
    {
      _stepNow = false;
      return;
    }

    Status = "Automapping is waiting for you to step through it (with F11.)";
    while (!_stepNow) await Task.Delay(50, token);
    token.ThrowIfCancellationRequested();
    _stepNow = false;
  }

  private async Task<string> WaitForNewLine(
    TextReader reader,
    CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var line = await reader.ReadLineAsync(cancellationToken);

      if (line is not null)
        return line;

      Status = "Automapping is waiting for more text.";

      await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
    }
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
        var indexOfMarker = FindDecorativeSuffix(line, decorativeSuffixMarker);
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

    if (line.Count(character => character == '"') % 2 != 0)
    {
      SetFailureReason("the line contains unmatched quotation marks");
      return false;
    }

    var unquotedLine = line.Trim('"');
    if (unquotedLine.Length == 0 || !char.IsLetterOrDigit(unquotedLine[unquotedLine.Length - 1]))
    {
      // the last character of the room description must be a number or a letter
      SetFailureReason("after stripping suffixes from the line, it didn't end with a letter or a number");
      return false;
    }

    if (!char.IsLetterOrDigit(unquotedLine[0]))
    {
      // the first character of the room description must be a number or a letter
      SetFailureReason("the line must start with a letter or a number");
      return false;
    }

    if (!StartsWithCapitalOrNonLetter(unquotedLine))
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
    var hasGridReference = false;
    var inCommaQualifier = false;
    foreach (var rawWord in words)
    {
      var word = rawWord.Trim('"');
      // a dash between words, as in "Hall - Upstairs", kept because the text after it is capitalised
      if (word == "-") continue;

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

      if (!inCommaQualifier && !StartsWithCapitalOrNonLetter(word) && word.Length >= 4)
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
      hasGridReference |= IsGridReference(word);
      inCommaQualifier |= word.Contains(',');
    }

    if (words.Length > 1 && maxWordLength < 3 && !hasGridReference)
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

  /// <summary>
  ///   A comma introduces decoration only for a lower-case location phrase such as "on the bed".
  ///   Other comma qualifiers belong to the name; a dash still introduces decoration when followed by lower case.
  /// </summary>
  private static int FindDecorativeSuffix(string line, string marker)
  {
    var lowerCaseOnly = marker == " - ";
    var index = line.IndexOf(marker, StringComparison.Ordinal);
    if (marker == ",")
    {
      while (index >= 0)
      {
        var words = line.Substring(index + 1).Split(_wordSeparators, StringSplitOptions.RemoveEmptyEntries);
        // Keep the existing sentence guard for long lower-case continuations of wrapped prose.
        if (line.Length - index > 30 && words.Length > 0 && char.IsLower(words[0][0]))
          return index;
        if (words.Length >= 3 &&
            words[0] is "on" or "in" or "under" or "inside" or "outside" or "by" or "near" or "at" or
              "behind" or "beneath" or "above" or "below" &&
            words[1] is "the" or "a" or "an")
          return index;
        index = line.IndexOf(marker, index + marker.Length, StringComparison.Ordinal);
      }

      return -1;
    }

    while (index >= 0 && lowerCaseOnly)
    {
      var after = line.Substring(index + marker.Length).TrimStart();
      if (after.Length == 0 || !(char.IsUpper(after[0]) || char.IsDigit(after[0]))) break;
      index = line.IndexOf(marker, index + marker.Length, StringComparison.Ordinal);
    }

    return index;
  }

  /// <summary>A line that only appears when a game (or its transcript) starts: Infocom/Frotz and Inform banners.</summary>
  internal static bool IsSessionStartLine(string line)
  {
    if (string.IsNullOrWhiteSpace(line)) return false;
    return System.Text.RegularExpressions.Regex.IsMatch(
      line,
      @"^\s*(Here begins a transcript of interaction with|Start of a transcript of)\b|\bRelease \d+ / Serial number \d+",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
  }

  /// <summary>The line a transcript file starts with (Infocom/Frotz, Inform).</summary>
  internal static bool IsTranscriptStartLine(string line)
  {
    if (string.IsNullOrWhiteSpace(line)) return false;
    return System.Text.RegularExpressions.Regex.IsMatch(
      line,
      @"^\s*(Here begins a transcript of interaction with|Start of a transcript of)\b",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
  }

  private static readonly string[] _bannerCommands = { "version", "about", "info", "credits", "help", "license", "licence" };

  /// <summary>Commands that print the game's banner without starting the game again.</summary>
  internal static bool PrintsBanner(string command)
  {
    if (string.IsNullOrWhiteSpace(command)) return false;
    var firstWord = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    return _bannerCommands.Contains(firstWord, StringComparer.OrdinalIgnoreCase);
  }

  private void StartedAgain()
  {
    Trace("Automap: the game started again; back in the first room.");
    _lastMoveDirection = null;
    _lastJourneyCommand = null;
    _pendingStubRemoval = null;
    _seenCommand = false;
    var start = _startRoom != null ? _canvas.ResolveRoom(_startRoom) : null;
    _lastKnownRoom = start;
    if (start != null) _canvas.SelectRoom(start);
  }

  /// <summary>
  ///   True when the paragraph after a heading reads like a game's opening banner rather than a room description
  ///   (release, serial number, copyright, "interactive fiction", "by Author").
  /// </summary>
  internal static bool LooksLikeBanner(string paragraph)
  {
    if (string.IsNullOrWhiteSpace(paragraph)) return false;
    return System.Text.RegularExpressions.Regex.IsMatch(
      paragraph,
      @"\b(release|serial number|copyright|interactive fiction|trademark|inform)\b|\(c\)|©",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant) ||
      System.Text.RegularExpressions.Regex.IsMatch(paragraph, @"\bby [A-Z]");
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
    // a grid reference such as "Y2" (Colossal Cave) is a name, not shouting
    if (IsGridReference(word)) return false;
    return word.ToUpper() == word;
  }

  private static bool IsGridReference(string word)
  {
    return word.Count(char.IsLetter) == 1 && word.Any(char.IsDigit);
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

  private async Task<Room> FindRoomAsync(
    string roomName,
    string roomDescription,
    string line,
    CancellationToken token)
  {
    var result = _canvas.FindRooms(roomName, roomDescription, (n, d, r) => Match(r, n, d));
    if (result.Certain != null) return result.Certain;

    var maybes = result.Maybes;
    if (maybes == null || maybes.Count == 0) return null;
    if (_dontAskAboutAmbiguities) return maybes[0];
    if (maybes.Count == 1 && (string.IsNullOrEmpty(roomDescription) || maybes[0].PrimaryDescription == null))
      return maybes[0];

    var choice = await Prompts.DisambiguateRoomAsync(line, roomDescription, maybes, token);
    token.ThrowIfCancellationRequested();
    if (choice.AutoChooseRest)
    {
      _dontAskAboutAmbiguities = true;
      return maybes[0];
    }

    return choice.NewRoom ? null : choice.Chosen;
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

    if (string.IsNullOrEmpty(description))
      // a brief revisit (name only): it may be this room; a single candidate is taken as it
      return null;

    if (room.Descriptions.Any(known => SameFirstSentence(known, description)))
      // the descriptions start the same way; they're the same room. Only the first sentence counts: games add
      // changing text after it ("The door is open.", an object you dropped), which doesn't make it another room.
      return true;

    // the descriptions don't match; they may or may not be the same room
    return null;
  }

  /// <summary>Whether two room descriptions have the same first sentence, ignoring case and spacing.</summary>
  internal static bool SameFirstSentence(string one, string two) =>
    string.Equals(FirstSentence(one), FirstSentence(two), StringComparison.OrdinalIgnoreCase);

  /// <summary>
  ///   The first sentence of a description, with runs of spaces and line breaks made single spaces: up to the first
  ///   ".", "!" or "?" (and any closing quote or bracket) that ends the text or is followed by a space.
  /// </summary>
  internal static string FirstSentence(string description)
  {
    if (string.IsNullOrWhiteSpace(description)) return string.Empty;
    var text = Regex.Replace(description.Trim(), @"\s+", " ");
    var end = Regex.Match(text, @"[.!?][""'\u201D\u2019)\]]*(?=\s|$)");
    return end.Success ? text.Substring(0, end.Index + end.Length) : text;
  }

  private static Room OtherEnd(Connection connection, Room room)
  {
    var source = connection.GetSourceRoom();
    var target = connection.GetTargetRoom();
    return source == room ? target : source;
  }

  // Undo and redo replace room objects and the user may delete rooms, so look the remembered room up again by id.
  private void RefreshLastKnownRoom()
  {
    if (_lastKnownRoom == null || _canvas == null) return;
    _lastKnownRoom = _canvas.ResolveRoom(_lastKnownRoom);
  }

  private void RemovePendingStub()
  {
    if (_pendingStubRemoval is not { } pending) return;
    _pendingStubRemoval = null;
    var stubRoom = _canvas.ResolveRoom(pending.Room);
    if (stubRoom != null) _canvas.RemoveExitStub(stubRoom, pending.Direction);
  }

  /// <summary>A tried direction that led nowhere: remove its stub as a step of its own.</summary>
  private async Task FlushPendingStubRemoval(CancellationToken token)
  {
    if (_pendingStubRemoval is not { } pending) return;
    using (await BeginBatchAsync("Automap: " + pending.Room.Name, token)) RemovePendingStub();
  }

  /// <summary>
  ///   Opens an undo step for automap's changes, waiting while the user is part-way through a change of their own
  ///   (a drag), so automap's work never becomes part of the user's step.
  /// </summary>
  private async Task<IDisposable> BeginBatchAsync(string title, CancellationToken token)
  {
    var waited = false;
    while (true)
    {
      token.ThrowIfCancellationRequested();
      var batch = _canvas.BeginBatch(title);
      if (batch != null)
      {
        if (waited) Trace("Automap: carried on after the user's change.");
        return batch;
      }

      if (!waited) Trace("Automap: waiting for the user to finish a change.");
      waited = true;
      await Task.Delay(50, token);
    }
  }

  private void NowInRoom(Room room)
  {
    // the first room of the run, whether automap made it or found it already on the map
    _startRoom ??= room;
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
    // A new game session in the same transcript (Frotz appends each session to the file): after the player has
    // typed commands, a fresh opening banner means the game started again, so the player is back in the first room.
    // "version" (and the like) prints the release banner too without starting again, so a banner alone only counts
    // after other commands; the transcript's own "Here begins a transcript" line always counts.
    if (_seenCommand && (lines.Any(IsTranscriptStartLine) ||
                         (lines.Any(IsSessionStartLine) && !PrintsBanner(_lastCommand))))
      StartedAgain();

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

        // work out which room the transcript is referring to here, asking them if necessary.
        // All prompts happen before the batch below opens: the user can't undo while a batch is open.
        RefreshLastKnownRoom();
        var room = await FindRoomAsync(roomName, roomDescription, line, token);
        token.ThrowIfCancellationRequested();
        RefreshLastKnownRoom();
        if (room != null) room = _canvas.ResolveRoom(room);

        AutomapSameDirectionResult? decision = null;
        Room otherRoom = null;
        if (room == null && _lastKnownRoom != null && _lastMoveDirection != null)
        {
          // is there already a connection in that direction?
          // The room at the other end of a connection already leaving this way. (The reference took the
          // connection's target, which is this very room when the connection arrived here from elsewhere.)
          // A one-way connection that only arrives here says nothing about where this exit leads (two-way
          // connections off: going east to here is not a promise that west leads back), so it doesn't count.
          otherRoom = _lastKnownRoom
                      .GetConnections(CompassPointHelper.GetCompassDirection(_lastMoveDirection.Value))
                      .Where(connection => !AutomapCanvas.IsCommandJoin(connection))
                      .Where(connection => connection.Flow != ConnectionFlow.OneWay ||
                                           connection.GetSourceRoom() == _lastKnownRoom)
                      .Select(connection => OtherEnd(connection, _lastKnownRoom))
                      .FirstOrDefault(room => room != null && room != _lastKnownRoom);
          if (otherRoom != null)
          {
            decision = await Prompts.ChooseSameDirectionAsync(otherRoom, roomName, token);
            token.ThrowIfCancellationRequested();
            if (decision == AutomapSameDirectionResult.Cancel)
            {
              // treat like a cancelled run
              Stop();
              token.ThrowIfCancellationRequested();
              throw new OperationCanceledException(token);
            }

            // the user may have changed the map while the prompt was open
            RefreshLastKnownRoom();
            otherRoom = _canvas.ResolveRoom(otherRoom);
          }
        }

        var waits = 0;
        using (await BeginBatchAsync("Automap: " + roomName, token))
        {
          // the user may have undone something while automap waited for them to finish a change
          RefreshLastKnownRoom();
          if (room != null) room = _canvas.ResolveRoom(room);
          otherRoom = _canvas.ResolveRoom(otherRoom);
          RemovePendingStub();
          if (room == null)
          {
            // new room
            if (_lastKnownRoom != null && _lastMoveDirection != null)
            {
              // player moved to new room
              if (otherRoom != null && decision.HasValue)
              {
                switch (decision.Value)
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
              // The game's title: a heading before the player's first command with no description or with banner
              // text under it, or the same title shown again later (a restart). Anything else is a room, including
              // the first heading after "look" (GitHub #25, I-042).
              var isTitle = !_seenCommand
                ? string.IsNullOrWhiteSpace(roomDescription) || LooksLikeBanner(roomDescription)
                : _gameName.Length > 0 && _gameName == roomName;
              if (isTitle)
              {
                // most likely this is the game title
                _gameName = roomName;
                waits++;
              }
              else
              {
                // player arrived somewhere new without a compass direction: a teleport, a magic word, "enter
                // boat"... Join it from where they were, labelled with what they typed, if we know.
                room = _canvas.CreateRoom(_lastKnownRoom, roomName);
                if (_lastKnownRoom == null)
                {
                  room.IsStartRoom = true;
                  _startRoom ??= room;
                  Trace("{0}: first room, {1}.", FormatTranscriptLineForDisplay(line), roomName);
                }
                else if (_lastJourneyCommand != null)
                {
                  _canvas.ConnectByCommand(_lastKnownRoom, room, _lastJourneyCommand);
                  Trace("{0}: {1} reached from {2} by \"{3}\".", FormatTranscriptLineForDisplay(line), roomName,
                    _lastKnownRoom.Name, _lastJourneyCommand);
                }
                else
                {
                  Trace("{0}: teleported to new room, {1}.", FormatTranscriptLineForDisplay(line), roomName);
                }
                waits++;
              }
            }

            if (room != null)
            {
              DeduceExitsFromDescription(room, roomDescription);
              NowInRoom(room);
            }

            waits++;
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
            else if (_lastKnownRoom != null && _lastJourneyCommand != null)
            {
              // back to a known room by something other than a direction ("xyzzy" again)
              _canvas.ConnectByCommand(_lastKnownRoom, room, _lastJourneyCommand);
              Trace("{0}: {1} reached from {2} by \"{3}\".", FormatTranscriptLineForDisplay(line), roomName,
                _lastKnownRoom.Name, _lastJourneyCommand);
            }

            NowInRoom(room);
            waits++;
          }
          else
          {
            // player didn't change rooms
            Trace("{0}: still in {1}.", FormatTranscriptLineForDisplay(line), _lastKnownRoom.Name);
          }

          // add this description if the room doesn't have it already
          if (room != null) room.AddDescription(roomDescription);

          // let the rooms around the player make space for each other, in this move's undo step
          if (_settings.AutoTidy && _lastKnownRoom != null) _canvas.TidyAround(_lastKnownRoom);
        }

        // single stepping waits outside the batch, so the user can undo while we wait
        for (var wait = 0; wait < waits; wait++) await WaitForStep(token);

        // now forget the last movement direction we saw.
        // we'll still place any rooms we see before we see a movement direction,
        // but we won't join them up to this room.
        // we might end up caring if, for example, the user gives multiple commands at one prompt,
        // or they're moved to one room and then teleported to another.
        _lastMoveDirection = null;
        _lastJourneyCommand = null;
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

  private async Task ProcessPromptCommand(string command, CancellationToken token)
  {
    if (!string.IsNullOrWhiteSpace(command)) _seenCommand = true;
    _lastCommand = command;
    RefreshLastKnownRoom();
    await FlushPendingStubRemoval(token);
    var title = _lastKnownRoom != null ? "Automap: " + _lastKnownRoom.Name : "Automap";
    using (await BeginBatchAsync(title, token))
    {
      RefreshLastKnownRoom();
      ProcessPromptCommandCore(command);
    }

    // a command that isn't a direction may still take the player somewhere ("xyzzy", "enter boat")
    _lastJourneyCommand = _lastMoveDirection == null && IsPossibleJourney(command) ? command.Trim() : null;
  }

  private static readonly string[] _notJourneys =
  {
    "undo", "restore", "load", "restart", "quit", "q", "save", "look", "l", "again", "g", "oops", "script", "unscript",
    "transcript"
  };

  /// <summary>
  ///   A command that could take the player to another room without a compass direction. Not the automap commands,
  ///   and not undo/restore/restart and the like, which move the player without a journey.
  /// </summary>
  private bool IsPossibleJourney(string command)
  {
    if (string.IsNullOrWhiteSpace(command)) return false;
    var trimmed = command.Trim();
    if (trimmed.StartsWith(_settings.AddRegionCommand, StringComparison.OrdinalIgnoreCase) ||
        trimmed.StartsWith(_settings.AddObjectCommand, StringComparison.OrdinalIgnoreCase))
      return false;
    var firstWord = trimmed.Split(_wordSeparators, StringSplitOptions.RemoveEmptyEntries)[0];
    return !_notJourneys.Contains(firstWord, StringComparer.OrdinalIgnoreCase);
  }

  private void ProcessPromptCommandCore(string command)
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
            if (_lastKnownRoom == null) continue;
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

          // remove any stub exit in this direction: we'll either add a proper connection shortly, or the player
          // can't go that way. Done with the response, so the move is one undo step.
          if (_lastKnownRoom != null) _pendingStubRemoval = (_lastKnownRoom, _lastMoveDirection.Value);

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
  private bool _seenCommand;
  private string _lastJourneyCommand;
  private Room _startRoom;

  /// <summary>The last command typed at a prompt.</summary>
  private string _lastCommand;

  /// <summary>
  ///   The exit stub to remove for the direction the player just tried. It is removed with the response, so a move
  ///   and its stub are one undo step; if no room follows (the way is blocked) it goes at the next command.
  /// </summary>
  private (Room Room, MappableDirection Direction)? _pendingStubRemoval;
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

  /// <summary>
  ///   Deprecated adapter kept for tests: called (message, title) in addition to
  ///   <see cref="IEditorPrompts.ShowMessageAsync" />. The app reports errors through the prompts only.
  /// </summary>
  public static Action<string, string> ErrorReporter { get; set; }

  public IEditorPrompts Prompts { get; set; } = NonInteractiveEditorPrompts.Instance;

  public Automap() : this((msg, title) => ErrorReporter?.Invoke(msg, title), NonInteractiveEditorPrompts.Instance)
  {
  }

  public Automap(Action<string, string> reportError, IEditorPrompts prompts)
  {
    _reportError = reportError;
    Prompts = prompts ?? NonInteractiveEditorPrompts.Instance;
    Status = "Automap is not running.";
  }

  private void InitializeRun(IAutomapCanvas canvas, AutomapSettings settings)
  {
    _canvas = canvas;
    _settings = settings;
    _canvas.MakeSpaceForNewRooms = settings.AutoTidy;
    _seenCommand = false;
    _startRoom = null;
    _lastCommand = null;
    _pendingStubRemoval = null;
    _lastKnownRoom = null;
    _lastMoveDirection = null;
    _lastJourneyCommand = null;
    _gameName = string.Empty;
    _stepNow = false;
    _dontAskAboutAmbiguities = false;
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

  public string Status
  {
    get => _status;
    private set
    {
      _status = value;
      StatusChanged?.Invoke(value);
    }
  }

  private string _status;

  public event Action<string> StatusChanged;

  public event Action<Exception> AutomapFailed;

  public bool IsRunning => Running;

  public bool SingleStep => _settings.SingleStep;

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


  private const string AccessErrorMessage =
    "Could not gain access to the transcript file. Your interpreter may be restricting access to it. Try again in a few minutes " +
    "or with scripting off in your interpreter.\n\nAutomapping halted.";

  /// <summary>
  ///   Writes an error log (the error, the transcript, the settings and automap's reasoning for the last lines) and
  ///   returns a plain message for the player that says where the log is. No .NET wording: it means nothing to them.
  /// </summary>
  private string UnexpectedErrorMessage(Exception ex)
  {
    var details = new System.Text.StringBuilder();
    details.AppendLine("Automap error");
    details.AppendLine("Transcript: " + _settings.FileName);
    details.AppendLine(
      $"Settings: verbose transcript {_settings.VerboseTranscript}, same name = same room {_settings.AssumeRoomsWithSameNameAreSameRoom}, " +
      $"two-way connections {_settings.AssumeTwoWayConnections}, guess exits {_settings.GuessExits}, " +
      $"continue transcript {_settings.ContinueTranscript}, single step {_settings.SingleStep}");
    details.AppendLine("Current room: " + (_lastKnownRoom?.Name ?? "(none)"));
    details.AppendLine();
    details.AppendLine($"Automap's reasoning for the last {RecentTraceLines} lines (most recent last):");
    details.AppendLine(RecentTrace());

    var logPath = Trizbort.Domain.Recovery.RecoveryStore.WriteCrashLog(ex, AppVersion(), details: details.ToString());
    var message = "Automap hit a problem it couldn't handle and has stopped. Your map is fine up to this point.";
    return logPath == null
      ? message + "\n\nThe details could not be saved."
      : message + "\n\nThe details are saved in:\n" + logPath +
        "\n\nPlease send that file (and your transcript if you can) to " + IssuesUrl + " so it can be fixed.";
  }

  private const string IssuesUrl = "https://github.com/leeoades/trizbort/issues";

  private static string AppVersion()
  {
    var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? typeof(Automap).Assembly;
    var version = System.Reflection.CustomAttributeExtensions
                    .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(assembly)
                    ?.InformationalVersion
                  ?? assembly.GetName().Version?.ToString()
                  ?? "unknown";
    var plus = version.IndexOf('+');
    return (plus > 0 ? version[..plus] : version) + " (" + assembly.GetName().Name + ")";
  }

  /// <summary>
  ///   Stops the run after an error: marks it finished, sets the status, raises <see cref="AutomapFailed" /> and shows
  ///   one message. Never throws.
  /// </summary>
  private async Task FailAsync(CancellationTokenSource tokenSource, Exception exception, string title, string message)
  {
    if (ReferenceEquals(_tokenSource, tokenSource)) _tokenSource = null;
    Status = "Automapping halted.";
    try
    {
      AutomapFailed?.Invoke(exception);
    }
    catch (Exception)
    {
      // a faulty subscriber must not mask the original error
    }

    _reportError?.Invoke(message, title);
    try
    {
      await Prompts.ShowMessageAsync(title, message);
    }
    catch (Exception)
    {
      // reporting is best effort
    }
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
    var ownsRun = false;
    try
    {
      List<string> lines;
      using (var stream = File.Open(_settings.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      using (var reader = new StreamReader(stream))
      {
        lines = new List<string>();
        while (await reader.ReadLineAsync(tokenSource.Token) is { } line)
          lines.Add(line);
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
          await ProcessPromptCommand(command, tokenSource.Token);

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
      await FailAsync(tokenSource, ex, "File Error", "Error opening transcript file:\n" + ex.Message + "\n\nAutomapping halted.");
      return;
    }
    catch (UnauthorizedAccessException ex)
    {
      await FailAsync(tokenSource, ex, "Access Error", AccessErrorMessage);
      return;
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      await FailAsync(tokenSource, ex, "Automap Error", UnexpectedErrorMessage(ex));
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


  public async Task Start(IAutomapCanvas canvas, AutomapSettings settings)
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
      {
        string next;
        while ((next = await reader.ReadLineAsync(tokenSource.Token)) != null)
          lastline = next;
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
        if (_settings.ContinueTranscript)
        {
          line = lastline;
          _settings.ContinueTranscript = false;
        }
        else
        {
          // ...read a line of text
          line = await WaitForNewLine(reader, tokenSource.Token);
          atFileEnd = await reader.IsAtEndAsync(tokenSource.Token); // store this now so that it's still valid when we use it below
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

          // handle the case where we're at the end of the file, waiting for user input: the prompt has been
          // written but the command hasn't. When the prompt line already holds the command (">north"), keep it: an
          // interpreter writes that before the game's reply, so the next line is the reply, not the command.
          if (atFileEnd && string.IsNullOrWhiteSpace(command))
            // we've already read the prompt, now just read the command when the player enters it
            command = (await WaitForNewLine(reader, tokenSource.Token)).Trim();

          //                  var nextParagraph = getTextToNextPrompt(reader);

          // process the next command
          tokenSource.Token.ThrowIfCancellationRequested();
          await ProcessPromptCommand(command, tokenSource.Token);

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
      await FailAsync(tokenSource, ex, "File Error", "Error opening transcript file:\n" + ex.Message + "\n\nAutomapping halted.");
    }
    catch (UnauthorizedAccessException ex)
    {
      await FailAsync(tokenSource, ex, "Access Error", AccessErrorMessage);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      await FailAsync(tokenSource, ex, "Automap Error", UnexpectedErrorMessage(ex));
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

  private const int RecentTraceLines = 200;
  private static readonly Queue<string> _recentTrace = new();

  /// <summary>
  ///   Explains what automap made of each transcript line. Written to debug output in Debug builds, and the last
  ///   <see cref="RecentTraceLines" /> lines are always kept for the error log, in every build.
  /// </summary>
  private static void Trace(string format, params object[] args)
  {
    string text;
    try
    {
      text = args == null || args.Length == 0 ? format : string.Format(format, args);
    }
    catch (FormatException)
    {
      text = format;
    }

    lock (_recentTrace)
    {
      _recentTrace.Enqueue(text);
      while (_recentTrace.Count > RecentTraceLines) _recentTrace.Dequeue();
    }

    Debug.WriteLine(text);
  }

  private static string RecentTrace()
  {
    lock (_recentTrace)
    {
      return string.Join(Environment.NewLine, _recentTrace);
    }
  }

  #endregion
}
