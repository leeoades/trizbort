using Trizbort.Domain.Application;

namespace Trizbort.Export;

/// <summary>The Title, Author and Subject written into an exported PDF.</summary>
public sealed class PdfMetadata
{
  public const string DefaultTitle = "A Trizbort Map";
  public const string DefaultAuthor = "A Trizbort User";

  public string Title { get; set; } = DefaultTitle;
  public string Author { get; set; } = DefaultAuthor;

  /// <summary>The PDF Subject; empty when the project has no description.</summary>
  public string Subject { get; set; } = string.Empty;

  /// <summary>Takes the metadata from the project's title, author and description, with the legacy fallbacks.</summary>
  public static PdfMetadata FromProject(Project project)
  {
    var metadata = new PdfMetadata();
    if (project == null) return metadata;
    if (!string.IsNullOrWhiteSpace(project.Title)) metadata.Title = project.Title;
    if (!string.IsNullOrWhiteSpace(project.Author)) metadata.Author = project.Author;
    if (!string.IsNullOrWhiteSpace(project.Description)) metadata.Subject = project.Description;
    return metadata;
  }
}
