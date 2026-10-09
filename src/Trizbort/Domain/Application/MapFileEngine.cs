namespace Trizbort.Domain.Application;

public abstract class MapFileEngine {
  private readonly string _fileName;

  protected MapFileEngine()
  {
  }

  protected MapFileEngine(string fileName)
  {
    _fileName = fileName;
  }

  public virtual bool Load()
  {
    return Load(_fileName);
  }

  public virtual bool Load(string fileName)
  {
    return false;
  }

  public virtual bool Save()
  {
    return Save(_fileName);
  }

  public virtual bool Save(string fileName)
  {
    return false;
  }
}