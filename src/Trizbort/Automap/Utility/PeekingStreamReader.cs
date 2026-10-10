using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Trizbort.Automap.Utility;

public class PeekingStreamReader : StreamReader
{
  private readonly Queue<string> _peeks;

  public PeekingStreamReader(Stream stream) : base(stream)
  {
    _peeks = new Queue<string>();
  }

  public override Task<string> ReadLineAsync()
  {
    return Task.Run(() => ReadLine());
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
}