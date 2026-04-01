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
    public IClipEditor Editor { get; }

    /// <summary>
    /// Fires when the editor content changes due to user edits (debounced).
    /// Not fired for external XML pushes (guarded by generation counter).
    /// </summary>
    public event EventHandler? EditorContentChanged;

    private int _syncGeneration;

    public ClipViewModel(FileMakerClip clip)
    {
        Clip = clip;

        Editor = clip.ClipboardFormat switch
        {
            "Mac-XMSS" or "Mac-XMSC" => new ScriptClipEditor(clip.XmlData),
            "Mac-XMTB" or "Mac-XMFD" => new TableClipEditor(clip.XmlData),
            _ => new FallbackXmlEditor(clip.XmlData),
        };

        Editor.ContentChanged += OnEditorContentChanged;
    }

    private void OnEditorContentChanged(object? sender, EventArgs e)
    {
        var gen = _syncGeneration;
        Clip.XmlData = Editor.ToXml();
        if (gen != _syncGeneration) return; // external update arrived during sync, stale
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
    // These delegate to the typed editor so MainWindow.axaml doesn't need to change.

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

    /// <summary>
    /// Sync the editor state to XML. Called before save/clipboard operations.
    /// </summary>
    public void SyncModelFromEditor()
    {
        Clip.XmlData = Editor.ToXml();
    }

    /// <summary>
    /// Push XML into the editor (reverse sync). Bumps the generation counter
    /// so the debounced ContentChanged event knows to discard the stale tick.
    /// </summary>
    public void SyncEditorFromXml()
    {
        _syncGeneration++;
        Editor.FromXml(Clip.XmlData ?? "");
    }
}
