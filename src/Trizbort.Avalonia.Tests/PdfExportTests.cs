using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using PdfSharp.Pdf.IO;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Export;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class PdfExportTests
{
    private Project? _previousProject;
    private string _folder = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        Trizbort.Setup.Settings.Reset();
        _folder = Path.Combine(Path.GetTempPath(), "trizbort-pdf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
        Trizbort.Setup.Settings.Reset();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Test]
    public void Metadata_FromProject_UsesFallbacksWhenEmpty()
    {
        var metadata = PdfMetadata.FromProject(new Project());
        metadata.Title.ShouldBe("A Trizbort Map");
        metadata.Author.ShouldBe("A Trizbort User");
        metadata.Subject.ShouldBe(string.Empty);
    }

    [Test]
    public void Metadata_FromProject_UsesProjectValues()
    {
        var project = new Project { Title = "Cave", Author = "Ada", Description = "Underground" };
        var metadata = PdfMetadata.FromProject(project);
        metadata.Title.ShouldBe("Cave");
        metadata.Author.ShouldBe("Ada");
        metadata.Subject.ShouldBe("Underground");
    }

    [AvaloniaTest]
    public void WriteAsync_SampleMap_WritesSinglePageVectorPdfWithMetadata()
    {
        var project = Project.Current;
        project.Title = "PdfTitleXyz";
        project.Author = "PdfAuthorXyz";
        project.Description = "PdfSubjectXyz";
        var first = new Room(project) { Name = "Hall", Position = new CoreVector(0, 0), Size = new CoreVector(64, 48) };
        var second = new Room(project) { Name = "Cellar", Position = new CoreVector(200, 120), Size = new CoreVector(64, 48) };
        project.Elements.Add(first);
        project.Elements.Add(second);

        var path = Path.Combine(_folder, "map.pdf");
        new SkiaMapPdfWriter().WriteAsync(project, path, PdfMetadata.FromProject(project)).GetAwaiter().GetResult();

        var bytes = File.ReadAllBytes(path);
        bytes.Length.ShouldBeGreaterThan(500);
        Encoding.ASCII.GetString(bytes, 0, 5).ShouldBe("%PDF-");

        var text = Encoding.Latin1.GetString(bytes);
        Regex.Matches(text, @"/Type\s*/Page(?![a-zA-Z])").Count.ShouldBe(1);
        text.ShouldContain("PdfTitleXyz");
        text.ShouldContain("PdfAuthorXyz");
        text.ShouldContain("PdfSubjectXyz");
    }

    [AvaloniaTest]
    public void WriteAsync_RoomsWithDescriptions_AddNoteAnnotationsInsideThePage()
    {
        var project = Project.Current;
        var first = new Room(project) { Name = "Hall", Position = new CoreVector(0, 0), Size = new CoreVector(64, 48) };
        first.AddDescription("A grand hall");
        var second = new Room(project) { Name = "Cellar", Position = new CoreVector(200, 120), Size = new CoreVector(64, 48) };
        second.AddDescription("A damp cellar");
        var third = new Room(project) { Name = "Attic", Position = new CoreVector(400, 0), Size = new CoreVector(64, 48) };
        project.Elements.Add(first);
        project.Elements.Add(second);
        project.Elements.Add(third);

        var path = Path.Combine(_folder, "notes.pdf");
        new SkiaMapPdfWriter().WriteAsync(project, path, PdfMetadata.FromProject(project)).GetAwaiter().GetResult();

        Directory.GetFiles(_folder).Length.ShouldBe(1);
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.ReadOnly);
        document.PageCount.ShouldBe(1);
        var page = document.Pages[0];
        var notes = new List<string>();
        for (var i = 0; i < page.Annotations.Count; i++)
        {
            var annotation = page.Annotations[i];
            notes.Add(annotation.Contents);
            var rectangle = annotation.Rectangle;
            rectangle.X1.ShouldBeGreaterThanOrEqualTo(0);
            rectangle.Y1.ShouldBeGreaterThanOrEqualTo(0);
            rectangle.X2.ShouldBeLessThanOrEqualTo(page.MediaBox.Width + 0.01);
            rectangle.Y2.ShouldBeLessThanOrEqualTo(page.MediaBox.Height + 0.01);
        }

        notes.Count.ShouldBe(2);
        notes.ShouldContain("A grand hall");
        notes.ShouldContain("A damp cellar");
    }

    [AvaloniaTest]
    public void WriteAsync_NoDescriptions_AddsNoAnnotations()
    {
        var project = Project.Current;
        project.Elements.Add(new Room(project) { Name = "Hall", Position = new CoreVector(0, 0), Size = new CoreVector(64, 48) });

        var path = Path.Combine(_folder, "plain.pdf");
        new SkiaMapPdfWriter().WriteAsync(project, path, PdfMetadata.FromProject(project)).GetAwaiter().GetResult();

        using var document = PdfReader.Open(path, PdfDocumentOpenMode.ReadOnly);
        document.Pages[0].Annotations.Count.ShouldBe(0);
    }

    [AvaloniaTest]
    public void WriteAsync_EmptyMap_Throws()
    {
        var path = Path.Combine(_folder, "empty.pdf");
        Should.Throw<InvalidOperationException>(
            () => new SkiaMapPdfWriter().WriteAsync(Project.Current, path, new PdfMetadata()));
        File.Exists(path).ShouldBeFalse();
    }

    /// <summary>Writes a sample map to PDF for visual checking; runs only when TRIZBORT_CAPTURE_OUTPUT is set.</summary>
    [AvaloniaTest]
    public async Task CaptureSamplePdf()
    {
        var root = Environment.GetEnvironmentVariable("TRIZBORT_CAPTURE_OUTPUT");
        if (string.IsNullOrWhiteSpace(root)) Assert.Ignore("Set TRIZBORT_CAPTURE_OUTPUT to write a sample PDF.");
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        string? sample = null;
        while (dir != null && sample == null)
        {
            var candidate = Path.Combine(dir.FullName, "samples", "Zork_770614.trizbort");
            if (File.Exists(candidate)) sample = candidate;
            dir = dir.Parent;
        }
        sample.ShouldNotBeNull();
        new MapLoader(Project.Current).LoadMap(sample!).ShouldBeTrue();
        Directory.CreateDirectory(root!);
        await new SkiaMapPdfWriter().WriteAsync(Project.Current, Path.Combine(root!, "sample-zork.pdf"), PdfMetadata.FromProject(Project.Current));

        // A map with room descriptions, so the PDF also carries the orange note annotations (ticket 124).
        var spiral = Path.Combine(Path.GetDirectoryName(sample!)!, "spiral.trizbort");
        var withDescriptions = new Project();
        var previous = Project.Current;
        Project.Current = withDescriptions;
        try
        {
            new MapLoader(withDescriptions).LoadMap(spiral).ShouldBeTrue();
            await new SkiaMapPdfWriter().WriteAsync(withDescriptions, Path.Combine(root!, "sample-spiral.pdf"), PdfMetadata.FromProject(withDescriptions));
        }
        finally
        {
            Project.Current = previous;
            withDescriptions.Dispose();
        }
    }
}
