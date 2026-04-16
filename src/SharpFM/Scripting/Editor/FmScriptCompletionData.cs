using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.CodeCompletion;

namespace SharpFM.Scripting.Editor;

[ExcludeFromCodeCoverage]
public class FmScriptCompletionData : ICompletionData
{
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
        if (_snippet != null)
        {
            // Convert Monaco snippet syntax to plain text with first placeholder selected
            // ${1:placeholder} → placeholder (selected), ${2:text} → text, $0 removed
            var (plainText, selectStart, selectLength) = ConvertSnippet(_snippet);
            textArea.Document.Replace(completionSegment, plainText);

            if (selectStart >= 0 && selectLength > 0)
            {
                var offset = completionSegment.Offset + selectStart;
                textArea.Caret.Offset = offset;
                textArea.Selection = Selection.Create(textArea, offset, offset + selectLength);
            }
        }
        else
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }

    /// <summary>
    /// Convert Monaco snippet syntax to plain text + first placeholder selection info.
    /// </summary>
    private static (string Text, int SelectStart, int SelectLength) ConvertSnippet(string snippet)
    {
        var selectStart = -1;
        var selectLength = 0;

        // Remove $0 (final cursor position marker)
        var text = snippet.Replace("$0", "");

        // Replace ${N:placeholder} with just the placeholder text
        // Track the first placeholder (${1:...}) for selection
        text = Regex.Replace(text, @"\$\{(\d+):([^}]*)\}", match =>
        {
            var index = int.Parse(match.Groups[1].Value);
            var placeholder = match.Groups[2].Value;

            if (index == 1 && selectStart < 0)
            {
                // Calculate position in the output string up to this point
                selectStart = match.Index;
                // Adjust for previous replacements — we need the position in the final string
                selectLength = placeholder.Length;
            }

            return placeholder;
        });

        // Recalculate selectStart since Regex.Replace processes sequentially
        // but match.Index is from the original string. Do it properly:
        if (selectStart >= 0)
        {
            var firstPlaceholder = Regex.Match(snippet, @"\$\{1:([^}]*)\}");
            if (firstPlaceholder.Success)
            {
                // Count the plain text before the first placeholder
                var beforeSnippet = snippet.Substring(0, firstPlaceholder.Index);
                // Remove any snippet markers that come before it
                var beforePlain = Regex.Replace(beforeSnippet, @"\$\{\d+:([^}]*)\}", "$1")
                    .Replace("$0", "");
                selectStart = beforePlain.Length;
                selectLength = firstPlaceholder.Groups[1].Value.Length;
            }
        }

        // Clean up backslash escapes (e.g., \$ → $) and trailing whitespace
        text = text.Replace("\\$", "$").TrimEnd('\t', '\n', '\r');

        return (text, selectStart, selectLength);
    }
}
