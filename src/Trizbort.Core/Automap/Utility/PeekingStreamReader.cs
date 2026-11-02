using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Trizbort.Automap.Utility;

/// <summary>
///   A line reader that can look ahead: lines read by <see cref="PeekReadLine" /> or <see cref="IsAtEndAsync" /> are
///   queued and returned again by the next reads, sync or async.
/// </summary>
/// <remarks>
///   It wraps a <see cref="StreamReader" /> rather than deriving from one. A class derived from StreamReader doesn't get
///   StreamReader's own async line reading (it falls back to the base TextReader's), so calling base.ReadLineAsync
///   from an override is fragile; the wrapped reader is a plain StreamReader and reads asynchronously.
/// </remarks>
public sealed class PeekingStreamReader : TextReader
{
  private readonly StreamReader _inner;
  private readonly Queue<string> _peeks = new();

  public PeekingStreamReader(Stream stream)
  {
    _inner = new StreamReader(stream);
  }

  public override Task<string> ReadLineAsync()
  {
    return ReadLineAsync(CancellationToken.None).AsTask();
  }

  public override ValueTask<string> ReadLineAsync(CancellationToken cancellationToken)
  {
    return _peeks.Count > 0 ? ValueTask.FromResult(_peeks.Dequeue()) : _inner.ReadLineAsync(cancellationToken);
  }

  public override string ReadLine()
  {
    return _peeks.Count > 0 ? _peeks.Dequeue() : _inner.ReadLine();
  }

  public string PeekReadLine()
  {
    var nextLine = _inner.ReadLine();
    _peeks.Enqueue(nextLine);
    return nextLine;
  }

  /// <summary>
  ///   True when there is nothing more to read yet. Unlike <see cref="StreamReader.EndOfStream" />, this never blocks
  ///   the thread on file I/O (CA2024). A line read to find out is kept for the next read.
  /// </summary>
  public async Task<bool> IsAtEndAsync(CancellationToken cancellationToken = default)
  {
    if (_peeks.Count > 0) return false;
    var nextLine = await _inner.ReadLineAsync(cancellationToken);
    if (nextLine == null) return true;
    _peeks.Enqueue(nextLine);
    return false;
  }

  // Character reads would skip queued lines, so they aren't offered.
  public override int Peek() => throw new System.NotSupportedException("PeekingStreamReader reads whole lines.");

  public override int Read() => throw new System.NotSupportedException("PeekingStreamReader reads whole lines.");

  protected override void Dispose(bool disposing)
  {
    if (disposing) _inner.Dispose();
    base.Dispose(disposing);
  }
}
