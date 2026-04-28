using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SharpFM.Model.ClipTypes;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Models;
using SharpFM.Plugin;
using SharpFM.ViewModels;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SharpFM.Services;

namespace SharpFM;

[ExcludeFromCodeCoverage]
public partial class App : Application
{
    ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddNLog());
    ILogger logger => loggerFactory.CreateLogger<MainWindow>();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Reflection scan lands at a predictable moment; otherwise the
        // same initialization runs lazily on first registry access.
        StepRegistry.Initialize();

        // Clip-type strategies are explicitly registered (no reflection); do
        // it once at startup so paste / file load / plugin push all see the
        // built-in formats from the first request onward.
        ClipTypeRegistry.RegisterBuiltIns();

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
            var pluginHost = new PluginHost(viewModel, loggerFactory);
            var pluginUIHost = new PluginUIHost(pluginHost);
            var pluginConfigService = new PluginConfigService(logger);
            var pluginService = new PluginService(logger, pluginConfigService);
            pluginService.LoadPlugins(pluginUIHost);

            viewModel.AllPlugins = pluginService.AllPlugins;
            viewModel.PluginUI = pluginUIHost;

            // Build repository list: built-in + plugin-registered
            var repos = new List<IClipRepository> { viewModel.ActiveRepository };
            repos.AddRange(pluginHost.Repositories);
            viewModel.AvailableRepositories = repos;

            // Give the window access to plugin services for the manager dialog
            if (desktop.MainWindow is MainWindow mainWindow)
                mainWindow.SetPluginServices(pluginService, pluginUIHost, pluginConfigService);

            desktop.MainWindow.DataContext = viewModel;

            // Dispose all plugins on app exit so background servers (MCP, etc.) stop cleanly
            desktop.Exit += (_, _) =>
            {
                foreach (var plugin in pluginService.AllPlugins)
                {
                    try { plugin.Dispose(); }
                    catch (Exception ex) { logger.LogWarning(ex, "Error disposing plugin {Id}", plugin.Id); }
                }
            };
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