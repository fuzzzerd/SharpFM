using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AvaloniaEdit.Document;
using SharpFM.Editors;
using SharpFM.Schema.Editor;

namespace SharpFM.ViewModels;

public partial class ClipViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FileMakerClip Clip { get; set; }

    /// <summary>
    /// The clip-type-specific editor. Handles change detection, XML serialization,
    /// and reverse sync for this clip's format.
    /// </summary>
    public IClipEditor Editor { get; private set; }

    /// <summary>
    /// Fires when the editor content changes due to user edits (debounced).
    /// </summary>
    public event EventHandler? EditorContentChanged;

    public ClipViewModel(FileMakerClip clip)
    {
        Clip = clip;
        Editor = CreateEditor(clip.XmlData);
        Editor.ContentChanged += OnEditorContentChanged;
    }

    /// <summary>
    /// Wholesale replacement: discard the current editor and create a fresh one
    /// from the given XML. Used for all external updates (MCP, plugins, XML viewer).
    /// The new editor re-parses the XML into fresh domain model state.
    /// </summary>
    public void ReplaceEditor(string xml)
    {
        Editor.ContentChanged -= OnEditorContentChanged;
        Clip.XmlData = xml;
        Editor = CreateEditor(xml);
        Editor.ContentChanged += OnEditorContentChanged;

        NotifyPropertyChanged(nameof(Editor));
        NotifyPropertyChanged(nameof(ScriptDocument));
        NotifyPropertyChanged(nameof(TableEditor));
        NotifyPropertyChanged(nameof(XmlDocument));
        NotifyPropertyChanged(nameof(ClipXml));
    }

    private IClipEditor CreateEditor(string? xml) => Clip.ClipboardFormat switch
    {
        "Mac-XMSS" or "Mac-XMSC" => new ScriptClipEditor(xml),
        "Mac-XMTB" or "Mac-XMFD" => new TableClipEditor(xml),
        _ => new FallbackXmlEditor(xml),
    };

    private void OnEditorContentChanged(object? sender, EventArgs e)
    {
        Clip.XmlData = Editor.ToXml();
        EditorContentChanged?.Invoke(this, EventArgs.Empty);
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

    // --- Convenience properties for AXAML bindings ---

    public TextDocument? ScriptDocument => (Editor as ScriptClipEditor)?.Document;

    public TableEditorViewModel? TableEditor => (Editor as TableClipEditor)?.ViewModel;

    public TextDocument XmlDocument =>
        (Editor as FallbackXmlEditor)?.Document ?? new TextDocument(Clip.XmlData ?? "");

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
