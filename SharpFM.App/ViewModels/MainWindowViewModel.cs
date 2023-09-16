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
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindowViewModel()
    {
        Keys = new ObservableCollection<FileMakerClip>();
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
            Keys.Add(new FileMakerClip("New", FileMakerClip.ClipTypes.First()?.KeyId ?? "", Array.Empty<byte>()));
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

            var classString = SelectedClip.CreateClass();
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
                if (Keys.Any(k => k.XmlData == clip.XmlData))
                {
                    continue;
                }

                Keys.Add(clip);
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

            if (SelectedClip is not FileMakerClip data)
            {
                return; // no data
            }

            dp.SetData(data.ClipboardFormat, data.RawData);

            await provider.SetDataObjectAsync(dp);
        }
        catch (Exception e)
        {
        }
    }

    public ObservableCollection<FileMakerClip> Keys { get; set; }

    private FileMakerClip? _selectedClip;
    public FileMakerClip? SelectedClip
    {
        get => _selectedClip;
        set
        {
            _selectedClip = value;
            NotifyPropertyChanged();
        }
    }
}