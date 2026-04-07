using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SharpFM.Models;
using SharpFM.Model;
using SharpFM.Plugin;
using SharpFM.Services;

namespace SharpFM.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ILogger _logger;
    private readonly IClipboardService _clipboard;
    private readonly IFolderService _folderService;
    private readonly DispatcherTimer _statusTimer;
    private IClipRepository _repository;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindowViewModel(
        ILogger logger,
        IClipboardService clipboard,
        IFolderService folderService)
    {
        _logger = logger;
        _clipboard = clipboard;
        _folderService = folderService;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            StatusMessage = "";
        };

        // default to the local app data folder + \SharpFM, otherwise use provided path
        _currentPath ??= Path.Join(
            path1: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            path2: Path.Join("SharpFM", "Clips")
        );

        FileMakerClips = [];
        FileMakerClips.CollectionChanged += (sender, e) =>
        {
            // reset search text, which will trigger a property notify changed
            // that will re-run the search with empty value (which shows all)
            SearchText = string.Empty;
        };

        FilteredClips = [];

        _repository = new ClipRepository(_currentPath);
        LoadClipsFromRepository();
    }

    public IClipRepository ActiveRepository => _repository;

    private void LoadClipsFromRepository()
    {
        var clips = _repository.LoadClipsAsync().GetAwaiter().GetResult();

        FileMakerClips.Clear();

        foreach (var clip in clips)
        {
            FileMakerClips.Add(new ClipViewModel(
                new FileMakerClip(clip.Name, clip.ClipType, clip.Xml)));
        }
    }

    private async Task LoadClipsFromRepositoryAsync()
    {
        var clips = await _repository.LoadClipsAsync();

        FileMakerClips.Clear();

        foreach (var clip in clips)
        {
            FileMakerClips.Add(new ClipViewModel(
                new FileMakerClip(clip.Name, clip.ClipType, clip.Xml)));
        }
    }

    public async Task OpenFolderPicker()
    {
        try
        {
            CurrentPath = await _folderService.GetFolderAsync();
            _repository = new ClipRepository(CurrentPath);
            await LoadClipsFromRepositoryAsync();
            ShowStatus($"Opened {CurrentPath}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error opening folder.");
            ShowStatus("Error opening folder", isError: true);
        }
    }

    public async Task SwitchRepository(IClipRepository repository)
    {
        _repository = repository;
        CurrentPath = repository.CurrentLocation;
        await LoadClipsFromRepositoryAsync();
        ShowStatus($"Switched to {repository.ProviderName}: {repository.CurrentLocation}");
    }

    public async Task SaveClipsStorageAsync()
    {
        try
        {
            // Ensure XML is up-to-date from editor state before saving
            foreach (var clip in FileMakerClips)
                clip.Clip.XmlData = clip.Editor.ToXml();

            var clipData = FileMakerClips
                .Select(c => new ClipData(c.Name, c.ClipType, c.ClipXml))
                .ToList();

            await _repository.SaveClipsAsync(clipData);

            ShowStatus($"Saved {clipData.Count} clip(s) to {_repository.CurrentLocation}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving clips.");
            ShowStatus("Error saving clips", isError: true);
        }
    }

    public void SaveClipsStorage()
    {
        SaveClipsStorageAsync().GetAwaiter().GetResult();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound to Xaml Button, throws when static.")]
    public void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    private static readonly string EmptyScriptXml =
        "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>";

    private static readonly string EmptyTableXml =
        "<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"NewTable\"></BaseTable></fmxmlsnippet>";

    public void DeleteSelectedClip()
    {
        if (SelectedClip is null)
        {
            ShowStatus("No clip selected");
            return;
        }

        var name = SelectedClip.Name;
        var clip = SelectedClip;
        SelectedClip = null;
        FileMakerClips.Remove(clip);
        ShowStatus($"Deleted clip '{name}'");
    }

    public void NewScriptCommand() =>
        CreateNewClip("New Script", "Mac-XMSS", EmptyScriptXml, "script");

    public void NewTableCommand() =>
        CreateNewClip("New Table", "Mac-XMTB", EmptyTableXml, "table");

    private void CreateNewClip(string name, string format, string xml, string kind)
    {
        try
        {
            var clip = new FileMakerClip(name, format, xml);
            var vm = new ClipViewModel(clip);
            FileMakerClips.Add(vm);
            SelectedClip = vm;
            ShowStatus($"Created new {kind}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating {Kind}.", kind);
            ShowStatus($"Error creating {kind}", isError: true);
        }
    }

    public async Task CopyAsClass()
    {
        if (SelectedClip == null)
        {
            ShowStatus("No clip selected");
            return;
        }

        try
        {
            var classString = SelectedClip.Clip.CreateClass();
            await _clipboard.SetTextAsync(classString);
            ShowStatus("Copied C# class to clipboard");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying as class.");
            ShowStatus("Error copying to clipboard", isError: true);
        }
    }

    public async Task PasteFileMakerClipData()
    {
        try
        {
            var formats = await _clipboard.GetFormatsAsync();
            int count = 0;

            foreach (var format in formats.Where(f => f.StartsWith("Mac-", StringComparison.CurrentCultureIgnoreCase)).Distinct())
            {
                if (string.IsNullOrEmpty(format)) continue;

                object? clipData = await _clipboard.GetDataAsync(format);

                if (clipData is not byte[] dataObj) continue;

                var clip = new FileMakerClip("new-clip", format, dataObj);

                // don't add duplicates
                if (FileMakerClips.Any(k => k.ClipXml == clip.XmlData)) continue;

                FileMakerClips.Add(new ClipViewModel(clip));
                count++;
            }

            ShowStatus(count > 0 ? $"Pasted {count} clip(s) from FileMaker" : "No FileMaker clips found on clipboard");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error pasting from FileMaker clipboard.");
            ShowStatus("Error pasting from clipboard", isError: true);
        }
    }

    public async Task CopySelectedToClip()
    {
        if (SelectedClip is not ClipViewModel data)
        {
            ShowStatus("No clip selected");
            return;
        }

        try
        {
            // Ensure XML is up-to-date from editor state before copying
            data.Clip.XmlData = data.Editor.ToXml();
            await _clipboard.SetDataAsync(data.ClipType, data.Clip.RawData);
            ShowStatus("Copied to FileMaker clipboard");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying to FileMaker clipboard.");
            ShowStatus("Error copying to clipboard", isError: true);
        }
    }

    private void ShowStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        _statusTimer.Interval = isError ? TimeSpan.FromSeconds(8) : TimeSpan.FromSeconds(3);
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    /// <summary>
    /// SharpFM Version.
    /// </summary>
    public static string Version
    {
        get
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            return version ?? "v?.?.?";
        }
    }

    public ObservableCollection<ClipViewModel> FileMakerClips { get; set; }

    public ObservableCollection<ClipViewModel> FilteredClips { get; set; }

    private ClipViewModel? _selectedClip;
    public ClipViewModel? SelectedClip
    {
        get => _selectedClip;
        set
        {
            _selectedClip = value;
            StatusMessage = "";
            NotifyPropertyChanged();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            var previousSelection = _selectedClip;
            FilteredClips.Clear();
            foreach (var c in FileMakerClips.Where(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
            {
                FilteredClips.Add(c);
            }
            // Preserve selection if still visible in filtered results
            if (previousSelection != null && FilteredClips.Contains(previousSelection))
                SelectedClip = previousSelection;
            NotifyPropertyChanged();
        }
    }

    private string _currentPath;
    public string CurrentPath
    {
        get => _currentPath;
        set
        {
            _currentPath = value;
            NotifyPropertyChanged();
        }
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            NotifyPropertyChanged();
        }
    }

    // --- Plugin support ---

    private IReadOnlyList<IPanelPlugin> _panelPlugins = [];
    public IReadOnlyList<IPanelPlugin> PanelPlugins
    {
        get => _panelPlugins;
        set
        {
            _panelPlugins = value;
            NotifyPropertyChanged();
        }
    }

    private IPanelPlugin? _activePlugin;
    public IPanelPlugin? ActivePlugin
    {
        get => _activePlugin;
        private set
        {
            _activePlugin = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(IsPluginPanelVisible));
            NotifyPropertyChanged(nameof(PluginPanelControl));
        }
    }

    public bool IsPluginPanelVisible => _activePlugin is not null;

    private Control? _pluginPanelControl;
    public Control? PluginPanelControl
    {
        get => _pluginPanelControl;
        private set
        {
            _pluginPanelControl = value;
            NotifyPropertyChanged();
        }
    }

    private IReadOnlyList<IClipTransformPlugin> _transformPlugins = [];
    public IReadOnlyList<IClipTransformPlugin> TransformPlugins
    {
        get => _transformPlugins;
        set { _transformPlugins = value; NotifyPropertyChanged(); }
    }

    private IReadOnlyList<IClipRepository> _availableRepositories = [];
    public IReadOnlyList<IClipRepository> AvailableRepositories
    {
        get => _availableRepositories;
        set { _availableRepositories = value; NotifyPropertyChanged(); }
    }

    public void TogglePluginPanel(IPanelPlugin plugin)
    {
        if (_activePlugin?.Id == plugin.Id)
        {
            ActivePlugin = null;
            PluginPanelControl = null;
        }
        else
        {
            PluginPanelControl = plugin.CreatePanel();
            ActivePlugin = plugin;
        }
    }
}
