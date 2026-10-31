using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Trizbort.Domain.Application;

namespace Trizbort.Editor;

/// <summary>Downloads a map. Faked in tests so nothing touches the network.</summary>
public interface IMapDownloader
{
  /// <summary>The bytes at <paramref name="uri" />. Throws with a readable message when the download fails.</summary>
  Task<byte[]> DownloadAsync(Uri uri, CancellationToken token = default);
}

/// <summary>Downloads over HTTP(S) with a shared <see cref="HttpClient" />.</summary>
public sealed class HttpMapDownloader : IMapDownloader
{
  private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(30) };

  public async Task<byte[]> DownloadAsync(Uri uri, CancellationToken token = default)
  {
    using var response = await _client.GetAsync(uri, token).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
      throw new InvalidOperationException($"the server answered {(int)response.StatusCode} {response.ReasonPhrase}");
    return await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
  }
}

/// <summary>Validation of the text typed into the Load from Web dialog.</summary>
public static class MapUrl
{
  public const string InvalidUrlMessage = "Please enter a valid http or https address.";

  /// <summary>Accepts only absolute http and https addresses (surrounding white space is ignored).</summary>
  public static bool TryParse(string text, out Uri uri, out string error)
  {
    uri = null;
    error = null;
    var trimmed = text == null ? string.Empty : text.Trim();
    if (trimmed.Length == 0)
    {
      error = "Please enter an address.";
      return false;
    }

    if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed) ||
        (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
    {
      error = InvalidUrlMessage;
      return false;
    }

    uri = parsed;
    return true;
  }

  /// <summary>The last path segment of <paramref name="uri" />, or the host when there is none.</summary>
  public static string FileName(Uri uri)
  {
    var segments = uri.Segments;
    var last = segments.Length > 0 ? Uri.UnescapeDataString(segments[segments.Length - 1]).Trim('/') : string.Empty;
    return last.Length > 0 ? last : uri.Host;
  }
}

/// <summary>File &gt; Open Map from Web (tickets 060 and 089).</summary>
public static class OpenFromUrl
{
  public const string Title = "Load from Web";

  /// <summary>
  ///   Checks for unsaved changes (<paramref name="canLoseProject" />), asks for a URL and opens it. Returns true when a
  ///   map was opened. Failures are shown as "Could not open &lt;url&gt;: &lt;reason&gt;"; nothing is thrown.
  /// </summary>
  public static async Task<bool> RunAsync(
    EditorSession session, IMapDownloader downloader, Func<Task<bool>> canLoseProject, Action<string> recordOpened,
    CancellationToken token = default)
  {
    if (canLoseProject != null && !await canLoseProject()) return false;

    var text = await session.Prompts.PromptUrlAsync(Title, "URL:", token);
    if (string.IsNullOrWhiteSpace(text)) return false;

    return await OpenAsync(session, downloader, text, recordOpened, token);
  }

  /// <summary>Downloads and loads the map at <paramref name="text" />, replacing the current project.</summary>
  public static async Task<bool> OpenAsync(
    EditorSession session, IMapDownloader downloader, string text, Action<string> recordOpened,
    CancellationToken token = default)
  {
    if (!MapUrl.TryParse(text, out var uri, out var error))
    {
      await Fail(session, text == null ? string.Empty : text.Trim(), error);
      return false;
    }

    var url = uri.AbsoluteUri;
    var project = new Project();
    Exception loadError = null;
    bool loaded;
    try
    {
      var bytes = await downloader.DownloadAsync(uri, token);
      var engine = new LegacyMapFileEngine(project, ex => loadError = ex, p => p.CheckDocVersion(), (m, t) => { });
      using (var stream = new MemoryStream(bytes))
        loaded = engine.Load(stream);
    }
    catch (Exception ex)
    {
      await Fail(session, url, ex.Message);
      return false;
    }

    if (!loaded)
    {
      await Fail(session, url, loadError != null ? loadError.Message : "the file is not a valid Trizbort map.");
      return false;
    }

    // The URL is the file name so that Save redirects to Save As and the Recent Maps list keeps the address.
    project.FileName = url;
    project.IsDirty = false;
    session.ReplaceProject(project);
    session.OnMapLoaded();
    recordOpened?.Invoke(url);
    return true;
  }

  private static Task Fail(EditorSession session, string url, string reason)
  {
    return session.Prompts.ShowMessageAsync(Title, $"Could not open {url}: {reason}");
  }
}
