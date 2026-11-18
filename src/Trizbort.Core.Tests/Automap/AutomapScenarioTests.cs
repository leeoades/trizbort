using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Runs every transcript in <c>tests/automap/&lt;scenario&gt;/</c> through the automap engine (no UI) and compares
///   the resulting map with that folder's <c>expected.txt</c>. See tests/automap/README.md for the file formats.
///   <list type="bullet">
///     <item>No <c>expected.txt</c> yet: the test writes <c>actual.txt</c> and reports Inconclusive, so you can
///       copy it to <c>expected.txt</c> and correct it.</item>
///     <item>A mismatch writes <c>actual.txt</c> and fails with the differing lines.</item>
///     <item>A <c>known-issue.txt</c> marks a scenario that is expected to fail for now: the test is ignored with
///       that file's text, and fails once the map matches (so the marker gets removed).</item>
///     <item>Set <c>TRIZBORT_APPROVE_AUTOMAP=1</c> to overwrite <c>expected.txt</c> with the actual map.</item>
///   </list>
/// </summary>
[TestFixture]
[Category("AutomapScenario")]
public class AutomapScenarioTests : IsolatedProjectTests
{
  private const string TranscriptFile = "transcript.txt";
  private const string ExpectedFile = "expected.txt";
  private const string ActualFile = "actual.txt";
  private const string SettingsFile = "settings.txt";
  private const string KnownIssueFile = "known-issue.txt";
  private const string TraceFile = "trace.txt";

