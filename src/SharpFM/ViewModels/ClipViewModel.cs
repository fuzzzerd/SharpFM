using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using SharpFM.Editors;
using SharpFM.Model;
using SharpFM.Schema.Editor;

namespace SharpFM.ViewModels;

public partial class ClipViewModel : INotifyPropertyChanged, IDisposable
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
    /// Hierarchy the clip lives under in its repository. Matches
    /// <see cref="ClipData.FolderPath"/> — empty list means "root".
    /// </summary>
    public IReadOnlyList<string> FolderPath { get; set; } = [];

    /// <summary>
    /// Fires when the editor content changes due to user edits (debounced).
    /// </summary>
    public event EventHandler? EditorContentChanged;

    /// <summary>
    /// Snapshot of the editor's XML the last time the clip was known to be
    /// persisted. Used to compute <see cref="IsDirty"/>.
    /// </summary>
    private string _savedXml;

    public ClipViewModel(FileMakerClip clip)
    {
        Clip = clip;
        Editor = CreateEditor(clip.XmlData);
        Editor.ContentChanged += OnEditorContentChanged;
        _savedXml = Editor.ToXml();
    }

    // The Avalonia control that renders this clip. Cached so tab switches
    // reparent the same control instead of reconstructing a ScriptTextEditor
    // (and re-running its TextMate install) on every switch. Lazily built on
    // first access.
    private Control? _editorView;
    public Control EditorView => _editorView ??= ClipEditorViewFactory.Create(Editor);

    public void Dispose()
    {
        Editor.ContentChanged -= OnEditorContentChanged;
        if (_editorView is IDisposable d) d.Dispose();
        _editorView = null;
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

        // The cached editor view was built around the old editor instance.
        // Tear it down so the next EditorView access rebuilds cleanly.
        if (_editorView is IDisposable d) d.Dispose();
        _editorView = null;

        // Reverse sync from plugins / external tools must not flag the clip
        // dirty — the source of truth is what the editor now holds.
        _savedXml = Editor.ToXml();
        NotifyPropertyChanged(nameof(IsDirty));

        NotifyPropertyChanged(nameof(Editor));
        NotifyPropertyChanged(nameof(EditorView));
        NotifyPropertyChanged(nameof(ScriptDocument));
        NotifyPropertyChanged(nameof(TableEditor));
        NotifyPropertyChanged(nameof(XmlDocument));
    }

    /// <summary>
    /// Called by the host after a successful save; captures the current XML as
    /// the "clean" baseline so edits that follow light the dirty indicator.
    /// </summary>
    public void MarkSaved()
    {
        _savedXml = Editor.ToXml();
        NotifyPropertyChanged(nameof(IsDirty));
    }

    /// <summary>
    /// True when the editor's current XML differs from the last saved snapshot.
    /// Drives the dirty dot on open tabs. Computed on demand — cheap enough
    /// for a UI binding evaluated only when <c>ContentChanged</c> fires.
    /// </summary>
    public bool IsDirty =>
        !string.Equals(Editor.ToXml(), _savedXml, StringComparison.Ordinal);

    private IClipEditor CreateEditor(string? xml) => Clip.ClipboardFormat switch
    {
        "Mac-XMSS" or "Mac-XMSC" => new ScriptClipEditor(xml),
        "Mac-XMTB" or "Mac-XMFD" => new TableClipEditor(xml),
        _ => new FallbackXmlEditor(xml),
    };

    private void OnEditorContentChanged(object? sender, EventArgs e) =>
        HandleEditorContentChanged();

    // Exposed to the test assembly (InternalsVisibleTo SharpFM.Tests) so tests
    // can drive the post-debounce path without standing up an Avalonia
    // Dispatcher. In production this is invoked from Editor.ContentChanged.
    internal void HandleEditorContentChanged()
    {
        Clip.XmlData = Editor.ToXml();
        NotifyPropertyChanged(nameof(IsDirty));
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
}
