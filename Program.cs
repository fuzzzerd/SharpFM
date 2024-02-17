using Avalonia;
using NLog;
using System;

namespace SharpFM;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var logger = LogManager
            .Setup()
            .LoadConfigurationFromFile("nlog.config")
            .GetCurrentClassLogger();

        logger.Info("SharpFM has started up.");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        logger.Info("SharpFM has shut down.");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToNLog();
}
