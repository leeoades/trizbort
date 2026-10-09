namespace Trizbort.Domain.Misc;

/// <summary>
///   "Depth" of an element, indicating its drawing order.
/// </summary>
public enum Depth {
  /// <summary>
  ///   Low depth. Draw before Medium and High.
  /// </summary>
  Low,

  /// <summary>
  ///   Medium depth. Draw before High.
  /// </summary>
  Medium,

  /// <summary>
  ///   High depth.
  /// </summary>
  High
}