using System.Threading.Tasks;

namespace Trizbort.Editor;

/// <summary>Online help locations and the command handler that opens them (ticket 088).</summary>
public static class HelpLinks
{
  public const string UserGuide = "https://github.com/leeoades/trizbort/blob/master/Docs/index.md";

  public const string OriginalHelp = "https://trizbort.genstein.net/help/";

  public const string Title = "Online Help";

  /// <summary>Opens <paramref name="uri" /> through the session prompts; shows a message when that fails.</summary>
  public static async Task OpenAsync(EditorSession session, string uri)
  {
    var opened = await session.Prompts.OpenUriAsync(uri);
    if (opened) return;

    await session.Prompts.MessageAsync($"Unable to open the link:\n\n{uri}", Title);
  }
}
