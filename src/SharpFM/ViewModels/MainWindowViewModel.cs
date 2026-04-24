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
using SharpFM.Model.Scripting;
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
    private OpenTabViewModel? _trackedActiveTab;

    private void OnActiveTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OpenTabViewModel.Clip))
            NotifyPropertyChanged(nameof(SelectedClip));
    }

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

        OpenTabs = new OpenTabsViewModel();
        OpenTabs.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(OpenTabsViewModel.ActiveTab)) return;

            // Re-subscribe to the new active tab so preview-slot clip swaps
            // also surface as SelectedClip changes (the tab instance stays
            // the same, but Clip changes underneath).
            if (_trackedActiveTab is not null)
                _trackedActiveTab.PropertyChanged -= OnActiveTabPropertyChanged;
            _trackedActiveTab = OpenTabs.ActiveTab;
            if (_trackedActiveTab is not null)
                _trackedActiveTab.PropertyChanged += OnActiveTabPropertyChanged;

            NotifyPropertyChanged(nameof(SelectedClip));
        };

        RootNodes = [];

        FileMakerClips = [];
        FileMakerClips.CollectionChanged += (sender, e) =>
        {
            // A flat-list change invalidates the tree; rebuilding respects the
            // current search filter.
            RebuildTree();

            // When a clip is removed from the catalog (e.g., DeleteSelectedClip),
            // close any tabs that still point at it and dispose the cached
            // editor view so resources (e.g. TextMate installation) don't leak.
            if (e.OldItems is not null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is not ClipViewModel vm) continue;
                    OpenTabs.CloseClip(vm);
                    vm.Dispose();
                }
            }
        };

        _repository = new ClipRepository(_currentPath);
        LoadClipsFromRepository();
    }

    public IClipRepository ActiveRepository => _repository;

    private void LoadClipsFromRepository()
    {
        var clips = _repository.LoadClipsAsync().GetAwaiter().GetResult();
        PopulateClips(clips);
    }

    private async Task LoadClipsFromRepositoryAsync()
    {
        var clips = await _repository.LoadClipsAsync();
        PopulateClips(clips);
    }

    private void PopulateClips(IReadOnlyList<ClipData> clips)
    {
        // Closing the tab strip first keeps the editor area from holding refs
        // to clips that are about to be replaced.
        OpenTabs.Tabs.Clear();
        OpenTabs.ActiveTab = null;

        // Clear fires CollectionChanged with Reset (no OldItems), so dispose
        // the outgoing clips explicitly.
        foreach (var c in FileMakerClips) c.Dispose();
        FileMakerClips.Clear();
        foreach (var clip in clips)
        {
            FileMakerClips.Add(new ClipViewModel(
                new FileMakerClip(clip.Name, clip.ClipType, clip.Xml))
            {
                FolderPath = clip.FolderPath,
            });
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
                .Select(c => new ClipData(c.Clip.Name, c.ClipType, c.Clip.XmlData)
                {
                    FolderPath = c.FolderPath,
                })
                .ToList();

            await _repository.SaveClipsAsync(clipData);

            // Clip is now known-persisted — rebase the dirty snapshot so the
            // tab dirty dots clear.
            foreach (var c in FileMakerClips) c.MarkSaved();

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

        var clip = SelectedClip;
        var name = clip.Clip.Name;
        OpenTabs.CloseClip(clip);
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
            ClipViewModel? lastAdded = null;

            foreach (var format in formats.Where(f => f.StartsWith("Mac-", StringComparison.CurrentCultureIgnoreCase)).Distinct())
            {
                if (string.IsNullOrEmpty(format)) continue;

                object? clipData = await _clipboard.GetDataAsync(format);

                if (clipData is not byte[] dataObj) continue;

                var clip = new FileMakerClip("new-clip", format, dataObj);

                // don't add duplicates
                if (FileMakerClips.Any(k => k.Clip.XmlData == clip.XmlData)) continue;

                lastAdded = new ClipViewModel(clip);
                FileMakerClips.Add(lastAdded);
                count++;
            }

            if (lastAdded is not null)
            {
                SelectedClip = lastAdded;
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

    /// <summary>
    /// Explicitly copy a script clip to the clipboard as Mac-XMSC
    /// ("Script"), wrapping the step list in a <c>&lt;Script&gt;</c>
    /// envelope so FM Pro can paste it as a whole script. Supplies
    /// default metadata (using the clip's name) if none was captured
    /// from the source XML.
    /// </summary>
    public async Task CopyAsScript()
    {
        if (SelectedClip is not ClipViewModel data)
        {
            ShowStatus("No clip selected");
            return;
        }

        if (!data.IsScriptClip)
        {
            ShowStatus("Selected clip is not a script", isError: true);
            return;
        }

        try
        {
            // Editor state → FmScript → force Metadata present → emit as XMSC
            data.Clip.XmlData = data.Editor.ToXml();
            var script = FmScript.FromXml(data.Clip.XmlData);
            script.Metadata ??= ScriptMetadata.Default(data.Clip.Name);

            var xmlWithWrapper = script.ToXml();
            var clip = new FileMakerClip(data.Clip.Name, "Mac-XMSC", xmlWithWrapper);
            await _clipboard.SetDataAsync("Mac-XMSC", clip.RawData);
            ShowStatus("Copied as Script to FileMaker clipboard");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying as Script.");
            ShowStatus("Error copying to clipboard", isError: true);
        }
    }

    /// <summary>
    /// Explicitly copy a script clip as Mac-XMSS ("ScriptSteps") — bare
    /// step list with no <c>&lt;Script&gt;</c> wrapper. Useful for
    /// pasting steps *inside* an existing FM Pro script editor pane.
    /// </summary>
    public async Task CopyAsScriptSteps()
    {
        if (SelectedClip is not ClipViewModel data)
        {
            ShowStatus("No clip selected");
            return;
        }

        if (!data.IsScriptClip)
        {
            ShowStatus("Selected clip is not a script", isError: true);
            return;
        }

        try
        {
            data.Clip.XmlData = data.Editor.ToXml();
            var script = FmScript.FromXml(data.Clip.XmlData);
            script.Metadata = null;

            var xmlNoWrapper = script.ToXml();
            var clip = new FileMakerClip(data.Clip.Name, "Mac-XMSS", xmlNoWrapper);
            await _clipboard.SetDataAsync("Mac-XMSS", clip.RawData);
            ShowStatus("Copied as Script Steps to FileMaker clipboard");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying as Script Steps.");
            ShowStatus("Error copying to clipboard", isError: true);
        }
    }

    /// <summary>
    /// Public entry point for code outside this view model (e.g. the script
    /// editor controller) to display a transient status-bar message.
    /// </summary>
    public void ShowStatusMessage(string message, bool isError = false) =>
        ShowStatus(message, isError);

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

    /// <summary>
    /// VS Code-style open tabs — the right-side editor area binds to this.
    /// </summary>
    public OpenTabsViewModel OpenTabs { get; }

    /// <summary>
    /// Tree nodes shown in the left-side clip browser. Rebuilt whenever
    /// <see cref="FileMakerClips"/> or <see cref="SearchText"/> changes.
    /// </summary>
    public ObservableCollection<ClipTreeNodeViewModel> RootNodes { get; set; }

    /// <summary>
    /// The clip backing the active tab, if any. Kept as a property so existing
    /// commands (copy/paste/delete) and tests that reasoned about a single
    /// "current" clip continue to work with the tabbed UI.
    /// </summary>
    public ClipViewModel? SelectedClip
    {
        get => OpenTabs.ActiveTab?.Clip;
        set
        {
            StatusMessage = "";
            if (value is null) { OpenTabs.ActiveTab = null; NotifyPropertyChanged(); return; }
            OpenTabs.OpenAsPermanent(value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Open a clip as a preview tab (single-click in the tree).
    /// </summary>
    public void OpenClipAsPreview(ClipViewModel clip) => OpenTabs.OpenAsPreview(clip);

    /// <summary>
    /// Open a clip as a permanent tab (double-click in the tree).
    /// </summary>
    public void OpenClipAsPermanent(ClipViewModel clip) => OpenTabs.OpenAsPermanent(clip);

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            RebuildTree();
            NotifyPropertyChanged();
        }
    }

    private void RebuildTree()
    {
        var nodes = ClipTreeNodeViewModel.Build(FileMakerClips, _searchText);
        RootNodes.Clear();
        foreach (var n in nodes) RootNodes.Add(n);
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

    private IReadOnlyList<IPlugin> _allPlugins = [];
    public IReadOnlyList<IPlugin> AllPlugins
    {
        get => _allPlugins;
        set
        {
            _allPlugins = value;
            NotifyPropertyChanged();
        }
    }

    private IReadOnlyList<IClipRepository> _availableRepositories = [];
    public IReadOnlyList<IClipRepository> AvailableRepositories
    {
        get => _availableRepositories;
        set { _availableRepositories = value; NotifyPropertyChanged(); }
    }

    // Panel management is delegated to PluginUIHost (bound via PluginUI property)
    private PluginUIHost? _pluginUI;
    public PluginUIHost? PluginUI
    {
        get => _pluginUI;
        set { _pluginUI = value; NotifyPropertyChanged(); }
    }
}
