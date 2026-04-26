using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    // Resolve a real installed face name at startup. Avalonia's font
    // cache (SystemFontCollection._glyphTypefaceCache) is keyed by the
    // family-name string the caller asks for, but populated under the
    // platform's RESOLVED face name — so requesting an alias like
    // "Monospace" or "Cascadia Code" on a system that doesn't carry
    // that face misses the cache forever. The trace showed every one
    // of 1283 typeface lookups falling through to the slow path. By
    // detecting an actually-installed face name once at startup and
    // using it as the editor font, every subsequent typeface request
    // hits the cache after the first.
    private static readonly FontFamily MonoFont = ResolveMonospaceFont();

    private static FontFamily ResolveMonospaceFont()
    {
        // Per-platform preference order. First entries are the "good"
        // monospace fonts we'd pick if available; tail entries are
        // safer last-resorts known to ship with the OS.
        string[] preferred =
            OperatingSystem.IsWindows() ? new[] { "Cascadia Code", "Cascadia Mono", "Consolas", "Lucida Console", "Courier New" } :
            OperatingSystem.IsMacOS()   ? new[] { "Menlo", "Monaco", "Courier New" } :
                                          new[] { "JetBrains Mono", "DejaVu Sans Mono", "Liberation Mono", "Noto Sans Mono", "Ubuntu Mono" };

        try
        {
            var installed = FontManager.Current.SystemFonts
                .Select(f => f.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var name in preferred)
            {
                if (installed.Contains(name))
                    return new FontFamily(name);
            }
        }
        catch
        {
            // FontManager not yet available during certain init orderings.
            // Fall through to the default below.
        }

        // No known monospace face installed — accept the alias path. We
        // pay the cache-miss cost but the editor still works.
        return new FontFamily("Monospace");
    }

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
