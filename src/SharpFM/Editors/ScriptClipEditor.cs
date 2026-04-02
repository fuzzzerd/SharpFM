using System;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using SharpFM.Scripting;

namespace SharpFM.Editors;

/// <summary>
/// Editor for script clips (Mac-XMSS, Mac-XMSC). Wraps a TextDocument containing the
/// plain-text script representation and handles FmScript model round-tripping.
/// </summary>
public class ScriptClipEditor : IClipEditor
{
    private readonly DispatcherTimer _debounceTimer;
    private FmScript _script;

    public event EventHandler? ContentChanged;

    /// <summary>The TextDocument bound to the AvaloniaEdit script editor.</summary>
    public TextDocument Document { get; }

    public bool IsPartial { get; private set; }

    public ScriptClipEditor(string? xml)
    {
        _script = FmScript.FromXml(xml ?? "");
        Document = new TextDocument(_script.ToDisplayText());

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        };

        Document.TextChanged += (_, _) =>
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        };
    }

    public string ToXml()
    {
        try
        {
            _script = FmScript.FromDisplayText(Document.Text);
            IsPartial = false;
            return _script.ToXml();
        }
        catch
        {
            IsPartial = true;
            // Return best-effort XML from the last known good parse
            return _script.ToXml();
        }
    }

    public void FromXml(string xml)
    {
        _script = FmScript.FromXml(xml);
        Document.Text = _script.ToDisplayText();
    }
}
