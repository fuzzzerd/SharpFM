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

    public IPlugin Plugin { get; }
    public string Id => Plugin.Id;
    public string DisplayName => Plugin.DisplayName;
    public string Description => Plugin.Description;
    public string PluginVersion => Plugin.Version;
    public string AssemblyName => Plugin.GetType().Assembly.GetName().Name ?? "(unknown)";

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Notify(); }
    }

    public PluginEntry(IPlugin plugin, bool isActive)
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

    public void Refresh(IReadOnlyList<IPlugin> allPlugins, string? activePluginId)
    {
        Plugins.Clear();
        foreach (var plugin in allPlugins)
        {
            Plugins.Add(new PluginEntry(plugin, plugin.Id == activePluginId));
        }
    }
}
