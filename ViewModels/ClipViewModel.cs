using System.ComponentModel;
using System.Runtime.CompilerServices;
using AvaloniaEdit.Document;

namespace SharpFM.ViewModels;

public partial class ClipViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FileMakerClip Clip { get; set; }
    
    private TextDocument? _xmlDocument;

    public ClipViewModel(FileMakerClip clip)
    {
        Clip = clip;
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

    public TextDocument XmlDocument
    {
        get
        {
            if (_xmlDocument == null)
            {
                _xmlDocument = new TextDocument(Clip.XmlData ?? string.Empty);
            }
            return _xmlDocument;
        }
    }

    public string ClipXml
    {
        get => _xmlDocument?.Text ?? Clip.XmlData;
        set
        {
            Clip.XmlData = value;
            if (_xmlDocument != null)
            {
                _xmlDocument.Text = value ?? string.Empty;
            }
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(XmlDocument));
        }
    }
}