using System;

namespace Trizbort.Domain.Misc;

/// <summary>
///   Numeric functions.
/// </summary>
internal static class Numeric
{
  /// <summary>
  ///   A "small" number.
  /// </summary>
  public static readonly float Small = 0.001f;

  /// <summary>
  ///   Test whether two numbers are approximately equal.
  /// </summary>
  /// <param name="a">The first number.</param>
  /// <param name="b">The second number.</param>
  /// <returns>True if approximately equal; false otherwise.</returns>
  public static bool ApproxEqual(float a, float b)
  {
    return Distance(a, b) <= Small;
  }

  /// <summary>
  ///   Clamp a value within a specific (inclusive) range.
  /// </summary>
  /// <param name="value">The value.</param>
  /// <param name="min">The inclusive lower bound of the range.</param>
  /// <param name="max">The inclusive upper bound of the range.</param>
  /// <returns>The value, or the lower/upper bound of the range.</returns>
  public static float Clamp(float value, float min, float max)
  {
    if (min > max) return Math.Max(max, Math.Min(min, value));
    return Math.Max(min, Math.Min(max, value));
  }


  /// <summary>
  ///   Get the absolute distance between two numbers.
  /// </summary>
  /// <param name="a">The first number.</param>
  /// <param name="b">The second number.</param>
  /// <returns>The absolute (positive) distance between the two.</returns>
  public static float Distance(float a, float b)
  {
    return Math.Abs(b - a);
  }

  /// <summary>
  ///   Test whether a value is within an (inclusive) range.
  /// </summary>
  /// <param name="value">The value.</param>
  /// <param name="min">The inclusive lower bound of the range.</param>
  /// <param name="max">The inclusive upper bound of the range.</param>
  /// <returns>True if the value is in range; false otherwise.</returns>
  public static bool InRange(float value, float min, float max)
  {
    if (min > max) return value >= max && value <= min;
    return value >= min && value <= max;
  }

  /// <summary>
  ///   Swap two numbers.
  /// </summary>
  /// <param name="a">The first number.</param>
  /// <param name="b">The second number.</param>
  public static void Swap(ref float a, ref float b)
  {
    var temp = a;
    a = b;
    b = temp;
  }
}