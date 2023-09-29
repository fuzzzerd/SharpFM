using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SharpFM.App.ViewModels;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace SharpFM.App;

public partial class App : Application
{
    ILogger logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<MainWindow>();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(logger)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}