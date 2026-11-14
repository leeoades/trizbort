using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Maps a scenario's transcript with automap's tidy as you go on, and saves the map (with its layout) to
///   TRIZBORT_LAYOUT_OUT, or the temp folder, as &lt;scenario&gt;.trizbort, to look at how neat the layout is.
///   Run by hand.
/// </summary>
[TestFixture]
[Explicit]
[Category("LayoutRun")]
public class AutomapLayoutRuns : IsolatedProjectTests
{
  [TestCase("hollywood-hijinx-nov-6", AutomapSameDirectionResult.KeepRoom1)]
  [TestCase("hollywood-hijinx-old", AutomapSameDirectionResult.KeepBoth)]
  [TestCase("hollywood-hijinx-opening", AutomapSameDirectionResult.KeepRoom1)]
  public async Task Run(string scenario, AutomapSameDirectionResult sameDirection)
  {
    var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (root != null && !File.Exists(Path.Combine(root.FullName, "Trizbort.sln"))) root = root.Parent;
    var transcript = Path.Combine(root!.FullName, "tests", "automap", scenario, "transcript.txt");

    var project = new Project();
    Project.Current = project;
    var settings = AutomapSettings.Default;
    settings.AutoTidy = true;
    var repeatable = Environment.GetEnvironmentVariable("TRIZBORT_LAYOUT_AS_APP") != "1";
    var result = await HeadlessAutomap.RunAsync(project, transcript, settings, new Prompts(sameDirection), repeatable);
    TestContext.Out.WriteLine(result.Succeeded ? "mapped" : "error: " + result.Error);

    var outFolder = Environment.GetEnvironmentVariable("TRIZBORT_LAYOUT_OUT") ?? Path.GetTempPath();
    var path = Path.Combine(outFolder, scenario + ".trizbort");
    project.FileName = path;
    Assert.That(project.Save(), Is.True, "saving " + path);
    TestContext.Out.WriteLine("saved " + path);
  }

  /// <summary>
  ///   Maps the transcript again and again, one more command each time, saving the map after each command as
  ///   &lt;scenario&gt;-&lt;step&gt;.trizbort, to find the move where the layout starts going wrong.
  /// </summary>
  [TestCase("hollywood-hijinx-nov-6", AutomapSameDirectionResult.KeepRoom1)]
  [TestCase("hollywood-hijinx-old", AutomapSameDirectionResult.KeepBoth)]
  public async Task Steps(string scenario, AutomapSameDirectionResult sameDirection)
  {
    var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (root != null && !File.Exists(Path.Combine(root.FullName, "Trizbort.sln"))) root = root.Parent;
    var lines = File.ReadAllLines(Path.Combine(root!.FullName, "tests", "automap", scenario, "transcript.txt"));
    var outFolder = Path.Combine(Environment.GetEnvironmentVariable("TRIZBORT_LAYOUT_OUT") ?? Path.GetTempPath(), scenario);
    Directory.CreateDirectory(outFolder);
    var commands = Enumerable.Range(0, lines.Length).Where(i => lines[i].StartsWith(">")).ToList();
    for (var step = 1; step <= commands.Count; step++)
    {
      // everything up to the next command: this command and what the game said back
      var end = step < commands.Count ? commands[step] : lines.Length;
      var transcript = Path.Combine(outFolder, "transcript.txt");
      File.WriteAllLines(transcript, lines.Take(end));
      var project = new Project();
      Project.Current = project;
      var settings = AutomapSettings.Default;
      settings.AutoTidy = true;
      await HeadlessAutomap.RunAsync(project, transcript, settings, new Prompts(sameDirection));
      project.FileName = Path.Combine(outFolder, $"{step:000} {Sanitise(lines[commands[step - 1]])}.trizbort");
      project.Save();
    }
  }

  private static string Sanitise(string command) =>
    new string(command.TrimStart('>').Trim().Select(c => char.IsLetterOrDigit(c) || c == ' ' ? c : '_').ToArray());

  private sealed class Prompts : IEditorPrompts
  {
    private readonly AutomapSameDirectionResult _sameDirection;
    public Prompts(AutomapSameDirectionResult sameDirection) => _sameDirection = sameDirection;

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Trizbort.Domain.Elements.Room existing, string newRoomName, System.Threading.CancellationToken token = default)
      => Task.FromResult(_sameDirection);

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine, string roomDescription,
      System.Collections.Generic.IReadOnlyList<Trizbort.Domain.Elements.Room> candidates,
      System.Threading.CancellationToken token = default)
      => NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);

    public Task<bool> ConfirmAsync(string message, string title, System.Threading.CancellationToken token = default)
      => NonInteractiveEditorPrompts.Instance.ConfirmAsync(message, title, token);
  }
}
