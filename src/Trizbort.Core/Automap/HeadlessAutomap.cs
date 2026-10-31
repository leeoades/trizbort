using System;
using System.IO;
using System.Threading.Tasks;
using Trizbort.Domain.Application;
using Trizbort.Editor;

namespace Trizbort.Automap;

/// <summary>The outcome of a headless automap run: Error is null on success.</summary>
public sealed record HeadlessAutomapResult(string Transcript, string Error)
{
  public bool Succeeded => Error == null;
}

/// <summary>
///   Runs the automap engine over a whole transcript with no UI (the <c>--automap</c> command-line flag, ticket 107).
///   Questions are answered by the supplied prompts (use <see cref="NonInteractiveEditorPrompts" /> for the legacy
///   defaults: keep room 1, first match).
/// </summary>
public static class HeadlessAutomap
{
  /// <summary>
  ///   Maps <paramref name="transcriptPath" /> into <paramref name="project" /> and returns when the transcript has been
  ///   read to its end. Marks the project as not dirty afterwards, as the legacy command line did, so that exiting does
  ///   not ask about unsaved changes. Never throws for automap failures.
  /// </summary>
  public static async Task<HeadlessAutomapResult> RunAsync(
    Project project, string transcriptPath, AutomapSettings settings, IEditorPrompts prompts)
  {
    if (project == null) throw new ArgumentNullException(nameof(project));
    if (string.IsNullOrWhiteSpace(transcriptPath))
      return new HeadlessAutomapResult(transcriptPath, "No transcript file was given.");
    if (!File.Exists(transcriptPath))
      return new HeadlessAutomapResult(transcriptPath, "Transcript file not found: " + transcriptPath);

    string error = null;
    var engine = new Automap((message, title) => error ??= message, prompts ?? NonInteractiveEditorPrompts.Instance);
    engine.AutomapFailed += ex => error ??= ex.Message;

    settings.FileName = transcriptPath;
    settings.SingleStep = false;
    settings.ContinueTranscript = false;
    if (!settings.VerboseTranscript && !settings.AssumeRoomsWithSameNameAreSameRoom)
      settings.AssumeRoomsWithSameNameAreSameRoom = true;

    try
    {
      await engine.StartCl(new AutomapCanvas(project, engine), settings);
    }
    catch (Exception ex)
    {
      error ??= ex.Message;
    }
    finally
    {
      project.IsDirty = false;
    }

    return new HeadlessAutomapResult(transcriptPath, error);
  }
}
