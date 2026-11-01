using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Trizbort.Automap.Utility;

/// <summary>
///   A <see cref="StreamReader" /> that can look ahead: lines read by <see cref="PeekReadLine" /> or
///   <see cref="IsAtEndAsync" /> are queued and returned again by the next reads, sync or async.
/// </summary>
public class PeekingStreamReader : StreamReader
{
  private readonly Queue<string> _peeks;

  public PeekingStreamReader(Stream stream) : base(stream)
  {
    _peeks = new Queue<string>();
  }

  public override Task<string> ReadLineAsync()
  {
    return ReadLineAsync(CancellationToken.None).AsTask();
  }

  public override ValueTask<string> ReadLineAsync(CancellationToken cancellationToken)
  {
    return _peeks.Count > 0 ? ValueTask.FromResult(_peeks.Dequeue()) : base.ReadLineAsync(cancellationToken);
  }

  public override string ReadLine()
  {
    if (_peeks.Count <= 0) return base.ReadLine();

    var nextLine = _peeks.Dequeue();
    return nextLine;
  }

  public string PeekReadLine()
  {
    var nextLine = base.ReadLine();
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
    var nextLine = await base.ReadLineAsync(cancellationToken);
    if (nextLine == null) return true;
    _peeks.Enqueue(nextLine);
    return false;
  }
}
