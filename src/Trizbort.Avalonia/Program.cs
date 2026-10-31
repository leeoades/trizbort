using Avalonia;
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
