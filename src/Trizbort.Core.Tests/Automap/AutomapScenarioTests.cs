using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

    var result = await HeadlessAutomap.RunAsync(
      project, Path.Combine(folder, TranscriptFile), LoadSettings(folder), NonInteractiveEditorPrompts.Instance);
    var actual = AutomapMapSummary.Describe(project);
    if (!result.Succeeded) actual = "error: " + result.Error + "\n\n" + actual;

    var expectedPath = Path.Combine(folder, ExpectedFile);
    var actualPath = Path.Combine(folder, ActualFile);
    if (Environment.GetEnvironmentVariable("TRIZBORT_APPROVE_AUTOMAP") == "1")
    {
      File.WriteAllText(expectedPath, actual);
      DeleteIfPresent(actualPath);
      return;
    }

    if (!File.Exists(expectedPath))
    {
      File.WriteAllText(actualPath, actual);
      Assert.Inconclusive($"No {ExpectedFile} in tests/automap/{name}. The map automap made is in {ActualFile}:\n\n{actual}");
    }

    var expectedLines = AutomapMapSummary.Normalise(File.ReadAllText(expectedPath));
    var actualLines = AutomapMapSummary.Normalise(actual);
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
    var report = Difference(expectedLines, actualLines);
    if (File.Exists(knownIssuePath))
      Assert.Ignore($"Known issue: {File.ReadAllText(knownIssuePath).Trim()}\n\n{report}");
    Assert.Fail($"tests/automap/{name}: the map differs from {ExpectedFile} (full map in {ActualFile}).\n\n{report}");
  }

  /// <summary>The tests/automap folder of this repository, found by walking up from the test output folder.</summary>
  private static string ScenarioRoot()
  {
    var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Trizbort.sln")))
      directory = directory.Parent;
    return directory == null ? null : Path.Combine(directory.FullName, "tests", "automap");
  }

  /// <summary>Defaults as in the app's Automap Settings dialog, overridden by "Name = value" lines in settings.txt.</summary>
  private static AutomapSettings LoadSettings(string folder)
  {
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
      var field = typeof(AutomapSettings).GetField(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                  ?? throw new FormatException($"{SettingsFile}: unknown setting '{key}'.");
      field.SetValue(settings, Convert.ChangeType(value, field.FieldType));
    }

    return (AutomapSettings)settings;
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
