using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpFM.Models;
using SharpFM.Services;

namespace SharpFM.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ILogger _logger;
    private readonly SettingsService _settingsService;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindowViewModel(ILogger logger)
    {
        _logger = logger;
        _settingsService = new SettingsService();

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
        
        ChatPanel = new ChatPanelViewModel();
        
        // Load API key and model if they exist
        if (!string.IsNullOrEmpty(_settingsService.ApiKey))
        {
            ChatPanel.SetApiKey(_settingsService.ApiKey);
        }
        ChatPanel.SetModel(_settingsService.SelectedModel);

        LoadClips(CurrentPath);
    }

    private void LoadClips(string pathToLoad)
    {
        var clipContext = new ClipRepository(pathToLoad);
        clipContext.LoadClips();

        FileMakerClips.Clear();

        foreach (var clip in clipContext.Clips)
        {
            FileMakerClips.Add(new ClipViewModel(
                    new FileMakerClip(
                        clip.ClipName,
                        clip.ClipType,
                        clip.ClipXml
                    )
                )
            );
        }
    }

    public async Task OpenFolderPicker()
    {
        if (App.Current?.Services?.GetService<FolderService>() is FolderService folderService)
        {
            CurrentPath = await folderService.GetFolderAsync();

            LoadClips(CurrentPath);
        }
    }

    public void SaveClipsStorage()
    {
        var clipContext = new ClipRepository(CurrentPath);

        var fsClips = clipContext.Clips.ToList();

        foreach (var clip in FileMakerClips)
        {
            var dbClip = fsClips.FirstOrDefault(dbc => dbc.ClipName == clip.Name);

            if (dbClip is not null)
            {
                dbClip.ClipType = clip.ClipType;
                dbClip.ClipXml = clip.ClipXml;
            }
            else
            {
                clipContext.Clips.Add(new Clip()
                {
                    ClipName = clip.Name,
                    ClipType = clip.ClipType,
                    ClipXml = clip.ClipXml
                });
            }
        }

        clipContext.SaveChanges();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound to Xaml Button, throws when static.")]
    public void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    public void NewEmptyItem()
    {
        try
        {
            var clip = new FileMakerClip("New", FileMakerClip.ClipTypes.First()?.KeyId ?? "", Array.Empty<byte>());
            var clipVm = new ClipViewModel(clip);

            FileMakerClips.Add(clipVm);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error creating new Clip.");
        }
    }

    public void CopyAsClass()
    {
        // TODO: improve the UX of this whole thing. This works as a hack 
        // for proving the concept, but it could be so much better.
        try
        {
            if (SelectedClip == null)
            {
                // no clip selected;
                return;
            }

            // See DepInject project for a sample of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.Clipboard is not { } provider)
                throw new NullReferenceException("Missing Clipboard instance.");

            var classString = SelectedClip.Clip.CreateClass();
            provider.SetTextAsync(classString);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error Copying as Class.");
        }
    }

    public async Task PasteFileMakerClipData()
    {
        try
        {
            // See DepInject project for a sample of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.Clipboard is not { } provider)
                throw new NullReferenceException("Missing Clipboard instance.");

            var formats = await provider.GetFormatsAsync();

            foreach (var format in formats.Where(f => f.StartsWith("Mac-", StringComparison.CurrentCultureIgnoreCase)).Distinct())
            {
                if (string.IsNullOrEmpty(format)) { continue; }

                object? clipData = await provider.GetDataAsync(format);

                if (clipData is not byte[] dataObj)
                {
                    // this is some type of clipboard data this program can't handle
                    continue;
                }

                var clip = new FileMakerClip("new-clip", format, dataObj);

                if (clip is null) { continue; }

                // don't bother adding a duplicate. For some reason entries were getting entered twice per clip
                // this is not the most efficient method to detect it, but it works well enough for now
                if (FileMakerClips.Any(k => k.ClipXml == clip.XmlData))
                {
                    continue;
                }

                FileMakerClips.Add(new ClipViewModel(clip));
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error translating FileMaker blob to Xml.");
        }
    }

    public async Task CopySelectedToClip()
    {
        try
        {
            // See DepInject project for a sample of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.Clipboard is not { } provider)
                throw new NullReferenceException("Missing Clipboard instance.");

            var dp = new DataPackage();

            if (SelectedClip is not ClipViewModel data)
            {
                return; // no data
            }

            dp.SetData(data.ClipType, data.Clip.RawData);

            await provider.SetDataObjectAsync(dp);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error returning the selected Clip FileMaker blob format.");
        }
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
            ChatPanel.SetSelectedClip(value);
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
            FilteredClips.Clear();
            foreach (var c in FileMakerClips.Where(c => c.Name.Contains(_searchText)))
            {
                FilteredClips.Add(c);
            }
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

    public ChatPanelViewModel ChatPanel { get; }

    public void ToggleChatPanel()
    {
        ChatPanel.ToggleVisibility();
    }

    public void SetApiKey(string apiKey)
    {
        _settingsService.ApiKey = apiKey;
        ChatPanel.SetApiKey(apiKey);
    }
    
    public void SetModel(string model)
    {
        _settingsService.SelectedModel = model;
        ChatPanel.SetModel(model);
    }

    public string? GetApiKey()
    {
        return _settingsService.ApiKey;
    }

    public async Task ShowApiKeyDialog()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow is not { } mainWindow)
                return;

            var dialog = new Views.ApiKeyDialog();
            dialog.SetCurrentSettings(_settingsService.ApiKey, _settingsService.SelectedModel);
            
            var result = await dialog.ShowDialog<bool>(mainWindow);
            
            if (result)
            {
                if (!string.IsNullOrEmpty(dialog.ApiKey))
                {
                    SetApiKey(dialog.ApiKey);
                }
                
                if (!string.IsNullOrEmpty(dialog.SelectedModel))
                {
                    SetModel(dialog.SelectedModel);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error showing API key dialog");
        }
    }

}