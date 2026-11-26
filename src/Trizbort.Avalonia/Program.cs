using Avalonia;
using Velopack;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // Velopack first: when it runs Recast to install or update it, it does its work and exits here. Updates are
        // installed when Recast decides (the user's choice in App Settings), not at start-up.
        try
        {
            VelopackApp.Build()
                .SetAutoApplyOnStartup(false)
                // double-clicking a map opens Recast from the start; App Settings can turn it off (Lee, 26 Nov 2026)
                .OnAfterInstallFastCallback(_ => Trizbort.Avalonia.Services.FileAssociation.Apply(true))
                .OnBeforeUninstallFastCallback(_ => Trizbort.Avalonia.Services.FileAssociation.Apply(false))
                .OnRestarted(version => Trizbort.Updates.UpdateCoordinator.JustUpdatedTo = version.ToString())
                .Run();
        }
        catch (Exception)
        {
            // updating must never stop Recast starting
        }

        AppOptions.Initialize(CommandLineParsing.Parse(args));
        var code = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return code != 0 ? code : App.ExitCode;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
