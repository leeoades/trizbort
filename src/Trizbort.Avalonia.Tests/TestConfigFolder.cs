using System;
using System.IO;
using NUnit.Framework;
using Trizbort.Domain.AppSettings;

// Keeps every test in this assembly from reading or writing the real per-user settings folder.
[SetUpFixture]
public class TestConfigFolder
{
  private string _folder;

  [OneTimeSetUp]
  public void Start()
  {
    _folder = Path.Combine(Path.GetTempPath(), "Trizbort.Avalonia.Tests", Guid.NewGuid().ToString("N"));
    AppPaths.OverrideConfigFolder = _folder;
  }

  [OneTimeTearDown]
  public void Stop()
  {
    AppPaths.OverrideConfigFolder = null;
    try
    {
      if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }
    catch (IOException)
    {
    }
  }
}
