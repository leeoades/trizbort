using System.Threading.Tasks;

namespace Trizbort.Editor;

/// <summary>Text clipboard abstraction; UI hosts wrap the system clipboard, tests use <see cref="InMemoryClipboard" />.</summary>
public interface IClipboard
{
  Task SetTextAsync(string text);

  /// <summary>The clipboard text, or null when it holds none.</summary>
  Task<string> GetTextAsync();
}

/// <summary>A clipboard that only lives in memory.</summary>
public sealed class InMemoryClipboard : IClipboard
{
  public string Text { get; set; }

  public Task SetTextAsync(string text)
  {
    Text = text;
    return Task.CompletedTask;
  }

  public Task<string> GetTextAsync()
  {
    return Task.FromResult(Text);
  }
}
