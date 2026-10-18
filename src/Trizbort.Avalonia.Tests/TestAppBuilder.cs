using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;

[assembly: AvaloniaTestApplication(typeof(Trizbort.Avalonia.Tests.TestAppBuilder))]

namespace Trizbort.Avalonia.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
