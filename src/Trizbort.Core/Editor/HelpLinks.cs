using System.Threading.Tasks;

namespace Trizbort.Editor;

/// <summary>Online help locations and the command handler that opens them (ticket 088).</summary>
public static class HelpLinks
{
  /// <summary>The user guide website (GitHub Pages, built from Docs/ at each release).</summary>
  public const string UserGuide = "https://leeoades.github.io/trizbort/";

  public const string Title = "Online Help";

  /// <summary>Opens <paramref name="uri" /> through the session prompts; shows a message when that fails.</summary>
  public static async Task OpenAsync(EditorSession session, string uri)
  {
    var opened = await session.Prompts.OpenUriAsync(uri);
    if (opened) return;

    await session.Prompts.MessageAsync($"Unable to open the link:\n\n{uri}", Title);
  }
}
