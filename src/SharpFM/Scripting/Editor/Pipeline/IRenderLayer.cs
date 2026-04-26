using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// When the pipeline asks the layer to recompute its state.
/// <list type="bullet">
///   <item><c>Realtime</c>: recomputed inside the pipeline's
///   <see cref="Caret.PositionChanged"/> handler — used when the
///   layer's draw output tracks the caret (bracket match,
///   statement highlight).</item>
///   <item><c>Idle</c>: recomputed inside a debounced
///   <see cref="TextDocument.TextChanged"/> handler — used when the
///   layer's draw output only changes with the document content,
///   not with the caret (sealed-step squiggle, error markers).
///   Skipping these out of the per-keystroke path keeps the hot
///   path tight without making the visual lag perceptible.</item>
/// </list>
/// </summary>
internal enum RenderCadence
{
    Realtime,
    Idle,
}

/// <summary>
/// One feature in the script editor's render pipeline (statement
/// highlight, bracket match, continuation rail, error squiggle,
/// sealed-step layer). Layers own only their own dirty state;
/// everything else they read from the shared <see cref="RenderContext"/>.
/// The pipeline owner dispatches lifecycle events to every layer and
/// consolidates invalidations into at most one <c>InvalidateLayer</c>
/// call per affected <see cref="KnownLayer"/>.
/// </summary>
internal interface IRenderLayer
{
    /// <summary>
    /// Which AvaloniaEdit layer this draws on. Determines which
    /// <c>IBackgroundRenderer</c> wrapper hosts the layer and which
    /// <c>InvalidateLayer</c> target the pipeline calls when the
    /// layer reports dirty.
    /// </summary>
    KnownLayer TargetLayer { get; }

    /// <summary>
    /// When the pipeline should drive this layer's recompute step.
    /// </summary>
    RenderCadence Cadence { get; }

    /// <summary>
    /// Caret position changed. Realtime layers do their work here.
    /// Idle layers should return false. Returns true if the layer's
    /// draw output would differ from the previous paint.
    /// </summary>
    bool OnCaretChanged(RenderContext ctx);

    /// <summary>
    /// Document text changed (debounced). Idle layers do their work
    /// here. Realtime layers should return false. Returns true if
    /// the layer's draw output would differ from the previous paint.
    /// </summary>
    bool OnTextChanged(RenderContext ctx);

    /// <summary>
    /// Render the layer's contribution to the given drawing context.
    /// Called by AvaloniaEdit during paint via the IBackgroundRenderer
    /// wrapper.
    /// </summary>
    void Draw(RenderContext ctx, TextView textView, DrawingContext drawingContext);
}
