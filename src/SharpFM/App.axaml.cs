using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SharpFM.ViewModels;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using SharpFM.Services;

namespace SharpFM;

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
            desktop.MainWindow = new MainWindow();

            var services = new ServiceCollection();
            services.AddSingleton<IFolderService>(x => new FolderService(desktop.MainWindow));
            services.AddSingleton<IClipboardService>(x => new ClipboardService(desktop.MainWindow));
            Services = services.BuildServiceProvider();

            var viewModel = new MainWindowViewModel(
                logger,
                Services.GetRequiredService<IClipboardService>(),
                Services.GetRequiredService<IFolderService>());

            // Load plugins
            var pluginHost = new PluginHost(viewModel);
            var pluginService = new PluginService(logger);
            pluginService.LoadPlugins(pluginHost);
            viewModel.PanelPlugins = pluginService.LoadedPlugins;

            // Give the window access to plugin services for the manager dialog
            if (desktop.MainWindow is MainWindow mainWindow)
                mainWindow.SetPluginServices(pluginService, pluginHost);

            desktop.MainWindow.DataContext = viewModel;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Get a reference to the current app.
    /// </summary>
    public new static App? Current => Application.Current as App;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider? Services { get; private set; }
}