using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia;

/// <summary>
///   Process-wide command-line options, set once from <c>Program.Main</c>.
/// </summary>
public static class AppOptions
{
    public static CommandLineOptions Current { get; private set; } = new();

    /// <summary>True when launched with <c>--debug</c> (enables the debug commands and rendering toggles).</summary>
    public static bool Debug => Current.Debug;

    public static void Initialize(CommandLineOptions options)
    {
        Current = options ?? new CommandLineOptions();
        // read when the command catalogue is built, so debug commands stay out of the menus without --debug
        Trizbort.Editor.DebugOptions.Enabled = Current.Debug;
        if (!string.IsNullOrEmpty(Current.ConfigDir))
        {
            AppPaths.OverrideConfigFolder = Path.GetFullPath(Current.ConfigDir);
        }
    }
}
