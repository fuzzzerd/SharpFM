using System.ComponentModel;
using System.Runtime.CompilerServices;
using AvaloniaEdit.Document;
using SharpFM.Schema.Editor;
using SharpFM.Schema.Model;
using SharpFM.Scripting;

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
    private TextDocument? _scriptDocument;
    private FmScript? _script;
    private TableEditorViewModel? _tableEditor;

    public ClipViewModel(FileMakerClip clip)
    {
        Clip = clip;
    }

    public bool IsScriptClip =>
        Clip.ClipboardFormat == "Mac-XMSS" || Clip.ClipboardFormat == "Mac-XMSC";

    public bool IsTableClip =>
        Clip.ClipboardFormat == "Mac-XMTB" || Clip.ClipboardFormat == "Mac-XMFD";

    public bool IsFallbackClip => !IsScriptClip && !IsTableClip;

    public string ClipTypeDisplay => Clip.ClipboardFormat switch
    {
        "Mac-XMSS" => "Script Steps",
        "Mac-XMSC" => "Script",
        "Mac-XMTB" => "Table",
        "Mac-XMFD" => "Field",
        "Mac-XML2" => "Layout",
        _ => Clip.ClipboardFormat
    };

    public TableEditorViewModel? TableEditor
    {
        get
        {
            if (_tableEditor == null && IsTableClip)
            {
                var table = FmTable.FromXml(Clip.XmlData ?? "");
                _tableEditor = new TableEditorViewModel(table);
            }
            return _tableEditor;
        }
    }

    public string ClipType
    {
        get => Clip.ClipboardFormat;
        set
        {
            Clip.ClipboardFormat = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(IsScriptClip));
            NotifyPropertyChanged(nameof(IsTableClip));
            NotifyPropertyChanged(nameof(IsFallbackClip));
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
                _xmlDocument = new TextDocument(Clip.XmlData ?? string.Empty);
            return _xmlDocument;
        }
    }

    public TextDocument ScriptDocument
    {
        get
        {
            if (_scriptDocument == null)
            {
                if (IsScriptClip)
                {
                    _script = FmScript.FromXml(Clip.XmlData ?? "");
                    _scriptDocument = new TextDocument(_script.ToDisplayText());
                }
                else
                {
                    _scriptDocument = new TextDocument("");
                }
            }
            return _scriptDocument;
        }
    }

    public string ClipXml
    {
        get => _xmlDocument?.Text ?? Clip.XmlData;
        set
        {
            Clip.XmlData = value;
            if (_xmlDocument != null)
                _xmlDocument.Text = value ?? string.Empty;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(XmlDocument));
        }
    }

    /// <summary>
    /// Sync the model from the current script editor text, then update XML.
    /// Called before save/clipboard operations and on tab switch.
    /// </summary>
    public void SyncModelFromEditor()
    {
        if (IsScriptClip && _scriptDocument != null)
        {
            _script = FmScript.FromDisplayText(_scriptDocument.Text);
            var xml = _script.ToXml();
            Clip.XmlData = xml;
            if (_xmlDocument != null)
                _xmlDocument.Text = xml;
        }
        else if (IsTableClip && _tableEditor != null)
        {
            _tableEditor.SyncToModel();
            var xml = _tableEditor.Table.ToXml();
            Clip.XmlData = xml;
            if (_xmlDocument != null)
                _xmlDocument.Text = xml;
        }
    }

    /// <summary>
    /// Rebuild the script editor text from the XML.
    /// Used when XML is edited externally (e.g., View XML window).
    /// </summary>
    public void SyncEditorFromXml()
    {
        if (!IsScriptClip) return;

        var xmlText = _xmlDocument?.Text ?? Clip.XmlData ?? "";
        _script = FmScript.FromXml(xmlText);
        if (_scriptDocument != null)
            _scriptDocument.Text = _script.ToDisplayText();
    }
}