  public static IEnumerable<TestCaseData> Scenarios()
  {
    var root = ScenarioRoot();
    if (root == null || !Directory.Exists(root)) yield break;
    foreach (var folder in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
      if (File.Exists(Path.Combine(folder, TranscriptFile)))
        yield return new TestCaseData(Path.GetFileName(folder)).SetName("Scenario_" + Path.GetFileName(folder));
  }

  [TestCaseSource(nameof(Scenarios))]
  public async Task Scenario(string name)
  {
    var folder = Path.Combine(ScenarioRoot(), name);
    var project = new Project();
    Project.Current = project;

    // The engine explains each transcript line (room or not, and why) through Debug/Trace output; keep it so a
    // failing scenario can be read alongside its trace.txt.
    var trace = new StringWriter();
    var listener = new System.Diagnostics.TextWriterTraceListener(trace);
    System.Diagnostics.Trace.Listeners.Add(listener);
    HeadlessAutomapResult result;
    try
    {
      var settings = LoadSettings(folder, out var sameDirectionAnswer, out var whichRoomByDescription);
      result = await HeadlessAutomap.RunAsync(
        project, Path.Combine(folder, TranscriptFile), settings,
        new ScenarioPrompts(sameDirectionAnswer, whichRoomByDescription));
    }
    finally
    {
      listener.Flush();
      System.Diagnostics.Trace.Listeners.Remove(listener);
    }
    var actual = AutomapMapSummary.Describe(project, result.CurrentRoom);
    if (result.RoomNotOnMap != null) actual = "not on the map: " + result.RoomNotOnMap + "\n" + actual;
    if (!result.Succeeded)
    {
      actual = "error: " + result.Error + "\n\n" + actual;
      if (result.Failure != null) TestContext.Out.WriteLine(result.Failure);
    }

    var expectedPath = Path.Combine(folder, ExpectedFile);
    var actualPath = Path.Combine(folder, ActualFile);
    var tracePath = Path.Combine(folder, TraceFile);
    DeleteIfPresent(tracePath);
    if (Environment.GetEnvironmentVariable("TRIZBORT_APPROVE_AUTOMAP") == "1")
    {
      File.WriteAllText(expectedPath, actual);
      DeleteIfPresent(actualPath);
      return;
    }

    if (!File.Exists(expectedPath))
    {
      File.WriteAllText(actualPath, actual);
      File.WriteAllText(tracePath, trace.ToString());
      Assert.Inconclusive($"No {ExpectedFile} in tests/automap/{name}. The map automap made is in {ActualFile}:\n\n{actual}");
    }

    var expectedLines = AutomapMapSummary.Normalise(File.ReadAllText(expectedPath));
    var actualLines = AutomapMapSummary.Normalise(actual);
    if (!LayoutRepeatsHere)
    {
      expectedLines = WithoutLayout(expectedLines);
      actualLines = WithoutLayout(actualLines);
    }

    var matches = expectedLines.SequenceEqual(actualLines);
    var knownIssuePath = Path.Combine(folder, KnownIssueFile);

    if (matches)
    {
      DeleteIfPresent(actualPath);
      if (File.Exists(knownIssuePath))
        Assert.Fail($"tests/automap/{name} now matches {ExpectedFile}: the known issue is fixed, so delete {KnownIssueFile}.");
      return;
    }

    File.WriteAllText(actualPath, actual);
    File.WriteAllText(tracePath, trace.ToString());
    var report = Difference(expectedLines, actualLines);
    if (File.Exists(knownIssuePath))
      Assert.Ignore($"Known issue: {File.ReadAllText(knownIssuePath).Trim()}\n\n{report}");
    Assert.Fail($"tests/automap/{name}: the map differs from {ExpectedFile} (full map in {ActualFile}).\n\n{report}");
  }

  /// <summary>
  ///   Whether the layout comes out as in expected.txt. The layout search (CP-SAT, in the relayout as the map grows)
  ///   doesn't lay a map out quite the same on every system, even when repeatable, and expected.txt files are approved
  ///   on Windows. Elsewhere a scenario is compared on what's connected to what (<see cref="WithoutLayout" />).
  /// </summary>
  private static bool LayoutRepeatsHere => OperatingSystem.IsWindows();

  /// <summary>
  ///   A map summary without what depends on the layout: the measures of how neat it is, and the side an up, down,
  ///   in or out join leaves from (<c>[southsouthwest "d"]</c> becomes <c>["d"]</c>). Compass joins leave from their
  ///   direction's side, wherever the rooms are.
  /// </summary>
  private static List<string> WithoutLayout(IEnumerable<string> lines) =>
    lines.Where(line => !LayoutMeasure.IsMatch(line))
      .Select(line => LabelledSide.Replace(line, "[\""))
      .ToList();

  private static Regex LayoutMeasure { get; } = new(@"^(natural joins|exact joins|not natural|not exact):");
  private static Regex LabelledSide { get; } = new(@"\[[a-z]+ """);

  /// <summary>The tests/automap folder of this repository, found by walking up from the test output folder.</summary>
  private static string ScenarioRoot()
  {
    var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Trizbort.sln")))
      directory = directory.Parent;
    return directory == null ? null : Path.Combine(directory.FullName, "tests", "automap");
  }

  /// <summary>
  ///   Defaults as in the app's Automap Settings dialog, overridden by "Name = value" lines in settings.txt.
  ///   <c>SameDirectionAnswer</c> (KeepRoom1, KeepRoom2, KeepBoth) is the answer to "there's already a room that way";
  ///   <c>WhichRoomAnswer</c> (First, ByDescription) the answer to "which room is this?".
  /// </summary>
  private static AutomapSettings LoadSettings(
    string folder, out AutomapSameDirectionResult sameDirectionAnswer, out bool whichRoomByDescription)
  {
    // as the app does: keep both, side by side, without asking
    sameDirectionAnswer = AutomapSameDirectionResult.SamePlace;
    whichRoomByDescription = false;
    object settings = AutomapSettings.Default;
    var path = Path.Combine(folder, SettingsFile);
    if (!File.Exists(path)) return (AutomapSettings)settings;

    foreach (var raw in File.ReadAllLines(path))
    {
      var line = raw.Trim();
      if (line.Length == 0 || line.StartsWith('#')) continue;
      var equals = line.IndexOf('=');
      if (equals < 0) throw new FormatException($"{SettingsFile}: expected 'Name = value' but found '{raw}'.");
      var key = line[..equals].Trim();
      var value = line[(equals + 1)..].Trim();
      if (string.Equals(key, "SameDirectionAnswer", StringComparison.OrdinalIgnoreCase))
      {
        sameDirectionAnswer = Enum.Parse<AutomapSameDirectionResult>(value, true);
        continue;
      }

      if (string.Equals(key, "WhichRoomAnswer", StringComparison.OrdinalIgnoreCase))
      {
        whichRoomByDescription = value.Equals("ByDescription", StringComparison.OrdinalIgnoreCase) ? true
          : value.Equals("First", StringComparison.OrdinalIgnoreCase) ? false
          : throw new FormatException($"{SettingsFile}: WhichRoomAnswer is First or ByDescription, not '{value}'.");
        continue;
      }

      var field = typeof(AutomapSettings).GetField(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                  ?? throw new FormatException($"{SettingsFile}: unknown setting '{key}'.");
      field.SetValue(settings, Convert.ChangeType(value, field.FieldType));
    }

    return (AutomapSettings)settings;
  }

  /// <summary>
  ///   The app's non-interactive answers, except "there's already a room that way" and "which room is this?", which a
  ///   scenario may set. By description, "which room?" is answered as a player would: the room with the description the
  ///   game shows, or a new room when none has it.
  /// </summary>
  private sealed class ScenarioPrompts : IEditorPrompts
  {
    private readonly AutomapSameDirectionResult _sameDirection;
    private readonly bool _whichRoomByDescription;

    public ScenarioPrompts(AutomapSameDirectionResult sameDirection, bool whichRoomByDescription)
    {
      _sameDirection = sameDirection;
      _whichRoomByDescription = whichRoomByDescription;
    }

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Trizbort.Domain.Elements.Room existing, string newRoomName, System.Threading.CancellationToken token = default)
      => Task.FromResult(_sameDirection);

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine, string roomDescription, IReadOnlyList<Trizbort.Domain.Elements.Room> candidates,
      System.Threading.CancellationToken token = default)
    {
      if (!_whichRoomByDescription)
        return NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);
      var described = candidates.FirstOrDefault(room =>
        room.Descriptions.Any(known => Trizbort.Automap.Automap.SameFirstSentence(known, roomDescription)));
      return Task.FromResult(described != null ? RoomDisambiguation.Choose(described) : RoomDisambiguation.CreateNew());
    }

    public Task<bool> ConfirmAsync(string message, string title, System.Threading.CancellationToken token = default)
      => NonInteractiveEditorPrompts.Instance.ConfirmAsync(message, title, token);
  }

  private static string Difference(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
  {
    var builder = new StringBuilder();
    foreach (var line in expected.Except(actual)) builder.Append("- expected, missing: ").Append(line).Append('\n');
    foreach (var line in actual.Except(expected)) builder.Append("+ not expected:      ").Append(line).Append('\n');
    if (builder.Length == 0) builder.Append("Same lines in a different order.\n");
    return builder.ToString();
  }

  private static void DeleteIfPresent(string path)
  {
    if (File.Exists(path)) File.Delete(path);
  }
}
