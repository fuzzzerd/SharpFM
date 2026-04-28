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

    /// <summary>Re-parse the clip with new XML; used for external updates (MCP, plugins, XML viewer).</summary>
    public void Replace(string xml)
    {
        Editor.ContentChanged -= OnEditorContentChanged;
        Clip = _clip.WithXml(xml);
        Editor = ClipEditorViewFactory.CreateEditor(_clip);
        Editor.ContentChanged += OnEditorContentChanged;

        if (_editorView is IDisposable d) d.Dispose();
        _editorView = null;

        // Reverse sync from external sources must not flag the clip dirty —
        // the source of truth is what the editor now holds.
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

    public ClipParseReport ParseReport => _clip.Parsed.Report;

    public bool IsLossless => ParseReport.IsLossless;

    /// <summary>Captures the current XML as the saved baseline; clears the dirty indicator.</summary>
    public void MarkSaved()
    {
        _savedXml = Editor.ToXml();
        NotifyPropertyChanged(nameof(IsDirty));
    }

    public bool IsDirty =>
        !string.Equals(Editor.ToXml(), _savedXml, StringComparison.Ordinal);

    private void OnEditorContentChanged(object? sender, EventArgs e) =>
        HandleEditorContentChanged();

    // Exposed to the test assembly (InternalsVisibleTo SharpFM.Tests) so tests
    // can drive the post-debounce path without standing up an Avalonia
    // Dispatcher. In production this is invoked from Editor.ContentChanged.
    internal void HandleEditorContentChanged()
    {
        // Trusted-edit path: the editor produced this XML from a model it
        // already holds, so the round-trip is lossless by construction. Hand
        // both over to the aggregate to skip the strategy parse + diff —
        // critical for large scripts where that work would freeze the UI
        // every debounced keystroke.
        var xml = Editor.ToXml();
        var model = Editor.GetModel();
        Clip = Clip.FromEditor(_clip.Name, _clip.FormatId, xml, model);
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
