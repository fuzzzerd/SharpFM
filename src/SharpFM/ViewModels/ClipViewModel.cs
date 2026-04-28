using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using SharpFM.Editors;
using SharpFM.Model;
using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;
using SharpFM.Schema.Editor;

namespace SharpFM.ViewModels;

/// <summary>
/// View-model wrapper around an immutable <see cref="Clip"/> aggregate. The
/// clip itself owns parsing and the round-trip report; this class adds the
/// editor lifecycle, dirty tracking, and INPC for Avalonia bindings.
/// </summary>
public partial class ClipViewModel : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private Clip _clip;

    /// <summary>The current immutable clip aggregate. Replaced wholesale on edits.</summary>
    public Clip Clip
    {
        get => _clip;
        private set
        {
            if (ReferenceEquals(_clip, value)) return;
            _clip = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The clip-type-specific editor. Receives an already-parsed model from
    /// <see cref="ClipEditorViewFactory.CreateEditor"/>; no XML parsing happens
    /// here.
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

    public ClipViewModel(Clip clip)
    {
        _clip = clip;
        Editor = ClipEditorViewFactory.CreateEditor(clip);
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
    /// Wholesale replacement: re-parse the clip with new XML and rebuild the
    /// editor around the resulting model. Used for all external updates
    /// (MCP, plugins, XML viewer).
    /// </summary>
    public void Replace(string xml)
    {
        Editor.ContentChanged -= OnEditorContentChanged;
        Clip = _clip.WithXml(xml);
        Editor = ClipEditorViewFactory.CreateEditor(_clip);
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
        NotifyPropertyChanged(nameof(ParseReport));
        NotifyPropertyChanged(nameof(IsLossless));
    }

    /// <summary>The fidelity report from the most recent XML→domain parse.</summary>
    public ClipParseReport ParseReport => _clip.Parsed.Report;

    /// <summary>True when no parse loss was detected on the current XML.</summary>
    public bool IsLossless => ParseReport.IsLossless;

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

    private void OnEditorContentChanged(object? sender, EventArgs e) =>
        HandleEditorContentChanged();

    // Exposed to the test assembly (InternalsVisibleTo SharpFM.Tests) so tests
    // can drive the post-debounce path without standing up an Avalonia
    // Dispatcher. In production this is invoked from Editor.ContentChanged.
    internal void HandleEditorContentChanged()
    {
        // Editor edits are display→XML→model. The aggregate's parsed state
        // becomes stale; re-derive it via WithXml so ParseReport reflects the
        // current XML.
        Clip = _clip.WithXml(Editor.ToXml());
        NotifyPropertyChanged(nameof(IsDirty));
        NotifyPropertyChanged(nameof(ParseReport));
        NotifyPropertyChanged(nameof(IsLossless));
        EditorContentChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsScriptClip =>
        _clip.Parsed is ParseSuccess { Model: ScriptClipModel };

    public bool IsTableClip =>
        _clip.Parsed is ParseSuccess { Model: TableClipModel };

    public bool IsFallbackClip => !IsScriptClip && !IsTableClip;

    public string ClipTypeDisplay => ClipTypeRegistry.For(_clip.FormatId).DisplayName;

    // --- Convenience properties for AXAML bindings ---

    public TextDocument? ScriptDocument => (Editor as ScriptClipEditor)?.Document;

    public TableEditorViewModel? TableEditor => (Editor as TableClipEditor)?.ViewModel;

    public TextDocument XmlDocument =>
        (Editor as FallbackXmlEditor)?.Document ?? new TextDocument(_clip.Xml);

    public string ClipType => _clip.FormatId;
}
