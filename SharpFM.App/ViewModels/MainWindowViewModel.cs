using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Data;
using Microsoft.Extensions.Logging;
using SharpFM.App.Models;
using SharpFM.Core;

namespace SharpFM.App.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ILogger _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindowViewModel(ILogger logger)
    {
        _logger = logger;

        using var clipContext = new ClipDbContext();
        clipContext.Database.EnsureCreated();

        _logger.LogInformation($"Database path: {clipContext.DbPath}.");

        FileMakerClips = new ObservableCollection<ClipViewModel>();

        foreach (var clip in clipContext.Clips)
        {
            FileMakerClips.Add(new ClipViewModel(
                    new FileMakerClip(
                        clip.ClipName,
                        clip.ClipType,
                        clip.ClipXml
                    ),
                    clip.ClipId
                )
            );
        }
    }

    public void SaveToDb()
    {
        using var clipContext = new ClipDbContext();

        var dbClips = clipContext.Clips.ToList();

        foreach (var clip in FileMakerClips)
        {
            var dbClip = dbClips.FirstOrDefault(dbc => dbc.ClipName == clip.Name);

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

    public void ClearDb()
    {
        using var clipContext = new ClipDbContext();

        var clips = clipContext.Clips.ToList();

        foreach (var clip in clips)
        {
            clipContext.Clips.Remove(clip);
        }

        clipContext.SaveChanges();
    }

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
    public string Version
    {
        get
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            return version ?? "v?.?.?";
        }
    }

    public ObservableCollection<ClipViewModel> FileMakerClips { get; set; }

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
}