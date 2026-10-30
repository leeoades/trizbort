using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia;

/// <summary>
///   Process-wide command-line options, set once from <c>Program.Main</c>.
/// </summary>
public static class AppOptions
{
    public static CommandLineOptions Current { get; private set; } = new();

    /// <summary>True when launched with <c>--debug</c> (consumed by later debug features).</summary>
    public static bool Debug => Current.Debug;

    public static void Initialize(CommandLineOptions options)
    {
        Current = options ?? new CommandLineOptions();
        if (!string.IsNullOrEmpty(Current.ConfigDir))
        {
            AppPaths.OverrideConfigFolder = Path.GetFullPath(Current.ConfigDir);
        }
    }
}
