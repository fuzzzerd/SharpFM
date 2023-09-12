using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Data;
using SharpFM.Core;

namespace SharpFM.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Keys = new ObservableCollection<FileMakerClip>();
    }

    [ObservableProperty] private string? _text;

    [RelayCommand]
    private void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    [RelayCommand]
    private void NewEmptyItem()
    {
        ErrorMessages?.Clear();
        try
        {
            Keys.Add(new FileMakerClip("New", "", Array.Empty<byte>()));
        }
        catch (Exception e)
        {
            ErrorMessages?.Add(e.Message);
        }
    }

    [RelayCommand]
    private async Task PasteText(CancellationToken token)
    {
        ErrorMessages?.Clear();
        try
        {
            await DoGetClipboardDataAsync();
        }
        catch (Exception e)
        {
            ErrorMessages?.Add(e.Message);
        }
    }
    [RelayCommand]
    private async Task CopySelectedToClip(CancellationToken token)
    {
        ErrorMessages?.Clear();
        try
        {
            // See DepInject project for a sample of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.Clipboard is not { } provider)
                throw new NullReferenceException("Missing Clipboard instance.");


            var dp = new DataPackage();

            if (!(SelectedClip is FileMakerClip data))
            {
                return; // no data
            }

            // recalculate the length of the original text and make sure that is the first four bytes in the stream
            //var code = data.RawData;// XamlCodeRenderer.Text;
            //byte[] byteList = Encoding.UTF8.GetBytes(code);
            //int bl = byteList.Length;
            //byte[] intBytes = BitConverter.GetBytes(bl);

            //dp.SetData("Mac-XMSS", intBytes.Concat(byteList).ToArray().AsBuffer().AsStream().AsRandomAccessStream());
            dp.SetData(data.ClipboardFormat, data.RawData);

            await provider.SetDataObjectAsync(dp);
        }
        catch (Exception e)
        {
            ErrorMessages?.Add(e.Message);
        }
    }
    private async Task DoGetClipboardDataAsync()
    {
        // For learning purposes, we opted to directly get the reference
        // for StorageProvider APIs here inside the ViewModel. 

        // For your real-world apps, you should follow the MVVM principles
        // by making service classes and locating them with DI/IoC.

        // See DepInject project for a sample of how to accomplish this.
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            throw new NullReferenceException("Missing Clipboard instance.");

        var formats = await provider.GetFormatsAsync();

        foreach (var format in formats.Where(f => f.StartsWith("Mac-", StringComparison.CurrentCultureIgnoreCase)).Distinct())
        {
            if (string.IsNullOrEmpty(format)) { continue; }

            object? clipData = await provider.GetDataAsync(format);

            if (!(clipData is byte[] dataObj))
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

    [ObservableProperty]
    private ObservableCollection<FileMakerClip> _keys;

    //public ObservableCollection<FileMakerClip> Layouts { get; }

    [ObservableProperty]
    private FileMakerClip? _selectedLayout;

    [ObservableProperty]
    private FileMakerClip? _selectedClip;
}