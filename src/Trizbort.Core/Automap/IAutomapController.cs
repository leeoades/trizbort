using System;

namespace Trizbort.Automap;

/// <summary>
///   What the editor needs from the automap engine while a run is in progress: its state, the single-step keys and
///   its events. <see cref="Automap" /> implements it; tests substitute a fake.
/// </summary>
public interface IAutomapController
{
  bool IsRunning { get; }

  /// <summary>True while the current run waits for <see cref="Step" /> between commands (debug only).</summary>
  bool SingleStep { get; }

  string Status { get; }

  void Step();

  void RunToCompletion();

  /// <summary>Raised with the new status text each time it is set. May be raised off the UI thread.</summary>
  event Action<string> StatusChanged;

  /// <summary>Raised when a run stops because of an error. May be raised off the UI thread.</summary>
  event Action<Exception> AutomapFailed;
}
