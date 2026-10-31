using Avalonia.Skia.Helpers;
using SkiaSharp;
using Trizbort.Avalonia.Controls;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Writes the map as a single-page vector PDF (ticket 123, engine option A). The page is the map bounds at 100%
///   (one pixel of the image export is one PDF point) and is drawn by the same <c>MapCanvasControl.RenderMap</c> that
///   draws the screen and the image export, through a Skia canvas that Avalonia wraps as a drawing context.
/// </summary>
public sealed class SkiaMapPdfWriter : IMapPdfWriter
{
    /// <summary>The largest page side the PDF format allows, in points.</summary>
    public const int MaxPageSize = 14400;

    public Task WriteAsync(Project project, string path, PdfMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var plan = ImageExportPlanner.Plan(
            project,
            null,
            null,
            new ImageExportOptions { Scale = 1.0 },
            ApplicationSettingsController.AppSettings);
        if (plan.IsEmpty) throw new InvalidOperationException("Cannot export an empty map.");

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        Write(project, plan, stream, metadata ?? PdfMetadata.FromProject(project));
        return Task.CompletedTask;
    }

    /// <summary>Writes the PDF for <paramref name="plan"/> to <paramref name="stream"/>.</summary>
    public static void Write(Project project, ImageExportPlan plan, Stream stream, PdfMetadata metadata)
    {
        var pdfMetadata = new SKDocumentPdfMetadata
        {
            Title = metadata.Title,
            Author = metadata.Author,
            Subject = metadata.Subject,
            Creator = "Trizbort",
            Producer = "Trizbort (Skia)"
        };

        // The plan is already clamped (8192 px), so the page limit is a safeguard rather than an expected case.
        var width = (float)Math.Min(plan.Width, MaxPageSize);
        var height = (float)Math.Min(plan.Height, MaxPageSize);

        using var document = SKDocument.CreatePdf(stream, pdfMetadata)
            ?? throw new InvalidOperationException("Skia could not create a PDF document.");
        var canvas = document.BeginPage(width, height);
        // Avalonia renders a visual straight onto the Skia (PDF) canvas, so the page keeps vectors and uses the
        // same RenderMap as the screen. RenderAsync completes synchronously (immediate renderer).
        var visual = new MapPrintVisual(project, plan) { Width = plan.Width, Height = plan.Height };
        var size = new global::Avalonia.Size(plan.Width, plan.Height);
        visual.Measure(size);
        visual.Arrange(new global::Avalonia.Rect(size));
        DrawingContextHelper.RenderAsync(canvas, visual).GetAwaiter().GetResult();

        document.EndPage();
        document.Close();
    }

    /// <summary>A throw-away visual whose only job is to draw the map for the PDF page.</summary>
    private sealed class MapPrintVisual : global::Avalonia.Controls.Control
    {
        private readonly Project _project;
        private readonly ImageExportPlan _plan;

        public MapPrintVisual(Project project, ImageExportPlan plan)
        {
            _project = project;
            _plan = plan;
        }

        public override void Render(global::Avalonia.Media.DrawingContext context)
        {
            MapCanvasControl.RenderMap(
                context, _project, _plan.Bounds.Center, _plan.Scale, _plan.Width, _plan.Height, _plan.Elements);
        }
    }
}
