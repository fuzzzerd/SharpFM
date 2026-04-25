using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Owns the script editor's background render layers and consolidates
/// their event handling + invalidation. Replaces the four standalone
/// <c>IBackgroundRenderer</c> implementations (bracket, statement,
/// continuation, error) with one shared pipeline:
///
/// <list type="bullet">
///   <item>One subscription to <see cref="Caret.PositionChanged"/>;
///   layers are dispatched in order and each reports whether its
///   draw output changed.</item>
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
    private readonly LayeredBackgroundRenderer _bgRenderer;
    private readonly LayeredBackgroundRenderer _selRenderer;
    private bool _disposed;

    public ScriptEditorRenderPipeline(TextArea textArea)
    {
        _textArea = textArea;
        _context = new RenderContext(textArea);

        _statementLayer = new StatementHighlightLayer();
        _continuationLayer = new ContinuationRailLayer();
        _bracketLayer = new BracketMatchLayer();
        _errorLayer = new ErrorMarkerLayer();

        _bgRenderer = new LayeredBackgroundRenderer(
            _context,
            KnownLayer.Background,
            new IRenderLayer[] { _statementLayer, _continuationLayer });

        _selRenderer = new LayeredBackgroundRenderer(
            _context,
            KnownLayer.Selection,
            new IRenderLayer[] { _bracketLayer, _errorLayer });

        textArea.TextView.BackgroundRenderers.Add(_bgRenderer);
        textArea.TextView.BackgroundRenderers.Add(_selRenderer);

        textArea.Caret.PositionChanged += OnCaretChanged;
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

        if (_statementLayer.OnCaretChanged(_context))
        {
            if (_statementLayer.TargetLayer == KnownLayer.Background) bgDirty = true;
            else selDirty = true;
        }
        if (_continuationLayer.OnCaretChanged(_context))
        {
            if (_continuationLayer.TargetLayer == KnownLayer.Background) bgDirty = true;
            else selDirty = true;
        }
        if (_bracketLayer.OnCaretChanged(_context))
        {
            if (_bracketLayer.TargetLayer == KnownLayer.Background) bgDirty = true;
            else selDirty = true;
        }
        if (_errorLayer.OnCaretChanged(_context))
        {
            if (_errorLayer.TargetLayer == KnownLayer.Background) bgDirty = true;
            else selDirty = true;
        }

        // One InvalidateLayer call per dirty layer, max one per known
        // target. Multiple feature layers reporting dirty on the same
        // KnownLayer collapse into a single repaint.
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
