using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Data;
using SharpFM.Core;

namespace SharpFM.App.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    public ClipDbContext _context;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindowViewModel()
    {
        _context = new ClipDbContext();
        _context.Database.EnsureCreated();

        Console.WriteLine($"Database path: {_context.DbPath}.");

        Keys = new ObservableCollection<ClipViewModel>();

        foreach (var clip in _context.Clips)
        {
            Keys.Add(new ClipViewModel(
                    new FileMakerClip(
                        clip.ClipName,
                        clip.ClipType,
                        clip.ClipXml
                    )
                )
            );
        }
    }

    public void SaveToDb()
    {
        var dbClips = _context.Clips.ToList();

        foreach (var clip in Keys)
        {
            if (dbClips.Any(dbc => dbc.ClipName == clip.Name))
            {
                var dbClip = dbClips.First(dbc => dbc.ClipName == clip.Name);
                dbClip.ClipType = clip.ClipType;
                dbClip.ClipXml = clip.ClipXml;
            }
            else
            {
                _context.Clips.Add(new Clip()
                {
                    ClipName = clip.Name,
                    ClipType = clip.ClipType,
                    ClipXml = clip.ClipXml
                });
            }
        }

        _context.SaveChanges();
    }

    public void ClearDb()
    {
        var clips = _context.Clips.ToList();

        foreach (var clip in clips)
        {
            _context.Clips.Remove(clip);
        }

        _context.SaveChanges();
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

            Keys.Add(clipVm);
        }
        catch (Exception e)
        {
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
        }
    }

    public async Task PasteFileMakerClipData(CancellationToken token)
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
                if (Keys.Any(k => k.ClipXml == clip.XmlData))
                {
                    continue;
                }

                Keys.Add(new ClipViewModel(clip));
            }
        }
        catch (Exception e)
        {
        }
    }

    public async Task CopySelectedToClip(CancellationToken token)
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
        }
    }

    public string Version
    {
        get
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            return version ?? "v?.?.?";
        }
    }

    public ObservableCollection<ClipViewModel> Keys { get; set; }

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