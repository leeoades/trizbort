using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Editor;

namespace Trizbort.Export;

/// <summary>What Smart Save would do for a project: the files it would write, or why it cannot run.</summary>
public sealed class SmartSavePlan
{
  public bool SavePdf { get; set; }
  public bool SaveImage { get; set; }

  /// <summary>The PDF to write, or null when PDF output is off or the map has no local file yet.</summary>
  public string PdfPath { get; set; }

  /// <summary>The image to write, or null when image output is off or the map has no local file yet.</summary>
  public string ImagePath { get; set; }

  public ImageFormat ImageFormat { get; set; } = ImageFormat.Png;

  /// <summary>True when the map must be saved (it is new, dirty or opened from a URL) before Smart Save can run.</summary>
  public bool NeedsSave { get; set; }

  /// <summary>Why Smart Save cannot run now; null when it can.</summary>
  public string Reason { get; set; }

  public bool CanRun => Reason == null;
}

/// <summary>The outcome of one Smart Save file. <see cref="Error" /> is null on success.</summary>
public sealed record SmartSaveFileResult(SmartSaveKind Kind, string Path, string Error)
{
  public bool Succeeded => Error == null;
}

/// <summary>The outcome of a Smart Save run.</summary>
public sealed class SmartSaveResult
{
  public SmartSaveResult(IReadOnlyList<SmartSaveFileResult> files, string summary)
  {
    Files = files;
    Summary = summary;
  }

  public IReadOnlyList<SmartSaveFileResult> Files { get; }

  /// <summary>The message to show the user (or print): files written, per-file errors, or the "no files" text.</summary>
  public string Summary { get; }

  /// <summary>True when at least one file was written and none failed.</summary>
  public bool Succeeded => Files.Count > 0 && Files.All(f => f.Succeeded);
}

/// <summary>
///   Smart Save without any UI (ticket 110): works out the PDF and image paths next to the map file and writes them
///   through the <see cref="IMapPdfWriter" /> and <see cref="IMapImageWriter" /> the UI project provides.
/// </summary>
public static class SmartSaveService
{
  public const string NothingEnabledMessage =
    "Your settings are set to not save anything. Please check your App Settings if this is not what you want.";

  public const string NeedsSaveMessage =
    "Your project needs to be saved before we can do a SmartSave.  Would you like to save the project now?";

  public const string NoFilesMessage = "No files have been saved during the SmartSave.";

  public static string PdfPath(string mapFileName)
  {
    return Path.ChangeExtension(mapFileName, ".pdf");
  }

  public static string ImagePath(string mapFileName, ImageFormat format)
  {
    return Path.ChangeExtension(mapFileName, ImageFormats.Extension(format));
  }

  /// <summary>Decides what Smart Save would write for <paramref name="project" /> under <paramref name="settings" />.</summary>
  public static SmartSavePlan Plan(Project project, ApplicationSettings settings)
  {
    if (project == null) throw new ArgumentNullException(nameof(project));
    if (settings == null) throw new ArgumentNullException(nameof(settings));

    var plan = new SmartSavePlan
    {
      SavePdf = settings.SaveToPDF,
      SaveImage = settings.SaveToImage,
      ImageFormat = ImageFormats.FromSetting(settings.DefaultImageType)
    };

    if (!plan.SavePdf && !plan.SaveImage)
    {
      plan.Reason = NothingEnabledMessage;
      return plan;
    }

    var fileName = project.FileName ?? string.Empty;
    if (RecentFiles.IsUrl(fileName) || !project.HasFileName || project.IsDirty)
    {
      plan.NeedsSave = true;
      plan.Reason = NeedsSaveMessage;
      return plan;
    }

    if (plan.SavePdf) plan.PdfPath = PdfPath(fileName);
    if (plan.SaveImage) plan.ImagePath = ImagePath(fileName, plan.ImageFormat);
    return plan;
  }

