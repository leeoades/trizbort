using System;
using System.Collections.Generic;
using System.Linq;

namespace Trizbort.Setup;

/// <summary>
///   Region naming rules of the legacy map settings dialog: names are trimmed, double quotes become single
///   quotes, they can't be empty, can't contain '_' or ':' and can't repeat another region's name.
/// </summary>
public static class RegionNaming
{
  public const string EmptyMessage = "You can't have an empty region name.";
  public const string InvalidCharacterMessage = "Region names can't contain '_' or ':'.";

  /// <summary>Trims the name and replaces double quotes with single quotes.</summary>
  public static string Sanitize(string name)
  {
    return (name ?? string.Empty).Trim().Replace("\"", "'");
  }

  public static string DuplicateMessage(string name)
  {
    return $"A region already exists with the name '{name}'.";
  }

  /// <summary>
  ///   Returns an error message for the name, or an empty string when it is valid. <paramref name="existing" />
  ///   holds the names to clash with; a name equal (ignoring case) to <paramref name="current" />, the region's own
  ///   name, never counts as a duplicate.
  /// </summary>
  public static string Validate(string name, IEnumerable<string> existing, string current)
  {
    var sanitized = Sanitize(name);
    if (sanitized.Length == 0) return EmptyMessage;
    if (sanitized.IndexOfAny(new[] { '_', ':' }) >= 0) return InvalidCharacterMessage;
    if (current != null && sanitized.Equals(current, StringComparison.OrdinalIgnoreCase)) return string.Empty;
    if (existing != null && existing.Any(p => p != null && p.Equals(sanitized, StringComparison.OrdinalIgnoreCase)))
      return DuplicateMessage(sanitized);
    return string.Empty;
  }
}
