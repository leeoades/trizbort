using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Editor;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 110: Smart Save planning and writing without any UI.</summary>
[TestFixture]
[Category("Unit")]
public class SmartSaveServiceTests
{
  private const string MapName = "/maps/castle.trizbort";

  private Project _project;

  private sealed class FakeWriters : IMapImageWriter, IMapPdfWriter
  {
    public List<string> Written { get; } = new();
    public ImageFormat LastFormat { get; private set; }
    public string PdfTitle { get; private set; }
    public bool FailPdf { get; set; }
    public bool FailImage { get; set; }

    public Task WriteAsync(Project project, string path, ImageFormat format)
    {
      if (FailImage) throw new InvalidOperationException("image disk full");
      Written.Add(path);
      LastFormat = format;
      return Task.CompletedTask;
    }

    public Task WriteAsync(Project project, string path, PdfMetadata metadata)
    {
      if (FailPdf) throw new InvalidOperationException("pdf locked");
      Written.Add(path);
      PdfTitle = metadata.Title;
      return Task.CompletedTask;
    }
  }

  [SetUp]
  public void SetUp()
  {
    _project = new Project { FileName = MapName };
    _project.IsDirty = false;
  }

  [TearDown]
  public void TearDown()
  {
    _project.Dispose();
  }

  private static ApplicationSettings Settings(bool pdf, bool image, int imageType = 0)
  {
    return new ApplicationSettings { SaveToPDF = pdf, SaveToImage = image, DefaultImageType = imageType };
  }

  [Test]
  public void Plan_BothOutputs_UsesMapNameWithPdfAndImageExtensions()
  {
    var plan = SmartSaveService.Plan(_project, Settings(true, true));

    plan.CanRun.ShouldBeTrue();
    plan.PdfPath.ShouldBe(SmartSaveService.PdfPath(MapName));
    plan.PdfPath.ShouldEndWith("castle.pdf");
    plan.ImagePath.ShouldEndWith("castle.png");
  }

  [Test]
  public void Plan_ImageExtensionFollowsDefaultImageType()
  {
    SmartSaveService.Plan(_project, Settings(false, true, 1)).ImagePath.ShouldEndWith("castle.jpg");
    SmartSaveService.Plan(_project, Settings(false, true, 2)).ImagePath.ShouldEndWith("castle.bmp");
    SmartSaveService.Plan(_project, Settings(false, true, 3)).ImagePath.ShouldEndWith("castle.png");
  }

  [Test]
  public void Plan_OnlyPdf_HasNoImagePath()
  {
    var plan = SmartSaveService.Plan(_project, Settings(true, false));

    plan.PdfPath.ShouldNotBeNull();
    plan.ImagePath.ShouldBeNull();
  }

  [Test]
  public void Plan_BothOff_CannotRunAndExplains()
  {
    var plan = SmartSaveService.Plan(_project, Settings(false, false));

    plan.CanRun.ShouldBeFalse();
    plan.NeedsSave.ShouldBeFalse();
    plan.Reason.ShouldStartWith("Your settings are set to not save anything.");
  }

  [Test]
  public void Plan_DirtyProject_NeedsSave()
  {
    _project.IsDirty = true;

    var plan = SmartSaveService.Plan(_project, Settings(true, true));

    plan.CanRun.ShouldBeFalse();
    plan.NeedsSave.ShouldBeTrue();
    plan.Reason.ShouldBe(SmartSaveService.NeedsSaveMessage);
    plan.PdfPath.ShouldBeNull();
  }

  [Test]
  public void Plan_UnsavedAndUrlProjects_NeedSave()
  {
    _project.FileName = string.Empty;
    SmartSaveService.Plan(_project, Settings(true, true)).NeedsSave.ShouldBeTrue();

    _project.FileName = "https://example.com/map.trizbort";
    SmartSaveService.Plan(_project, Settings(true, true)).NeedsSave.ShouldBeTrue();
  }

  [Test]
  public async Task Run_WritesBothFilesAndSummarisesThem()
  {
    var writers = new FakeWriters();
    _project.Title = "The Castle";
    var plan = SmartSaveService.Plan(_project, Settings(true, true, 1));

    var result = await SmartSaveService.RunAsync(_project, plan, writers, writers);

    result.Succeeded.ShouldBeTrue();
    writers.Written.Count.ShouldBe(2);
    writers.LastFormat.ShouldBe(ImageFormat.Jpeg);
    writers.PdfTitle.ShouldBe("The Castle");
    result.Summary.ShouldContain("PDF file has been saved to " + plan.PdfPath);
    result.Summary.ShouldContain("Image file has been saved to " + plan.ImagePath);
  }

  [Test]
  public async Task Run_PdfFails_ImageStillWritten()
  {
    var writers = new FakeWriters { FailPdf = true };
    var plan = SmartSaveService.Plan(_project, Settings(true, true));

    var result = await SmartSaveService.RunAsync(_project, plan, writers, writers);

    result.Succeeded.ShouldBeFalse();
    result.Files.Count.ShouldBe(2);
    result.Files[0].Error.ShouldBe("pdf locked");
    result.Files[1].Succeeded.ShouldBeTrue();
    writers.Written.ShouldBe(new[] { plan.ImagePath });
    result.Summary.ShouldContain("There was an error saving the PDF file");
    result.Summary.ShouldContain("pdf locked");
    result.Summary.ShouldContain("Image file has been saved to");
  }

  [Test]
  public async Task Run_NoPdfWriter_ReportsNotSupportedWithoutFailingImage()
  {
    var writers = new FakeWriters();
    var plan = SmartSaveService.Plan(_project, Settings(true, true));

    var result = await SmartSaveService.RunAsync(_project, plan, writers, null);

    result.Files[0].Kind.ShouldBe(SmartSaveKind.Pdf);
    result.Files[0].Error.ShouldContain("not supported");
    result.Files[1].Succeeded.ShouldBeTrue();
  }

  [Test]
  public async Task Run_PlanThatCannotRun_WritesNothing()
  {
    var writers = new FakeWriters();
    _project.IsDirty = true;

    var needsSave = await SmartSaveService.RunAsync(_project, SmartSaveService.Plan(_project, Settings(true, true)), writers, writers);
    var nothingOn = await SmartSaveService.RunAsync(_project, SmartSaveService.Plan(_project, Settings(false, false)), writers, writers);

    writers.Written.ShouldBeEmpty();
    needsSave.Summary.ShouldBe(SmartSaveService.NoFilesMessage);
    needsSave.Succeeded.ShouldBeFalse();
    nothingOn.Summary.ShouldBe(SmartSaveService.NothingEnabledMessage);
  }

  [Test]
  public async Task RunSilent_WritesWithoutPrompts()
  {
    var writers = new FakeWriters();

    var result = await SmartSaveService.RunSilentAsync(_project, Settings(false, true), writers, writers);

    result.Succeeded.ShouldBeTrue();
    writers.Written.Count.ShouldBe(1);
  }

  [Test]
  public async Task WriterExporter_WritesNextToMapAndReturnsPath()
  {
    var writers = new FakeWriters();
    var exporter = new SmartSaveWriterExporter(SmartSaveKind.Pdf, writers, writers, () => Settings(true, true));
    var previous = Project.Current;
    Project.Current = _project;
    try
    {
      using var session = new EditorSession();
      var path = await exporter.ExportAsync(session, MapName);

      path.ShouldBe(SmartSaveService.PdfPath(MapName));
      writers.Written.ShouldBe(new[] { path });
    }
    finally
    {
      Project.Current = previous;
    }
  }
}
