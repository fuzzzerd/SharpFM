using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Hosts a fixed set of <see cref="IRenderLayer"/> instances on a single
/// AvaloniaEdit <see cref="KnownLayer"/>. Per paint, dispatches Draw to
/// each layer in registration order. AvaloniaEdit only knows about this
/// wrapper — the four feature-specific layers it contains are invisible
/// to the editor's layering system.
/// </summary>
internal sealed class LayeredBackgroundRenderer : IBackgroundRenderer
{
    private readonly RenderContext _context;
    private readonly IReadOnlyList<IRenderLayer> _layers;

    public LayeredBackgroundRenderer(RenderContext context, KnownLayer layer, IReadOnlyList<IRenderLayer> layers)
    {
        _context = context;
        Layer = layer;
        _layers = layers;
    }

    public KnownLayer Layer { get; }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        foreach (var layer in _layers)
            layer.Draw(_context, textView, drawingContext);
    }
}
