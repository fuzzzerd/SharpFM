using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using SharpFM.Schema.Editor;

namespace SharpFM.Editors;

/// <summary>
/// Builds the Avalonia control used to edit a given clip. Kept centralised so
/// the clip view-model can cache a single control instance per clip — tab
/// switches reparent that control rather than reconstructing it, which
/// matters because <see cref="ScriptTextEditor"/> installs TextMate on
/// construction and that is expensive.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ClipEditorViewFactory
{
    private static readonly FontFamily MonoFont =
        new("Cascadia Code,Consolas,Menlo,Monospace");

    public static Control Create(IClipEditor editor) => editor switch
    {
        ScriptClipEditor s => new ScriptTextEditor
        {
            FontFamily = MonoFont,
            // Step-index margin is installed by ScriptEditorController;
            // skipping the built-in line-number margin avoids adding a
            // margin we'll just remove (and that AvaloniaEdit can re-add
            // if the editor's template is re-applied later — in the
            // post-pipeline trace it was still rendering at 5.8s/30s).
            ShowLineNumbers = false,
            WordWrap = false,
            DataContext = s,
        },
        TableClipEditor t => new TableEditorControl
        {
            DataContext = t.ViewModel,
        },
        FallbackXmlEditor f => new TextEditor
        {
            FontFamily = MonoFont,
            ShowLineNumbers = true,
            WordWrap = false,
            Document = f.Document,
            SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Xml"),
        },
        _ => new TextBlock { Text = "No editor available for this clip type." },
    };
}
