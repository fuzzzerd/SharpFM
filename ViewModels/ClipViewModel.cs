using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SharpFM.ViewModels;

public partial class ClipViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FileMakerClip Clip { get; set; }

    public ClipViewModel(FileMakerClip clip) : this(clip, null) { }

    public ClipViewModel(FileMakerClip clip, int? clipId)
    {
        Clip = clip;
        ClipId = clipId;
    }

    private int? _clipId;
    public int? ClipId
    {
        get => _clipId;
        set
        {
            _clipId = value;
            NotifyPropertyChanged();
        }
    }

    public string ClipType
    {
        get => Clip.ClipboardFormat;
        set
        {
            Clip.ClipboardFormat = value;
            NotifyPropertyChanged();
        }
    }

    public string Name
    {
        get => Clip.Name;
        set
        {
            Clip.Name = value;
            NotifyPropertyChanged();
        }
    }

    public string ClipXml
    {
        get => Clip.XmlData;
        set
        {
            Clip.XmlData = value;
            NotifyPropertyChanged();
        }
    }
}