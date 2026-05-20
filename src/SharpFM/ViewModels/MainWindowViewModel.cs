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
using SharpFM.Dialogs;
using SharpFM.Models;
using SharpFM.Model;
using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;
using SharpFM.Model.Scripting;
using SharpFM.Plugin;
using SharpFM.Services;

namespace SharpFM.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ILogger _logger;
    private readonly IClipboardService _clipboard;
    private readonly IFolderService _folderService;
    private readonly IInputPrompt _prompt;
    private readonly IClipCollisionPrompt _collisionPrompt;
    private readonly DispatcherTimer _statusTimer;
    private IClipRepository _repository;
    private OpenTabViewModel? _trackedActiveTab;

    private ClipViewModel? _trackedSelectedClip;

    private void OnActiveTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OpenTabViewModel.Clip))
        {
            NotifyPropertyChanged(nameof(SelectedClip));
            ResubscribeSelectedClipParseReport();
        }
    }

    private void ResubscribeSelectedClipParseReport()
    {
        if (_trackedSelectedClip is not null)
        {
            _trackedSelectedClip.PropertyChanged -= OnSelectedClipPropertyChanged;
        }

        _trackedSelectedClip = SelectedClip;
        if (_trackedSelectedClip is not null)
        {
            _trackedSelectedClip.PropertyChanged += OnSelectedClipPropertyChanged;
        }

        NotifyPropertyChanged(nameof(ParseFidelityVisible));
        NotifyPropertyChanged(nameof(ParseFidelityIsLossless));
        NotifyPropertyChanged(nameof(ParseFidelitySummary));
    }

    private void OnSelectedClipPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClipViewModel.ParseReport)
            || e.PropertyName == nameof(ClipViewModel.IsLossless)
            || e.PropertyName == nameof(ClipViewModel.Clip))
        {
            NotifyPropertyChanged(nameof(ParseFidelityIsLossless));
            NotifyPropertyChanged(nameof(ParseFidelitySummary));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindowViewModel(
        ILogger logger,
        IClipboardService clipboard,
        IFolderService folderService,
        IInputPrompt? prompt = null,
        IClipCollisionPrompt? collisionPrompt = null)
    {
        _logger = logger;
        _clipboard = clipboard;
        _folderService = folderService;
        _prompt = prompt ?? new NullInputPrompt();
        _collisionPrompt = collisionPrompt ?? new NullClipCollisionPrompt();

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
            ResubscribeSelectedClipParseReport();
        };

        RootNodes = [];

        Folders = [];
        Folders.CollectionChanged += (_, _) => RebuildTree();

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
        LoadFromRepository();
    }

    public IClipRepository ActiveRepository => _repository;

    private void LoadFromRepository()
    {
        var clips = _repository.LoadClipsAsync().GetAwaiter().GetResult();
        var folders = _repository.LoadFoldersAsync().GetAwaiter().GetResult();
        Populate(clips, folders);
    }

    private async Task LoadFromRepositoryAsync()
    {
        var clips = await _repository.LoadClipsAsync();
        var folders = await _repository.LoadFoldersAsync();
        Populate(clips, folders);
    }

    private void Populate(IReadOnlyList<ClipData> clips, IReadOnlyList<FolderData> folders)
    {
        // Closing the tab strip first keeps the editor area from holding refs
        // to clips that are about to be replaced.
        OpenTabs.Tabs.Clear();
        OpenTabs.ActiveTab = null;

        // Clear fires CollectionChanged with Reset (no OldItems), so dispose
        // the outgoing clips explicitly.
        foreach (var c in FileMakerClips) c.Dispose();
        FileMakerClips.Clear();
        Folders.Clear();

        foreach (var folder in folders) Folders.Add(folder);

        foreach (var clip in clips)
        {
            FileMakerClips.Add(new ClipViewModel(
                Clip.FromXml(clip.Name, clip.ClipType, clip.Xml))
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
            await LoadFromRepositoryAsync();
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
        await LoadFromRepositoryAsync();
        ShowStatus($"Switched to {repository.ProviderName}: {repository.CurrentLocation}");
    }

    public async Task SaveClipsStorageAsync()
    {
        try
        {
            foreach (var clip in FileMakerClips)
                clip.HandleEditorContentChanged();

            var clipData = FileMakerClips
                .Select(c => new ClipData(c.Clip.Name, c.ClipType, c.Clip.Xml)
                {
                    FolderPath = c.FolderPath,
                })
                .ToList();

            var sanitized = 0;
            void OnSanitized(object? _, FolderPathSanitizedEventArgs __) => sanitized++;
            var fileRepo = _repository as ClipRepository;
            if (fileRepo is not null) fileRepo.FolderPathSanitized += OnSanitized;

            try
            {
                // Folders persist first so the orphan sweep in SaveClipsAsync
                // sees up-to-date marker files when reclaiming empty directories.
                await _repository.SaveFoldersAsync(Folders.ToList());
                await _repository.SaveClipsAsync(clipData);
            }
            finally
            {
                if (fileRepo is not null) fileRepo.FolderPathSanitized -= OnSanitized;
            }

            foreach (var c in FileMakerClips) c.MarkSaved();

            var hadSanitization = sanitized > 0;
            var suffix = hadSanitization
                ? $"; sanitized {sanitized} folder path(s) — see log for dropped segments"
                : "";
            ShowStatus(
                $"Saved {clipData.Count} clip(s) to {_repository.CurrentLocation}{suffix}",
                isError: hadSanitization);
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


    public async Task RenameSelectedClip()
    {
        var clip = SelectedClip;
        if (clip is null)
        {
            ShowStatus("No clip selected");
            return;
        }

        var current = clip.Clip.Name;
        var entered = await _prompt.PromptAsync("Rename clip", "New name:", current);
        if (entered is null) return;

        var trimmed = entered.Trim();
        if (trimmed.Length == 0 || trimmed == current) return;

        clip.RenameTo(trimmed);
        // Tree label binds Clip.Clip.Name; the rebuild also re-sorts under the
        // new name so the node lands in the right folder slot.
        RebuildTree();
        ShowStatus($"Renamed to '{trimmed}'");
    }

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

    public void NewScriptCommand() => CreateNewClip("New Script", "Mac-XMSC", "script");

    public void NewScriptStepsCommand() => CreateNewClip("New Script Steps", "Mac-XMSS", "script steps");

    public void NewTableCommand() => CreateNewClip("New Table", "Mac-XMTB", "table");

    /// <summary>
    /// Create an empty folder under the current target folder. The user is
    /// prompted for the folder name; cancelling or supplying a blank/colliding
    /// name aborts.
    /// </summary>
    public async Task NewFolderCommand()
    {
        var parent = TargetFolderPath;

        var entered = await _prompt.PromptAsync("New folder", "Folder name:", "New Folder");
        if (entered is null) return;

        var trimmed = entered.Trim();
        if (trimmed.Length == 0)
        {
            ShowStatus("Folder name cannot be empty", isError: true);
            return;
        }

        var path = Combine(parent, new[] { trimmed });
        if (Folders.Any(f => FolderPathsEqual(f.Path, path)))
        {
            ShowStatus($"Folder '{trimmed}' already exists at this level", isError: true);
            return;
        }

        Folders.Add(new FolderData(path));
        ShowStatus($"Created folder '{trimmed}'");
    }

    private void CreateNewClip(string name, string format, string kind)
    {
        try
        {
            var folderPath = TargetFolderPath;
            var seed = ClipTypeRegistry.For(format).DefaultXml(name);
            var vm = new ClipViewModel(Clip.FromXml(name, format, seed))
            {
                FolderPath = folderPath,
            };
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
        catch (NotSupportedException e)
        {
            _logger.LogInformation(e, "Copy as class is only available for table clips.");
            ShowStatus(e.Message, isError: true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying as class.");
            ShowStatus("Error copying to clipboard", isError: true);
        }
    }

    // Pick a clip name that doesn't collide with anything already loaded in
    // the same folder. Folder paths are compared case-insensitively, matching
    // the tree's grouping rules.
    private string UniqueClipName(string desired, IReadOnlyList<string> folderPath)
    {
        bool Collides(string candidate) => FileMakerClips.Any(c =>
            c.Clip.Name == candidate && FolderPathsEqual(c.FolderPath, folderPath));

        if (!Collides(desired)) return desired;
        for (var n = 2; ; n++)
        {
            var candidate = $"{desired} ({n})";
            if (!Collides(candidate)) return candidate;
        }
    }

    private static bool FolderPathsEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    public async Task PasteFileMakerClipData()
    {
        try
        {
            var formats = await _clipboard.GetFormatsAsync();
            int count = 0;
            ClipViewModel? lastAdded = null;

            // Group pastes land relative to the current target folder so the
            // user can drop a folder into the spot they're looking at — whether
            // that's a selected clip's folder or an explicitly tapped empty one.
            var pasteRoot = TargetFolderPath;

            var recognized = new List<string>();
            var unrecognized = new List<string>();
            foreach (var format in formats.Distinct())
            {
                if (string.IsNullOrEmpty(format)) continue;
                if (!format.StartsWith("Mac-", StringComparison.CurrentCultureIgnoreCase)) continue;
                (ClipTypeRegistry.IsRegistered(format) ? recognized : unrecognized).Add(format);
            }

            var batch = new PasteBatchState();

            foreach (var format in recognized)
            {
                var (added, last) = await PasteOneFormat(format, pasteRoot, batch);
                lastAdded = last ?? lastAdded;
                count += added;
                if (batch.Cancelled) break;
            }

            // Fall back to opaque only when no recognized format produced a
            // paste — same payload often arrives in both a known and a
            // not-yet-registered encoding, and the known one already covered it.
            string? unknownFormatPasted = null;
            if (count == 0 && !batch.Cancelled)
            {
                foreach (var format in unrecognized)
                {
                    var (added, last) = await PasteOneFormat(format, pasteRoot, batch);
                    if (added == 0) continue;
                    lastAdded = last ?? lastAdded;
                    count += added;
                    unknownFormatPasted = format;
                    _logger.LogInformation(
                        "Pasted unknown format {Format}; will round-trip as raw XML.",
                        format);
                    break;
                }
            }

            if (lastAdded is not null)
            {
                SelectedClip = lastAdded;
            }

            if (unknownFormatPasted is not null)
            {
                ShowStatus($"Pasted unknown format {unknownFormatPasted}; will round-trip as raw XML.");
            }
            else if (batch.Cancelled)
            {
                ShowStatus($"Paste cancelled at name collision; kept {count} clip(s)", isError: true);
            }
            else
            {
                ShowStatus(count > 0 ? $"Pasted {count} clip(s) from FileMaker" : "No FileMaker clips found on clipboard");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error pasting from FileMaker clipboard.");
            ShowStatus("Error pasting from clipboard", isError: true);
        }
    }

    private async Task<(int added, ClipViewModel? last)> PasteOneFormat(
        string format,
        IReadOnlyList<string> pasteRoot,
        PasteBatchState batch)
    {
        object? clipData = await _clipboard.GetDataAsync(format);
        if (clipData is not byte[] dataObj) return (0, null);

        var rawClip = Clip.FromWireBytes("new-clip", format, dataObj);
        int added = 0;
        ClipViewModel? last = null;

        var decomposed = GroupPasteDecomposer.TryDecompose(rawClip.Xml);
        if (decomposed is { Entries.Count: > 0 })
        {
            foreach (var folder in decomposed.Folders)
            {
                var fullPath = Combine(pasteRoot, folder.Path);
                UpsertFolder(folder with { Path = fullPath });
            }

            foreach (var entry in decomposed.Entries)
            {
                if (batch.Cancelled) break;
                var folderPath = Combine(pasteRoot, entry.FolderPath);
                var entryClip = Clip.FromXml(entry.Name, format, entry.Xml);
                var result = await TryPasteEntry(entry.Name, folderPath, entryClip, batch);
                if (result is null) continue;
                last = result;
                added++;
            }
            return (added, last);
        }

        var sourceName = ClipTypeRegistry.For(format).TryGetSourceName(rawClip.Xml);
        var desired = string.IsNullOrWhiteSpace(sourceName) ? "new-clip" : sourceName;
        var single = await TryPasteEntry(desired, pasteRoot, rawClip, batch);
        return single is null ? (0, null) : (1, single);
    }

    private async Task<ClipViewModel?> TryPasteEntry(
        string name,
        IReadOnlyList<string> folderPath,
        Clip clip,
        PasteBatchState batch)
    {
        var existing = FileMakerClips.FirstOrDefault(c =>
            c.Clip.Name == name && FolderPathsEqual(c.FolderPath, folderPath));

        if (existing is not null)
        {
            if (existing.Clip.Xml == clip.Xml) return null;

            var decision = batch.StickyDecision
                ?? await _collisionPrompt.PromptAsync(name, folderPath);
            if (decision.ApplyToAll) batch.StickyDecision = decision;

            switch (decision.Choice)
            {
                case ClipCollisionChoice.Cancel:
                    batch.Cancelled = true;
                    return null;
                case ClipCollisionChoice.Replace:
                    existing.Replace(clip.Xml);
                    return existing;
                case ClipCollisionChoice.KeepBoth:
                    // fall through to the rename-and-add path below
                    break;
            }
        }

        var renamed = clip.Rename(UniqueClipName(name, folderPath));
        var added = new ClipViewModel(renamed) { FolderPath = folderPath };
        FileMakerClips.Add(added);
        return added;
    }

    private sealed class PasteBatchState
    {
        public ClipCollisionDecision? StickyDecision { get; set; }
        public bool Cancelled { get; set; }
    }

    private static IReadOnlyList<string> Combine(IReadOnlyList<string> root, IReadOnlyList<string> sub)
    {
        if (root.Count == 0) return sub;
        if (sub.Count == 0) return root;
        var combined = new string[root.Count + sub.Count];
        for (var i = 0; i < root.Count; i++) combined[i] = root[i];
        for (var i = 0; i < sub.Count; i++) combined[root.Count + i] = sub[i];
        return combined;
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
            data.HandleEditorContentChanged();
            await _clipboard.SetDataAsync(data.ClipType, data.Clip.WireBytes);
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
            data.HandleEditorContentChanged();
            var script = FmScript.FromXml(data.Clip.Xml);
            script.Metadata ??= ScriptMetadata.Default(data.Clip.Name);

            var xmlWithWrapper = script.ToXml();
            var clip = Clip.FromXml(data.Clip.Name, "Mac-XMSC", xmlWithWrapper);
            await _clipboard.SetDataAsync("Mac-XMSC", clip.WireBytes);
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
            data.HandleEditorContentChanged();
            var script = FmScript.FromXml(data.Clip.Xml);
            script.Metadata = null;

            var xmlNoWrapper = script.ToXml();
            var clip = Clip.FromXml(data.Clip.Name, "Mac-XMSS", xmlNoWrapper);
            await _clipboard.SetDataAsync("Mac-XMSS", clip.WireBytes);
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
    /// Folder metadata for every materialized folder — empty folders the user
    /// created locally and the FileMaker <c>&lt;Group&gt;</c> attributes captured
    /// when a Group paste landed. The tree builder consults this collection so
    /// empty folders still render.
    /// </summary>
    public ObservableCollection<FolderData> Folders { get; }

    private void UpsertFolder(FolderData folder)
    {
        for (var i = 0; i < Folders.Count; i++)
        {
            if (FolderPathsEqual(Folders[i].Path, folder.Path))
            {
                Folders[i] = folder;
                return;
            }
        }
        Folders.Add(folder);
    }

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
            SelectedFolderPath = null;
            OpenTabs.OpenAsPermanent(value);
            NotifyPropertyChanged();
        }
    }

    private IReadOnlyList<string>? _selectedFolderPath;

    /// <summary>
    /// Folder path tapped in the tree, if any. New clip/folder/paste operations
    /// land relative to this path (it takes priority over the selected clip's
    /// own folder). Cleared when a clip is opened.
    /// </summary>
    public IReadOnlyList<string>? SelectedFolderPath
    {
        get => _selectedFolderPath;
        set
        {
            _selectedFolderPath = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(TargetFolderPath));
        }
    }

    /// <summary>
    /// Where the next "new" or "paste" operation will land. Resolves to the
    /// explicitly tapped folder, else the selected clip's folder, else root.
    /// </summary>
    public IReadOnlyList<string> TargetFolderPath =>
        SelectedFolderPath ?? SelectedClip?.FolderPath ?? [];

    /// <summary>
    /// Select a folder path as the active target for new clip / paste /
    /// new folder operations. An empty path means "the repository root".
    /// The tab strip is intentionally left alone — navigating the tree should
    /// not close whatever the user is editing.
    /// </summary>
    public void OpenFolderAsSelection(IReadOnlyList<string> folderPath)
    {
        SelectedFolderPath = folderPath;
    }

    /// <summary>True when the status bar should display a parse-fidelity summary for the selected clip.</summary>
    public bool ParseFidelityVisible => SelectedClip is not null;

    /// <summary>True when the selected clip parsed losslessly. Drives the warning glyph in the status bar.</summary>
    public bool ParseFidelityIsLossless => SelectedClip?.IsLossless ?? true;

    /// <summary>
    /// Human-readable summary of the selected clip's parse report, e.g.
    /// <c>"Parsed losslessly"</c> or <c>"Parsed with 3 issues: 2 unknown step elements, 1 unknown attribute"</c>.
    /// </summary>
    public string ParseFidelitySummary
    {
        get
        {
            var report = SelectedClip?.ParseReport;
            if (report is null) return string.Empty;
            if (report.IsLossless) return "Parsed losslessly";

            var byKind = report.Diagnostics
                .GroupBy(d => d.Kind)
                .Select(g => $"{g.Count()} {g.Key.ToHumanLabel(g.Count())}");

            return $"Parsed with {report.Diagnostics.Count} issue(s): {string.Join(", ", byKind)}";
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
        var nodes = ClipTreeNodeViewModel.Build(FileMakerClips, _searchText, Folders);
        var root = ClipTreeNodeViewModel.Folder(RootNodeLabel, []);
        foreach (var n in nodes) root.Children.Add(n);
        RootNodes.Clear();
        RootNodes.Add(root);
    }

    /// <summary>
    /// Label for the synthetic root node shown at the top of the tree. Derived
    /// from the current repository location's leaf segment so it adapts when
    /// the user switches folders; falls back to a generic label for repos
    /// whose location string isn't a filesystem path.
    /// </summary>
    private string RootNodeLabel
    {
        get
        {
            if (string.IsNullOrEmpty(_currentPath)) return "All Clips";
            var trimmed = _currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var leaf = Path.GetFileName(trimmed);
            return string.IsNullOrEmpty(leaf) ? "All Clips" : leaf;
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
            RebuildTree();
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
