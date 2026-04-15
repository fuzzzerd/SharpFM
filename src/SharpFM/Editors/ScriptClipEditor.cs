using System;
using AvaloniaEdit.Document;
using SharpFM.Model.Scripting;
using SharpFM.Scripting;

namespace SharpFM.Editors;

/// <summary>
/// Editor for script clips (Mac-XMSS, Mac-XMSC). Wraps a TextDocument containing the
/// plain-text script representation and handles FmScript model round-tripping.
/// <para>
/// Script wrapper metadata (Mac-XMSC only) is cached here because display
/// text carries only steps, not wrapper attrs. On <see cref="ToXml"/>, the
/// cached metadata is re-applied so the emitted XML keeps its <c>&lt;Script&gt;</c>
/// envelope across display-text edits.
/// </para>
/// </summary>
public class ScriptClipEditor : IClipEditor
{
    private readonly DebouncedEventRaiser _debouncer;
    private FmScript _script;
    private ScriptMetadata? _metadata;

    public event EventHandler? ContentChanged;

    /// <summary>The TextDocument bound to the AvaloniaEdit script editor.</summary>
    public TextDocument Document { get; }

    public bool IsPartial { get; private set; }

    public ScriptClipEditor(string? xml)
    {
        _script = FmScript.FromXml(xml ?? "");
        _metadata = _script.Metadata;
        Document = new TextDocument(_script.ToDisplayText());

        _debouncer = new DebouncedEventRaiser(500, () => ContentChanged?.Invoke(this, EventArgs.Empty));
        Document.TextChanged += (_, _) => _debouncer.Trigger();
    }

    public string ToXml()
    {
        try
        {
            _script = ScriptTextParser.FromDisplayText(Document.Text);
            _script.Metadata = _metadata;
            IsPartial = false;
            return _script.ToXml();
        }
        catch
        {
            IsPartial = true;
            _script.Metadata = _metadata;
            return _script.ToXml();
        }
    }

    public void FromXml(string xml)
    {
        _script = FmScript.FromXml(xml);
        _metadata = _script.Metadata;
        Document.Text = _script.ToDisplayText();
    }
}
