using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// Applies italic font style to sealed-step lines so they read as
/// "annotation / read-only summary" rather than as first-class code.
/// </summary>
[ExcludeFromCodeCoverage]
public class SealedStepItalicColorizer : DocumentColorizingTransformer
{
    private readonly ScriptClipEditor _editor;

    public SealedStepItalicColorizer(ScriptClipEditor editor)
    {
        _editor = editor;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        foreach (var anchor in _editor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            var anchorLine = CurrentContext.Document.GetLineByOffset(anchor.Offset);
            if (anchorLine.LineNumber != line.LineNumber) continue;

            ChangeLinePart(line.Offset, line.EndOffset, element =>
            {
                element.TextRunProperties.SetTypeface(
                    new Typeface(element.TextRunProperties.Typeface.FontFamily, FontStyle.Italic));
            });
            return;
        }
    }
}
