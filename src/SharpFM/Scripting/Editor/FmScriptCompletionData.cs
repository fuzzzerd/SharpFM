using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.CodeCompletion;

namespace SharpFM.Scripting.Editor;

[ExcludeFromCodeCoverage]
public class FmScriptCompletionData : ICompletionData
{
    public FmScriptCompletionData(string text, string? description = null, double priority = 0)
    {
        Text = text;
        Description = description ?? text;
        Priority = priority;
    }

    public IImage? Image => null;
    public string Text { get; }
    public object Content => new TextBlock { Text = Text };
    public object Description { get; }
    public double Priority { get; }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
