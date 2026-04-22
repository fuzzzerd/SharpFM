using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SharpFM.Plugin;
using SharpFM.Services;
using SharpFM.ViewModels;

namespace SharpFM.PluginManager;

[ExcludeFromCodeCoverage]
public partial class PluginManagerWindow : Window
{
    private readonly PluginManagerViewModel _viewModel = new();
    private PluginService? _pluginService;
    private PluginUIHost? _host;
    private MainWindowViewModel? _mainVm;

    public PluginManagerWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        var install = this.FindControl<Button>("installButton");
        var remove = this.FindControl<Button>("removeButton");
        var close = this.FindControl<Button>("closeButton");

        if (install is not null) install.Click += OnInstall;
        if (remove is not null) remove.Click += OnRemove;
        if (close is not null) close.Click += (_, _) => Close();
    }

    public void Configure(PluginService pluginService, PluginUIHost host, MainWindowViewModel mainVm)
    {
        _pluginService = pluginService;
        _host = host;
        _mainVm = mainVm;
        _viewModel.Refresh(pluginService.AllPlugins, host.ActivePluginId);
    }

    private async void OnInstall(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_pluginService is null || _host is null || _mainVm is null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Plugin DLL",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Plugin DLL") { Patterns = ["*.dll"] }]
        });

        if (files.Count == 0) return;

        var path = files[0].TryGetLocalPath();
        if (path is null) return;

        var newPlugins = _pluginService.InstallPlugin(path, _host);
        if (newPlugins.Count > 0)
        {
            _mainVm.AllPlugins = _pluginService.AllPlugins;
            _viewModel.Refresh(_pluginService.AllPlugins, _host.ActivePluginId);
        }
    }

    private void OnRemove(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_pluginService is null || _host is null || _mainVm is null) return;

        var entry = _viewModel.SelectedPlugin;
        if (entry is null) return;

        // Deactivate if this is the active panel plugin
        if (_host.ActivePluginId == entry.Id)
            _host.TogglePanel(entry.Plugin);

        _pluginService.UninstallPlugin(entry.Plugin);
        _mainVm.AllPlugins = _pluginService.AllPlugins;
        _viewModel.Refresh(_pluginService.AllPlugins, _host.ActivePluginId);
    }
}
