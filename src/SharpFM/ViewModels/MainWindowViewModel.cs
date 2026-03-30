using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SharpFM.Models;
using SharpFM.Services;

namespace SharpFM.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ILogger _logger;
    private readonly IClipboardService _clipboard;
    private readonly DispatcherTimer _statusTimer;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindowViewModel(ILogger logger, IClipboardService clipboard)
    {
        _logger = logger;
        _clipboard = clipboard;

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
        if (App.Current?.Services?.GetService(typeof(FolderService)) is FolderService folderService)
        {
            CurrentPath = await folderService.GetFolderAsync();
            LoadClips(CurrentPath);
        }
    }

    public void SaveClipsStorage()
    {
        try
        {
            var clipContext = new ClipRepository(CurrentPath);
            var fsClips = clipContext.Clips.ToList();

            foreach (var clip in FileMakerClips)
            {
                // Sync model to XML before saving
                clip.SyncModelFromEditor();

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
            ShowStatus($"Saved {FileMakerClips.Count} clip(s) to {CurrentPath}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving clips.");
            ShowStatus("Error saving clips");
        }
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
            FileMakerClips.Add(new ClipViewModel(clip));
            ShowStatus("Created new clip");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating new clip.");
            ShowStatus("Error creating clip");
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
            ShowStatus("Error copying to clipboard");
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
            ShowStatus("Error pasting from clipboard");
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
            // Sync model to XML before copying to ensure current data
            data.SyncModelFromEditor();
            await _clipboard.SetDataAsync(data.ClipType, data.Clip.RawData);
            ShowStatus("Copied to FileMaker clipboard");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error copying to FileMaker clipboard.");
            ShowStatus("Error copying to clipboard");
        }
    }

    private void ShowStatus(string message)
    {
        StatusMessage = message;
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
}
