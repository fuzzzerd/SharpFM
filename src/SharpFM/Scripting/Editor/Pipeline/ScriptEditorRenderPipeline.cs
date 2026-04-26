using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SharpFM.Editors;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Owns the script editor's render layers and consolidates their event
/// handling + invalidation. Replaces the standalone
/// <c>IBackgroundRenderer</c> implementations (bracket, statement,
/// continuation, error, sealed-step) with one shared pipeline:
///
/// <list type="bullet">
///   <item>One subscription to <see cref="Caret.PositionChanged"/>;
///   layers are dispatched in order and each reports whether its draw
///   output changed.</item>
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
    private readonly TextArea _textArea;
    private readonly RenderContext _context;
    private readonly StatementHighlightLayer _statementLayer;
    private readonly ContinuationRailLayer _continuationLayer;
    private readonly BracketMatchLayer _bracketLayer;
    private readonly ErrorMarkerLayer _errorLayer;
    private readonly SealedStepLayer _sealedLayer;
    private readonly LayeredBackgroundRenderer _bgRenderer;
    private readonly LayeredBackgroundRenderer _selRenderer;
    private readonly IRenderLayer[] _allLayers;
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
    }

    /// <summary>
    /// Wire the pipeline to a clip editor so layers (sealed-step layer
    /// today) can read its cached snapshot through
    /// <see cref="RenderContext.SealedLineNumbers"/>. Pass <c>null</c>
    /// to detach.
    /// </summary>
    public void AttachClipEditor(ScriptClipEditor? clipEditor)
    {
        _context.SetClipEditor(clipEditor);
        // The Selection layer hosts the sealed-step layer; force a
        // repaint so the new clip's stripe + squiggle render against
        // the freshly-attached snapshot rather than whatever the
        // previous clip left on screen.
        _textArea.TextView.InvalidateLayer(KnownLayer.Selection);
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
            if (!layer.OnCaretChanged(_context)) continue;
            if (layer.TargetLayer == KnownLayer.Background) bgDirty = true;
            else selDirty = true;
        }

        if (bgDirty) _textArea.TextView.InvalidateLayer(KnownLayer.Background);
        if (selDirty) _textArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _textArea.Caret.PositionChanged -= OnCaretChanged;
        _textArea.TextView.BackgroundRenderers.Remove(_bgRenderer);
        _textArea.TextView.BackgroundRenderers.Remove(_selRenderer);
    }
}
