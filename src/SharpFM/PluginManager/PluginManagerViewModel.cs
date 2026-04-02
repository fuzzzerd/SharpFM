using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SharpFM.Plugin;

namespace SharpFM.PluginManager;

public class PluginEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public IPanelPlugin Plugin { get; }
    public string Id => Plugin.Id;
    public string DisplayName => Plugin.DisplayName;
    public string AssemblyName => Plugin.GetType().Assembly.GetName().Name ?? "(unknown)";

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Notify(); }
    }

    public PluginEntry(IPanelPlugin plugin, bool isActive)
    {
        Plugin = plugin;
        _isActive = isActive;
    }
}

public class PluginManagerViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ObservableCollection<PluginEntry> Plugins { get; } = [];

    private PluginEntry? _selectedPlugin;
    public PluginEntry? SelectedPlugin
    {
        get => _selectedPlugin;
        set { _selectedPlugin = value; Notify(); Notify(nameof(HasSelection)); }
    }

    public bool HasSelection => _selectedPlugin is not null;

    public void Refresh(IReadOnlyList<IPanelPlugin> loadedPlugins, IPanelPlugin? activePlugin)
    {
        Plugins.Clear();
        foreach (var plugin in loadedPlugins)
        {
            Plugins.Add(new PluginEntry(plugin, plugin.Id == activePlugin?.Id));
        }
    }
}
