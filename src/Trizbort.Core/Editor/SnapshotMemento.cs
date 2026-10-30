using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

/// <summary>
///   Whole-map snapshot (D1 point 3): the map XML written by <see cref="LegacyMapFileEngine" />, gzip-compressed,
///   plus the selection as element ids.
/// </summary>
internal sealed class SnapshotMemento : IUndoMemento
{
  private readonly byte[] _data;
  private readonly byte[] _hash;
  private readonly int[] _selectedIds;

  private SnapshotMemento(byte[] data, byte[] hash, int[] selectedIds)
  {
    _data = data;
    _hash = hash;
    _selectedIds = selectedIds;
  }

  public long SizeBytes => _data.Length;

  /// <summary>Size of the compressed map data alone, in bytes.</summary>
  internal int CompressedLength => _data.Length;

  /// <summary>Length of the uncompressed map XML, in bytes.</summary>
  internal int XmlLength { get; private set; }

  /// <summary>True if the map XML of this snapshot equals the map XML whose hash is given.</summary>
  internal bool HasSameMap(byte[] hash)
  {
    return CryptographicOperations.FixedTimeEquals(_hash, hash);
  }

  public static SnapshotMemento Capture(EditorSession session)
  {
    var xml = Serialize(session.Project);
    var length = (int)xml.Length;
    var hash = Hash(xml);

    var compressed = new MemoryStream();
    using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, true))
    {
      gzip.Write(xml.GetBuffer(), 0, length);
    }

    var ids = session.Selection.Items.Select(element => element.Id).ToArray();
    return new SnapshotMemento(compressed.ToArray(), hash, ids) { XmlLength = length };
  }

  /// <summary>Hash of the current map XML, used to detect "nothing changed" without compressing.</summary>
  public static byte[] ComputeHash(EditorSession session)
  {
    return Hash(Serialize(session.Project));
  }

  public void Restore(EditorSession session)
  {
    var xml = new MemoryStream();
    using (var input = new MemoryStream(_data))
    using (var gzip = new GZipStream(input, CompressionMode.Decompress))
    {
      gzip.CopyTo(xml);
    }
    xml.Position = 0;

    // Element loading reads Project.Current, so the fresh project must be current while it loads.
    var fresh = new Project();
    fresh.CopyTransientStateFrom(session.Project);
    session.ReplaceProject(fresh);

    Exception error = null;
    var engine = new LegacyMapFileEngine(fresh, ex => error = ex, _ => { }, (message, title) => { });
    if (!engine.Load(xml))
      throw new InvalidOperationException("The undo snapshot could not be restored.", error);

    var selected = new List<Element>();
    foreach (var id in _selectedIds)
      if (fresh.FindElement(id, out var element))
        selected.Add(element);
    session.Selection.Set(selected);

    session.Invalidate(EditorChange.Model | EditorChange.Selection | EditorChange.Adorners);
  }

  private static MemoryStream Serialize(Project project)
  {
    var xml = new MemoryStream();
    new LegacyMapFileEngine(project).Save(xml);
    return xml;
  }

  private static byte[] Hash(MemoryStream xml)
  {
    return SHA256.HashData(new ReadOnlySpan<byte>(xml.GetBuffer(), 0, (int)xml.Length));
  }
}
