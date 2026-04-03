using System;
using AvaloniaEdit.Document;

namespace SharpFM.Editors;

/// <summary>
/// Editor for clips with no specialized editor (layouts, unknown formats).
/// The user edits the raw XML directly via a TextDocument.
/// The saved XML string is the source of truth.
/// </summary>
public class FallbackXmlEditor : IClipEditor
{
    private string _savedXml;
    private bool _suppressDirty;

    public event EventHandler? BecameDirty;
    public event EventHandler? Saved;

    /// <summary>The TextDocument bound to the AvaloniaEdit XML editor.</summary>
    public TextDocument Document { get; }

    public bool IsDirty { get; private set; }
    public bool IsPartial => false;

    public FallbackXmlEditor(string? xml)
    {
        _savedXml = xml ?? "";
        Document = new TextDocument(_savedXml);

        Document.TextChanged += OnTextChanged;
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_suppressDirty) return;
        if (!IsDirty)
        {
            IsDirty = true;
            BecameDirty?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool Save()
    {
        _savedXml = Document.Text;
        IsDirty = false;
        Saved?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public string ToXml() => _savedXml;

    public void FromXml(string xml)
    {
        _savedXml = xml;

        _suppressDirty = true;
        try
        {
            Document.Text = xml;
        }
        finally
        {
            _suppressDirty = false;
        }

        IsDirty = false;
    }
}
