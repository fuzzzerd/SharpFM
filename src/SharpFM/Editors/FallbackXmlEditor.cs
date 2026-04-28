using System;
using AvaloniaEdit.Document;
using SharpFM.Model.Parsing;

namespace SharpFM.Editors;

/// <summary>
/// Editor for clips with no specialized editor (layouts, unknown formats).
/// The user edits the raw XML directly via a TextDocument.
/// </summary>
public class FallbackXmlEditor : IClipEditor
{
    private readonly DebouncedEventRaiser _debouncer;

    public event EventHandler? ContentChanged;

    /// <summary>The TextDocument bound to the AvaloniaEdit XML editor.</summary>
    public TextDocument Document { get; }

    public bool IsPartial => false;

    public FallbackXmlEditor(string? xml)
    {
        Document = new TextDocument(xml ?? "");

        _debouncer = new DebouncedEventRaiser(500, () => ContentChanged?.Invoke(this, EventArgs.Empty));
        Document.TextChanged += (_, _) => _debouncer.Trigger();
    }

    public string ToXml() => Document.Text;

    public ClipModel GetModel() => new OpaqueClipModel(Document.Text);
}
