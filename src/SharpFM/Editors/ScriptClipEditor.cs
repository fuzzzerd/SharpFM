using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.Document;
using SharpFM.Scripting;

namespace SharpFM.Editors;

/// <summary>
/// Editor for script clips (Mac-XMSS, Mac-XMSC). The FmScript model is the source of truth.
/// The TextDocument is a projection that the user edits. Save parses text back into the model,
/// merging with the existing model to preserve multi-line content (comments, calculations)
/// that is only shown truncated in the text editor.
/// </summary>
public class ScriptClipEditor : IClipEditor
{
    private FmScript _script;
    private string[] _renderedLines;
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
        _renderedLines = _script.ToDisplayLines();
        Document = new TextDocument(string.Join("\n", _renderedLines));

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
            var textParsed = FmScript.FromDisplayText(Document.Text);
            _script = MergeWithModel(textParsed);
            _renderedLines = _script.ToDisplayLines();

            // Re-render to normalize the document text with the merged model
            _suppressDirty = true;
            try
            {
                Document.Text = string.Join("\n", _renderedLines);
            }
            finally
            {
                _suppressDirty = false;
            }

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
        _renderedLines = _script.ToDisplayLines();

        _suppressDirty = true;
        try
        {
            Document.Text = string.Join("\n", _renderedLines);
        }
        finally
        {
            _suppressDirty = false;
        }

        IsDirty = false;
        IsPartial = false;
    }

    /// <summary>
    /// Merge text-parsed steps with the existing model. For each line in the text:
    /// - If it matches the rendered line from the model at the same position,
    ///   keep the model step (preserves multi-line content shown truncated).
    /// - If it differs, take the text-parsed step (user edited this line).
    /// </summary>
    private FmScript MergeWithModel(FmScript textParsed)
    {
        var merged = new List<ScriptStep>();
        var textSteps = textParsed.Steps;
        var modelSteps = _script.Steps;

        // Build display lines for each text-parsed step (what the user typed, normalized)
        var textLines = textSteps.Select(s => NormalizeDisplayLine(s)).ToList();

        for (int i = 0; i < textSteps.Count; i++)
        {
            // Check if this line matches the model at the same index
            if (i < modelSteps.Count && i < _renderedLines.Length)
            {
                var rendered = NormalizeLine(_renderedLines[i]);
                var typed = NormalizeLine(textLines[i]);

                if (rendered == typed)
                {
                    // User didn't change this line — preserve model step (with full multi-line content)
                    merged.Add(modelSteps[i]);
                    continue;
                }
            }

            // User changed this line (or it's a new line) — use the text-parsed step
            merged.Add(textSteps[i]);
        }

        return new FmScript(merged);
    }

    private static string NormalizeDisplayLine(ScriptStep step)
    {
        var line = step.ToDisplayLine();
        if (!step.Enabled) line = $"// {line}";
        return line;
    }

    private static string NormalizeLine(string line)
    {
        return line.Trim();
    }
}
