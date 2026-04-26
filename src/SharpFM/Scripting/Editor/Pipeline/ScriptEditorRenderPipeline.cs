using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SharpFM.Editors;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Owns the script editor's render layers and consolidates their event
/// handling + invalidation. Replaces the four standalone
/// <c>IBackgroundRenderer</c> implementations (bracket, statement,
/// continuation, error) with one shared pipeline:
///
/// <list type="bullet">
///   <item>One subscription to <see cref="Caret.PositionChanged"/>;
///   realtime layers are dispatched in order and each reports whether
///   its draw output changed.</item>
///   <item>One debounced subscription to <c>Document.TextChanged</c>;
///   idle layers are recomputed once per quiet window so per-keystroke
///   work stays out of the hot path.</item>
///   <item>Two <see cref="IBackgroundRenderer"/> wrappers — one per
///   <see cref="KnownLayer"/> (Background + Selection) — host the
///   feature layers in their correct visual stacking order.</item>
///   <item>One <see cref="TextView.InvalidateLayer"/> call per affected
///   <see cref="KnownLayer"/> per event, instead of one per dirty
///   layer.</item>
/// </list>
///
/// The pipeline is owned by <see cref="ScriptEditorController"/> and
/// disposed alongside it.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ScriptEditorRenderPipeline : IDisposable
{
    private const int IdleDebounceMs = 150;

    private readonly TextArea _textArea;
    private readonly RenderContext _context;
    private readonly StatementHighlightLayer _statementLayer;
    private readonly ContinuationRailLayer _continuationLayer;
    private readonly BracketMatchLayer _bracketLayer;
    private readonly ErrorMarkerLayer _errorLayer;
    private readonly SealedStepLayer _sealedLayer;
    private readonly LayeredBackgroundRenderer _bgRenderer;
    private readonly LayeredBackgroundRenderer _selRenderer;
    private readonly DispatcherTimer _idleTimer;
    private readonly IRenderLayer[] _allLayers;
    private TextDocument? _attachedDocument;
    private ScriptClipEditor? _clipEditor;
    private bool _disposed;

    public ScriptEditorRenderPipeline(TextArea textArea)
    {
        _textArea = textArea;
        _context = new RenderContext(textArea);

        _statementLayer = new StatementHighlightLayer();
        _continuationLayer = new ContinuationRailLayer();
        _bracketLayer = new BracketMatchLayer();
        _errorLayer = new ErrorMarkerLayer();
        _sealedLayer = new SealedStepLayer();

        _allLayers = new IRenderLayer[]
        {
            _statementLayer, _continuationLayer,
            _bracketLayer, _errorLayer, _sealedLayer,
        };

        _bgRenderer = new LayeredBackgroundRenderer(
            _context,
            KnownLayer.Background,
            new IRenderLayer[] { _statementLayer, _continuationLayer });

        _selRenderer = new LayeredBackgroundRenderer(
            _context,
            KnownLayer.Selection,
            new IRenderLayer[] { _sealedLayer, _bracketLayer, _errorLayer });

        textArea.TextView.BackgroundRenderers.Add(_bgRenderer);
        textArea.TextView.BackgroundRenderers.Add(_selRenderer);

        textArea.Caret.PositionChanged += OnCaretChanged;

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(IdleDebounceMs) };
        _idleTimer.Tick += OnIdleTick;

        AttachDocument(textArea.Document);
        textArea.DocumentChanged += OnTextAreaDocumentChanged;
    }

    /// <summary>
    /// Wire the pipeline to a clip editor so layers (sealed-step layer
    /// today; possibly more later) can read its cached snapshot through
    /// <see cref="RenderContext.SealedLineNumbers"/>. Pass <c>null</c>
    /// to detach.
    /// </summary>
    public void AttachClipEditor(ScriptClipEditor? clipEditor)
    {
        if (_clipEditor != null)
            _clipEditor.SealedCacheChanged -= OnSealedCacheChanged;

        _clipEditor = clipEditor;
        _context.SetClipEditor(clipEditor);

        if (clipEditor != null)
            clipEditor.SealedCacheChanged += OnSealedCacheChanged;

        // Force a fresh idle pass so the sealed layer recomputes
        // immediately under the new clip's snapshot.
        ScheduleIdleRecompute();
    }

    /// <summary>
    /// Replace the validator's diagnostic list. Returns whether the
    /// list changed; callers (the controller's RunValidation path)
    /// only need to invalidate when this returns true.
    /// </summary>
    public bool UpdateDiagnostics(List<ScriptDiagnostic> diagnostics)
    {
        if (!_context.SetDiagnostics(diagnostics)) return false;
        _textArea.TextView.InvalidateLayer(_errorLayer.TargetLayer);
        return true;
    }

    /// <summary>
    /// Look up the diagnostic at a document offset. Used by the
    /// controller's pointer-hover path to populate tooltips.
    /// </summary>
    public ScriptDiagnostic? GetDiagnosticAtOffset(int offset) =>
        _errorLayer.GetDiagnosticAtOffset(_context, offset);

    private void OnCaretChanged(object? sender, EventArgs e)
    {
        var bgDirty = false;
        var selDirty = false;

        foreach (var layer in _allLayers)
        {
            if (layer.Cadence != RenderCadence.Realtime) continue;
            if (!layer.OnCaretChanged(_context)) continue;
            if (layer.TargetLayer == KnownLayer.Background) bgDirty = true;
            else selDirty = true;
        }

        if (bgDirty) _textArea.TextView.InvalidateLayer(KnownLayer.Background);
        if (selDirty) _textArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    private void OnIdleTick(object? sender, EventArgs e)
    {
        _idleTimer.Stop();

        var bgDirty = false;
        var selDirty = false;

        foreach (var layer in _allLayers)
        {
            if (layer.Cadence != RenderCadence.Idle) continue;
            if (!layer.OnTextChanged(_context)) continue;
            if (layer.TargetLayer == KnownLayer.Background) bgDirty = true;
            else selDirty = true;
        }

        if (bgDirty) _textArea.TextView.InvalidateLayer(KnownLayer.Background);
        if (selDirty) _textArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    private void ScheduleIdleRecompute()
    {
        _idleTimer.Stop();
        _idleTimer.Start();
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e) =>
        ScheduleIdleRecompute();

    private void OnSealedCacheChanged(object? sender, EventArgs e) =>
        ScheduleIdleRecompute();

    private void OnTextAreaDocumentChanged(object? sender, EventArgs e)
    {
        AttachDocument(_textArea.Document);
        // New document means the cached snapshot the layers hold may
        // not match the doc that's about to paint — recompute soon.
        ScheduleIdleRecompute();
    }

    private void AttachDocument(TextDocument? document)
    {
        if (ReferenceEquals(_attachedDocument, document)) return;
        if (_attachedDocument != null)
            _attachedDocument.TextChanged -= OnDocumentTextChanged;
        _attachedDocument = document;
        if (document != null)
            document.TextChanged += OnDocumentTextChanged;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _idleTimer.Stop();
        _idleTimer.Tick -= OnIdleTick;
        _textArea.Caret.PositionChanged -= OnCaretChanged;
        _textArea.DocumentChanged -= OnTextAreaDocumentChanged;
        if (_attachedDocument != null)
            _attachedDocument.TextChanged -= OnDocumentTextChanged;
        if (_clipEditor != null)
            _clipEditor.SealedCacheChanged -= OnSealedCacheChanged;
        _textArea.TextView.BackgroundRenderers.Remove(_bgRenderer);
        _textArea.TextView.BackgroundRenderers.Remove(_selRenderer);
    }
}
