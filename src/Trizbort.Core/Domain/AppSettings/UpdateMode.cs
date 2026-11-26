namespace Trizbort.Domain.AppSettings;

/// <summary>How Recast updates itself (App Settings, General). Lee, 26 Nov 2026.</summary>
public enum UpdateMode
{
  /// <summary>Tell me when there's a new version, and let me choose when to download it.</summary>
  Notify = 0,

  /// <summary>Download new versions, and let me choose when to update.</summary>
  DownloadAndAsk = 1,

  /// <summary>Download new versions, and update when Recast closes.</summary>
  Automatic = 2
}
