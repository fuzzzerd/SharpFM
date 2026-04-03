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
    /// Fires when the editor is saved, indicating the model has been updated.
    /// Replaces the old debounced ContentChanged event.
    /// </summary>
    public event EventHandler? EditorContentChanged;

    /// <summary>
    /// Fires when the editor becomes dirty (user made first edit since last save/load).
    /// </summary>
    public event EventHandler? EditorBecameDirty;

    /// <summary>
    /// Whether the editor has unsaved changes.
    /// </summary>
    public bool IsDirty => Editor.IsDirty;

    public ClipViewModel(FileMakerClip clip)
    {
        Clip = clip;

        Editor = clip.ClipboardFormat switch
        {
            "Mac-XMSS" or "Mac-XMSC" => new ScriptClipEditor(clip.XmlData),
            "Mac-XMTB" or "Mac-XMFD" => new TableClipEditor(clip.XmlData),
            _ => new FallbackXmlEditor(clip.XmlData),
        };

        Editor.Saved += OnEditorSaved;
        Editor.BecameDirty += OnEditorBecameDirty;
    }

    private void OnEditorSaved(object? sender, EventArgs e)
    {
        Clip.XmlData = Editor.ToXml();
        NotifyPropertyChanged(nameof(IsDirty));
        EditorContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorBecameDirty(object? sender, EventArgs e)
    {
        NotifyPropertyChanged(nameof(IsDirty));
        EditorBecameDirty?.Invoke(this, EventArgs.Empty);
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
    /// Save the editor's local buffer to the model. Updates Clip.XmlData.
    /// Returns true if save succeeded.
    /// </summary>
    public bool SaveEditor()
    {
        var result = Editor.Save();
        if (result)
            Clip.XmlData = Editor.ToXml();
        return result;
    }

    /// <summary>
    /// Sync the editor state to XML. Called before save/clipboard operations.
    /// Auto-saves if dirty.
    /// </summary>
    public void SyncModelFromEditor()
    {
        if (Editor.IsDirty)
            SaveEditor();
        else
            Clip.XmlData = Editor.ToXml();
    }

    /// <summary>
    /// Push XML into the editor from an external source (plugin, agent, other editor).
    /// Replaces the local buffer and model. Clears dirty state.
    /// </summary>
    public void SyncEditorFromXml()
    {
        Editor.FromXml(Clip.XmlData ?? "");
        NotifyPropertyChanged(nameof(IsDirty));
    }
}
