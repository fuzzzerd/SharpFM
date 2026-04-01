using System;
using Avalonia.Threading;
using AvaloniaEdit.Document;

namespace SharpFM.Editors;

/// <summary>
/// Editor for clips with no specialized editor (layouts, unknown formats).
/// The user edits the raw XML directly via a TextDocument.
/// </summary>
public class FallbackXmlEditor : IClipEditor
{
    private readonly DispatcherTimer _debounceTimer;

    public event EventHandler? ContentChanged;

    /// <summary>The TextDocument bound to the AvaloniaEdit XML editor.</summary>
    public TextDocument Document { get; }

    public bool IsPartial => false;

    public FallbackXmlEditor(string? xml)
    {
        Document = new TextDocument(xml ?? "");

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

    public string ToXml() => Document.Text;

    public void FromXml(string xml)
    {
        Document.Text = xml;
    }
}
