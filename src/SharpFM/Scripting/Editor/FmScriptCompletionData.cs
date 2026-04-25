using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Snippets;

namespace SharpFM.Scripting.Editor;

[ExcludeFromCodeCoverage]
public class FmScriptCompletionData : ICompletionData
{
    private static readonly Regex PlaceholderRegex =
        new(@"\$\{(\d+):([^}]*)\}|\$0", RegexOptions.Compiled);

    private readonly string? _snippet;

    public FmScriptCompletionData(string text, string? description = null,
        double priority = 0, string? snippet = null)
    {
        Text = text;
        Description = description ?? text;
        Priority = priority;
        _snippet = snippet;
    }

    public IImage? Image => null;
    public string Text { get; }
    public object Content => new TextBlock { Text = Text };
    public object Description { get; }
    public double Priority { get; }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        if (_snippet == null)
        {
            textArea.Document.Replace(completionSegment, Text);
            return;
        }

        // Use AvaloniaEdit's snippet engine for the insert — that gives us
        // per-placeholder selection plus Tab-through navigation across all
        // ${N:...} stops natively. Replacing the segment with empty first
        // positions the caret where the snippet should expand.
        var snippet = ParseMonacoSnippet(_snippet);
        textArea.Document.Replace(completionSegment, "");
        textArea.Caret.Offset = completionSegment.Offset;
        snippet.Insert(textArea);
    }

    /// <summary>
    /// Convert a Monaco-style snippet (<c>${N:placeholder}</c> / <c>$0</c>)
    /// into an AvaloniaEdit <see cref="Snippet"/>. Each <c>${N:..}</c>
    /// becomes a <see cref="SnippetReplaceableTextElement"/> — the engine
    /// hooks Tab to advance through them. <c>$0</c> becomes a
    /// <see cref="SnippetCaretElement"/> marking where the caret lands
    /// after the user finishes the last replaceable.
    /// </summary>
    private static Snippet ParseMonacoSnippet(string template)
    {
        var snippet = new Snippet();
        int pos = 0;
        foreach (Match m in PlaceholderRegex.Matches(template))
        {
            if (m.Index > pos)
            {
                snippet.Elements.Add(new SnippetTextElement
                {
                    Text = Unescape(template.Substring(pos, m.Index - pos)),
                });
            }
            if (m.Value == "$0")
            {
                snippet.Elements.Add(new SnippetCaretElement());
            }
            else
            {
                snippet.Elements.Add(new SnippetReplaceableTextElement
                {
                    Text = m.Groups[2].Value,
                });
            }
            pos = m.Index + m.Length;
        }
        if (pos < template.Length)
        {
            snippet.Elements.Add(new SnippetTextElement
            {
                Text = Unescape(template.Substring(pos)),
            });
        }
        return snippet;
    }

    private static string Unescape(string s) => s.Replace("\\$", "$");
}
