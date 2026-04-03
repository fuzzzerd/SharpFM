using System;
using AvaloniaEdit.Document;
using SharpFM.Scripting;

namespace SharpFM.Editors;

/// <summary>
/// Editor for script clips (Mac-XMSS, Mac-XMSC). The FmScript model is the source of truth.
/// The TextDocument is a projection that the user edits. Save parses text back into the model.
/// </summary>
public class ScriptClipEditor : IClipEditor
{
    private FmScript _script;
    private bool _suppressDirty;

    public event EventHandler? BecameDirty;
    public event EventHandler? Saved;

    /// <summary>The TextDocument bound to the AvaloniaEdit script editor.</summary>
    public TextDocument Document { get; }

    /// <summary>The authoritative script model. Updated on Save or FromXml.</summary>
    public FmScript Script => _script;

    public bool IsDirty { get; private set; }
    public bool IsPartial { get; private set; }

    public ScriptClipEditor(string? xml)
    {
        _script = FmScript.FromXml(xml ?? "");
        Document = new TextDocument(_script.ToDisplayText());

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
        try
        {
            _script = FmScript.FromDisplayText(Document.Text);
            IsPartial = false;
            IsDirty = false;
            Saved?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch
        {
            IsPartial = true;
            return false;
        }
    }

    public string ToXml()
    {
        return _script.ToXml();
    }

    public void FromXml(string xml)
    {
        _script = FmScript.FromXml(xml);

        _suppressDirty = true;
        try
        {
            Document.Text = _script.ToDisplayText();
        }
        finally
        {
            _suppressDirty = false;
        }

        IsDirty = false;
        IsPartial = false;
    }
}
