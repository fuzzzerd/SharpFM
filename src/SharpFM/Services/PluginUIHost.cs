using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using SharpFM.Plugin;
using SharpFM.Plugin.UI;

namespace SharpFM.Services;

/// <summary>
/// Manages plugin panel state and implements <see cref="IPluginUIHost"/>.
/// Owns the active panel, its Control, and visibility — the MainWindowViewModel
/// exposes this via composition for XAML binding.
/// </summary>
public class PluginUIHost : IPluginUIHost, INotifyPropertyChanged
{
    private readonly IPluginHost _baseHost;
    private IPanelPlugin? _activePlugin;
    private Control? _panelControl;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PluginUIHost(IPluginHost baseHost)
    {
        _baseHost = baseHost;
    }

    public bool IsVisible => _activePlugin is not null;

    public Control? PanelControl
    {
        get => _panelControl;
        private set { _panelControl = value; OnPropertyChanged(); }
    }

    public string? ActivePluginId => _activePlugin?.Id;

    public void TogglePanel(IPlugin plugin)
    {
        if (plugin is not IPanelPlugin panelPlugin) return;

        if (_activePlugin?.Id == plugin.Id)
        {
            _activePlugin = null;
            PanelControl = null;
        }
        else
        {
            PanelControl = panelPlugin.CreatePanel();
            _activePlugin = panelPlugin;
        }

        OnPropertyChanged(nameof(IsVisible));
        OnPropertyChanged(nameof(ActivePluginId));
    }

    public bool HasPanel(IPlugin plugin) => plugin is IPanelPlugin;

    // --- IPluginUIHost ---

    public Task<bool> ShowContentDialogAsync(string title, Control content)
    {
        // TODO: implement with Avalonia Window
        return Task.FromResult(false);
    }

    // --- IPluginHost delegation ---
    // All base host methods delegate to the wrapped IPluginHost.

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => _baseHost.CreateLogger(categoryName);
    public Model.ClipData? SelectedClip => _baseHost.SelectedClip;
    public event System.EventHandler<Model.ClipData?>? SelectedClipChanged
    {
        add => _baseHost.SelectedClipChanged += value;
        remove => _baseHost.SelectedClipChanged -= value;
    }
    public void UpdateSelectedClipXml(string xml, string originPluginId) => _baseHost.UpdateSelectedClipXml(xml, originPluginId);
    public event System.EventHandler<Plugin.ClipContentChangedArgs>? ClipContentChanged
    {
        add => _baseHost.ClipContentChanged += value;
        remove => _baseHost.ClipContentChanged -= value;
    }
    public System.Collections.Generic.IReadOnlyList<Model.ClipData> AllClips => _baseHost.AllClips;
    public event System.EventHandler? ClipCollectionChanged
    {
        add => _baseHost.ClipCollectionChanged += value;
        remove => _baseHost.ClipCollectionChanged -= value;
    }
    public void ShowStatus(string message) => _baseHost.ShowStatus(message);
    public Model.ClipData? GetClip(string clipName) => _baseHost.GetClip(clipName);
    public void UpdateClipXml(string clipName, string xml, string originPluginId) => _baseHost.UpdateClipXml(clipName, xml, originPluginId);
    public void CreateClip(string name, string clipType, string? xml = null) => _baseHost.CreateClip(name, clipType, xml);
    public bool RemoveClip(string clipName) => _baseHost.RemoveClip(clipName);
    public void RegisterRepository(Model.IClipRepository repository) => _baseHost.RegisterRepository(repository);
    public void RegisterTransform(Plugin.IClipTransform transform) => _baseHost.RegisterTransform(transform);
    public Task<string?> ShowDialogAsync(string title, string message, string[] buttons) => _baseHost.ShowDialogAsync(title, message, buttons);
    public Task<string?> ShowInputDialogAsync(string title, string prompt, string? defaultValue = null) => _baseHost.ShowInputDialogAsync(title, prompt, defaultValue);

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
