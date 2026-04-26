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
    // Use a single font name so Avalonia/Skia resolves through the
    // platform font manager once per Typeface request. The previous
    // comma-separated fallback chain ("Cascadia Code,Consolas,Menlo,
    // Monospace") forced every text-run shape pass to walk the chain
    // looking for missing fonts on platforms where most of those
    // names don't resolve — on Linux, three of the four miss, and the
    // trace showed 2321 native font lookups in a 30s window with
    // ~8.7 lookups per visual-line build. "Monospace" is the standard
    // generic alias resolved to the platform's default monospace face
    // (DejaVu Sans Mono on Linux, Menlo on macOS, Consolas on Windows).
    private static readonly FontFamily MonoFont = new("Monospace");

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
