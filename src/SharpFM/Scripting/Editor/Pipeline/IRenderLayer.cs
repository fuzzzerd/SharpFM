using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace SharpFM.Scripting.Editor.Pipeline;

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
    /// Caret position changed. Layer should update its internal state
    /// and return <c>true</c> if its draw output would differ from
    /// the previous paint.
    /// </summary>
    bool OnCaretChanged(RenderContext ctx);

    /// <summary>
    /// Render the layer's contribution to the given drawing context.
    /// Called by AvaloniaEdit during paint via the IBackgroundRenderer
    /// wrapper.
    /// </summary>
    void Draw(RenderContext ctx, TextView textView, DrawingContext drawingContext);
}
