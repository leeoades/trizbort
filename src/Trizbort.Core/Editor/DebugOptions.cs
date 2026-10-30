namespace Trizbort.Editor;

/// <summary>
///   Process-wide switch for the developer features (debug toggles, automap single-step). Set once at start-up from
///   <c>--debug</c>; the command catalogue reads it when it is created so debug commands stay out of the menus
///   otherwise. Each <see cref="EditorSession" /> also carries its own <see cref="EditorSession.DebugEnabled" />,
///   which is what actually gates the commands.
/// </summary>
public static class DebugOptions
{
  public static bool Enabled { get; set; }
}
