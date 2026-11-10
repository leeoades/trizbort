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

  /// <summary>
  ///   When playing move by move, wait before the player's next command until <see cref="PlayNextMove" /> or
  ///   <see cref="PlayToEnd" />. The map and the transcript read so far are up to date while it waits.
  /// </summary>
  private async Task<bool> WaitForNextMove(string command, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();
    if (Volatile.Read(ref _movesBack) > 0) return true;
    if (!_moveByMove) return false;
    if (TakeMove()) return false;

    NextCommand = command;
    Status = string.IsNullOrWhiteSpace(command)
      ? "Paused before the next move."
      : "Paused before the next move: " + command;
    while (_moveByMove && !TakeMove())
    {
      if (Volatile.Read(ref _movesBack) > 0) return true;
      await Task.Delay(50, token);
    }

    token.ThrowIfCancellationRequested();
    NextCommand = null;
    Status = "Automapping is processing the transcript.";
    return false;
  }

  /// <summary>Where automap was when it stopped before a command: enough to go back there and play on.</summary>
  private sealed record MovePoint(int LinesRead, int ScreenLines, Room Room, string Command, bool SeenCommand);

  private readonly List<MovePoint> _movePoints = new();
  private int _movesBack;
  private int _linesRead;

  /// <summary>Notes the place before a command, unless it is the place automap has just gone back to.</summary>
  private void NoteMovePoint(string command)
  {
    if (_movePoints.Count > 0 && _movePoints[^1].LinesRead == _linesRead) return;
    int screen;
    lock (_screen) screen = _screen.Count;
    _movePoints.Add(new MovePoint(_linesRead, screen, _lastKnownRoom, command, _seenCommand));
  }

  /// <summary>
  ///   Goes back the asked-for number of moves: the transcript is read again from that point, the map is left as it
  ///   is, and automap waits there. Returns the command it waits before.
  /// </summary>
  private async Task<(string Command, PeekingStreamReader Reader, Stream Stream)> GoBackAsync(
    PeekingStreamReader reader, Stream stream, CancellationToken token, bool fromTheEnd = false)
  {
    var back = Interlocked.Exchange(ref _movesBack, 0);
    // the last point is the one automap is waiting at now, unless it is waiting at the end of the transcript for the
    // game to go on, past the last move it played
    var target = Math.Max(0, _movePoints.Count - (fromTheEnd ? 0 : 1) - back);
    var point = _movePoints[target];
    _movePoints.RemoveRange(target + 1, _movePoints.Count - target - 1);

    reader.Dispose();
    stream.Dispose();
    stream = File.Open(_settings.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    reader = new PeekingStreamReader(stream);
    for (var i = 0; i < point.LinesRead; i++)
      if (await reader.ReadLineAsync(token) == null)
        break;
    _linesRead = point.LinesRead;

    lock (_screen)
      if (_screen.Count > point.ScreenLines)
        _screen.RemoveRange(point.ScreenLines, _screen.Count - point.ScreenLines);
    ScreenChanged?.Invoke();

    // the map stays as it is; automap carries on from the room it was in then, if it's still there
    _lastKnownRoom = point.Room == null ? null : _canvas.ResolveRoom(point.Room);
    // and the map shows that room as the one you're in again
    if (_lastKnownRoom != null) _canvas.SelectRoom(_lastKnownRoom);
    _lastMoveDirection = null;
    _lastJourneyCommand = null;
    _pendingStubRemoval = null;
    _seenCommand = point.SeenCommand;
    _moveByMove = true;
    Volatile.Write(ref _movesToPlay, 0);
    Trace("Automap: went back to before \"{0}\".", point.Command);
    return (point.Command, reader, stream);
  }

  private bool TakeMove()
  {
    while (true)
    {
      var moves = Volatile.Read(ref _movesToPlay);
      if (moves <= 0) return false;
      if (Interlocked.CompareExchange(ref _movesToPlay, moves - 1, moves) == moves) return true;
    }
  }

  private void AddScreenLine(string line)
  {
    lock (_screen) _screen.Add(line ?? string.Empty);
    ScreenChanged?.Invoke();
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
      {
        _linesRead++;
        return line;
      }

      // asked to go back a move while waiting for the game to go on: null tells the caller to go back
      if (Volatile.Read(ref _movesBack) > 0)
      {
        if (_movePoints.Count > 0) return null;
        Volatile.Write(ref _movesBack, 0);
      }

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
    // copies of rooms (portals, a moving room's stops) are found by the map, not by name
    var result = _canvas.FindRooms(roomName, roomDescription, (n, d, r) => r.IsReference ? false : Match(r, n, d));
    if (result.Certain != null) return result.Certain;

    var maybes = result.Maybes;
    if (maybes == null || maybes.Count == 0) return null;
    if (_dontAskAboutAmbiguities) return Guess(maybes, roomDescription);
    if (maybes.Count == 1 && (string.IsNullOrEmpty(roomDescription) || maybes[0].PrimaryDescription == null))
      return maybes[0];

    _askedWhichRoom = true;
    var choice = await Prompts.DisambiguateRoomAsync(line, roomDescription, maybes, token);
    token.ThrowIfCancellationRequested();
    return Answered(choice);
  }

  /// <summary>
  ///   The user's answer: the room chosen, or null for a new room, or for nothing (the heading is then skipped).
  ///   "Don't ask again" makes automap guess from then on (<see cref="Guess" />).
  /// </summary>
  private Room Answered(RoomDisambiguation choice)
  {
    if (choice.AutoChooseRest) _dontAskAboutAmbiguities = true;
    if (choice.Skip)
    {
      _skipHeading = true;
      return null;
    }

    return choice.NewRoom || choice.Chosen == null ? null : _canvas.ResolveRoom(choice.Chosen);
  }

  /// <summary>
  ///   Automap's guess, once the user has said not to ask: staying put (no move), the room the player is in; moving
  ///   on, the room whose description starts the same way, or, when the game shows a description none of them has,
  ///   a new room; otherwise the first.
  /// </summary>
  private Room Guess(IReadOnlyList<Room> candidates, string roomDescription)
  {
    if (candidates == null || candidates.Count == 0) return null;
    var here = _lastKnownRoom;
    if (_lastMoveDirection == null) return here != null && candidates.Contains(here) ? here : candidates[0];
    var described = candidates.FirstOrDefault(room =>
      room.Descriptions.Any(known => SameFirstSentence(known, roomDescription)));
    if (described != null) return described;
    return string.IsNullOrWhiteSpace(roomDescription) ? candidates[0] : null;
  }

  /// <summary>The user cancelled "which room?": nothing is mapped for this heading.</summary>
  private bool _skipHeading;

  /// <summary>The user was asked which room a heading meant, by <see cref="FindRoomAsync" />.</summary>
  private bool _askedWhichRoom;

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
    if (_suspended)
    {
      // tracking only: the map stays as it is
      _pendingStubRemoval = null;
      return;
    }

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

  /// <summary>
  ///   A room heading while automap isn't mapping. Suspended, it only follows the player through rooms it knows. In
  ///   a room marked "don't automap from here", it waits until the player is somewhere else (another room with the
  ///   same name, as in a maze, counts as still there). Leaving that room, it jumps to a room it knows, or adds a new
  ///   one joined from the room by a dotted "?" line: there is a way through, not yet mapped. Resumed somewhere it
  ///   didn't know (the player may have moved on, or gone back in the transcript), it waits for the next room it
  ///   sees and maps from there, joining it to nothing.
  /// </summary>
  private async Task FollowWhilePausedAsync(
    string roomName, string roomDescription, string line, bool pausedHere, CancellationToken token)
  {
    try
    {
      if (pausedHere && string.Equals(roomName, _lastKnownRoom.Name, StringComparison.OrdinalIgnoreCase))
      {
        Status = $"Not mapping in {_lastKnownRoom.Name}: waiting for you to leave.";
        Trace("{0}: still in {1}, which automap doesn't map from.", FormatTranscriptLineForDisplay(line), roomName);
        return;
      }

      var room = await FindRoomAsync(roomName, roomDescription, line, token);
      token.ThrowIfCancellationRequested();
      RefreshLastKnownRoom();
      if (room != null) room = _canvas.ResolveRoom(room);

      if (_suspended)
      {
        // follow the player, mapping nothing; somewhere new leaves automap not knowing where they are
        if (room != null)
        {
          NowInRoom(room);
          _pausedFrom = room;
        }
        else
        {
          _lastKnownRoom = null;
        }

        Status = room != null ? $"Automap is suspended (in {room.Name})." : "Automap is suspended (somewhere new).";
        Trace("{0}: suspended, {1}.", FormatTranscriptLineForDisplay(line), room != null ? "in " + room.Name : "somewhere new");
        return;
      }

      var from = pausedHere ? _lastKnownRoom : _canvas.ResolveRoom(_pausedFrom);
      using (await BeginBatchAsync("Automap: " + roomName, token))
      {
        if (room != null) room = _canvas.ResolveRoom(room);
        from = _canvas.ResolveRoom(from);
        if (room == null)
        {
          // placed near where mapping stopped; joined to it only when leaving a room automap doesn't map from
          room = _canvas.CreateRoom(from, roomName);
          DeduceExitsFromDescription(room, roomDescription);
          if (pausedHere && from != null) _canvas.ConnectByCommand(from, room, UnmappedWay);
          Trace("{0}: {1}, reached from {2} by a way automap didn't map.", FormatTranscriptLineForDisplay(line),
            roomName, from?.Name ?? "nowhere known");
        }
        else
        {
          Trace("{0}: back in {1}.", FormatTranscriptLineForDisplay(line), roomName);
        }

        room.AddDescription(roomDescription);
        NowInRoom(room);
        if (_settings.AutoTidy) _canvas.TidyAround(room);
      }

      _pausedFrom = null;
      Status = "Automapping is processing the transcript.";
    }
    finally
    {
      _lastMoveDirection = null;
      _lastJourneyCommand = null;
    }
  }

  /// <summary>The label of a join automap adds for a way through it didn't map (see <see cref="FollowWhilePausedAsync" />).</summary>
  public const string UnmappedWay = "?";

  /// <summary>True while the user has suspended mapping: automap follows the player but doesn't change the map.</summary>
  public bool Suspended => _suspended;

  /// <summary>True while the player is somewhere dark, so automap waits to see the room before mapping it.</summary>
  public bool InDark => _dark != null;

  /// <summary>
  ///   "I'm here": the user says the player is in <paramref name="room" /> (automap lost track, or got it wrong).
  ///   Automap maps on from there; the move it was in the middle of, if any, isn't joined to anything.
  /// </summary>
  public void SetCurrentRoom(Room room)
  {
    if (room == null) return;
    _lastKnownRoom = room;
    _lastMoveDirection = null;
    _lastJourneyCommand = null;
    _pendingStubRemoval = null;
    _pausedFrom = null;
    _dark = null;
    _followTo = null;
    _doneInRoom = null;
    _canvas?.SelectRoom(room);
    Status = $"Automap: you're in {room.Name}.";
  }

  /// <summary>
  ///   Stop mapping for now: automap keeps following the player through rooms it knows, and maps again after
  ///   <see cref="Resume" />, from a room it knows or with a "?" join to somewhere new.
  /// </summary>
  public void Suspend()
  {
    if (_suspended) return;
    _pausedFrom = _lastKnownRoom;
    _suspended = true;
    Status = "Automap is suspended.";
  }

  /// <summary>Map again (see <see cref="Suspend" />).</summary>
  public void Resume()
  {
    if (!_suspended) return;
    _suspended = false;
    Status = "Automap will map again from the next room.";
  }

  private volatile bool _suspended;
  private Room _pausedFrom;

  /// <summary>How the player got somewhere dark, so the room can be mapped that way when it can be seen.</summary>
  private sealed record DarkArrival(
    Room From, MappableDirection? Direction, string Journey, (Room Room, MappableDirection Direction)? Stub);

  private DarkArrival _dark;

  /// <summary>The room about to be entered was first found in the dark: it's marked dark.</summary>
  private bool _seenInTheDark;

  /// <summary>
  ///   Words in a room heading that mean the player can't see where they are ("Darkness", "It is pitch black"), so
  ///   the heading isn't a room.
  /// </summary>
  private static readonly string[] _darkWords = { "darkness", "pitch dark", "pitch black", "too dark" };

  /// <summary>A heading that says it's too dark to see ("Darkness", "It is pitch dark"), not a room's name.</summary>
  public static bool IsDarkness(string roomName)
  {
    if (string.IsNullOrWhiteSpace(roomName)) return false;
    var name = roomName.Trim();
    if (string.Equals(name, "dark", StringComparison.OrdinalIgnoreCase)) return true;
    return _darkWords.Any(word => System.Text.RegularExpressions.Regex.IsMatch(name,
      @"\b" + System.Text.RegularExpressions.Regex.Escape(word) + @"\b",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase));
  }

  /// <summary>
  ///   The player went somewhere they can't see: automap remembers how they got there, and maps the room when it can
  ///   be seen (once the lamp is lit), as reached that way.
  /// </summary>
  private void EnterTheDark(string line)
  {
    if (_dark == null) _dark = new DarkArrival(_lastKnownRoom, _lastMoveDirection, _lastJourneyCommand, _pendingStubRemoval);
    else if (_lastMoveDirection != null) _dark = new DarkArrival(null, null, null, null); // moved on in the dark
    _pendingStubRemoval = null; // the way leads somewhere: its stub stays until the room is seen
    _lastMoveDirection = null;
    _lastJourneyCommand = null;
    Status = "Somewhere dark: the room will be mapped when it can be seen.";
    Trace("{0}: somewhere dark, not mapped until it can be seen.", FormatTranscriptLineForDisplay(line));
  }

  /// <summary>
  ///   A short first line of the game's answer that says it's dark ("It is pitch black.", "It's too dark to see."),
  ///   rather than why the player can't go somewhere ("It's too dark to go that way.").
  /// </summary>
  public static bool IsDarkReply(string line)
  {
    if (string.IsNullOrWhiteSpace(line)) return false;
    var text = line.Trim();
    if (text.Length > 80 || !IsDarkness(text)) return false;
    return !System.Text.RegularExpressions.Regex.IsMatch(text, @"\b(can't|cannot|can not|won't|go|going)\b",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  }

  private void NowInRoom(Room room)
  {
    if (room != _lastKnownRoom) _doneInRoom = null;
    if (_seenInTheDark)
    {
      // found in the dark: it needs a light
      room.IsDark = true;
      _seenInTheDark = false;
    }

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
    // the game answered the last command with a question ("Which peg do you mean, the first or the second?"): the
    // next command may answer it ("first"), and the two make one ("pull first peg")
    var lastText = lines.LastOrDefault(text => !string.IsNullOrWhiteSpace(text));
    if (lastText != null) _askedAbout = lastText.TrimEnd().EndsWith("?") && !string.IsNullOrWhiteSpace(_lastCommand) ? _lastCommand : null;

    var firstReply = lines.FindIndex(text => !string.IsNullOrWhiteSpace(text));
    for (var index = 0; index < lines.Count; ++index)
    {
      token.ThrowIfCancellationRequested();
      var line = lines[index];
      if (IsDeathOrEnd(line))
      {
        // the player died, or the game ended: wherever they are next isn't reached from here
        _lastMoveDirection = null;
        _lastJourneyCommand = null;
        Trace("{0}: the player died, or the game ended.", FormatTranscriptLineForDisplay(line));
      }

      // the game answers a move with a sentence saying it's dark ("It is pitch black."), not a heading
      if (index == firstReply && !_suspended && (_lastMoveDirection != null || _lastJourneyCommand != null) &&
          IsDarkReply(line))
      {
        EnterTheDark(line);
        previousLine = line;
        continue;
      }

      string roomName;
      if (ExtractRoomName(line, previousLine, out roomName))
      {
        string roomDescription;
        ExtractParagraph(lines, index + 1, out roomDescription);

        // the command was the label of a join the user drew ("pull first peg"): a heading naming the room at its
        // other end (or the room it was made from, "Cupboard" for "Cupboard (-1)") is that room, not a journey;
        // any other heading is mapped as usual
        if (_followTo != null)
        {
          var followed = _canvas.ResolveRoom(_followTo);
          _followTo = null;
          if (followed != null && followed.Name.StartsWith(roomName.Trim(), StringComparison.OrdinalIgnoreCase))
          {
            await FollowAsync(followed, token);
            previousLine = line;
            continue;
          }
        }

        // not mapping for now: suspended by the user, or in a room marked "don't automap from here" (a maze, a
        // cupboard that turns out to be a lift). Automap only follows where the player is until it can map again.
        RefreshLastKnownRoom();
        var pausedHere = !_suspended && _lastKnownRoom != null && _lastKnownRoom.AutomapPause;
        var resuming = !_suspended && !pausedHere && _pausedFrom != null;
        if (resuming && _lastKnownRoom != null) _pausedFrom = null; // resumed somewhere known: map from there
        else if (_suspended || pausedHere || resuming)
        {
          await FollowWhilePausedAsync(roomName, roomDescription, line, pausedHere, token);
          previousLine = line;
          continue;
        }

        // somewhere dark: not a room yet. Automap remembers how the player got there, and maps the room when it can
        // be seen (once the lamp is lit), as reached that way.
        if (IsDarkness(roomName))
        {
          EnterTheDark(line);
          previousLine = line;
          continue;
        }

        if (_dark != null)
        {
          if (_lastMoveDirection == null)
          {
            // lit up where the player stands ("light lamp"): the room is where the move into the dark led
            _lastKnownRoom = _dark.From == null ? null : _canvas.ResolveRoom(_dark.From);
            _lastMoveDirection = _dark.Direction;
            _lastJourneyCommand = _dark.Journey;
            _pendingStubRemoval = _dark.Stub;
            _seenInTheDark = true;
          }
          else
          {
            // moved on in the dark: where from isn't known
            _lastKnownRoom = null;
          }

          _dark = null;
        }

        // work out which room the transcript is referring to here, asking them if necessary.
        // All prompts happen before the batch below opens: the user can't undo while a batch is open.
        // The map comes first: a join the way the player went (or back the way they came) says which room it is, so
        // several rooms sharing a name needn't be asked about.
        RefreshLastKnownRoom();
        await ChangeStopAsync(roomName, token);
        var room = VariantRoom(roomName, null);
        _skipHeading = false;
        if (room == null)
        {
          _askedWhichRoom = false;
          room = await FindRoomAsync(roomName, roomDescription, line, token);
          token.ThrowIfCancellationRequested();
          RefreshLastKnownRoom();
          if (room != null) room = AtStop(_canvas.ResolveRoom(room));
          // (once only: if the user has just said which room it is, that's that)
          if (!_askedWhichRoom) room = await WhichOfTheSameNameAsync(room, roomName, roomDescription, line, token);
        }

        if (_skipHeading)
        {
          // the user said it's none of these, and not a new room either: map nothing for it, and don't join the next
          // room from the wrong one
          _skipHeading = false;
          _lastKnownRoom = null;
          _lastMoveDirection = null;
          _lastJourneyCommand = null;
          Trace("{0}: skipped, as the user asked.", FormatTranscriptLineForDisplay(line));
          previousLine = line;
          continue;
        }

        AutomapSameDirectionResult? decision = null;
        Room otherRoom = null;
        // a room already on the map, reached a way the player has been seen to go somewhere else (out of the closet to
        // the foyer, and now, after pulling the peg, to the cellar): ask, as for a new room, so the room left can be
        // split
        var elsewhere = room != null && room != _lastKnownRoom && _lastKnownRoom != null && _lastMoveDirection != null
          ? SeenLeadingElsewhere(_lastKnownRoom, _lastMoveDirection.Value, room)
          : null;
        if (elsewhere != null)
        {
          otherRoom = elsewhere;
          decision = await Prompts.ChooseSameDirectionAsync(_lastKnownRoom, otherRoom, roomName, token);
          token.ThrowIfCancellationRequested();
          if (decision == AutomapSameDirectionResult.Cancel)
          {
            Stop();
            token.ThrowIfCancellationRequested();
            throw new OperationCanceledException(token);
          }

          RefreshLastKnownRoom();
          otherRoom = _canvas.ResolveRoom(otherRoom);
        }
        else if (room == null && _lastKnownRoom != null && _lastMoveDirection != null)
        {
          // is there already a connection in that direction?
          // The room at the other end of a connection already leaving this way. (The reference took the
          // connection's target, which is this very room when the connection arrived here from elsewhere.)
          // A one-way connection that only arrives here says nothing about where this exit leads (two-way
          // connections off: going east to here is not a promise that west leads back), so it doesn't count.
          // The join's ways say where it leads, wherever it leaves the room: "n / up" on the north side leads up.
          otherRoom = _canvas.RoomsAlong(_lastKnownRoom, _lastMoveDirection.Value).Select(Real)
                      .FirstOrDefault(room => room != null && room != _lastKnownRoom);
          if (otherRoom != null)
          {
            decision = await Prompts.ChooseSameDirectionAsync(_lastKnownRoom, otherRoom, roomName, token);
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
                  case AutomapSameDirectionResult.Moves:
                  {
                    // the room the player left moves (a lift): it's at another stop now, a copy of it there
                    var stop = NewStop(_lastKnownRoom);
                    room = _canvas.CreateRoom(stop, _lastMoveDirection.Value, roomName, line);
                    _canvas.Connect(stop, _lastMoveDirection.Value, room, _settings.AssumeTwoWayConnections);
                    Trace("{0}: {1} moves; {2} is {3} from this stop.", FormatTranscriptLineForDisplay(line),
                      stop.Name, roomName, _lastMoveDirection.Value.ToString().ToLower());
                    break;
                  }
                  case AutomapSameDirectionResult.Split:
                  {
                    // the room the player left is a second place like it ("Cupboard (2)", down the lift), reached
                    // from the first by what they did there; the way out joins the second to the new room
                    var first = _lastKnownRoom;
                    var second = _canvas.CreateRoom(first, SplitName(first.Name));
                    foreach (var description in first.Descriptions) second.AddDescription(description);
                    second.IsDark = first.IsDark;
                    _canvas.ConnectByCommand(first, second, _doneInRoom ?? UnmappedWay);
                    room = _canvas.CreateRoom(second, _lastMoveDirection.Value, roomName, line);
                    _canvas.Connect(second, _lastMoveDirection.Value, room, _settings.AssumeTwoWayConnections);
                    Trace("{0}: {1} split; {2} is {3} from {4}.", FormatTranscriptLineForDisplay(line), first.Name,
                      roomName, _lastMoveDirection.Value.ToString().ToLower(), second.Name);
                    break;
                  }
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
              // player moved sensibly; ensure rooms are connected up (as the user said, if the way led somewhere else
              // before)
              ConnectToKnownRoom(room, otherRoom, decision, line);
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
              _canvas.ConnectByCommand(_lastKnownRoom, Near(_lastKnownRoom, room), _lastJourneyCommand);
              Trace("{0}: {1} reached from {2} by \"{3}\".", FormatTranscriptLineForDisplay(line), roomName,
                _lastKnownRoom.Name, _lastJourneyCommand);
            }

            NowInRoom(room);
            waits++;
          }
          else if (_lastKnownRoom != null && _lastMoveDirection != null)
          {
            // the way out led straight back in ("north" in the forest returns to the forest); a move that fails
            // doesn't show the room again, so it never gets here
            _canvas.ConnectLoop(_lastKnownRoom, _lastMoveDirection.Value);
            Trace("{0}: {1} leads from {2} back to itself.", FormatTranscriptLineForDisplay(line),
              _lastMoveDirection.Value.ToString().ToLower(), _lastKnownRoom.Name);
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

    // no room heading in the reply: the labelled join took the player along it ("The cupboard lurches downwards.")
    if (_followTo != null)
    {
      var followed = _canvas.ResolveRoom(_followTo);
      _followTo = null;
      if (followed != null) await FollowAsync(followed, token);
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

    // a join on the map labelled with this command takes the player along it, once the game has replied
    var asked = _askedAbout;
    _askedAbout = null;
    // what the player did here, should the room turn out to be two (Split): the last such command, with the
    // command it answered ("pull peg, first")
    if (_lastJourneyCommand != null) _doneInRoom = asked != null ? asked.Trim() + ", " + command.Trim() : command.Trim();
    _followTo = _lastJourneyCommand != null ? LabelledJoinEnd(command, asked) : null;
  }

  /// <summary>
  ///   A copy of a room the user (or Split) made for the same place seen differently, "Cupboard (-1)" for a heading of
  ///   "Cupboard": it's the room the heading means when the player is in it already (looking round), or when the way
  ///   they just went is known to lead to it. Otherwise <paramref name="found" />, the room with the heading's name.
  /// </summary>
  /// <summary>
  ///   The room the heading means, as the map shows it, or null when the map doesn't say: staying put in a copy of
  ///   the room ("Cupboard (-1)" for "Cupboard"); the way the player went, as the map shows it (by its label, wherever
  ///   it leaves the room), to a room by this name or a copy of it; or, going back the way a one-way join came (down,
  ///   after coming up), the room it came from. Several rooms can share a name: two "In the Chimney" one above the
  ///   other.
  /// </summary>
  private Room VariantRoom(string roomName, Room found)
  {
    var here = _lastKnownRoom;
    if (here == null) return null;
    if (_lastMoveDirection == null && _lastJourneyCommand == null)
      return NamedAs(here.Name, roomName) ? here : null;
    // a moving room takes the player with it ("pull peg" in the lift): still in it, at whichever stop
    if (_lastMoveDirection == null) return Moving(here) && NamedAs(here.Name, roomName) ? here : null;
    var direction = _lastMoveDirection.Value;

    // through a portal ("To Front Porch") to the room it stands for
    var along = _canvas.RoomsAlong(here, direction).Select(Real)
      .FirstOrDefault(other => other != null && other != here && NamedAs(other.Name, roomName));
    if (along != null) return along;

    // the natural way back along a one-way join that brought the player here, from a room by this name, until the
    // player finds it leads somewhere else
    var cameBy = CompassPointHelper.GetOpposite(direction);
    var back = here.GetConnections()
      .Where(connection => connection.Flow == ConnectionFlow.OneWay && connection.GetTargetRoom() == here)
      .Select(connection => (Join: connection, From: Real(connection.GetSourceRoom())))
      .Where(pair => pair.From != null && pair.From != here && NamedAs(pair.From.Name, roomName))
      .FirstOrDefault(pair =>
      {
        var ways = JoinWays.Of(pair.Join);
        return !JoinWays.SeenFrom(ways, false) &&
               ways.Any(way => way.AtStart && way.IsDirection && way.Seen && way.Direction == cameBy);
      }).From;
    return back == null ? null : _canvas.ResolveRoom(back);
  }

  /// <summary>The room's name is the heading, or a copy of it ("Cupboard (-1)" for "Cupboard").</summary>
  private static bool NamedAs(string name, string heading) =>
    IsVariantOf(name, heading) ||
    string.Equals(name?.Trim(), heading?.Trim(), StringComparison.OrdinalIgnoreCase);

  /// <summary>
  ///   A room found by its name alone, when the map doesn't say which, and the game shows a description: of the rooms
  ///   with that name, the one whose description starts the same way. If none does, it may be another room by that
  ///   name (a second "In the Chimney" further along), so automap asks, since descriptions can change as a game goes
  ///   on. Returns null for a new room. Not asked for a way already mapped as leading back into the room, or when
  ///   automap isn't mapping.
  /// </summary>
  private async Task<Room> WhichOfTheSameNameAsync(
    Room room, string roomName, string roomDescription, string line, CancellationToken token)
  {
    var here = _lastKnownRoom;
    if (room == null || _suspended || (_lastMoveDirection == null && _lastJourneyCommand == null) ||
        (here != null && here.AutomapPause) || string.IsNullOrWhiteSpace(roomDescription) ||
        !string.Equals(room.Name?.Trim(), roomName?.Trim(), StringComparison.OrdinalIgnoreCase) ||
        room.Descriptions.Any(known => SameFirstSentence(known, roomDescription)))
      return room;

    var sameName = _canvas.FindRooms(roomName, roomDescription, (name, _, other) =>
        !other.IsReference && string.Equals(other.Name?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase) ? null : false)
      .Maybes?.ToList() ?? new List<Room>();
    var described = sameName.FirstOrDefault(other =>
      other.Descriptions.Any(known => SameFirstSentence(known, roomDescription)));
    if (described != null) return described;

    // a room whose name no other room has is that room, whatever it says now (a revisit can show only what's lying
    // about); the question is only which of several, or whether the room you left is somewhere new
    if (room.Descriptions.Count == 0 || (sameName.Count < 2 && room != here)) return room;
    if (room == here && _lastMoveDirection != null && LoopsBack(room, _lastMoveDirection.Value)) return room;
    if (!sameName.Contains(room)) sameName.Insert(0, room);
    if (_dontAskAboutAmbiguities) return Guess(sameName, roomDescription);

    var choice = await Prompts.DisambiguateRoomAsync(line, roomDescription, sameName, token);
    token.ThrowIfCancellationRequested();
    RefreshLastKnownRoom();
    return Answered(choice);
  }

  /// <summary>
  ///   A room the player was seen going <paramref name="direction" /> to from <paramref name="here" />, other than
  ///   <paramref name="room" />: the map says that way leads somewhere else.
  /// </summary>
  private Room SeenLeadingElsewhere(Room here, MappableDirection direction, Room room)
  {
    foreach (var connection in here.GetConnections())
    {
      if (AutomapCanvas.IsCommandJoin(connection)) continue;
      var source = connection.GetSourceRoom();
      var target = connection.GetTargetRoom();
      if (source == null || target == null || source == target) continue;
      var hereIsStart = source == here;
      if (!hereIsStart && connection.Flow == ConnectionFlow.OneWay) continue;
      var other = Real(hereIsStart ? target : source);
      if (other == room || other == here) continue;
      if (JoinWays.Of(connection).Any(way => way.AtStart == hereIsStart && way.IsDirection && way.Seen &&
                                            way.Direction == direction))
        return _canvas.ResolveRoom(other);
    }

    return null;
  }

  /// <summary>
  ///   Joins the room the player left to <paramref name="room" />, already on the map, the way they went; or, when that
  ///   way was seen leading to <paramref name="before" />, as the user decided: it still leads there (no join), it
  ///   leads here instead, it leads to both, or the room left is really two places (split, as for a new room).
  /// </summary>
  private void ConnectToKnownRoom(Room room, Room before, AutomapSameDirectionResult? decision, string line)
  {
    var here = _lastKnownRoom;
    var direction = _lastMoveDirection.Value;
    if (before == null || decision == null || decision == AutomapSameDirectionResult.KeepBoth)
    {
      _canvas.Connect(here, direction, Near(here, room), _settings.AssumeTwoWayConnections);
      return;
    }

    switch (decision.Value)
    {
      case AutomapSameDirectionResult.KeepRoom1:
        // the way still leads where it did; the player got here some other way
        Trace("{0}: {1} still leads to {2}; not joined.", FormatTranscriptLineForDisplay(line),
          direction.ToString().ToLower(), before.Name);
        break;
      case AutomapSameDirectionResult.KeepRoom2:
        _canvas.ForgetWay(here, direction, Near(here, before));
        _canvas.Connect(here, direction, Near(here, room), _settings.AssumeTwoWayConnections);
        break;
      case AutomapSameDirectionResult.Moves:
      {
        var stop = NewStop(here);
        _canvas.Connect(stop, direction, Near(stop, room), _settings.AssumeTwoWayConnections);
        _lastKnownRoom = stop;
        Trace("{0}: {1} moves; {2} is {3} from this stop.", FormatTranscriptLineForDisplay(line), stop.Name,
          room.Name, direction.ToString().ToLower());
        break;
      }
      case AutomapSameDirectionResult.Split:
      {
        var second = _canvas.CreateRoom(here, SplitName(here.Name));
        foreach (var description in here.Descriptions) second.AddDescription(description);
        second.IsDark = here.IsDark;
        _canvas.ConnectByCommand(here, second, _doneInRoom ?? UnmappedWay);
        _canvas.Connect(second, direction, room, _settings.AssumeTwoWayConnections);
        Trace("{0}: {1} split; {2} is {3} from {4}.", FormatTranscriptLineForDisplay(line), here.Name, room.Name,
          direction.ToString().ToLower(), second.Name);
        break;
      }
      default:
        _canvas.Connect(here, direction, Near(here, room), _settings.AssumeTwoWayConnections);
        break;
    }
  }

  /// <summary>
  ///   The room <paramref name="room" /> stands for: a portal ("To Front Porch", a reference room) is the room it
  ///   refers to; any other room, a moving room's stop included, is itself.
  /// </summary>
  private Room Real(Room room) =>
    room is { IsPortal: true } ? _canvas.ResolveRoom(room.ReferenceRoom) ?? room : room;

  /// <summary>
  ///   What to join <paramref name="here" /> to for <paramref name="room" />: a portal to it already joined to
  ///   <paramref name="here" /> ("To Front Porch" beside the foyer), so the map keeps to its areas; else the room.
  /// </summary>
  private static Room Near(Room here, Room room)
  {
    if (here == null || room == null || room.IsReference) return room;
    Room portal = null;
    foreach (var connection in here.GetConnections())
    {
      var other = OtherEnd(connection, here);
      if (other == room) return room;
      if (other is { IsPortal: true } && other.ReferenceRoomId == room.Id) portal ??= other;
    }

    return portal ?? room;
  }

  /// <summary>
  ///   A moving room found by its name, from where the player is: the stop of it joined to that room ("enter closet"
  ///   from the cellar goes into the closet down there), else the room.
  /// </summary>
  private Room AtStop(Room room)
  {
    var here = _lastKnownRoom;
    if (room is not { Moves: true } || here == null || here == room || MovingRootOrNull(here) == room) return room;
    return here.GetConnections().Select(connection => OtherEnd(connection, here))
      .FirstOrDefault(other => other is { IsStop: true } && other.ReferenceRoomId == room.Id) ?? room;
  }

  private static Room MovingRootOrNull(Room room) => room.IsStop ? room.ReferenceRoom : room.Moves ? room : null;

  /// <summary>A room that moves, or one of its stops (<see cref="Room.Moves" />).</summary>
  private static bool Moving(Room room) => room != null && (room.Moves || room.IsStop);

  /// <summary>The moving room <paramref name="room" /> is, or is a stop of.</summary>
  private static Room MovingRoot(Room room) => room.IsStop ? room.ReferenceRoom : room;

  /// <summary>
  ///   A new stop for the moving room the player is in (it's somewhere else now): a copy of it, with its name,
  ///   beside <paramref name="here" /> and joined from it by what moved it ("pull peg"). The room is marked as moving.
  /// </summary>
  private Room NewStop(Room here)
  {
    var root = MovingRoot(here);
    root.Moves = true;
    var stop = _canvas.CreateRoom(here, root.Name);
    stop.ReferenceRoomId = root.Id;
    stop.CopyStyleFrom(root);
    foreach (var description in root.Descriptions) stop.AddDescription(description);
    _canvas.ConnectByCommand(here, stop, _doneInRoom ?? UnmappedWay);
    return stop;
  }

  /// <summary>
  ///   In a moving room (a lift), the way out says which stop it's at: when the way the player went is known to lead
  ///   somewhere other than this heading, they're at another stop, the one where it does lead here, or a new one.
  ///   Automap carries on from that stop, so a lift's doors needn't be asked about.
  /// </summary>
  private async Task ChangeStopAsync(string roomName, CancellationToken token)
  {
    var here = _lastKnownRoom;
    if (_suspended || here == null || _lastMoveDirection == null || !Moving(here) || MovingRoot(here) == null) return;
    var direction = _lastMoveDirection.Value;
    List<Room> Along(Room stop) => _canvas.RoomsAlong(stop, direction).Select(Real)
      .Where(other => other != null && other != stop).ToList();

    var along = Along(here);
    if (along.Count == 0 || along.Any(other => NamedAs(other.Name, roomName))) return;

    var root = MovingRoot(here);
    var stops = Trizbort.Domain.Application.Project.Current.Elements.OfType<Room>()
      .Where(other => other == root || other.ReferenceRoomId == root.Id && other.IsStop).ToList();
    var stop = stops.FirstOrDefault(other => other != here && Along(other).Any(room => NamedAs(room.Name, roomName)));
    using (await BeginBatchAsync("Automap: " + root.Name + " moves", token))
    {
      if (stop == null) stop = NewStop(here);
      else if (!here.GetConnections().Any(connection => OtherEnd(connection, here) == stop))
        _canvas.ConnectByCommand(here, stop, _doneInRoom ?? UnmappedWay);
    }

    Trace("{0} is at another stop.", root.Name);
    _lastKnownRoom = stop;
  }

  /// <summary>Whether the map has <paramref name="direction" /> leading from <paramref name="room" /> straight back in.</summary>
  private static bool LoopsBack(Room room, MappableDirection direction) =>
    room.GetConnections().Any(connection =>
      connection.GetSourceRoom(out var from) == room && connection.GetTargetRoom(out var to) == room && from != to &&
      JoinWays.Lists(JoinWays.Of(connection), true, direction));

  /// <summary>"Cupboard (-1)" or "Cupboard 2" for "Cupboard": the name, then something more after a space or bracket.</summary>
  private static bool IsVariantOf(string name, string heading)
  {
    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(heading)) return false;
    name = name.Trim();
    heading = heading.Trim();
    return name.Length > heading.Length && name.StartsWith(heading, StringComparison.OrdinalIgnoreCase) &&
           !char.IsLetterOrDigit(name[heading.Length]);
  }

  /// <summary>The command the game answered with a question, if the last reply was one.</summary>
  private string _askedAbout;

  /// <summary>The last command, not a direction, the player typed in the room they're in (see Split).</summary>
  private string _doneInRoom;

  /// <summary>"Cupboard (2)" for "Cupboard", or the next number free.</summary>
  private string SplitName(string name)
  {
    var names = new HashSet<string>(
      Trizbort.Domain.Application.Project.Current.Elements.OfType<Room>().Select(room => room.Name),
      StringComparer.OrdinalIgnoreCase);
    for (var number = 2; ; number++)
    {
      var candidate = $"{name} ({number})";
      if (!names.Contains(candidate)) return candidate;
    }
  }

  /// <summary>
  ///   The room at the other end of a join labelled with the last command: the player is there once the game has
  ///   replied, unless the reply shows them somewhere else.
  /// </summary>
  private Room _followTo;

  private async Task FollowAsync(Room room, CancellationToken token)
  {
    using (await BeginBatchAsync("Automap: " + room.Name, token))
    {
      NowInRoom(room);
    }

    _lastMoveDirection = null;
    _lastJourneyCommand = null;
    _pendingStubRemoval = null;
    Trace("Automap: followed the join labelled \"{0}\" to {1}.", _lastCommand, room.Name);
  }

  /// <summary>
  ///   A join from where the player is, labelled with the command they typed (or with it and the command it answers,
  ///   "pull peg" then "first" for "pull first peg"): the user drew it for a way automap can't see, such as a lift.
  ///   Returns the room at its other end. Words count in any order, and "the", "a" and "an" don't count.
  /// </summary>
  private Room LabelledJoinEnd(string command, string asked)
  {
    RefreshLastKnownRoom();
    var here = _lastKnownRoom;
    if (here == null) return null;
    var tries = new List<string> { command };
    if (asked != null) tries.Add(asked + " " + command);

    foreach (var connection in here.GetConnections())
    {
      var to = OtherEnd(connection, here);
      if (to == null || to == here) continue;
      if (connection.Flow == ConnectionFlow.OneWay && connection.GetSourceRoom() != here) continue;
      var labels = new[] { connection.MidText, connection.GetSourceRoom() == here ? connection.StartText : connection.EndText };
      var match = labels.Where(label => !string.IsNullOrWhiteSpace(label))
        .SelectMany(label => label.Split('/'))
        .FirstOrDefault(label => tries.Any(attempt => SameWords(label, attempt)));
      if (match != null) return Real(to);
    }

    return null;
  }

  private static readonly string[] _articles = { "the", "a", "an" };

  /// <summary>The same words, in any order, leaving out "the", "a" and "an".</summary>
  private static bool SameWords(string a, string b)
  {
    static List<string> Words(string text) => text.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
      .Select(word => word.ToLowerInvariant()).Where(word => !_articles.Contains(word)).OrderBy(word => word).ToList();
    var first = Words(a);
    return first.Count > 0 && first.SequenceEqual(Words(b));
  }

  private static readonly string[] _notJourneys =
  {
    "undo", "restore", "load", "restart", "quit", "q", "save", "look", "l", "again", "g", "oops", "script", "unscript",
    "transcript",
    // answers to the game's questions ("Do you want to be reincarnated?", "Are you sure you want to quit?")
    "yes", "y", "no"
  };

  /// <summary>
  ///   The game says the player died, or the game is over ("You have died", "You are dead", "*** You have won ***").
  ///   What follows (a reincarnation, a restart) is no journey from the room they were in.
  /// </summary>
  public static bool IsDeathOrEnd(string line)
  {
    if (string.IsNullOrWhiteSpace(line)) return false;
    return _deathOrEnd.IsMatch(line);
  }

  private static readonly System.Text.RegularExpressions.Regex _deathOrEnd = new(
    @"\byou(?: have|'ve| are|'re)? (?:died|dead|been killed)\b|\byou die\b|^\s*\*{2,}.*\*{2,}\s*$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

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

      // tracking only: the map stays as it is
      if (!string.IsNullOrEmpty(regionName) && _lastKnownRoom != null && !_suspended)
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

      // tracking only: the map stays as it is
      if (!string.IsNullOrEmpty(objectName) && _lastKnownRoom != null && !_suspended)
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
  private volatile bool _moveByMove;
  private int _movesToPlay;
  private readonly List<string> _screen = new();
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
    _canvas.AbbreviateDirections = settings.AbbreviateDirections;
    _seenCommand = false;
    _startRoom = null;
    _lastCommand = null;
    _pendingStubRemoval = null;
    _lastKnownRoom = null;
    _lastMoveDirection = null;
    _lastJourneyCommand = null;
    _gameName = string.Empty;
    _stepNow = false;
    _moveByMove = settings.MoveByMove;
    _suspended = false;
    _pausedFrom = null;
    _dark = null;
    _seenInTheDark = false;
    _askedAbout = null;
    _followTo = null;
    _doneInRoom = null;
    Volatile.Write(ref _movesToPlay, 0);
    Volatile.Write(ref _movesBack, 0);
    _movePoints.Clear();
    _linesRead = 0;
    NextCommand = null;
    lock (_screen) _screen.Clear();
    ScreenChanged?.Invoke();
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
    PlayToEnd();
  }

  /// <summary>The room automap thinks the player is in, as it is on the map now; null when it doesn't know.</summary>
  public Room CurrentRoom => _lastKnownRoom == null ? null : _canvas?.ResolveRoom(_lastKnownRoom);

  /// <summary>True while the run waits before each move (see <see cref="AutomapSettings.MoveByMove" />).</summary>
  public bool MoveByMove => _moveByMove;

  /// <summary>The command automap is paused before, while playing move by move; otherwise null.</summary>
  public string NextCommand { get; private set; }

  /// <summary>Play the next move of the transcript, then pause again.</summary>
  public void PlayNextMove()
  {
    _moveByMove = true;
    Interlocked.Increment(ref _movesToPlay);
  }

  /// <summary>Play the rest of the transcript without pausing (and keep following it as the game goes on).</summary>
  public void PlayToEnd()
  {
    _moveByMove = false;
    Volatile.Write(ref _movesToPlay, 0);
    // and no more single stepping (the debug option), as F5 does
    _settings.SingleStep = false;
    _stepNow = true;
  }

  /// <summary>
  ///   Go back a move in the transcript (again to go further), and wait there; the map is left as it is, and playing
  ///   on walks through what's already mapped, putting back anything that's missing.
  /// </summary>
  public void GoBack()
  {
    Interlocked.Increment(ref _movesBack);
  }

  /// <summary>Whether there is a move to go back to.</summary>
  public bool CanGoBack => _movePoints.Count > 1 || (_movePoints.Count == 1 && NextCommand == null);

  /// <summary>How many transcript lines automap has read in this run, as <see cref="ScreenLines" /> holds them.</summary>
  public int ScreenLineCount
  {
    get
    {
      lock (_screen) return _screen.Count;
    }
  }

  /// <summary>
  ///   How many moves are left to play in the transcript as it is now: the command automap is paused before, if
  ///   any, and each command typed after it. Reads the transcript again, so call it once, not on a timer.
  /// </summary>
  public int MovesAhead()
  {
    var fileName = _settings.FileName;
    if (!Running || string.IsNullOrEmpty(fileName)) return 0;
    var skip = _linesRead;
    var moves = string.IsNullOrWhiteSpace(NextCommand) ? 0 : 1;
    try
    {
      using var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var reader = new StreamReader(stream);
      var index = 0;
      while (reader.ReadLine() is { } line)
      {
        if (index++ < skip) continue;
        if (IsPrompt(line, out var command) && !string.IsNullOrWhiteSpace(command)) moves++;
      }
    }
    catch (IOException)
    {
    }
    catch (UnauthorizedAccessException)
    {
    }

    return moves;
  }

  /// <summary>Pause before the next move.</summary>
  public void Pause()
  {
    Volatile.Write(ref _movesToPlay, 0);
    _moveByMove = true;
  }

  /// <summary>
  ///   The transcript lines automap has read in this run, from <paramref name="from" /> on: what the game window
  ///   showed up to this point.
  /// </summary>
  public IReadOnlyList<string> ScreenLines(int from = 0)
  {
    lock (_screen)
      return from >= _screen.Count ? Array.Empty<string>() : _screen.GetRange(Math.Max(0, from), _screen.Count - Math.Max(0, from));
  }

  /// <summary>Raised when automap reads another line of the transcript, or a run starts. May be raised off the UI thread.</summary>
  public event Action ScreenChanged;

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

          // the reply is all in: a direction tried that led nowhere loses its stub now, not when the player types
          // the next command (playing live, or move by move, the map would show the stub until then)
          await FlushPendingStubRemoval(tokenSource.Token);

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

    // the transcript, reopened when automap goes back a move
    Stream stream = null;
    PeekingStreamReader reader = null;
    try
    {
      stream = File.Open(_settings.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      reader = new PeekingStreamReader(stream);
      var lastline = "";

      if (_settings.ContinueTranscript)
      {
        string next;
        while ((next = await reader.ReadLineAsync(tokenSource.Token)) != null)
        {
          lastline = next;
          _linesRead++;
        }
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
        string command = null;
        var wentBack = false;
        if (_settings.ContinueTranscript)
        {
          // carrying on from the end of a game still running: the last line is usually its prompt, waiting for the
          // player's next command, which is read as the command when it's typed
          line = lastline;
          _settings.ContinueTranscript = false;
          atFileEnd = await reader.IsAtEndAsync(tokenSource.Token);
        }
        else
        {
          // ...read a line of text
          line = await WaitForNewLine(reader, tokenSource.Token);
          if (line == null)
          {
            // went back a move while waiting at the end of the transcript (after playing to the end)
            (command, reader, stream) = await GoBackAsync(reader, stream, tokenSource.Token, true);
            linesBetweenPrompts.Clear();
            wentBack = true;
          }
          else
          {
            atFileEnd = await reader.IsAtEndAsync(tokenSource.Token); // store this now so that it's still valid when we use it below
          }
        }

        if (!wentBack) AddScreenLine(line);

        //Trace("[" + line + "]");
        if (wentBack || IsPrompt(line, out command))
        {
          // this is a prompt line

          if (!wentBack)
          {
            // let's process everything leading up to it since the last prompt, but not necessarily this new prompt itself
            await ProcessTranscriptText(linesBetweenPrompts, tokenSource.Token);
            tokenSource.Token.ThrowIfCancellationRequested();

            // we've now dealt with all lines to this point
            linesBetweenPrompts.Clear();

            // the reply is all in: a direction tried that led nowhere loses its stub now, not when the player types
            // the next command (playing live, or move by move, the map would show the stub until then)
            await FlushPendingStubRemoval(tokenSource.Token);
          }

          // handle the case where we're at the end of the file, waiting for user input: the prompt has been
          // written but the command hasn't. When the prompt line already holds the command (">north"), keep it: an
          // interpreter writes that before the game's reply, so the next line is the reply, not the command.
          if (!wentBack && atFileEnd && string.IsNullOrWhiteSpace(command))
            // we've already read the prompt, now just read the command when the player enters it
          {
            var typed = await WaitForNewLine(reader, tokenSource.Token);
            if (typed == null)
            {
              // went back a move while waiting for the player's next command
              (command, reader, stream) = await GoBackAsync(reader, stream, tokenSource.Token, true);
              linesBetweenPrompts.Clear();
              atFileEnd = false;
            }
            else
            {
              AddScreenLine(typed);
              command = typed.Trim();
            }
          }

          //                  var nextParagraph = getTextToNextPrompt(reader);

          // playing move by move: wait here, with the game's reply to the last move mapped; going back reads the
          // transcript again from an earlier move and waits there
          NoteMovePoint(command);
          while (await WaitForNextMove(command, tokenSource.Token))
          {
            (command, reader, stream) = await GoBackAsync(reader, stream, tokenSource.Token);
            linesBetweenPrompts.Clear();
            atFileEnd = false;
          }

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
      reader?.Dispose();
      stream?.Dispose();
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
