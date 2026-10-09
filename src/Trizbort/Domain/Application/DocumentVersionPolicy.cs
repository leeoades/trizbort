using System;

namespace Trizbort.Domain.Application;

internal enum DocumentVersionWarning {
  None,
  Major,
  Minor,
  Build,
  Revision
}

internal static class DocumentVersionPolicy {
  public static DocumentVersionWarning Compare(Version document, Version application)
  {
    if (document.Major != application.Major)
      return document.Major > application.Major ? DocumentVersionWarning.Major : DocumentVersionWarning.None;
    if (document.Minor != application.Minor)
      return document.Minor > application.Minor ? DocumentVersionWarning.Minor : DocumentVersionWarning.None;
    if (document.Build != application.Build)
      return document.Build > application.Build ? DocumentVersionWarning.Build : DocumentVersionWarning.None;
    return document.MinorRevision > application.MinorRevision
      ? DocumentVersionWarning.Revision
      : DocumentVersionWarning.None;
  }
}