  /// <summary>
  ///   Writes one output, throwing when there is no writer or the writer fails. Existing files are overwritten silently.
  /// </summary>
  public static async Task WriteFileAsync(
    SmartSaveKind kind, Project project, string path, ImageFormat format,
    IMapImageWriter image, IMapPdfWriter pdf)
  {
    if (kind == SmartSaveKind.Pdf)
    {
      if (pdf == null) throw new NotSupportedException("PDF export is not supported.");
      await pdf.WriteAsync(project, path, PdfMetadata.FromProject(project));
    }
    else
    {
      if (image == null) throw new NotSupportedException("Image export is not supported.");
      await image.WriteAsync(project, path, format);
    }
  }

  /// <summary>
  ///   Writes the files of <paramref name="plan" />. A failure of one file (or a missing writer) is recorded and does
  ///   not stop the other. Never throws for export failures.
  /// </summary>
  public static async Task<SmartSaveResult> RunAsync(
    Project project, SmartSavePlan plan, IMapImageWriter image, IMapPdfWriter pdf)
  {
    if (plan == null) throw new ArgumentNullException(nameof(plan));
    var files = new List<SmartSaveFileResult>();
    if (!plan.CanRun) return new SmartSaveResult(files, plan.NeedsSave ? NoFilesMessage : plan.Reason);

    if (plan.SavePdf && plan.PdfPath != null) files.Add(await TryWriteAsync(SmartSaveKind.Pdf, project, plan, image, pdf));
    if (plan.SaveImage && plan.ImagePath != null) files.Add(await TryWriteAsync(SmartSaveKind.Image, project, plan, image, pdf));
    return new SmartSaveResult(files, Summarise(files));
  }

  /// <summary>Plans and runs Smart Save with no prompts: for the command line, where the map is already saved.</summary>
  public static Task<SmartSaveResult> RunSilentAsync(
    Project project, ApplicationSettings settings, IMapImageWriter image, IMapPdfWriter pdf)
  {
    return RunAsync(project, Plan(project, settings), image, pdf);
  }

  /// <summary>The summary text for a set of file results.</summary>
  public static string Summarise(IReadOnlyList<SmartSaveFileResult> files)
  {
    var lines = new List<string>();
    foreach (var file in files)
    {
      var name = file.Kind == SmartSaveKind.Pdf ? "PDF" : "Image";
      lines.Add(file.Succeeded
        ? $"{name} file has been saved to {file.Path}"
        : $"There was an error saving the {name} file during the SmartSave.  Please make sure the {name} is not already opened.\n{file.Error}");
    }

    return lines.Count == 0 ? NoFilesMessage : string.Join(Environment.NewLine, lines);
  }

  private static async Task<SmartSaveFileResult> TryWriteAsync(
    SmartSaveKind kind, Project project, SmartSavePlan plan, IMapImageWriter image, IMapPdfWriter pdf)
  {
    var path = kind == SmartSaveKind.Pdf ? plan.PdfPath : plan.ImagePath;
    try
    {
      await WriteFileAsync(kind, project, path, plan.ImageFormat, image, pdf);
      return new SmartSaveFileResult(kind, path, null);
    }
    catch (Exception ex)
    {
      return new SmartSaveFileResult(kind, path, ex.Message);
    }
  }
}

/// <summary>
///   The <see cref="ISmartSaveExporter" /> that <see cref="SmartSave" /> (the File &gt; Smart Save command) uses for one
///   output kind, backed by <see cref="SmartSaveService" />.
/// </summary>
public sealed class SmartSaveWriterExporter : ISmartSaveExporter
{
  private readonly IMapImageWriter _image;
  private readonly IMapPdfWriter _pdf;
  private readonly Func<ApplicationSettings> _settings;

  public SmartSaveWriterExporter(
    SmartSaveKind kind, IMapImageWriter image, IMapPdfWriter pdf, Func<ApplicationSettings> settings)
  {
    Kind = kind;
    _image = image;
    _pdf = pdf;
    _settings = settings;
  }

  public SmartSaveKind Kind { get; }

  public async Task<string> ExportAsync(
    EditorSession session, string mapFileName, System.Threading.CancellationToken token = default)
  {
    var format = ImageFormats.FromSetting(_settings().DefaultImageType);
    var path = Kind == SmartSaveKind.Pdf
      ? SmartSaveService.PdfPath(mapFileName)
      : SmartSaveService.ImagePath(mapFileName, format);
    await SmartSaveService.WriteFileAsync(Kind, session.Project, path, format, _image, _pdf);
    return path;
  }
}
